using System.Collections.Generic;
using MissionGame;
using UnityEngine;
using MissionGame.Design;
using MissionCore.Design;
using MissionCore.Text;

namespace MissionGame.UI.Design
{
    public abstract class Tab : MonoBehaviour
    {
        public DisplayFormatter Formatter => CatalogManager.Instance.Formatter;

        protected virtual void OnEnable()
        {
            if (Formatter != null) OnDesignChanged(null, null, null);
        }
        protected virtual void Start()
        {
            if (MissionDesignManager.Instance != null)
                MissionDesignManager.Instance.DesignChanged += OnDesignChanged;
            CatalogManager.Instance.DisplayModeChanged += Redraw;
        }

        protected virtual void OnDestroy()
        {
            if (MissionDesignManager.Instance != null)
                MissionDesignManager.Instance.DesignChanged -= OnDesignChanged;
            if (CatalogManager.Instance != null)
                CatalogManager.Instance.DisplayModeChanged -= Redraw;
        }

        void Redraw() => OnDesignChanged(MissionDesignManager.Instance.Report, null, null);

        public abstract void OnDesignChanged(DesignReport report, DesignReport previous, List<DesignChange> changes);
    }
}
