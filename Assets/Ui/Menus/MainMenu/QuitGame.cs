using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class QuitGame : BaseButton
{
    void Start()
    {
        this.text.SetText("Quit");
        this.text.fontSize = UIUtils.MediumFontSize;
    }
    protected override void Execute()
    {
        Application.Quit();
    }
}
