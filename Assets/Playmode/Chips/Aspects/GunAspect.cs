using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GunAspect : BaseAspect
{
    float charge = 0f;
    float power;
    float dPowerPerFrame;

    float projectileWeight;
    float projectileVelocity;
    float bulletWeightPerTime;
    float recoilImpulse;

    ObjectPool<Bullet> bulletPool;

    protected override void Awake()
    {
        base.Awake();
        this.power = this.value;
        print($"gun power: {this.power}");

        this.projectileWeight = 1e-6f * this.power;
        this.projectileVelocity = 33f;
        this.bulletWeightPerTime = this.projectileWeight / Time.fixedDeltaTime;
        this.recoilImpulse = this.bulletWeightPerTime * this.projectileVelocity;
        // dp/dt = (dm*dv)/dt = m*dv/dt 
        // dv = vel, dt = fixeddeltatime, m = projectileweight

        float dpowerdt = 1e3f / this.power;
        this.dPowerPerFrame = dpowerdt * Time.deltaTime;

        // rate = ???
        // rate = 1/dpowerdt
        // rate = 5 bullets/second
        // lifetime = 5 seconds
        // number of bullets = rate * lifetime
        int numberOfBullets = Mathf.CeilToInt(Time.deltaTime / this.dPowerPerFrame * Bullet.LifeTime);
        print($"Number of bullets: {numberOfBullets}");
        this.bulletPool = new ObjectPool<Bullet>(numberOfBullets, GenerateBullet);
    }

    void Update()
    {
        if (this.charge < 1f)
        {
            this.charge += this.dPowerPerFrame;
            //print($"Charge: {this.charge}");
        } // if charge is full then check if shooting
        else if (this.brake > 1e-9f)
        {
            this.Fire();
        }
    }

    void Fire()
    {
        // charge is full, shoot
        this.rb.AddForce(-this.transform.forward * this.recoilImpulse, ForceMode.Impulse);
        this.charge = 0f;
        this.bulletPool.Next().Fire(this.transform.position, this.transform.forward * this.projectileVelocity);
        //Debug.DrawLine(this.transform.position, this.transform.position + this.transform.forward*2f);
    }

    Bullet GenerateBullet()
    {
        Bullet b = Instantiate(Resources.Load(UIStrings.GunRelated + "/" + UIStrings.Bullet) as GameObject).AddComponent<Bullet>();
        b.SetupBullet(this.bulletWeightPerTime, this.power);
        return b;
    }

    void OnDestroy()
    {
        foreach(var b in this.bulletPool.objects)
        {
            Destroy(b);
        }
    }
}
