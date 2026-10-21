using System.Collections.Generic;
using MissionGame;
using UnityEngine;
using MissionGame.Design;
using MissionCore.Catalogs;
using MissionCore.Design;
using MissionCore.Mission;

namespace MissionGame.UI.Design
{
    public class OrbitTab : Tab
    {
        [SerializeField] private CatalogListView orbitPresetList;

        [SerializeField] private MySlider altitudeSlider;
        [SerializeField] private MySlider inclinationSlider;

        [SerializeField] private OrbitPreview orbitPreview;


        // Start is called once before the first execution of Update after the MonoBehaviour is created
        protected override void Start()
        {
            base.Start();

            var rows = MakeRows();

            orbitPresetList.RowClicked += OnOrbitPresetClicked;
            orbitPresetList.Build(rows);

            altitudeSlider.ValueChanged += OnAltitudeSliderChanged;
            inclinationSlider.ValueChanged += OnInclinationSliderChanged;
        }

        protected override void OnDestroy()
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

            MissionTemplate mission = null;
            if (!string.IsNullOrEmpty(design.MissionId))
                CatalogManager.Instance.Catalog.Missions.TryGetValue(design.MissionId, out mission);
            orbitPreview.Show(design, mission);
        }

        private List<CatalogRowData> MakeRows()
        {
            Catalog catalog = CatalogManager.Instance.Catalog;
            List<CatalogRowData> rows = new();
            MissionDesign design = MissionDesignManager.Instance.Design;
            MissionTemplate mission = null;
            if (!string.IsNullOrEmpty(design.MissionId)) CatalogManager.Instance.Catalog.Missions.TryGetValue(MissionDesignManager.Instance.Design.MissionId, out mission);

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
                        Formatter.Value(Metric.InclinationDeg, spec.EffectiveInclinationDeg)
                    },
                    Selected = design.OrbitPreset == spec.id,
                    Disabled = mission != null && !mission.altitudeM.Contains(spec.altitudeM)
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
                d.InclinationDeg = preset.EffectiveInclinationDeg;
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
}
