using System;
using System.Collections.Generic;
using UnityEngine;
using MissionGame.Design;
using MissionCore.Budgets;
using MissionCore.Catalogs;
using MissionCore.Design;
using MissionCore.Mission;

namespace MissionGame.UI.Design
{
    public class LaunchTab : Tab
    {
        [SerializeField] CatalogListView launcherList;
        [SerializeField] CatalogListView propulsionList;

        [SerializeField] MySlider propellantSlider;

        public override void OnDesignChanged(DesignReport report, DesignReport previous, List<DesignChange> changes)
        {
            (List<CatalogRowData> launcherRows, List<CatalogRowData> propulsionRows) = MakeRows();

            launcherList.Refresh(launcherRows);
            propulsionList.Refresh(propulsionRows);

            UpdatePropellantSlider();
        }

        protected override void Start()
        {
            base.Start();

            launcherList.RowClicked += OnLauncherClicked;
            propulsionList.RowClicked += OnPropulsionClicked;

            propellantSlider.ValueChanged += OnPropellantSliderChanged;

            (List<CatalogRowData> launcherRows, List<CatalogRowData> propulsionRows) = MakeRows();

            launcherList.Build(launcherRows);
            propulsionList.Build(propulsionRows);

            UpdatePropellantSlider();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();

            launcherList.RowClicked -= OnLauncherClicked;
            propulsionList.RowClicked -= OnPropulsionClicked;

            propellantSlider.ValueChanged -= OnPropellantSliderChanged;
        }

        private void OnLauncherClicked(string id, bool isOn)
        {
            MissionDesignManager.Instance.ModifyDesign(d => d.LauncherId = id);
        }

        private void OnPropulsionClicked(string id, bool isOn)
        {
            // The "no propulsion" row has an empty id.
            MissionDesignManager.Instance.ModifyDesign(d => d.PropulsionId = string.IsNullOrEmpty(id) ? null : id);
        }

        private void OnPropellantSliderChanged(double value)
        {
            MissionDesignManager.Instance.ModifyDesign(d => d.PropellantMassKg = value);
        }

        // Max = mass still free below the mass limit. The propellant itself is left out,
        // so the max does not move while the player drags the slider.
        private void UpdatePropellantSlider()
        {
            MissionDesign design = MissionDesignManager.Instance.Design;
            MassBudgetResult mass = MissionDesignManager.Instance.Report.MassResult;
            bool hasPropulsion = !string.IsNullOrEmpty(design.PropulsionId);

            double limitKg = mass.LimitKg;
            if (double.IsInfinity(limitKg))
            {
                // No launcher chosen yet: fall back to the platform limit.
                limitKg = !string.IsNullOrEmpty(design.PlatformId) && CatalogManager.Instance.Catalog.Platforms.TryGetValue(design.PlatformId, out PlatformSpec platform)
                    ? platform.maxMassKg
                    : 0;
            }

            double freeKg = limitKg - mass.DryMassKg - mass.MarginKg;
            float max = hasPropulsion ? (float)Math.Max(0, freeKg) : 0f;

            // Range first, then the value: the slider works on 0..1 internally.
            propellantSlider.range = new Vector2(0, max);
            propellantSlider.slider.interactable = hasPropulsion && max > 0;
            propellantSlider.SetValueWithoutNotify((float)design.PropellantMassKg);
        }

        private (List<CatalogRowData>, List<CatalogRowData>) MakeRows()
        {
            Catalog catalog = CatalogManager.Instance.Catalog;
            MissionDesign design = MissionDesignManager.Instance.Design;
            // Always the current report, so the first build already shows real prices.
            double massKg = MissionDesignManager.Instance.Report.MassResult.TotalMassKg;

            List<CatalogRowData> launcherRows = new();
            List<CatalogRowData> propulsionRows = new();

            foreach (LauncherSpec spec in catalog.Launchers.Values)
            {
                double? payload = design.LaunchOrbit is LaunchOrbit o ? spec.PayloadKgFor(o) : null;

                launcherRows.Add(new CatalogRowData
                {
                    Id = spec.id,
                    NameKey = "part." + spec.id,
                    DescriptionKey = "part." + spec.id + ".desc",
                    Values = new[]
                    {
                        payload is double kg ? Formatter.Value(Metric.MassKg, kg) : "–",
                        Formatter.Value(Metric.CostUsd, spec.GetLaunchCost(massKg))
                    },
                    Selected = design.LauncherId == spec.id,
                    Disabled = payload == null || massKg > payload, 
                });
            }

            // "No propulsion" once, before the catalog entries.
            propulsionRows.Add(new CatalogRowData
            {
                Id = "",
                NameKey = "propulsion.none",
                DescriptionKey = "propulsion.none.desc",
                Values = new[] { "–", "–", "–" },
                Selected = string.IsNullOrEmpty(design.PropulsionId),
                Disabled = false
            });

            foreach (PropulsionSpec spec in catalog.Propulsion.Values)
            {
                propulsionRows.Add(new CatalogRowData
                {
                    Id = spec.id,
                    NameKey = "part." + spec.id,
                    DescriptionKey = "part." + spec.id + ".desc",
                    Values = new[]
                    {
                        Formatter.Value(Metric.IspS, spec.ispS),
                        Formatter.Value(Metric.MassKg, spec.dryMassKg),
                        Formatter.Value(Metric.CostUsd, spec.priceUsd)
                    },
                    Selected = design.PropulsionId == spec.id,
                    Disabled = false
                });
            }

            return (launcherRows, propulsionRows);
        }
    }
}
