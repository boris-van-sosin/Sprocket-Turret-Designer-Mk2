using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class HelpPanel : MonoBehaviour
{
    void Awake()
    {
        _textBox = GetComponentInChildren<TextMeshProUGUI>();
    }

    public void ToggleHelpPanel()
    {
        _visible = !_visible;
        gameObject.SetActive(_visible);
    }

    public void SetText(string text)
    {
        _textBox.text = text;
    }

    public void SetText()
    {
        _textBox.text = _defaultHelpText;
    }

    private static readonly string
        _defaultHelpText = "Move: shift + mid-mouse\n" +
                           "Orbit: mid-mouse";

    private bool _visible;
    private TextMeshProUGUI _textBox;
}
