using System;
using System.Collections.Generic;
using MissionCore.Text;

namespace MissionCore.Design
{
    public static class ReviewText
    {
        public static string TitleKey(ReviewCode code) => "review." + code.ToSnakeCase() + ".title";

        public static string Key(ReviewItem item)
        {
            string ending;

            if (item.Detail == "battery") ending = "battery";
            else if (item.Detail == "no_propulsion") ending = "no_propulsion";
            else if (item.Status == Rules.BudgetStatus.Green && item.Limit == 0) ending = "not_needed";
            else ending = item.Status.ToSnakeCase();

            return $"review.{item.Code.ToSnakeCase()}.{ending}";
        }

        public static Metric MetricFor(ReviewCode code)
        {
            return code switch
            {
                ReviewCode.BudgetExceeded => Metric.CostUsd,
                ReviewCode.LaunchMassTooHigh => Metric.MassKg,
                ReviewCode.LifetimeShorterThanMission => Metric.LifetimeS,
                ReviewCode.LowPowerReserve => Metric.Raw,
                ReviewCode.NoAvoidancePropellant => Metric.DeltaVMPerS,
                ReviewCode.NoDisposalPropellant => Metric.DeltaVMPerS,
                ReviewCode.NoGroundStation => Metric.Raw,
                ReviewCode.RequiredInstrumentMissing => Metric.Raw,
                ReviewCode.StorageFillsUp => Metric.Raw,
                _ => throw new ArgumentException()
            };
        }

        // The three text arguments of a row: {0} value, {1} limit, {2} detail
        public static object[] Args(ReviewItem item, DisplayFormatter formatter)
        {
            return new object[] { Format(item.Code, item.Value, formatter), Format(item.Code, item.Limit, formatter), item.Detail ?? "" };
        }

        private static string Format(ReviewCode code, double v, DisplayFormatter formatter)
        {
            if (double.IsNaN(v)) return "–";
            if (double.IsPositiveInfinity(v)) return "∞";
            return code switch
            {
                ReviewCode.LowPowerReserve => formatter.Reserve(v),
                ReviewCode.RequiredInstrumentMissing or ReviewCode.NoGroundStation or ReviewCode.StorageFillsUp => Math.Floor(v).ToString("0", formatter.Culture),
                _ => formatter.Value(MetricFor(code), v)
            };
        }
    }
}