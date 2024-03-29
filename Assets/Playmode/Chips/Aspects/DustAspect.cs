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
        this.particles.gameObject.SetActive(true);
    }

    void OnCollisionEnter(Collision collision)
    {
        this.particles.transform.position = this.transform.position;
        //this.particles.transform.rotation = this.transform.rotation;
        this.particles.gameObject.SetActive(true);
        this.particles.Play();
    }

}
