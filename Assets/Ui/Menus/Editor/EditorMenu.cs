using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EditorMenu : BaseMenu
{
    protected override void Setup()
    {
        base.Setup();
        this.vProp = new VirtualProp(PropType.Panel, 1f, right,
            new VirtualProp(PropType.Panel, 0.5f, down,
                new VirtualProp(PropType.Panel, 0.05f, right,
                    new VirtualProp(PropType.Button, 1/3f),
                    new VirtualProp(PropType.Button, 1/3f),
                    new VirtualProp(PropType.Button, 1/3f)
                )
            ),
            new VirtualProp(PropType.Panel, -1f
            )
        );
    }
}
