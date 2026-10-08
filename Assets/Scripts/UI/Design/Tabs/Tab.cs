using System.Collections.Generic;
using MissionCore;
using MissionGame;
using UnityEngine;

public abstract class Tab : MonoBehaviour
{
    public DisplayFormatter Formatter => CatalogManager.Instance.Formatter;

    public void OnEnable()
    {
        if (Formatter != null) OnDesignChanged(null, null, null);
    }
    public void Start()
    {
        if (MissionDesignManager.Instance != null)
            MissionDesignManager.Instance.DesignChanged += OnDesignChanged;
    }
    public void OnDestroy()
    {
        if (MissionDesignManager.Instance != null)
            MissionDesignManager.Instance.DesignChanged -= OnDesignChanged;
    }

    public abstract void OnDesignChanged(DesignReport report, DesignReport previous, List<DesignChange> changes);
}