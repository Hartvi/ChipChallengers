using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MoonSharp.Interpreter;

public enum SensorType
{
    Distance, Altitude, AngularVelocity, Rotation, Acceleration, Velocity
}

public class SensorAspect : BaseAspect
{
    public SensorType sensorType;
    RaycastHit hit;
    Vector3 oldVelocity = Vector3.zero;

    /*
     probably:
    on start up:
    - create a function that replaces chip_name.read() with :
       sensorType == Lidar => SensorAspect.ReadLidar()
    //*/

    void Update()
    {
        if (this.sensorType == SensorType.Acceleration)
        {
            this.oldVelocity = this.rb.velocity;
        }
    }

    public float ReadDistance()
    {
        Physics.Raycast(this.transform.position, this.transform.forward, out this.hit);
        // uncertainty grow by 1% for every ten meters
        return this.hit.distance * (1 + Random.value * 0.001f);
    }

    public float ReadAltitude()
    {
        return this.transform.position.y * (1 + Random.value * 0.001f);
    }

    public Table ReadAngularVelocity(Script script)
    {
        Table tbl = new Table(script);

        var w = this.rb.angularVelocity * (1 + Random.value * 0.001f);
        for (int i = 1; i <= 3; i++)
            tbl[i] = w[i - 1];

        return tbl;
    }

    public Table ReadAcceleration(Script script)
    {
        var a = (this.rb.velocity - this.oldVelocity) / Time.deltaTime;
        Table tbl = new Table(script);

        for (int i = 1; i <= 3; i++)
            tbl[i] = a[i - 1];

        return tbl;
    }

    public Table ReadRotation(Script script)
    {
        var f = this.transform.forward * (1 + Random.value * 0.001f);
        Table tbl = new Table(script);

        for (int i = 1; i <= 3; i++)
            tbl[i] = f[i - 1];

        return tbl;
    }

    public Table ReadVelocity(Script script)
    {
        var f = this.rb.velocity * (1 + Random.value * 0.01f);
        Table tbl = new Table(script);

        for (int i = 1; i <= 3; i++)
            tbl[i] = f[i - 1];

        return tbl;
    }

}

