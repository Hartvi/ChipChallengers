using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class QuitGame : BaseButton
{
    void Start()
    {
        this.text.SetText("Quit");
        UIUtils.SetFontSize(this.text, UIUtils.MediumFontSize);
    }
    protected override void Execute()
    {
        Application.Quit();
    }
}
