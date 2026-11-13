using System;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public class ChipTypeSetupTests
{
    GameObject root;

    [TearDown]
    public void TearDown()
    {
        if (root != null) { UnityEngine.Object.DestroyImmediate(root); }
    }

    [TestCase(VChip.wheelStr, typeof(WheelAspects), typeof(WheelSoundAspect))]
    [TestCase(VChip.jetStr, typeof(JetAspect), typeof(JetFlameAspect))]
    [TestCase(VChip.gunStr, typeof(GunAspect), typeof(GunSoundAspect))]
    public void TypeOnlySavedChipGetsItsRuntimeComponents(string type, Type physics, Type effect)
    {
        var savedChip = new VChip(new[] { VChip.typeStr }, new[] { type }, 0, null);
        var chip = Setup(savedChip);

        Assert.That(root.GetComponent(physics), Is.Not.Null);
        Assert.That(root.GetComponent(effect), Is.Not.Null);
        Assert.That(chip.value, Is.Zero);
        Assert.That(chip.brake, Is.Zero);
        if (type == VChip.wheelStr)
        {
            var wheel = root.GetComponent<WheelAspects>();
            Assert.That(wheel.myChip, Is.SameAs(chip));
            Assert.That(root.GetComponent<TireSoundAspect>().wheelAspect, Is.SameAs(wheel));
        }
    }

    [Test]
    public void WheelKeepsExplicitControlsWhenOtherPropertiesAreMissing()
    {
        var savedChip = new VChip(
            new[] { VChip.typeStr, VChip.valueStr, VChip.brakeStr },
            new[] { VChip.wheelStr, "12", "3" }, 0, null);
        var chip = Setup(savedChip);

        Assert.That(chip.value, Is.EqualTo(12f));
        Assert.That(chip.brake, Is.EqualTo(3f));
        Assert.That(root.GetComponent<WheelAspects>(), Is.Not.Null);
    }

    [Test]
    public void ControlFieldsDoNotGiveAnOrdinaryChipDriveComponents()
    {
        Setup(new VChip(
            new[] { VChip.typeStr, VChip.valueStr, VChip.brakeStr },
            new[] { VChip.chipStr, "12", "3" }, 0, null));

        Assert.That(root.GetComponent<WheelAspects>(), Is.Null);
        Assert.That(root.GetComponent<JetAspect>(), Is.Null);
        Assert.That(root.GetComponent<GunAspect>(), Is.Null);
    }

    [Test]
    public void NewWheelStoresDefaultsBeforeOpeningTheChipPanel()
    {
        var wheel = new VChip(VChip.wheelStr, (LocalDirection)0, null);
        Assert.That(wheel.TryGetProperty<string>(VChip.valueStr, out var value), Is.True);
        Assert.That(wheel.TryGetProperty<string>(VChip.brakeStr, out var brake), Is.True);
        Assert.That(value, Is.EqualTo("0"));
        Assert.That(brake, Is.EqualTo("0"));
    }

    CommonChip Setup(VChip virtualChip)
    {
        root = new GameObject("Chip type setup regression test");
        // Test setup synchronously without starting network, audio, or physics updates.
        root.SetActive(false);
        var chip = root.AddComponent<CommonChip>();
        chip.equivalentVirtualChip = virtualChip;
        typeof(CommonChip).GetMethod("SetupTypeAspects", BindingFlags.Instance | BindingFlags.NonPublic)
            .Invoke(chip, null);
        return chip;
    }
}
