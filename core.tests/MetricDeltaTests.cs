using NUnit.Framework;

namespace MissionCore.Tests
{
    public class MetricDeltaTests
    {
        [TestCase(Metric.PowerGenerationW)]
        [TestCase(Metric.DeltaVMPerS)]
        [TestCase(Metric.DataDownlinkBitsPerDay)]
        [TestCase(Metric.DataStorageBits)]
        public void HigherIsBetter_True(Metric m) => Assert.That(m.HigherIsBetter(), Is.True);

        [TestCase(Metric.MassKg)]
        [TestCase(Metric.CostUsd)]
        [TestCase(Metric.PowerUseW)]
        [TestCase(Metric.DataGeneratedBitsPerDay)]
        public void HigherIsBetter_False(Metric m) => Assert.That(m.HigherIsBetter(), Is.False);

        [Test]
        public void DataStorage_Increase_IsBetter() =>
            Assert.That(new MetricDelta(Metric.DataStorageBits, 8e9, 16e9).IsBetter, Is.True);

        [Test]
        public void Mass_Increase_IsNotBetter() =>
            Assert.That(new MetricDelta(Metric.MassKg, 100, 120).IsBetter, Is.False);

        [Test]
        public void ZeroDelta_IsNeverBetter()
        {
            Assert.That(new MetricDelta(Metric.MassKg, 5, 5).IsBetter, Is.False);
            Assert.That(new MetricDelta(Metric.DeltaVMPerS, 5, 5).IsBetter, Is.False);
        }

        [Test]
        public void Delta_IsAfterMinusBefore() =>
            Assert.That(new MetricDelta(Metric.CostUsd, 10, 4).Delta, Is.EqualTo(-6));
    }
}
