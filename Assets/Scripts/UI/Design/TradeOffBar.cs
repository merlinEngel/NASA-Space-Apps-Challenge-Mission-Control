using System;
using System.Collections.Generic;
using System.Linq;
using MissionCore;
using MissionGame;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;

public class TradeOffBar : MonoBehaviour
{
    [SerializeField] TMP_Text changedValueText;
    [SerializeField] List<DeltaChip> deltaChips;
    [SerializeField] RectTransform content;

    void Start()
    {
        if (MissionDesignManager.Instance != null)
            MissionDesignManager.Instance.DesignChanged += OnDesignChanged;
        
        content.gameObject.SetActive(false);
    }

    void OnDesignChanged(DesignReport current, DesignReport previous, List<DesignChange> changes)
    {
        if(changes == null || changes.Count <= 0)
        {
            content.gameObject.SetActive(false);
        }

        Dictionary<BudgetKind, MetricDelta> deltas = new()
        {
            [BudgetKind.Mass] = new(current.MassResult.TotalMetric, current.MassResult.Total, previous.MassResult.Total),
            [BudgetKind.Data] = new(current.DataResult.TotalMetric, current.DataResult.Total, previous.DataResult.Total),
            [BudgetKind.Power] = new(current.PowerResult.TotalMetric, current.PowerResult.Total, previous.PowerResult.Total),
            [BudgetKind.Cost] = new(current.CostResult.TotalMetric, current.CostResult.Total, previous.CostResult.Total),
            [BudgetKind.DeltaV] = new(current.DeltaVResult.TotalMetric, current.DeltaVResult.Total, previous.DeltaVResult.Total),
        };
        changedValueText.text = changes[0].Format(CatalogManager.Instance.Texts, CatalogManager.Instance.Formatter);
        content.gameObject.SetActive(true);
        UpdateDeltaChips(deltas);
    }

    void UpdateDeltaChips(Dictionary<BudgetKind, MetricDelta> deltas)
    {
        foreach ((BudgetKind kind, MetricDelta delta) in deltas)
        {
            DeltaChip chip = GetDeltaChip(kind);
            if (chip == null) continue;

            chip.UpdateDelta(delta);
        }
    }

    DeltaChip GetDeltaChip(BudgetKind kind)
    {
        return deltaChips.FirstOrDefault(dc => dc.kind == kind);
    }

    void OnDestroy()
    {
        if (MissionDesignManager.Instance != null)
            MissionDesignManager.Instance.DesignChanged -= OnDesignChanged;
    }
}
