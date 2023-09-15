using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using TMPro;

public class BaseScrollMenu : BasePanel
{
    //public const int NUMDISPLAYITEMS = 7;
    //protected string[] myLabels = new string[] { };

    protected BaseItemScroll itemScroll;
    protected BaseScrollbar scrollbar;

    protected override void Setup()
    {
        base.Setup();
        this.vProp = new VirtualProp(PropType.Panel, 1f,
            new VirtualProp(PropType.Panel, 0.8f, right,
                new VirtualProp(PropType.Panel, 0.9f, typeof(BaseItemScroll),
                    new VirtualProp(PropType.Button, 1f)
                ),
                new VirtualProp(PropType.Scrollbar, -1f, right, right)
            ),
            new VirtualProp(PropType.Image, -1f, 
                new VirtualProp(PropType.Button, 0.5f),
                new VirtualProp(PropType.Button, 0.5f)
            )
        );
    }

    void Start()
    {
        this.itemScroll = this.GetComponentInChildren<BaseItemScroll>();

        // example of how to setup the scroller
        //this.itemScroll.SetupItemList(x => UnityEngine.Debug.Log(x), VariablePanel.NUMDISPLAYITEMS, this.myLabels);

        this.scrollbar = this.itemScroll.Siblings<BaseScrollbar>(false)[0];
    }

}
