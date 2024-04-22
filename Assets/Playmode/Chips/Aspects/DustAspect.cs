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

    void OnCollisionEnter(Collision collision)
    {
        this.particles.transform.position = this.transform.position;
        this.particles.gameObject.SetActive(true);
        this.particles.Play();
    }

    void OnDestroy()
    {
        if (this.particles)
        {
            Object.Destroy(this.particles.gameObject);
        }
    }

}
