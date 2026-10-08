using System.Collections.Generic;
using MissionCore;
using MissionGame;
using UnityEngine;

public class PlatformTab : Tab
{
    [SerializeField] CatalogListView platformList;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    new void Start()
    {
        base.Start();
        
        var rows = MakeRows();

        platformList.RowClicked += OnPlatformClicked;
        platformList.Build(rows);
    }

    private void OnPlatformClicked(string id, bool isOn)
    {
        MissionDesignManager.Instance.ModifyDesign(d => d.PlatformId = id);
    }

    new void OnDestroy()
    {
        base.OnDestroy();
        platformList.RowClicked -= OnPlatformClicked;
    }

    public override void OnDesignChanged(DesignReport pr, DesignReport r, List<DesignChange> c)
    {
        List<CatalogRowData> rows = MakeRows();

        platformList.Refresh(rows);
    }

    private List<CatalogRowData> MakeRows()
    {
        List<CatalogRowData> rows = new();

        Catalog catalog = CatalogManager.Instance.Catalog;
        MissionDesign design = MissionDesignManager.Instance.Design;

        foreach (PlatformSpec spec in catalog.Platforms.Values)
        {
            rows.Add(new CatalogRowData
            {
                Id = spec.id,
                NameKey = "part." + spec.id,
                DescriptionKey = "part." + spec.id + ".desc",
                Values = new[]
                {
                    Formatter.Value(Metric.MassKg, spec.maxMassKg),
                    Formatter.Value(Metric.PowerUseW, spec.busPowerW),
                    Formatter.Value(Metric.DataStorageBits, spec.storageBits),
                    Formatter.Value(Metric.CostUsd, spec.priceUSD),
                },
                Selected = design.PlatformId == spec.id,
                Disabled = false
            });
        }

        return rows;
    }
}
