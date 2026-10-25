using MissionGame.Design;
using TMPro;
using UnityEngine;
using Screen = MissionGame.Design.Screen;

namespace MissionGame.UI
{
    public class LaunchScreen : Singleton<LaunchScreen>
    {
        [SerializeField] TMP_Text seedText, rollText, successRateText, successText;
        [SerializeField] MyButton continueButton;

        void Start()
        {
            if (MissionSession.Instance == null || MissionSession.Instance.Stage != SessionStage.Launch) { Screens.LoadScreen(Screen.Design); return; }

            continueButton.onClick.AddListener(MissionSession.Instance.FinishLaunch);
        }
    }
}