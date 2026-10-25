using MissionGame.Design;
using TMPro;
using UnityEngine;
using Screen = MissionGame.Design.Screen;

namespace MissionGame.UI
{
    public class ResultScreen : Singleton<ResultScreen>
    {
        [SerializeField] TMP_Text failedLaunchText;
        [SerializeField] MyButton returnToDesignButton;

        void Start()
        {
            if (MissionSession.Instance == null || MissionSession.Instance.Stage != SessionStage.Result) { Screens.LoadScreen(Screen.Design); return; }

            returnToDesignButton.onClick.AddListener(MissionSession.Instance.BackToDesign);
        }
    }
}