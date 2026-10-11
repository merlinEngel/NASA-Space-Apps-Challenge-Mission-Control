using System;
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
    public class PlatformTab : Tab
    {
        [SerializeField] CatalogListView platformList;

        // Start is called once before the first execution of Update after the MonoBehaviour is created
        protected override void Start()
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

        protected override void OnDestroy()
        {
            base.OnDestroy();
            platformList.RowClicked -= OnPlatformClicked;
        }

        public override void OnDesignChanged(DesignReport r, DesignReport pr, List<DesignChange> c)
        {
            List<CatalogRowData> rows = MakeRows();

            platformList.Refresh(rows);
        }

        private List<CatalogRowData> MakeRows()
        {
            List<CatalogRowData> rows = new();

            Catalog catalog = CatalogManager.Instance.Catalog;
            MissionDesign design = MissionDesignManager.Instance.Design;
            MissionTemplate mission = catalog.Missions[MissionDesignManager.Instance.Design.MissionId];

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
                    Disabled = IsRowDisabled(spec, design, catalog, mission)
                });
            }

            return rows;
        }

        private bool IsRowDisabled(PlatformSpec spec, MissionDesign design, Catalog catalog, MissionTemplate mission)
        {
            bool lifetimeToShort = spec.lifetimeYears < mission.DurationYears;
            bool isRideshare = !string.IsNullOrEmpty(design.LauncherId) && catalog.Launchers[design.LauncherId].IsRideshare;

            LauncherSpec launcher = catalog.Launchers.Keys.Contains(design.LauncherId) ? catalog.Launchers[design.LauncherId] : null;

            bool massToBig = isRideshare && spec.maxMassKg > launcher.maxPayloadPerCustomerKg;

            return lifetimeToShort || massToBig;
        }
    }
}
