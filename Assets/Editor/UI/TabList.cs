using UnityEditor;
using UnityEngine;

public class TabList
{
    [MenuItem("GameObject/UI/Tab List", false, 10)]
    static void CreateModeSelector(MenuCommand command)
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/UI/TabList.prefab");
        var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);

        PrefabUtility.UnpackPrefabInstance(
            instance, 
            PrefabUnpackMode.Completely, 
            InteractionMode.AutomatedAction
        );

        GameObjectUtility.SetParentAndAlign(instance, command.context as GameObject);
        Undo.RegisterCreatedObjectUndo(instance, "Create Tab List");
        Selection.activeObject = instance;
    }
}