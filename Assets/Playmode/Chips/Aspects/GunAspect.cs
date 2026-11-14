using System;
using System.Collections.Generic;
using UnityEngine;

public class GunAspect : BaseAspect
{
    public event Action Fired;
    const float ProjectileVelocity = 333f;
    const int MaximumPooledBullets = 256;
    readonly GunFireControl fireControl = new GunFireControl();
    readonly List<Bullet> bullets = new List<Bullet>();
    int recycleIndex;
    public float charge => this.fireControl.Charge;
    public float LastShotPower { get; private set; }

    public override void RuntimeFunction() { }

    void FixedUpdate()
    {
        if (!this.myChip.isServer || this.myChip.IsDead || this.rb.isKinematic || this.myChip.myCore.IsRebuilding) { return; }
        if (!this.fireControl.Step(this.value, this.brake > 1e-4f, Time.fixedDeltaTime, out float power)) { return; }
        Vector3 velocity = this.transform.forward * ProjectileVelocity;
        this.rb.AddForce(-velocity * GunFireControl.ProjectileMass(power) * GunFireControl.RecoilScale, ForceMode.Impulse);
        this.PlayShot(this.transform.position, velocity, power, true);
        this.myChip.myCore.RpcPlayGunShot(this.myChip.netId, this.transform.position, velocity, power);
    }

    public void PlayRemoteShot(Vector3 position, Vector3 velocity, float power)
    {
        if (this.myChip.isServer || !GunFireControl.TryPower(power, out power)) { return; }
        this.PlayShot(position, velocity, power, false);
    }

    void PlayShot(Vector3 position, Vector3 velocity, float power, bool authoritative)
    {
        this.bullets.RemoveAll(bullet => bullet == null);
        Bullet available = this.bullets.Find(bullet => !bullet.gameObject.activeSelf);
        if (available == null)
        {
            if (this.bullets.Count < MaximumPooledBullets)
            {
                available = Instantiate(Resources.Load<GameObject>(UIStrings.GunRelated + "/" + UIStrings.Bullet)).AddComponent<Bullet>();
                available.SetupBullet(power);
                this.bullets.Add(available);
            }
            else
            {
                available = this.bullets[this.recycleIndex];
                this.recycleIndex = (this.recycleIndex + 1) % MaximumPooledBullets;
            }
        }
        available.SetPower(power);
        available.Fire(position, velocity, this.rb, this.myChip.myCore, authoritative);
        this.LastShotPower = power;
        this.Fired?.Invoke();
    }

    protected override void OnDestroy()
    {
        foreach (var bullet in this.bullets)
        {
            if (bullet == null) { continue; }
            bullet.gameObject.SetActive(false);
            if (Application.isPlaying) { Destroy(bullet.gameObject); }
            else { DestroyImmediate(bullet.gameObject); }
        }
        this.bullets.Clear();
        base.OnDestroy();
    }
}
