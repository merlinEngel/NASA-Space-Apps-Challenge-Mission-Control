using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using MissionCore.Catalogs;
using MissionCore.Design;
using MissionCore.Simulation;
using MissionCore.Mission;
using MissionCore.Rules;

namespace MissionCore.Tests
{
    // P7 steps 1 and 2: FlightReadinessReview.Review, red and yellow checks and the review data types.
    // Reference values computed by hand from the catalog (python):
    //   tech demo good: dry 10 + 3 + 0.3 + 2 × 0.65 + 3.5 × 0.2 = 15.3 kg, × 1.25 margin = 19.125 kg (limit 24 kg, 12U)
    //                   cost 3.16 M hardware + 50 kg × 6 500 USD rideshare = 3.485 M USD (budget 5 M)
    //   + cold gas, 5 kg propellant: 16.1 × 1.25 + 5 = 25.125 kg > 24 kg
    //   on a dedicated Electron: 3.16 M + 7.5 M = 10.66 M USD > 5 M
    public class FlightReadinessReviewTests
    {
        // Like MissionSimulationStartTests.RealCatalog(), plus scoring.json (disposal_max_years)
        internal static Catalog CatalogWithScoring() => Catalog();

        static Catalog Catalog(Action<BalanceRules> changeBalance = null, Action<MissionTemplate> changeMission = null)
        {
            Catalog c = MissionSimulationStartTests.RealCatalog();
            BalanceRules balance = TestHelpers.LoadRealBalanceRules();
            changeBalance?.Invoke(balance);
            ScoringRules scoring = CatalogLoader.LoadObject<ScoringRules>(File.ReadAllText(TestHelpers.RulesFile("scoring.json")), "scoring.json");
            var missions = c.Missions.Values.ToList();
            if (changeMission != null) changeMission(missions.First(m => m.id == "techdemo"));
            return new Catalog(c.Propulsion.Values.ToList(), c.Instruments.Values.ToList(), c.Launchers.Values.ToList(),
                c.Comms.Values.ToList(), c.Batteries.Values.ToList(), c.Platforms.Values.ToList(),
                c.GroundStations.Values.ToList(), c.SolarCells.Values.ToList(), missions,
                balance, scoring, c.Sim, null, new List<OrbitPresetSpec>());
        }
        static MissionDesign Good() => MissionSimulationStartTests.TechDemo();

        static MissionDesign StreamingTestDesign(string file)
        {
            string path = Path.Combine(TestHelpers.StreamingDataDir(), "TestDesigns", file);
            return CatalogLoader.LoadObject<MissionDesign>(File.ReadAllText(path), file);
        }

        // Decided 23.10.: no propulsion means no collision avoidance, so the tech demo has exactly one yellow item.
        [Test]
        public void TechDemoGood_OnlyAvoidanceYellow_CanLaunch()
        {
            ReviewResult r = FlightReadinessReview.Review(Catalog(), Good());

            Assert.That(r.Items.Count, Is.EqualTo(Enum.GetValues(typeof(ReviewCode)).Length));
            Assert.That(r.Items.Count, Is.EqualTo(9));
            Assert.That(r.Items.Where(i => i.Status != BudgetStatus.Green).Select(i => i.Code),
                Is.EqualTo(new[] { ReviewCode.NoAvoidancePropellant }));
            Assert.That(r.Item(ReviewCode.NoAvoidancePropellant).Status, Is.EqualTo(BudgetStatus.Yellow));
            Assert.That(r.CanLaunch, Is.True);
            Assert.That(r.Risks.Select(i => i.Code), Is.EqualTo(new[] { ReviewCode.NoAvoidancePropellant }));
        }

        [Test]
        public void StreamingTechDemoGood_NoRed_CanLaunch()
        {
            ReviewResult r = FlightReadinessReview.Review(Catalog(), StreamingTestDesign("tech_demo_good.json"));
            Assert.That(r.CanLaunch, Is.True);
            Assert.That(r.Risks.Select(i => i.Code), Is.EqualTo(new[] { ReviewCode.NoAvoidancePropellant }));
        }

