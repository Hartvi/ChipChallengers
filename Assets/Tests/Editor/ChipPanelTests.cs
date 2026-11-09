using System;
using System.Reflection;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ChipPanelTests
{
    GameObject root;
    ChipPanel panel;
    VChip chip;
    int modelChanges;

    [SetUp]
    public void SetUp()
    {
        modelChanges = 0;
        root = new GameObject("Chip panel regression test", typeof(RectTransform));
        panel = root.AddComponent<ChipPanel>();
        // Exercise the controls without starting the network initialization coroutine.
        panel.enabled = false;
        panel.Setup();
        panel.AddChildren();
        var items = root.GetComponentsInChildren<ItemBase>(true);
        SetField("NameItem", items[0]);
        SetField("ValueItem", items[1]);
        SetField("PopUpItem", items[2]);

        chip = new VChip(VChip.wheelStr, (LocalDirection)0, null);
        panel.DisplayChip(chip);
        chip.vals.SetSetListeners(new Action[] {
            () => {
                ++modelChanges;
                // A model rebuild refreshes the panel and closes any open popup.
                panel.DisplayChip(chip);
            }
        });
    }

    [TearDown]
    public void TearDown()
    {
        UnityEngine.Object.DestroyImmediate(root);
    }

    [TestCase(VChip.optionStr, 1, "1")]
    [TestCase(VChip.typeStr, 1, VChip.rudderStr)]
    public void LosingFocusBeforePopupClickDoesNotRebuild(string property, int choice, string expected)
    {
        int index = Array.IndexOf((string[])chip.keys, property);
        var input = GetField<TMP_InputField[]>("currentInputs")[index];
        input.onSelect.Invoke(input.text);
        var popup = GetField<ItemBase>("PopUpItem");
        var buttons = popup.Siblings<ItemBase>(false);

        // Pressing the button ends editing before releasing the mouse fires onClick.
        input.onEndEdit.Invoke(input.text);
        Assert.That(modelChanges, Is.Zero, "Focus loss must not rebuild an unchanged model.");
        Assert.That(buttons[choice].gameObject.activeSelf, Is.True);

        buttons[choice].GetComponent<Button>().onClick.Invoke();
        Assert.That(input.text, Is.EqualTo(expected));
        Assert.That(chip.vals[Array.IndexOf((string[])chip.keys, property)], Is.EqualTo(expected));
        Assert.That(modelChanges, Is.EqualTo(1), "The choice should update the model once.");
        Assert.That(popup.Siblings<ItemBase>(false), Is.Empty);
    }

    [Test]
    public void ManualOptionEditStillUpdatesModelOnce()
    {
        int index = Array.IndexOf((string[])chip.keys, VChip.optionStr);
        var input = GetField<TMP_InputField[]>("currentInputs")[index];
        input.SetTextWithoutNotify("2");
        input.onEndEdit.Invoke(input.text);
        Assert.That(chip.vals[index], Is.EqualTo("2"));
        Assert.That(modelChanges, Is.EqualTo(1));

        input.onEndEdit.Invoke(input.text);
        Assert.That(modelChanges, Is.EqualTo(1));
    }

    void SetField(string name, object value)
    {
        typeof(ChipPanel).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(panel, value);
    }

    T GetField<T>(string name)
    {
        return (T)typeof(ChipPanel).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(panel);
    }
}
