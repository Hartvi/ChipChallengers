using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public class VariableReplicationTests
{
    [Test]
    public void ReplicatedValuesUpdateAnObserverAndWaitForMatchingModelShape()
    {
        var root = new GameObject("Observer variable test");
        root.SetActive(false);
        try
        {
            var core = root.AddComponent<CoreChip>();
            var variable = new VVar { name = "power", minValue = -10, maxValue = 10 };
            var model = new VModel { variables = new[] { variable } };
            var flags = BindingFlags.Instance | BindingFlags.NonPublic;
            typeof(CoreChip).GetField("_ClientVirtualModel", flags).SetValue(core, model);
            var values = typeof(CoreChip).GetField("variableValues", flags);
            var apply = typeof(CoreChip).GetMethod("SetSyncVariableValues", flags);
            values.SetValue(core, new[] { 3f, 4f });
            Assert.DoesNotThrow(() => apply.Invoke(core, null));
            Assert.That(variable.currentValue, Is.Zero);
            values.SetValue(core, new[] { 3f });
            apply.Invoke(core, null);
            Assert.That(variable.currentValue, Is.EqualTo(3f));
        }
        finally { Object.DestroyImmediate(root); }
    }
}
