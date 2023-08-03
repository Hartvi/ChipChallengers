using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class SingleplayerMenu : BaseMenu
{
    // HUD, etc
    // km/h m/s variables
    TMP_Text IntroText;
    TMP_Text F1Text;
    protected override void Setup()
    {
        base.Setup();
        this.vProp = new VirtualProp(PropType.Panel, 1f, Vector2Int.down,
            new VirtualProp(PropType.Panel, 1f, Vector2Int.down, typeof(IntroScreen),
                new VirtualProp(PropType.Panel, 0.2f),
                new VirtualProp(PropType.Panel, 0.2f, Vector2Int.right,
                    new VirtualProp(PropType.Panel, 0.3f),
                    new VirtualProp(PropType.Text, 0.2f),
                    new VirtualProp(PropType.Text, 0.2f)
                    )
            ),
            new VirtualProp(PropType.Panel, 1f, Vector2Int.down,
                new VirtualProp(PropType.Panel, 0.2f),
                new VirtualProp(PropType.Text, 0.2f, typeof(MainTitle))
            ),
            new VirtualProp(PropType.Panel, 1f, Vector2Int.down,
                new VirtualProp(PropType.Panel, 0.2f),
                new VirtualProp(PropType.Text, 0.2f, typeof(MainTitle))
                //new VirtualProp(PropType.Button, 0.2f, typeof(GoToSingleplayer))
                //new VirtualProp(PropType.Button, 0.2f, typeof(GoToMultiplayer)),
                //new VirtualProp(PropType.Button, 0.2f, typeof(GoToEditor)),
                //new VirtualProp(PropType.Button, 0.2f, typeof(GoToOptions)),
            )
        );
    }
    protected override void Start()
    {
        base.Start();
        
    }

}
