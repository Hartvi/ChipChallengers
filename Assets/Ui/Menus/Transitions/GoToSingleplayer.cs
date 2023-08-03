using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GoToSingleplayer : BaseButton
{
    protected override void Setup()
    {
        base.Setup();
        this.text.text = UIStrings.Singleplayer;
        this.text.fontSize = UIUtils.MediumFontSize;
    }
    protected override void Execute()
    {
        //BaseMenu.SwitchToPreviousMenu();
        BaseMenu.SwitchToMenu(typeof(SingleplayerMenu));
    }
}
