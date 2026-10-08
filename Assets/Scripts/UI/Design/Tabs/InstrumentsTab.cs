using System.Collections.Generic;
using MissionCore;
using UnityEngine;

namespace MissionGame.UI
{
    public class InstrumentsTab : Tab
    {
        [SerializeField] private CatalogListView cameraList;
        [SerializeField] private CatalogListView othersList;

        new void Start()
        {
            var (cameraRows, otherRows) = MakeRows();

            cameraList.RowClicked += OnCameraClicked;
            cameraList.Build(cameraRows);

            othersList.RowClicked += OnOthersClicked;
            othersList.Build(otherRows);
        }

        public override void OnDesignChanged(DesignReport report, DesignReport previous, List<DesignChange> changes)
        {
            var (cameraRows, otherRows) = MakeRows();
            cameraList.Refresh(cameraRows);
            othersList.Refresh(otherRows);
        }

        private (List<CatalogRowData> cameraRows, List<CatalogRowData> otherRows) MakeRows()
        {
            Catalog catalog = CatalogManager.Instance.Catalog;
            var cameraRows = new List<CatalogRowData>();
            var otherRows = new List<CatalogRowData>();

            foreach (InstrumentSpec spec in catalog.Instruments.Values)
            {
                CatalogRowData row = MakeRow(spec);
                (spec.category switch
                {
                    InstrumentCategory.Camera => cameraRows,
                    _ => otherRows
                }).Add(row);
            }

            return (cameraRows, otherRows);
        }

        private CatalogRowData MakeRow(InstrumentSpec spec)
        {
            MissionDesign design = MissionDesignManager.Instance.Design;

            double bitsPerDay = spec.dataRateBpS * spec.dutyCycle * 86400;

            return new CatalogRowData
            {
                Id = spec.id,
                NameKey = "part." + spec.id,
                DescriptionKey = "part." + spec.id + ".desc",
                Values = new[]
                {
                    Formatter.Value(Metric.MassKg, spec.massKg),
                    Formatter.Value(Metric.PowerUseW, spec.powerW),
                    Formatter.Value(Metric.DataGeneratedBitsPerDay, bitsPerDay),
                    Formatter.Value(Metric.CostUsd, spec.priceUSD),
                },
                Selected = design.InstrumentIds.Contains(spec.id),
                Disabled = false,
            };
        }

        private void OnCameraClicked(string id, bool isOn)
        {
            Catalog catalog = CatalogManager.Instance.Catalog;
            MissionDesignManager.Instance.ModifyDesign(d =>
            {
                // Only one camera: remove every camera, then add the chosen one.
                d.InstrumentIds.RemoveAll(i =>
                    catalog.Instruments.TryGetValue(i, out InstrumentSpec s) && s.category == InstrumentCategory.Camera);
                d.InstrumentIds.Add(id);
            });
        }

        private void OnOthersClicked(string id, bool isOn)
        {
            MissionDesignManager.Instance.ModifyDesign(d =>
            {
                // Remove first so the id is never listed twice.
                d.InstrumentIds.Remove(id);
                if (isOn) d.InstrumentIds.Add(id);
            });
        }
    }
}
