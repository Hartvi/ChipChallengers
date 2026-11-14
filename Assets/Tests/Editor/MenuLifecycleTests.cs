using System;
using NUnit.Framework;
using UnityEngine;

public class ReviewTestMenu : BaseMenu { }

public class MenuLifecycleTests
{
    [Test]
    public void RepeatedSelectionDoesNotRepeatEntryOrExitSideEffects()
    {
        var root = new GameObject("Menu lifecycle test", typeof(RectTransform));
        var menu = root.AddComponent<ReviewTestMenu>();
        try
        {
            int entered = 0, left = 0;
            menu.selectedCallbacks.SetCallbacks(new Action[] { () => entered++ });
            menu.deselectedCallbacks.SetCallbacks(new Action[] { () => left++ });
            menu.isSelected = false;
            menu.isSelected = true;
            menu.isSelected = true;
            menu.isSelected = false;
            menu.isSelected = false;
            Assert.That(entered, Is.EqualTo(1));
            Assert.That(left, Is.EqualTo(1));
        }
        finally
        {
            BaseMenu._allMenus.Remove(menu);
            UnityEngine.Object.DestroyImmediate(root);
        }
    }
}
