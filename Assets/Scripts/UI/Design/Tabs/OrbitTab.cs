using System.Collections.Generic;
using MissionCore;
using MissionGame;
using UnityEngine;

public class OrbitTab : Tab
{
    [SerializeField] private CatalogListView orbitPresetList;

    [SerializeField] private MySlider altitudeSlider;
    [SerializeField] private MySlider inclinationSlider;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    new void Start()
    {
        base.Start();

        var rows = MakeRows();

        orbitPresetList.RowClicked += OnOrbitPresetClicked;
        orbitPresetList.Build(rows);

        altitudeSlider.ValueChanged += OnAltitudeSliderChanged;
        inclinationSlider.ValueChanged += OnInclinationSliderChanged;
    }

    new private void OnDestroy()
    {
        base.OnDestroy();
        orbitPresetList.RowClicked -= OnOrbitPresetClicked;
        altitudeSlider.ValueChanged -= OnAltitudeSliderChanged;
        inclinationSlider.ValueChanged -= OnInclinationSliderChanged;
    }

    public override void OnDesignChanged(DesignReport report, DesignReport previous, List<DesignChange> changes)
    {
        List<CatalogRowData> rows = MakeRows();
        MissionDesign design = MissionDesignManager.Instance.Design;

        orbitPresetList.Refresh(rows);
        altitudeSlider.SetValueWithoutNotify((float)design.AltitudeM);
        inclinationSlider.SetValueWithoutNotify((float)design.InclinationDeg);
    }

    private List<CatalogRowData> MakeRows()
    {
        Catalog catalog = CatalogManager.Instance.Catalog;
        List<CatalogRowData> rows = new();
        MissionDesign design = MissionDesignManager.Instance.Design;

        foreach (OrbitPresetSpec spec in catalog.OrbitPresets.Values)
        {
            rows.Add(new CatalogRowData
            {
                Id = spec.id,
                NameKey = "orbit_preset." + spec.id,
                DescriptionKey = "orbit_preset." + spec.id + ".desc",
                Values = new[]
                {
                    Formatter.Value(Metric.AltitudeM, spec.altitudeM),
                    Formatter.Value(Metric.InclinationDeg, spec.inclinationDeg)
                },
                Selected = design.OrbitPreset == spec.id,
                Disabled = false
            });
        }
        return rows;
    }

    private void OnOrbitPresetClicked(string id, bool isOn)
    {
        MissionDesignManager.Instance.ModifyDesign(d =>
        {
            OrbitPresetSpec preset = CatalogManager.Instance.Catalog.OrbitPresets[id];
            d.OrbitPreset = id;
            d.AltitudeM = preset.altitudeM;
            d.InclinationDeg = preset.inclinationDeg;
        });
    }

    private void OnAltitudeSliderChanged(double newValue)
    {
        MissionDesignManager.Instance.ModifyDesign(d => d.AltitudeM = newValue);
    }
    private void OnInclinationSliderChanged(double newValue)
    {
        MissionDesignManager.Instance.ModifyDesign(d => d.InclinationDeg = newValue);
    }
}
