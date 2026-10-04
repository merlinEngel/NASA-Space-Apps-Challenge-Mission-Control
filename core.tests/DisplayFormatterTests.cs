using System;
using System.Collections.Generic;
using NUnit.Framework;
using MissionCore;

namespace MissionCore.Tests
{
    // Expected strings are derived from the unit tables in Assets/Core/Math/Units.cs:
    //   Power: W (1, 0/2 decimals), kW (1000, 1/2)
    //   Money: USD (1, 0/2), M USD (1e6, 1/2)
    //   Delta-v: m/s (1, 0/0)
    //   Data: b (1, 0/0), B (8, 0/0), kB (8e3, 1/2), MB (8e6, 1/2), GB (8e9, 1/2), TB (8e12, 1/2)
    //   Mass: kg (1, 1/2), t (1000, 2/3)
    // (decimals given as Simple/Realistic)
    public class DisplayFormatterTests
    {
        const string Dash = "–";

        static TextTable Texts(string language)
        {
            var table = new TextTable(new Dictionary<string, Dictionary<string, string>>
            {
                ["unit.per_day"] = new Dictionary<string, string> { ["de"] = "/Tag", ["en"] = "/d" }
            });
            table.Language = language;
            return table;
        }

        static DisplayFormatter En(DisplayMode mode = DisplayMode.Simple) => new DisplayFormatter(mode, Texts("en"));
        static DisplayFormatter De(DisplayMode mode = DisplayMode.Simple) => new DisplayFormatter(mode, Texts("de"));

        static MetricDelta D(Metric m, double delta) => new MetricDelta(m, 0, delta);

        // ---------- Unit selection ----------

        [Test]
        public void SmallPower_StaysInWatts() =>
            Assert.That(En().Value(Metric.PowerUseW, 50), Is.EqualTo("50 W"));

        [Test]
        public void LargePower_SwitchesToKilowatts() =>
            Assert.That(En().Value(Metric.PowerUseW, 1500), Is.EqualTo("1.5 kW"));

        [Test]
        public void ExactThreshold_PicksLargerUnit() =>
            Assert.That(En().Value(Metric.PowerGenerationW, 1000), Is.EqualTo("1.0 kW"));

        [Test]
        public void ValueBelowSmallestUnit_FallsBackToSmallestUnit() =>
            Assert.That(En(DisplayMode.Realistic).Value(Metric.PowerUseW, 0.5), Is.EqualTo("0.50 W"));

        [Test]
        public void Zero_UsesSmallestUnit() =>
            Assert.That(En().Value(Metric.PowerUseW, 0), Is.EqualTo("0 W"));

        [Test]
        public void NegativeValue_PicksUnitByAbsoluteValue()
        {
            Assert.That(En().Value(Metric.PowerUseW, -1500), Is.EqualTo("-1.5 kW"));
            Assert.That(En().Value(Metric.PowerUseW, -50), Is.EqualTo("-50 W"));
        }

        [Test]
        public void Money_SwitchesToMillions()
        {
            Assert.That(En().Value(Metric.CostUsd, 999_999), Is.EqualTo("999999 USD"));
            Assert.That(En().Value(Metric.CostUsd, 2_500_000), Is.EqualTo("2.5 M USD"));
        }

        [Test]
        public void Mass_SwitchesToTonnes()
        {
            Assert.That(En().Value(Metric.MassKg, 12.34), Is.EqualTo("12.3 kg"));
            Assert.That(En().Value(Metric.MassKg, 1234), Is.EqualTo("1.23 t"));
        }

        [Test]
        public void DeltaV_HasNoDecimalsInBothModes()
        {
            Assert.That(En().Value(Metric.DeltaVMPerS, 1234.6), Is.EqualTo("1235 m/s"));
            Assert.That(En(DisplayMode.Realistic).Value(Metric.DeltaVMPerS, 1234.6), Is.EqualTo("1235 m/s"));
        }

        // ---------- Simple vs Realistic decimals ----------

        [Test]
        public void Realistic_ShowsMoreDecimalsThanSimple()
        {
            Assert.That(En(DisplayMode.Simple).Value(Metric.PowerUseW, 1234), Is.EqualTo("1.2 kW"));
            Assert.That(En(DisplayMode.Realistic).Value(Metric.PowerUseW, 1234), Is.EqualTo("1.23 kW"));
            Assert.That(En(DisplayMode.Simple).Value(Metric.PowerUseW, 50), Is.EqualTo("50 W"));
            Assert.That(En(DisplayMode.Realistic).Value(Metric.PowerUseW, 50), Is.EqualTo("50.00 W"));
            Assert.That(En(DisplayMode.Realistic).Value(Metric.MassKg, 1234.6), Is.EqualTo("1.235 t"));
        }

