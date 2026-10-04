using System;
using System.Globalization;
using System.Linq;

namespace MissionCore
{
    public enum DisplayMode { Simple, Realistic }

    public class DisplayFormatter
    {
        public DisplayMode Mode { get; private set; }
        public CultureInfo Culture => texts.Culture;
        private TextTable texts;

        public DisplayFormatter(DisplayMode mode, TextTable texts)
        {
            Mode = mode;
            this.texts = texts;
        }

        public string Value(Metric m, double v) => m switch
        {
            Metric.DataDownlinkBitsPerDay or Metric.DataGeneratedBitsPerDay => !double.IsFinite(v) ? "–" : Format(v, m) + texts.Get("unit.per_day"),
            _ => Format(v, m)
        };

        public string Reserve(double fraction) =>
            double.IsFinite(fraction) ? (fraction * 100).ToString(Mode == DisplayMode.Simple ? "+0;-0;0" : "+0.0;-0.0;0.0", Culture) + " %" : "–";

        public string Delta(MetricDelta delta)
        {
            return (delta.Delta > 0 ? "+" : "") + Value(delta.Metric, delta.Delta);
        }

        (Unit unit, int decimals) Pick(double v, Unit[] units)
        {
            double threshold = 1;
            Unit unit = units.OrderByDescending(u => u.Factor).FirstOrDefault(u => Math.Abs(v) >= u.Factor * threshold) ?? units.OrderBy(u => u.Factor).First();
            return (unit, Mode == DisplayMode.Simple ? unit.SimpleDecimalPlaces : unit.RealisticDecimalPlaces);
        }

        public bool RoundsToZero(MetricDelta d)
        {
            var (unit, decimals) = Pick(d.Delta, UnitsFor(d.Metric));
            return Math.Round(d.Delta / unit.Factor, decimals) == 0;
        }

        static Unit[] UnitsFor(Metric m) => m switch
        {
            Metric.CostUsd => Unit.MoneyUnits,
            Metric.DataDownlinkBitsPerDay or Metric.DataGeneratedBitsPerDay => Unit.DataStorageUnits,
            Metric.DeltaVMPerS => Unit.DeltaVMUnits,
            Metric.MassKg => Unit.MassUnits,
            Metric.PowerGenerationW or Metric.PowerUseW => Unit.PowerUnits,
            _ => throw new ArgumentOutOfRangeException(nameof(m), m, null)
        };


        string Format(double v, Metric m)
        {
            if (!double.IsFinite(v)) return "–";

            (Unit unit, int decimals) = Pick(v, UnitsFor(m));

            double rounded = Math.Round(v / unit.Factor, decimals, MidpointRounding.AwayFromZero);
            if (rounded == 0) rounded = 0;
            return rounded.ToString("F" + decimals, Culture) + " " + unit.Name;
        }
    }
}