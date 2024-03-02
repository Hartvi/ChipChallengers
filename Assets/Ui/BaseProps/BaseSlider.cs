using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class BaseSlider : TopProp
{
    public Slider slider;
    public override void Setup()
    {
        slider = GetComponent<Slider>();
        slider.onValueChanged.AddListener(x => Execute(x));
    }
    protected virtual void Execute(float x) {}
}
