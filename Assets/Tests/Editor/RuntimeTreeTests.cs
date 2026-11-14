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
