using System;
using System.Collections.Generic;
using NUnit.Framework;
using MissionCore;

namespace MissionCore.Tests
{
    public class CatalogLoaderTests
    {
        // ---------- LoadList ----------

        [Test]
        public void LoadList_ParsesPropulsion_WithSnakeCaseKeys()
        {
            const string json = @"[
                { ""id"": ""cold_gas"", ""name"": ""Cold gas"", ""isp_s"": 70, ""thrust_N"": 1, ""source"": ""a.md"" },
                { ""id"": ""ion"", ""name"": ""Ion"", ""isp_s"": 3100, ""thrust_N"": 0.092, ""power_W"": 2300, ""source"": ""b.md"" }
            ]";
            var list = CatalogLoader.LoadList<PropulsionSpec>(json, "propulsion.json");

            Assert.That(list.Count, Is.EqualTo(2));
            Assert.That(list[0].id, Is.EqualTo("cold_gas"));
            Assert.That(list[0].name, Is.EqualTo("Cold gas"));
            Assert.That(list[0].source, Is.EqualTo("a.md"));
            Assert.That(list[0].ispS, Is.EqualTo(70));
            Assert.That(list[0].thrustN, Is.EqualTo(1));
            Assert.That(list[1].ispS, Is.EqualTo(3100));
            Assert.That(list[1].thrustN, Is.EqualTo(0.092));
            Assert.That(list[1].powerW, Is.EqualTo(2300));
        }

        [Test]
        public void LoadList_MissingNullableField_IsNull()
        {
            const string json = @"[{ ""id"": ""cold_gas"", ""isp_s"": 70, ""thrust_N"": 1 }]";
            var list = CatalogLoader.LoadList<PropulsionSpec>(json, "propulsion.json");
            Assert.That(list[0].powerW, Is.Null);
        }

        [Test]
        public void LoadList_ParsesLaunchers_WithOptionalOrbitCapacities()
        {
            const string json = @"[
                { ""id"": ""small"", ""leo_kg"": 300, ""sso_kg"": 200, ""price_USD"": 7500000, ""success_rate"": 0.9 },
                { ""id"": ""big"", ""leo_kg"": 17500, ""gto_kg"": 5500, ""price_USD"": 70000000, ""success_rate"": 0.99 }
            ]";
            var list = CatalogLoader.LoadList<LauncherSpec>(json, "launchers.json");

