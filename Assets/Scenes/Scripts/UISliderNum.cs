using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class UISliderNum : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        SliderObj.onValueChanged.AddListener(new UnityEngine.Events.UnityAction<float>(OnSliderChange));
        InputObj.onEndEdit.AddListener(new UnityEngine.Events.UnityAction<string>(OnTextChange));
        Value = SliderObj.value;
    }

    private void OnSliderChange(float val)
    {
        InputObj.text = (Value = val).ToString();
    }

    private void OnTextChange(string val)
    {
        float valNum;
        if (float.TryParse(val, out valNum))
        {
            Value = SliderObj.value = valNum;
        }
    }

    public Slider SliderObj;
    public TMP_InputField InputObj;
    public float Value { get; private set; }
}
