using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class WheelAspects : BaseAspect
{
    public static readonly Vector3 MomentOfInertia = PhysicsData.smallMass * new Vector3(1 / 4f, 1 / 2f, 1 / 4f)
        .Multiply(StaticChip.ChipSize)
        .Multiply(StaticChip.ChipSize);

    public static readonly float radius = StaticChip.ChipSide * 0.5f;

    public static readonly float PlanarMomentOfInertia = WheelAspects.MomentOfInertia[1];
    public static readonly float InvPlanarMomentOfInertia = 1f / WheelAspects.MomentOfInertia[1];

    public const float carTyreStiffness = 200 * 1e3f;  // N/mm * 1e3
    
    public static readonly float DepenetrationVelocity = 5f;
    public static readonly float DepenetrationVelocitySqr = DepenetrationVelocity * DepenetrationVelocity;
    
    // tire parameters:
    public const float stiffness = 20;  // =20 means that a deformation of 0.05 yields a depenetration velocity of 1
    public const float B = 10f;
    public const float C = 1.9f;
    public const float D = 1f;
    public const float E = 0.97f;

    Vector3 point, forwardPosition, normal, impulse, v1;

    int layerMask = ~((1 << 6) | (1 << 7));

    float Omega;

    void ApplyTorque(float T)
    {
        // T = dL / dt
        // dL = T * dt
        // dL = I * dOmega
        // d Omega = dL / I = T * dt / I
        // L = momentum, t = time
        // L = I * Omega
        // dOmega
        float dOmega = T * Time.deltaTime * InvPlanarMomentOfInertia;
        this.Omega += dOmega;
    }

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
        float dOmega = this.PhysicsHandle() * 0f;
    }

    float PhysicsHandle()
    {
        // TODO maybe replace up with the normal to the this.normal
        // this isnt strictly parallel to the ground
        Vector3 up = this.transform.up;
        //Vector3 up = Vector3.ProjectOnPlane(this.transform.up, this.normal);

        Vector3 longitudinalVelocity = Vector3.ProjectOnPlane(this.v1, this.normal);
        //Vector3 longitudinalVelocity = Vector3.Project(this.v1, this.transform.right);

        Vector3 nUp = Vector3.Dot(longitudinalVelocity, up) * up;

        float nUpMagnitude = nUp.magnitude;
        float nForwardMagnitude = longitudinalVelocity.magnitude - nUpMagnitude;
        float zSlip = nForwardMagnitude - WheelAspects.radius * this.Omega;

        float xSlip = nUpMagnitude;
        return 0f;
    }

    static float GetK(float k) {
        // https://www.mathworks.com/help/sdl/ref/tireroadinteractionmagicformula.html
        return 0f;
    }

    float Pacejka(float k)
    {
        float Bk = B * k;
        return D * Mathf.Sin(C * arctan(Bk - E*(Bk - arctan(Bk))));
    }

    float arctan(float x)
    {
        return (1.7f*x) / (0.4f + Mathf.Sqrt(1.7f+1.3f*x*x));
    }

    void Start()
    {
        this.gameObject.layer = 7;
    }

    void Update()
    {
        if(this.RaycastFromBase(out this.point, out this.forwardPosition, out this.normal))
        {
            Vector3 n = this.normal;
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
            Vector3 nUp = Vector3.Dot(v1, up) * up;

            Vector3 targetVelocity = -0.5f * (n1 + nUp);

            Vector3 NaiveImpulse = m * (WheelAspects.stiffness * err);
            //Vector3 p1 = m * v1;
            Vector3 J = m * (n * WheelAspects.DepenetrationVelocity);

            float ImpulseDifference = (J - NaiveImpulse).sqrMagnitude;

            Vector3 FinalImpulse = (ImpulseDifference > 0f) ? NaiveImpulse : J;

            this.impulse = FinalImpulse + targetVelocity;
            this.rb.AddForceAtPosition(this.impulse, this.forwardPosition, ForceMode.Impulse);
        }
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
