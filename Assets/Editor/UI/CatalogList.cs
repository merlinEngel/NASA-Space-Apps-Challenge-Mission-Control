using UnityEditor;
using UnityEngine;

public class CatalogList
{
    [MenuItem("GameObject/UI/Catalog List", false, 10)]
    static void CreateCatalogList(MenuCommand command)
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/UI/CatalogList.prefab");
        var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);

        // PrefabUtility.UnpackPrefabInstance(
        //     instance,
        //     PrefabUnpackMode.Completely,
        //     InteractionMode.AutomatedAction
        // );

        GameObjectUtility.SetParentAndAlign(instance, command.context as GameObject);
        Undo.RegisterCreatedObjectUndo(instance, "Create Catalog List");
        Selection.activeObject = instance;
    }
}