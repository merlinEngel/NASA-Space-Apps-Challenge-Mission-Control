using UnityEditor;
using UnityEditor.UI;
using MissionGame.UI;

namespace MissionGame.Editor
{
    [CustomEditor(typeof(MyButton), true)]
    [CanEditMultipleObjects]
    public class MyButtonEditor : ButtonEditor
    {
        SerializedProperty text;
        SerializedProperty border;

        protected override void OnEnable()
        {
            base.OnEnable();
            text = serializedObject.FindProperty("text");
            border = serializedObject.FindProperty("border");
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();          // draws all Button fields
            serializedObject.Update();
            EditorGUILayout.PropertyField(text);
            EditorGUILayout.PropertyField(border);
            serializedObject.ApplyModifiedProperties();
        }
    }
}
