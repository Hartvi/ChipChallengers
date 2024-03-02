using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class BaseText : TopProp
{
    public TMP_Text text;
    public override void Setup()
    {
        text = this.GetComponent<TMP_Text>();
    }
}
