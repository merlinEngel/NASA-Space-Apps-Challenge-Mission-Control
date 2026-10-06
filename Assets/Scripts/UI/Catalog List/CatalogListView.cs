using System;
using System.Collections.Generic;
using MissionCore;
using TMPro;
using UnityEngine;

namespace MissionGame
{
    public enum SelectionMode
    {
        Single,
        Multiple
    }

    /// <summary>
    /// A catalog table: header plus one row per catalog entry.
    /// The columns are set up here in the Inspector; header and rows build their
    /// cells from that list, so one row prefab works for every catalog.
    /// Build() creates the rows once, Refresh() only updates them.
    /// The list never changes the design. It raises RowClicked and the owning
    /// tab decides what to do (usually MissionDesignManager.ModifyDesign).
    /// </summary>
    public class CatalogListView : MonoBehaviour
    {
        [Header("Setup")]
        [SerializeField] private CatalogRowView rowPrefab;
        [SerializeField] private RectTransform content;
        [SerializeField] private SelectionMode selectionMode = SelectionMode.Single;

        [Header("Columns")]
        [Tooltip("Name and description column. Header key, weight and header alignment.")]
        [SerializeField] private CatalogColumn firstColumn = new() { headerKey = "ui.col.model", weight = 3f, alignment = HorizontalAlignmentOptions.Left };
        [Tooltip("Value columns, in the same order as CatalogRowData.Values.")]
        [SerializeField] private List<CatalogColumn> columns = new();

        [Header("Header")]
        [SerializeField] private TMP_Text firstColumnHeader;
        [Tooltip("Inactive header text that is copied once per value column.")]
        [SerializeField] private TMP_Text headerCellTemplate;

        [Header("Row corners (optional)")]
        [SerializeField] private Sprite middleRowSprite;
        [SerializeField] private Sprite lastRowSprite;

        [Header("Editor preview")]
        [SerializeField, Min(0)] private int previewRowCount = 3;

        /// <summary>Arguments: catalog id, wanted state. In Single mode always true.</summary>
        public event Action<string, bool> RowClicked;

        public SelectionMode SelectionMode => selectionMode;

        private readonly List<CatalogRowView> rows = new();
        private List<TMP_Text> headerCells = new();

        private void Awake()
        {
            // Remove the preview rows that only exist so the list looks right in the editor.
            RemoveRowsInContent();
        }

        private void OnDestroy()
        {
            foreach (CatalogRowView row in rows) row.Clicked -= OnRowClicked;
        }

        /// <summary>Creates header and one row per entry. Call once when the tab is set up.</summary>
        public void Build(IReadOnlyList<CatalogRowData> data)
        {
            BuildHeader(CatalogManager.Instance.Texts);

            foreach (CatalogRowView old in rows)
            {
                old.Clicked -= OnRowClicked;
                CatalogColumns.DestroySafe(old.gameObject);
            }
            rows.Clear();

            foreach (CatalogRowView row in CreateRows(data.Count))
            {
                row.Clicked += OnRowClicked;
                rows.Add(row);
            }

            Refresh(data);
        }

        /// <summary>Updates values, selection and disabled state. Call on every DesignChanged.</summary>
        public void Refresh(IReadOnlyList<CatalogRowData> data)
        {
            if (data.Count != rows.Count)
            {
                // The catalog itself changed (new entries): rebuild instead of guessing.
                Build(data);
                return;
            }

            TextTable texts = CatalogManager.Instance.Texts;
            for (int i = 0; i < rows.Count; i++) rows[i].Show(data[i], texts);
        }

        private List<CatalogRowView> CreateRows(int count)
        {
            var created = new List<CatalogRowView>();
            for (int i = 0; i < count; i++)
            {
                CatalogRowView row = Instantiate(rowPrefab, content);
                row.name = $"Row {i}";
                row.SetColumns(firstColumn, columns);
                row.SetSelectionMode(selectionMode);
                row.SetBackgroundSprite(i == count - 1 ? lastRowSprite : middleRowSprite);
                created.Add(row);
            }
            return created;
        }

        /// <summary>texts may be null in the editor preview; then the keys are shown.</summary>
        private void BuildHeader(TextTable texts)
        {
            if (firstColumnHeader != null)
            {
                firstColumnHeader.text = Localize(texts, firstColumn.headerKey);
                firstColumnHeader.horizontalAlignment = firstColumn.alignment;
                CatalogColumns.SetWidth(firstColumnHeader.gameObject, firstColumn.weight);
            }

            headerCells = CatalogColumns.Build(headerCellTemplate, columns);
            for (int i = 0; i < headerCells.Count; i++)
                headerCells[i].text = Localize(texts, columns[i].headerKey);
        }

        private void RemoveRowsInContent()
        {
            for (int i = content.childCount - 1; i >= 0; i--)
            {
                if (content.GetChild(i).GetComponent<CatalogRowView>() != null)
                    CatalogColumns.DestroySafe(content.GetChild(i).gameObject);
            }
        }

        private static string Localize(TextTable texts, string key) =>
            texts != null && !string.IsNullOrEmpty(key) ? texts.Get(key) : key;

        private void OnRowClicked(string id, bool isOn)
        {
            if (selectionMode == SelectionMode.Single && !isOn)
            {
                // Clicking the chosen row again must not leave the slot empty.
                rows.Find(r => r.Id == id)?.ForceState(true);
                return;
            }

            RowClicked?.Invoke(id, isOn);
        }

#if UNITY_EDITOR
        /// <summary>
        /// Right-click the component header (or the ⋮ menu) → Rebuild Preview.
        /// Builds header and a few dummy rows in edit mode so you can check widths and alignment.
        /// The dummy rows are removed again in Awake when the game starts.
        /// </summary>
        [ContextMenu("Rebuild Preview")]
        private void RebuildPreview()
        {
            if (rowPrefab == null || content == null)
            {
                Debug.LogWarning("Row Prefab and Content must be set first.", this);
                return;
            }

            BuildHeader(null);
            RemoveRowsInContent();

            List<CatalogRowView> preview = CreateRows(previewRowCount);
            for (int i = 0; i < preview.Count; i++)
            {
                var values = new string[columns.Count];
                for (int c = 0; c < values.Length; c++) values[c] = "0.00";

                preview[i].Show(new CatalogRowData
                {
                    Id = $"preview_{i}",
                    NameKey = "Name",
                    DescriptionKey = "Description",
                    Values = values,
                    Selected = i == 0
                }, null);
            }

            UnityEditor.EditorUtility.SetDirty(gameObject);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(gameObject.scene);
        }

        [ContextMenu("Clear Preview")]
        private void ClearPreview()
        {
            RemoveRowsInContent();
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(gameObject.scene);
        }
#endif
    }
}
