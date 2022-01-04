using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIRadioGroup : MonoBehaviour
{
    public void OnChange()
    {
        for (int i = 0; i < Options.Length; ++i)
        {
            if (Options[i].isOn)
            {
                SelectedOption = i;
                break;
            }
        }
        Changed?.Invoke(SelectedOption);
    }

    public int SelectedOption { get; private set; }
    public event Action<int> Changed;

    public Toggle[] Options;
}
