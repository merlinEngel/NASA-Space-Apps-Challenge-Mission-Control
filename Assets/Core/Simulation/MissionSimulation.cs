using System;
using System.Collections.Generic;
using MissionCore.Bodies;
using MissionCore.Budgets;
using MissionCore.Catalogs;
using MissionCore.Earth;
using MissionCore.Mission;
using MissionCore.Numerics;
using MissionCore.Rules;
using MissionCore.Time;

namespace MissionCore.Simulation
{
    public sealed class MissionSimulation
    {
        public SimParameters Parameters { get; private set; }
        public SimState State { get; private set; }
        public SimLog Log { get; private set; }
        public SimClock Clock { get; private set; }
        public int Seed { get; private set; }
        private KeplerOrbit orbit;
        private Random random;
        private bool safeModeAuto;
        private bool safeModeCommanded;
        private bool instrumentsShed;
        private LogCause InstrumentsOffCause => safeModeCommanded ? LogCause.SafeMode : LogCause.EnergyShortage; // Why the instruments are off: commanded safe mode is the player's choice, everything else is lack of energy.
        double batteryEmptyS = 0;
        public IReadOnlyList<ScheduledEvent> EventSchedule { get; private set; }
        const int SampleEveryTicks = 10;   // one point every 10 min for the curves in P7

        private MissionSimulation() { }

        public static MissionSimulation Start(Catalog catalog, MissionDesign design, int seed)
        {
            SimParameters parameters = SimParameters.From(catalog, design);

            KeplerOrbit orbit = J2Drift.ApplyTo(KeplerOrbit.Circular(design.AltitudeM, design.InclinationDeg * Constants.DegToRad, parameters.RaanRad, Constants.RadiusEarth, Constants.MuEarth, parameters.EpochS));

            SimClock clock = new(parameters.TickS, parameters.EpochS);

            Random random = new(seed);

            Vec3d startPosition = orbit.PositionAt(parameters.EpochS);
            SimState state = new(
                0,
                startPosition,
                design.AltitudeM,
                false,
                false,
                parameters.BatteryCapacityWh,
                parameters.BatteryCapacityWh,
                0,
                parameters.StorageCapacityBits,
                0,
                0,
                design.PropellantMassKg,
                true,
                false,
                EndReason.None
            );

            SimLog log = new();

            return new MissionSimulation
            {
                Parameters = parameters,
                State = state,
                Log = log,
                Clock = clock,
                Seed = seed,
                orbit = orbit,
                random = random,
                EventSchedule = ScheduleEvents(parameters, random)
            };
        }

        private static List<ScheduledEvent> ScheduleEvents(SimParameters parameters, Random random)
        {
            var schedule = new List<ScheduledEvent>();
            foreach (FixedEvent fixedEvent in parameters.FixedEvents)
            {
                double day = fixedEvent.day.Sample(random);
                double timeS = Math.Floor(day * Constants.SecondsPerDay / parameters.TickS) * parameters.TickS;
                schedule.Add(new ScheduledEvent(fixedEvent.type, timeS));
            }
            schedule.Sort((a, b) => a.TimeS.CompareTo(b.TimeS));
            return schedule;
        }

        // Derives State.SafeMode and State.InstrumentsOn from the three flags.
        private void ApplyModes()
        {
            State.SafeMode = safeModeAuto || safeModeCommanded;
            State.InstrumentsOn = !State.SafeMode && !instrumentsShed;
        }

        // Advances the mission by simSeconds of simulated time, in whole ticks only.
        // The remainder is kept for the next call. Returns the number of ticks executed.
        public int Step(double simSeconds)
        {
            if (State.Ended) return 0;
            int due = Clock.Advance(simSeconds);
            int executedTicks = due;
            for (int i = 0; i < due; i++)
            {
                Tick();
                if (State.Ended)
                {
                    executedTicks = i + 1;
                    break;
                }
            }
            return executedTicks;
        }

        private void Tick()
        {
            double t = Clock.SimTime;            // absolute time at the start of this tick (s since J2000)
            double startS = Clock.MissionTime;   // mission time at the start, for the log

            UpdateOrbit(t);
            UpdateShadow(t);
            UpdateContact(t);
            UpdateEnergy(startS);
            UpdateData(startS);
            UpdateLoadShedding();
            UpdateDecay();

            Clock.Tick();
            State.MissionTimeS = Clock.MissionTime;

            CheckEnd();

            //Add Curve Samples
            if (Clock.StepIndex % SampleEveryTicks == 0)
                Log.AddSample(new SimSample(State.MissionTimeS, State.BatterySoc, State.StorageBits, State.AltitudeM));
        }

