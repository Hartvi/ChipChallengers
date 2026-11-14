using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public class DisplayMessageTests
{
    [Test]
    public void AnonymousMessagesAreIndependentAndExpire()
    {
        var flags = BindingFlags.Static | BindingFlags.NonPublic;
        var singleton = typeof(DisplaySingleton).GetField("instance", flags);
        var previous = singleton.GetValue(null);
        singleton.SetValue(null, null);
        var canvas = new GameObject("Message test canvas", typeof(Canvas));
        var root = new GameObject("Message test display");
        try
        {
            var display = root.AddComponent<DisplaySingleton>();
            var instanceFlags = BindingFlags.Instance | BindingFlags.NonPublic;
            typeof(DisplaySingleton).GetField("parentCanvas", instanceFlags).SetValue(display, canvas);
            display.DisplayText(text => text.SetText("first"), 1);
            display.DisplayText(text => text.SetText("second"), 1);
            var messages = (IDictionary)typeof(DisplaySingleton).GetField("displayedTexts", instanceFlags).GetValue(display);
            Assert.That(messages.Count, Is.EqualTo(2));
            foreach (var info in messages.Values)
                info.GetType().GetField("LastDisplayTime").SetValue(info, Time.time - 2);
            typeof(DisplaySingleton).GetMethod("Update", instanceFlags).Invoke(display, null);
            Assert.That(messages.Count, Is.Zero);
        }
        finally
        {
            Object.DestroyImmediate(root);
            Object.DestroyImmediate(canvas);
            singleton.SetValue(null, previous);
        }
    }
}
