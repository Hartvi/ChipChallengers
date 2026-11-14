using System;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;

public class BundledExampleTests
{
    [Test]
    public void LoadMenuSelectionsCanOpenBundledVersionWithoutReplacingLegacyWork()
    {
        string directory = Path.Combine(Path.GetTempPath(), "ChipBundled-" + Guid.NewGuid().ToString("N"));
        string user = Path.Combine(directory, "user"), bundled = Path.Combine(directory, "bundled");
        var field = typeof(IOHelpers).GetField("modelFiles", BindingFlags.Static | BindingFlags.NonPublic);
        object previous = field.GetValue(null);
        try
        {
            Directory.CreateDirectory(user); Directory.CreateDirectory(bundled);
            string model = "{chips={{id='a',Type='Core',orientation='0',parentId=''}},variables={},script='%SCRIPT%'}";
            File.WriteAllText(Path.Combine(user, "car.txt"), model.Replace("%SCRIPT%", "-- user work"));
            File.WriteAllText(Path.Combine(bundled, "car.txt"), model.Replace("%SCRIPT%", "-- updated example"));
            File.WriteAllText(Path.Combine(user, ".legacy-models-migrated"), "old migration");
            field.SetValue(null, new ModelFileStore(user, bundled));
            Assert.That(IOHelpers.GetLoadableModels().Contains(ModelFileStore.BundledPrefix + "car"), Is.True);
            Assert.That(VModel.LoadModelFromFile("car").script, Is.EqualTo("-- user work"));
            var example = VModel.LoadModelFromFile(ModelFileStore.BundledPrefix + "car");
            Assert.That(example.script, Is.EqualTo("-- updated example"));
            Assert.That(example.ModelName, Is.EqualTo("car"));
            Assert.That(File.ReadAllText(Path.Combine(user, "car.txt")), Does.Contain("-- user work"));
        }
        finally
        {
            field.SetValue(null, previous);
            if (Directory.Exists(directory)) Directory.Delete(directory, true);
        }
    }
}
