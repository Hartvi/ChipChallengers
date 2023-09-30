using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class WheelAspects : BaseAspect
{
    public static readonly Vector3 MomentOfInertia = PhysicsData.smallMass * new Vector3(1 / 4f, 1 / 2f, 1 / 4f) * StaticChip.ChipSide * StaticChip.ChipSide;
    public static readonly float radius = StaticChip.ChipSide * 0.5f;

    public static readonly float PlanarMomentOfInertia = WheelAspects.MomentOfInertia[1];
    public static readonly float InvPlanarMomentOfInertia = 1f / WheelAspects.MomentOfInertia[1];
    public static float fixedTimeInvInertia = 0f;
    public static float inertiaInvFixedTime = 0f;

    public const float carTyreStiffness = 200 * 1e3f;  // N/mm * 1e3
    
    public static readonly float DepenetrationVelocity = 5f;
    public static readonly float DepenetrationVelocitySqr = DepenetrationVelocity * DepenetrationVelocity;
    
    // tire parameters:
    public const float stiffness = 5;  // =20 means that a deformation of 0.05 yields a depenetration velocity of 1
    public const float B = 10f;
    public const float C = 1.9f;
    public const float D = 1f;
    public const float E = 0.97f;
    public const float k1 = 1e-4f;
    public const float k2 = 1e-8f;

    Vector3 point, forwardPosition, normal, impulse, v1;
    Vector3 xForce;
    Vector3 yForce;
    Vector3 oldUp;

    // VT = Omega * r
    // deltaVx = Vx - VT

    int layerMask = ~((1 << 6) | (1 << 7));

    float Omega;

    void Start()
    {
        this.gameObject.layer = 7;
        if(WheelAspects.fixedTimeInvInertia == 0f)
        {
            WheelAspects.fixedTimeInvInertia = Time.fixedDeltaTime * InvPlanarMomentOfInertia;
            WheelAspects.inertiaInvFixedTime = 1f / WheelAspects.fixedTimeInvInertia;
        }
    }

    void ApplyTorque(float T)
    {
        // T = dL / dt
        // dL = T * dt
        // dL = I * dOmega
        // d Omega = dL / I = T * dt / I
        // L = momentum, t = time
        // L = I * Omega
        // dOmega
        float absOmega = Mathf.Abs(this.Omega);
        float dOmega = T * WheelAspects.fixedTimeInvInertia - Mathf.Sign(this.Omega) * (k1 * absOmega + k2 * absOmega * absOmega);

        //if (Mathf.Abs(this.value) > 0.01f)
        //{
        //    print($"Torque: {T} Omega: {this.Omega}, dOmega: {dOmega}");
        //}
        //print($"Torque: {T} Omega: {this.Omega}, dOmega: {dOmega}");


        // T * dt / I = dOmega
        // T = dOmega * I / dt
        Vector3 up = this.transform.up;
        Vector3 newUp = up * this.Omega;
        Vector3 torque = (newUp - oldUp) * WheelAspects.inertiaInvFixedTime;
        this.rb.AddTorque(torque + T * up);

        this.oldUp = newUp;
        this.Omega = this.Omega + dOmega;
    }

    //void Update()
    //{
    //    print($"Torque: {this.value} Omega: {this.Omega}, xForce: {this.xForce}");
    //}

    void ApplyForce()
    {
        // T = r x F
        // T = dL / dt
        // L = I * Omega => dL = I * dOmega
        // T = T => r x F = dL / dt
        // r x F = I * dOmega / dt
        // slip = Omega * r - surface velocity
        // Pacejka(slip) * Fn = F
        // dOmega = r x F / I * dt = Pacejka(slip) * r * Fn.magnitude / I * dt

        (float yImpulse, Vector3 yDir, float xImpulse, Vector3 xDir) = this.PhysicsHandle();

        this.xForce = xImpulse * xDir;
        this.yForce = yImpulse * yDir;
        this.rb.AddForceAtPosition(this.yForce, this.point, ForceMode.Impulse);
        this.rb.AddForceAtPosition(this.xForce, this.point, ForceMode.Impulse);

        // impulse = S (F) dt => no need to multiply xImpulse * dt to get dOmega since dOmega = k * Force * dt
        float dOmega = -radius * xImpulse * InvPlanarMomentOfInertia;

        //Debug.DrawLine(this.transform.position, this.transform.position + 10f*xForce, Color.red);
        //Debug.DrawLine(this.transform.position, this.transform.position + 10f*yForce, Color.green);
        
        //print($"xForce: {xForce} yForce: {yForce}");
        //print($"adding force: omega: {Omega}, dOmega: {dOmega}");
        //if (Mathf.Abs(this.value) > 0.1f)
        //{
        //    print($"xForce: {xForce} yForce: {yForce}");
        //    print($"adding force: omega: {Omega}, dOmega: {dOmega}");
        //}
        this.Omega += dOmega;
    }

    (float yImpulse, Vector3 yDir, float xImpulse, Vector3 xDir) PhysicsHandle()
    {
        // TODO maybe replace up with the normal to the this.normal
        // this isnt strictly parallel to the ground

        Vector3 up = this.transform.up;
        Vector3 n = this.normal;
        // project orthonormal on plane - this projection doesn't need normalization wrt vector magnitude since it's 1
        // but we want to stretch it to signify direction, i.e. a unit vector
        Vector3 yDirectionInPlane = (up - Vector3.Dot(up, n) * n).normalized;

        float yVelocity = Vector3.Dot(this.v1, yDirectionInPlane);
        // just to be consistent with the need to split it below in the x section
        //Vector3 transversalVelocity = yVelocity * yDirectionInPlane;

        // A: less accurate but faster:
        Vector3 right = this.transform.right;
        // project orthonormal on plane - this projection doesn't need normalization wrt vector magnitude since it's 1
        // but we want to stretch it to signify direction, i.e. a unit vector
        Vector3 xDirectionInPlane = (right - Vector3.Dot(right, n) * n).normalized;

        float xVelocity = Vector3.Dot(this.v1, xDirectionInPlane);
        // need the information separately later on
        //Vector3 longitudinalVelocity = xVelocity * xDirectionInPlane;

        // change in deformation
        float impulseStrength = this.impulse.magnitude;

        float VTX = WheelAspects.radius * this.Omega;
        float VX = xVelocity;
        float invVX = 1f / (1e-8f + Mathf.Abs(VX));
        float xSlip = (VTX - VX) * invVX;
        float xImpulse = impulseStrength * Pacejka(xSlip);

        //float ySlip = -yVelocity;
        //float ySlip = -Mathf.Sign(yVelocity);
        float ySlip = arctan(-yVelocity * invVX);
        float yImpulse = impulseStrength * Pacejka(ySlip);

        return (yImpulse, yDirectionInPlane, xImpulse, xDirectionInPlane);
    }

    // https://www.mathworks.com/help/sdl/ref/tireroadinteractionmagicformula.html
    static float Pacejka(float k)
    {
        float Bk = B * k;
        return D * Mathf.Sin(C * arctan(Bk - E*(Bk - arctan(Bk))));
    }

    static float arctan(float x)
    {
        return (1.7f*x) / (0.4f + Mathf.Sqrt(1.7f+1.3f*x*x));
    }

    void FixedUpdate()
    {
        if(this.RaycastFromBase(out this.point, out this.forwardPosition, out this.normal))
        {
            Vector3 n = this.normal;
            // tire deformation
            Vector3 err = (this.point - this.forwardPosition).magnitude * n;
            // J = p2 - p1
            // s.t. ||p2|| <= m ||v_const||
            // p2 = J + p1 <= m ||v_const||
            // p1 = m * v1
            float m = this.rb.mass;
            Vector3 up = this.transform.up;

            this.v1 = this.rb.velocity;
            // project incoming velocity on normalized vector perpendicular to ground
            Vector3 n1 = Vector3.Dot(v1, n) * n;
            // project incoming velocity on normalized vector perpendicular to wheel to stop lateral sliding
            //Vector3 nUp = Vector3.Dot(v1, up) * up;

            //Vector3 targetVelocity = -0.5f * (n1 + nUp);
            Vector3 targetVelocity = -0.5f * n1;

            Vector3 NaiveImpulse = m * (WheelAspects.stiffness * err);
            //Vector3 p1 = m * v1;
            Vector3 J = m * (n * WheelAspects.DepenetrationVelocity);

            float ImpulseDifference = (J - NaiveImpulse).sqrMagnitude;

            Vector3 FinalImpulse = (ImpulseDifference > 0f) ? NaiveImpulse : J;

            this.impulse = FinalImpulse + targetVelocity;
            this.rb.AddForceAtPosition(this.impulse, this.point, ForceMode.Impulse);
            this.ApplyForce();
        }
        this.ApplyTorque(this.value);
    }

    bool RaycastFromBase(out Vector3 point, out Vector3 forwardPosition, out Vector3 normal)
    {
        Transform t = this.transform;
        Vector3 forwardShift = 0.5f * GeometricChip.ChipSide * t.forward;
        Vector3 basePosition = t.position - forwardShift;
        forwardPosition = t.position + forwardShift;

        //Debug.DrawLine(basePosition, forwardPosition, Color.red, 0.1f);
        if(Physics.Raycast(basePosition, t.forward, out RaycastHit h, GeometricChip.ChipSide, this.layerMask))
        {
            point = h.point;
            normal = h.normal;
            return true;
        }
        else
        {
            point = Vector3.zero;
            normal = Vector3.zero;
            return false;
        }
    }

}