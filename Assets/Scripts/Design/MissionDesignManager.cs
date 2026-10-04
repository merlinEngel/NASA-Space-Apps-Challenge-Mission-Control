using System;
using MissionCore;
using MissionGame;
using UnityEngine;

public class MissionDesignManager : Singleton<MissionDesignManager>
{
    public MissionDesign Design { get; private set; }

    public DesignReport Report { get; private set; }
    public DesignReport PreviousReport { get; private set; }

    public Action<DesignReport, DesignReport> DesignChanged;

    [SerializeField] TextAsset startDesign;

    public void ModifyDesign(Action<MissionDesign> change)
    {
        PreviousReport = Report;
        change(Design);
        
        Report = DesignValidator.Evaluate(CatalogManager.Instance.Catalog, Design);

        DesignChanged?.Invoke(Report, PreviousReport);
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        DesignChanged += (r, pr) => Debug.Log(r.Format(false));

        Design = startDesign != null
        ? CatalogLoader.LoadObject<MissionDesign>(startDesign.text, startDesign.name)
        : new MissionDesign();

        Report = DesignValidator.Evaluate(CatalogManager.Instance.Catalog, Design);
        Debug.Log(Report.Format(false));
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
