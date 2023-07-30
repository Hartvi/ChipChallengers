using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StcControlsMenu : BaseMenu
{
    protected override void Setup()
    {
        vProp = new VirtualProp(
            new VirtualProp(PropType.Image,
                new VirtualProp(PropType.Panel, 1f, Vector2Int.down, typeof(ControlsStrings),
                    new VirtualProp(PropType.Panel, 0.1f),
                    new VirtualProp(PropType.Text, 0.1f),
                    new VirtualProp(PropType.Panel, 0.6f, Vector2Int.right, 
                        new VirtualProp(PropType.Panel, 0.1f),
                        new VirtualProp(PropType.Text, 0.4f),
                        new VirtualProp(PropType.Text, 0.4f)
                        ),
                    new VirtualProp(PropType.Panel, 0.1f, Vector2Int.right,
                        new VirtualProp(PropType.Panel, 0.4f),
                        new VirtualProp(PropType.Button, 0.2f, typeof(GoToPreviousMenu))
                        )
                )
            )
            );
    }
}
