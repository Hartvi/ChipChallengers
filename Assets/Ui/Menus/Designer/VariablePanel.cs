using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VariablePanel : BasePanel
{
    protected override void Setup()
    {
        base.Setup();
        this.vProp = new VirtualProp(PropType.Panel, 1f,
            new VirtualProp(PropType.Panel, 0.5f, right,
                new VirtualProp(PropType.Panel, 0.9f, typeof(BaseItemScroll),
                    new VirtualProp(PropType.Button, 1f)
                ),
                new VirtualProp(PropType.Scrollbar, -1f, right, right)
            ),
            new VirtualProp(PropType.Image, -1f)
        );
    }
    void Start()
    {
        Action<string> action = x => UnityEngine.Debug.Log(x);
        int numItems = 6;
        string[] items = { "a", "b", "c", "d", "e", "f", "g", "h", "i", "j", "k", "l", "m", "n", "o", "p", "q", "r", "s", "t", "u", "v", "w", "x", "y", "z" };
        var baseScroll = this.GetComponentInChildren<BaseItemScroll>();
        print($"basescroll: {baseScroll}");
        baseScroll.SetupItemList(action, numItems, items);
        var scrollbar = baseScroll.Siblings<BaseScrollbar>(false)[0];
        scrollbar.scrollbar.onValueChanged.AddListener(baseScroll.Scroll);
        baseScroll.Scroll(0f);
        scrollbar.scrollbar.value = 0f;
    }
}
