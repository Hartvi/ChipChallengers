using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GunSoundAspect : BaseSoundAspect
{
    public GunAspect gunAspect;

    protected override void Awake()
    {
        base.Awake();
        this.audioClip = Resources.Load<AudioClip>(UIStrings.Sounds + "/gun");
        this.NumberOfSounds = 8;
    }

    protected override void Start()
    {
        base.Start();
        foreach (var s in this.soundPool.objects)
        {
            s.clip = this.audioClip;
        }
        this.gunAspect.Fired += this.PlayShot;
    }

    void PlayShot()
    {
        if (!this.isActiveAndEnabled) { return; }
        var s = this.soundPool.Next();
        s.volume = GameManager.RealTimeSettings.Volume;
        s.Play();
    }

    protected override void OnDestroy()
    {
        if (this.gunAspect is not null) { this.gunAspect.Fired -= this.PlayShot; }
        base.OnDestroy();
        // normally has to be done when this spawns extra objects
        //this.soundPool.DeleteObjects();
    }
}
