using System.IO;
using NUnit.Framework;
using MissionCore.Catalogs;

namespace MissionCore.Tests
{
    // P6 step 8: one loader for Unity, simrun and the tests
    public class CatalogLoadAllTests
    {
        static string ReadReal(string file)
        {
            string path = Path.Combine(TestHelpers.StreamingDataDir(), file);
            return File.Exists(path) ? File.ReadAllText(path) : null;
        }

        [Test]
        public void LoadAll_RealFiles_SameAsHandBuiltCatalog()
        {
            Catalog loaded = CatalogLoader.LoadAll(ReadReal);
            Catalog manual = MissionSimulationStartTests.RealCatalog();

            Assert.That(loaded.Platforms.Count, Is.EqualTo(manual.Platforms.Count));
            Assert.That(loaded.Instruments.Count, Is.EqualTo(manual.Instruments.Count));
            Assert.That(loaded.Comms.Count, Is.EqualTo(manual.Comms.Count));
            Assert.That(loaded.Batteries.Count, Is.EqualTo(manual.Batteries.Count));
            Assert.That(loaded.SolarCells.Count, Is.EqualTo(manual.SolarCells.Count));
            Assert.That(loaded.GroundStations.Count, Is.EqualTo(manual.GroundStations.Count));
            Assert.That(loaded.Launchers.Count, Is.EqualTo(manual.Launchers.Count));
            Assert.That(loaded.Propulsion.Count, Is.EqualTo(manual.Propulsion.Count));
            Assert.That(loaded.Missions.Count, Is.EqualTo(manual.Missions.Count));
            Assert.That(loaded.OrbitPresets.Count, Is.GreaterThan(0));
            Assert.That(loaded.Sim, Is.Not.Null);
            Assert.That(loaded.Texts, Is.Not.Null);
            Assert.That(loaded.Balance, Is.Not.Null);
        }

        [Test]
        public void LoadAll_WithoutSimRules_SimIsNull()
        {
            Catalog catalog = CatalogLoader.LoadAll(f => f == "Rules/sim_rules.json" ? null : ReadReal(f));

            Assert.That(catalog.Sim, Is.Null);
            Assert.That(catalog.Platforms.Count, Is.GreaterThan(0));
        }

        [Test]
        public void LoadAll_WithoutPlatforms_ThrowsWithFileName()
        {
            var ex = Assert.Throws<FileNotFoundException>(() => CatalogLoader.LoadAll(f => f == "platforms.json" ? null : ReadReal(f)));
            Assert.That(ex.Message, Does.Contain("platforms.json"));
        }
    }
}
