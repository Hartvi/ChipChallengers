using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//public class UDCLASS { }
//public class UP : UDCLASS { }
//public class DOWN : UDCLASS { }

/// <summary>
/// Must have ONLY ONE neighbour. Will drop down or up depending on where it is.
/// </summary>
public class DropdownItemScroll : BasePanel
{
    TopProp src;
    public BaseItemScroll itemScroll;
    public BaseScrollbar bScrollbar;

    public override void Setup()
    {
        base.Setup();
        this.vProp = new VirtualProp(PropType.Panel, 1f, right,
            new VirtualProp(PropType.Panel, 0.9f, typeof(BaseItemScroll),
                new VirtualProp(PropType.Button, 1f)
            ),
            new VirtualProp(PropType.Scrollbar, -1f, right, right)
        );
    }

    protected virtual void Start()
    {
        //this.src = this.Siblings<TopProp>(false)[0];
        this.itemScroll = this.GetComponentInChildren<BaseItemScroll>();
        this.bScrollbar = this.itemScroll.Siblings<BaseScrollbar>(false)[0];

        this.bScrollbar.scrollbar.onValueChanged.AddListener(this.itemScroll.Scroll);

        // IN INHERITED START:
        //this.itemScroll.SetupItemList(this.SelectVariable, VariablePanel.NUMDISPLAYITEMS, this.myLabels);
        //this.itemScroll.Scroll(0f);
        //this.bScrollbar.scrollbar.value = 0f;

    }
}
