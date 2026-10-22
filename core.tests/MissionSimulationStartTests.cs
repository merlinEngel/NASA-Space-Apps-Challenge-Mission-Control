using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using MissionCore.Budgets;
using MissionCore.Catalogs;
using MissionCore.Design;
using MissionCore.Earth;
using MissionCore.Mission;
using MissionCore.Rules;
using MissionCore.Simulation;

namespace MissionCore.Tests
{
    // P6 step 1: SimParameters.From and MissionSimulation.Start build the initial state
    public class MissionSimulationStartTests
    {
        internal static Catalog RealCatalog()
        {
            string data = TestHelpers.StreamingDataDir();
            List<T> L<T>(string f) => CatalogLoader.LoadList<T>(File.ReadAllText(Path.Combine(data, f)), f);
            SimRules sim = CatalogLoader.LoadObject<SimRules>(File.ReadAllText(TestHelpers.RulesFile("sim_rules.json")), "sim_rules.json");
            return new Catalog(L<PropulsionSpec>("propulsion.json"), L<InstrumentSpec>("instruments.json"),
                L<LauncherSpec>("launchers.json"), L<CommsSpec>("comms.json"), L<BatterySpec>("batteries.json"),
                L<PlatformSpec>("platforms.json"), L<GroundStationSpec>("groundstations.json"), L<SolarCellSpec>("solar.json"),
                L<MissionTemplate>("missions.json"),
                TestHelpers.LoadRealBalanceRules(),
                null, sim, null, new List<OrbitPresetSpec>());
        }

        // core.tests/Data/designs, found next to Assets
        internal static MissionDesign TechDemo()
        {
            string root = Directory.GetParent(TestHelpers.StreamingDataDir()).Parent.Parent.FullName;
            string file = Path.Combine(root, "core.tests", "Data", "designs", "techdemo_good.json");
            return CatalogLoader.LoadObject<MissionDesign>(File.ReadAllText(file), "techdemo_good.json");
        }

        [Test]
        public void Start_TechDemo_BeginsFullAndEmpty()
        {
            MissionSimulation sim = MissionSimulation.Start(RealCatalog(), TechDemo(), 42);
            SimState s = sim.State;

            Assert.That(s.BatteryCapacityWh, Is.EqualTo(160).Within(1e-9));    // 2 × 80 Wh, full capacity, no depth of discharge
            Assert.That(s.BatteryWh, Is.EqualTo(160).Within(1e-9));
            Assert.That(s.BatterySoc, Is.EqualTo(1).Within(1e-12));
            Assert.That(s.StorageBits, Is.EqualTo(0));
            Assert.That(s.StorageCapacityBits, Is.EqualTo(128e9));
            Assert.That(s.DeliveredBits, Is.EqualTo(0));
            Assert.That(s.LostBits, Is.EqualTo(0));
            Assert.That(s.AltitudeM, Is.EqualTo(500e3));
            Assert.That(s.MissionTimeS, Is.EqualTo(0));
            Assert.That(s.InstrumentsOn, Is.True);
            Assert.That(s.SafeMode, Is.False);
            Assert.That(s.EndReason, Is.EqualTo(EndReason.None));
            Assert.That(s.Ended, Is.False);
            Assert.That(sim.Seed, Is.EqualTo(42));
            Assert.That(sim.Log.Entries, Is.Empty);
        }

        [Test]
        public void Start_PositionLiesOnTheOrbit()
        {
            MissionSimulation sim = MissionSimulation.Start(RealCatalog(), TechDemo(), 42);

            Assert.That(sim.State.PositionM.Length, Is.EqualTo(TestHelpers.REarth + 500e3).Within(1));
        }

        [Test]
        public void Start_SameSeedTwice_SameInitialState()
        {
            MissionSimulation a = MissionSimulation.Start(RealCatalog(), TechDemo(), 7);
            MissionSimulation b = MissionSimulation.Start(RealCatalog(), TechDemo(), 7);

            TestHelpers.AssertVec(a.State.PositionM, b.State.PositionM, 1e-9);
            Assert.That(b.State.BatteryWh, Is.EqualTo(a.State.BatteryWh));
        }

