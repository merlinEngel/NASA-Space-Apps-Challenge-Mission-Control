using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using MissionCore.Catalogs;
using MissionCore.Design;
using MissionCore.Mission;
using MissionCore.Rules;
using MissionCore.Text;

namespace MissionCore.Tests
{
    // P7 step 3: text keys for the checklist rows (ReviewText) and their entries in texts.json
    public class ReviewTextTests
    {
        static Dictionary<string, Dictionary<string, string>> Texts() =>
            TextTableLoader.LoadTextTable(File.ReadAllText(Path.Combine(TestHelpers.StreamingDataDir(), "texts.json")), "texts.json");

        static void AssertKey(Dictionary<string, Dictionary<string, string>> texts, string key)
        {
            Assert.That(texts.ContainsKey(key), Is.True, $"missing key {key}");
            Assert.That(texts[key].ContainsKey("de") && !string.IsNullOrWhiteSpace(texts[key]["de"]), Is.True, $"{key}: no de");
            Assert.That(texts[key].ContainsKey("en") && !string.IsNullOrWhiteSpace(texts[key]["en"]), Is.True, $"{key}: no en");
        }

        static readonly ReviewCode[] RedCodes =
            { ReviewCode.LaunchMassTooHigh, ReviewCode.BudgetExceeded, ReviewCode.RequiredInstrumentMissing, ReviewCode.NoGroundStation };

        [Test]
        public void EveryCode_HasTitleGreenAndProblemText()
        {
            var texts = Texts();
            foreach (ReviewCode code in Enum.GetValues(typeof(ReviewCode)))
            {
                string problem = RedCodes.Contains(code) ? "red" : "yellow";
                AssertKey(texts, ReviewText.TitleKey(code));
                AssertKey(texts, ReviewText.Key(new ReviewItem(code, BudgetStatus.Green, 1, 2)));
                AssertKey(texts, ReviewText.Key(new ReviewItem(code, problem == "red" ? BudgetStatus.Red : BudgetStatus.Yellow, 1, 2)));
            }
        }

        [TestCase("review.low_power_reserve.battery")]
        [TestCase("review.no_avoidance_propellant.no_propulsion")]
        [TestCase("review.no_disposal_propellant.not_needed")]
        [TestCase("review.title")]
        [TestCase("review.confirm")]
        [TestCase("review.launch")]
        [TestCase("review.back")]
        [TestCase("launch.countdown")]
        [TestCase("launch.success")]
        [TestCase("launch.failure")]
        [TestCase("launch.result_screen")]
        public void SpecialAndScreenKeys_Exist(string key) => AssertKey(Texts(), key);

        [Test]
        public void TitleKey_IsSnakeCase()
        {
            Assert.That(ReviewText.TitleKey(ReviewCode.StorageFillsUp), Is.EqualTo("review.storage_fills_up.title"));
            Assert.That(ReviewText.TitleKey(ReviewCode.LifetimeShorterThanMission), Is.EqualTo("review.lifetime_shorter_than_mission.title"));
        }

        // The keys of real review rows, from the same designs as FlightReadinessReviewTests
        static ReviewResult ReviewOf(Action<MissionDesign> change = null, string streamingFile = null)
        {
            MissionDesign d;
            if (streamingFile != null)
            {
                string path = Path.Combine(TestHelpers.StreamingDataDir(), "TestDesigns", streamingFile);
                d = CatalogLoader.LoadObject<MissionDesign>(File.ReadAllText(path), streamingFile);
            }
            else d = MissionSimulationStartTests.TechDemo();
            change?.Invoke(d);
            return FlightReadinessReview.Review(FlightReadinessReviewTests.CatalogWithScoring(), d);
        }

        [Test]
        public void Key_TechDemo()
        {
            ReviewResult r = ReviewOf();
            Assert.That(ReviewText.Key(r.Item(ReviewCode.NoAvoidancePropellant)), Is.EqualTo("review.no_avoidance_propellant.no_propulsion"));
            Assert.That(ReviewText.Key(r.Item(ReviewCode.NoDisposalPropellant)), Is.EqualTo("review.no_disposal_propellant.not_needed"));
            Assert.That(ReviewText.Key(r.Item(ReviewCode.LaunchMassTooHigh)), Is.EqualTo("review.launch_mass_too_high.green"));
            Assert.That(ReviewText.Key(r.Item(ReviewCode.StorageFillsUp)), Is.EqualTo("review.storage_fills_up.green"));
        }

