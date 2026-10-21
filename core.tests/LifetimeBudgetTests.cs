using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using MissionCore.Budgets;
using MissionCore.Catalogs;
using MissionCore.Design;
using MissionCore.Earth;
using MissionCore.Mission;
using MissionCore.Rules;

namespace MissionCore.Tests
{
    // P5 step 5: sixth traffic light "lifetime"
    public class LifetimeBudgetTests
    {
        const double Day = 86400;

        static Catalog RealCatalog()
        {
            string data = TestHelpers.StreamingDataDir();
            List<T> L<T>(string f) => CatalogLoader.LoadList<T>(File.ReadAllText(Path.Combine(data, f)), f);
            return new Catalog(L<PropulsionSpec>("propulsion.json"), L<InstrumentSpec>("instruments.json"),
                L<LauncherSpec>("launchers.json"), L<CommsSpec>("comms.json"), L<BatterySpec>("batteries.json"),
                L<PlatformSpec>("platforms.json"), L<GroundStationSpec>("groundstations.json"), L<SolarCellSpec>("solar.json"),
                L<MissionTemplate>("missions.json"),
                TestHelpers.LoadRealBalanceRules(),
                null, null, null, new List<OrbitPresetSpec>());
        }

        // core.tests/Data/designs, found next to Assets
        static MissionDesign TechDemo()
        {
            string root = Directory.GetParent(TestHelpers.StreamingDataDir()).Parent.Parent.FullName;
            string file = Path.Combine(root, "core.tests", "Data", "designs", "techdemo_good.json");
            return CatalogLoader.LoadObject<MissionDesign>(File.ReadAllText(file), "techdemo_good.json");
        }

        static DesignReport EvaluateAt(double altitudeM)
        {
            MissionDesign design = TechDemo();
            design.AltitudeM = altitudeM;
            return DesignValidator.Evaluate(RealCatalog(), design);
        }

        // "Fertig, wenn" from p5-schritte.md: 300 km red, 400 km yellow, 500 km green (90-day mission)
        [TestCase(300e3, BudgetStatus.Red)]
        [TestCase(400e3, BudgetStatus.Yellow)]
        [TestCase(500e3, BudgetStatus.Green)]
        public void TechDemo_StatusByAltitude(double altitudeM, BudgetStatus expected)
        {
            DesignReport r = EvaluateAt(altitudeM);
            Assert.That(r.Statuses[BudgetKind.Lifetime], Is.EqualTo(expected),
                $"lifetime {r.LifetimeResult.LifetimeS / Day:0.0} d");
        }

        [Test]
        public void TechDemo_At500Km_LifetimeAndMissionDuration()
        {
            DesignReport r = EvaluateAt(500e3);
            Assert.That(r.LifetimeResult.LifetimeS / Day, Is.EqualTo(641.7).Within(5).Percent);
            Assert.That(r.LifetimeResult.MissionDurationS / Day, Is.EqualTo(90));
            Assert.That(r.LifetimeResult.IsCapped, Is.False);
        }

        [Test]
        public void UsesMassWithoutMargin()
        {
            // A heavier satellite lives longer, so the budget must not use the mass with margin
            MissionDesign design = TechDemo();
            Catalog catalog = RealCatalog();
            var ctx = new BudgetContext(catalog, design);
            ctx.Mass = new MassBudget().Evaluate(ctx);
            var result = new LifetimeBudget().Evaluate(ctx);

            double massKg = ctx.Mass.DryMassKg + ctx.Mass.PropellantMassKg;
            double area = catalog.Platforms[design.PlatformId].dragAreaM2 + design.SolarAreaM2 * 0.5;
            Assert.That(result.LifetimeS, Is.EqualTo(OrbitDecay.LifetimeS(design.AltitudeM, massKg, area)).Within(1e-9).Percent);
        }

        [Test]
        public void LargerSolarPanels_ShorterLifetime()
        {
            MissionDesign small = TechDemo();
            MissionDesign large = TechDemo();
            large.SolarAreaM2 = small.SolarAreaM2 * 2;
            Catalog catalog = RealCatalog();
            double lifeSmall = DesignValidator.Evaluate(catalog, small).LifetimeResult.LifetimeS;
            double lifeLarge = DesignValidator.Evaluate(catalog, large).LifetimeResult.LifetimeS;
            Assert.That(lifeLarge, Is.LessThan(lifeSmall));
        }

        [Test]
        public void WithoutMission_DoesNotCrash()
        {
            MissionDesign design = TechDemo();
            design.MissionId = null;
            DesignReport r = DesignValidator.Evaluate(RealCatalog(), design);
            Assert.That(r.LifetimeResult.MissionDurationS, Is.EqualTo(0));
            Assert.That(r.LifetimeResult.ReserveFraction, Is.EqualTo(1));
        }

        [Test]
        public void WithoutPlatform_IsCapped()
        {
            MissionDesign design = TechDemo();
            design.PlatformId = null;
            DesignReport r = DesignValidator.Evaluate(RealCatalog(), design);
            Assert.That(r.LifetimeResult.IsCapped, Is.True);
        }

        [Test]
        public void ResultProperties()
        {
            var r = new LifetimeBudgetResult(200 * Day, 100 * Day);
            Assert.That(r.ReserveFraction, Is.EqualTo(1.0).Within(1e-12));
            Assert.That(r.Total, Is.EqualTo(200 * Day));
            Assert.That(r.TotalMetric, Is.EqualTo(Metric.LifetimeS));
            Assert.That(new LifetimeBudgetResult(OrbitDecay.MaxLifetimeS, Day).IsCapped, Is.True);
        }

        [Test]
        public void RealBalanceRules_HaveLifetimeThresholds()
        {
            BalanceRules rules = TestHelpers.LoadRealBalanceRules();
            Assert.That(rules.StatusFor(BudgetKind.Lifetime, 1.0), Is.EqualTo(BudgetStatus.Green));
            Assert.That(rules.StatusFor(BudgetKind.Lifetime, 0.5), Is.EqualTo(BudgetStatus.Yellow));
            Assert.That(rules.StatusFor(BudgetKind.Lifetime, -0.1), Is.EqualTo(BudgetStatus.Red));
        }
    }
}
