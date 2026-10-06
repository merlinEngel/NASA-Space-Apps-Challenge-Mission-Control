using System.Collections;
using System.Collections.Generic;
using MissionCore;
using MissionGame;
using UnityEngine;
using UnityEngine.UI;

public class OrbitTab : MonoBehaviour
{
    [SerializeField] private CatalogListView orbitPresetList;

    private DisplayFormatter formatter;

    [SerializeField] private RectTransform scrollContent;

    private IEnumerator RebuildLayoutNextFrame()
    {
        // Wait one frame so all rows exist and TMP has measured its texts.
        yield return null;
        LayoutRebuilder.ForceRebuildLayoutImmediate(scrollContent);
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        TextTable texts = CatalogManager.Instance.Texts;
        formatter = new DisplayFormatter(DisplayMode.Simple, texts);

        var rows = MakeRows();

        orbitPresetList.RowClicked += OnOrbitPresetClicked;
        orbitPresetList.Build(rows);
        
        MissionDesignManager.Instance.DesignChanged += OnDesignChanged;
    }

    private void OnEnable()
    {
        if (formatter != null) OnDesignChanged(null, null);
        StartCoroutine(RebuildLayoutNextFrame());
    }

    private void OnDestroy()
    {
        if (MissionDesignManager.Instance != null)
            MissionDesignManager.Instance.DesignChanged -= OnDesignChanged;
    }

    private void OnDesignChanged(DesignReport report, DesignReport previous)
    {
        List<CatalogRowData> rows = MakeRows();
        orbitPresetList.Refresh(rows);
    }

    private List<CatalogRowData> MakeRows()
    {
        Catalog catalog = CatalogManager.Instance.Catalog;
        List<CatalogRowData> rows = new();

        foreach (OrbitPresetSpec spec in catalog.OrbitPresets.Values)
        {
            MissionDesign design = MissionDesignManager.Instance.Design;

            rows.Add(new CatalogRowData
            {
                Id = spec.id,
                NameKey = "orbit_preset." + spec.id,
                DescriptionKey = "orbit_preset." + spec.id + ".desc",
                Values = new[]
                {
                    formatter.Value(Metric.AltitudeM, spec.altitudeM),
                    formatter.Value(Metric.InclinationDeg, spec.inclinationDeg)
                },
                Selected = design.OrbitPreset == spec.id,
                Disabled = false
            });
            Debug.Log(spec.id + "; " + design.OrbitPreset);
        }
        return rows;
    }

    private void OnOrbitPresetClicked(string id, bool isOn)
    {
        Catalog catalog = CatalogManager.Instance.Catalog;
        MissionDesignManager.Instance.ModifyDesign(d =>
        {
            OrbitPresetSpec preset = CatalogManager.Instance.Catalog.OrbitPresets[id];
            d.OrbitPreset = id;
            d.AltitudeM = preset.altitudeM;
            d.InclinationDeg = preset.inclinationDeg;
        });
    }
}
