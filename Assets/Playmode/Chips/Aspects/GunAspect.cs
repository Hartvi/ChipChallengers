using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GunAspect : BaseAspect
{
    float _charge = 0f;
    public float charge => _charge;
    float power = 1f;
    float dPowerPerFrame = 1f;

    float projectileWeight;
    float projectileVelocity;
    float bulletWeightPerTime;
    float recoilImpulse;

    ObjectPool<Bullet> bulletPool;

    protected override void Awake()
    {
        base.Awake();
        this.power = this.value;
        //print($"gun power: {this.power}");

        this.projectileWeight = 1e-6f * this.power;
        this.projectileVelocity = 333f;
        Debug.LogWarning($"TODO: update bullet strengths when physics rate is changed");

        this.bulletWeightPerTime = this.projectileWeight / Time.fixedDeltaTime;
        this.recoilImpulse = this.bulletWeightPerTime * this.projectileVelocity;
        // dp/dt = (dm*dv)/dt = m*dv/dt 
        // dv = vel, dt = fixeddeltatime, m = projectileweight

        float dpowerdt = 1e3f / this.power;
        this.dPowerPerFrame = dpowerdt;

        // rate = ???
        // rate = 1/dpowerdt
        // rate = 5 bullets/second
        // lifetime = 5 seconds
        // number of bullets = rate * lifetime
        //print($"Time.deltaTime {Time.deltaTime} this.dPowerPerFrame {this.dPowerPerFrame} Bullet.LifeTime {Bullet.LifeTime}");
        int numberOfBullets = Mathf.CeilToInt(1f / this.dPowerPerFrame * Bullet.LifeTime);
        //print($"Number of bullets: {numberOfBullets}");
        this.bulletPool = new ObjectPool<Bullet>(numberOfBullets, GenerateBullet, x =>
        {
            if (x)
            {
                Destroy(x.gameObject);
            }
        }
        );
    }

    void Update()
    {
        if (this._charge < 1f)
        {
            this._charge += this.dPowerPerFrame * Time.deltaTime;
            print($"Charge: {this.charge}");
        } // if charge is full then check if shooting
        else if (this.brake > 1e-9f)
        {
            this.Fire();
        }
    }

    void Fire()
    {
        // charge is full, shoot
        Vector3 forward = this.transform.forward;
        this.rb.AddForce(-forward * this.recoilImpulse, ForceMode.Impulse);
        this._charge = 0f;

        Vector3 initialBulletPosition = this.transform.position + forward * GeometricChip.ChipSide;

        this.bulletPool.Next().Fire(initialBulletPosition, forward * this.projectileVelocity);
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
        this.bulletPool.DeleteObjects();
    }
}
