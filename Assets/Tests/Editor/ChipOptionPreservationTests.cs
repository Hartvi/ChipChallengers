using System;
using NUnit.Framework;

public class ChipOptionPreservationTests
{
    [TestCase("Chip", "Rudder", 0, 0)]
    [TestCase("Chip", "Rudder", 1, 1)]
    [TestCase("Chip", "Rudder", 2, 2)]
    [TestCase("Chip", "Rudder", 3, 3)]
    [TestCase("Rudder", "Axle", 2, 2)]
    [TestCase("Wheel", "Chip", 2, 0)]
    [TestCase("Sensor", "Wheel", 5, 0)]
    [TestCase("Gun", "Wheel", 0, 0)]
    public void TypeChangesPreserveCompatibleVariantsAndNotifyOnce(string from, string to, int option, int expected)
    {
        var root = new VChip(new[] { VChip.typeStr }, new[] { VChip.coreStr }, 0, null);
        var chip = new VChip(from, (LocalDirection)0, root);
        chip.vals[Array.IndexOf((string[])chip.keys, VChip.optionStr)] = option.ToString();
        var model = new VModel { chips = new[] { root, chip } };
        int changes = 0;
        model.AddModelChangedCallback(_ => changes++);
        chip.ChangeType(to);
        Assert.That(chip.TryGetProperty<int>(VChip.optionStr, out int result), Is.True);
        Assert.That(result, Is.EqualTo(expected));
        Assert.That(changes, Is.EqualTo(1));
        Assert.DoesNotThrow(() => VModel.FromLuaModel(model.ToLuaString()));
    }
}
