using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class MainTitle : BaseText
{
    public override void Setup()
    {
        base.Setup();
        this.text.fontSize = UIUtils.LargeFontSize;
        this.text.horizontalAlignment = HorizontalAlignmentOptions.Center;
        this.text.text = UIStrings.GameTitle;
    }
}
