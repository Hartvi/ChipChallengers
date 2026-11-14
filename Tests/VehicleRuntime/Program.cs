using System;
using System.Globalization;
using MoonSharp.Interpreter;

// Only the unused owner reference is stubbed; the variable implementation and interpreter are production code.
public class VModel { }

partial class Program
{
    static int checks;
    static void Check(bool condition, string message)
    {
        if (!condition) { throw new Exception(message); }
        checks++;
    }

    static void CheckNumbers()
    {
        var originalCulture = CultureInfo.CurrentCulture;
        try
        {
            foreach (string culture in new[] { "en-US", "fr-FR", "de-DE" })
            {
                CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(culture);
                var variable = new VVar { name = "power", minValue = -10.125f, maxValue = 100.25f,
                    defaultValue = 0.5f, backstep = 0.03125f };
                var lua = new Script();
                var restored = new VVar(lua.DoString("return " + variable.ToLuaString()).Table);
                Check(restored.defaultValue == variable.defaultValue && restored.minValue == variable.minValue &&
                    restored.maxValue == variable.maxValue && restored.backstep == variable.backstep,
                    "Variable numeric round trip failed for " + culture);
                Check(ModelNumbers.Parse("1.25") == 1.25f && ModelNumbers.Parse("1,25") == 1.25f,
                    "Chip number parsing depended on " + culture);
                Check(!ModelNumbers.TryParse("NaN", out _) && !ModelNumbers.TryParse("Infinity", out _),
                    "Nonfinite model number accepted");
            }
        }
        finally { CultureInfo.CurrentCulture = originalCulture; }
    }

    static void Main()
    {
        CheckNumbers();
        CheckLuaVariables();
        CheckProgramInitialization();
        CheckLuaLimits();
        CheckModelValidation();
        CheckControlTiming();
        var keyInput = new InputMessage();
        var emptyButtons = new bool[3];
        keyInput.Accumulate(new[] { 'W', '1', ' ' }, new[] { 'W' }, new[] { '1' }, emptyButtons, emptyButtons, emptyButtons, new float[2]);
        Check(keyInput.Held('w') && keyInput.Held('W') && keyInput.Held('1') && keyInput.Held(' '), "Supported keys or uppercase aliases were lost");
        Check(keyInput.Down('w') && keyInput.Up('1') && !keyInput.Held('é'), "Key edge/unsupported-key handling failed");
        keyInput.Accumulate(null, null, null, null, null, null, null);
        Check(!keyInput.Held('w') && keyInput.KeysDown.Length == 0, "Malformed input did not clear retained controls");
        CheckGunTiming();
        CheckUserModelFiles();
        Check(Math.Abs(GunFireControl.ProjectileMass(100) - 0.01f) < 1e-8f &&
            GunFireControl.ProjectileMass(1e-30f) > 0, "Projectile/recoil mass is invalid");
        var history = new HistoryStack();
        Check(history.Undo() == null && history.Redo() == null, "Empty history depends on a local player");
        history.SaveState("A"); history.SaveState("B"); history.SaveState("C");
        Check(history.Undo() == "B" && history.Redo() == "C", "Undo/redo did not retain the active branch");
        history.Undo(); history.SaveState("D");
        Check(history.Redo() == "D" && history.Undo() == "B", "An abandoned redo branch survived a new edit");
        Check(InputCoordinates.Normalize(1440, 1920) == InputCoordinates.Normalize(2880, 3840), "Mouse aim depends on client resolution");
        Check(InputCoordinates.Normalize(0, 1920) == -1 && InputCoordinates.Normalize(960, 1920) == 0 &&
            InputCoordinates.Normalize(1920, 1920) == 1, "Mouse viewport coordinates changed");
        Check(InputCoordinates.Normalize(100, 0) == 0 && InputCoordinates.Clamp(float.NaN) == 0, "Invalid mouse coordinates were accepted");
        foreach (string text in new[] { "", "\nleading newline", "\r\nwindows\r\nlines", "--[[ comment ]]\nfunction Loop() end",
            "local s = [=[nested ]] strings]=]", "quotes '\" and \\slashes", "\0" + "123\t\b", "café and 中文" })
        {
            Check(new Script().DoString("return " + LuaStrings.Quote(text)).String == text,
                "Lua text did not round trip exactly");
        }
        Console.WriteLine("PASS: " + checks + " vehicle runtime checks.");
    }

