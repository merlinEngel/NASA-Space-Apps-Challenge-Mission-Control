using MissionGame.Design;
using TMPro;
using UnityEngine;
using Screen = MissionGame.Design.Screen;

namespace MissionGame.UI
{
    public class OperationsScreen : Singleton<OperationsScreen>
    {
        [SerializeField] TMP_Text missionTimeText;

        void Start()
        {
            if (MissionSession.Instance == null || MissionSession.Instance.Stage != SessionStage.Operations) { Screens.LoadScreen(Screen.Design); return; }
        }
    }
}