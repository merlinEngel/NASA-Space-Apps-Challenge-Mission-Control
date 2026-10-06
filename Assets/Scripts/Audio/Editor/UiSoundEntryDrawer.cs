using UnityEditor;
using UnityEngine;

namespace MissionGame
{
    // Inspector layout for UiSoundEntry:
    // a foldout header with name + summary, then clips, volume and two min-max sliders.
    [CustomPropertyDrawer(typeof(UiSoundEntry))]
    public class UiSoundEntryDrawer : PropertyDrawer
    {
        private const float Gap = 2f;
        private const float RangeLimit = 0.5f;     // offsets are limited to -0.5 .. +0.5
        private const float NumberWidth = 46f;

        private static float Line => EditorGUIUtility.singleLineHeight;

        private static GUIStyle headerStyle;
        private static GUIStyle HeaderStyle =>
            headerStyle ??= new GUIStyle(EditorStyles.foldout) { fontStyle = FontStyle.Bold };

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            SerializedProperty sound = property.FindPropertyRelative("sound");
            SerializedProperty clips = property.FindPropertyRelative("clips");
            SerializedProperty volume = property.FindPropertyRelative("volume");
            SerializedProperty pitchRange = property.FindPropertyRelative("pitchRange");
            SerializedProperty volumeRange = property.FindPropertyRelative("volumeRange");

            bool hasClips = clips.arraySize > 0;
            Rect row = new Rect(position.x, position.y, position.width, Line);

            // Header: "Click   2 clips · volume 0.80" and a warning icon if nothing is assigned.
            string title = sound.enumValueIndex >= 0 ? sound.enumDisplayNames[sound.enumValueIndex] : "?";
            string summary = hasClips
                ? $"{clips.arraySize} clip{(clips.arraySize == 1 ? "" : "s")} · volume {volume.floatValue:0.00}"
                : "no clips";
            property.isExpanded = EditorGUI.Foldout(row, property.isExpanded,
                new GUIContent($"{title}   ({summary})"), true, HeaderStyle);

            if (!hasClips)
            {
                Rect iconRect = new Rect(row.xMax - Line, row.y, Line, Line);
                GUI.Label(iconRect, EditorGUIUtility.IconContent("console.warnicon.sml"));
            }

            if (property.isExpanded)
            {
                EditorGUI.indentLevel++;

                // The library keeps one entry per enum value, so the sound itself is read-only.
                row.y += Line + Gap;
                using (new EditorGUI.DisabledScope(true))
                    EditorGUI.PropertyField(row, sound);

                row.y += Line + Gap;
                row.height = EditorGUI.GetPropertyHeight(clips, true);
                EditorGUI.PropertyField(row, clips, true);

                row.y += row.height + Gap;
                row.height = Line;
                EditorGUI.Slider(row, volume, 0f, 1f, new GUIContent("Volume"));

                row.y += Line + Gap;
                DrawRange(row, volumeRange, new GUIContent("Volume variation",
                    "Random offset added to the volume each time the sound plays."));

                row.y += Line + Gap;
                DrawRange(row, pitchRange, new GUIContent("Pitch variation",
                    "Random offset added to pitch 1.0. Small values (±0.05) keep repeated sounds from feeling mechanical."));

                if (!hasClips)
                {
                    row.y += Line + Gap;
                    row.height = Line * 2;
                    EditorGUI.HelpBox(row, "No clips assigned: this sound stays silent.", MessageType.Warning);
                }

                EditorGUI.indentLevel--;
            }

            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            if (!property.isExpanded) return Line;

            SerializedProperty clips = property.FindPropertyRelative("clips");

            float height = Line + Gap;                                      // header
            height += Line + Gap;                                           // sound
            height += EditorGUI.GetPropertyHeight(clips, true) + Gap;       // clips
            height += (Line + Gap) * 3;                                     // volume + two ranges
            if (clips.arraySize == 0) height += Line * 2 + Gap;             // warning box
            return height;
        }

        // [min field] [====min-max slider====] [max field]
        private static void DrawRange(Rect rect, SerializedProperty property, GUIContent label)
        {
            Rect content = EditorGUI.PrefixLabel(rect, label);

            // PrefixLabel already indented the rect; without this the fields would be indented twice.
            int indent = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;

            Rect minRect = new Rect(content.x, content.y, NumberWidth, content.height);
            Rect maxRect = new Rect(content.xMax - NumberWidth, content.y, NumberWidth, content.height);
            Rect sliderRect = new Rect(minRect.xMax + 4f, content.y, maxRect.x - minRect.xMax - 8f, content.height);

            Vector2 value = property.vector2Value;
            float min = value.x;
            float max = value.y;

            EditorGUI.BeginChangeCheck();
            min = EditorGUI.FloatField(minRect, min);
            EditorGUI.MinMaxSlider(sliderRect, ref min, ref max, -RangeLimit, RangeLimit);
            max = EditorGUI.FloatField(maxRect, max);
            if (EditorGUI.EndChangeCheck())
            {
                min = Mathf.Clamp(min, -RangeLimit, RangeLimit);
                max = Mathf.Clamp(max, min, RangeLimit);
                property.vector2Value = new Vector2((float)System.Math.Round(min, 3), (float)System.Math.Round(max, 3));
            }

            EditorGUI.indentLevel = indent;
        }
    }
}
