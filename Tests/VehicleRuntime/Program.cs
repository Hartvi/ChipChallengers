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
        foreach (string text in new[] { "", "\nleading newline", "\r\nwindows\r\nlines", "--[[ comment ]]\nfunction Loop() end",
            "local s = [=[nested ]] strings]=]", "quotes '\" and \\slashes", "\0" + "123\t\b", "café and 中文" })
        {
            Check(new Script().DoString("return " + LuaStrings.Quote(text)).String == text,
                "Lua text did not round trip exactly");
        }
        Console.WriteLine("PASS: " + checks + " vehicle runtime checks.");
    }
}
