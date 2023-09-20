using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GoToEditor : BaseTransition
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
        GoToEditor.Function();
    }

    new public static void Function()
    {
        BaseMenu.SwitchToMenu(typeof(EditorMenu));

        BaseTransition.InvokeAfterClickedCallbacks(typeof(GoToEditor));
    }
}

