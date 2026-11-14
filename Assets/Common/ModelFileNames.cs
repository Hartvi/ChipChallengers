using System;
using System.Linq;

public static class ModelFileNames
{
    public static string Normalize(string input)
    {
        string name = input?.Trim().Normalize();
        if (string.IsNullOrEmpty(name) || name.Any(c => char.IsControl(c) || "/\\:*?\"<>|".IndexOf(c) >= 0))
            throw new ArgumentException("Enter a model name without directory paths or filename punctuation.");
        string stem = name.EndsWith(".txt", StringComparison.OrdinalIgnoreCase) ? name.Substring(0, name.Length - 4) : name;
        if (stem.Length == 0 || stem.Length > 128 || stem.EndsWith(".", StringComparison.Ordinal) || stem.EndsWith(" ", StringComparison.Ordinal))
            throw new ArgumentException("Use a model name of 1 to 128 characters, without a trailing dot or space.");
        string device = stem.Split('.')[0].ToUpperInvariant();
        if (new[] { "CON", "PRN", "AUX", "NUL" }.Contains(device) ||
            (device.Length == 4 && (device.StartsWith("COM") || device.StartsWith("LPT")) && "123456789¹²³".IndexOf(device[3]) >= 0))
            throw new ArgumentException("This model name is reserved by the operating system.");
        return stem + ".txt";
    }

    public static bool TryNormalize(string input, out string filename)
    {
        try { filename = Normalize(input); return true; }
        catch (ArgumentException) { filename = null; return false; }
    }

    public static string DisplayName(string filename)
    {
        string canonical = Normalize(filename);
        string stem = canonical.Substring(0, canonical.Length - 4);
        // Keep repeated suffixes unambiguous: normalizing every displayed label must find the same file.
        return stem.EndsWith(".txt", StringComparison.OrdinalIgnoreCase) ? canonical : stem;
    }
}