    static void CheckLuaVariables()
    {
        var lua = new Script();
        var variable = new VVar { minValue = -100, maxValue = 100 };
        var environment = new VehicleLuaEnvironment(lua,
            new System.Collections.Generic.Dictionary<string, VVar> { { "a", variable } });
        lua.Globals["Key"] = (Func<string, bool>)(_ => true);
        lua.DoString("if a == 0 then a = 1 end", environment.Globals);
        Check(variable.currentValue == 1 && variable.hasChanged, "Lua comparison/assignment failed");
        lua.DoString("if Key('w') then a = a + 1 end", environment.Globals);
        Check(variable.currentValue == 2, "Inline Lua assignment failed");
        lua.DoString("local a = 9; result = a; function Loop() local a = 3; result = a end", environment.Globals);
        Check(variable.currentValue == 2 && environment.Get("result").Number == 9, "Local Lua scope was changed");
        lua.Call(environment.Get("Loop"));
        Check(variable.currentValue == 2 && environment.Get("result").Number == 3, "Loop local scope was changed");
        lua.DoString("literal = 'value a changed'; -- a = 99\n t = {a = 8}; a = t.a", environment.Globals);
        Check(environment.Get("literal").String == "value a changed" && variable.currentValue == 8,
            "Lua strings, comments or table fields were changed");
        lua.DoString("_G.a = a + 1", environment.Globals);
        Check(variable.currentValue == 9, "_G did not resolve declared variables");
    }

    static void CheckProgramInitialization()
    {
        var lua = new Script();
        var environment = new VehicleLuaEnvironment(lua, new System.Collections.Generic.Dictionary<string, VVar>());
        foreach (string name in new[] { "front", "back" })
        {
            var sensor = new Table(lua);
            sensor["Read"] = (Func<float>)(() => 3f);
            lua.Globals[name] = sensor;
        }
        var program = new VehicleLuaProgram(lua, environment,
            "count = (count or 0) + 1; initial = front.Read() + back.Read(); function Loop() result = initial end");
        Check(program.Initialize() && program.Initialize(), "Program did not initialize");
        Check(environment.Get("count").Number == 1 && environment.Get("initial").Number == 6,
            "Sensor initialization ran repeatedly or before all bindings existed");
        Check(program.CallLoop() && environment.Get("result").Number == 6, "Initialized loop failed");
        var broken = new VehicleLuaProgram(lua, environment, "function Loop(");
        Check(!broken.Initialize() && broken.Error != null && !broken.CallLoop(), "Syntax failure escaped initialization");
        var runtimeError = new VehicleLuaProgram(lua, environment, "missing.Read()");
        Check(!runtimeError.Initialize() && runtimeError.Error != null, "Top-level sensor error escaped initialization");
    }

