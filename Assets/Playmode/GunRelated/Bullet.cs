using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bullet : MonoBehaviour
{
    public const float LifeTime = 5f;

    float myPower;
    float bulletWeightPerTime;

    float myLifeTime;
    float myRayMagnitude;
    Rigidbody rb;
    Ray myRay = new Ray();

    public void SetupBullet(float bulletWeightPerTime, float power)
    {
        this.bulletWeightPerTime = bulletWeightPerTime;
        this.myPower = power;

        this.gameObject.SetActive(false);
    }

    public void Fire(Vector3 initialPosition, Vector3 velocity)
    {
        this.rb = this.GetComponent<Rigidbody>();
        this.rb.velocity = velocity;
        //this.rb.position = initialPosition;
        this.transform.position = initialPosition;
        this.myRayMagnitude = this.rb.velocity.magnitude * Time.deltaTime;
        // make it last some time
        this.myLifeTime = Bullet.LifeTime;

        this.gameObject.SetActive(true);
    }

    void Update()
    {
        this.myLifeTime = this.myLifeTime - Time.deltaTime;

        var vel = this.rb.velocity;
        var pos = this.transform.position;

        this.myRay.direction = vel;
        this.myRay.origin = pos;

        Debug.DrawLine(pos, pos + vel.normalized * this.myRayMagnitude, Color.red, 0.000005f);
        if (Physics.Raycast(this.myRay, out RaycastHit hitInfo, this.myRayMagnitude))
        {
            // apply force to rigidbody that was hit
            if (hitInfo.rigidbody is not null)
            {
                hitInfo.rigidbody.AddForceAtPosition(vel * this.bulletWeightPerTime, pos, ForceMode.Impulse);
            }

            // apply damage to live object that was hit
            HealthAspect h = hitInfo.collider.gameObject.GetComponent<HealthAspect>();
            if (h is not null)
            {
                h.ApplyDamage(this.myPower);
            }
        }

        if(this.myLifeTime <= 0f)
        {
            this.gameObject.SetActive(false);
        }
    }
}
