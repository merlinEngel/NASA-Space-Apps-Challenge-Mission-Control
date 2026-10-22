using System;
using System.IO;
using NUnit.Framework;
using MissionCore.Catalogs;
using MissionCore.Design;
using MissionCore.Mission;
using MissionCore.Simulation;

namespace MissionCore.Tests
{
    // P6 step 5: orbit decay per tick and the three ways a mission ends
    public class MissionSimulationEndTests
    {
        const double Day = 86400;
        const double Tick = 60;
        const double BigStep = 2000 * Tick;   // MaxStepsPerAdvance of SimClock

        static MissionDesign Design(string file)
        {
            string root = Directory.GetParent(TestHelpers.StreamingDataDir()).Parent.Parent.FullName;
            string path = Path.Combine(root, "core.tests", "Data", "designs", file);
            return CatalogLoader.LoadObject<MissionDesign>(File.ReadAllText(path), file);
        }

        static MissionSimulation Start(MissionDesign design, Action<Catalog> changeCatalog = null)
        {
            Catalog catalog = MissionSimulationStartTests.RealCatalog();
            changeCatalog?.Invoke(catalog);
            return MissionSimulation.Start(catalog, design, 42);
        }

        static MissionDesign GoodAt(double altitudeM)
        {
            MissionDesign d = Design("techdemo_good.json");
            d.AltitudeM = altitudeM;
            return d;
        }

        static void RunToEnd(MissionSimulation sim)
        {
            int guard = 0;
            while (!sim.State.Ended && guard++ < 1000) sim.Step(BigStep);
            Assert.That(sim.State.Ended, Is.True);
        }

        [Test]
        public void Decay_OneTickAt500km_LosesSixCentimetres()
        {
            MissionSimulation sim = Start(GoodAt(500e3));

            // B = 2.2 × 0.202 m² / 15.3 kg = 0.02905 m²/kg; rho(500 km) = 6.967e-13 kg/m³ (Vallado 8-4)
            // da/dt = −rho · sqrt(mu · a) · B = −1.0596 mm/s → −6.36 cm per tick
            Assert.That(sim.Parameters.BallisticCoefficient, Is.EqualTo(0.02905).Within(1e-4));
            double a = TestHelpers.REarth + 500e3;
            double expected = -6.967e-13 * Math.Sqrt(TestHelpers.MuEarth * a) * sim.Parameters.BallisticCoefficient * Tick;

            sim.Step(Tick);
            Assert.That(sim.State.AltitudeM - 500e3, Is.EqualTo(expected).Within(Math.Abs(expected) * 0.01));
            Assert.That(sim.State.AltitudeM - 500e3, Is.EqualTo(-0.0636).Within(0.001));
        }

        [Test]
        public void Duration_GoodDesign_EndsAfterNinetyDaysAt491km()
        {
            MissionSimulation sim = Start(GoodAt(500e3));
            RunToEnd(sim);

            Assert.That(sim.State.EndReason, Is.EqualTo(EndReason.DurationReached));
            Assert.That(sim.State.MissionTimeS, Is.EqualTo(90 * Day));
            // 90 days of decay with the density rising as the orbit sinks: 500 → 491.1 km
            Assert.That(sim.State.AltitudeM, Is.EqualTo(491.15e3).Within(500));
        }

        [Test]
        public void Step_AfterEnd_DoesNothing()
        {
            MissionSimulation sim = Start(GoodAt(500e3));
            RunToEnd(sim);
            double altitude = sim.State.AltitudeM;
            double delivered = sim.State.DeliveredBits;

            Assert.That(sim.Step(Day), Is.EqualTo(0));
            Assert.That(sim.State.MissionTimeS, Is.EqualTo(90 * Day));
            Assert.That(sim.State.AltitudeM, Is.EqualTo(altitude));
            Assert.That(sim.State.DeliveredBits, Is.EqualTo(delivered));
        }

        [Test]
        public void Duration_At400km_SinksTo309kmWithoutReentry()
        {
            MissionSimulation sim = Start(GoodAt(400e3));
            RunToEnd(sim);

            Assert.That(sim.State.EndReason, Is.EqualTo(EndReason.DurationReached));
            Assert.That(sim.State.AltitudeM, Is.EqualTo(309.2e3).Within(2e3));
        }