    static void CheckLuaLimits()
    {
        foreach (string source in new[] { "while true do end", "local function spin() return spin() end; spin()",
            "coroutine.resume(coroutine.create(function() while true do end end))", "string.rep('x', 1000000000)" })
        {
            bool rejected = false;
            try { LuaExecution.Run(LuaExecution.CreateScript(), source); }
            catch (InterpreterException) { rejected = true; }
            Check(rejected, "Unbounded script was not stopped");
        }
        var valid = LuaExecution.CreateScript();
        Check(LuaExecution.Run(valid, "local total = 0; for i=1,100 do total=total+i end; return total").Number == 5050,
            "Instruction limit rejected an ordinary program");
        foreach (string source in new[] { "{chips = (function() while true do end end)()}", "{}; while true do end", "{x = 1 + 2}" })
        {
            bool rejected = false;
            try { LuaModelData.Parse(source); }
            catch (FormatException) { rejected = true; }
            Check(rejected, "Model parser accepted executable code");
        }
        foreach (string text in new[] { "--[[nested]]\nlocal value = [=[hello]=]", "\0" + "123\t", "\r\ntext", "quotes '\"\\" })
            Check(LuaModelData.Parse("{script=" + LuaStrings.Quote(text) + "}").Get("script").String == text,
                "Literal data parser changed script contents");
        foreach (string file in System.IO.Directory.GetFiles("Assets/StreamingAssets/Models", "*.txt"))
            Check(LuaModelData.Parse(System.IO.File.ReadAllText(file)).Get("chips").Type == DataType.Table,
                "Existing vehicle no longer parses: " + file);
        foreach (string file in new[] { "Assets/StreamingAssets/chips.lua", "Assets/StreamingAssets/optionNames.lua" })
            Check(LuaModelData.Parse(System.IO.File.ReadAllText(file)).Get("keys").Type == DataType.Table,
                "Chip specification no longer parses");
    }

    static void CheckModelValidation()
    {
        var definitions = LuaModelData.Parse(System.IO.File.ReadAllText("Assets/StreamingAssets/chips.lua"));
        var optionDefinitions = LuaModelData.Parse(System.IO.File.ReadAllText("Assets/StreamingAssets/optionNames.lua"));
        var properties = new System.Collections.Generic.Dictionary<string, string[]>();
        var options = new System.Collections.Generic.Dictionary<string, int>();
        for (int i = 1; i <= definitions.Get("keys").Table.Length; i++)
            properties.Add(definitions.Get("keys").Table.Get(i).String,
                System.Linq.Enumerable.ToArray(System.Linq.Enumerable.Select(definitions.Get("values").Table.Get(i).Table.Values, value => value.String)));
        for (int i = 1; i <= optionDefinitions.Get("keys").Table.Length; i++)
            options.Add(optionDefinitions.Get("keys").Table.Get(i).String, optionDefinitions.Get("values").Table.Get(i).Table.Length);
        foreach (string file in System.IO.Directory.GetFiles("Assets/StreamingAssets/Models", "*.txt"))
        {
            ModelValidation.Validate(LuaModelData.Parse(System.IO.File.ReadAllText(file)), properties, options, properties.Keys);
            Check(true, "Bundled model validation failed: " + file);
        }
        const string root = "{id='a',Type='Core',orientation='0',parentId=''}";
        foreach (string chips in new[] { "", "{id='a',Type='Wheel',orientation='0',parentId=''}", root + "," + root,
            root + ",{id='b',Type='Chip',orientation='0',parentId='missing'}",
            root + ",{id='b',Type='Chip',orientation='0',parentId='c'},{id='c',Type='Chip',orientation='0',parentId='b'}",
            root + ",{id='b',Type='Wheel',orientation='9',parentId='a'}",
            root + ",{id='b',Type='Wheel',orientation='0',parentId='a',Option='999'}",
            root + ",{id='b',Type='Chip',orientation='0',parentId='a',Colour='#bad-colour'}" })
        {
            bool rejected = false;
            try { ModelValidation.Validate(LuaModelData.Parse("{chips={" + chips + "},variables={},script=''}"), properties, options, properties.Keys); }
            catch (ArgumentException) { rejected = true; }
            Check(rejected, "Malformed model graph was accepted");
        }
    }

