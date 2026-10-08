using System.Collections.Generic;
using MissionCore;
using MissionGame;
using UnityEngine;

public class PowerTab : Tab
{
    [SerializeField] private CatalogListView solarCellsList;
    [SerializeField] private CatalogListView batteriesList;
    [SerializeField] private MySlider solarAreaSlider;
    [SerializeField] private Stepper batteryCountStepper;

    new void Start()
    {
        base.Start();

        var (solarCellRows, batteryRows) = MakeRows();

        solarCellsList.RowClicked += OnSolarCellClicked;
        batteriesList.RowClicked += OnBatteryClicked;
        solarCellsList.Build(solarCellRows);
        batteriesList.Build(batteryRows);

        solarAreaSlider.ValueChanged += OnSolarAreaSliderChanged;
        batteryCountStepper.ValueChanged += OnBatteryCountStepperChanged;
    }

    private (List<CatalogRowData> solarCellRows, List<CatalogRowData> batteryRows) MakeRows()
    {
        Catalog catalog = CatalogManager.Instance.Catalog;
        List<CatalogRowData> solarCellRows = new();
        List<CatalogRowData> batteryRows = new();
        MissionDesign design = MissionDesignManager.Instance.Design;

        foreach (SolarCellSpec spec in catalog.SolarCells.Values)
        {
            solarCellRows.Add(new CatalogRowData
            {
                Id = spec.id,
                NameKey = "part." + spec.id,
                DescriptionKey = "part." + spec.id + ".desc",
                Values = new[]
                {
                    Formatter.Value(Metric.Raw, spec.efficiency),
                    Formatter.Value(Metric.MassKg, spec.massPerAreaKgM2),
                    Formatter.Value(Metric.CostUsd, spec.pricePerAreaUSDM2),
                },
                Selected = design.SolarCellId == spec.id,
                Disabled = false
            });
        }
        foreach (BatterySpec spec in catalog.Batteries.Values)
        {
            batteryRows.Add(new CatalogRowData
            {
                Id = spec.id,
                NameKey = "part." + spec.id,
                DescriptionKey = "part." + spec.id + ".desc",
                Values = new[]
                {
                    Formatter.Value(Metric.PowerStorage, spec.energyWh),
                    Formatter.Value(Metric.MassKg, spec.massKg),
                    Formatter.Value(Metric.CostUsd, spec.priceUSD),
                },
                Selected = design.BatteryId == spec.id,
                Disabled = false
            });
        }
        return (solarCellRows, batteryRows);
    }

    public override void OnDesignChanged(DesignReport report, DesignReport previous, List<DesignChange> changes)
    {
        var (solarCellRows, batteryRows) = MakeRows();
        MissionDesign design = MissionDesignManager.Instance.Design;

        solarCellsList.Refresh(solarCellRows);
        batteriesList.Refresh(batteryRows);

        solarAreaSlider.SetValueWithoutNotify((float)design.SolarAreaM2);
        batteryCountStepper.SetValueWithoutNotify(design.BatteryCount);
    }

    private void OnBatteryCountStepperChanged(int value)
    {
        MissionDesignManager.Instance.ModifyDesign(d => d.BatteryCount = value);
    }

    private void OnSolarAreaSliderChanged(double value)
    {
        MissionDesignManager.Instance.ModifyDesign(d => d.SolarAreaM2 = value);
    }

    private void OnSolarCellClicked(string id, bool isOn)
    {
        MissionDesignManager.Instance.ModifyDesign(d => d.SolarCellId = id);
    }

    private void OnBatteryClicked(string id, bool isOn)
    {
        MissionDesignManager.Instance.ModifyDesign(d => d.BatteryId = id);
    }

    new private void OnDestroy()
    {
        base.OnDestroy();

        solarCellsList.RowClicked -= OnSolarCellClicked;
        batteriesList.RowClicked -= OnBatteryClicked;
    }
}
