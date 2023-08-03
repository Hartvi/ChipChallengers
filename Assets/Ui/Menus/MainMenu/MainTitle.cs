using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MainTitle : BaseText
{
    protected override void Setup()
    {
        base.Setup();
        this.text.fontSize = UIUtils.LargeFontSize;
        this.text.horizontalAlignment = TMPro.HorizontalAlignmentOptions.Center;
        this.text.text = UIStrings.GameTitle;
    }
}
