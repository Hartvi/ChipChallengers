using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class JetDustAspect : BaseAspect
{
    ParticleSystem particles;
    float oldVal = 0f;

    protected override void Awake()
    {
        base.Awake();
        this.particles = Instantiate(Resources.Load<ParticleSystem>(UIStrings.JetDust));
        this.particles.gameObject.SetActive(true);
        particles.transform.position = Vector3.zero;
        //particles.transform.rotation = Quaternion.Euler(Vector3.up);
        particles.transform.SetParent(this.transform, false);
        Quaternion q = Quaternion.Euler(90f, 0f, 0f);
        particles.transform.localRotation = q;
    }

    void Update()
    {
        var em = this.particles.emission;
        em.rateOverTime = Mathf.Min(Mathf.Abs(this.value * 0.1f), 100f);
        var m = this.particles.main;
        m.startSpeed = Mathf.Abs(this.value * 1f);
        if (this.value > 0 && this.oldVal <= 0)
        {
            this.particles.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        }
        else
        if (this.value < 0 && this.oldVal >= 0)
        {
            Quaternion q = Quaternion.Euler(-90f, 0f, 0f);
            this.particles.transform.localRotation = q;
        }
        this.oldVal = this.value;
    }

    void OnDestroy()
    {
        if (this.particles)
        {
            Object.Destroy(this.particles.gameObject);
        }
    }
}
