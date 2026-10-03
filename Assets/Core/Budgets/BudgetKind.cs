using System.Collections.Generic;

namespace MissionCore
{
    public enum BudgetKind
    {
        Data,
        Power,
        Cost,
        DeltaV,
        Mass
    }

    public static class BudgetKindExtensions
    {
        public static StatusThreshold Threshold(this BudgetKind kind, Dictionary<string, StatusThreshold> thresholds) =>
            thresholds.TryGetValue(kind.ToSnakeCase(), out StatusThreshold threshold) ? threshold : null;
    }
}