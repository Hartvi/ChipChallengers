using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SetVolumeSlider : BaseSlider
{
    protected override void Execute(float x)
    {
        AudioManager.volume = x;
        PlayerPrefs.SetFloat("Volume", x);
    }

}
