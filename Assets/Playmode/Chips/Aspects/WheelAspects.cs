using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class WheelAspects : BaseAspect
{
    ParticleSystem particles;

    public static readonly Vector3 MomentOfInertia = PhysicsData.mediumMass * new Vector3(1 / 4f, 1 / 2f, 1 / 4f) * StaticChip.ChipSide * StaticChip.ChipSide;
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
    public const float D = 0.9f;
    public const float E = 0.97f;
    public const float k1 = 1e-4f;
    public const float k2 = 1e-8f;

    Vector3 point, forwardPosition, normal, impulse, v1;
    Vector3 xForce;
    Vector3 yForce;
    Vector3 oldUp;
    float xSlip = 0f, ySlip = 0f;

    // VT = Omega * r
    // deltaVx = Vx - VT

    int layerMask = ~((1 << 6) | (1 << 7));

    float Omega;

    Transform childTransform;
    public float totalSlip = 0f, totalSlip1 = 0f, totalSlip2 = 0f;

    void Start()
    {
        this.gameObject.layer = 7;
        if (WheelAspects.fixedTimeInvInertia == 0f)
        {
            WheelAspects.fixedTimeInvInertia = Time.fixedDeltaTime * InvPlanarMomentOfInertia;
            WheelAspects.inertiaInvFixedTime = 1f / WheelAspects.fixedTimeInvInertia;
        }
        this.childTransform = this.transform.GetChild(0);

        this.particles = Instantiate(Resources.Load<ParticleSystem>(UIStrings.Dust));

        var m = this.particles.main;
        m.startSize = 0f;
        m.startLifetime = 0f;
        var em = this.particles.emission;
        em.rateOverTime = 0f;
        this.particles.gameObject.SetActive(false);
        this.particles.transform.position = this.transform.position;
        this.particles.transform.SetParent(this.transform);
        this.particles.transform.localScale = Vector3.one;
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
        float dOmega = T * WheelAspects.fixedTimeInvInertia - Mathf.Sign(this.Omega) * (this.brake * WheelAspects.fixedTimeInvInertia + k1 * absOmega + k2 * absOmega * absOmega);

        // T * dt / I = dOmega
        // T = dOmega * I / dt
        Vector3 up = this.transform.up;
        Vector3 newUp = up * this.Omega;
        Vector3 torque = (newUp - oldUp) * WheelAspects.inertiaInvFixedTime;
        this.rb.AddTorque(torque + T * up);

        this.oldUp = newUp;
        this.Omega = this.Omega + dOmega;
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

        (float yImpulse, Vector3 yDir, float xImpulse, Vector3 xDir) = this.PhysicsHandle();

        this.xForce = xImpulse * xDir;
        this.yForce = yImpulse * yDir;
        this.rb.AddForceAtPosition(this.yForce, this.point, ForceMode.Impulse);
        this.rb.AddForceAtPosition(this.xForce, this.point, ForceMode.Impulse);

        // impulse = S (F) dt => no need to multiply xImpulse * dt to get dOmega since dOmega = k * Force * dt
        float dOmega = -radius * xImpulse * InvPlanarMomentOfInertia;

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
        this.xSlip = (VTX - VX) * invVX;
        float xImpulse = impulseStrength * Pacejka(this.xSlip);

        this.ySlip = arctan(-yVelocity * invVX);
        float yImpulse = impulseStrength * Pacejka(this.ySlip);

        return (yImpulse, yDirectionInPlane, xImpulse, xDirectionInPlane);
    }

    // https://www.mathworks.com/help/sdl/ref/tireroadinteractionmagicformula.html
    static float Pacejka(float k)
    {
        float Bk = B * k;
        return D * Mathf.Sin(C * arctan(Bk - E * (Bk - arctan(Bk))));
    }

    static float arctan(float x)
    {
        return (1.7f * x) / (0.4f + Mathf.Sqrt(1.7f + 1.3f * x * x));
    }

    void Update()
    {
        this.childTransform.Rotate(Vector3.up, -this.Omega, Space.Self);

        this.totalSlip = 0.3333f * (Mathf.Abs(this.xSlip) + Mathf.Abs(this.ySlip) + this.totalSlip1 + this.totalSlip2);
        this.totalSlip2 = this.totalSlip1;
        this.totalSlip1 = this.totalSlip;

        this.particles.gameObject.SetActive(true);
        if (!this.particles.isPlaying)
        {
            this.particles.Play();
            var m = this.particles.main;
            m.startSize = Mathf.Min(1f, totalSlip);
            m.startLifetime = Mathf.Min(1f, totalSlip);
            var em = this.particles.emission;
            em.rateOverTime = 7f * totalSlip;
        }
        else
        {
            if (!this.particles.isPlaying)
            {
                this.particles.gameObject.SetActive(false);
            }
        }
    }

    void FixedUpdate()
    {
        if (this.RaycastFromBase(out this.point, out this.forwardPosition, out this.normal))
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

            Vector3 targetVelocity = -0.5f * n1;

            Vector3 NaiveImpulse = m * (WheelAspects.stiffness * err);
            Vector3 J = m * (n * WheelAspects.DepenetrationVelocity);

            float ImpulseDifference = (J - NaiveImpulse).sqrMagnitude;

            Vector3 FinalImpulse = (ImpulseDifference > 0f) ? NaiveImpulse : J;

            this.impulse = FinalImpulse + targetVelocity;
            this.rb.AddForceAtPosition(this.impulse, this.point, ForceMode.Impulse);
            this.ApplyForce();
        }
        else
        {
            this.xSlip = 0f;
            this.ySlip = 0f;
        }
        this.ApplyTorque(this.value);
    }

    bool RaycastFromBase(out Vector3 point, out Vector3 forwardPosition, out Vector3 normal)
    {
        Transform t = this.transform;
        Vector3 forwardShift = 0.5f * GeometricChip.ChipSide * t.forward;
        Vector3 basePosition = t.position - forwardShift;
        forwardPosition = t.position + forwardShift;

        if (Physics.Raycast(basePosition, t.forward, out RaycastHit h, GeometricChip.ChipSide, this.layerMask))
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

    void OnDestroy()
    {
        if (this.particles)
        {
            Object.Destroy(this.particles.gameObject);
        }
    }

}
