using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public class RuntimeTreeTests
{
    [Test]
    public void ReparentingAndClearingDetachBothDirections()
    {
        var objects = new List<GameObject>();
        CommonChip Create()
        {
            var item = new GameObject("Runtime tree test");
            item.SetActive(false);
            objects.Add(item);
            return item.AddComponent<CommonChip>();
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
