using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BaseSoundAspect : BaseAspect
{
    protected AudioClip audioClip;
    protected ObjectPool<AudioSource> soundPool;
    protected int NumberOfSounds = 4;

    protected virtual void Start()
    {
        this.soundPool = new ObjectPool<AudioSource>(this.NumberOfSounds, () => this.gameObject.AddComponent<AudioSource>(), (x) => Destroy(x));
    }

}
