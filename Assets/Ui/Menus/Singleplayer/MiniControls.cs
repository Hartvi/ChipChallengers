using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MiniControls : BaseText
{
    void Start()
    {
        this.text.SetText(UIStrings.MiniControls);
        this.text.fontSize = UIUtils.SmallFontSize;
    }

    void Update()
    {
        
    }
}