        [Test]
        public void Items_AreInEnumOrder_OnePerCode()
        {
            ReviewResult r = FlightReadinessReview.Review(Catalog(), Good());
            for (int i = 0; i < r.Items.Count; i++)
                Assert.That(r.Items[i].Code, Is.EqualTo((ReviewCode)i), $"index {i}");
            foreach (ReviewCode code in Enum.GetValues(typeof(ReviewCode)))
                Assert.That(r.Item(code).Code, Is.EqualTo(code));
        }

        [Test]
        public void TechDemoInvalid_Throws_WithIssueCode()
        {
            var ex = Assert.Throws<ArgumentException>(() =>
                FlightReadinessReview.Review(Catalog(), StreamingTestDesign("tech_demo_invalid.json")));
            Assert.That(ex.Message, Does.Contain(nameof(DesignIssueCode.MissingLauncher)));
        }

        [Test]
        public void BatteryRemoved_Throws_NamesMissingBattery()
        {
            MissionDesign d = Good();
            d.BatteryId = null;
            var ex = Assert.Throws<ArgumentException>(() => FlightReadinessReview.Review(Catalog(), d));
            Assert.That(ex.Message, Does.Contain(nameof(DesignIssueCode.MissingBattery)));
        }

        [Test]
        public void TechDemoGood_MassAndBudgetRows_CarryValueAndLimit()
        {
            ReviewResult r = FlightReadinessReview.Review(Catalog(), Good());

            ReviewItem mass = r.Item(ReviewCode.LaunchMassTooHigh);
            Assert.That(mass.Value, Is.EqualTo(19.125).Within(1e-9));
            Assert.That(mass.Limit, Is.EqualTo(24).Within(1e-9));

            ReviewItem cost = r.Item(ReviewCode.BudgetExceeded);
            Assert.That(cost.Value, Is.EqualTo(3.485e6).Within(1e-3));
            Assert.That(cost.Limit, Is.EqualTo(5e6).Within(1e-3));
        }

        [Test]
        public void TooHeavy_LaunchMassRed_CannotLaunch()
        {
            MissionDesign d = Good();
            d.PropulsionId = "cold_gas";
            d.PropellantMassKg = 5;
            ReviewResult r = FlightReadinessReview.Review(Catalog(), d);

            ReviewItem mass = r.Item(ReviewCode.LaunchMassTooHigh);
            Assert.That(mass.Status, Is.EqualTo(BudgetStatus.Red));
            Assert.That(mass.Value, Is.EqualTo(25.125).Within(1e-9));
            Assert.That(mass.Value, Is.GreaterThan(mass.Limit));
            Assert.That(r.Item(ReviewCode.BudgetExceeded).Status, Is.EqualTo(BudgetStatus.Green), "3.545 M USD is still in budget");
            Assert.That(r.CanLaunch, Is.False);
        }

        [Test]
        public void DedicatedElectron_BudgetRed_CannotLaunch()
        {
            MissionDesign d = Good();
            d.LauncherId = "electron";
            ReviewResult r = FlightReadinessReview.Review(Catalog(), d);

            ReviewItem cost = r.Item(ReviewCode.BudgetExceeded);
            Assert.That(cost.Status, Is.EqualTo(BudgetStatus.Red));
            Assert.That(cost.Value, Is.EqualTo(10.66e6).Within(1e-3));
            Assert.That(cost.Value, Is.EqualTo(r.Report.CostResult.TotalCostUSD));
            Assert.That(r.Item(ReviewCode.LaunchMassTooHigh).Status, Is.EqualTo(BudgetStatus.Green));
            Assert.That(r.CanLaunch, Is.False);
        }

        [Test]
        public void HighResCamera_BudgetRed()
        {
            MissionDesign d = Good();
            d.InstrumentIds = new() { "camera_high_res" };
            DesignReport direct = DesignValidator.Evaluate(Catalog(), d);
            Assume.That(direct.IsValid, "camera_high_res must make a valid design for this test");

            ReviewResult r = FlightReadinessReview.Review(Catalog(), d);
            Assert.That(r.Item(ReviewCode.BudgetExceeded).Status, Is.EqualTo(BudgetStatus.Red));
            Assert.That(r.Item(ReviewCode.BudgetExceeded).Value, Is.EqualTo(direct.CostResult.TotalCostUSD).Within(1e-6));
            Assert.That(r.CanLaunch, Is.False);
        }

