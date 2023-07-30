using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LoadingText : BaseText
{
    void Start()
    {
        text.SetText("Loading...");
        text.alignment = TMPro.TextAlignmentOptions.Top;
        text.fontSize = UIUtils.LargeFontSize;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
