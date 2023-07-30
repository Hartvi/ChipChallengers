using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MPText : BaseText
{
    void Start()
    {
        text.SetText("Connect to multiplayer");
        text.fontSize = UIUtils.LargeFontSize;
        text.alignment = TMPro.TextAlignmentOptions.Top;
        text.enableWordWrapping = false;
    }

}
