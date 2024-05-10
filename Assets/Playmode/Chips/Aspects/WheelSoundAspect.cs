using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WheelSoundAspect : BaseSoundAspect
{
    int everyNth = 10;
    int everyNthCounter = 0;
    protected override void Awake()
    {
        base.Awake();
        this.audioClip = Resources.Load<AudioClip>(UIStrings.Sounds + "/engine");
        this.NumberOfSounds = 5;
    }

    protected override void Start()
    {
        base.Start();
        foreach (var s in this.soundPool.objects)
        {
            s.clip = this.audioClip;
        }
    }

    void Update()
    {
        if (everyNthCounter++ >= everyNth)
        {
            var s = this.soundPool.Next();
            s.volume = GameManager.RealTimeSettings.Volume * Mathf.Min(Mathf.Max(0f, this.value * 0.03f), 0.8f);
            s.Play();
            everyNthCounter = 0;
        }
    }

    void OnDestroy()
    {
        // normally has to be done when this spawns extra objects
        //this.soundPool.DeleteObjects();
    }
}
