using System;
using System.IO;
using System.Reflection;
using NUnit.Framework;

public class ScriptDraftTests
{
    [Test]
    public void UnfinishedScriptCanBeSavedAndReloadedWithoutExecutingIt()
    {
        string directory = Path.Combine(Path.GetTempPath(), "ChipDraft-" + Guid.NewGuid().ToString("N"));
        var storageField = typeof(IOHelpers).GetField("modelFiles", BindingFlags.Static | BindingFlags.NonPublic);
        object previous = storageField.GetValue(null);
        try
        {
            storageField.SetValue(null, new ModelFileStore(Path.Combine(directory, "user"), Path.Combine(directory, "bundled")));
            var model = VModel.FromLuaModel("{chips={{id='a',Type='Core',orientation='0',parentId=''}},variables={},script='function Loop('}");
            using (NetworkTestObjects.ClientScope()) model.SaveThisModelToFile("unfinished");
            Assert.That(VModel.LoadModelFromFile("unfinished").script, Is.EqualTo("function Loop("));
            var script = LuaExecution.CreateScript();
            var environment = new VehicleLuaEnvironment(script, new System.Collections.Generic.Dictionary<string,VVar>());
            var program = new VehicleLuaProgram(script, environment, model.script);
            Assert.That(program.Initialize(), Is.False);
            Assert.That(program.Error, Is.Not.Empty);
            Assert.That(program.CallLoop(), Is.False);
        }
        finally
        {
            storageField.SetValue(null, previous);
            if (Directory.Exists(directory)) Directory.Delete(directory, true);
        }
    }
}
