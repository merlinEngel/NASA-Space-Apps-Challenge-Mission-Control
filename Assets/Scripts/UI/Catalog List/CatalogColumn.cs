using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MissionGame
{
    /// <summary>
    /// One column of a catalog table, set up in the Inspector of CatalogListView.
    /// The header and every row are built from the same list, so they always line up.
    /// </summary>
    [Serializable]
    public class CatalogColumn
    {
        [Tooltip("Text key for the header, e.g. ui.col.mass")]
        public string headerKey;

        [Tooltip("Share of the free width. Two columns with 2 and 1 split the space 2 : 1.")]
        [Min(0f)] public float weight = 1f;

        [Tooltip("Horizontal text alignment of the header and all cells in this column.")]
        public HorizontalAlignmentOptions alignment = HorizontalAlignmentOptions.Right;
    }

    /// <summary>
    /// Creates the column cells for a row or the header by copying an inactive template cell.
    /// Used by CatalogRowView and CatalogListView so both build their cells the same way.
    /// </summary>
    public static class CatalogColumns
    {
        // Generated cells get this name prefix, so they can be found and removed again,
        // even after a script reload when no list of them survives.
        private const string CellPrefix = "Column ";

        /// <summary>
        /// Removes old generated cells next to the template and creates one new cell per column.
        /// The template stays inactive and only serves as a style source.
        /// </summary>
        public static List<TMP_Text> Build(TMP_Text template, IReadOnlyList<CatalogColumn> columns)
        {
            var cells = new List<TMP_Text>();
            if (template == null) return cells;

            Transform parent = template.transform.parent;
            Clear(parent);
            template.gameObject.SetActive(false);

            for (int i = 0; i < columns.Count; i++)
            {
                TMP_Text cell = UnityEngine.Object.Instantiate(template, parent);
                cell.name = CellPrefix + i;
                cell.gameObject.SetActive(true);
                cell.horizontalAlignment = columns[i].alignment;
                SetWidth(cell.gameObject, columns[i].weight);
                cells.Add(cell);
            }

            return cells;
        }

        /// <summary>
        /// Width by weight: preferred width 0 means the text itself never pushes the column wider,
        /// so the free space is split only by the flexible weights.
        /// </summary>
        public static void SetWidth(GameObject target, float weight)
        {
            if (!target.TryGetComponent(out LayoutElement layout))
                layout = target.AddComponent<LayoutElement>();

            layout.minWidth = 0f;
            layout.preferredWidth = 0f;
            layout.flexibleWidth = weight;
        }

        private static void Clear(Transform parent)
        {
            for (int i = parent.childCount - 1; i >= 0; i--)
            {
                GameObject child = parent.GetChild(i).gameObject;
                if (child.name.StartsWith(CellPrefix)) DestroySafe(child);
            }
        }

        /// <summary>Destroy only works in Play mode; in the editor (preview) objects must go immediately.</summary>
        public static void DestroySafe(GameObject target)
        {
            if (Application.isPlaying)
            {
                // Destroy happens at the end of the frame. Detach first so layout and
                // child counts ignore the old object right away.
                target.SetActive(false);
                target.transform.SetParent(null, false);
                UnityEngine.Object.Destroy(target);
                return;
            }

#if UNITY_EDITOR
            if (UnityEditor.PrefabUtility.IsPartOfPrefabInstance(target)
                && !UnityEditor.PrefabUtility.IsAddedGameObjectOverride(target))
            {
                Debug.LogWarning($"'{target.name}' belongs to a prefab and cannot be removed here. " +
                                 "Run Rebuild Preview inside the prefab (Prefab Mode) instead.", target);
                return;
            }
#endif
            UnityEngine.Object.DestroyImmediate(target);
        }
    }
}