        [Test]
        public void TechDemoGood_InstrumentRow_OneOfOne()
        {
            ReviewItem item = FlightReadinessReview.Review(Catalog(), Good()).Item(ReviewCode.RequiredInstrumentMissing);
            Assert.That(item.Status, Is.EqualTo(BudgetStatus.Green));
            Assert.That(item.Value, Is.EqualTo(1));
            Assert.That(item.Limit, Is.EqualTo(1));
        }

        [Test]
        public void TechDemoGood_GroundStationRow_TwoStations_SBand()
        {
            ReviewItem item = FlightReadinessReview.Review(Catalog(), Good()).Item(ReviewCode.NoGroundStation);
            Assert.That(item.Status, Is.EqualTo(BudgetStatus.Green));
            Assert.That(item.Value, Is.EqualTo(2));
            Assert.That(item.Detail, Is.EqualTo("S"));
        }

        [Test]
        public void Report_MatchesDirectEvaluate()
        {
            ReviewResult r = FlightReadinessReview.Review(Catalog(), Good());
            DesignReport direct = DesignValidator.Evaluate(Catalog(), Good());
            Assert.That(r.Report.MassResult.TotalMassKg, Is.EqualTo(direct.MassResult.TotalMassKg));
            Assert.That(r.Report.CostResult.TotalCostUSD, Is.EqualTo(direct.CostResult.TotalCostUSD));
        }

        [Test]
        public void ReviewResult_CanLaunchAndRisks_FollowStatuses()
        {
            var items = new[]
            {
                new ReviewItem(ReviewCode.LaunchMassTooHigh, BudgetStatus.Green),
                new ReviewItem(ReviewCode.LowPowerReserve, BudgetStatus.Yellow),
                new ReviewItem(ReviewCode.StorageFillsUp, BudgetStatus.Yellow),
            };
            var r = new ReviewResult(null, items);
            Assert.That(r.CanLaunch, Is.True, "yellow does not block the launch");
            Assert.That(r.Risks.Select(i => i.Code), Is.EqualTo(new[] { ReviewCode.LowPowerReserve, ReviewCode.StorageFillsUp }));

            var blocked = new ReviewResult(null, items.Append(new ReviewItem(ReviewCode.BudgetExceeded, BudgetStatus.Red)).ToList());
            Assert.That(blocked.CanLaunch, Is.False);
        }

        [Test]
        public void ReviewItem_DefaultsAreNaNAndNull()
        {
            var item = new ReviewItem(ReviewCode.StorageFillsUp, BudgetStatus.Green);
            Assert.That(item.Value, Is.NaN);
            Assert.That(item.Limit, Is.NaN);
            Assert.That(item.Detail, Is.Null);
        }

        // --- P7 step 2: yellow checks. Budget values from a probe run of the core, delta-v checked in python ---

        static MissionDesign At600Km(double coldGasKg)
        {
            MissionDesign d = Good();
            d.AltitudeM = 600e3;
            if (coldGasKg > 0) { d.PropulsionId = "cold_gas"; d.PropellantMassKg = coldGasKg; }
            return d;
        }

        [Test]
        public void TechDemoGood_PowerStorageDisposal_Green()
        {
            ReviewResult r = FlightReadinessReview.Review(Catalog(), Good());

            ReviewItem power = r.Item(ReviewCode.LowPowerReserve);
            Assert.That(power.Value, Is.EqualTo(1.1045).Within(1e-3));
            Assert.That(power.Limit, Is.EqualTo(0.10));
            Assert.That(power.Detail, Is.Null);

            ReviewItem storage = r.Item(ReviewCode.StorageFillsUp);
            Assert.That(storage.Value, Is.EqualTo(double.PositiveInfinity), "3.46 Gbit made < 7.77 Gbit sent per day");
            Assert.That(storage.Limit, Is.EqualTo(90).Within(1e-9));

            ReviewItem disposal = r.Item(ReviewCode.NoDisposalPropellant);
            Assert.That(disposal.Status, Is.EqualTo(BudgetStatus.Green), "1.76 years orbit, decays on its own");
            Assert.That(disposal.Limit, Is.EqualTo(0));
        }

