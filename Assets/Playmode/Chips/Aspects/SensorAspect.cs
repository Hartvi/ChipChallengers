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
    public const int NUMSENSORTYPES = 6;

    public SensorType sensorType;
    RaycastHit hit;
    Vector3 oldVelocity = Vector3.zero;

    /*
     probably:
    on start up:
    - create a function that replaces chip_name.read() with :
       sensorType == Lidar => SensorAspect.ReadLidar()
    //*/

    protected override void Awake()
    {
        base.Awake();
        // Distance, Altitude, AngularVelocity, Rotation, Acceleration, Velocity

        if(this.myChip.option >= SensorAspect.NUMSENSORTYPES)
        {
            Debug.LogError($"option was larger than number of possible sensors");
        }
        this.sensorType = (SensorType)(this.myChip.option % SensorAspect.NUMSENSORTYPES);
        //print($"setting sensortype: {this.sensorType}");
    }

    public override void RuntimeFunction()
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

        float dist = this.hit.distance * (1 + Random.value * 0.001f);
        //print($"READ DISTANCE {dist}");
        return dist;
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

