using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HitSoundAspect : BaseSoundAspect
{
    protected override void Awake()
    {
        base.Awake();
        this.audioClip = Resources.Load<AudioClip>(UIStrings.Sounds + "/hit");
        this.NumberOfSounds = 1;
    }

    protected override void Start()
    {
        base.Start();
        foreach (var s in this.soundPool.objects)
        {
            s.clip = this.audioClip;
        }
    }
    void OnCollisionEnter(Collision collision)
    {
        var s = this.soundPool.Next();
        s.volume = GameManager.RealTimeSettings.Volume;
        s.Play();
    }
}
