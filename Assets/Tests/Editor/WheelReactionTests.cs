using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public class WheelReactionTests
{
    [Test]
    public void NormalSupportTransfersOppositeMomentumToADynamicPlatform()
    {
        var wheelObject = new GameObject("Support reaction wheel");
        var platform = GameObject.CreatePrimitive(PrimitiveType.Cube);
        var coreObject = new GameObject("Support reaction core");
        coreObject.SetActive(false);
        bool autoSimulation = Physics.autoSimulation;
        try
        {
            Vector3 origin = new Vector3(20000, 20000, 20000);
            platform.transform.position = origin;
            wheelObject.transform.position = origin + Vector3.up;
            var groundBody = platform.AddComponent<Rigidbody>();
            groundBody.useGravity = false;
            groundBody.mass = 10;
            var wheelBody = wheelObject.AddComponent<Rigidbody>();
            wheelBody.useGravity = false;
            wheelBody.mass = 2;
            var chip = NetworkTestObjects.Add<CommonChip>(wheelObject);
            chip.myCore = NetworkTestObjects.Add<CoreChip>(coreObject);
            var wheel = wheelObject.AddComponent<WheelAspects>();
            wheel.myChip = chip;
            Physics.SyncTransforms();
            Assert.That(Physics.Raycast(wheelObject.transform.position, Vector3.down, out var hit, 2), Is.True);
            var flags = BindingFlags.NonPublic | BindingFlags.Instance;
            typeof(WheelAspects).GetField("contacts", flags).SetValue(wheel, new[] { hit });
            typeof(WheelAspects).GetField("contactCount", flags).SetValue(wheel, 1);
            typeof(WheelAspects).GetField("point", flags).SetValue(wheel, hit.point);
            Physics.autoSimulation = false;
            typeof(WheelAspects).GetMethod("ApplyContactForces", flags).Invoke(wheel, new object[] { Vector3.up * 100 });
            Physics.Simulate(Time.fixedDeltaTime);
            Assert.That(wheelBody.velocity.y, Is.GreaterThan(0));
            Assert.That(groundBody.velocity.y, Is.LessThan(0));
            Assert.That(wheelBody.mass * wheelBody.velocity.y + groundBody.mass * groundBody.velocity.y,
                Is.EqualTo(0).Within(0.0001));
        }
        finally
        {
            Physics.autoSimulation = autoSimulation;
            Object.DestroyImmediate(wheelObject);
            Object.DestroyImmediate(platform);
            Object.DestroyImmediate(coreObject);
        }
    }
}
