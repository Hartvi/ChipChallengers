using System.Linq;
using System.Reflection;
using Mirror;
using NUnit.Framework;
using UnityEngine;

public class VariableSubmitTests
{
    GameObject root;
    GameObject player;
    PropertyInfo localPlayer;
    object previous;
    VModel model;
    VariableFields fields;
    BaseInput[] inputs;
    int submissions;

    [SetUp]
    public void SetUp()
    {
        this.root = new GameObject("Variable submit test", typeof(RectTransform));
        this.player = new GameObject("Variable submit player");
        this.player.SetActive(false);
        this.localPlayer = typeof(NetworkClient).GetProperty("localPlayer");
        this.previous = this.localPlayer.GetValue(null);
        var core = NetworkTestObjects.Add<CoreChip>(this.player);
        this.model = new VModel();
        typeof(CoreChip).GetField("_ClientVirtualModel", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(core, this.model);
        this.localPlayer.SetValue(null, this.player.GetComponent<NetworkIdentity>());
        this.fields = this.root.AddComponent<VariableFields>();
        for (int i = 0; i < 5; ++i)
        {
            var input = Object.Instantiate(UIUtils.Input, this.root.transform).AddComponent<BaseInput>();
            input.Setup();
            var label = Object.Instantiate(UIUtils.Text, this.root.transform).AddComponent<BaseText>();
            label.Setup();
        }
        for (int i = 0; i < 2; ++i)
        {
            var button = Object.Instantiate(UIUtils.Button, this.root.transform).AddComponent<BaseButton>();
            button.Setup();
        }
        this.fields.PopulateFields(new VVar(new[] { "throttle", "1", "0", "10", "0" }));
        this.inputs = this.root.GetComponentsInChildren<BaseInput>();
        this.submissions = 0;
        this.model.AddAddedVariableListener(_ => this.submissions++);
    }

    [TearDown]
    public void TearDown()
    {
        this.localPlayer.SetValue(null, this.previous);
        Object.DestroyImmediate(this.root);
        Object.DestroyImmediate(this.player);
    }

    [TestCase(0)]
    [TestCase(1)]
    [TestCase(2)]
    [TestCase(3)]
    [TestCase(4)]
    public void SubmittingAnyFieldAddsAndUpdatesExactlyOnceWithoutPollingFocus(int field)
    {
        Assert.That(this.inputs.All(input => !input.input.isFocused), Is.True);
        this.inputs[field].input.onSubmit.Invoke(this.inputs[field].input.text);
        Assert.That(this.model.variables.Single(variable => variable.name == "throttle").defaultValue, Is.EqualTo(1f));
        this.inputs[1].input.SetTextWithoutNotify("2");
        this.inputs[field].input.onSubmit.Invoke(this.inputs[field].input.text);
        Assert.That(this.model.variables.Single(variable => variable.name == "throttle").defaultValue, Is.EqualTo(2f));
        Assert.That(this.submissions, Is.EqualTo(2));
        this.root.SetActive(false);
        this.inputs[field].input.onSubmit.Invoke(this.inputs[field].input.text);
        Assert.That(this.submissions, Is.EqualTo(2), "A hidden panel must ignore submissions");
    }

    [TestCase(0, "steering")]
    [TestCase(1, "2")]
    [TestCase(2, "-2")]
    [TestCase(3, "20")]
    [TestCase(4, "2")]
    public void EndingAnyFieldEditAddsAndUpdatesWithoutDuplicateSubmissions(int field, string value)
    {
        string[] expected = { "throttle", "1", "0", "10", "0" };
        expected[field] = value;
        this.inputs[field].input.text = value;
        Assert.That(this.submissions, Is.Zero, "Typing must not create partial variable names");
        this.inputs[field].input.onEndEdit.Invoke(value);
        Assert.That(this.model.GetSelectedVariable().ToStringArray(), Is.EqualTo(expected));
        Assert.That(this.submissions, Is.EqualTo(1));

        this.inputs[1].input.text = "3";
        this.inputs[1].input.onEndEdit.Invoke("3");
        Assert.That(this.model.variables.Single(variable => variable.name == expected[0]).defaultValue, Is.EqualTo(3f));
        Assert.That(this.submissions, Is.EqualTo(2));

        this.inputs[1].input.onEndEdit.Invoke("3");
        this.inputs[1].input.onSubmit.Invoke("3");
        this.fields.AddVariable();
        Assert.That(this.submissions, Is.EqualTo(2), "Enter, focus loss and Add must not repeat the same edit");
    }

    [Test]
    public void UnchangedVariableIsSelectedWithoutPublishingAnotherEdit()
    {
        var variable = new VVar(new[] { "throttle", "1", "0", "10", "0" });
        this.model.AddVariable(variable);
        this.inputs[1].input.onEndEdit.Invoke("1");
        Assert.That(this.model.GetSelectedVariable(), Is.SameAs(variable));
        Assert.That(this.submissions, Is.EqualTo(1));
    }

    [Test]
    public void NameEditCreatesVariableWithDefaultsForBlankNumericFields()
    {
        this.fields.PopulateFields(VModel.EmptyVariable);
        for (int i = 1; i < this.inputs.Length; ++i)
            this.inputs[i].input.SetTextWithoutNotify(string.Empty);
        this.inputs[0].input.text = "acceleration";
        this.inputs[0].input.onEndEdit.Invoke("acceleration");

        Assert.That(this.model.GetSelectedVariable().ToStringArray(), Is.EqualTo(new[] { "acceleration", "0", "0", "0", "0" }));
        Assert.That(this.submissions, Is.EqualTo(1));
    }

    [Test]
    public void NumericEditsWaitForAVariableName()
    {
        this.fields.PopulateFields(VModel.EmptyVariable);
        this.inputs[1].input.text = "1";
        this.inputs[1].input.onEndEdit.Invoke("1");
        Assert.That(this.submissions, Is.Zero);

        this.inputs[0].input.text = "acceleration";
        this.inputs[0].input.onEndEdit.Invoke("acceleration");
        Assert.That(this.model.GetSelectedVariable().defaultValue, Is.EqualTo(1f));
        Assert.That(this.submissions, Is.EqualTo(1));
    }

    [Test]
    public void ClosingPanelSavesTheLastFieldEdit()
    {
        this.inputs[1].input.text = "2";
        this.root.SetActive(false);
        this.inputs[1].input.onEndEdit.Invoke("2");
        Assert.That(this.model.GetSelectedVariable().defaultValue, Is.EqualTo(2f));
        Assert.That(this.submissions, Is.EqualTo(1));
    }

    [Test]
    public void DisconnectedPanelIgnoresEndEdit()
    {
        this.inputs[1].input.text = "2";
        this.localPlayer.SetValue(null, null);
        Assert.DoesNotThrow(() => this.inputs[1].input.onEndEdit.Invoke("2"));
        Assert.That(this.submissions, Is.Zero);
    }
}
