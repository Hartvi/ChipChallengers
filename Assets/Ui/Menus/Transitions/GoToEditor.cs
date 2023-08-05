using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GoToEditor : BaseButton
{
    protected override void Setup()
    {
        base.Setup();
        this.text.text = UIStrings.Editor;
        this.text.fontSize = UIUtils.MediumFontSize;
    }
    protected override void Execute()
    {
        //BaseMenu.SwitchToPreviousMenu();
        BaseMenu.SwitchToMenu(typeof(EditorMenu));
    }
}