        [Test]
        public void ModeProperty_ReflectsConstructorArgument()
        {
            Assert.That(En(DisplayMode.Simple).Mode, Is.EqualTo(DisplayMode.Simple));
            Assert.That(En(DisplayMode.Realistic).Mode, Is.EqualTo(DisplayMode.Realistic));
        }

        // ---------- Culture ----------

        [Test]
        public void German_UsesDecimalComma()
        {
            Assert.That(De().Value(Metric.PowerUseW, 1500), Is.EqualTo("1,5 kW"));
            Assert.That(De(DisplayMode.Realistic).Value(Metric.MassKg, 12.5), Is.EqualTo("12,50 kg"));
        }

        [Test]
        public void English_UsesDecimalPoint() =>
            Assert.That(En(DisplayMode.Realistic).Value(Metric.MassKg, 12.5), Is.EqualTo("12.50 kg"));

        [Test]
        public void Culture_FollowsTextTableLanguage()
        {
            Assert.That(En().Culture.Name, Is.EqualTo("en-US"));
            Assert.That(De().Culture.Name, Is.EqualTo("de-DE"));
        }

        // ---------- Data metrics ----------

        [Test]
        public void DataMetric_UsesBytesAndPerDaySuffix()
        {
            Assert.That(En().Value(Metric.DataDownlinkBitsPerDay, 8e9), Is.EqualTo("1.0 GB/d"));
            Assert.That(En().Value(Metric.DataGeneratedBitsPerDay, 8e9), Is.EqualTo("1.0 GB/d"));
            Assert.That(En(DisplayMode.Realistic).Value(Metric.DataDownlinkBitsPerDay, 8e9), Is.EqualTo("1.00 GB/d"));
        }

        [Test]
        public void DataMetric_GermanSuffix() =>
            Assert.That(De().Value(Metric.DataDownlinkBitsPerDay, 1.2e10), Is.EqualTo("1,5 GB/Tag"));

        [Test]
        public void DataMetric_SmallValues_UseBitsAndBytes()
        {
            Assert.That(En().Value(Metric.DataGeneratedBitsPerDay, 4), Is.EqualTo("4 b/d"));
            Assert.That(En().Value(Metric.DataGeneratedBitsPerDay, 16), Is.EqualTo("2 B/d"));
            Assert.That(En().Value(Metric.DataGeneratedBitsPerDay, 8e6), Is.EqualTo("1.0 MB/d"));
            Assert.That(En().Value(Metric.DataGeneratedBitsPerDay, 1.6e13), Is.EqualTo("2.0 TB/d"));
        }

        [Test]
        public void NonDataMetric_HasNoPerDaySuffix() =>
            Assert.That(En().Value(Metric.PowerUseW, 8e9), Does.Not.Contain("/d"));

        // ---------- NaN / Infinity ----------

        [TestCase(double.NaN)]
        [TestCase(double.PositiveInfinity)]
        [TestCase(double.NegativeInfinity)]
        public void NonFinite_GivesDash(double v)
        {
            Assert.That(En().Value(Metric.PowerUseW, v), Is.EqualTo(Dash));
            Assert.That(En().Value(Metric.MassKg, v), Is.EqualTo(Dash));
            Assert.That(En().Value(Metric.CostUsd, v), Is.EqualTo(Dash));
        }

        [Test]
        public void NonFinite_DataMetric_GivesPlainDash() =>
            Assert.That(En().Value(Metric.DataDownlinkBitsPerDay, double.NaN), Is.EqualTo(Dash));

        // ---------- Delta ----------

        [Test]
        public void Delta_Positive_HasPlusSign()
        {
            Assert.That(En().Delta(D(Metric.PowerUseW, 1500)), Is.EqualTo("+1.5 kW"));
            Assert.That(En().Delta(D(Metric.DataDownlinkBitsPerDay, 8e9)), Is.EqualTo("+1.0 GB/d"));
        }

        [Test]
        public void Delta_Negative_HasMinusSign()
        {
            Assert.That(En().Delta(D(Metric.PowerUseW, -1500)), Is.EqualTo("-1.5 kW"));
            Assert.That(En().Delta(D(Metric.MassKg, -12.34)), Is.EqualTo("-12.3 kg"));
        }