    static void CheckControlTiming()
    {
        foreach (int rate in new[] { 30, 50, 60, 120, 500, 1000 })
        {
            var clock = new VehicleControlClock();
            var variable = new VVar { minValue = -100, maxValue = 100, backstep = 1 };
            variable.currentValue = 50;
            int ticks = 0;
            for (int i = 0; i < rate; i++)
                clock.Advance(1.0 / rate, seconds => { ticks++; VehicleControlClock.Backstep(new[] { variable }); });
            Check(ticks == 50 && variable.currentValue == 0, "Control/backstep rate changed at " + rate + " Hz");
        }
        var input = new InputMessage();
        var buttons = new[] { false, false, false };
        input.Accumulate(new[] { 'w' }, new[] { 'w' }, Array.Empty<char>(), buttons, new[] { true, false, false }, buttons, new float[2]);
        input.Accumulate(new[] { 'w' }, Array.Empty<char>(), Array.Empty<char>(), buttons, buttons, buttons, new float[2]);
        Check(input.KeysDown.Length == 1 && input.MouseDown[0], "An input edge was lost between control ticks");
        input.ConsumeEdges();
        Check(input.KeysDown.Length == 0 && !input.MouseDown[0] && input.Keys.Length == 1, "Input edges repeated or held state was lost");
    }

    static void CheckGunTiming()
    {
        foreach (int rate in new[] { 50, 500, 1000 })
        {
            foreach (float power in new[] { 100f, 0.001f })
            {
                var fire = new GunFireControl();
                int shots = 0;
                for (int i = 0; i < rate; i++) if (fire.Step(power, true, 1f / rate, out _)) { shots++; }
                Check(shots == (power == 100f ? 10 : 50), "Gun shot rate changed with the physics rate");
            }
        }
        foreach (float power in new[] { 0, -1, float.NaN, float.PositiveInfinity })
            Check(!new GunFireControl().Step(power, true, 1, out _), "Invalid/disabled gun power fired a shot");
        var changing = new GunFireControl();
        Check(!changing.Step(0, true, 1, out _) && changing.Step(100, true, 0.1f, out float activePower) && activePower == 100,
            "A gun did not respond to changed power");
    }

    static void CheckUserModelFiles()
    {
        string root = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "chip-model-files-" + Guid.NewGuid().ToString("N"));
        string bundled = System.IO.Path.Combine(root, "installed"), user = System.IO.Path.Combine(root, "user");
        System.IO.Directory.CreateDirectory(bundled);
        try
        {
            System.IO.File.WriteAllText(System.IO.Path.Combine(bundled, "car.txt"), "legacy car");
            var files = new ModelFileStore(user, bundled);
            files.MigrateLegacyFiles();
            Check(files.Read("car.txt") == "legacy car" && System.IO.File.Exists(System.IO.Path.Combine(user, "car.txt")), "Legacy model was not migrated");
            files.Write("car.txt", "player edit");
            files.MigrateLegacyFiles();
            Check(files.Read("car.txt") == "player edit" && System.IO.File.ReadAllText(System.IO.Path.Combine(bundled, "car.txt")) == "legacy car",
                "User save changed installed content or migration replaced user work");
            Check(System.IO.File.ReadAllText(System.IO.Path.Combine(user, "car.txt.bak")) == "legacy car", "Atomic overwrite did not retain its backup");
            try { AtomicTextFile.Write(System.IO.Path.Combine(user, "car.txt"), writer => { writer.Write("partial"); throw new System.IO.IOException("Injected write failure"); }); }
            catch (System.IO.IOException) { }
            Check(files.Read("car.txt") == "player edit" && System.IO.Directory.GetFiles(user, "*.tmp").Length == 0,
                "An interrupted save damaged the model or retained its partial file");
            System.IO.File.WriteAllText(System.IO.Path.Combine(bundled, "new-example.txt"), "new example");
            Check(files.Read("new-example.txt") == "new example" && files.Names().Length == 2, "New bundled examples became unavailable");
        }
        finally
        {
            if (System.IO.Path.GetFullPath(root).StartsWith(System.IO.Path.GetFullPath(System.IO.Path.GetTempPath()), StringComparison.OrdinalIgnoreCase))
                System.IO.Directory.Delete(root, true);
        }
    }
}
