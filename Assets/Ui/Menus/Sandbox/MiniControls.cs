using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MiniControls : BaseText
{
    void Start()
    {
        this.text.SetText(UIStrings.MiniControls);
        UIUtils.SetFontSize(this.text, UIUtils.SmallFontSize);
    }

    void Update()
    {
        
    }
}
