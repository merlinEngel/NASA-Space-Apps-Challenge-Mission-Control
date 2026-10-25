using System;
using System.Collections.Generic;
using UnityEngine;
using MissionGame.Design;
using MissionCore.Design;
using MissionCore.Budgets;

namespace MissionGame.UI.Design
{
    public class Budgets : MonoBehaviour
    {
        public Budget massBudget;
        public Budget costBudget;
        public Budget powerBudget;
        public Budget dataBudget;
        public Budget deltaVBudget;
        public Budget lifetimeBudget;

        DesignReport lastReport, lastPrevious;

        void Start()
        {
            MissionDesignManager.Instance.DesignChanged += OnDesignChanged;
            CatalogManager.Instance.DisplayModeChanged += Redraw;
            OnDesignChanged(null, null, null);
        }

        void OnDestroy()
        {
            if (MissionDesignManager.Instance != null) MissionDesignManager.Instance.DesignChanged -= OnDesignChanged;
            if (CatalogManager.Instance != null) CatalogManager.Instance.DisplayModeChanged -= Redraw;
        }

        void Redraw() => OnDesignChanged(lastReport, lastPrevious, null);

        public void OnDesignChanged(DesignReport report, DesignReport previous, List<DesignChange> changes)
        {
            report ??= MissionDesignManager.Instance.Report;
            if (report == null) return;
            lastReport = report;
            lastPrevious = previous;

            Show(costBudget, report, BudgetKind.Cost, previous, r => r.CostResult.TotalCostUSD, report.CostResult.BudgetUSD);
            Show(massBudget, report, BudgetKind.Mass, previous, r => r.MassResult.TotalMassKg, report.MassResult.LimitKg);
            Show(powerBudget, report, BudgetKind.Power, previous, r => r.PowerResult.RequiredW, report.PowerResult.AverageGenerationW);
            Show(dataBudget, report, BudgetKind.Data, previous, r => r.DataResult.GeneratedBitsPerDay, report.DataResult.DownlinkBitsPerDay);
            Show(deltaVBudget, report, BudgetKind.DeltaV, previous, r => r.DeltaVResult.RequiredDeltaVMPerS, report.DeltaVResult.AvailableDeltaVMPerS);
            Show(lifetimeBudget, report, BudgetKind.Lifetime, previous, r => r.LifetimeResult.MissionDurationS, report.LifetimeResult.LifetimeS);
        }

        static void Show(Budget budget, DesignReport report, BudgetKind kind, DesignReport previous, Func<DesignReport, double> need, double limit)
        {
            double value = need(report);
            double delta = previous == null ? 0 : value - need(previous);
            budget.UpdateValue(limit, value, delta, report.Statuses[kind]);
        }
    }
}
