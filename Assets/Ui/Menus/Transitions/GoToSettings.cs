using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GoToSettings : BaseTransition
{

    public override void Setup()
    {
        base.Setup();
        this.text.text = UIStrings.Settings;
        UIUtils.SetFontSize(this.text, UIUtils.MediumFontSize);
    }

    protected override void Execute()
    {
        GoToSettings.Function();
    }

    public static void Function()
    {
        BaseMenu.SwitchToMenu(typeof(SettingsMenu));

        BaseTransition.InvokeAfterClickedCallbacks(typeof(GoToSettings));
    }

}

