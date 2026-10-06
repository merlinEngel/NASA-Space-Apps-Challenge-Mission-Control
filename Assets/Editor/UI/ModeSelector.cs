using UnityEditor;
using UnityEngine;

public class ModeSelector
{
    [MenuItem("GameObject/UI/Mode Selector", false, 10)]
    static void CreateModeSelector(MenuCommand command)
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/UI/ModeSelector.prefab");
        var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);

        PrefabUtility.UnpackPrefabInstance(
            instance, 
            PrefabUnpackMode.Completely, 
            InteractionMode.AutomatedAction
        );

        GameObjectUtility.SetParentAndAlign(instance, command.context as GameObject);
        Undo.RegisterCreatedObjectUndo(instance, "Create Mode Selector");
        Selection.activeObject = instance;
    }
}