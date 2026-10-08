using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace MissionCore.Tests
{
    public class LauncherSpecTests
    {
        static LauncherSpec Dedicated() => new LauncherSpec
        {
            id = "electron", name = "Electron", leoKg = 300, priceUSD = 7500000, successRate = 0.9
        };

        static LauncherSpec Rideshare() => new LauncherSpec
        {
            id = "transporter", name = "Transporter", type = LaunchType.Rideshare, successRate = 0.99,
            pricePerKgUSD = 6500, minBillableMassKg = 50, maxPayloadPerCustomerKg = 500,
            orbits = new[] { LaunchOrbit.Sso }
        };

        static FormatException ValidateFails(LauncherSpec l) =>
            Assert.Throws<FormatException>(() => CatalogValidator.Validate(new List<LauncherSpec> { l }, "launchers.json"));

        // ---------- GetLaunchCost ----------

        [TestCase(0)]
        [TestCase(10)]
        [TestCase(250)]
        public void GetLaunchCost_Dedicated_IsFixedPrice(double massKG)
        {
            Assert.That(Dedicated().GetLaunchCost(massKG), Is.EqualTo(7500000));
        }

        [Test]
        public void GetLaunchCost_Rideshare_IsMassTimesPricePerKg()
        {
            Assert.That(Rideshare().GetLaunchCost(200), Is.EqualTo(200 * 6500));
        }

        [TestCase(0)]
        [TestCase(12)]
        [TestCase(50)]
        public void GetLaunchCost_Rideshare_BillsAtLeastMinimumMass(double massKG)
        {
            Assert.That(Rideshare().GetLaunchCost(massKG), Is.EqualTo(325000));
        }

        [Test]
        public void GetLaunchCost_NegativeMass_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => Rideshare().GetLaunchCost(-1));
        }

        // ---------- Loading ----------

        [Test]
        public void LoadList_MissingType_IsDedicated()
        {
            const string json = @"[{ ""id"": ""small"", ""leo_kg"": 300, ""price_USD"": 7500000, ""success_rate"": 0.9 }]";
            var list = CatalogLoader.LoadList<LauncherSpec>(json, "launchers.json");
            Assert.That(list[0].type, Is.EqualTo(LaunchType.Dedicated));
            Assert.That(list[0].IsRideshare, Is.False);
        }

        [Test]
        public void LoadList_ParsesRideshare()
        {
            const string json = @"[{ ""id"": ""rs"", ""name"": ""RS"", ""type"": ""rideshare"", ""price_per_kg_USD"": 6500,
                ""min_billable_mass_kg"": 50, ""max_payload_per_customer_kg"": 500, ""orbits"": [""sso"", ""leo""],
                ""altitude_m"": { ""min"": 500000, ""max"": 600000 }, ""success_rate"": 0.99 }]";
            var l = CatalogLoader.LoadList<LauncherSpec>(json, "launchers.json")[0];

            Assert.That(l.type, Is.EqualTo(LaunchType.Rideshare));
            Assert.That(l.pricePerKgUSD, Is.EqualTo(6500));
            Assert.That(l.minBillableMassKg, Is.EqualTo(50));
            Assert.That(l.maxPayloadPerCustomerKg, Is.EqualTo(500));
            Assert.That(l.orbits, Is.EqualTo(new[] { LaunchOrbit.Sso, LaunchOrbit.Leo }));
            Assert.That(l.altitudeM.Min, Is.EqualTo(500000));
            Assert.That(l.altitudeM.Max, Is.EqualTo(600000));
        }

        [TestCase(@"[{ ""id"": ""x"", ""type"": ""balloon"" }]")]
        [TestCase(@"[{ ""id"": ""x"", ""type"": ""rideshare"", ""orbits"": [""moon""] }]")]
        public void LoadList_UnknownTypeOrOrbit_ThrowsWithFileName(string json)
        {
            var ex = Assert.Throws<FormatException>(() => CatalogLoader.LoadList<LauncherSpec>(json, "launchers.json"));
            Assert.That(ex.Message, Does.Contain("launchers.json"));
        }

        // ---------- Validate ----------

        [Test]
        public void Validate_AcceptsDedicatedAndRideshare()
        {
            var list = new List<LauncherSpec> { Dedicated(), Rideshare() };
            Assert.DoesNotThrow(() => CatalogValidator.Validate(list, "launchers.json"));
        }

        [Test]
        public void Validate_RideshareNeedsNoLeoCapacityOrFixedPrice()
        {
            var l = Rideshare();
            Assert.That(l.leoKg, Is.EqualTo(0));
            Assert.That(l.priceUSD, Is.EqualTo(0));
            Assert.DoesNotThrow(() => CatalogValidator.Validate(new List<LauncherSpec> { l }, "launchers.json"));
        }

        [Test]
        public void Validate_DedicatedWithoutPrice_Fails()
        {
            var l = Dedicated();
            l.priceUSD = 0;
            Assert.That(ValidateFails(l).Message, Does.Contain("launchers.json").And.Contain("'electron'").And.Contain("price_USD"));
        }

        [Test]
        public void Validate_RideshareWithoutPricePerKg_Fails()
        {
            var l = Rideshare();
            l.pricePerKgUSD = 0;
            Assert.That(ValidateFails(l).Message, Does.Contain("launchers.json").And.Contain("'transporter'").And.Contain("price_per_kg_USD"));
        }

        [Test]
        public void Validate_RideshareWithoutMaxPayload_Fails()
        {
            var l = Rideshare();
            l.maxPayloadPerCustomerKg = 0;
            Assert.That(ValidateFails(l).Message, Does.Contain("max_payload_per_customer_kg"));
        }

        [Test]
        public void Validate_RideshareMinBillableAboveMaxPayload_Fails()
        {
            var l = Rideshare();
            l.minBillableMassKg = 600;
            Assert.That(ValidateFails(l).Message, Does.Contain("min_billable_mass_kg"));
        }

        [Test]
        public void Validate_RideshareWithoutOrbits_Fails()
        {
            var l = Rideshare();
            l.orbits = null;
            Assert.That(ValidateFails(l).Message, Does.Contain("orbits"));
        }

        [Test]
        public void Validate_RideshareWithInvertedAltitudeRange_Fails()
        {
            var l = Rideshare();
            l.altitudeM = new ValueRange(600000, 500000);
            Assert.That(ValidateFails(l).Message, Does.Contain("altitude_m"));
        }

        [Test]
        public void Validate_UndefinedTypeValue_FailsWithFileName()
        {
            var l = Dedicated();
            l.type = (LaunchType)42;
            Assert.That(ValidateFails(l).Message, Does.Contain("launchers.json").And.Contain("unknown type"));
        }
    }
}
