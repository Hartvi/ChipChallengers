using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SettingsHandler : TopProp
{
    BaseSlider[] sliders;
    BaseText[] texts;
    protected override void Setup()
    {
    }
    protected void Start()
    {
        sliders = GetComponentsInChildren<BaseSlider>();
        texts = GetComponentsInChildren<BaseText>();
        texts[0].text.alignment = TextAlignmentOptions.Top;
        texts[0].text.SetText("Settings");
        texts[1].text.SetText("Volume");
        float savedVolume = PlayerPrefs.GetFloat("Volume");
        //print("saved volume: " + savedVolume);
        sliders[0].slider.SetValueWithoutNotify(savedVolume);
        
    }

}
