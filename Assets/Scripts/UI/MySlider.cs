using System;
using MissionCore;
using MissionGame;
using NaughtyAttributes;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Wraps a Unity Slider that always runs from 0 to 1 and maps it to a real range (min..max).
/// Player input raises ValueChanged with the real value.
/// Updates coming from the design use SetValueWithoutNotify, so they never raise ValueChanged.
/// </summary>
public class MySlider : MonoBehaviour
{
    public Slider slider;
    public TMP_Text valueText;
    public Metric metric;
    [Label("Min/Max")] public Vector2 range;

    /// <summary>Raised only when the player moves the slider. Argument: real value.</summary>
    public event Action<double> ValueChanged;

    private DisplayFormatter Formatter => CatalogManager.Instance.Formatter;

    public float Min => range.x;
    public float Max => range.y;

    /// <summary>Real value. Setting it behaves like player input and raises ValueChanged.</summary>
    public float Value
    {
        get => GetTrueValue(slider.value);
        set => slider.value = GetNormalizedValue(value);
    }

    /// <summary>Slider position 0..1. Setting it raises ValueChanged.</summary>
    public float NormalizedValue
    {
        get => slider.value;
        set => slider.value = value;
    }

    private void Reset()
    {
        TryGetComponent(out slider);
        if (slider != null)
        {
            slider.minValue = 0f;
            slider.maxValue = 1f;
        }
    }

    private void Start()
    {
        slider.onValueChanged.AddListener(OnSliderChanged);
    }

    private void OnDestroy()
    {
        if (slider != null) slider.onValueChanged.RemoveListener(OnSliderChanged);
    }

    /// <summary>Design → UI: shows a real value without raising ValueChanged. Use this in OnDesignChanged.</summary>
    public void SetValueWithoutNotify(float trueValue)
    {
        float normalized = GetNormalizedValue(trueValue);
        slider.SetValueWithoutNotify(normalized);
        UpdateText(normalized);
    }

    public float GetNormalizedValue(float trueValue) => Mathf.InverseLerp(Min, Max, trueValue);
    public float GetTrueValue(float normalizedValue) => Mathf.Lerp(Min, Max, normalizedValue);

    // UI → design: only runs for real slider changes (player drag or Value/NormalizedValue setter).
    private void OnSliderChanged(float normalizedValue)
    {
        UpdateText(normalizedValue);
        ValueChanged?.Invoke(GetTrueValue(normalizedValue));
    }

    private void UpdateText(float normalizedValue)
    {
        if (valueText == null) return;
        valueText.text = Formatter.Value(metric, GetTrueValue(normalizedValue));
    }
}