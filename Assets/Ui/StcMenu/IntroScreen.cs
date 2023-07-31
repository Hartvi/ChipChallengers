using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IntroScreen : BaseMenu
{
    protected override void Setup()
    {
        //myProp = new VirtualProp(
        //    new VirtualProp(PropType.Panel, 1f, Vector2Int.right,
        //        new VirtualProp(PropType.Panel, 0.2f),
        //        new VirtualProp(PropType.Panel, 0.6f, Vector2Int.down, typeof(PlayIntro),
        //            new VirtualProp(PropType.Panel, 0.1f),
        //            new VirtualProp(PropType.RawImage, 0.4f),
        //            new VirtualProp(PropType.Panel, 0.4f, Vector2Int.right,
        //                new VirtualProp(PropType.Panel, 0.5f, Vector2Int.down,
        //                    new VirtualProp(PropType.Text, 0.4f),
        //                    new VirtualProp(PropType.Text, 0.4f)
        //                ),
        //                new VirtualProp(PropType.Panel, 0.5f, Vector2Int.down,
        //                    new VirtualProp(PropType.Text, 0.4f),
        //                    new VirtualProp(PropType.Text, 0.4f)
        //                )
        //            )
        //        )
        //    )
        //);
    }
    //protected override VirtualProp GetVProp()
    //{
    //    VirtualProp(
    //        new VirtualProp(PropType.Panel, 1f, Vector2Int.right,
    //            new VirtualProp(PropType.Panel, 0.2f),
    //            new VirtualProp(PropType.Panel, 0.6f, Vector2Int.down, typeof(PlayIntro),
    //                new VirtualProp(PropType.Panel, 0.1f),
    //                new VirtualProp(PropType.RawImage, 0.4f),
    //                new VirtualProp(PropType.Panel, 0.4f, Vector2Int.right,
    //                    new VirtualProp(PropType.Panel, 0.5f, Vector2Int.down,
    //                        new VirtualProp(PropType.Text, 0.4f),
    //                        new VirtualProp(PropType.Text, 0.4f)
    //                    ),
    //                    new VirtualProp(PropType.Panel, 0.5f, Vector2Int.down,
    //                        new VirtualProp(PropType.Text, 0.4f),
    //                        new VirtualProp(PropType.Text, 0.4f)
    //                    )
    //                )
    //            )
    //        )
    //    );
    //}
    protected override void Start()
    {
        base.Start();
        isSelected = true;
    }

    void Update()
    {
        if (Input.anyKeyDown)
        {
            SwitchToMenu(null);
        }
    }

}
