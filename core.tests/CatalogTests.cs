using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;

namespace MissionCore.Tests
{
    public class CatalogTests
    {
        static Catalog SmallCatalog() => new Catalog(
            new List<PropulsionSpec>
            {
                new PropulsionSpec { id = "cold_gas", ispS = 70 },
                new PropulsionSpec { id = "ion", ispS = 3100, powerW = 2300 },
            },
            new List<InstrumentSpec> { new InstrumentSpec { id = "mag", massKg = 3 } },
            new List<LauncherSpec> { new LauncherSpec { id = "electron", leoKg = 300 } },
            new List<CommsSpec>(), new List<BatterySpec>(), new List<PlatformSpec>(),
            new List<GroundStationSpec>(), new List<SolarCellSpec>(), new List<MissionTemplate>(),
            new BalanceRules { MassMarginEarlyPhase = 0.25 }, null, null, null, new List<OrbitPresetSpec>());

        [Test]
        public void LookupById_ReturnsMatchingEntry()
        {
            var c = SmallCatalog();
            Assert.That(c.Propulsion["ion"].ispS, Is.EqualTo(3100));
            Assert.That(c.Instruments["mag"].massKg, Is.EqualTo(3));
            Assert.That(c.Launchers["electron"].leoKg, Is.EqualTo(300));
            Assert.That(c.Balance.MassMarginEarlyPhase, Is.EqualTo(0.25));
        }

        [Test]
        public void LookupById_UnknownId_IsNotFound()
        {
            var c = SmallCatalog();
            Assert.That(c.Propulsion.ContainsKey("warp_drive"), Is.False);
            Assert.That(c.Propulsion.TryGetValue("warp_drive", out _), Is.False);
        }

        [Test]
        public void DuplicateIdInList_Throws()
        {
            var dup = new List<PropulsionSpec>
            {
                new PropulsionSpec { id = "a", ispS = 70 },
                new PropulsionSpec { id = "a", ispS = 300 },
            };
            var ex = Assert.Throws<FormatException>(() => new Catalog(
                dup, new List<InstrumentSpec>(), new List<LauncherSpec>(), new List<CommsSpec>(),
                new List<BatterySpec>(), new List<PlatformSpec>(), new List<GroundStationSpec>(),
                new List<SolarCellSpec>(), new List<MissionTemplate>(), new BalanceRules(), null, null, null, new List<OrbitPresetSpec>()));
            Assert.That(ex.Message, Does.Contain("propulsion.json").And.Contain("'a'"));
        }

        // ---------- Integration: the real files in Assets/StreamingAssets/Data ----------

        static string Read(string file) => File.ReadAllText(Path.Combine(TestHelpers.StreamingDataDir(), file));

        static string ReadRules(string file) => File.ReadAllText(TestHelpers.RulesFile(file));

        static List<T> List<T>(string file) => CatalogLoader.LoadList<T>(Read(file), file);

        static Catalog LoadRealCatalog() => new Catalog(
            List<PropulsionSpec>("propulsion.json"), List<InstrumentSpec>("instruments.json"),
            List<LauncherSpec>("launchers.json"), List<CommsSpec>("comms.json"),
            List<BatterySpec>("batteries.json"), List<PlatformSpec>("platforms.json"),
            List<GroundStationSpec>("groundstations.json"), List<SolarCellSpec>("solar.json"),
            List<MissionTemplate>("missions.json"),
            CatalogLoader.LoadObject<BalanceRules>(ReadRules("balance_rules.json"), "balance_rules.json"),
            null, null, null, new List<OrbitPresetSpec>());

        [Test]
        public void RealFiles_AllLoad()
        {
            var c = LoadRealCatalog();
            Assert.That(c.Propulsion, Is.Not.Empty);
            Assert.That(c.Instruments, Is.Not.Empty);
            Assert.That(c.Launchers, Is.Not.Empty);
            Assert.That(c.Balance, Is.Not.Null);
            Assert.That(c.Balance.DryMassFractions, Is.Not.Null);
        }

