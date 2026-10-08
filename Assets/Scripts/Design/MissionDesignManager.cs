using System;
using System.Collections.Generic;
using MissionCore;
using MissionGame;
using Newtonsoft.Json;
using UnityEngine;

// Runs after CatalogManager (-100) and before all UI scripts (0),
// so Design and Report already exist when the tabs call Start.
[DefaultExecutionOrder(-50)]
public class MissionDesignManager : Singleton<MissionDesignManager>
{
    public MissionDesign Design { get; private set; }

    public DesignReport Report { get; private set; }
    public DesignReport PreviousReport { get; private set; }

    public event Action<DesignReport, DesignReport, List<DesignChange>> DesignChanged;

    [SerializeField] TextAsset startDesign;

    public void ModifyDesign(Action<MissionDesign> change)
    {
        PreviousReport = Report;
        MissionDesign designBefore = JsonConvert.DeserializeObject<MissionDesign>(JsonConvert.SerializeObject(Design));
        change(Design);

        Report = DesignValidator.Evaluate(CatalogManager.Instance.Catalog, Design);
        DesignChanged?.Invoke(Report, PreviousReport, DesignDifference.Between(designBefore, Design));
    }

    protected override void Awake()
    {
        base.Awake();                  // sets Instance
        if (Instance != this) return;  // duplicate manager, already being destroyed

        Design = startDesign != null
            ? CatalogLoader.LoadObject<MissionDesign>(startDesign.text, startDesign.name)
            : new MissionDesign();

        Report = DesignValidator.Evaluate(CatalogManager.Instance.Catalog, Design);
    }

    void Start()
    {
        // DesignChanged += (r, pr) => Debug.Log(r.Format(false));
        // Debug.Log(Report.Format(false));
    }
}