        [Test]
        public void Start_ClockBeginsAtMissionEpoch()
        {
            MissionSimulation sim = MissionSimulation.Start(RealCatalog(), TechDemo(), 42);
            MissionTemplate mission = RealCatalog().Missions["techdemo"];

            Assert.That(sim.Clock.StepSeconds, Is.EqualTo(60));
            Assert.That(sim.Clock.StepIndex, Is.EqualTo(0));
            Assert.That(sim.Clock.SimTime, Is.EqualTo(mission.EpochSeconds));
            Assert.That(sim.Parameters.EpochS, Is.EqualTo(mission.EpochSeconds));
            Assert.That(sim.Parameters.DurationS, Is.EqualTo(90 * 86400));
        }

        [Test]
        public void Parameters_TechDemo_MatchHandCalculation()
        {
            SimParameters p = SimParameters.From(RealCatalog(), TechDemo());

            // 1361 W/m² × 0.2 m² × 0.85 packing × 0.30 efficiency = 69.41 W (no ageing at start)
            Assert.That(p.SolarPowerW, Is.EqualTo(69.411).Within(0.01));
            Assert.That(p.SolarDegradationPerYear, Is.EqualTo(0.01));
            Assert.That(p.BusPowerW, Is.EqualTo(8));
            Assert.That(p.InstrumentPowerW, Is.EqualTo(6));
            Assert.That(p.CommsPowerW, Is.EqualTo(8));
            // camera_wide: 2 Mbit/s × 2 % duty cycle
            Assert.That(p.InstrumentDataBps, Is.EqualTo(40000).Within(1e-6));
            Assert.That(p.DownlinkBps, Is.EqualTo(1e6));
            Assert.That(p.Stations.Select(s => s.id), Is.EquivalentTo(new[] { "svalbard", "weilheim" }));
            Assert.That(p.BatteryCapacityWh, Is.EqualTo(160).Within(1e-9));
            Assert.That(p.StorageCapacityBits, Is.EqualTo(128e9));
            Assert.That(p.TickS, Is.EqualTo(60));
            Assert.That(p.DensityFactor, Is.EqualTo(1.0));       // techdemo: medium solar activity
            Assert.That(p.RaanRad, Is.EqualTo(0));
            Assert.That(p.LoadShedding.InstrumentsOffBelowSoc, Is.EqualTo(0.20));
        }

        [Test]
        public void Parameters_SafeModeBusPower_IsHalfOfBusPower()
        {
            SimParameters p = SimParameters.From(RealCatalog(), TechDemo());

            // safe_mode_bus_power_factor = 0.5 in sim_rules.json (estimate)
            Assert.That(p.SafeModeBusPowerW, Is.EqualTo(4).Within(1e-9));
        }

        [Test]
        public void Parameters_BallisticCoefficient_MatchesLifetimeBudget()
        {
            Catalog catalog = RealCatalog();
            MissionDesign design = TechDemo();
            SimParameters p = SimParameters.From(catalog, design);

            DesignReport report = DesignValidator.Evaluate(catalog, design);
            double massKg = report.MassResult.DryMassKg + report.MassResult.PropellantMassKg;
            double areaM2 = catalog.Platforms[design.PlatformId].dragAreaM2 + design.SolarAreaM2 * 0.5;
            double expected = OrbitDecay.DragCoefficient * areaM2 / massKg;

            Assert.That(p.BallisticCoefficient, Is.EqualTo(expected).Within(1e-12));
            Assert.That(LifetimeBudget.DragAreaM2(catalog.Platforms[design.PlatformId], design), Is.EqualTo(areaM2).Within(1e-12));
            // Same lifetime as the budget when fed into OrbitDecay
            double lifetimeFromSim = OrbitDecay.LifetimeS(design.AltitudeM, massKg, areaM2, p.DensityFactor);
            Assert.That(report.LifetimeResult.LifetimeS, Is.EqualTo(lifetimeFromSim).Within(1));
        }

