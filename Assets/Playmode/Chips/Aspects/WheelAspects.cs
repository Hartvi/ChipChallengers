using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;


public class WheelAspects : BaseAspect
{
    ParticleSystem particles;

    float radius = StaticChip.ChipSide * 0.5f;

    float distBetweenRayEnds = 0.1f;
    float phi; // = this.distBetweenRayEnds / this.radius;
    int numRays; // = Mathf.RoundToInt(2*Mathf.PI / this.phi);
    // MomentOfInertia models the default wheel as a thin disk using radius rather than diameter.
    public static readonly Vector3 MomentOfInertia = PhysicsData.mediumMass * new Vector3(1 / 4f, 1 / 2f, 1 / 4f) * (StaticChip.ChipSide * 0.5f) * (StaticChip.ChipSide * 0.5f);

    public static readonly float PlanarMomentOfInertia = WheelAspects.MomentOfInertia[1];
    public static readonly float InvPlanarMomentOfInertia = 1f / WheelAspects.MomentOfInertia[1];
    // inverseInertia caches the wheel's mass-and-radius-scaled spin response for drive and tire forces.
    float inverseInertia = InvPlanarMomentOfInertia;
    //public static float inertiaInvFixedTime = 0f;

    public const float carTyreStiffness = 200 * 1e3f;  // N/mm * 1e3

    public static readonly float DepenetrationVelocity = 1f;
    public static readonly float DepenetrationVelocitySqr = DepenetrationVelocity * DepenetrationVelocity;

    // tire parameters:
    // Support spring strength: higher values reduce tire penetration under load.
    public const float stiffness = 20;
    public const float B = 10f;
    public const float C = 1.9f;
    public const float D = 0.9f;
    public const float E = 0.97f;
    const float ContactForceScale = 1000f;

    Vector3 point, normal, impulse, v1;
    float compression;
    Vector3 xForce;
    Vector3 yForce;
    Vector3 oldUp;
    float xSlip = 0f, ySlip = 0f;

    // VT = Omega * r
    // deltaVx = Vx - VT

    int layerMask = ~((1 << 6) | (1 << 7));

    bool isServer => this.myChip.isServer;

    public float totalSlip = 0f, totalSlip1 = 0f, totalSlip2 = 0f, totalSlip3 = 0f;

    Func<Vector3>[] vectors;

    Rigidbody collidingRigidbody;

    protected override void Start()
    {
        base.Start();
        this.gameObject.layer = 7;

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
        this.radius = this.radius * (this.myChip.option * 0.5f + 1);
        /*
        the larger the wheel the more rays there should be
        \|/
        ---
        /|\
        */
        this.phi = this.distBetweenRayEnds / this.radius;
        this.numRays = Mathf.RoundToInt(2 * Mathf.PI / this.phi);
        this.vectors = new Func<Vector3>[this.numRays];
        // cast rays this.radius` long from the center of the object
        for(int i = 0; i < this.numRays; ++i)
        {
            int _i = i;
            this.vectors[_i] = () => this.RayDirection(_i);
        }
        this.inverseInertia = WheelPhysics.DiskInverseInertia(this.rb.mass, this.radius);
    }

    private Vector3 RayDirection(int i)
    {
        return this.transform.forward * Mathf.Cos(2*Mathf.PI / ((float)this.numRays) * i) + this.transform.right * Mathf.Sin(2*Mathf.PI / ((float)this.numRays) * i);
    }

    // ResetSpinHistory clears the previous axle direction so a staged race cannot resume with a torque kick.
    public void ResetSpinHistory()
    {
        this.oldUp = Vector3.zero;
    }

    // ApplyTorque integrates wheel spin and applies motor and axle-turning reaction impulses to its body.
    void ApplyTorque(float torque)
    {
        Vector3 up = this.transform.up;
        Vector3 turningImpulse = this.oldUp == Vector3.zero ? Vector3.zero :
            this.myChip.Omega / this.inverseInertia * (up - this.oldUp);
        this.rb.AddTorque(turningImpulse + Time.fixedDeltaTime * torque * up, ForceMode.Impulse);
        this.oldUp = up;
        if (this.isServer)
        {
            this.myChip.Omega = WheelPhysics.IntegrateSpin(this.myChip.Omega, torque, this.brake,
                this.inverseInertia, Time.fixedDeltaTime);
        }
    }