        private void CheckEnd()
        {
            if (State.AltitudeM < OrbitDecay.ReentryAltitudeM) State.EndReason = EndReason.Reentry;
            else if (batteryEmptyS > Parameters.MaxBatteryEmptyS) State.EndReason = EndReason.BatteryDepleted;
            else if (State.MissionTimeS >= Parameters.DurationS) State.EndReason = EndReason.DurationReached;
        }

        private void UpdateDecay()
        {
            double rate = OrbitDecay.DecayRate(State.AltitudeM, Parameters.BallisticCoefficient, Parameters.DensityFactor);
            State.AltitudeM += rate * Parameters.TickS;
        }

        private void UpdateData(double startS)
        {
            //Generated Data
            double generatedBits = Parameters.InstrumentDataBps * Parameters.TickS;
            if (!State.InstrumentsOn)
            {
                State.LostBits += generatedBits;
                Log.Add(startS, Parameters.TickS, LogQuantity.DataBits, InstrumentsOffCause, generatedBits);
            }
            else
                State.StorageBits += generatedBits;

            //Sent Data (Downlink)
            if (State.InContact)
            {
                double sent = Math.Min(State.StorageBits, Parameters.DownlinkBps * Parameters.TickS);
                State.StorageBits -= sent;
                State.DeliveredBits += sent;
            }

            //Overflow
            double overflow = State.StorageBits - State.StorageCapacityBits;
            if (overflow > 0)
            {
                Log.Add(startS, Parameters.TickS, LogQuantity.DataBits, LogCause.StorageFull, overflow);
                State.LostBits += overflow;
                State.StorageBits = Math.Min(State.StorageBits, State.StorageCapacityBits);
            }
        }

        // Player command: switch safe mode on or off. Takes effect from the next tick.
        public void Command(bool safeMode)
        {
            safeModeCommanded = safeMode;
            ApplyModes();
        }

        private void UpdateLoadShedding()
        {
            double soc = State.BatterySoc;
            LoadSheddingRules rules = Parameters.LoadShedding;

            if (soc < rules.InstrumentsOffBelowSoc) instrumentsShed = true;
            if (soc < rules.SafeModeBelowSoc) safeModeAuto = true;
            if (soc >= rules.InstrumentsOnAboveSoc)
            {
                instrumentsShed = false;
                safeModeAuto = false;
            }

            ApplyModes();
        }

        private void UpdateEnergy(double startS)
        {
            bool safeMode = State.SafeMode;
            double consumptionW = 0;
            if (State.InstrumentsOn) consumptionW += Parameters.InstrumentPowerW;
            consumptionW += safeMode ? Parameters.SafeModeBusPowerW : Parameters.BusPowerW;
            consumptionW += State.InContact ? Parameters.CommsPowerW : 0;

            double productionW = State.InShadow ? 0 : Parameters.SolarPowerW * Math.Pow(1 - Parameters.SolarDegradationPerYear, startS / (Constants.DaysPerYear * Constants.SecondsPerDay));

            double total = (productionW - consumptionW) * Parameters.TickS / 3600; //Total [W] * 60 -> Energy for tick in Ws /3600 -> Energy for tick in Wh
            State.BatteryWh = Math.Min(total + State.BatteryWh, State.BatteryCapacityWh);
            if (State.BatteryWh < 0)
            {
                double missing = -State.BatteryWh;
                State.BatteryWh = 0;
                Log.Add(startS, Parameters.TickS, LogQuantity.EnergyWh, LogCause.EnergyShortage, missing);
            }

            if (State.BatteryWh <= 0) batteryEmptyS += Parameters.TickS;
            else batteryEmptyS = 0;
        }

        private void UpdateContact(double t)
        {
            double rotation = EarthRotation.Gmst(t);
            bool inContact = false;
            foreach (GroundStationSpec station in Parameters.Stations)
            {
                if (station.minElevationDeg <= GroundContact.ElevationDeg(State.PositionM, station.latDeg, station.lonDeg, rotation))
                {
                    inContact = true;
                    break;
                }
            }

            State.InContact = inContact;
        }

        private void UpdateShadow(double t)
        {
            State.InShadow = Shadow.IsInShadow(State.PositionM, Sun.Direction(t));
        }

        private void UpdateOrbit(double t)
        {
            Vec3d direction = orbit.PositionAt(t).Normalized;
            State.PositionM = direction * (Constants.RadiusEarth + State.AltitudeM);
        }
    }
}