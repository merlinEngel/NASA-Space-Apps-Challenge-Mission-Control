using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using MissionCore.Catalogs;
using MissionCore.Mission;
using MissionCore.Simulation;

namespace MissionCore.Tests
{
    // P6 step 3: energy flow, load shedding with hysteresis, safe mode
    public class MissionSimulationEnergyTests
    {
        const double Day = 86400;
        const int TicksPerDay = 1440;
        const double Tick = 60;

        // Tech demo "good": solar 1361 × 0.2 × 0.85 × 0.30 = 69.411 W, bus 8 W, camera 6 W, S-band 8 W
        const double GoodSolarW = 69.411;
        const double GoodBusW = 8, GoodCameraW = 6, GoodCommsW = 8;

        static MissionDesign Design(string file)
        {
            string root = Directory.GetParent(TestHelpers.StreamingDataDir()).Parent.Parent.FullName;
            string path = Path.Combine(root, "core.tests", "Data", "designs", file);
            return CatalogLoader.LoadObject<MissionDesign>(File.ReadAllText(path), file);
        }

        static MissionSimulation Start(MissionDesign design) =>
            MissionSimulation.Start(MissionSimulationStartTests.RealCatalog(), design, 42);

        static MissionSimulation StartGood() => Start(Design("techdemo_good.json"));

        // Expected change of the battery in one tick, from the flags the tick was computed with
        static double ExpectedDeltaWh(bool inShadow, bool inContact, double solarW, bool instrumentsOn = true)
        {
            double produced = inShadow ? 0 : solarW;
            double used = GoodBusW + (instrumentsOn ? GoodCameraW : 0) + (inContact ? GoodCommsW : 0);
            return (produced - used) * Tick / 3600;
        }

        // Checks every tick against the hand formula. When the battery clips at the top it is reset to
        // half full, so the next ticks are checked unclipped again.
        static void CheckTicks(MissionSimulation sim, int ticks, double solarW, bool requireAllCases = true)
        {
            bool sawLight = false, sawShadow = false, sawContact = false;
            for (int i = 0; i < ticks; i++)
            {
                double before = sim.State.BatteryWh;
                sim.Step(Tick);
                double expected = ExpectedDeltaWh(sim.State.InShadow, sim.State.InContact, solarW);
                if (sim.State.BatteryWh >= sim.State.BatteryCapacityWh - 1e-9) { sim.State.BatteryWh = 80; continue; }   // clipped at the top: refill to half and go on
                // 5e-5 Wh: ageing during the checked half day itself (< 1e-3 W) is ignored by the hand formula
                Assert.That(sim.State.BatteryWh - before, Is.EqualTo(expected).Within(5e-5),
                    $"tick {i}: shadow={sim.State.InShadow}, contact={sim.State.InContact}");

                if (sim.State.InShadow) sawShadow = true; else if (!sim.State.InContact) sawLight = true;
                if (sim.State.InContact) sawContact = true;
            }
            if (requireAllCases)
                Assert.That(sawLight && sawShadow && sawContact, Is.True, "test day should contain light, shadow and contact ticks");
        }

        [Test]
        public void Energy_EachTick_MatchesHandFormula()
        {
            MissionSimulation sim = StartGood();
            sim.State.BatteryWh = 80;   // half full: no clipping at the top within a few hours

            // Light, no contact: +(69.411 − 14) / 60 = +0.9235 Wh; shadow: −14 / 60 = −0.2333 Wh;
            // contact adds 8 W: −0.1333 Wh per tick
            Assert.That(ExpectedDeltaWh(false, false, GoodSolarW), Is.EqualTo(0.9235).Within(1e-4));
            Assert.That(ExpectedDeltaWh(true, false, GoodSolarW), Is.EqualTo(-0.2333).Within(1e-4));
            Assert.That(ExpectedDeltaWh(true, true, GoodSolarW), Is.EqualTo(-0.3667).Within(1e-4));

            // Contact passes over Svalbard and Weilheim come within the first half day
            CheckTicks(sim, 720, GoodSolarW);
        }

