using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GunDustAspect : BaseAspect
{
    GunAspect gunAspect;
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

    protected override void Start()
    {
        base.Start();
        this.gunAspect = this.GetComponent<GunAspect>();
        this.gunAspect.Fired += this.PlayShot;
        var em = this.particles.emission;
        em.rateOverTime = GameManager.RealTimeSettings.ParticleRate * Mathf.Abs(this.value);
        var m = this.particles.main;
        m.startSpeed = Mathf.Abs(this.value * 0.1f);
        m.startSize = 0.1f * this.value;
    }

    public override void RuntimeFunction()
    {
    }

    void PlayShot()
    {
        if (!this.isActiveAndEnabled) { return; }
        float power = this.gunAspect.LastShotPower;
        var emission = this.particles.emission;
        emission.rateOverTime = GameManager.RealTimeSettings.ParticleRate * Mathf.Min(power, 1000f);
        var main = this.particles.main;
        main.startSpeed = Mathf.Min(power * 0.1f, 100f);
        main.startSize = Mathf.Min(0.1f * power, 1f);
        main.startLifetime = 0.051f;
        this.particles.Play();
    }

    protected override void OnDestroy()
    {
        if (this.gunAspect != null) { this.gunAspect.Fired -= this.PlayShot; }
        base.OnDestroy();
        if (this.particles)
        {
            GameObject.Destroy(this.particles.gameObject);
        }
    }
}
