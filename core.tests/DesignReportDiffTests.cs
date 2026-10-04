using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using MissionCore;

namespace MissionCore.Tests
{
    public class DesignReportDiffTests
    {
        // ---------- Real data helpers (same loading as DesignValidatorTests) ----------

        static string DataDir(params string[] parts)
        {
            var dir = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
            while (dir != null && !Directory.Exists(Path.Combine(dir.FullName, "Assets", "StreamingAssets", "Data"))) dir = dir.Parent;
            return Path.Combine(new[] { dir.FullName }.Concat(parts).ToArray());
        }

        static Catalog RealCatalog()
        {
            string data = DataDir("Assets", "StreamingAssets", "Data");
            List<T> L<T>(string f) => CatalogLoader.LoadList<T>(File.ReadAllText(Path.Combine(data, f)), f);
            return new Catalog(L<PropulsionSpec>("propulsion.json"), L<InstrumentSpec>("instruments.json"),
                L<LauncherSpec>("launchers.json"), L<CommsSpec>("comms.json"), L<BatterySpec>("batteries.json"),
                L<PlatformSpec>("platforms.json"), L<GroundStationSpec>("groundstations.json"), L<SolarCellSpec>("solar.json"),
                L<MissionTemplate>("missions.json"),
                TestHelpers.LoadRealBalanceRules(),
                null, null, null);
        }

        static MissionDesign LoadDesign(string file)
        {
            string json = File.ReadAllText(DataDir("core.tests", "Data", "designs", file));
            return CatalogLoader.LoadObject<MissionDesign>(json, file);
        }

        static DesignReport Evaluate(Catalog catalog, MissionDesign design) => DesignValidator.Evaluate(catalog, design);

        static MetricDelta SingleDelta(DesignReportDiff diff, Metric metric)
        {
            var matches = diff.MetricDeltas.Where(d => d.Metric == metric).ToList();
            Assert.That(matches, Has.Count.EqualTo(1),
                $"expected one {metric} delta, got: {string.Join(", ", diff.MetricDeltas.Select(d => d.Metric))}");
            return matches[0];
        }

        // ---------- Null / identical ----------

        [Test]
        public void NullOldReport_GivesEmptyLists()
        {
            var report = Evaluate(RealCatalog(), LoadDesign("techdemo_good.json"));

            DesignReportDiff diff = null;
            Assert.DoesNotThrow(() => diff = new DesignReportDiff(null, report));
            Assert.That(diff.MetricDeltas, Is.Not.Null.And.Empty);
            Assert.That(diff.StatusChanges, Is.Not.Null.And.Empty);
        }

        [Test]
        public void IdenticalReports_GiveEmptyLists()
        {
            var catalog = RealCatalog();
            var a = Evaluate(catalog, LoadDesign("techdemo_good.json"));
            var b = Evaluate(catalog, LoadDesign("techdemo_good.json"));

            var diff = new DesignReportDiff(a, b);

            Assert.That(diff.MetricDeltas, Is.Empty);
            Assert.That(diff.StatusChanges, Is.Empty);
        }

        [Test]
        public void SameReportInstance_GivesEmptyLists()
        {
            var report = Evaluate(RealCatalog(), LoadDesign("techdemo_good.json"));

            var diff = new DesignReportDiff(report, report);

            Assert.That(diff.MetricDeltas, Is.Empty);
            Assert.That(diff.StatusChanges, Is.Empty);
        }

        // ---------- Solar area ----------

        [Test]
        public void DoublingSolarArea_RaisesGenerationAndMass()
        {
            var catalog = RealCatalog();
            var design = LoadDesign("techdemo_good.json");
            var before = Evaluate(catalog, design);
            design.SolarAreaM2 *= 2;
            var after = Evaluate(catalog, design);

            var diff = new DesignReportDiff(before, after);

            var power = SingleDelta(diff, Metric.PowerGenerationW);
            Assert.That(power.Before, Is.EqualTo(before.Value(Metric.PowerGenerationW)));
            Assert.That(power.After, Is.EqualTo(after.Value(Metric.PowerGenerationW)));
            Assert.That(power.Delta, Is.GreaterThan(0));
            Assert.That(power.IsBetter, Is.True);

            var mass = SingleDelta(diff, Metric.MassKg);
            Assert.That(mass.Delta, Is.GreaterThan(0));
            Assert.That(mass.IsBetter, Is.False);
        }

