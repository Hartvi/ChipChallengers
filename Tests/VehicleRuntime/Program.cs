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
}