        [Test]
        public void Delta_Zero_HasPlusSign() =>
            Assert.That(En().Delta(D(Metric.PowerUseW, 0)), Is.EqualTo("+0 W"));

        [Test]
        public void Delta_UsesBeforeAndAfter() =>
            Assert.That(En().Delta(new MetricDelta(Metric.PowerUseW, 2000, 500)), Is.EqualTo("-1.5 kW"));

        [Test]
        public void Delta_NaN_GivesDash() =>
            Assert.That(En().Delta(D(Metric.PowerUseW, double.NaN)), Is.EqualTo(Dash));

        [Test]
        public void Delta_German_UsesComma() =>
            Assert.That(De().Delta(D(Metric.PowerUseW, 1500)), Is.EqualTo("+1,5 kW"));

        [Test]
        public void Delta_TinyNegative_DoesNotShowMinusZero() =>
            Assert.That(En().Delta(D(Metric.PowerUseW, -0.4)), Does.Not.StartWith("-0 "));

        // ---------- RoundsToZero ----------

        [Test]
        public void RoundsToZero_TinyDelta_DependsOnMode()
        {
            // Simple: W has 0 decimals → 0.4 W rounds to "0"; Realistic: 2 decimals → "0.40"
            Assert.That(En(DisplayMode.Simple).RoundsToZero(D(Metric.PowerUseW, 0.4)), Is.True);
            Assert.That(En(DisplayMode.Realistic).RoundsToZero(D(Metric.PowerUseW, 0.4)), Is.False);
            Assert.That(En(DisplayMode.Realistic).RoundsToZero(D(Metric.PowerUseW, 0.004)), Is.True);
        }

        [Test]
        public void RoundsToZero_NegativeTinyDelta_IsTrue() =>
            Assert.That(En().RoundsToZero(D(Metric.PowerUseW, -0.4)), Is.True);

        [Test]
        public void RoundsToZero_VisibleDelta_IsFalse()
        {
            Assert.That(En().RoundsToZero(D(Metric.PowerUseW, 1)), Is.False);
            Assert.That(En().RoundsToZero(D(Metric.DataDownlinkBitsPerDay, 8e9)), Is.False);
        }

        [Test]
        public void RoundsToZero_ZeroDelta_IsTrue() =>
            Assert.That(En().RoundsToZero(D(Metric.MassKg, 0)), Is.True);

        // ---------- Reserve ----------

        [Test]
        public void Reserve_Simple_RoundsToWholePercent()
        {
            Assert.That(En().Reserve(0.123), Is.EqualTo("+12 %"));
            Assert.That(En().Reserve(-0.05), Is.EqualTo("-5 %"));
        }

        [Test]
        public void Reserve_Realistic_HasOneDecimal()
        {
            Assert.That(En(DisplayMode.Realistic).Reserve(0.1256), Is.EqualTo("+12.6 %"));
            Assert.That(En(DisplayMode.Realistic).Reserve(-0.05), Is.EqualTo("-5.0 %"));
        }

        [Test]
        public void Reserve_Zero_HasNoSign()
        {
            Assert.That(En().Reserve(0), Is.EqualTo("0 %"));
            Assert.That(En(DisplayMode.Realistic).Reserve(0), Is.EqualTo("0.0 %"));
        }

        [Test]
        public void Reserve_RoundingToZero_HasNoSign()
        {
            Assert.That(En().Reserve(0.001), Is.EqualTo("0 %"));
            Assert.That(En().Reserve(-0.001), Is.EqualTo("0 %"));
        }

        [Test]
        public void Reserve_German_UsesComma() =>
            Assert.That(De(DisplayMode.Realistic).Reserve(0.123), Is.EqualTo("+12,3 %"));

        [TestCase(double.NaN)]
        [TestCase(double.PositiveInfinity)]
        [TestCase(double.NegativeInfinity)]
        public void Reserve_NonFinite_GivesDash(double v) =>
            Assert.That(En().Reserve(v), Is.EqualTo(Dash));

        // ---------- Unknown metric ----------

        [Test]
        public void UnknownMetric_Throws()
        {
            var unknown = (Metric)999;
            Assert.Throws<ArgumentOutOfRangeException>(() => En().Value(unknown, 1));
            Assert.Throws<ArgumentOutOfRangeException>(() => En().Delta(D(unknown, 1)));
            Assert.Throws<ArgumentOutOfRangeException>(() => En().RoundsToZero(D(unknown, 1)));
        }
    }
}
