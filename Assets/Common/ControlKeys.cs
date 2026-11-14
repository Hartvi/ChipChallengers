public static class ControlKeys
{
    public const string Supported = "abcdefghijklmnopqrstuvwxyz0123456789 !\"#$&'()*+,-./:;<=>?@[\\]^_`\t\b\n\u001b";
    public static char Normalize(char key) => char.ToLowerInvariant(key);
    public static bool IsSupported(char key) => Supported.IndexOf(Normalize(key)) >= 0;
}
