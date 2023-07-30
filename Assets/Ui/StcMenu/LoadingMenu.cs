using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LoadingMenu : BaseMenu
{
    protected override void Setup()
    {
        vProp = new VirtualProp(
            new VirtualProp(PropType.Image, 1f, Vector2Int.down,
                new VirtualProp(PropType.Panel, 0.3f),
                new VirtualProp(PropType.Text, 0.4f, typeof(LoadingText))
            )
        );
    }
}