        [Test]
        public void RealFiles_AllPassValidate()
        {
            Assert.DoesNotThrow(() =>
            {
                CatalogValidator.Validate(List<PropulsionSpec>("propulsion.json"), "propulsion.json");
                CatalogValidator.Validate(List<InstrumentSpec>("instruments.json"), "instruments.json");
                CatalogValidator.Validate(List<LauncherSpec>("launchers.json"), "launchers.json");
                CatalogValidator.Validate(List<CommsSpec>("comms.json"), "comms.json");
                CatalogValidator.Validate(List<BatterySpec>("batteries.json"), "batteries.json");
                CatalogValidator.Validate(List<PlatformSpec>("platforms.json"), "platforms.json");
                CatalogValidator.Validate(List<GroundStationSpec>("groundstations.json"), "groundstations.json");
                CatalogValidator.Validate(List<SolarCellSpec>("solar.json"), "solar.json");
                CatalogValidator.Validate(List<MissionTemplate>("missions.json"), "missions.json");
                CatalogValidator.Validate(CatalogLoader.LoadObject<BalanceRules>(ReadRules("balance_rules.json"), "balance_rules.json"), "balance_rules.json");
                CatalogValidator.Validate(CatalogLoader.LoadObject<ScoringRules>(ReadRules("scoring.json"), "scoring.json"), "scoring.json");
                CatalogValidator.Validate(CatalogLoader.LoadObject<SimRules>(ReadRules("sim_rules.json"), "sim_rules.json"), "sim_rules.json");
            });
        }

        [TestCase("instruments.json")]
        [TestCase("launchers.json")]
        public void RealFiles_IdsPresentAndUnique(string file)
        {
            IEnumerable<string> ids = file == "instruments.json"
                ? CatalogLoader.LoadList<InstrumentSpec>(Read(file), file).Select(i => i.id)
                : CatalogLoader.LoadList<LauncherSpec>(Read(file), file).Select(l => l.id);
            var list = ids.ToList();
            Assert.That(list, Has.None.Null.And.None.Empty);
            Assert.That(list, Is.Unique);
        }

        [Test]
        public void RealFiles_EveryEntryHasNameAndSource()
        {
            var c = LoadRealCatalog();
            var entries = c.Propulsion.Values.Select(p => (p.id, p.name, p.source))
                .Concat(c.Instruments.Values.Select(i => (i.id, i.name, i.source)))
                .Concat(c.Launchers.Values.Select(l => (l.id, l.name, l.source)));
            foreach (var (id, name, source) in entries)
            {
                Assert.That(name, Is.Not.Null.And.Not.Empty, $"{id}: name");
                Assert.That(source, Is.Not.Null.And.Not.Empty, $"{id}: source");
            }
        }

        [Test]
        public void RealFiles_DedicatedLaunchersHavePositiveLeoCapacityAndValidSuccessRate()
        {
            foreach (var l in LoadRealCatalog().Launchers.Values.Where(l => !l.IsRideshare))
            {
                Assert.That(l.leoKg, Is.GreaterThan(0), l.id);
                Assert.That(l.priceUSD, Is.GreaterThan(0), l.id);
                Assert.That(l.successRate, Is.InRange(0.0, 1.0), l.id);
            }
        }

        [Test]
        public void RealFiles_LaunchersContainARideshareWithPositiveLaunchCost()
        {
            var rideshares = LoadRealCatalog().Launchers.Values.Where(l => l.IsRideshare).ToList();
            Assert.That(rideshares, Is.Not.Empty);
            foreach (var l in rideshares)
            {
                Assert.That(l.GetLaunchCost(0), Is.GreaterThan(0), l.id);
                Assert.That(l.orbits, Is.Not.Empty, l.id);
            }
        }

        [Test]
        public void RealFiles_InstrumentsHavePositivePrice()
        {
            foreach (var i in LoadRealCatalog().Instruments.Values)
                Assert.That(i.priceUSD, Is.GreaterThan(0), i.id);
        }

        // Rough guard: the tech demo must stay affordable with a camera on a CubeSat via rideshare.
        [Test]
        public void RealFiles_TechDemoAffordableWithWideCameraOnRideshare()
        {
            var c = LoadRealCatalog();
            double hardwareUSD = c.Platforms["cubesat_6u"].priceUSD + c.Instruments["camera_wide"].priceUSD;
            double launchUSD = c.Launchers["spacex_transporter"].GetLaunchCost(c.Platforms["cubesat_6u"].maxMassKg);
            Assert.That(hardwareUSD + launchUSD, Is.LessThan(c.Missions["techdemo"].budgetUSD));
        }

        [Test]
        public void RealFiles_MassFractionsSumToOne()
        {
            var f = LoadRealCatalog().Balance.DryMassFractions;
            double sum = f.Payload + f.Structure + f.Thermal + f.Power + f.Communications
                       + f.AttitudeControl + f.Computer + f.Propulsion + f.Other;
            Assert.That(sum, Is.EqualTo(1.0).Within(1e-9));
        }
    }
}