        [Test]
        public void TechDemoGood_Avoidance_NoPropulsion()
        {
            ReviewItem item = FlightReadinessReview.Review(Catalog(), Good()).Item(ReviewCode.NoAvoidancePropellant);
            Assert.That(item.Status, Is.EqualTo(BudgetStatus.Yellow));
            Assert.That(item.Detail, Is.EqualTo("no_propulsion"));
            Assert.That(item.Value, Is.EqualTo(0));
            Assert.That(item.Limit, Is.EqualTo(90 / 365.25).Within(1e-9));   // 1 m/s per year × 90 days
        }

        [Test]
        public void LowPower_PowerYellow_Battery_CanLaunch()
        {
            ReviewResult r = FlightReadinessReview.Review(Catalog(), StreamingTestDesign("tech_demo_low_power.json"));
            ReviewItem power = r.Item(ReviewCode.LowPowerReserve);
            Assert.That(power.Status, Is.EqualTo(BudgetStatus.Yellow));
            Assert.That(power.Value, Is.EqualTo(-0.6561).Within(1e-3));
            Assert.That(power.Detail, Is.EqualTo("battery"));
            Assert.That(r.CanLaunch, Is.True, "too little power loses data, the rocket still flies");
            Assert.That(r.Risks.Select(i => i.Code), Does.Contain(ReviewCode.LowPowerReserve));
        }

        [Test]
        public void Storage_DaysUntilFull_IsCapacityOverSurplus()
        {
            // One station less: less downlink than data made, storage fills up
            MissionDesign d = Good();
            d.GroundStationIds = new() { "svalbard" };
            ReviewResult r = FlightReadinessReview.Review(Catalog(), d);
            var data = r.Report.DataResult;
            ReviewItem item = r.Item(ReviewCode.StorageFillsUp);

            if (data.GeneratedBitsPerDay > data.DownlinkBitsPerDay && data.StorageOk)
            {
                double expected = data.StorageCapacityBits / (data.GeneratedBitsPerDay - data.DownlinkBitsPerDay);
                Assert.That(item.Value, Is.EqualTo(expected).Within(1e-9));
                Assert.That(item.Status, Is.EqualTo(expected < 90 ? BudgetStatus.Yellow : BudgetStatus.Green));
            }
            else
                Assert.That(item.Value, Is.EqualTo(data.StorageOk ? double.PositiveInfinity : 0));
        }

        [Test]
        public void Storage_SmallStorage_FillsBeforeMissionEnd()
        {
            // Platform storage is a catalog value; a 6U with half the storage and a slower downlink:
            // use the slow_downlink test design, which makes more data than it sends
            string json = File.ReadAllText(Path.Combine(Directory.GetParent(TestHelpers.StreamingDataDir()).Parent.Parent.FullName,
                "core.tests", "Data", "designs", "slow_downlink.json"));
            MissionDesign d = CatalogLoader.LoadObject<MissionDesign>(json, "slow_downlink.json");
            ReviewResult r = FlightReadinessReview.Review(Catalog(), d);
            var data = r.Report.DataResult;
            Assert.That(data.GeneratedBitsPerDay, Is.GreaterThan(data.DownlinkBitsPerDay), "precondition");

            ReviewItem item = r.Item(ReviewCode.StorageFillsUp);
            double expected = data.StorageOk ? data.StorageCapacityBits / (data.GeneratedBitsPerDay - data.DownlinkBitsPerDay) : 0;
            Assert.That(item.Value, Is.EqualTo(expected).Within(1e-9));
            Assert.That(item.Status, Is.EqualTo(BudgetStatus.Yellow));
            Assert.That(r.CanLaunch, Is.True);
        }

        [Test]
        public void At600Km_NoPropulsion_DisposalYellow()
        {
            ReviewResult r = FlightReadinessReview.Review(Catalog(), At600Km(0));
            ReviewItem item = r.Item(ReviewCode.NoDisposalPropellant);
            Assert.That(item.Status, Is.EqualTo(BudgetStatus.Yellow));
            Assert.That(item.Limit, Is.EqualTo(83.476).Within(1e-2));
            Assert.That(item.Value, Is.EqualTo(0));
            Assert.That(item.Detail, Is.EqualTo("8.7"), "8.96 years orbit minus 90 days");
        }

