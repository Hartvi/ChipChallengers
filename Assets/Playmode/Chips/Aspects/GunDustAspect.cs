using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GunDustAspect : BaseAspect
{
    GunAspect gunAspect;
    float oldCharge = 0f;
    ParticleSystem particles;

    protected override void Awake()
    {
        base.Awake();
        this.particles = Instantiate(Resources.Load<ParticleSystem>(UIStrings.JetDust));
        this.particles.gameObject.SetActive(true);
        this.particles.Stop();
        var m = this.particles.main;
        m.duration = 0.01f;
        m.loop = false;
        m.startLifetime = 0.01f;
        particles.transform.position = Vector3.zero;
        particles.transform.SetParent(this.transform, false);
        Quaternion q = Quaternion.Euler(0f, 0f, 0f);
        particles.transform.localRotation = q;
    }

    void Start()
    {
        this.gunAspect = this.GetComponent<GunAspect>();
        var em = this.particles.emission;
        em.rateOverTime = Mathf.Abs(this.value);
        var m = this.particles.main;
        m.startSpeed = Mathf.Abs(this.value * 0.1f);
        m.startSize = 0.1f * this.value;
    }

    public override void RuntimeFunction()
    {
        if(this.gunAspect == null) { return; }
        if (this.gunAspect.charge < this.oldCharge)
        {
            // TODO FIRE
            //var em = this.particles.emission;
            //em.rateOverTime = Mathf.Min(Mathf.Abs(this.value * 0.1f), 100f);
            var m = this.particles.main;
            m.startLifetime = 0.051f;
            this.particles.Play();
        }
        this.oldCharge = this.gunAspect.charge;
    }

    void OnDestroy()
    {
        if (this.particles)
        {
            Object.Destroy(this.particles.gameObject);
        }
    }
}
