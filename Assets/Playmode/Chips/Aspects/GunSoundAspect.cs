using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GunSoundAspect : BaseSoundAspect
{
    GunAspect gunAspect;
    float oldCharge = 0f;

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
        this.gunAspect = this.GetComponent<GunAspect>();
    }

    void Update()
    {
        if (this.gunAspect.charge < this.oldCharge)
        {
            var s = this.soundPool.Next();
            s.volume = GameManager.RealTimeSettings.Volume;
            s.Play();
        }
        this.oldCharge = this.gunAspect.charge;
    }

    void OnDestroy()
    {
        // normally has to be done when this spawns extra objects
        //this.soundPool.DeleteObjects();
    }
}
