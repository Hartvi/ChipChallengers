using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EditorMenu : BaseMenu
{
    protected override void Setup()
    {
        base.Setup();
        this.vProp = new VirtualProp(PropType.Panel, 1f, right,
            new VirtualProp(PropType.Panel, 0.25f, down,
                new VirtualProp(PropType.Panel, 0.1f, right,
                    new VirtualProp(PropType.Button, 1/3f),
                    new VirtualProp(PropType.Button, 1/3f),
                    new VirtualProp(PropType.Button, -1f)
                ),
                new VirtualProp(PropType.Panel, -1f, zero,
                    new VirtualProp(PropType.Panel, 1f,  // Chip panel
                        new VirtualProp(PropType.Panel, 0.5f, right,
                            new VirtualProp(PropType.Panel, 0.8f, typeof(BaseItemScroll),
                                new VirtualProp(PropType.Button, 1f)
                            ),
                            new VirtualProp(PropType.Scrollbar, -1f, right, right)
                        )
                    )
                    //new VirtualProp(PropType.Panel, 1f,  // Variable panel
                    //    new VirtualProp(PropType.Button, 1f)
                    //),
                    //new VirtualProp(PropType.Panel, 1f,  // Controls panel
                    //    new VirtualProp(PropType.Button, 1f)
                    //),
                    //new VirtualProp(PropType.Panel, 1f,  // Script panel
                    //    new VirtualProp(PropType.Button, 1f)
                    //)
                )
            ),
            new VirtualProp(PropType.Panel, -1f
            )
        );
    }

    protected override void Start()
    {
        base.Start();

        // test
        //Action<string> action = x => UnityEngine.Debug.Log(x);
        //int numItems = 3;
        //string[] items = { "a", "b", "c", "d", "e", "f", "g", "h" };
        //this.GetComponentInChildren<BaseItemScroll>().SetupItemList(action, numItems, items);
    }
}
