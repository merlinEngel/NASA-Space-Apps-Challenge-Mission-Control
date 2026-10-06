using UnityEditor;
using UnityEngine;

public class LocalizedText
{
    [MenuItem("GameObject/UI/Localized Text", false, 10)]
    static void CreateModeSelector(MenuCommand command)
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/UI/LocalizedText.prefab");
        var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);

        // PrefabUtility.UnpackPrefabInstance(
        //     instance,
        //     PrefabUnpackMode.Completely,
        //     InteractionMode.AutomatedAction
        // );

        GameObjectUtility.SetParentAndAlign(instance, command.context as GameObject);
        Undo.RegisterCreatedObjectUndo(instance, "Create Localized Text");
        Selection.activeObject = instance;
    }
}