using System;
using System.Collections.Generic;
using MissionCore;
using MissionGame;
using UnityEngine;

public class Budgets : MonoBehaviour
{
    public Budget massBudget;
    public Budget costBudget;
    public Budget powerBudget;
    public Budget dataBudget;
    public Budget deltaVBudget;

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

        Show(massBudget, report, previous, r => r.MassResult.TotalMassKg, report.MassResult.LimitKg);
        Show(costBudget, report, previous, r => r.CostResult.TotalCostUSD, report.CostResult.BudgetUSD);
        Show(powerBudget, report, previous, r => r.PowerResult.RequiredW, report.PowerResult.AverageGenerationW);
        Show(dataBudget, report, previous, r => r.DataResult.GeneratedBitsPerDay, report.DataResult.DownlinkBitsPerDay);
        Show(deltaVBudget, report, previous, r => r.DeltaVResult.RequiredDeltaVMPerS, report.DeltaVResult.AvailableDeltaVMPerS);
    }

    static void Show(Budget budget, DesignReport report, DesignReport previous, Func<DesignReport, double> need, double limit)
    {
        double value = need(report);
        double delta = previous == null ? 0 : value - need(previous);
        budget.UpdateValue(limit, value, delta);
    }
}
