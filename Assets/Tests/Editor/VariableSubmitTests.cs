using System.Linq;
using System.Reflection;
using Mirror;
using NUnit.Framework;
using UnityEngine;

public class VariableSubmitTests
{
    [TestCase(0)]
    [TestCase(1)]
    [TestCase(2)]
    [TestCase(3)]
    [TestCase(4)]
    public void SubmittingAnyFieldAddsAndUpdatesExactlyOnceWithoutPollingFocus(int field)
    {
        var root = new GameObject("Variable submit test", typeof(RectTransform));
        var player = new GameObject("Variable submit player");
        player.SetActive(false);
        var localPlayer = typeof(NetworkClient).GetProperty("localPlayer");
        var previous = localPlayer.GetValue(null);
        try
        {
            var core = NetworkTestObjects.Add<CoreChip>(player);
            var model = new VModel();
            typeof(CoreChip).GetField("_ClientVirtualModel", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(core, model);
            localPlayer.SetValue(null, player.GetComponent<NetworkIdentity>());
            var fields = root.AddComponent<VariableFields>();
            for (int i = 0; i < 5; ++i)
            {
                var input = Object.Instantiate(UIUtils.Input, root.transform).AddComponent<BaseInput>();
                input.Setup();
                var label = Object.Instantiate(UIUtils.Text, root.transform).AddComponent<BaseText>();
                label.Setup();
            }
            for (int i = 0; i < 2; ++i)
            {
                var button = Object.Instantiate(UIUtils.Button, root.transform).AddComponent<BaseButton>();
                button.Setup();
            }
            fields.PopulateFields(new VVar(new[] { "throttle", "1", "0", "10", "0" }));
            var inputs = root.GetComponentsInChildren<BaseInput>();
            int submissions = 0;
            model.AddAddedVariableListener(_ => submissions++);
            Assert.That(inputs.All(input => !input.input.isFocused), Is.True);
            inputs[field].input.onSubmit.Invoke(inputs[field].input.text);
            Assert.That(model.variables.Single(variable => variable.name == "throttle").defaultValue, Is.EqualTo(1f));
            inputs[1].input.SetTextWithoutNotify("2");
            inputs[field].input.onSubmit.Invoke(inputs[field].input.text);
            Assert.That(model.variables.Single(variable => variable.name == "throttle").defaultValue, Is.EqualTo(2f));
            Assert.That(submissions, Is.EqualTo(2));
            root.SetActive(false);
            inputs[field].input.onSubmit.Invoke(inputs[field].input.text);
            Assert.That(submissions, Is.EqualTo(2), "A hidden panel must ignore submissions");
        }
        finally
        {
            localPlayer.SetValue(null, previous);
            Object.DestroyImmediate(root);
            Object.DestroyImmediate(player);
        }
    }
}
