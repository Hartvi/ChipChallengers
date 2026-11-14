using NUnit.Framework;
using UnityEngine;

public class ItemNavigationTests
{
    [Test]
    public void NavigationUsesActualSiblingsAndHandlesInactiveOrEmptyLists()
    {
        var root = new GameObject("Item navigation test");
        try
        {
            var first = AddItem(root, "First");
            var hidden = AddItem(root, "Hidden");
            var last = AddItem(root, "Last");
            hidden.gameObject.SetActive(false);
            first.MyIndex = 900;
            Assert.That(first.Next(), Is.SameAs(last));
            Assert.That(first.Next(true), Is.SameAs(hidden));
            Assert.That(hidden.Next(), Is.SameAs(last));
            Assert.That(last.Next(), Is.SameAs(first));
            last.enabled = false;
            Assert.That(first.Next(), Is.SameAs(first));
            last.enabled = true;
            first.gameObject.SetActive(false);
            last.gameObject.SetActive(false);
            Assert.That(first.Next(), Is.Null);
            Assert.That(first.Next(true), Is.SameAs(hidden));
        }
        finally { Object.DestroyImmediate(root); }
    }

    private static ItemBase AddItem(GameObject parent, string name)
    {
        var child = new GameObject(name, typeof(RectTransform));
        child.transform.SetParent(parent.transform);
        return child.AddComponent<ItemBase>();
    }
}
