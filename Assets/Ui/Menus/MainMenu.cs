using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MainMenu : DeclaredProp
{
    protected override VirtualProp GetVProp()
    {
        return new VirtualProp(PropType.Panel, 1f, Vector2Int.down,
            new VirtualProp(PropType.Image, 1f, Vector2Int.down,
                new VirtualProp(PropType.Panel, 0.3f),
                new VirtualProp(PropType.Text, 0.4f, typeof(LoadingText))
            )
        );
    }
}
