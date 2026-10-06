using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;
using Unity.Theme.Binders;

// NaughtyInspector überschreibt alle Inspectors mit IMGUI. Die Drawer von Unity Theme
// nutzen nur UI Toolkit (CreatePropertyGUI) und zeigen sonst "No GUI Implemented".
// Diese Editoren sind spezifischer als NaughtyInspector und erzwingen UI Toolkit.
namespace MissionControl.Editor
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(BaseColorBinder), true)]
    public class ThemeColorBinderInspector : UnityEditor.Editor
    {
        public override VisualElement CreateInspectorGUI()
        {
            var rootElement = new VisualElement();
            InspectorElement.FillDefaultInspector(rootElement, serializedObject, this);
            return rootElement;
        }
    }

    [CanEditMultipleObjects]
    [CustomEditor(typeof(BaseMultiColorBinder), true)]
    public class ThemeMultiColorBinderInspector : UnityEditor.Editor
    {
        public override VisualElement CreateInspectorGUI()
        {
            var root = new VisualElement();
            InspectorElement.FillDefaultInspector(root, serializedObject, this);
            return root;
        }
    }
}
