using System;
using TMPro;
using UnityEngine;

[RequireComponent(typeof(TMP_Text))]
public class TextLocalizer : MonoBehaviour
{
    public string key;
    [SerializeField] TMP_Text text;
    public bool setTextAsKey = true;

    public Action<TextLocalizer, object[]> UpdateRequested;

    void Reset()
    {
        text = GetComponent<TMP_Text>();
    }

    public void RequestUpdate(object[] args = null, string key = null)
    {
        key ??= this.key;
        this.key = key;

        args ??= new object[]{};
        UpdateRequested?.Invoke(this, args);
    }

    void OnValidate()
    {
        if (setTextAsKey) text.text = key;
    }

    public void SetText(string text)
    {
        this.text.text = text;
    }
}   
