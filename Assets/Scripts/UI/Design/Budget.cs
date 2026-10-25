using System;
using MissionGame;
using MissionGame.UI;
using NaughtyAttributes;
using TMPro;
using Unity.Theme.Binders;
using UnityEngine;
using MissionCore.Design;
using MissionCore.Rules;

namespace MissionGame.UI.Design
{
    [RequireComponent(typeof(GraphicsColorBinder))]
    public class Budget : MonoBehaviour
    {
        [SerializeField] TMP_Text valueText;
        [SerializeField] TMP_Text metricText;
        [SerializeField] TMP_Text ofLimitText;
    
        GraphicsColorBinder ColorBinder => GetComponent<GraphicsColorBinder>();

        [SerializeField] RectTransform budgetLine;
        [SerializeField] RectTransform bar;
        [SerializeField] RectTransform fillArea;
        [SerializeField] RectTransform positiveDelta;
        [SerializeField] RectTransform negativeDelta;

        [SerializeField] Metric metric;
        [SerializeField] string metricKey;

        [Label("Budget Line Height %")][SerializeField] double budgetLineHeightPercent;

        void Start()
        {
            metricText.text = CatalogManager.Instance.Texts.Get(metricKey);
        }

        public void UpdateValue(double budget, double value, double delta, BudgetStatus status)
        {
            float totalHeight = bar.rect.height;
            float lineHeight = totalHeight * (float)(budgetLineHeightPercent / 100);
            double before = value - delta;

            // Without a usable limit (0 or infinite, e.g. no launcher yet) the larger value sits on the line.
            bool hasLimit = budget > 0 && !double.IsInfinity(budget);
            double scaleMax = hasLimit ? budget : Math.Max(Math.Max(value, before), 1e-9);
            budgetLine.gameObject.SetActive(hasLimit);
            budgetLine.anchoredPosition = new Vector2(budgetLine.anchoredPosition.x, lineHeight);

            float valueY = ToHeight(value, scaleMax, lineHeight, totalHeight);
            float beforeY = ToHeight(before, scaleMax, lineHeight, totalHeight);
            SetHeight(fillArea, valueY);

            // Both deltas hang from the top edge of the fill.
            float diff = valueY - beforeY;
            bool grew = diff > 0.5f;    // ignore changes smaller than half a pixel
            bool shrank = diff < -0.5f;
            positiveDelta.gameObject.SetActive(grew);
            negativeDelta.gameObject.SetActive(shrank);

            // Grown: the new part is the top piece inside the fill.
            if (grew)
            {
                SetHeight(positiveDelta, diff);
            }

            // Shrunk: the removed part sits above the fill, so move it up by its own height.
            if (shrank)
            {
                SetHeight(negativeDelta, -diff);
                negativeDelta.anchoredPosition = new Vector2(negativeDelta.anchoredPosition.x, -diff);
            }

            valueText.text = CatalogManager.Instance.Formatter.Value(metric, value);
            ofLimitText.text = CatalogManager.Instance.Texts.Get("ui.of_limit", CatalogManager.Instance.Formatter.Value(metric, budget));

            ColorBinder.SetColorByName(Constants.StatusColorKeys[status]);
        }

        static float ToHeight(double v, double scaleMax, float lineHeight, float totalHeight)
        {
            return Mathf.Clamp((float)(v / scaleMax * lineHeight), 0, totalHeight);
        }

        static void SetHeight(RectTransform rt, float height)
        {
            rt.sizeDelta = new Vector2(rt.sizeDelta.x, height);
        }
    }
}
