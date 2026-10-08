using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Stepper : MonoBehaviour
{
    [SerializeField] Button plusButton;
    [SerializeField] Button minusButton;
    [SerializeField] TMP_Text valueText;

    public int Value
    {
        get
        {
            return _value;
        }
        set
        {
            _value = value;
            if (valueText != null) valueText.SetText("{0}", value);
            ValueChanged?.Invoke(Value);
        }
    }

    public event Action<int> OnValueIncreased;
    public event Action<int> OnValueDecreased;
    public event Action<int> ValueChanged;

    private int _value = 0;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        plusButton.onClick.AddListener(OnPlusButtonPressed);
        minusButton.onClick.AddListener(OnMinusButtonPressed);
    }

    void OnPlusButtonPressed()
    {
        Value++;
        OnValueIncreased?.Invoke(Value);
    }

    void OnMinusButtonPressed()
    {
        Value--;
        OnValueDecreased?.Invoke(Value);
    }

    public void SetValueWithoutNotify(int value)
    {
        _value = value;
        if (valueText != null) valueText.SetText("{0}", value);
    }
}
