using System;
using System.Collections.Generic;
using System.Linq;
using MissionCore.Budgets;
using MissionCore.Catalogs;
using MissionCore.Design;
using MissionCore.Earth;
using MissionCore.Mission;
using MissionCore.Numerics;
using MissionCore.Rules;

namespace MissionCore.Simulation
{
    public sealed class SimParameters
    {
        public double TickS { get; private set; }
        public double EpochS { get; private set; }                 // mission start, seconds since J2000
        public double DurationS { get; private set; }

        public double SolarPowerW { get; private set; }            // in full sunlight, begin of life
        public double SolarDegradationPerYear { get; private set; }
        public double BusPowerW { get; private set; }
        public double SafeModeBusPowerW { get; private set; }
        public double InstrumentPowerW { get; private set; }
        public double CommsPowerW { get; private set; }

        public double InstrumentDataBps { get; private set; }      // sum of data_rate_bps × duty_cycle
        public double DownlinkBps { get; private set; }
        public IReadOnlyList<GroundStationSpec> Stations { get; private set; }  // only those matching the band

        public double BatteryCapacityWh { get; private set; }
        public double StorageCapacityBits { get; private set; }
        public double BallisticCoefficient { get; private set; }   // Cd · A / m [m²/kg]
        public double DensityFactor { get; private set; }          // solar activity

        public LoadSheddingRules LoadShedding { get; private set; }

        public double RaanRad { get; private set; }
        public double MaxBatteryEmptyS { get; private set; }
        public IReadOnlyList<FixedEvent> FixedEvents { get; private set; }

        public static SimParameters From(Catalog catalog, MissionDesign design)
        {
            if (catalog.Sim == null) throw new ArgumentException("Sim Rules is null", nameof(catalog));

            if (string.IsNullOrEmpty(design.MissionId) || !catalog.Missions.TryGetValue(design.MissionId, out MissionTemplate mission))
                throw new ArgumentException($"Mission '{design.MissionId}' not found", nameof(design));

            SolarCellSpec solarCellSpec = null;
            if (!string.IsNullOrEmpty(design.SolarCellId)) catalog.SolarCells.TryGetValue(design.SolarCellId, out solarCellSpec);

            if (string.IsNullOrEmpty(design.PlatformId) || !catalog.Platforms.TryGetValue(design.PlatformId, out PlatformSpec platformSpec))
                throw new ArgumentException($"Platform '{design.PlatformId}' not found", nameof(design));

            List<InstrumentSpec> instrumentSpecs = new();

            foreach (string id in design.InstrumentIds)
            {
                if (catalog.Instruments.TryGetValue(id, out InstrumentSpec instrumentSpec)) instrumentSpecs.Add(instrumentSpec);
            }

            CommsSpec commsSpec = null;
            if (!string.IsNullOrEmpty(design.CommsId)) catalog.Comms.TryGetValue(design.CommsId, out commsSpec);

            if (string.IsNullOrEmpty(design.BatteryId) || !catalog.Batteries.TryGetValue(design.BatteryId, out BatterySpec batterySpec))
                throw new ArgumentException($"Battery '{design.BatteryId}' not found", nameof(design));

            DesignReport report = DesignValidator.Evaluate(catalog, design);
            double massKg = report.MassResult.DryMassKg + report.MassResult.PropellantMassKg;

            return new SimParameters
            {
                TickS = catalog.Sim.TickS,
                EpochS = mission.EpochSeconds,
                DurationS = mission.durationDays * CoreConstants.SecondsPerDay,
                SolarPowerW = solarCellSpec == null ? 0 :
                    CoreConstants.SolarConstant *
                    design.SolarAreaM2 *
                    solarCellSpec.packingFactor *
                    solarCellSpec.efficiency,
                SolarDegradationPerYear = solarCellSpec?.degradationPerYear ?? 0,
                BusPowerW = platformSpec.busPowerW,
                SafeModeBusPowerW = platformSpec.busPowerW * catalog.Sim.SafeModeBusPowerFactor,
                InstrumentPowerW = instrumentSpecs.Sum(i => i.powerW),
                CommsPowerW = commsSpec?.powerW ?? 0,
                InstrumentDataBps = new DataBudget().GetGeneratedBitsPerDay(catalog, design) / CoreConstants.SecondsPerDay,
                DownlinkBps = commsSpec?.dataRateBpS ?? 0,
                Stations = new DataBudget().GetCompatibleStations(catalog, design, commsSpec?.band),
                BatteryCapacityWh = batterySpec == null ? 0 : batterySpec.energyWh * design.BatteryCount,
                StorageCapacityBits = platformSpec.storageBits,
                BallisticCoefficient = OrbitDecay.DragCoefficient * LifetimeBudget.DragAreaM2(platformSpec, design) / massKg,
                DensityFactor = catalog.Sim.DensityFactor(mission.solarActivity),
                LoadShedding = catalog.Sim.LoadShedding,
                RaanRad = mission.raanDeg * CoreConstants.DegToRad,
                MaxBatteryEmptyS = catalog.Sim.MaxBatteryEmptyS,
                FixedEvents = mission.fixedEvents ?? new List<FixedEvent>()
            };
        }
    }
}