        [Test]
        public void Reentry_At300km_MatchesLifetimeBudget()
        {
            Catalog catalog = MissionSimulationStartTests.RealCatalog();
            MissionDesign design = GoodAt(300e3);
            double budgetDays = DesignValidator.Evaluate(catalog, design).LifetimeResult.LifetimeS / Day;

            MissionSimulation sim = MissionSimulation.Start(catalog, design, 42);
            RunToEnd(sim);

            Assert.That(sim.State.EndReason, Is.EqualTo(EndReason.Reentry));
            Assert.That(sim.State.AltitudeM, Is.LessThan(150e3));
            Assert.That(sim.State.MissionTimeS / Day, Is.EqualTo(13.3).Within(0.1));
            Assert.That(sim.State.MissionTimeS / Day, Is.EqualTo(budgetDays).Within(0.1));
        }

        [Test]
        public void Decay_HighSolarActivity_ThreeTimesFaster()
        {
            MissionSimulation medium = Start(GoodAt(500e3));
            MissionSimulation high = Start(GoodAt(500e3), c => c.Missions["techdemo"].solarActivity = SolarActivity.High);

            medium.Step(Day);
            high.Step(Day);

            double lossMedium = 500e3 - medium.State.AltitudeM;
            double lossHigh = 500e3 - high.State.AltitudeM;
            Assert.That(lossHigh / lossMedium, Is.EqualTo(3).Within(0.05));   // density factor 3 in sim_rules.json
        }

        [Test]
        public void BatteryDepleted_NoSolarCells_EndsAfterSixHoursEmpty()
        {
            MissionDesign design = Design("low_power.json");
            design.SolarCellId = null;
            MissionSimulation sim = Start(design);
            Assert.That(sim.Parameters.MaxBatteryEmptyS, Is.EqualTo(21600));

            double emptySince = -1;
            int guard = 0;
            while (!sim.State.Ended && guard++ < 5 * 1440)
            {
                double start = sim.State.MissionTimeS;
                sim.Step(Tick);
                if (sim.State.BatteryWh <= 0) { if (emptySince < 0) emptySince = start; }
                else emptySince = -1;
            }

            Assert.That(sim.State.EndReason, Is.EqualTo(EndReason.BatteryDepleted));
            // Ends in the first tick after more than 6 h without energy: 361 empty ticks
            Assert.That(sim.State.MissionTimeS - emptySince, Is.EqualTo(21600 + Tick));
        }

        [Test]
        public void BatteryDepleted_EmptyCounterRestartsAfterRecharge()
        {
            MissionDesign design = Design("low_power.json");
            design.SolarCellId = null;
            MissionSimulation sim = Start(design);

            while (sim.State.BatteryWh > 0) sim.Step(Tick);
            sim.Step(5 * 3600);                      // 5 h empty: not yet the end
            Assert.That(sim.State.Ended, Is.False);

            sim.State.BatteryWh = 5;                 // as if the panels had recharged it a little
            double rechargedAt = sim.State.MissionTimeS;
            sim.Step(Tick);
            Assert.That(sim.State.Ended, Is.False, "one tick with energy resets the counter");

            RunToEnd(sim);
            Assert.That(sim.State.EndReason, Is.EqualTo(EndReason.BatteryDepleted));
            Assert.That(sim.State.MissionTimeS - rechargedAt, Is.GreaterThan(21600));
        }

        [Test]
        public void Step_ReturnsTicksActuallyRunUntilTheEnd()
        {
            MissionSimulation sim = Start(GoodAt(300e3));
            for (int i = 0; i < 26; i++) sim.Step(Day / 2);   // 13 days, reentry comes at ~13.3
            Assert.That(sim.State.Ended, Is.False);

            double before = sim.State.MissionTimeS;
            int ticks = sim.Step(BigStep);

            Assert.That(sim.State.EndReason, Is.EqualTo(EndReason.Reentry));
            Assert.That(ticks, Is.LessThan(2000));
            Assert.That(ticks, Is.EqualTo((int)((sim.State.MissionTimeS - before) / Tick)));
        }
    }
}