    // ApplyForce applies the same tire force to the vehicle, contacted body and opposing wheel spin.
    void ApplyForce()
    {
        (float yImpulse, Vector3 yDir, float xImpulse, Vector3 xDir) = this.PhysicsHandle();

        this.xForce = xImpulse * xDir;
        this.yForce = yImpulse * yDir;
        Vector3 totalForce = ContactForceScale * (this.yForce + this.xForce);
        this.rb.AddForceAtPosition(totalForce, this.point, ForceMode.Force);
        if (this.collidingRigidbody != null)
        {
            this.collidingRigidbody.AddForceAtPosition(-totalForce, this.point, ForceMode.Force);
        }

        float dOmega = WheelPhysics.ContactSpinDelta(ContactForceScale * xImpulse, this.radius,
            this.inverseInertia, Time.fixedDeltaTime);

        if (this.isServer)
        {
            this.myChip.Omega += dOmega;
        }
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

        // Follow the rolling tangent, reversing traction on the opposite side of the wheel.
        Vector3 xDirectionInPlane = Vector3.Cross(n, up).normalized;

        float xVelocity = Vector3.Dot(this.v1, xDirectionInPlane);
        // need the information separately later on
        //Vector3 longitudinalVelocity = xVelocity * xDirectionInPlane;

        // change in deformation
        float impulseStrength = this.impulse.magnitude;

        float VTX = this.radius * this.myChip.Omega;
        float VX = xVelocity;
        float invVX = 1f / (1e-1f + Mathf.Abs(VX));
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

    public override void RuntimeFunction()
    {
        var wheelVisual = this.myChip.SelectedOptionTransform;
        if (wheelVisual == null)
        {
            print($"CHILD TRANSFORM OF WHEEL IS NONE");
            return;
        }
        wheelVisual.Rotate(Vector3.up, -WheelPhysics.SpinDegrees(this.myChip.Omega, Time.deltaTime), Space.Self);

        //this.totalSlip = 0.5f * (Mathf.Abs(this.xSlip) + Mathf.Abs(this.ySlip) + this.totalSlip1);
        this.totalSlip = 0.25f * (Mathf.Abs(this.xSlip) + Mathf.Abs(this.ySlip) + this.totalSlip1 + this.totalSlip2 + this.totalSlip3);
        this.totalSlip3 = this.totalSlip2;
        this.totalSlip2 = this.totalSlip1;
        this.totalSlip1 = this.totalSlip;

        this.particles.gameObject.SetActive(true);
        if (!this.particles.isPlaying)
        {
            this.particles.Play();
            var m = this.particles.main;
            m.startSize = Mathf.Min(0.5f, totalSlip);
            m.startLifetime = Mathf.Min(0.5f, totalSlip);
            var em = this.particles.emission;
            em.rateOverTime = GameManager.RealTimeSettings.ParticleRate * 3f * totalSlip;
        }
        else
        {
            //if (!this.particles.isPlaying)
            //{
            //    this.particles.gameObject.SetActive(false);
            //}
        }
    }

    void FixedUpdate()
    {
        if (this.rb.isKinematic || this.myChip.myCore.IsRebuilding) { return; }
        if (this.RaycastFromBase(out this.point, out this.compression, out this.normal))
        {
            Vector3 n = this.normal;
            float m = this.rb.mass;
            // Dampen motion at the contact, including rocking around the wheel attachment.
            this.v1 = this.rb.GetPointVelocity(this.point);
            if (this.collidingRigidbody != null)
            {
                this.v1 -= this.collidingRigidbody.GetPointVelocity(this.point);
            }
            float normalVelocity = Vector3.Dot(this.v1, n);
            float desiredForce = ContactForceScale * (m * stiffness * this.compression - 0.5f * normalVelocity);
            float supportForce = WheelPhysics.LimitSupportForce(desiredForce, m, normalVelocity,
                DepenetrationVelocity, Time.fixedDeltaTime);

            // Keep the existing tire-friction load units consistent with the bounded support force.
            this.impulse = (supportForce / ContactForceScale) * n;
            this.rb.AddForceAtPosition(supportForce * n, this.point, ForceMode.Force);
            this.ApplyForce();
        }
        else
        {
            this.xSlip = 0f;
            this.ySlip = 0f;
        }
        this.ApplyTorque(this.value);
    }

    bool RaycastFromBase(out Vector3 point, out float compression, out Vector3 normal)
    {
        Transform t = this.transform;
        Vector3 basePosition = t.position;
        point = Vector3.zero;
        normal = Vector3.zero;
        compression = 0f;
        this.collidingRigidbody = null;
        bool hasContact = false;

        for (int i = 0; i < this.vectors.Length; ++i)
        {
            Vector3 rayDirection = this.vectors[i]().normalized;
            if (Physics.Raycast(basePosition, rayDirection, out RaycastHit h, this.radius, this.layerMask, QueryTriggerInteraction.Ignore))
            {
                float hitCompression = WheelPhysics.NormalCompression(this.radius, h.distance,
                    Vector3.Dot(rayDirection, h.normal));
                // Keep the deepest contact rather than the first ray that grazes a surface.
                if (hasContact && hitCompression <= compression) { continue; }

                hasContact = true;
                point = h.point;
                normal = h.normal;
                compression = hitCompression;
                this.collidingRigidbody = h.rigidbody;
            }
        }
        return hasContact;
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        if (this.particles)
        {
            GameObject.Destroy(this.particles.gameObject);
        }
    }

}
