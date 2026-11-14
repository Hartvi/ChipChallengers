using System.Reflection;
using MoonSharp.Interpreter;
using NUnit.Framework;
using UnityEngine;

public class SensorAspectTests
{
    [Test]
    public void AccelerationUsesPhysicsSamplesAndSurvivesPresentationUpdates()
    {
        var gameObject = new GameObject("Accelerometer test");
        gameObject.SetActive(false);
        try
        {
            var body = gameObject.AddComponent<Rigidbody>();
            var chip = gameObject.AddComponent<CommonChip>();
            var sensor = gameObject.AddComponent<SensorAspect>();
            sensor.myChip = chip;
            sensor.sensorType = SensorType.Acceleration;
            var sample = typeof(SensorAspect).GetMethod("FixedUpdate", BindingFlags.Instance | BindingFlags.NonPublic);
            body.velocity = Vector3.zero;
            sample.Invoke(sensor, null);
            body.velocity = Vector3.right * 2;
            sample.Invoke(sensor, null);
            sensor.RuntimeFunction();
            sensor.RuntimeFunction();
            Assert.That(sensor.ReadAcceleration(new Script()).Get(1).Number,
                Is.EqualTo(2f / Time.fixedDeltaTime).Within(0.01));
            body.isKinematic = true;
            sample.Invoke(sensor, null);
            Assert.That(sensor.ReadAcceleration(new Script()).Get(1).Number, Is.Zero);
        }
        finally { Object.DestroyImmediate(gameObject); }
    }
}
