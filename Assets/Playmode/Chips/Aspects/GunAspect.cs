using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GunAspect : BaseAspect
{
    void Update()
    {
        if(this.brake > 1e-9f)
        {
            this.Fire();
        }
        print($"Brake: {this.brake}");
    }

    void Fire()
    {
        this.rb.AddForce(this.transform.forward * 10000f);
        Debug.DrawLine(this.transform.position, this.transform.position + this.transform.forward*2f);
    }
}
