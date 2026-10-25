using System;
using System.Collections.Generic;
using System.Linq;
using MissionCore.Budgets;
using MissionCore.Catalogs;
using MissionCore.Design;
using MissionCore.Mission;
using MissionCore.Text;
using MissionGame;
using MissionGame.Design;
using MissionGame.UI;
using NaughtyAttributes;
using TMPro;
using UnityEngine;

public class FlightReadinessReviewUIManager : MonoBehaviour
{
    [Header("Top")]
    [SerializeField] TMP_Text missionDescription;
    [SerializeField] TMP_Text modeText;

    [Header("Summary")]
    [SerializeField] TMP_Text okCount;
    [SerializeField] TMP_Text riskCount;
    [SerializeField] TMP_Text blockerCount;
    [SerializeField] TMP_Text launcherNameText;
    [SerializeField] TMP_Text reliabilityText;
    [SerializeField] MyButton backToDesignButton;
    [SerializeField] MyButton launchButton;

    [HorizontalLine]
    [Foldout("Blockers")] [SerializeField] FRRRow launchMassRow; 
    [Foldout("Blockers")] [SerializeField] FRRRow budgetRow;
    [Foldout("Blockers")] [SerializeField] FRRRow requiredInstrumentsRow;
    [Foldout("Blockers")] [SerializeField] FRRRow groundStationRow;

    [HorizontalLine]
    [Foldout("Risks")] [SerializeField] FRRRow lowPowerRow;
    [Foldout("Risks")] [SerializeField] FRRRow storageFillsUpRow;
    [Foldout("Risks")] [SerializeField] FRRRow avoidancePropellantRow;
    [Foldout("Risks")] [SerializeField] FRRRow disposalPropellantRow;
    [Foldout("Risks")] [SerializeField] FRRRow lifetimeRow;


    MissionDesign Design => MissionSession.Instance.Design;
    Catalog Catalog => CatalogManager.Instance.Catalog;
    TextTable Texts => CatalogManager.Instance.Texts;
    DisplayFormatter Formatter => CatalogManager.Instance.Formatter;
    MissionSession Session => MissionSession.Instance;

    void Start()
    {
        if (MissionSession.Instance?.Review == null)
        {
            Screens.LoadScreen(MissionGame.Design.Screen.Design);
            return;
        }

        missionDescription.text = GetDescription(Catalog.Missions[Design.MissionId]);
        modeText.text = Texts.Get(Session.Mode.TextKey());

        UpdateBlockerRows(Session);
        UpdateRiskRows(Session);

        okCount.     SetText("{0}", Session.Review.Items.Count(i => i.Status == MissionCore.Rules.BudgetStatus.Green    ));
        riskCount.   SetText("{0}", Session.Review.Items.Count(i => i.Status == MissionCore.Rules.BudgetStatus.Yellow   ));
        blockerCount.SetText("{0}", Session.Review.Items.Count(i => i.Status == MissionCore.Rules.BudgetStatus.Red      ));

        launcherNameText.text = Texts.Get("part." + Design.LauncherId + ".title");

        string reliabilityFormatted = string.Format(Formatter.Culture, "{0:0.00}", Catalog.Launchers[Design.LauncherId]);
        reliabilityText.text = Session.Mode == DisplayMode.Simple ? Texts.Get("review.launcher.reliability_simple", reliabilityFormatted) : Texts.Get("review.launcher.reliability", reliabilityFormatted);

        backToDesignButton.onClick.AddListener(Session.BackToDesign);
        launchButton.onClick.AddListener(Launch);
    }

    private void Launch()
    {
        Session.TryLaunch((ReviewCode[]) Enum.GetValues(typeof(ReviewCode)));
    }

    private void UpdateBlockerRows(MissionSession session)
    {
        launchMassRow.SetStatus(session.Review.Item(ReviewCode.LaunchMassTooHigh));
        budgetRow.SetStatus(session.Review.Item(ReviewCode.BudgetExceeded));
        requiredInstrumentsRow.SetStatus(session.Review.Item(ReviewCode.RequiredInstrumentMissing));
        groundStationRow.SetStatus(session.Review.Item(ReviewCode.NoGroundStation));
    }

    private void UpdateRiskRows(MissionSession session)
    {
        lowPowerRow.SetStatus(session.Review.Item(ReviewCode.LowPowerReserve));
        storageFillsUpRow.SetStatus(session.Review.Item(ReviewCode.StorageFillsUp));
        avoidancePropellantRow.SetStatus(session.Review.Item(ReviewCode.NoAvoidancePropellant));
        disposalPropellantRow.SetStatus(session.Review.Item(ReviewCode.NoDisposalPropellant));
        lifetimeRow.SetStatus(session.Review.Item(ReviewCode.LifetimeShorterThanMission));
    }

    private string GetDescription(MissionTemplate mission)
    {
        List<string> parts = new();
        
        LaunchOrbit? orbit = OrbitClassifier.Classify(Design.AltitudeM, Design.InclinationDeg);

        parts.Add(Texts.Get(mission.titleKey));
        parts.Add(Texts.Get("part." + Design.LauncherId));
        
        parts.Add(Texts.Get("orbit." + orbit.Value.ToSnakeCase()));
        parts.Add(Formatter.Value(Metric.AltitudeM, Design.AltitudeM));
        parts.Add(Formatter.Value(Metric.InclinationDeg, Design.InclinationDeg));

        return Texts.Get("review.subtitle", parts.ToArray());
    }
}
