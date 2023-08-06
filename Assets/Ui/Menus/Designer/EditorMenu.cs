using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EditorMenu : BaseMenu
{
    protected override void Setup()
    {
        base.Setup();
        this.vProp = new VirtualProp(PropType.Panel, 1f, left,
            new VirtualProp(PropType.Panel, 0.25f, up,
                new VirtualProp(PropType.Panel, 0.9f, down,
                    new VirtualProp(PropType.Panel, -1f, zero,
                        new VirtualProp(PropType.Panel, 1f, typeof(VariablePanel)  // Variable panel
                            //new VirtualProp(PropType.Panel, 0.5f, right,
                            //    new VirtualProp(PropType.Panel, 0.9f, typeof(BaseItemScroll),
                            //        new VirtualProp(PropType.Button, 1f)
                            //    ),
                            //    new VirtualProp(PropType.Scrollbar, -1f, right, right)
                            //),
                            //new VirtualProp(PropType.Image, -1f)
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
                new VirtualProp(PropType.Panel, -1f, right,
                    new VirtualProp(PropType.Button, 1/4f),
                    new VirtualProp(PropType.Button, 1/4f),
                    new VirtualProp(PropType.Button, 1/4f),
                    new VirtualProp(PropType.Button, -1f)
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
        //int numItems = 5;
        //string[] items = { "a", "b", "c", "d", "e", "f", "g", "h", "i", "j", "k", "l", "m", "n", "o", "p", "q", "r", "s", "t", "u", "v", "w", "x", "y", "z" };
        //var baseScroll = this.GetComponentInChildren<BaseItemScroll>();
        //print($"basescroll: {baseScroll}");
        //baseScroll.SetupItemList(action, numItems, items);
        //var scrollbar = baseScroll.Siblings<BaseScrollbar>(false)[0];
        //scrollbar.scrollbar.onValueChanged.AddListener(baseScroll.Scroll);
        //scrollbar.scrollbar.value = 0f;
    }
}
