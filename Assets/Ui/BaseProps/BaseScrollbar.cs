using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class BaseScrollbar : TopProp
{
    public Scrollbar scrollbar;
    protected override void Setup()
    {
        scrollbar = GetComponent<Scrollbar>();
        scrollbar.onValueChanged.AddListener(x => Execute(x));
    }
    protected virtual void Execute(float x) {}
}

