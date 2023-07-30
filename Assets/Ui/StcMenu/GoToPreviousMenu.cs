using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class GoToPreviousMenu : BaseButton
{
    void Start()
    {
        text.SetText("Back");
    }

    protected override void Execute()
    {
        BaseMenu.SwitchToPreviousMenu();
    }
}
