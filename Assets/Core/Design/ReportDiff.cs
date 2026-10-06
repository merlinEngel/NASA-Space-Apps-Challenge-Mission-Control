using System;
using System.Collections.Generic;

namespace MissionCore
{
    public class DesignReportDiff
    {
        const double MetricThreshold = 1e-9;

        public IReadOnlyList<MetricDelta> MetricDeltas { get; private set; }
        public IReadOnlyList<StatusChange> StatusChanges { get; private set; }

        public DesignReportDiff(DesignReport rOld, DesignReport rNew)
        {
            List<MetricDelta> metricDeltas = new();
            List<StatusChange> statusChanges = new();

            MetricDeltas = metricDeltas;
            StatusChanges = statusChanges;

            if(rOld == null)
                return;

            foreach (Metric metric in Enum.GetValues(typeof(Metric)))
            {
                double oldValue = rOld.Value(metric);
                double newValue = rNew.Value(metric);
                double diff = Math.Abs(newValue - oldValue);

                if (diff < MetricThreshold || !double.IsFinite(oldValue) || !double.IsFinite(newValue))
                    continue;
                
                metricDeltas.Add(new(metric, oldValue, newValue));
            }
            foreach(BudgetKind kind in Enum.GetValues(typeof(BudgetKind)))
            {
                BudgetStatus oldStatus = rOld.Statuses[kind];
                BudgetStatus newStatus = rNew.Statuses[kind];
                if(oldStatus == newStatus)
                    continue;
                
                statusChanges.Add(new(kind, oldStatus, newStatus));
            }

            MetricDeltas = metricDeltas;
            StatusChanges = statusChanges;
        }
    }
}