using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using MoonSharp.Interpreter;

public static class ModelValidation
{
    public const int MaximumChips = 1024;
    public const int MaximumVariables = 256;
    public const int MaximumAttachmentDepth = 64;
    static readonly HashSet<string> reserved = new HashSet<string>((
        "and break do else elseif end false for function goto if in local nil not or repeat return then true until while " +
        "_G _ENV Loop Key KeyDown KeyUp Mouse IsClicked MouseDown MouseUp Sin Cos Print SetVar GetVar").Split(' '));

    public static bool IsIdentifier(string name) => !string.IsNullOrEmpty(name) && name.Length <= 64 &&
        (name[0] == '_' || (name[0] >= 'a' && name[0] <= 'z') || (name[0] >= 'A' && name[0] <= 'Z')) &&
        name.All(c => c == '_' || (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') || (c >= '0' && c <= '9'));

    public static bool IsVariableName(string name) => IsIdentifier(name) && !reserved.Contains(name);

    public static void Validate(Table model, IDictionary<string, string[]> properties, IDictionary<string, int> options,
        IEnumerable<string> spawnableTypes)
    {
        if (model == null) { throw new ArgumentException("A model table is required."); }
        foreach (var key in model.Keys)
            if (key.Type != DataType.String || (key.String != "chips" && key.String != "variables" && key.String != "script"))
                throw new ArgumentException("Unknown model field.");
        var chips = List(model.Get("chips"), "chips", MaximumChips);
        var variables = List(model.Get("variables"), "variables", MaximumVariables);
        if (chips.Length == 0) { throw new ArgumentException("A model requires a Core chip."); }
        var source = model.Get("script");
        if (!source.IsNil() && (source.Type != DataType.String || source.String.Length > LuaExecution.MaximumSourceLength))
            throw new ArgumentException("Model script is not a string or exceeds its size limit.");
        var names = new HashSet<string>(StringComparer.Ordinal);
        foreach (var entry in variables)
        {
            var variable = RequireTable(entry, "variable");
            string name = Text(variable.Get("name"), "variable name");
            if ((name.Length > 0 && !IsVariableName(name)) || !names.Add(name))
                throw new ArgumentException("Variable names must be unique Lua identifiers, separate from the script API.");
            foreach (string field in new[] { "defaultValue", "minValue", "maxValue", "backstep" })
                ModelNumbers.Read(variable.Get(field));
        }
        var allowedTypes = new HashSet<string>(spawnableTypes, StringComparer.Ordinal) { "Core" };
        var byId = new Dictionary<string, Table>(StringComparer.Ordinal);
        var parents = new Dictionary<string, string>(StringComparer.Ordinal);
        var sensorNames = new HashSet<string>(StringComparer.Ordinal);
        int cores = 0;
        foreach (var entry in chips)
        {
            var chip = RequireTable(entry, "chip");
            string id = Text(chip.Get("id"), "chip ID");
            if (id.Length == 0 || id.Length > 128 || id.Any(char.IsControl) || byId.ContainsKey(id))
                throw new ArgumentException("Chip IDs must be nonempty and unique.");
            byId.Add(id, chip);
            string type = Text(chip.Get("Type"), "chip type");
            if (!allowedTypes.Contains(type) || !properties.TryGetValue(type, out var schema))
                throw new ArgumentException("Unsupported chip type: " + type);
            string parent = chip.Get("parentId").IsNil() ? "" : Text(chip.Get("parentId"), "parent ID");
            parents.Add(id, parent);
            string orientation = ScalarText(chip.Get("orientation"), "orientation");
            if (!int.TryParse(orientation, NumberStyles.Integer, CultureInfo.InvariantCulture, out int direction) || direction < 0 || direction > 3)
                throw new ArgumentException("Chip orientation must be from 0 to 3.");
            chip.Set("orientation", DynValue.NewString(orientation));
            if (type == "Core")
            {
                cores++;
                if (id != "a" || parent.Length != 0) { throw new ArgumentException("The Core must be root chip 'a'."); }
            }
            else if (parent.Length == 0 || parent == id) { throw new ArgumentException("Every other chip needs a different parent."); }
            foreach (var pair in chip.Pairs.ToArray())
            {
                string key = Text(pair.Key, "chip field");
                if (key == "id" || key == "parentId" || key == "orientation") { continue; }
                if (!schema.Contains(key)) { throw new ArgumentException(type + " does not support property " + key); }
                string value = ScalarText(pair.Value, key);
                chip.Set(key, DynValue.NewString(value));
                if (key == "Spring" || key == "Damper")
                {
                    if (ModelNumbers.Parse(value) < 0) { throw new ArgumentException(key + " cannot be negative."); }
                }
                else if (key == "Option")
                {
                    if (!int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int option) ||
                        !options.TryGetValue(type, out int count) || option < 0 || option >= count)
                        throw new ArgumentException("Invalid option for " + type);
                }
                else if (key == "Name" && !IsIdentifier(value)) { throw new ArgumentException("Chip name must be a Lua identifier."); }
                else if (key == "Colour" && !IsVariableName(value) &&
                    !int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out _) &&
                    !(value.StartsWith("#", StringComparison.Ordinal) && new[] { 4, 5, 7, 9 }.Contains(value.Length) && value.Skip(1).All(Uri.IsHexDigit)))
                    throw new ArgumentException("Colour requires a valid hexadecimal colour, integer or variable name.");
                else if (key == "Angle" || key == "Value" || key == "Brake")
                {
                    if (!ModelNumbers.TryParse(value, out _) && !IsVariableName(value))
                        throw new ArgumentException(key + " requires a finite number or variable name.");
                }
            }
            if (type == "Sensor")
            {
                string name = chip.Get("Name").IsNil() ? "chip_name" : chip.Get("Name").String;
                if (!IsVariableName(name) || names.Contains(name) || !sensorNames.Add(name))
                    throw new ArgumentException("Sensor names must be unique and separate from variables and the script API.");
            }
        }
        if (cores != 1) { throw new ArgumentException("A model requires exactly one Core chip."); }
        foreach (string id in byId.Keys)
        {
            string current = id;
            var path = new HashSet<string>(StringComparer.Ordinal);
            while (current != "a")
            {
                if (!path.Add(current) || path.Count > MaximumAttachmentDepth)
                    throw new ArgumentException("Chip attachments contain a cycle or exceed the depth limit.");
                if (!parents.TryGetValue(current, out current) || !byId.ContainsKey(current))
                    throw new ArgumentException("A chip refers to a missing parent.");
            }
        }
    }

    static DynValue[] List(DynValue value, string name, int limit)
    {
        var table = RequireTable(value, name);
        var entries = table.Pairs.ToArray();
        if (entries.Length > limit || entries.Any(pair => pair.Key.Type != DataType.Number ||
            pair.Key.Number < 1 || pair.Key.Number != Math.Floor(pair.Key.Number) || pair.Key.Number > entries.Length))
            throw new ArgumentException(name + " must be a bounded, contiguous list.");
        return Enumerable.Range(1, entries.Length).Select(table.Get).ToArray();
    }

    static Table RequireTable(DynValue value, string label) => value.Type == DataType.Table ? value.Table :
        throw new ArgumentException("Expected a table for " + label);
    static string Text(DynValue value, string label) => value.Type == DataType.String ? value.String :
        throw new ArgumentException("Expected a string for " + label);
    static string ScalarText(DynValue value, string label) => value.Type == DataType.Number ?
        ModelNumbers.Format(ModelNumbers.Read(value)) : Text(value, label);
}