        [Test]
        public void Key_LowPower_Battery() =>
            Assert.That(ReviewText.Key(ReviewOf(streamingFile: "tech_demo_low_power.json").Item(ReviewCode.LowPowerReserve)),
                Is.EqualTo("review.low_power_reserve.battery"));

        [Test]
        public void Key_Electron_BudgetRed() =>
            Assert.That(ReviewText.Key(ReviewOf(d => d.LauncherId = "electron").Item(ReviewCode.BudgetExceeded)),
                Is.EqualTo("review.budget_exceeded.red"));

        [Test]
        public void Key_600Km_ColdGas2Kg_DisposalYellow()
        {
            ReviewResult r = ReviewOf(d => { d.AltitudeM = 600e3; d.PropulsionId = "cold_gas"; d.PropellantMassKg = 2; });
            Assert.That(ReviewText.Key(r.Item(ReviewCode.NoDisposalPropellant)), Is.EqualTo("review.no_disposal_propellant.yellow"));
            Assert.That(ReviewText.Key(r.Item(ReviewCode.NoAvoidancePropellant)), Is.EqualTo("review.no_avoidance_propellant.green"));
        }

        [Test]
        public void Key_600Km_ColdGas3Kg_DisposalGreenWithLimit()
        {
            // green with a limit > 0 means "enough propellant to lower the orbit", not "not needed"
            ReviewResult r = ReviewOf(d => { d.AltitudeM = 600e3; d.PropulsionId = "cold_gas"; d.PropellantMassKg = 3; });
            Assert.That(ReviewText.Key(r.Item(ReviewCode.NoDisposalPropellant)), Is.EqualTo("review.no_disposal_propellant.green"));
        }

        [TestCase(ReviewCode.LaunchMassTooHigh, Metric.MassKg)]
        [TestCase(ReviewCode.BudgetExceeded, Metric.CostUsd)]
        [TestCase(ReviewCode.RequiredInstrumentMissing, Metric.Raw)]
        [TestCase(ReviewCode.NoGroundStation, Metric.Raw)]
        [TestCase(ReviewCode.LowPowerReserve, Metric.Raw)]
        [TestCase(ReviewCode.StorageFillsUp, Metric.Raw)]
        [TestCase(ReviewCode.NoAvoidancePropellant, Metric.DeltaVMPerS)]
        [TestCase(ReviewCode.NoDisposalPropellant, Metric.DeltaVMPerS)]
        [TestCase(ReviewCode.LifetimeShorterThanMission, Metric.LifetimeS)]
        public void MetricFor_MatchesValueUnit(ReviewCode code, Metric expected) =>
            Assert.That(ReviewText.MetricFor(code), Is.EqualTo(expected));

        [Test]
        public void EveryRowText_FormatsWithoutLeftoverPlaceholders()
        {
            var table = new TextTable(Texts());
            foreach (string lang in new[] { "de", "en" })
            {
                table.Language = lang;
                foreach (ReviewItem item in ReviewOf().Items)
                {
                    string text = table.Get(ReviewText.Key(item), "19 kg", "24 kg", item.Detail ?? "S");
                    Assert.That(text, Does.Not.Contain("{"), $"{lang} {ReviewText.Key(item)}: {text}");
                }
            }
            Assert.That(table.MissingKeys, Is.Empty);
        }

        [Test]
        public void StorageYellow_NamesTheUnitDays()
        {
            // Raw adds no unit, so the text has to say "days" itself
            var texts = Texts();
            Assert.That(texts["review.storage_fills_up.yellow"]["de"], Does.Contain("Tage"));
            Assert.That(texts["review.storage_fills_up.yellow"]["en"], Does.Contain("days"));
        }
    }
}
