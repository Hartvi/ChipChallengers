using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class SingleplayerMenu : BaseMenu
{
    // HUD, etc
    // km/h m/s variables
    protected override void Setup()
    {
        base.Setup();
        this.vProp = new VirtualProp(PropType.Panel, 1f, zero,
            new VirtualProp(PropType.Panel, 1f, down, typeof(IntroScreen),
                new VirtualProp(PropType.Panel, 0.2f),
                new VirtualProp(PropType.Text, 0.2f, typeof(MainTitle)),
                new VirtualProp(PropType.Panel, 0.2f, right,
                    new VirtualProp(PropType.Panel, 0.3f),
                    new VirtualProp(PropType.Text, 0.1f),
                    new VirtualProp(PropType.Text, 0.3f)
                    )
            ),
            new VirtualProp(PropType.Panel, 1f, right, typeof(HUD),
                new VirtualProp(PropType.Panel, 0.1f, up,
                    new VirtualProp(PropType.Text, 0.05f, typeof(ItemBase))
                ),
                new VirtualProp(PropType.Panel, 0.1f, up,
                    new VirtualProp(PropType.Text, 0.05f, typeof(ItemBase))
                )
                //new VirtualProp(PropType.Text, 0.2f, typeof(MainTitle))
            )
            //new VirtualProp(PropType.Panel, 1f, Vector2Int.down,
            //    new VirtualProp(PropType.Panel, 0.2f),
            //    new VirtualProp(PropType.Text, 0.2f, typeof(MainTitle))
                //new VirtualProp(PropType.Button, 0.2f, typeof(GoToSingleplayer))
                //new VirtualProp(PropType.Button, 0.2f, typeof(GoToMultiplayer)),
                //new VirtualProp(PropType.Button, 0.2f, typeof(GoToEditor)),
                //new VirtualProp(PropType.Button, 0.2f, typeof(GoToOptions)),
            //)
        );
    }
    protected override void Start()
    {
        base.Start();
        
    }

}
