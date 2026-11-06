using System;
using MoonSharp.Interpreter;

// ScenarioLuaMetadata parses and validates scenario metadata and processor compatibility from Lua tables.
public static class ScenarioLuaMetadata
{
    public const int CurrentLogicVersion = 1;

    // Parse parses scenario identity, versions and rules from a Lua metadata table.
    public static ScenarioMetadata Parse(Table table, string fallbackId)
    {
        if (table == null) { throw new ArgumentException("Scenario expects a metadata table."); }
        var metadata = new ScenarioMetadata {
            scenarioId = ReadString(table, "id", fallbackId),
            title = ReadString(table, "title", ""),
            author = ReadString(table, "author", ""),
            completionType = ReadString(table, "completionType", "timeTrial"),
            recordVersion = ReadInt(table, "recordVersion", 1, 1),
            scenarioLogicVersion = ReadInt(table, "scenarioLogicVersion", 1, 1)
        };
        if (string.IsNullOrWhiteSpace(metadata.scenarioId))
        {
            throw new ArgumentException("Scenario requires a stable id.");
        }
        if (metadata.scenarioLogicVersion != CurrentLogicVersion)
        {
            throw new ArgumentException("Scenario expects unsupported logic version " + metadata.scenarioLogicVersion);
        }
        var rules = table.Get("rules");
        if (!rules.IsNil())
        {
            if (rules.Type != DataType.Table) { throw new ArgumentException("rules must be a table."); }
            metadata.rules.maxChips = ReadInt(rules.Table, "maxChips", -1, -1);
            var physics = rules.Table.Get("physics");
            if (!physics.IsNil())
            {
                if (physics.Type != DataType.Table) { throw new ArgumentException("rules.physics must be a table."); }
                var settings = metadata.rules.physics;
                settings.fixedDeltaTime = ReadTimestep(physics.Table, settings.fixedDeltaTime);
                settings.solverIterations = ReadInt(physics.Table, "solverIterations", settings.solverIterations, 1, 255);
                settings.solverVelocityIterations = ReadInt(physics.Table, "solverVelocityIterations", settings.solverVelocityIterations, 1, 255);
            }
            var forbidden = rules.Table.Get("forbiddenChipTypes");
            if (!forbidden.IsNil())
            {
                if (forbidden.Type != DataType.Table)
                {
                    throw new ArgumentException("forbiddenChipTypes must be a list.");
                }
                for (int i = 1; i <= forbidden.Table.Length; ++i)
                {
                    var value = forbidden.Table.Get(i);
                    if (value.Type != DataType.String || string.IsNullOrWhiteSpace(value.String))
                    {
                        throw new ArgumentException("Forbidden chip types must be nonempty strings.");
                    }
                    metadata.rules.forbiddenChipTypes.Add(value.String);
                }
            }
        }
        return metadata;
    }

    // ReadString reads an optional string field and rejects values of another type.
    static string ReadString(Table table, string key, string fallback)
    {
        var value = table.Get(key);
        if (value.IsNil()) { return fallback; }
        if (value.Type != DataType.String) { throw new ArgumentException(key + " must be a string."); }
        return value.String;
    }

    // ReadInt reads an optional integer field and enforces its allowed range.
    static int ReadInt(Table table, string key, int fallback, int minimum, int maximum = int.MaxValue)
    {
        var value = table.Get(key);
        if (value.IsNil()) { return fallback; }
        if (value.Type != DataType.Number || double.IsNaN(value.Number) ||
            value.Number < minimum || value.Number > maximum || value.Number != Math.Floor(value.Number))
        {
            throw new ArgumentException(key + " must be an integer between " + minimum + " and " + maximum);
        }
        return (int)value.Number;
    }

    // ReadTimestep accepts finite physics steps between 50 Hz and 2000 Hz.
    static float ReadTimestep(Table table, float fallback)
    {
        var value = table.Get("fixedDeltaTime");
        if (value.IsNil()) { return fallback; }
        if (value.Type != DataType.Number || double.IsNaN(value.Number) ||
            value.Number < 0.0005 || value.Number > 0.02)
        {
            throw new ArgumentException("fixedDeltaTime must be between 0.0005 and 0.02 seconds.");
        }
        return (float)value.Number;
    }

}
