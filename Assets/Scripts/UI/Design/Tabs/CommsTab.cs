using System.Collections.Generic;
using System.Linq;
using MissionGame;
using UnityEngine;
using MissionGame.Design;
using MissionCore.Catalogs;
using MissionCore.Design;
using MissionCore.Mission;

namespace MissionGame.UI.Design
{
    public class CommsTab : Tab
    {
        [SerializeField] CatalogListView commsList;
        [SerializeField] CatalogListView groundStationsList;

        public override void OnDesignChanged(DesignReport report, DesignReport previous, List<DesignChange> changes)
        {
            (List<CatalogRowData> transmitterRows, List<CatalogRowData> groundStationsRows) = MakeRows();

            commsList.Refresh(transmitterRows);
            groundStationsList.Refresh(groundStationsRows);
        }

        protected override void Start()
        {
            base.Start();

            (List<CatalogRowData> commsRows, List<CatalogRowData> groundStationsRows) = MakeRows();
            commsList.Build(commsRows);
            groundStationsList.Build(groundStationsRows);

            commsList.RowClicked += OnCommsClicked;
            groundStationsList.RowClicked += OnGroundStationClicked;
        }

        public void OnCommsClicked(string id, bool isOn)
        {
            MissionDesignManager.Instance.ModifyDesign(d => d.CommsId = id);
        }

        public void OnGroundStationClicked(string id, bool isOn)
        {
            MissionDesignManager.Instance.ModifyDesign(d =>
            {
                if (d.GroundStationIds.Contains(id)) d.GroundStationIds.Remove(id);
                else d.GroundStationIds.Add(id);
            });
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();

            commsList.RowClicked -= OnCommsClicked;
            groundStationsList.RowClicked -= OnGroundStationClicked;
        }

        public (List<CatalogRowData>, List<CatalogRowData>) MakeRows()
        {
            List<CatalogRowData> commsRows = new();
            List<CatalogRowData> groundStationsRows = new();

            Catalog catalog = CatalogManager.Instance.Catalog;
            MissionDesign design = MissionDesignManager.Instance.Design;

            foreach (CommsSpec spec in catalog.Comms.Values)
            {
                commsRows.Add(new CatalogRowData
                {
                    Id = spec.id,
                    NameKey = "part." + spec.id,
                    DescriptionKey = "part." + spec.id + ".desc",
                    Values = new[]
                    {
                        Formatter.Value(Metric.DataDownlinkBitsPerDay, spec.dataRateBpS),
                        Formatter.Value(Metric.PowerUseW, spec.powerW),
                        Formatter.Value(Metric.MassKg, spec.massKg),
                        Formatter.Value(Metric.CostUsd, spec.priceUSD),
                    },
                    Selected = design.CommsId == spec.id,
                    Disabled = false
                });
            }
            CommsSpec commsSpec = catalog.Comms.Keys.Contains(design.CommsId) ? catalog.Comms[design.CommsId] : null;
            foreach(GroundStationSpec spec in catalog.GroundStations.Values)
            {
                groundStationsRows.Add(new CatalogRowData
                {
                    Id = spec.id,
                    NameKey = "part." + spec.id,
                    DescriptionKey = "part." + spec.id + ".desc",
                    Values = new[]
                    {
                        string.Join(", ", spec.bands),
                        Formatter.Value(Metric.CostUsd, spec.costPerContactUSD)
                    },
                    Selected = design.GroundStationIds.Contains(spec.id),
                    Disabled = commsSpec != null && !spec.bands.Contains(commsSpec.band)
                });
            }

            return (commsRows, groundStationsRows);
        }
    }
}
