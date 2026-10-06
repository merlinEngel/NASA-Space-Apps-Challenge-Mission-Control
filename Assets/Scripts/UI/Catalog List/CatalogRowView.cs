using System;
using System.Collections.Generic;
using MissionCore;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MissionGame
{
    /// <summary>
    /// One row of a catalog table. Only knows how to show a CatalogRowData
    /// and reports clicks. It never changes the design itself.
    /// The value columns are not part of the prefab: the list calls SetColumns()
    /// and the row copies its cell template once per column.
    /// </summary>
    [RequireComponent(typeof(Toggle))]
    public class CatalogRowView : MonoBehaviour
    {
        [Header("Parts")]
        [SerializeField] private Toggle toggle;
        [SerializeField] private Image background;
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private TMP_Text descriptionText;

        [Header("Columns")]
        [Tooltip("Object that holds name and description. Gets the width of the first column.")]
        [SerializeField] private RectTransform nameColumn;
        [Tooltip("Inactive text that is copied once per value column. Style it like a value cell.")]
        [SerializeField] private TMP_Text cellTemplate;

        [Header("Selection marker")]
        [SerializeField] private Image marker;
        [SerializeField] private Sprite radioSprite;
        [SerializeField] private Sprite checkboxSprite;

        [Header("Colors")]
        [SerializeField] private Color normalBackground = new Color32(0x21, 0x27, 0x24, 0xFF);
        [SerializeField] private Color selectedBackground = new Color32(0xED, 0xE8, 0xDC, 0xFF);
        [SerializeField] private Color normalText = new Color32(0xED, 0xE8, 0xDC, 0xFF);
        [SerializeField] private Color normalSubText = new Color32(0x9C, 0xA4, 0x9D, 0xFF);
        [SerializeField] private Color selectedText = new Color32(0x17, 0x1B, 0x19, 0xFF);
        [SerializeField] private Color selectedSubText = new Color32(0x4A, 0x52, 0x4D, 0xFF);
        [SerializeField, Range(0f, 1f)] private float disabledAlpha = 0.4f;

        /// <summary>Fired when the player clicks the row. Arguments: catalog id, wanted state.</summary>
        public event Action<string, bool> Clicked;

        public string Id { get; private set; }

        private List<TMP_Text> valueTexts = new();

        private void Reset()
        {
            // Called when the script is added in the editor: fill the obvious references.
            toggle = GetComponent<Toggle>();
            background = GetComponent<Image>();
            canvasGroup = GetComponent<CanvasGroup>();
        }

        private void OnEnable() => toggle.onValueChanged.AddListener(OnToggleChanged);
        private void OnDisable() => toggle.onValueChanged.RemoveListener(OnToggleChanged);

        /// <summary>Creates one value cell per column and sets the column widths. Call before Show().</summary>
        public void SetColumns(CatalogColumn firstColumn, IReadOnlyList<CatalogColumn> columns)
        {
            if (nameColumn != null) CatalogColumns.SetWidth(nameColumn.gameObject, firstColumn.weight);
            valueTexts = CatalogColumns.Build(cellTemplate, columns);
        }

        /// <summary>Single = radio marker, Multiple = checkbox marker.</summary>
        public void SetSelectionMode(SelectionMode mode)
        {
            if (marker == null) return;
            Sprite sprite = mode == SelectionMode.Single ? radioSprite : checkboxSprite;
            if (sprite != null) marker.sprite = sprite;
        }

        /// <summary>Corner sprite chosen by the list (plain middle, rounded bottom).</summary>
        public void SetBackgroundSprite(Sprite sprite)
        {
            if (background == null || sprite == null) return;
            background.sprite = sprite;
            background.type = Image.Type.Sliced;
        }

        /// <summary>
        /// Shows the data. Safe to call as often as needed.
        /// texts may be null in the editor preview; then the keys are shown as they are.
        /// </summary>
        public void Show(CatalogRowData data, TextTable texts)
        {
            Id = data.Id;

            nameText.text = Localize(texts, data.NameKey);

            bool hasDescription = !string.IsNullOrEmpty(data.DescriptionKey);
            descriptionText.gameObject.SetActive(hasDescription);
            if (hasDescription) descriptionText.text = Localize(texts, data.DescriptionKey);

            for (int i = 0; i < valueTexts.Count; i++)
            {
                // Missing values leave the cell empty instead of hiding it, so the columns keep their place.
                bool has = data.Values != null && i < data.Values.Length;
                valueTexts[i].text = has ? data.Values[i] : string.Empty;
            }

            // WithoutNotify: this is the design talking to the UI, not the player clicking.
            toggle.SetIsOnWithoutNotify(data.Selected);
            toggle.interactable = !data.Disabled;
            if (canvasGroup != null) canvasGroup.alpha = data.Disabled ? disabledAlpha : 1f;

            ApplyColors(data.Selected);
        }

        /// <summary>Puts the toggle back to a state without firing Clicked.</summary>
        public void ForceState(bool isOn)
        {
            toggle.SetIsOnWithoutNotify(isOn);
            ApplyColors(isOn);
        }

        private static string Localize(TextTable texts, string key) => texts != null ? texts.Get(key) : key;

        private void OnToggleChanged(bool isOn) => Clicked?.Invoke(Id, isOn);

        private void ApplyColors(bool selected)
        {
            if (background != null) background.color = selected ? selectedBackground : normalBackground;

            Color main = selected ? selectedText : normalText;
            Color sub = selected ? selectedSubText : normalSubText;

            nameText.color = main;
            descriptionText.color = sub;
            foreach (TMP_Text value in valueTexts) value.color = main;
            if (marker != null) marker.color = main;
        }
    }
}
