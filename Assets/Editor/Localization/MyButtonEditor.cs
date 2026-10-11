using UnityEditor;
using UnityEditor.UI;

[CustomEditor(typeof(MyButton), true)]
[CanEditMultipleObjects]
public class MyButtonEditor : ButtonEditor
{
    SerializedProperty text;

    protected override void OnEnable()
    {
        base.OnEnable();
        text = serializedObject.FindProperty("text");
    }

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();          // draws all Button fields
        serializedObject.Update();
        EditorGUILayout.PropertyField(text);
        serializedObject.ApplyModifiedProperties();
    }
}