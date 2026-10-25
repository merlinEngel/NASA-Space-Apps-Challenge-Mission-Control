using System.Collections.Generic;
using TMPro;
using Unity.Theme.Binders;
using UnityEngine;
using UnityEngine.UI;

namespace MissionGame.UI
{
    [RequireComponent(typeof(ButtonColorBinderRequired))]
    [RequireComponent(typeof(Image))]
    public class MyButton : Button
    {
        ButtonColorBinderRequired ButtonColorBinder => GetComponent<ButtonColorBinderRequired>();
        public Image border;
        public TMP_Text text;

        readonly Dictionary<SelectionState, int> ColorBinderStateIndices = new()
        {
            [SelectionState.Normal] = 0,
            [SelectionState.Highlighted] = 1,
            [SelectionState.Pressed] = 2,
            [SelectionState.Selected] = 3,
            [SelectionState.Disabled] = 4,
        };

        protected override void OnValidate()
        {
            text.color = ButtonColorBinder.GetColors()[ColorBinderStateIndices[currentSelectionState]];
            border.color = ButtonColorBinder.GetColors()[ColorBinderStateIndices[currentSelectionState]];
        }

        protected override void DoStateTransition(SelectionState state, bool instant)
        {
            base.DoStateTransition(state, instant);
            text.color = ButtonColorBinder.GetColors()[ColorBinderStateIndices[state]];
            border.color = ButtonColorBinder.GetColors()[ColorBinderStateIndices[state]];
        }
    }
}
