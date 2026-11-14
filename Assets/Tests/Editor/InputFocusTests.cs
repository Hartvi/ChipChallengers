using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public class ReviewInputReceiver : MonoBehaviour, InputReceiver
{
    public int starts, stops;
    public System.Action handle;
    bool InputReceiver.IsActive() => this.isActiveAndEnabled;
    void InputReceiver.HandleInputs() => this.handle?.Invoke();
    public void OnStartReceiving() => this.starts++;
    public void OnStopReceiving() => this.stops++;
}

public class InputFocusTests
{
    [Test]
    public void FirstReceiverStartsAndLastReceiverCanLeaveSafely()
    {
        var previous = UIManager.instance;
        var root = new GameObject("Input focus manager");
        var receiverObject = new GameObject("Input focus receiver");
        try
        {
            var manager = root.AddComponent<UIManager>();
            var receiver = receiverObject.AddComponent<ReviewInputReceiver>();
            manager.SwitchToMe(receiver);
            Assert.That(receiver.starts, Is.EqualTo(1));
            var update = typeof(UIManager).GetMethod("Update", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.DoesNotThrow(() => update.Invoke(manager, null));
            receiver.handle = () => manager.TurnMeOff(receiver);
            Assert.DoesNotThrow(() => update.Invoke(manager, null));
            Assert.That(receiver.stops, Is.EqualTo(1));
            receiver.handle = null;
            manager.SwitchToMe(receiver);
            Assert.DoesNotThrow(() => manager.TurnMeOff(receiver));
            Assert.That(receiver.stops, Is.EqualTo(2));
            Assert.DoesNotThrow(() => update.Invoke(manager, null));
            manager.SwitchToMe(receiver);
            receiverObject.SetActive(false);
            Assert.DoesNotThrow(() => update.Invoke(manager, null));
        }
        finally
        {
            Object.DestroyImmediate(receiverObject);
            Object.DestroyImmediate(root);
            UIManager.instance = previous;
        }
    }
}