            Assert.That(list[0].leoKg, Is.EqualTo(300));
            Assert.That(list[0].ssoKg, Is.EqualTo(200));
            Assert.That(list[0].gtoKg, Is.Null);
            Assert.That(list[0].priceUSD, Is.EqualTo(7500000));
            Assert.That(list[0].successRate, Is.EqualTo(0.9));
            Assert.That(list[1].ssoKg, Is.Null);
            Assert.That(list[1].gtoKg, Is.EqualTo(5500));
        }

        [Test]
        public void LoadList_ParsesInstruments()
        {
            const string json = @"[{ ""id"": ""mag"", ""name"": ""Magnetometer"", ""mass_kg"": 3.5, ""power_W"": 3, ""data_rate_class"": ""very_low"", ""price_USD"": 300000 }]";
            var list = CatalogLoader.LoadList<InstrumentSpec>(json, "instruments.json");

            Assert.That(list[0].id, Is.EqualTo("mag"));
            Assert.That(list[0].massKg, Is.EqualTo(3.5));
            Assert.That(list[0].powerW, Is.EqualTo(3));
            Assert.That(list[0].dataRateClass, Is.EqualTo("very_low"));
            Assert.That(list[0].priceUSD, Is.EqualTo(300000));
        }

        [Test]
        public void LoadList_UnknownField_IsIgnored()
        {
            const string json = @"[{ ""id"": ""x"", ""isp_s"": 100, ""thrust_N"": 1, ""comment"": ""not a property"" }]";
            Assert.DoesNotThrow(() => CatalogLoader.LoadList<PropulsionSpec>(json, "propulsion.json"));
        }

        [Test]
        public void LoadList_EmptyArray_ReturnsEmptyList()
        {
            var list = CatalogLoader.LoadList<PropulsionSpec>("[]", "propulsion.json");
            Assert.That(list, Is.Empty);
        }

        [TestCase("[{ \"id\": \"x\", ")]                 // truncated
        [TestCase("[{ \"id\": \"x\", \"isp_s\": \"abc\" }]")]  // wrong type
        [TestCase("{ \"id\": \"x\" }")]                   // object instead of array
        public void LoadList_MalformedJson_ThrowsWithFileName(string json)
        {
            var ex = Assert.Throws<FormatException>(
                () => CatalogLoader.LoadList<PropulsionSpec>(json, "propulsion.json"));
            Assert.That(ex.Message, Does.Contain("propulsion.json"));
        }

        [TestCase("")]
        [TestCase("   ")]
        [TestCase("null")]
        public void LoadList_EmptyOrNull_ThrowsWithFileName(string json)
        {
            var ex = Assert.Throws<FormatException>(
                () => CatalogLoader.LoadList<PropulsionSpec>(json, "propulsion.json"));
            Assert.That(ex.Message, Does.Contain("propulsion.json"));
        }

        // ---------- LoadObject ----------

        [Test]
        public void LoadObject_ParsesBalanceRules()
        {
            const string json = @"{
                ""dry_mass_fractions_earth_orbit_with_propulsion"": {
                    ""payload"": 0.25, ""structure"": 0.25, ""thermal"": 0.03, ""power"": 0.20,
                    ""communications"": 0.04, ""attitude_control"": 0.07, ""computer"": 0.04,
                    ""propulsion"": 0.10, ""other"": 0.02
                },
                ""mass_margin_early_phase"": 0.25,
                ""power_margin_early_phase"": 0.3,
                ""source"": ""smad.md""
            }";
            var rules = CatalogLoader.LoadObject<BalanceRules>(json, "balance_rules.json");

            Assert.That(rules.MassMarginEarlyPhase, Is.EqualTo(0.25));
            Assert.That(rules.PowerMarginEarlyPhase, Is.EqualTo(0.3));
            Assert.That(rules.Source, Is.EqualTo("smad.md"));
            Assert.That(rules.DryMassFractions, Is.Not.Null);
            Assert.That(rules.DryMassFractions.Payload, Is.EqualTo(0.25));
            Assert.That(rules.DryMassFractions.AttitudeControl, Is.EqualTo(0.07));
            Assert.That(rules.DryMassFractions.Propulsion, Is.EqualTo(0.10));
            Assert.That(rules.DryMassFractions.Other, Is.EqualTo(0.02));
        }

        [Test]
        public void LoadObject_MalformedJson_ThrowsWithFileName()
        {
            var ex = Assert.Throws<FormatException>(
                () => CatalogLoader.LoadObject<BalanceRules>("{ \"source\": ", "balance_rules.json"));
            Assert.That(ex.Message, Does.Contain("balance_rules.json"));
        }

        // LoadList rejects empty/null input; LoadObject should behave the same way
        [TestCase("")]
        [TestCase("null")]
        public void LoadObject_EmptyOrNull_ThrowsWithFileName(string json)
        {
            var ex = Assert.Throws<FormatException>(
                () => CatalogLoader.LoadObject<BalanceRules>(json, "balance_rules.json"));
            Assert.That(ex.Message, Does.Contain("balance_rules.json"));
        }

        // ---------- Validate ----------

        static PropulsionSpec Prop(string id, double isp) => new PropulsionSpec { id = id, name = "n", ispS = isp, thrustN = 1 };

        [Test]
        public void Validate_AcceptsValidList()
        {
            var list = new List<PropulsionSpec> { Prop("a", 70), Prop("b", 3100) };
            Assert.DoesNotThrow(() => CatalogValidator.Validate(list, "propulsion.json"));
        }

        [Test]
        public void Validate_RejectsDuplicateId()
        {
            var list = new List<PropulsionSpec> { Prop("a", 70), Prop("a", 300) };
            var ex = Assert.Throws<FormatException>(() => CatalogValidator.Validate(list, "propulsion.json"));
            Assert.That(ex.Message, Does.Contain("propulsion.json").And.Contain("'a'"));
        }

        [TestCase(null)]
        [TestCase("")]
        public void Validate_RejectsMissingId(string id)
        {
            var list = new List<PropulsionSpec> { Prop(id, 70) };
            var ex = Assert.Throws<FormatException>(() => CatalogValidator.Validate(list, "propulsion.json"));
            Assert.That(ex.Message, Does.Contain("propulsion.json"));
        }

        static InstrumentSpec Instrument(double priceUSD) =>
            new InstrumentSpec { id = "cam", name = "Camera", massKg = 3, powerW = 6, priceUSD = priceUSD, dataRateBpS = 2000000, dutyCycle = 0.05 };

        [Test]
        public void Validate_AcceptsInstrumentWithPrice()
        {
            var list = new List<InstrumentSpec> { Instrument(1500000) };
            Assert.DoesNotThrow(() => CatalogValidator.Validate(list, "instruments.json"));
        }

        [TestCase(0)]
        [TestCase(-1)]
        public void Validate_RejectsInstrumentWithoutPositivePrice(double priceUSD)
        {
            var list = new List<InstrumentSpec> { Instrument(priceUSD) };
            var ex = Assert.Throws<FormatException>(() => CatalogValidator.Validate(list, "instruments.json"));
            Assert.That(ex.Message, Does.Contain("instruments.json").And.Contain("'cam'").And.Contain("price_USD"));
        }

        [Test]
        public void Validate_RejectsInstrumentWithMissingPriceField()
        {
            const string json = @"[{ ""id"": ""cam"", ""name"": ""Camera"", ""mass_kg"": 3, ""power_W"": 6, ""data_rate_bps"": 2000000, ""duty_cycle"": 0.05 }]";
            var list = CatalogLoader.LoadList<InstrumentSpec>(json, "instruments.json");
            var ex = Assert.Throws<FormatException>(() => CatalogValidator.Validate(list, "instruments.json"));
            Assert.That(ex.Message, Does.Contain("'cam'").And.Contain("price_USD"));
        }

        [TestCase(0, 0.5, "data_rate_bps")]
        [TestCase(1000, 0, "duty_cycle")]
        [TestCase(1000, 1.5, "duty_cycle")]
        public void Validate_RejectsInstrumentWithInvalidDataFields(double dataRateBpS, double dutyCycle, string field)
        {
            var i = Instrument(1500000);
            i.dataRateBpS = dataRateBpS;
            i.dutyCycle = dutyCycle;
            var ex = Assert.Throws<FormatException>(() => CatalogValidator.Validate(new List<InstrumentSpec> { i }, "instruments.json"));
            Assert.That(ex.Message, Does.Contain("'cam'").And.Contain(field));
        }

        [Test]
        public void Validate_RejectsPlatformWithoutStorage()
        {
            var p = new PlatformSpec { id = "bus", name = "Bus", busMassKg = 1, maxMassKg = 2, maxSolarAreaM2 = 0.1, lifetimeYears = 1 };
            var ex = Assert.Throws<FormatException>(() => CatalogValidator.Validate(new List<PlatformSpec> { p }, "platforms.json"));
            Assert.That(ex.Message, Does.Contain("platforms.json").And.Contain("'bus'").And.Contain("storage_bits"));
        }

        [TestCase(0)]
        [TestCase(-10)]
        public void Validate_RejectsNonPositiveIsp(double isp)
        {
            var list = new List<PropulsionSpec> { Prop("bad", isp) };
            var ex = Assert.Throws<FormatException>(() => CatalogValidator.Validate(list, "propulsion.json"));
            Assert.That(ex.Message, Does.Contain("'bad'"));
        }
    }
}
