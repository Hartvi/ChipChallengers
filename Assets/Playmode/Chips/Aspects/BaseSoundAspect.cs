using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BaseSoundAspect : BaseAspect
{
    protected AudioClip audioClip;
    protected ObjectPool<AudioSource> soundPool;
    protected int NumberOfSounds = 4;

    protected override void Start()
    {
        base.Start();
        this.soundPool = new ObjectPool<AudioSource>(this.NumberOfSounds, () => this.gameObject.AddComponent<AudioSource>(), source => {
            if (source == null) { return; }
            source.Stop();
            if (Application.isPlaying) { Destroy(source); }
            else { DestroyImmediate(source); }
        });
    }

    public override void RuntimeFunction()
    {
    }

    void OnDisable()
    {
        if (this.soundPool == null) { return; }
        foreach (var source in this.soundPool.objects)
            if (source != null) { source.Stop(); }
    }

    protected override void OnDestroy()
    {
        this.soundPool?.DeleteObjects();
        this.soundPool = null;
        base.OnDestroy();
    }


}
