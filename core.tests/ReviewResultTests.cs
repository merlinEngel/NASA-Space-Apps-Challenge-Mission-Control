using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using MissionCore.Catalogs;
using MissionCore.Design;
using MissionCore.Mission;

namespace MissionCore.Tests
{
    // P7 step 4: ReviewResult.CanLaunchWith (red blocks, every yellow must be confirmed) and MissionDesign.DeepCopy
    public class ReviewResultTests
    {
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
        public void TechDemo_NothingConfirmed_CannotLaunch()
        {
            ReviewResult r = ReviewOf();
            Assert.That(r.CanLaunch, Is.True, "no red item");
            Assert.That(r.CanLaunchWith(new ReviewCode[0]), Is.False, "the avoidance risk is not confirmed");
        }

        [Test]
        public void TechDemo_AvoidanceConfirmed_CanLaunch() =>
            Assert.That(ReviewOf().CanLaunchWith(new[] { ReviewCode.NoAvoidancePropellant }), Is.True);

        [Test]
        public void LowPower_OnlyAvoidanceConfirmed_CannotLaunch() =>
            Assert.That(ReviewOf(streamingFile: "tech_demo_low_power.json").CanLaunchWith(new[] { ReviewCode.NoAvoidancePropellant }), Is.False);

        [Test]
        public void LowPower_BothConfirmed_CanLaunch()
        {
            ReviewResult r = ReviewOf(streamingFile: "tech_demo_low_power.json");
            Assert.That(r.CanLaunchWith(new[] { ReviewCode.NoAvoidancePropellant, ReviewCode.LowPowerReserve }), Is.True);
            Assert.That(r.CanLaunchWith(r.Risks.Select(i => i.Code)), Is.True, "what the FRR shortcut button passes");
        }

        [Test]
        public void Electron_RedCannotBeConfirmed()
        {
            ReviewResult r = ReviewOf(d => d.LauncherId = "electron");
            var everything = (ReviewCode[])Enum.GetValues(typeof(ReviewCode));
            Assert.That(r.CanLaunchWith(everything), Is.False);
        }

        [Test]
        public void ConfirmingAGreenItem_ChangesNothing()
        {
            ReviewResult r = ReviewOf();
            Assert.That(r.CanLaunchWith(new[] { ReviewCode.LaunchMassTooHigh }), Is.False);
            Assert.That(r.CanLaunchWith(new[] { ReviewCode.LaunchMassTooHigh, ReviewCode.NoAvoidancePropellant }), Is.True);
        }

        [Test]
        public void AllGreen_NothingToConfirm_CanLaunch()
        {
            ReviewResult r = ReviewOf(d => { d.AltitudeM = 600e3; d.PropulsionId = "cold_gas"; d.PropellantMassKg = 3; });
            Assert.That(r.Risks, Is.Empty);
            Assert.That(r.CanLaunchWith(new ReviewCode[0]), Is.True);
        }

        [Test]
        public void NullConfirmed_Throws() =>
            Assert.Throws<ArgumentNullException>(() => ReviewOf().CanLaunchWith(null));

        [Test]
        public void CanLaunchWith_EnumeratesConfirmedOnce()
        {
            // a lazy sequence must not be walked once per risk
            int walks = 0;
            IEnumerable<ReviewCode> Confirmed() { walks++; yield return ReviewCode.NoAvoidancePropellant; yield return ReviewCode.LowPowerReserve; }
            ReviewOf(streamingFile: "tech_demo_low_power.json").CanLaunchWith(Confirmed());
            Assert.That(walks, Is.EqualTo(1));
        }

        [Test]
        public void DeepCopy_IsEqualButIndependent()
        {
            MissionDesign original = MissionSimulationStartTests.TechDemo();
            MissionDesign copy = original.DeepCopy();

            Assert.That(copy, Is.Not.SameAs(original));
            Assert.That(copy.MissionId, Is.EqualTo(original.MissionId));
            Assert.That(copy.BatteryCount, Is.EqualTo(original.BatteryCount));
            Assert.That(copy.AltitudeM, Is.EqualTo(original.AltitudeM));
            Assert.That(copy.GroundStationIds, Is.EqualTo(original.GroundStationIds));

            copy.BatteryCount = 5;
            copy.GroundStationIds.Add("wallops");
            copy.InstrumentIds.Clear();
            Assert.That(original.BatteryCount, Is.EqualTo(2), "changing the copy must not touch the session's design");
            Assert.That(original.GroundStationIds.Count, Is.EqualTo(2), "lists are copied too, not shared");
            Assert.That(original.InstrumentIds.Count, Is.EqualTo(1));
        }

        [Test]
        public void DeepCopy_EvaluatesTheSame()
        {
            var c = FlightReadinessReviewTests.CatalogWithScoring();
            MissionDesign original = MissionSimulationStartTests.TechDemo();
            Assert.That(DesignValidator.Evaluate(c, original.DeepCopy()).MassResult.TotalMassKg,
                Is.EqualTo(DesignValidator.Evaluate(c, original).MassResult.TotalMassKg));
        }
    }
}
