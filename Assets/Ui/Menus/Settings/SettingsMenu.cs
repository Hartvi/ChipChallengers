using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SettingsMenu : BaseMenu
{

    /*
    frame-rate
    physics-rate
    volume
    particle amount
     */
    protected override void Setup()
    {
        base.Setup();
        this.vProp = new VirtualProp(PropType.Image, 1f, down,
            new VirtualProp(PropType.Panel, 0.1f),
            new VirtualProp(PropType.Text, 0.1f),
            new VirtualProp(PropType.Panel, 0.1f),
            new VirtualProp(PropType.Panel, -1f, right,
                new VirtualProp(PropType.Panel, 0.2f),
                new VirtualProp(PropType.Panel, 0.6f, right, typeof(SettingFields))
            )
        );
    }

    protected override void Start()
    {
        base.Start();

        // set first text to the title
        BaseText bt = this.GetComponentInChildren<BaseText>();
        bt.text.SetText(UIStrings.Settings);
        // set it to center and not to break text into a new line
        DisplaySingleton.NoOverflowEtc(bt.text);
    }
    
}
