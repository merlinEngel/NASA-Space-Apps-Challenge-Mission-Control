using System.Linq;

namespace MissionCore
{
    public enum Metric { MassKg, CostUsd, PowerUseW, PowerGenerationW, DeltaVMPerS, DataGeneratedBitsPerDay, DataDownlinkBitsPerDay, AltitudeM, InclinationDeg }

    public static class MetricExtensions
    {
        public static bool HigherIsBetter(this Metric self)
        {
            return new Metric[] {
                Metric.PowerGenerationW,
                Metric.DeltaVMPerS,
                Metric.DataDownlinkBitsPerDay
            }.Contains(self);
        }
    }

    public readonly struct MetricDelta
    {
        public MetricDelta(Metric metric, double before, double after)
        {
            Metric = metric;
            Before = before;
            After = after;
        }

        public Metric Metric { get; }
        public double Before { get; }
        public double After { get; }
        public double Delta => After - Before;
        public bool IsBetter => Metric.HigherIsBetter() ? Delta > 0 : Delta < 0;
    }
}