using System;
using System.Globalization;
using MoonSharp.Interpreter;

// Model files and network snapshots use one numeric format regardless of the player's locale.
public static class ModelNumbers
{
    public static string Format(float value)
    {
        if (float.IsNaN(value) || float.IsInfinity(value))
            throw new ArgumentException("Model numbers must be finite.");
        return value.ToString("R", CultureInfo.InvariantCulture);
    }

    public static bool TryParse(string text, out float value)
    {
        // Accept decimal commas from older chip files and localized input, without treating them as thousands separators.
        if (text != null && text.IndexOf('.') < 0 && text.IndexOf(',') >= 0)
            text = text.Replace(',', '.');
        return float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out value) &&
            !float.IsNaN(value) && !float.IsInfinity(value);
    }

    public static float Parse(string text)
    {
        if (!TryParse(text, out float value)) { throw new FormatException("Expected a finite model number."); }
        return value;
    }

    public static float Read(DynValue value)
    {
        if (value.Type == DataType.Number && !double.IsNaN(value.Number) &&
            !double.IsInfinity(value.Number) && Math.Abs(value.Number) <= float.MaxValue)
            return (float)value.Number;
        if (value.Type == DataType.String) { return Parse(value.String); }
        throw new FormatException("Expected a finite Lua number.");
    }
}
