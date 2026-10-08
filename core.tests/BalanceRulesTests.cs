using System;
using System.Collections.Generic;
using NUnit.Framework;
using Status = MissionCore.BudgetStatus;

namespace MissionCore.Tests
{
    public class BalanceRulesTests
    {
        static StatusThreshold T(double green, double yellow = 0) =>
            new StatusThreshold { GreenMinReserve = green, YellowMinReserve = yellow };

        static Dictionary<string, StatusThreshold> AllThresholds() => new Dictionary<string, StatusThreshold>
        {
            ["mass"] = T(0.10), ["cost"] = T(0.10), ["power"] = T(0.10), ["delta_v"] = T(0.20), ["data"] = T(0.20),
        };

        static BalanceRules Rules(Dictionary<string, StatusThreshold> thresholds) => new BalanceRules
        {
            DryMassFractions = new MassFractions { Payload = 0.5, Structure = 0.5 },
            MassMarginEarlyPhase = 0.25,
            PowerMarginEarlyPhase = 0.25,
            StatusThresholds = thresholds
        };

        static FormatException ValidateFails(BalanceRules rules) =>
            Assert.Throws<FormatException>(() => CatalogValidator.Validate(rules, "balance_rules.json"));

        // ---------- Validate ----------

        [Test]
        public void Validate_AcceptsAllBudgets()
        {
            Assert.DoesNotThrow(() => CatalogValidator.Validate(Rules(AllThresholds()), "balance_rules.json"));
        }

        [Test]
        public void Validate_MissingThresholds_Fails()
        {
            Assert.That(ValidateFails(Rules(null)).Message, Does.Contain("balance_rules.json").And.Contain("status_thresholds"));
        }

        [TestCase("mass")]
        [TestCase("delta_v")]
        [TestCase("data")]
        public void Validate_MissingBudget_FailsAndNamesIt(string key)
        {
            var thresholds = AllThresholds();
            thresholds.Remove(key);
            Assert.That(ValidateFails(Rules(thresholds)).Message, Does.Contain($"'{key}'"));
        }

        [Test]
        public void Validate_YellowAboveGreen_Fails()
        {
            var thresholds = AllThresholds();
            thresholds["power"] = T(0.10, 0.20);
            Assert.That(ValidateFails(Rules(thresholds)).Message,
                Does.Contain("'power'").And.Contain("yellow_min_reserve > green_min_reserve"));
        }

        [Test]
        public void Validate_YellowEqualGreen_IsAllowed()
        {
            var thresholds = AllThresholds();
            thresholds["cost"] = T(0, 0);   // no yellow band: green or red only
            Assert.DoesNotThrow(() => CatalogValidator.Validate(Rules(thresholds), "balance_rules.json"));
        }

        [TestCase("deltav")]
        [TestCase("Mass")]
        [TestCase("thermal")]
        public void Validate_UnknownBudgetKey_Fails(string key)
        {
            var thresholds = AllThresholds();
            thresholds[key] = T(0.1);
            Assert.That(ValidateFails(Rules(thresholds)).Message, Does.Contain($"unknown budget '{key}'"));
        }

        // ---------- Loading ----------

        [Test]
        public void Load_ParsesThresholdsWithSnakeCaseKeys()
        {
            const string json = @"{ ""status_thresholds"": {
                ""delta_v"": { ""green_min_reserve"": 0.2, ""yellow_min_reserve"": 0.05 } } }";
            var rules = CatalogLoader.LoadObject<BalanceRules>(json, "balance_rules.json");
            StatusThreshold t = BudgetKind.DeltaV.Threshold(rules.StatusThresholds);
            Assert.That(t, Is.Not.Null);
            Assert.That(t.GreenMinReserve, Is.EqualTo(0.2));
            Assert.That(t.YellowMinReserve, Is.EqualTo(0.05));
        }

        [Test]
        public void RealFile_HasThresholdForEveryBudget()
        {
            var rules = TestHelpers.LoadRealBalanceRules();
            foreach (BudgetKind kind in Enum.GetValues(typeof(BudgetKind)))
                Assert.That(kind.Threshold(rules.StatusThresholds), Is.Not.Null, kind.ToString());
        }

        // ---------- StatusFor ----------

        // mass: green from 0.10, yellow from 0, red below 0
        [TestCase(0.50, Status.Green)]
        [TestCase(0.10, Status.Green)]
        [TestCase(0.0999, Status.Yellow)]
        [TestCase(0.0, Status.Yellow)]
        [TestCase(-0.0001, Status.Red)]
        [TestCase(-1.0, Status.Red)]
        public void StatusFor_UsesThresholdsOfTheBudget(double reserveFraction, Status expected)
        {
            Assert.That(Rules(AllThresholds()).StatusFor(BudgetKind.Mass, reserveFraction), Is.EqualTo(expected));
        }

        [Test]
        public void StatusFor_DifferentBudgetsUseTheirOwnThresholds()
        {
            var rules = Rules(AllThresholds());
            Assert.That(rules.StatusFor(BudgetKind.Mass, 0.15), Is.EqualTo(Status.Green));     // green from 0.10
            Assert.That(rules.StatusFor(BudgetKind.DeltaV, 0.15), Is.EqualTo(Status.Yellow));  // green from 0.20
        }

        // A broken calculation must never show green.
        [Test]
        public void StatusFor_NaN_IsRed()
        {
            Assert.That(Rules(AllThresholds()).StatusFor(BudgetKind.Power, double.NaN), Is.EqualTo(Status.Red));
        }
    }
}
