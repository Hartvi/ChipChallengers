using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GoToMainMenu : BaseTransition
{
    public override void Setup()
    {
        base.Setup();
        this.text.text = UIStrings.MainMenu;
        this.text.fontSize = UIUtils.MediumFontSize;
    }

    protected override void Execute()
    {
        //BaseMenu.SwitchToPreviousMenu();
        GoToMainMenu.Function(false);
    }

    new public static void Function(bool switchBack)
    {
        //print($"Switching to main menu");
        BaseMenu.SwitchToMenu(typeof(MainMenu), switchBack);

        BaseTransition.InvokeAfterClickedCallbacks(typeof(GoToMainMenu));
    }
}
