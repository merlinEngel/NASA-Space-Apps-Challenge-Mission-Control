using System;
using MissionCore;
using MissionGame;
using TMPro;
using Unity.Theme;
using Unity.Theme.Binders;
using UnityEngine;
using UnityEngine.UI;


[RequireComponent(typeof(LayoutElement))]
public class DeltaChip : MonoBehaviour
{
    public BudgetKind kind;

    [SerializeField] TMP_Text value;
    [SerializeField] TextMeshProColorBinder valueColorBinder;
    [SerializeField] TextMeshProColorBinder labelColorBinder;
    [SerializeField] ImageColorBinder borderColorBinder;
    [SerializeField] string greenColorName;
    [SerializeField] string redColorName;

    GameObject Content => transform.GetChild(0).gameObject;

    public void UpdateDelta(MetricDelta metricDelta)
    {
        ColorData colorData;

        if (Math.Abs(metricDelta.Delta) < 1e-9)
        {
            Content.SetActive(false);
            GetComponent<LayoutElement>().ignoreLayout = true;
        }
        else
        {
            Content.SetActive(true);
            GetComponent<LayoutElement>().ignoreLayout = false;
        }
        colorData = Theme.Instance.GetColorByName(metricDelta.IsBetter ? greenColorName : redColorName);

        valueColorBinder.SetColor(colorData);
        labelColorBinder.SetColor(colorData);
        borderColorBinder.SetColor(colorData);
        value.text = CatalogManager.Instance.Formatter.Delta(metricDelta);
    }
}