        [Test]
        public void Energy_FullBatteryInLight_StaysAtCapacity()
        {
            MissionSimulation sim = StartGood();

            for (int i = 0; i < TicksPerDay; i++)
            {
                sim.Step(Tick);
                Assert.That(sim.State.BatteryWh, Is.LessThanOrEqualTo(160 + 1e-9));
                if (!sim.State.InShadow && i > 0)
                    Assert.That(sim.State.BatteryWh, Is.GreaterThan(150));
            }
        }

        [Test]
        public void Energy_GoodDesign_SurvivesEveryEclipse()
        {
            MissionSimulation sim = StartGood();

            for (int i = 0; i < TicksPerDay; i++)
            {
                sim.Step(Tick);
                Assert.That(sim.State.BatterySoc, Is.GreaterThan(0.95));
                Assert.That(sim.State.InstrumentsOn, Is.True);
                Assert.That(sim.State.SafeMode, Is.False);
            }
            Assert.That(sim.Log.Total(LogQuantity.EnergyWh, LogCause.EnergyShortage), Is.EqualTo(0));
        }

        [Test]
        // Updated in step 5: the tech demo now ends after 90 days, so the check runs at day 80
        public void Energy_AfterEightyDays_SolarCellsHaveAged()
        {
            MissionSimulation sim = StartGood();
            for (int i = 0; i < 160; i++) sim.Step(Day / 2);   // 720 ticks per call, below MaxStepsPerAdvance
            Assert.That(sim.State.MissionTimeS, Is.EqualTo(80 * Day));

            // 1 % per year: after 80 days the cells deliver 0.99^(80 / 365.25) = 99.78 %
            double agedSolarW = GoodSolarW * Math.Pow(0.99, 80 / 365.25);
            Assert.That(agedSolarW / GoodSolarW, Is.EqualTo(0.997801).Within(1e-5));

            sim.State.BatteryWh = 80;
            CheckTicks(sim, 720, agedSolarW, requireAllCases: false);   // late August: the SSO may see no eclipse
        }

        [Test]
        public void LoadShedding_LowPowerDesign_SwitchesWithHysteresis()
        {
            // 6U, camera 6 W, 0.05 m² silicon = 9.83 W, bus 5 W, one 20 Wh battery:
            // in light the camera alone already drains the battery, so it is shed again and again.
            MissionSimulation sim = Start(Design("low_power.json"));
            int offs = 0, ons = 0;
            bool wasOn = sim.State.InstrumentsOn;

            for (int i = 0; i < TicksPerDay; i++)
            {
                sim.Step(Tick);
                bool on = sim.State.InstrumentsOn;
                if (wasOn && !on)
                {
                    offs++;
                    Assert.That(sim.State.BatterySoc, Is.LessThan(0.20), $"switched off at SoC {sim.State.BatterySoc:F3}");
                }
                if (!wasOn && on)
                {
                    ons++;
                    Assert.That(sim.State.BatterySoc, Is.GreaterThanOrEqualTo(0.40), $"switched on at SoC {sim.State.BatterySoc:F3}");
                }
                Assert.That(sim.State.BatterySoc, Is.InRange(0, 1));
                wasOn = on;
            }

            Assert.That(offs, Is.GreaterThanOrEqualTo(2), "camera should be shed several times a day");
            Assert.That(ons, Is.GreaterThanOrEqualTo(2), "camera should come back after recharging");
        }

