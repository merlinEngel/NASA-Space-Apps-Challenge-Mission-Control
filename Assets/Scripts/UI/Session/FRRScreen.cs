using System.Linq;
using MissionGame.Design;
using TMPro;
using UnityEngine;
using Screen = MissionGame.Design.Screen;

namespace MissionGame.UI
{
    public class FRRScreen : Singleton<FRRScreen>
    {
        [SerializeField] TMP_Text canLaunchText, riskCountText, riskCodesText;
        [SerializeField] MyButton backToDesignButton, confirmAllAndLaunchButton;

        void Start()
        {
            if (MissionSession.Instance == null || MissionSession.Instance.Stage != SessionStage.Review) { Screens.LoadScreen(Screen.Design); return; }

            backToDesignButton.onClick.AddListener(MissionSession.Instance.BackToDesign);
            confirmAllAndLaunchButton.onClick.AddListener(ConfirmAllAndLaunch);
        }

        public void ConfirmAllAndLaunch()
        {
            MissionSession.Instance.TryLaunch(MissionSession.Instance.Review.Risks.Select(r => r.Code));
        }
    }
}