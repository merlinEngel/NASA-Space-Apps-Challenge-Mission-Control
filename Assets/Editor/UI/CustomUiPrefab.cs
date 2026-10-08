using UnityEditor;
using UnityEngine;

public static class CustomUiPrefab
{
    public const string Menu = "GameObject/UI/";
    private const string Folder = "Assets/Prefabs/UI/";

    /// <summary>Instantiates Assets/Prefabs/UI/{prefabName}.prefab under the clicked object.</summary>
    public static void Create(MenuCommand command, string prefabName)
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(Folder + prefabName + ".prefab");
        if (prefab == null)
        {
            Debug.LogError($"Prefab '{Folder}{prefabName}.prefab' not found.");
            return;
        }

        var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        GameObjectUtility.SetParentAndAlign(instance, command.context as GameObject);
        Undo.RegisterCreatedObjectUndo(instance, "Create " + prefabName);
        Selection.activeObject = instance;
    }
}

public static class CustomUiPrefabMenu
{
    [MenuItem(CustomUiPrefab.Menu + "Catalog List", false, 10)]     private static void CatalogList(MenuCommand c) => CustomUiPrefab.Create(c, "CatalogList");
    
    [MenuItem(CustomUiPrefab.Menu + "Localized Text", false, 10)]   private static void LocalizedText(MenuCommand c) => CustomUiPrefab.Create(c, "LocalizedText");

    [MenuItem(CustomUiPrefab.Menu + "Mode Selector", false, 10)]    private static void ModeSelector(MenuCommand c) => CustomUiPrefab.Create(c, "ModeSelector");
    
    [MenuItem(CustomUiPrefab.Menu + "Tab List", false, 10)]         private static void TabList(MenuCommand c) => CustomUiPrefab.Create(c, "TabList");

    [MenuItem(CustomUiPrefab.Menu + "My Slider", false, 10)]        private static void MySlider(MenuCommand c) => CustomUiPrefab.Create(c, "MySlider");
    [MenuItem(CustomUiPrefab.Menu + "Stepper", false, 10)]          private static void Stepper(MenuCommand c) => CustomUiPrefab.Create(c, "Stepper");
}