using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

// Debug helper: logs every UI object under the mouse on click. Remove when done.
public class UiRaycastDebug : MonoBehaviour
{
    private void Update()
    {
        if (Mouse.current == null || !Mouse.current.leftButton.wasPressedThisFrame) return;

        var data = new PointerEventData(EventSystem.current) { position = Mouse.current.position.ReadValue() };
        var hits = new List<RaycastResult>();
        EventSystem.current.RaycastAll(data, hits);

        if (hits.Count == 0) Debug.Log("UI raycast: nothing hit");
        foreach (RaycastResult hit in hits)
            Debug.Log($"UI raycast hit: {hit.gameObject.name} (depth {hit.depth})", hit.gameObject);
    }
}