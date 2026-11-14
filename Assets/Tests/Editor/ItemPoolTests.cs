using NUnit.Framework;
using UnityEngine;

public class ItemPoolTests
{
    [Test]
    public void GrowingAfterHidingItemsAlwaysProvidesTheRequestedCount()
    {
        var root = new GameObject("Item pool test", typeof(RectTransform));
        var template = new GameObject("Template", typeof(RectTransform));
        template.transform.SetParent(root.transform, false);
        var item = template.AddComponent<ItemBase>();
        try
        {
            Assert.That(item.DisplayNItems<ItemBase>(3), Has.Length.EqualTo(3));
            Assert.That(item.DisplayNItems<ItemBase>(0), Is.Empty);
            Assert.DoesNotThrow(() => item.HideItem());
            Assert.That(item.DisplayNItems<ItemBase>(5), Has.Length.EqualTo(5));
            Assert.That(root.transform.childCount, Is.EqualTo(5));
            Assert.That(item.DisplayNItems<ItemBase>(2), Has.Length.EqualTo(2));
            Assert.That(root.transform.childCount, Is.EqualTo(5));
        }
        finally { Object.DestroyImmediate(root); }
    }
}