        [Test]
        public void At600Km_ColdGas2Kg_DisposalStillYellow()
        {
            ReviewResult r = FlightReadinessReview.Review(Catalog(), At600Km(2));
            ReviewItem item = r.Item(ReviewCode.NoDisposalPropellant);
            Assert.That(item.Value, Is.EqualTo(65.039 - 0.2464).Within(1e-2), "avoidance takes its share first");
            Assert.That(item.Status, Is.EqualTo(BudgetStatus.Yellow));
            Assert.That(r.Item(ReviewCode.NoAvoidancePropellant).Status, Is.EqualTo(BudgetStatus.Green));
        }

        [Test]
        public void At600Km_ColdGas3Kg_AllGreen()
        {
            ReviewResult r = FlightReadinessReview.Review(Catalog(), At600Km(3));
            Assert.That(r.Item(ReviewCode.NoDisposalPropellant).Value, Is.EqualTo(95.385 - 0.2464).Within(1e-2));
            Assert.That(r.Item(ReviewCode.LaunchMassTooHigh).Value, Is.EqualTo(23.125).Within(1e-9), "still fits into 24 kg");
            Assert.That(r.Items.Select(i => i.Status), Is.All.EqualTo(BudgetStatus.Green),
                string.Join(", ", r.Items.Where(i => i.Status != BudgetStatus.Green).Select(i => i.Code)));
        }

        [Test]
        public void ColdGasHalfKg_AvoidanceGreen()
        {
            MissionDesign d = Good();
            d.PropulsionId = "cold_gas";
            d.PropellantMassKg = 0.5;
            ReviewItem item = FlightReadinessReview.Review(Catalog(), d).Item(ReviewCode.NoAvoidancePropellant);
            Assert.That(item.Status, Is.EqualTo(BudgetStatus.Green));
            Assert.That(item.Value, Is.EqualTo(16.847).Within(1e-2));
            Assert.That(item.Detail, Is.Null);
        }

        [TestCase(600e3, 300e3, 83.476)]
        [TestCase(500e3, 300e3, 56.364)]
        [TestCase(500e3, 500e3, 0)]
        [TestCase(500e3, 600e3, 0)]
        public void PeriapsisLoweringDeltaV_VisViva(double altitudeM, double perigeeM, double expected)
        {
            Assert.That(Maneuvers.PeriapsisLoweringDeltaV(TestHelpers.REarth + altitudeM, TestHelpers.REarth + perigeeM, TestHelpers.MuEarth), Is.EqualTo(expected).Within(1e-2));
        }

        [Test]
        public void MissionLongerThanOrbit_LifetimeYellow()
        {
            // 500 km lasts 1.76 years (642 days); a 800-day mission outlives the orbit
            Catalog c = Catalog(changeMission: m => m.durationDays = 800);
            ReviewResult r = FlightReadinessReview.Review(c, Good());
            ReviewItem item = r.Item(ReviewCode.LifetimeShorterThanMission);
            Assert.That(item.Status, Is.EqualTo(BudgetStatus.Yellow));
            Assert.That(item.Limit, Is.EqualTo(800 * 86400.0).Within(1e-6));
            Assert.That(item.Value, Is.LessThan(item.Limit));
        }

        [Test]
        public void NoScoring_Throws()
        {
            Catalog c = MissionSimulationStartTests.RealCatalog();   // has no scoring.json
            Assert.Throws<InvalidOperationException>(() => FlightReadinessReview.Review(c, Good()));
        }

        [Test]
        public void Thresholds_ComeFromJson()
        {
            Catalog c = Catalog(changeBalance: b => b.Review.MinPowerReserve = 2.0);
            ReviewItem item = FlightReadinessReview.Review(c, Good()).Item(ReviewCode.LowPowerReserve);
            Assert.That(item.Status, Is.EqualTo(BudgetStatus.Yellow), "110 % reserve is below 200 %");
            Assert.That(item.Limit, Is.EqualTo(2.0));
        }

        [Test]
        public void RealBalanceRules_HaveReviewGroup()
        {
            ReviewRules rules = TestHelpers.LoadRealBalanceRules().Review;
            Assert.That(rules, Is.Not.Null);
            Assert.That(rules.MinPowerReserve, Is.EqualTo(0.10));
            Assert.That(rules.AvoidanceDeltaVPerYearMPerS, Is.EqualTo(1.0));
            Assert.That(rules.DisposalPerigeeM, Is.EqualTo(300e3));
        }
    }
}