        [Test]
        public void DoublingSolarArea_DoesNotTouchUnrelatedMetrics()
        {
            var catalog = RealCatalog();
            var design = LoadDesign("techdemo_good.json");
            var before = Evaluate(catalog, design);
            design.SolarAreaM2 *= 2;
            var after = Evaluate(catalog, design);

            var diff = new DesignReportDiff(before, after);
            var metrics = diff.MetricDeltas.Select(d => d.Metric).ToList();

            Assert.That(metrics, Does.Not.Contain(Metric.PowerUseW));
            Assert.That(metrics, Does.Not.Contain(Metric.DataGeneratedBitsPerDay));
            Assert.That(metrics, Does.Not.Contain(Metric.DataDownlinkBitsPerDay));
        }

        static DesignReportDiff LowPowerDiff(double areaFactor, bool reverse = false)
        {
            var catalog = RealCatalog();
            var design = LoadDesign("low_power.json");
            var bad = Evaluate(catalog, design);
            design.SolarAreaM2 *= areaFactor;
            var better = Evaluate(catalog, design);
            return reverse ? new DesignReportDiff(better, bad) : new DesignReportDiff(bad, better);
        }

        // With the current data low_power has a power reserve of about -74 %; doubling the area
        // only lifts it to about -49 %, so the status stays Red. x4 reaches Yellow, x8 Green.
        [Test]
        public void LowPower_DoubledSolarArea_StaysRedButGenerationIsBetter()
        {
            var diff = LowPowerDiff(2);

            Assert.That(diff.StatusChanges.Select(c => c.Kind), Does.Not.Contain(BudgetKind.Power));
            Assert.That(SingleDelta(diff, Metric.PowerGenerationW).IsBetter, Is.True);
        }

        [TestCase(4, BudgetStatus.Yellow)]
        [TestCase(8, BudgetStatus.Green)]
        public void LowPower_LargerSolarArea_PowerStatusImproves(double areaFactor, BudgetStatus expected)
        {
            var diff = LowPowerDiff(areaFactor);

            var change = diff.StatusChanges.Single(c => c.Kind == BudgetKind.Power);
            Assert.That(change.From, Is.EqualTo(BudgetStatus.Red));
            Assert.That(change.To, Is.EqualTo(expected));
            Assert.That(change.IsWorse, Is.False);
        }

        [Test]
        public void LowPower_ReverseDirection_StatusChangeIsWorse()
        {
            var diff = LowPowerDiff(8, reverse: true);

            var change = diff.StatusChanges.Single(c => c.Kind == BudgetKind.Power);
            Assert.That(change.From, Is.EqualTo(BudgetStatus.Green));
            Assert.That(change.To, Is.EqualTo(BudgetStatus.Red));
            Assert.That(change.IsWorse, Is.True);
            Assert.That(SingleDelta(diff, Metric.PowerGenerationW).IsBetter, Is.False);
            Assert.That(SingleDelta(diff, Metric.MassKg).IsBetter, Is.True);
        }

        // ---------- Instruments ----------

        [Test]
        public void ExtraInstrument_RaisesPowerUse_NotBetter()
        {
            var catalog = RealCatalog();
            var design = LoadDesign("techdemo_good.json");
            var before = Evaluate(catalog, design);
            design.InstrumentIds = design.InstrumentIds.Concat(new[] { "magnetometer" }).ToList();
            var after = Evaluate(catalog, design);

            var diff = new DesignReportDiff(before, after);

            var use = SingleDelta(diff, Metric.PowerUseW);
            Assert.That(use.Delta, Is.GreaterThan(0));
            Assert.That(use.IsBetter, Is.False);
            Assert.That(SingleDelta(diff, Metric.MassKg).IsBetter, Is.False);
            Assert.That(SingleDelta(diff, Metric.CostUsd).IsBetter, Is.False);
        }

