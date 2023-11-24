using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GoToSettings : BaseTransition
{

    protected override void Setup()
    {
        base.Setup();
        this.text.text = UIStrings.Settings;
        this.text.fontSize = UIUtils.MediumFontSize;
    }

    protected override void Execute()
    {
        GoToSettings.Function();
    }

    new public static void Function()
    {
        BaseMenu.SwitchToMenu(typeof(SettingsMenu));

        BaseTransition.InvokeAfterClickedCallbacks(typeof(GoToSettings));
    }
}
