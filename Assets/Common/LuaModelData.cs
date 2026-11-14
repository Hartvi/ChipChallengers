using System;
using System.Globalization;
using System.Text;
using MoonSharp.Interpreter;

// Reads the literal table format used by existing vehicle files without executing Lua code.
public sealed class LuaModelData
{
    public const int MaximumLength = 1048576;
    const int MaximumDepth = 32;
    const int MaximumFields = 65536;
    readonly string source;
    int position;
    int fields;

    LuaModelData(string source) { this.source = source; }

    public static Table Parse(string source)
    {
        if (string.IsNullOrWhiteSpace(source) || source.Length > MaximumLength)
            throw new FormatException("Model data is empty or exceeds the size limit.");
        var reader = new LuaModelData(source);
        reader.Space();
        if (reader.Peek() != '{')
        {
            string prefix = reader.Identifier();
            if (prefix == "a") { reader.Expect('='); }
            else if (prefix != "return") { throw reader.Error("Expected a model table"); }
        }
        var value = reader.Value(0);
        reader.Space();
        if (reader.Peek() == ';') { reader.position++; reader.Space(); }
        if (reader.position != source.Length || value.Type != DataType.Table)
            throw reader.Error("Expected only literal model data");
        return value.Table;
    }

    char Peek() => this.position < this.source.Length ? this.source[this.position] : '\0';
    FormatException Error(string message) => new FormatException(message + " at model character " + this.position + ".");

    void Space()
    {
        while (this.position < this.source.Length)
        {
            if (char.IsWhiteSpace(this.Peek()) || this.Peek() == '\uFEFF') { this.position++; continue; }
            if (this.Peek() != '-' || this.position + 1 >= this.source.Length || this.source[this.position + 1] != '-') { break; }
            this.position += 2;
            if (this.LongOpening(out _)) { this.LongString(); }
            else { while (this.position < this.source.Length && this.Peek() != '\n' && this.Peek() != '\r') { this.position++; } }
        }
    }

    void Expect(char expected)
    {
        this.Space();
        if (this.Peek() != expected) { throw this.Error("Expected '" + expected + "'"); }
        this.position++;
    }

    string Identifier()
    {
        this.Space();
        int start = this.position;
        if (!char.IsLetter(this.Peek()) && this.Peek() != '_') { throw this.Error("Expected a field name"); }
        while (char.IsLetterOrDigit(this.Peek()) || this.Peek() == '_') { this.position++; }
        return this.source.Substring(start, this.position - start);
    }

    DynValue Value(int depth)
    {
        this.Space();
        char c = this.Peek();
        if (c == '{') { return DynValue.NewTable(this.Table(depth + 1)); }
        if (c == '\'' || c == '"') { return DynValue.NewString(this.Quoted()); }
        if (this.LongOpening(out _)) { return DynValue.NewString(this.LongString()); }
        if (c == '-' || c == '+' || c == '.' || char.IsDigit(c))
        {
            int start = this.position++;
            while (char.IsDigit(this.Peek()) || this.Peek() == '.' || this.Peek() == 'e' || this.Peek() == 'E' ||
                this.Peek() == '+' || this.Peek() == '-') { this.position++; }
            string number = this.source.Substring(start, this.position - start);
            if (!double.TryParse(number, NumberStyles.Float, CultureInfo.InvariantCulture, out double value) ||
                double.IsNaN(value) || double.IsInfinity(value)) { throw this.Error("Invalid number"); }
            return DynValue.NewNumber(value);
        }
        string identifier = this.Identifier();
        if (identifier == "nil") { return DynValue.Nil; }
        if (identifier == "true" || identifier == "false") { return DynValue.NewBoolean(identifier == "true"); }
        throw this.Error("Expressions and function calls are not allowed in model data");
    }