        [Test]
        public void Start_WithoutMission_ThrowsArgumentException()
        {
            MissionDesign design = TechDemo();
            design.MissionId = null;

            var ex = Assert.Throws<ArgumentException>(() => MissionSimulation.Start(RealCatalog(), design, 42));
            Assert.That(ex.Message, Does.Contain("ission"));
        }

        [Test]
        public void Start_UnknownMission_ThrowsArgumentException()
        {
            MissionDesign design = TechDemo();
            design.MissionId = "does_not_exist";

            Assert.Throws<ArgumentException>(() => MissionSimulation.Start(RealCatalog(), design, 42));
        }

        [Test]
        public void Start_WithoutBattery_ThrowsArgumentException()
        {
            MissionDesign design = TechDemo();
            design.BatteryId = null;

            var ex = Assert.Throws<ArgumentException>(() => MissionSimulation.Start(RealCatalog(), design, 42));
            Assert.That(ex.Message, Does.Contain("attery"));
        }

        [Test]
        public void Start_WithoutPlatform_ThrowsArgumentException()
        {
            MissionDesign design = TechDemo();
            design.PlatformId = null;

            Assert.Throws<ArgumentException>(() => MissionSimulation.Start(RealCatalog(), design, 42));
        }

        [Test]
        public void Start_WithoutComms_RunsWithoutDownlink()
        {
            MissionDesign design = TechDemo();
            design.CommsId = null;

            MissionSimulation sim = null;
            Assert.DoesNotThrow(() => sim = MissionSimulation.Start(RealCatalog(), design, 42));
            Assert.That(sim.Parameters.DownlinkBps, Is.EqualTo(0));
            Assert.That(sim.Parameters.CommsPowerW, Is.EqualTo(0));
            Assert.That(sim.Parameters.Stations, Is.Empty);
        }

        [Test]
        public void Start_WithoutSolarCells_RunsWithoutSolarPower()
        {
            MissionDesign design = TechDemo();
            design.SolarCellId = null;

            MissionSimulation sim = null;
            Assert.DoesNotThrow(() => sim = MissionSimulation.Start(RealCatalog(), design, 42));
            Assert.That(sim.Parameters.SolarPowerW, Is.EqualTo(0));
        }

        [Test]
        public void Start_WithoutInstruments_ProducesNoData()
        {
            MissionDesign design = TechDemo();
            design.InstrumentIds = new List<string>();

            SimParameters p = SimParameters.From(RealCatalog(), design);
            Assert.That(p.InstrumentDataBps, Is.EqualTo(0));
            Assert.That(p.InstrumentPowerW, Is.EqualTo(0));
        }

        [Test]
        public void Start_WithoutSimRules_ThrowsArgumentException()
        {
            Catalog full = RealCatalog();
            string data = TestHelpers.StreamingDataDir();
            List<T> L<T>(string f) => CatalogLoader.LoadList<T>(File.ReadAllText(Path.Combine(data, f)), f);
            var noSim = new Catalog(L<PropulsionSpec>("propulsion.json"), L<InstrumentSpec>("instruments.json"),
                L<LauncherSpec>("launchers.json"), L<CommsSpec>("comms.json"), L<BatterySpec>("batteries.json"),
                L<PlatformSpec>("platforms.json"), L<GroundStationSpec>("groundstations.json"), L<SolarCellSpec>("solar.json"),
                L<MissionTemplate>("missions.json"), full.Balance, null, null, null, new List<OrbitPresetSpec>());

            Assert.Throws<ArgumentException>(() => MissionSimulation.Start(noSim, TechDemo(), 42));
        }

        [Test]
        public void RealSimRules_HaveSafeModeFactor()
        {
            SimRules sim = CatalogLoader.LoadObject<SimRules>(File.ReadAllText(TestHelpers.RulesFile("sim_rules.json")), "sim_rules.json");

            Assert.That(sim.SafeModeBusPowerFactor, Is.GreaterThan(0).And.LessThan(1));
        }
    }
}
