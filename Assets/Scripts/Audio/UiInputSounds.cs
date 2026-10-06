using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace MissionGame
{
    // Plays UI sounds for every Selectable under the mouse. No per-button setup needed.
    public class UiInputSounds : MonoBehaviour
    {
        private readonly List<RaycastResult> hits = new();
        private PointerEventData pointer;
        private Selectable hovered;

        private void Update()
        {
            if (EventSystem.current == null || Mouse.current == null) return;

            Selectable current = FindSelectableUnderMouse();

            // Hover: only when the mouse moves onto a different element.
            if (current != hovered)
            {
                hovered = current;
                if (current != null && current.IsInteractable() && Override(current)?.playHover != false)
                    UiAudio.Instance.Play(UiSound.Hover);
            }

            // Click: on press, so the sound comes without delay.
            if (current != null && Mouse.current.leftButton.wasPressedThisFrame)
                UiAudio.Instance.Play(ClickSoundFor(current));
        }

        private Selectable FindSelectableUnderMouse()
        {
            pointer ??= new PointerEventData(EventSystem.current);
            pointer.position = Mouse.current.position.ReadValue();

            hits.Clear();
            EventSystem.current.RaycastAll(pointer, hits);
            if (hits.Count == 0) return null;

            // The topmost hit gets the click; walk up to the Button/Toggle it belongs to.
            return hits[0].gameObject.GetComponentInParent<Selectable>();
        }

        private static UiSound ClickSoundFor(Selectable selectable)
        {
            if (!selectable.IsInteractable()) return UiSound.Error;

            UiSoundOverride custom = Override(selectable);
            if (custom != null) return custom.clickSound;

            return selectable is Toggle ? UiSound.Select : UiSound.Click;
        }

        private static UiSoundOverride Override(Selectable selectable) =>
            selectable.GetComponent<UiSoundOverride>();
    }
}