        [Test]
        public void LoadShedding_NoSolarCells_SafeModeThenShortage()
        {
            MissionDesign design = Design("low_power.json");
            design.SolarCellId = null;
            MissionSimulation sim = Start(design);
            bool sawInstrumentsOffWithoutSafeMode = false, sawSafeMode = false;

            for (int i = 0; i < TicksPerDay; i++)
            {
                double before = sim.State.BatteryWh;
                bool safeBefore = sim.State.SafeMode;
                sim.Step(Tick);

                if (!sim.State.InstrumentsOn && !sim.State.SafeMode) sawInstrumentsOffWithoutSafeMode = true;
                if (sim.State.SafeMode) sawSafeMode = true;

                // In safe mode the bus draws half: 2.5 W (plus 8 W for the transmitter during contact)
                if (safeBefore && !sim.State.InContact && before > 0.1)
                    Assert.That(before - sim.State.BatteryWh, Is.EqualTo(2.5 * Tick / 3600).Within(1e-9));
            }

            Assert.That(sawInstrumentsOffWithoutSafeMode, Is.True, "instruments go off at 20 % before safe mode at 5 %");
            Assert.That(sawSafeMode, Is.True);
            Assert.That(sim.State.BatteryWh, Is.EqualTo(0));
            Assert.That(sim.Log.Total(LogQuantity.EnergyWh, LogCause.EnergyShortage), Is.GreaterThan(0));

            // Shortage entries are episodes of whole ticks in mission time
            foreach (SimLogEntry e in sim.Log.Entries)
            {
                Assert.That(e.StartS % Tick, Is.EqualTo(0));
                Assert.That(e.EndS, Is.GreaterThan(e.StartS));
                Assert.That(e.EndS, Is.LessThanOrEqualTo(Day));
            }
        }

        [Test]
        public void Energy_ShortageAmount_IsTheMissingEnergy()
        {
            MissionDesign design = Design("low_power.json");
            design.SolarCellId = null;
            MissionSimulation sim = Start(design);
            sim.Command(true);                 // bus at 2.5 W, camera off
            sim.State.BatteryWh = 0.1;

            sim.Step(3 * Tick);

            // 0.1 Wh − 3 × 2.5 W × 60 s / 3600 = −0.025 Wh, if no contact during these three ticks
            if (!sim.State.InContact)
            {
                Assert.That(sim.State.BatteryWh, Is.EqualTo(0));
                Assert.That(sim.Log.Total(LogQuantity.EnergyWh, LogCause.EnergyShortage), Is.EqualTo(0.025).Within(1e-9));
                // Since step 4 the log also holds data losses; look only at the energy episode
                SimLogEntry shortage = sim.Log.Entries.Single(e => e.Quantity == LogQuantity.EnergyWh);
                Assert.That(shortage.StartS, Is.EqualTo(120));
                Assert.That(shortage.EndS, Is.EqualTo(180));
            }
        }

        [Test]
        public void Command_SafeModeOn_StaysOnWithFullBattery()
        {
            MissionSimulation sim = StartGood();

            sim.Command(true);
            Assert.That(sim.State.SafeMode, Is.True);
            Assert.That(sim.State.InstrumentsOn, Is.False);

            sim.Step(Day);
            Assert.That(sim.State.BatterySoc, Is.GreaterThan(0.99));
            Assert.That(sim.State.SafeMode, Is.True, "a commanded safe mode is only left by command");
            Assert.That(sim.State.InstrumentsOn, Is.False);

            sim.Command(false);
            Assert.That(sim.State.SafeMode, Is.False);
            Assert.That(sim.State.InstrumentsOn, Is.True);
        }

        [Test]
        public void Command_Off_DoesNotEndAutomaticSafeMode()
        {
            MissionDesign design = Design("low_power.json");
            design.SolarCellId = null;
            MissionSimulation sim = Start(design);

            int guard = 0;
            while (!sim.State.SafeMode && guard++ < TicksPerDay) sim.Step(Tick);
            Assert.That(sim.State.SafeMode, Is.True, "battery should fall below 5 % within a day");

            sim.Command(false);
            Assert.That(sim.State.SafeMode, Is.True);
            Assert.That(sim.State.InstrumentsOn, Is.False);
        }

        [Test]
        public void Energy_DifferentDtSequences_GiveSameBattery()
        {
            MissionSimulation once = Start(Design("low_power.json"));
            once.Step(Day);

            MissionSimulation chunks = Start(Design("low_power.json"));
            var rng = new Random(3);
            double done = 0;
            while (done < Day)
            {
                double dt = Math.Min(rng.Next(1, 500), Day - done);
                chunks.Step(dt);
                done += dt;
            }

            Assert.That(chunks.State.BatteryWh, Is.EqualTo(once.State.BatteryWh).Within(1e-9));
            Assert.That(chunks.State.InstrumentsOn, Is.EqualTo(once.State.InstrumentsOn));
            Assert.That(chunks.Log.Entries.Count, Is.EqualTo(once.Log.Entries.Count));
        }
    }
}
