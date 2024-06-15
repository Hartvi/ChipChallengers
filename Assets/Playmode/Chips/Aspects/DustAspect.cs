using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DustAspect : BaseAspect
{
    ParticleSystem particles;

    protected override void Awake()
    {
        base.Awake();
        this.particles = Instantiate(Resources.Load<ParticleSystem>(UIStrings.Dust));
        this.particles.gameObject.SetActive(false);
    }

    public override void RuntimeFunction()
    {
    }

    void OnCollisionEnter(Collision collision)
    {
        this.particles.transform.position = this.transform.position;
        this.particles.gameObject.SetActive(true);
        var em = this.particles.emission;
        em.rateOverTime = 3f * GameManager.RealTimeSettings.ParticleRate;
        this.particles.Play();
    }

    void OnDestroy()
    {
        if (this.particles)
        {
            Object.DestroyImmediate(this.particles.gameObject);
        }
    }

}
