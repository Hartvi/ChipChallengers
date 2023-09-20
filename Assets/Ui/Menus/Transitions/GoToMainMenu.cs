using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GoToMainMenu : BaseTransition
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
        GoToMainMenu.Function();
    }

    new public static void Function()
    {
        BaseMenu.SwitchToMenu(typeof(MainMenu));

        BaseTransition.InvokeAfterClickedCallbacks(typeof(GoToMainMenu));
    }
}
