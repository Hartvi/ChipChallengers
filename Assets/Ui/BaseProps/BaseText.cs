using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class BaseText : TopProp
{
    public TMP_Text text;
    protected override void Setup()
    {
        text = GetComponent<TMP_Text>();
    }
}