        [Test]
        public void BiggerInstrument_RaisesPowerUse_NotBetter()
        {
            var catalog = RealCatalog();
            var design = LoadDesign("techdemo_good.json");
            var before = Evaluate(catalog, design);
            design.InstrumentIds = new List<string> { "radiometer" };   // 20 W instead of camera_wide's 6 W
            var after = Evaluate(catalog, design);

            var diff = new DesignReportDiff(before, after);

            var use = SingleDelta(diff, Metric.PowerUseW);
            Assert.That(use.Delta, Is.GreaterThan(0));
            Assert.That(use.IsBetter, Is.False);
        }

        // ---------- Threshold ----------

        [Test]
        public void DeltasBelowThreshold_AreNotListed()
        {
            var catalog = RealCatalog();
            var design = LoadDesign("techdemo_good.json");
            var before = Evaluate(catalog, design);
            design.SolarAreaM2 += 1e-15;   // changes generation and mass by far less than 1e-9
            var after = Evaluate(catalog, design);

            var diff = new DesignReportDiff(before, after);

            Assert.That(diff.MetricDeltas, Is.Empty);
        }

        [Test]
        public void DeltasAboveThreshold_AreListed()
        {
            var catalog = RealCatalog();
            var design = LoadDesign("techdemo_good.json");
            var before = Evaluate(catalog, design);
            design.SolarAreaM2 += 1e-3;
            var after = Evaluate(catalog, design);

            var diff = new DesignReportDiff(before, after);

            Assert.That(diff.MetricDeltas.Select(d => d.Metric), Does.Contain(Metric.PowerGenerationW));
            Assert.That(diff.MetricDeltas.All(d => Math.Abs(d.Delta) >= 1e-9), Is.True);
        }

        // ---------- MetricDelta / StatusChange / HigherIsBetter ----------

        [Test]
        public void HigherIsBetter_ExactlyForGenerationDeltaVAndDownlink()
        {
            var expected = new[] { Metric.PowerGenerationW, Metric.DeltaVMPerS, Metric.DataDownlinkBitsPerDay };
            foreach (Metric m in Enum.GetValues(typeof(Metric)))
                Assert.That(m.HigherIsBetter(), Is.EqualTo(expected.Contains(m)), m.ToString());
        }

        [Test]
        public void MetricDelta_IsBetterFollowsDirection()
        {
            Assert.That(new MetricDelta(Metric.PowerGenerationW, 10, 20).IsBetter, Is.True);
            Assert.That(new MetricDelta(Metric.PowerGenerationW, 20, 10).IsBetter, Is.False);
            Assert.That(new MetricDelta(Metric.MassKg, 10, 20).IsBetter, Is.False);
            Assert.That(new MetricDelta(Metric.MassKg, 20, 10).IsBetter, Is.True);
            Assert.That(new MetricDelta(Metric.CostUsd, 5, 5).IsBetter, Is.False);
            Assert.That(new MetricDelta(Metric.DeltaVMPerS, 5, 5).IsBetter, Is.False);
        }

        [Test]
        public void StatusChange_IsWorseWhenStatusGetsHigher()
        {
            Assert.That(new StatusChange(BudgetKind.Power, BudgetStatus.Green, BudgetStatus.Red).IsWorse, Is.True);
            Assert.That(new StatusChange(BudgetKind.Power, BudgetStatus.Green, BudgetStatus.Yellow).IsWorse, Is.True);
            Assert.That(new StatusChange(BudgetKind.Power, BudgetStatus.Red, BudgetStatus.Yellow).IsWorse, Is.False);
            Assert.That(new StatusChange(BudgetKind.Power, BudgetStatus.Red, BudgetStatus.Green).IsWorse, Is.False);
        }
    }
}
