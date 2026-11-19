using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public class RuntimeTreeTests
{
    [Test]
    public void DetachedDriveStopsItsLastCommand()
    {
        var root = new GameObject("Detached drive test");
        root.SetActive(false);
        try
        {
            var chip = NetworkTestObjects.Add<CommonChip>(root);
            chip.SetValue(100f);
            chip.SetBrake(10f);
            chip.Die();
            Assert.That(chip.IsDead, Is.True);
            Assert.That(chip.value, Is.Zero);
            Assert.That(chip.brake, Is.Zero);
            Assert.DoesNotThrow(chip.Die);
        }
        finally { Object.DestroyImmediate(root); }
    }

    [TestCase(false, typeof(JetFlameAspect), UIStrings.JetFlame)]
    [TestCase(true, typeof(JetFlameAspect), UIStrings.JetFlame)]
    [TestCase(false, typeof(JetDustAspect), UIStrings.JetDust)]
    [TestCase(true, typeof(JetDustAspect), UIStrings.JetDust)]
    public void DetachedJetStopsItsEffectsEvenIfPowerIsSetAgain(bool breakParent, System.Type effectType, string resource)
    {
        var parentObject = new GameObject("Jet attachment test");
        var jetObject = new GameObject("Detached jet test");
        parentObject.SetActive(false);
        jetObject.SetActive(false);
        var particles = Object.Instantiate(Resources.Load<ParticleSystem>(resource));
        float previousParticleRate = GameManager.RealTimeSettings.ParticleRate;
        try
        {
            var parent = NetworkTestObjects.Add<CommonChip>(parentObject);
            var jet = NetworkTestObjects.Add<CommonChip>(jetObject);
            jet.SetParent(parent);
            var effect = (BaseAspect)jetObject.AddComponent(effectType);
            effect.myChip = jet;
            var flags = BindingFlags.Instance | BindingFlags.NonPublic;
            effectType.GetField("particles", flags).SetValue(effect, particles);
            GameManager.RealTimeSettings.ParticleRate = 1f;
            jet.SetValue(1000f);
            jet.SetBrake(10f);
            effect.RuntimeFunction();
            particles.Play();
            particles.Emit(1);
            Assert.That(particles.emission.rateOverTime.constant, Is.GreaterThan(0f));
            Assert.That(particles.particleCount, Is.GreaterThan(0));

            typeof(CommonChip).GetMethod("OnJointBreak", flags)
                .Invoke(breakParent ? parent : jet, new object[] { 1f });
            Assert.That(jet.IsDead, Is.True);
            // A later script command must not restart a detached engine's effects.
            jet.SetValue(1000f);
            jet.SetBrake(10f);
            effect.RuntimeFunction();

            Assert.That(particles.emission.rateOverTime.constant, Is.Zero);
            Assert.That(particles.isStopped, Is.True);
            Assert.That(particles.particleCount, Is.Zero);
        }
        finally
        {
            GameManager.RealTimeSettings.ParticleRate = previousParticleRate;
            Object.DestroyImmediate(particles.gameObject);
            Object.DestroyImmediate(jetObject);
            Object.DestroyImmediate(parentObject);
        }
    }

    [Test]
    public void ReparentingAndClearingDetachBothDirections()
    {
        var objects = new List<GameObject>();
        CommonChip Create()
        {
            var item = new GameObject("Runtime tree test");
            item.SetActive(false);
            objects.Add(item);
            return NetworkTestObjects.Add<CommonChip>(item);
        }
        try
        {
            var first = Create(); var second = Create(); var child = Create();
            var field = typeof(GeometricChip).GetField("childChips", BindingFlags.Instance | BindingFlags.NonPublic);
            child.SetParent(first);
            child.SetParent(second);
            Assert.That((List<GeometricChip>)field.GetValue(first), Is.Empty);
            Assert.That(((List<GeometricChip>)field.GetValue(second)).Count, Is.EqualTo(1));
            second.ClearTreeLinks();
            Assert.That(child.parentChip, Is.Null);
            Assert.That((List<GeometricChip>)field.GetValue(second), Is.Empty);
        }
        finally { foreach (var item in objects) { Object.DestroyImmediate(item); } }
    }
}
