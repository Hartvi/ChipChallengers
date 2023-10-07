using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class JetAspect : BaseAspect
{
    void FixedUpdate()
    {
        //print($"adding force: {this.value}");
        this.rb.AddForce(this.transform.up * this.value);
    }
}
