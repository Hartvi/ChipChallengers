using System;
using System.Linq;
using System.Reflection;
using Mirror;
using NUnit.Framework;
using UnityEngine;

public class SensorPasteTests
{
    [Test]
    public void LegacySensorWithoutExplicitNameGetsAUniqueNameForItsCopy()
    {
        var model = VModel.FromLuaModel("{chips={{id='a',Type='Core',orientation='0',parentId=''},{id='b',Type='Sensor',orientation='0',parentId='a',Option='0'}},variables={},script=''}");
        var copied = (VChip[])typeof(Clipboard).GetMethod("CopyChildren", BindingFlags.Static | BindingFlags.NonPublic)
            .Invoke(null, new object[] { model.Core, new[] { model.chips[1] }, false });
        model.PrepareSensorNames(copied);
        model.chips = model.chips.Concat(copied).ToArray();
        Assert.DoesNotThrow(() => VModel.FromLuaModel(model.ToLuaString()));
        Assert.That(ArrayExtensions.AccessLikeDict(VChip.nameStr, copied[0].keys, copied[0].vals), Is.EqualTo("chip_name_1"));
    }

    [TestCase(false)]
    [TestCase(true)]
    public void PastingSensorsRepeatedlyAllocatesUniqueUsableNames(bool mirror)
    {
        var player = new GameObject("Sensor paste test");
        player.SetActive(false);
        var local = typeof(NetworkClient).GetProperty("localPlayer");
        var clipboard = typeof(Clipboard).GetField("newParent", BindingFlags.Static | BindingFlags.NonPublic);
        object previousPlayer = local.GetValue(null), previousCopy = clipboard.GetValue(null);
        try
        {
            var core = NetworkTestObjects.Add<CoreChip>(player);
            var model = VModel.FromLuaModel("{chips={{id='a',Type='Core',orientation='0',parentId=''},{id='b',Type='Sensor',Name='probe',orientation='0',parentId='a',Option='0'}},variables={{name='probe_1',defaultValue=0,minValue=-1,maxValue=1,backstep=0}},script='function Loop() end'}");
            typeof(CoreChip).GetField("_ClientVirtualModel", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(core, model);
            local.SetValue(null, player.GetComponent<NetworkIdentity>());
            var container = new VChip(VChip.chipStr, (LocalDirection)0, null);
            container.Children = new[] { model.chips[1] };
            clipboard.SetValue(null, container);
            Clipboard.AttachTo(model.Core, 0, mirror);
            Clipboard.AttachTo(model.Core, 0, mirror);
            var loaded = VModel.FromLuaModel(model.ToLuaString());
            var names = loaded.chips.Where(chip => chip.ChipType == VChip.sensorStr)
                .Select(chip => ArrayExtensions.AccessLikeDict(VChip.nameStr, chip.keys, chip.vals)).ToArray();
            CollectionAssert.AreEquivalent(new[] { "probe", "probe_2", "probe_3" }, names);
            Assert.Throws<ArgumentException>(() => model.AddVariable(new VVar(new[] { "probe", "0", "-1", "1", "0" })));
            Assert.That(model.variables.Length, Is.EqualTo(1));
        }
        finally
        {
            local.SetValue(null, previousPlayer);
            clipboard.SetValue(null, previousCopy);
            UnityEngine.Object.DestroyImmediate(player);
        }
    }
}
