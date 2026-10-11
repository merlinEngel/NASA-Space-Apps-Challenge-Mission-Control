using System;
using System.Collections.Generic;
using System.Linq;
using MissionCore;
using MissionGame;
using MissionGame.UI;
using TMPro;
using UnityEngine;

public class DesignUiManager : MonoBehaviour
{
    [SerializeField] ModeSelector difficultySelector;
    [SerializeField] TMP_Text missionTitleText;
    [SerializeField] TMP_Text missionDescriptionText;
    [SerializeField] MyButton toLaunchButton;

    void Start()
    {
        MissionDesignManager.Instance.DesignChanged += OnDesignChanged;
        difficultySelector.OnOptionSelected += OnDifficultySelected;
        OnDesignChanged(null, null, null);
    }

    void OnDestroy()
    {
        if (MissionDesignManager.Instance != null) MissionDesignManager.Instance.DesignChanged -= OnDesignChanged;
        difficultySelector.OnOptionSelected -= OnDifficultySelected;
    }

    private void OnDifficultySelected(int index)
    {
        // Option order in the ModeSelector: 0 = simple, 1 = realistic.
        CatalogManager.Instance.SetDisplayMode(index == 1 ? DisplayMode.Realistic : DisplayMode.Simple);
    }

    void OnDesignChanged(DesignReport report, DesignReport previous, List<DesignChange> changes)
    {
        if (report != null) toLaunchButton.interactable = report.IsValid;
        else toLaunchButton.interactable = false;
        
        MissionTemplate mission = CatalogManager.Instance.Catalog.Missions.Keys.Contains(MissionDesignManager.Instance.Design.MissionId) ? CatalogManager.Instance.Catalog.Missions[MissionDesignManager.Instance.Design.MissionId] : null;
        TextTable texts = CatalogManager.Instance.Texts;

        if (mission != null)
        {
            missionTitleText.text = texts.Get(mission.titleKey);
            missionDescriptionText.text = texts.Get(mission.descriptionKey, mission.TextValues(CatalogManager.Instance.Formatter));
        }
    }
}
