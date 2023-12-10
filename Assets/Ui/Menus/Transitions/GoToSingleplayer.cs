using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class GoToSingleplayer : BaseTransition
{

    protected override void Setup()
    {
        base.Setup();
        this.text.text = UIStrings.Singleplayer;
        this.text.fontSize = UIUtils.MediumFontSize;
    }

    protected override void Execute()
    {
        GoToSingleplayer.Function();
    }

    public static void Function()
    {
        BaseMenu.SwitchToMenu(typeof(SingleplayerMenu));

        BaseTransition.InvokeAfterClickedCallbacks(typeof(GoToSingleplayer));
    }
}

