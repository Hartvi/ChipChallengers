using System.Text;

public static class LuaStrings
{
    // Quoted strings preserve leading newlines and cannot be terminated by a nested Lua long string/comment.
    public static string Quote(string value)
    {
        var result = new StringBuilder("\"");
        foreach (char c in value ?? string.Empty)
        {
            switch (c)
            {
                case '\\': result.Append("\\\\"); break;
                case '"': result.Append("\\\""); break;
                case '\n': result.Append("\\n"); break;
                case '\r': result.Append("\\r"); break;
                default:
                    if (c < ' ' || c == 127)
                    {
                        // Three digits keep a following numeric character outside the escape.
                        result.Append('\\').Append(((int)c).ToString("D3", System.Globalization.CultureInfo.InvariantCulture));
                    }
                    else { result.Append(c); }
                    break;
            }
        }
        return result.Append('"').ToString();
    }
}
