using System;
using System.Reflection;
using Mirror;
using NUnit.Framework;
using UnityEngine;

public class WheelTorqueTests
{
    [TestCase(500, 10f, 100f, 0f, false, false)]
    [TestCase(1000, 10f, 100f, 0f, false, false)]
    [TestCase(500, -10f, 100f, 0f, false, false)]
    [TestCase(1000, -10f, 100f, 0f, false, false)]
    [TestCase(500, 10f, 0f, 0f, false, false)]
    [TestCase(1000, 10f, 0f, 0f, false, false)]
    [TestCase(500, 0f, 100f, 100f, false, false)]
    [TestCase(1000, 0f, 100f, -100f, false, false)]
    [TestCase(500, 0f, 0f, 100f, false, true)]
    [TestCase(1000, 0f, 0f, 100f, false, true)]
    [TestCase(500, 0f, 0f, 100f, true, false)]
    [TestCase(1000, 0f, 0f, 100f, true, false)]
    public void BodyReactionMatchesNetWheelSpinChange(int rate, float power, float brake,
        float initialSpin, bool grounded, bool turnAxle)
    {
        var wheelObject = new GameObject("Wheel torque test");
        var coreObject = new GameObject("Wheel torque core");
        coreObject.SetActive(false);
        bool automatic = Physics.autoSimulation;
        float previousStep = Time.fixedDeltaTime;
        NetworkIdentity identity = null;
        GameObject floor = null;
        try
        {
            Time.fixedDeltaTime = 1f / rate;
            Physics.autoSimulation = false;
            var body = wheelObject.AddComponent<Rigidbody>();
            body.useGravity = false;
            body.angularDrag = 0;
            body.inertiaTensor = Vector3.one;
            body.inertiaTensorRotation = Quaternion.identity;
            body.maxAngularVelocity = 1000;
            if (grounded)
            {
                floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
                floor.transform.position = new Vector3(100, -0.5f, 100);
                wheelObject.transform.SetPositionAndRotation(new Vector3(100, 0.2f, 100),
                    Quaternion.Euler(0, 0, 90));
                Physics.SyncTransforms();
            }
            var chip = NetworkTestObjects.Add<CommonChip>(wheelObject);
            chip.myCore = NetworkTestObjects.Add<CoreChip>(coreObject);
            identity = chip.netIdentity;
            typeof(NetworkIdentity).GetProperty("isServer").SetValue(identity, true);
            chip.SetValue(power); chip.SetBrake(brake); chip.Omega = initialSpin;
            var wheel = wheelObject.AddComponent<WheelAspects>();
            wheel.myChip = chip;
            var flags = BindingFlags.Instance | BindingFlags.NonPublic;
            typeof(WheelAspects).GetField("vectors", flags).SetValue(wheel,
                grounded ? new Func<Vector3>[] { () => Vector3.down } : Array.Empty<Func<Vector3>>());
            typeof(WheelAspects).GetField("contacts", flags).SetValue(wheel,
                grounded ? new RaycastHit[1] : Array.Empty<RaycastHit>());
            const float inverseInertia = 16f;
            typeof(WheelAspects).GetField("inverseInertia", flags).SetValue(wheel, inverseInertia);
            Vector3 axis = wheel.transform.up;
            Vector3 previousAxis = turnAxle ? Vector3.right : axis;
            if (turnAxle) typeof(WheelAspects).GetField("oldUp", flags).SetValue(wheel, previousAxis);
            Vector3 centerOfMass = body.worldCenterOfMass;
            typeof(WheelAspects).GetMethod("FixedUpdate", flags).Invoke(wheel, null);
            Vector3 externalAngularImpulse = Vector3.zero;
            if (grounded)
            {
                Assert.That((int)typeof(WheelAspects).GetField("contactCount", flags).GetValue(wheel), Is.EqualTo(1));
                Vector3 point = (Vector3)typeof(WheelAspects).GetField("point", flags).GetValue(wheel);
                Vector3 tireForce = 1000f * ((Vector3)typeof(WheelAspects).GetField("xForce", flags).GetValue(wheel) +
                    (Vector3)typeof(WheelAspects).GetField("yForce", flags).GetValue(wheel));
                Assert.That(tireForce.sqrMagnitude, Is.GreaterThan(0), "Exercise tire-induced spin changes");
                // Normal support points through the center in this fixture and has no moment.
                externalAngularImpulse = Vector3.Cross(point - centerOfMass, tireForce) * Time.fixedDeltaTime;
            }
            Physics.Simulate(Time.fixedDeltaTime);
            Vector3 wheelMomentumChange = (chip.Omega * axis - initialSpin * previousAxis) / inverseInertia;
            // Unit body inertia: body momentum plus virtual wheel momentum must change only by external torque.
            Assert.That((body.angularVelocity - wheelMomentumChange - externalAngularImpulse).magnitude,
                Is.LessThan(0.00001f), "Conserve angular momentum through drive, braking, tire contact and axle turning");
            if (initialSpin == 0 && brake >= Mathf.Abs(power))
            {
                Assert.That(chip.Omega, Is.Zero, "A sufficient brake must hold a powered wheel at rest");
                Assert.That(body.angularVelocity.sqrMagnitude, Is.Zero);
            }
        }
        finally
        {
            if (identity != null) typeof(NetworkIdentity).GetProperty("isServer").SetValue(identity, false);
            Physics.autoSimulation = automatic;
            Time.fixedDeltaTime = previousStep;
            UnityEngine.Object.DestroyImmediate(wheelObject);
            UnityEngine.Object.DestroyImmediate(coreObject);
            if (floor != null) UnityEngine.Object.DestroyImmediate(floor);
        }
    }
}
