using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TireSoundAspect : BaseSoundAspect
{
    WheelAspects wheelAspect;
    int everyNthCounter = 0;
    int everyNth = 5;
    const float slipOffset = 1.2f;

    protected override void Awake()
    {
        base.Awake();
        this.audioClip = Resources.Load<AudioClip>(UIStrings.Sounds + "/tire");
        this.NumberOfSounds = 8;
    }

    protected override void Start()
    {
        base.Start();
        foreach (var s in this.soundPool.objects)
        {
            s.clip = this.audioClip;
        }
        this.wheelAspect = this.GetComponent<WheelAspects>();
    }

    public override void RuntimeFunction()
    {
        if (everyNthCounter++ >= everyNth)
        {
            everyNthCounter = 0;
            if (this.wheelAspect.totalSlip > slipOffset)
            {
                var s = this.soundPool.Next();
                s.volume = GameManager.RealTimeSettings.Volume * Mathf.Min(0.5f, this.wheelAspect.totalSlip - slipOffset);
                s.Play();
            }
        }
    }

    void OnDestroy()
    {
        // normally has to be done when this spawns extra objects
        //this.soundPool.DeleteObjects();
    }
}
