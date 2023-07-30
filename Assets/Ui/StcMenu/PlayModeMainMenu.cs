using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayModeMainMenu : BaseMainMenu
{
    protected override void Setup()
    {
        vProp = new VirtualProp(
            new VirtualProp(PropType.Panel, 1f, Vector2Int.right)
        );
    }
    protected override void Start()
    {
        base.Start();
        SwitchToMenu(typeof(LoadingMenu));
    }
    
}
