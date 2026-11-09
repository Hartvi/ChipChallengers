using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GoToEditor : BaseTransition
{

    public override void Setup()
    {
        base.Setup();
        this.text.text = UIStrings.Editor;
        UIUtils.SetFontSize(this.text, UIUtils.MediumFontSize);
    }

    protected override void Execute()
    {
        //BaseMenu.SwitchToPreviousMenu();
        GoToEditor.Function();
    }

    public static void Function()
    {
        GameManager.isHost = true;
        BaseMenu.SwitchToMenu(typeof(EditorMenu));

        BaseTransition.InvokeAfterClickedCallbacks(typeof(GoToEditor));
    }
}

