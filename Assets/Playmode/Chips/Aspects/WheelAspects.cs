using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct WheelData
{
    public static float dt;

    public Vector3 Torque;
    public Vector3 InvMomentOfInertia;
    private Vector3 _MomentOfInertia;
    public Vector3 MomentOfInertia
    {
        get { return this._MomentOfInertia; }
        set
        {
            this._MomentOfInertia = value;
            this.InvMomentOfInertia = new Vector3(1f / value.x, 1f / value.y, 1f / value.z);
        }
    }
    public Vector3 Lengths;
    public float Mass;
    public Vector3 r;
    public Vector3 F;
    public static float FrictionCoefficient = 1f;  // How much
    public static float Stiffness = 1e-2f / 1e3f;  // ~1 cm / 100 kg => N/m 
    public Vector3 omega;

    public void Update(Vector3 T, Vector3 F, Vector3 r)
    {
        
    }
}

public class WheelAspects : BaseAspect
{
    WheelData wd;

    Vector3 point, forwardPosition;
    Vector3 oldDiff;

    int layerMask = ~((1 << 6) | (1 << 7));

    void Start()
    {
        this.gameObject.layer = 7;

        this.wd = new WheelData();
        WheelData wd = this.wd;
        wd.Torque = Vector3.zero;
        wd.F = Vector3.zero;
        wd.omega = Vector3.zero;

        wd.Mass = myChip.mass;
        wd.Lengths = GeometricChip.ChipSize;
        wd.r = GeometricChip.ChipSize;

        wd.MomentOfInertia = wd.Mass * new Vector3(1/4f, 1/2f, 1/4f).Multiply(wd.Lengths).Multiply(wd.Lengths);
    }

    void Update()
    {
        if(this.RaycastFromBase(out this.point, out this.forwardPosition))
        {
            Vector3 diff = (this.point - this.forwardPosition);
            // TODO add damping effect so it doesnt bounce into the stratosphere
            // the bounciness => saturate it so it doesn't catapult the model into orbit
            Vector3 Pcomponent = 10f*diff;
            // basically the stickiness => saturate it so it doesnt stick to ceilings
            Vector3 Dcomponent = 4.5f * (diff - this.oldDiff);
            this.rb.AddForceAtPosition(Pcomponent + Vector3.Dot(diff, this.rb.velocity)*Dcomponent, this.forwardPosition, ForceMode.Impulse);
            this.oldDiff = diff;
        }
    }

    bool RaycastFromBase(out Vector3 point, out Vector3 forwardPosition)
    {
        Transform t = this.transform;
        Vector3 forwardShift = 0.5f * GeometricChip.ChipSide * t.forward;
        Vector3 basePosition = t.position - forwardShift;
        forwardPosition = t.position + forwardShift;

        Debug.DrawLine(basePosition, forwardPosition, Color.red, 0.1f);
        if(Physics.Raycast(basePosition, t.forward, out RaycastHit h, GeometricChip.ChipSide, this.layerMask))
        {
            point = h.point;
            return true;
        }
        else
        {
            point = Vector3.zero;
            return false;
        }
    }

}