    Table Table(int depth)
    {
        if (depth > MaximumDepth) { throw this.Error("Model tables are nested too deeply"); }
        this.Expect('{');
        var table = new Table(null);
        int nextIndex = 1;
        this.Space();
        while (this.Peek() != '}')
        {
            if (++this.fields > MaximumFields) { throw this.Error("Model contains too many fields"); }
            DynValue key;
            if (this.Peek() == '[' && !this.LongOpening(out _))
            {
                this.position++;
                key = this.Value(depth);
                this.Expect(']'); this.Expect('=');
            }
            else if (char.IsLetter(this.Peek()) || this.Peek() == '_')
            {
                int start = this.position;
                string name = this.Identifier();
                this.Space();
                if (this.Peek() == '=') { this.position++; key = DynValue.NewString(name); }
                else { this.position = start; key = DynValue.NewNumber(nextIndex++); }
            }
            else { key = DynValue.NewNumber(nextIndex++); }
            if (key.Type != DataType.String && key.Type != DataType.Number) { throw this.Error("Invalid table key"); }
            if (!table.Get(key).IsNil()) { throw this.Error("Duplicate table field"); }
            table.Set(key, this.Value(depth));
            this.Space();
            if (this.Peek() == ',' || this.Peek() == ';') { this.position++; this.Space(); }
            else if (this.Peek() != '}') { throw this.Error("Expected a table separator"); }
        }
        this.position++;
        return table;
    }

    bool LongOpening(out int equals)
    {
        equals = 0;
        if (this.Peek() != '[') { return false; }
        int index = this.position + 1;
        while (index < this.source.Length && this.source[index] == '=') { equals++; index++; }
        return index < this.source.Length && this.source[index] == '[';
    }

    string LongString()
    {
        if (!this.LongOpening(out int equals)) { throw this.Error("Expected a long string"); }
        this.position += equals + 2;
        string closing = "]" + new string('=', equals) + "]";
        int end = this.source.IndexOf(closing, this.position, StringComparison.Ordinal);
        if (end < 0) { throw this.Error("Unclosed long string"); }
        string value = this.source.Substring(this.position, end - this.position).Replace("\r\n", "\n").Replace('\r', '\n');
        this.position = end + closing.Length;
        return value.StartsWith("\n", StringComparison.Ordinal) ? value.Substring(1) : value;
    }

    string Quoted()
    {
        char quote = this.source[this.position++];
        var result = new StringBuilder();
        while (this.position < this.source.Length)
        {
            char c = this.source[this.position++];
            if (c == quote) { return result.ToString(); }
            if (c == '\n' || c == '\r') { throw this.Error("Unescaped newline in a quoted string"); }
            if (c != '\\') { result.Append(c); continue; }
            if (this.position == this.source.Length) { break; }
            c = this.source[this.position++];
            switch (c)
            {
                case 'a': result.Append('\a'); break;
                case 'b': result.Append('\b'); break;
                case 'f': result.Append('\f'); break;
                case 'n': result.Append('\n'); break;
                case 'r': result.Append('\r'); break;
                case 't': result.Append('\t'); break;
                case 'v': result.Append('\v'); break;
                case '\\': case '\'': case '"': result.Append(c); break;
                case '\r': if (this.Peek() == '\n') { this.position++; } result.Append('\n'); break;
                case '\n': result.Append('\n'); break;
                case 'z': while (char.IsWhiteSpace(this.Peek())) { this.position++; } break;
                case 'x':
                    if (this.position + 2 > this.source.Length || !byte.TryParse(this.source.Substring(this.position, 2),
                        NumberStyles.HexNumber, CultureInfo.InvariantCulture, out byte hex)) { throw this.Error("Invalid hexadecimal escape"); }
                    result.Append((char)hex); this.position += 2; break;
                default:
                    if (c < '0' || c > '9') { throw this.Error("Unknown string escape"); }
                    int number = c - '0';
                    for (int count = 1; count < 3 && this.Peek() >= '0' && this.Peek() <= '9'; count++)
                        number = number * 10 + this.source[this.position++] - '0';
                    if (number > 255) { throw this.Error("Decimal escape is outside the byte range"); }
                    result.Append((char)number); break;
            }
        }
        throw this.Error("Unclosed quoted string");
    }
}
