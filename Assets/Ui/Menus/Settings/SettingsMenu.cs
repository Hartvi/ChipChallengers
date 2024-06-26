using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SettingsMenu : BaseMenu, InputReceiver
{

    /*
    frame-rate
    physics-rate
    volume
    particle amount
     */
    public override void Setup()
    {
        base.Setup();
        this.vProp = new VirtualProp(PropType.Panel, 1f, down,
            new VirtualProp(PropType.Image, 1f, down,
                new VirtualProp(PropType.Panel, 0.1f),
                new VirtualProp(PropType.Text, 0.1f),
                new VirtualProp(PropType.Panel, 0.1f),
                new VirtualProp(PropType.Panel, -1f, right,
                    new VirtualProp(PropType.Panel, 0.2f),
                    new VirtualProp(PropType.Panel, 0.6f, right, typeof(SettingFields))
                )
            )
        );
    }

    protected override void Start()
    {
        base.Start();

        // set first text to the title
        BaseText bt = this.GetComponentInChildren<BaseText>();
        bt.text.SetText(UIStrings.Settings);
        bt.text.fontSize = UIUtils.LargeFontSize;
        // set it to center and not to break text into a new line
        DisplaySingleton.NoOverflowEtc(bt.text);

        this.selectedCallbacks.SetCallbacks(new Action[] { () => UIManager.instance.SwitchToMe(this) });
        this.selectedCallbacks.Invoke();
    }

    void InputReceiver.OnStartReceiving() { }
    void InputReceiver.OnStopReceiving() { }
    bool InputReceiver.IsActive() => this.gameObject.activeSelf;

    void InputReceiver.HandleInputs()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            //print($"GOING TO MAIN MENU FROM SETTINGS");
            GoToMainMenu.Function(false);
        }
    }
    
}
