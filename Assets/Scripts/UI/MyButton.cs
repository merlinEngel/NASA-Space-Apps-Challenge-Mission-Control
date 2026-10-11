using System.Collections.Generic;
using TMPro;
using Unity.Theme.Binders;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(ButtonColorBinderRequired))]
[RequireComponent(typeof(Image))]public class MyButton : Button
{
    ButtonColorBinderRequired ButtonColorBinder => GetComponent<ButtonColorBinderRequired>();
    // Image Image => GetComponent<Image>();
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
    }

    protected override void DoStateTransition(SelectionState state, bool instant)
    {
        base.DoStateTransition(state, instant);
        text.color = ButtonColorBinder.GetColors()[ColorBinderStateIndices[state]];
        // Image.color = ButtonColorBinder.GetColors()[ColorBinderStateIndices[state]];
    }
}