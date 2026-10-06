using System.Collections;
using System.Collections.Generic;
using MissionCore;
using UnityEngine;
using UnityEngine.UI;

namespace MissionGame.UI
{
    public class InstrumentsTab : MonoBehaviour
    {
        [SerializeField] private CatalogListView cameraList;
        [SerializeField] private CatalogListView othersList;

        private DisplayFormatter formatter;

        [SerializeField] private RectTransform scrollContent;

        private void OnEnable()
        {
            if (formatter != null) OnDesignChanged(null, null);
            StartCoroutine(RebuildLayoutNextFrame());
        }

        private IEnumerator RebuildLayoutNextFrame()
        {
            // Wait one frame so all rows exist and TMP has measured its texts.
            yield return null;
            LayoutRebuilder.ForceRebuildLayoutImmediate(scrollContent);
        }

        private void Start()
        {
            TextTable texts = CatalogManager.Instance.Texts;
            formatter = new DisplayFormatter(DisplayMode.Simple, texts);

            var (cameraRows, otherRows) = MakeRows();

            cameraList.RowClicked += OnCameraClicked;
            cameraList.Build(cameraRows);

            othersList.RowClicked += OnOthersClicked;
            othersList.Build(otherRows);

            MissionDesignManager.Instance.DesignChanged += OnDesignChanged;
        }

        private void OnDestroy()
        {
            if (MissionDesignManager.Instance != null)
                MissionDesignManager.Instance.DesignChanged -= OnDesignChanged;
        }

        private void OnDesignChanged(DesignReport report, DesignReport previous)
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
                    formatter.Value(Metric.MassKg, spec.massKg),
                    formatter.Value(Metric.PowerUseW, spec.powerW),
                    formatter.Value(Metric.DataGeneratedBitsPerDay, bitsPerDay),
                    formatter.Value(Metric.CostUsd, spec.priceUSD),
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
