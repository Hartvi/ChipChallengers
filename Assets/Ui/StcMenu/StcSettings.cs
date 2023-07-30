using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StcSettings : BaseMenu
{
    protected override void Setup()
    {
        vProp = new VirtualProp(
            new VirtualProp(PropType.Image, 1f, Vector2Int.right,
                new VirtualProp(PropType.Panel, 0.1f),
                new VirtualProp(PropType.Panel, 0.8f, Vector2Int.down, typeof(SettingsHandler),
                    new VirtualProp(PropType.Panel, 0.1f),
                    new VirtualProp(PropType.Text, 0.1f),
                    new VirtualProp(PropType.Panel, 0.6f, Vector2Int.right,
                        new VirtualProp(PropType.Panel, 0.5f, Vector2Int.down,
                            new VirtualProp(PropType.Text, 0.167f)
                            ),
                        new VirtualProp(PropType.Panel, 0.5f, Vector2Int.down,
                            new VirtualProp(PropType.Slider, 0.167f, typeof(SetVolumeSlider))
                            )
                        ),
                    new VirtualProp(PropType.Button, 0.1f, typeof(GoToPreviousMenu))
                    )
                )
            );
    }


}
