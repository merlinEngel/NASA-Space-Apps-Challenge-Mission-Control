using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using MissionCore;

namespace MissionCore.Tests
{
    public class DataBudgetTests
    {
        const double Tol = 1e-6;

        static Catalog TestCatalog() => new Catalog(
            new List<PropulsionSpec>(),
            new List<InstrumentSpec>
            {
                new InstrumentSpec { id = "mag", massKg = 3, dataRateBpS = 1000, dutyCycle = 0.5 },
                new InstrumentSpec { id = "camera", massKg = 3, dataRateBpS = 2000000, dutyCycle = 0.02 },
            },
            new List<LauncherSpec>(),
            new List<CommsSpec>
            {
                new CommsSpec { id = "s_band", band = "S", dataRateBpS = 1000000 },
                new CommsSpec { id = "uhf", band = "UHF", dataRateBpS = 9600 },
            },
            new List<BatterySpec>(),
            new List<PlatformSpec>
            {
                new PlatformSpec { id = "big", busMassKg = 5, maxMassKg = 12, storageBits = 64e9 },
                new PlatformSpec { id = "tiny", busMassKg = 1, maxMassKg = 2, storageBits = 1e6 },
            },
            new List<GroundStationSpec>
            {
                new GroundStationSpec { id = "equator", latDeg = 0, lonDeg = 0, minElevationDeg = 5, bands = new[] { "S", "X" } },
                new GroundStationSpec { id = "equator_uhf", latDeg = 0, lonDeg = 0, minElevationDeg = 5, bands = new[] { "UHF" } },
            },
            new List<SolarCellSpec>(),
            new List<MissionTemplate>(),
            new BalanceRules(), null, null, null);

        static MissionDesign Design() => new MissionDesign
        {
            PlatformId = "big",
            InstrumentIds = new List<string> { "mag", "camera" },
            CommsId = "s_band",
            GroundStationIds = new List<string> { "equator" },
            AltitudeM = 500000,
            InclinationDeg = 0
        };

        // mag:    1,000 bps x 0.5  x 86,400 s =    43,200,000 bit/day
        // camera: 2,000,000 bps x 0.02 x 86,400 s = 3,456,000,000 bit/day
        // sum                                     = 3,499,200,000 bit/day
        [Test]
        public void GeneratedBitsPerDay_IsRateTimesDutyCycleTimesDay()
        {
            Assert.That(DataBudget.GetGeneratedBitsPerDay(TestCatalog(), Design()), Is.EqualTo(3499200000).Within(Tol));
        }

        [Test]
        public void DownlinkBitsPerDay_IsRateTimesContactTime()
        {
            var r = new DataBudgetResult(5e8, 4, 1000, 3600, 1e6, 0, 1);
            Assert.That(r.DownlinkBitsPerDay, Is.EqualTo(1e9));
            Assert.That(r.ReserveBitsPerDay, Is.EqualTo(5e8));
            Assert.That(r.ReserveFraction, Is.EqualTo(1.0));
        }

        [Test]
        public void Evaluate_UsesCommsRateAndContactsOfCompatibleStation()
        {
            var r = DataBudget.Evaluate(TestCatalog(), Design());
            Assert.That(r.DownlinkBitsPerS, Is.EqualTo(1000000));
            Assert.That(r.ContactsPerDay, Is.GreaterThan(10));
            Assert.That(r.DownlinkBitsPerDay, Is.EqualTo(r.DownlinkBitsPerS * r.ContactTimeSPerDay).Within(Tol));
        }

        [Test]
        public void BandMismatch_GivesNoContact()
        {
            var d = Design();
            d.CommsId = "uhf";   // station "equator" has only S and X
            var r = DataBudget.Evaluate(TestCatalog(), d);
            Assert.That(r.ContactsPerDay, Is.EqualTo(0));
            Assert.That(r.DownlinkBitsPerDay, Is.EqualTo(0));
            Assert.That(r.ReserveBitsPerDay, Is.LessThan(0));
        }

        [Test]
        public void BandMatch_IgnoresCase()
        {
            var stations = DataBudget.GetCompatibleStations(TestCatalog(), Design(), "s");
            Assert.That(stations.Count, Is.EqualTo(1));
        }

        [Test]
        public void StorageNeeded_IsGenerationDuringLongestGap()
        {
            var r = DataBudget.Evaluate(TestCatalog(), Design());
            Assert.That(r.StorageNeededBits, Is.EqualTo(3499200000 / Constants.SecondsPerDay * r.LongestGapS).Within(Tol));
            Assert.That(r.StorageCapacityBits, Is.EqualTo(64e9));
            Assert.That(r.StorageOk, Is.True);
        }

        [Test]
        public void SmallStorage_IsNotOk()
        {
            var d = Design();
            d.PlatformId = "tiny";
            Assert.That(DataBudget.Evaluate(TestCatalog(), d).StorageOk, Is.False);
        }

        [Test]
        public void UnknownIds_AreIgnored()
        {
            var d = Design();
            d.InstrumentIds.Add("warp_scanner");
            d.GroundStationIds.Add("moon_base");
            var r = DataBudget.Evaluate(TestCatalog(), d);
            Assert.That(r.GeneratedBitsPerDay, Is.EqualTo(3499200000).Within(Tol));
        }

        [Test]
        public void EmptyDesign_DoesNotCrash()
        {
            DataBudgetResult r = null;
            Assert.DoesNotThrow(() => r = DataBudget.Evaluate(TestCatalog(), new MissionDesign()));
            Assert.That(r.GeneratedBitsPerDay, Is.EqualTo(0));
            Assert.That(r.ContactsPerDay, Is.EqualTo(0));
            Assert.That(r.DownlinkBitsPerDay, Is.EqualTo(0));
            Assert.That(r.ReserveFraction, Is.EqualTo(0));
            Assert.That(r.StorageNeededBits, Is.EqualTo(0));
        }

        // ---------- Real data ----------

        static Catalog RealCatalog()
        {
            string data = TestHelpers.StreamingDataDir();
            List<T> L<T>(string f) => CatalogLoader.LoadList<T>(File.ReadAllText(Path.Combine(data, f)), f);
            return new Catalog(L<PropulsionSpec>("propulsion.json"), L<InstrumentSpec>("instruments.json"),
                L<LauncherSpec>("launchers.json"), L<CommsSpec>("comms.json"), L<BatterySpec>("batteries.json"),
                L<PlatformSpec>("platforms.json"), L<GroundStationSpec>("groundstations.json"), L<SolarCellSpec>("solar.json"),
                L<MissionTemplate>("missions.json"),
                TestHelpers.LoadRealBalanceRules(),
                null, null, null);
        }

        // Tech demo: 6U, wide camera, S-band, SSO 500 km, Svalbard + Weilheim must close the data budget.
        [Test]
        public void RealFiles_TechDemoDesignClosesDataBudget()
        {
            var d = new MissionDesign
            {
                MissionId = "techdemo", PlatformId = "cubesat_6u", CommsId = "s_band",
                InstrumentIds = new List<string> { "camera_wide" },
                GroundStationIds = new List<string> { "svalbard", "weilheim" },
                AltitudeM = 500000, InclinationDeg = 97.5
            };
            var r = DataBudget.Evaluate(RealCatalog(), d);
            Assert.That(r.ReserveBitsPerDay, Is.GreaterThan(0));
            Assert.That(r.StorageOk, Is.True);
        }
    }
}
