using System.Collections.Generic;
using Newtonsoft.Json;
using NUnit.Framework;

namespace MissionCore.Tests
{
    public class MissionDesignTests
    {
        static MissionDesign FullDesign() => new MissionDesign
        {
            MissionId = "tech_demo",
            Name = "First try",
            PlatformId = "cubesat_6u",
            InstrumentIds = new List<string> { "mag", "camera" },
            LauncherId = "spacex_transporter",
            PropulsionId = "cold_gas",
            CommsId = "s_band",
            BatteryId = "liion_small",
            SolarCellId = "gaas_triple",
            GroundStationIds = new List<string> { "svalbard", "weilheim" },
            BatteryCount = 2,
            SolarAreaM2 = 0.3,
            PropellantMassKg = 1.5,
            AltitudeM = 550000,
            InclinationDeg = 97.6
        };

        static MissionDesign RoundTrip(MissionDesign design) =>
            CatalogLoader.LoadObject<MissionDesign>(JsonConvert.SerializeObject(design), "design.json");

        [Test]
        public void NewDesign_HasEmptyListsAndNoParts()
        {
            var d = new MissionDesign();
            Assert.That(d.InstrumentIds, Is.Not.Null.And.Empty);
            Assert.That(d.GroundStationIds, Is.Not.Null.And.Empty);
            Assert.That(d.PlatformId, Is.Null);
            Assert.That(d.LauncherId, Is.Null);
        }

        [Test]
        public void NewDesign_NumbersDefaultToZero()
        {
            var d = new MissionDesign();
            Assert.That(d.SolarAreaM2, Is.EqualTo(0));
            Assert.That(d.PropellantMassKg, Is.EqualTo(0));
            Assert.That(d.AltitudeM, Is.EqualTo(0));
            Assert.That(d.InclinationDeg, Is.EqualTo(0));
        }

        [Test]
        public void Properties_CanBeSetAndChanged()
        {
            var d = FullDesign();
            d.InstrumentIds.Add("radiometer");
            d.InstrumentIds.Remove("mag");
            d.LauncherId = "electron";
            d.AltitudeM = 600000;

            Assert.That(d.InstrumentIds, Is.EqualTo(new[] { "camera", "radiometer" }));
            Assert.That(d.LauncherId, Is.EqualTo("electron"));
            Assert.That(d.AltitudeM, Is.EqualTo(600000));
        }

        [Test]
        public void Json_UsesSnakeCaseKeys()
        {
            string json = JsonConvert.SerializeObject(FullDesign());
            foreach (string key in new[] { "mission_id", "name", "platform_id", "instrument_ids", "launcher_id",
                                           "propulsion_id", "comms_id", "battery_id", "solar_cell_id",
                                           "ground_station_ids", "battery_count", "solar_area_m2",
                                           "propellant_mass_kg", "altitude_m", "inclination_deg" })
                Assert.That(json, Does.Contain($"\"{key}\":"), key);
        }

        [Test]
        public void Json_RoundTrip_KeepsEveryField()
        {
            var original = FullDesign();
            var d = RoundTrip(original);

            Assert.That(d.MissionId, Is.EqualTo(original.MissionId));
            Assert.That(d.Name, Is.EqualTo(original.Name));
            Assert.That(d.PlatformId, Is.EqualTo(original.PlatformId));
            Assert.That(d.InstrumentIds, Is.EqualTo(original.InstrumentIds));
            Assert.That(d.LauncherId, Is.EqualTo(original.LauncherId));
            Assert.That(d.PropulsionId, Is.EqualTo(original.PropulsionId));
            Assert.That(d.CommsId, Is.EqualTo(original.CommsId));
            Assert.That(d.BatteryId, Is.EqualTo(original.BatteryId));
            Assert.That(d.SolarCellId, Is.EqualTo(original.SolarCellId));
            Assert.That(d.GroundStationIds, Is.EqualTo(original.GroundStationIds));
            Assert.That(d.BatteryCount, Is.EqualTo(original.BatteryCount));
            Assert.That(d.SolarAreaM2, Is.EqualTo(original.SolarAreaM2));
            Assert.That(d.PropellantMassKg, Is.EqualTo(original.PropellantMassKg));
            Assert.That(d.AltitudeM, Is.EqualTo(original.AltitudeM));
            Assert.That(d.InclinationDeg, Is.EqualTo(original.InclinationDeg));
        }

        [Test]
        public void Json_RoundTrip_DoesNotDuplicateListEntries()
        {
            // Newtonsoft adds into the pre-initialised lists instead of replacing them,
            // so the defaults must stay empty or loaded entries would be appended to them
            var d = RoundTrip(FullDesign());
            Assert.That(d.InstrumentIds.Count, Is.EqualTo(2));
            Assert.That(d.GroundStationIds.Count, Is.EqualTo(2));
        }

        [Test]
        public void Json_HandWrittenFile_Loads()
        {
            const string json = @"{
                ""mission_id"": ""tech_demo"", ""platform_id"": ""cubesat_6u"",
                ""instrument_ids"": [""mag""], ""launcher_id"": ""electron"",
                ""battery_count"": 1, ""altitude_m"": 500000, ""inclination_deg"": 51.6
            }";
            var d = CatalogLoader.LoadObject<MissionDesign>(json, "design.json");

            Assert.That(d.MissionId, Is.EqualTo("tech_demo"));
            Assert.That(d.InstrumentIds, Is.EqualTo(new[] { "mag" }));
            Assert.That(d.GroundStationIds, Is.Not.Null.And.Empty);
            Assert.That(d.CommsId, Is.Null);
            Assert.That(d.InclinationDeg, Is.EqualTo(51.6));
        }

        [Test]
        public void Json_NullInstrumentList_KeepsEmptyDefault()
        {
            var d = CatalogLoader.LoadObject<MissionDesign>(@"{ ""instrument_ids"": null }", "design.json");
            Assert.That(d.InstrumentIds, Is.Not.Null.And.Empty);
        }

        [Test]
        public void NewDesign_HasOneBattery()
        {
            Assert.That(new MissionDesign().BatteryCount, Is.EqualTo(1));
        }

        [Test]
        public void Json_Malformed_ThrowsWithFileName()
        {
            var ex = Assert.Throws<System.FormatException>(
                () => CatalogLoader.LoadObject<MissionDesign>(@"{ ""battery_count"": ""two"" }", "design.json"));
            Assert.That(ex.Message, Does.Contain("design.json"));
        }
    }
}
