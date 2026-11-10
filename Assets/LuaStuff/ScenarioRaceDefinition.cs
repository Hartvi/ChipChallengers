using System;
using System.Collections.Generic;
using System.Linq;
using MoonSharp.Interpreter;

// ChallengeRacePoint stores a race position without depending on a scene object.
public struct ChallengeRacePoint
{
    public float x, y, z;
    // ChallengeRacePoint initializes the three coordinates of a race position.
    public ChallengeRacePoint(float x, float y, float z) { this.x = x; this.y = y; this.z = z; }
}

// ChallengeRaceGate stores one checkpoint's position, radius and RGBA color.
public class ChallengeRaceGate
{
    public ChallengeRacePoint position;
    public float radius;
    public float[] color;
}

// ChallengeRaceDefinition describes ordered gates and optional lifecycle callbacks declared by Lua.
public class ChallengeRaceDefinition
{
    public readonly List<ChallengeRaceGate> gates = new List<ChallengeRaceGate>();
    public DynValue onStarted = DynValue.Nil;
    public DynValue onCheckpoint = DynValue.Nil;
    public DynValue onFinished = DynValue.Nil;

    // Parse validates a named race configuration while retaining the original gate-list format.
    public static ChallengeRaceDefinition Parse(Table configuration, DynValue legacyFinished)
    {
        if (configuration == null) { throw new ArgumentException("Race expects a configuration table."); }
        var race = new ChallengeRaceDefinition();
        if (!configuration.Get("start").IsNil() || !configuration.Get("finish").IsNil())
        {
            float radius = Number(configuration.Get("radius"), "radius", 5f);
            if (radius <= 0) { throw new ArgumentException("Race radius must be positive."); }
            var color = Color(configuration.Get("color"));
            race.gates.Add(Gate(configuration.Get("start"), "start", radius, color));
            var checkpoints = configuration.Get("checkpoints");
            if (!checkpoints.IsNil())
            {
                if (checkpoints.Type != DataType.Table)
                    throw new ArgumentException("Race checkpoints must be a list of positions.");
                for (int i = 1; i <= checkpoints.Table.Length; ++i)
                    race.gates.Add(Gate(checkpoints.Table.Get(i), "checkpoint " + i, radius, color));
            }
            race.gates.Add(Gate(configuration.Get("finish"), "finish", radius, color));
            race.onStarted = Callback(configuration.Get("onStarted"), "onStarted");
            race.onCheckpoint = Callback(configuration.Get("onCheckpoint"), "onCheckpoint");
            race.onFinished = Callback(configuration.Get("onFinished"), "onFinished");
        }
        else
        {
            if (configuration.Length < 2)
                throw new ArgumentException("Race requires start and finish positions.");
            for (int i = 1; i <= configuration.Length; ++i)
            {
                var value = configuration.Get(i);
                if (value.Type != DataType.Table)
                    throw new ArgumentException("Race gate " + i + " must be a table.");
                var size = value.Table.Get("size");
                float diameter = 1f;
                if (!size.IsNil())
                {
                    if (size.Type != DataType.Table) { throw new ArgumentException("Gate size must be a table."); }
                    diameter = Math.Max(Coordinate(size.Table, "x", 1, 1),
                        Math.Max(Coordinate(size.Table, "y", 2, 1), Coordinate(size.Table, "z", 3, 1)));
                }
                if (diameter <= 0) { throw new ArgumentException("Gate size must be positive."); }
                race.gates.Add(Gate(value.Table.Get("position"), "gate " + i, diameter * 0.5f,
                    Color(value.Table.Get("color"))));
            }
        }
        if (legacyFinished != null && !legacyFinished.IsNil())
        {
            if (!race.onFinished.IsNil())
                throw new ArgumentException("Use either onFinished or the second Race argument, not both.");
            race.onFinished = Callback(legacyFinished, "onFinished");
        }
        return race;
    }

    // Gate validates a position and copies the shared appearance into an independent gate definition.
    static ChallengeRaceGate Gate(DynValue value, string label, float radius, float[] color)
    {
        if (value.Type != DataType.Table) { throw new ArgumentException("Race " + label + " requires a position table."); }
        return new ChallengeRaceGate {
            position = new ChallengeRacePoint(Coordinate(value.Table, "x", 1),
                Coordinate(value.Table, "y", 2), Coordinate(value.Table, "z", 3)),
            radius = radius, color = color.ToArray()
        };
    }

    // Coordinate reads a named or indexed vector component and rejects missing required coordinates.
    static float Coordinate(Table table, string key, int index, float? fallback = null)
    {
        var value = table.Get(key);
        if (value.IsNil()) { value = table.Get(index); }
        return Number(value, key, fallback);
    }

    // Number reads a finite float or supplies the declared fallback for an omitted value.
    static float Number(DynValue value, string label, float? fallback = null)
    {
        if (value.IsNil() && fallback.HasValue) { return fallback.Value; }
        if (value.Type != DataType.Number || double.IsNaN(value.Number) ||
            double.IsInfinity(value.Number) || Math.Abs(value.Number) > float.MaxValue)
            throw new ArgumentException("Race " + label + " must be a finite number.");
        return (float)value.Number;
    }

    // Color reads shared RGBA components and supplies the standard green gate appearance.
    static float[] Color(DynValue value)
    {
        var defaults = new float[] { 0, 150, 0, 100 };
        if (value.IsNil()) { return defaults; }
        if (value.Type != DataType.Table) { throw new ArgumentException("Race color must be an RGBA table."); }
        string[] keys = { "r", "g", "b", "a" };
        return keys.Select((key, index) => Coordinate(value.Table, key, index + 1, defaults[index])).ToArray();
    }

    // Callback accepts an optional Lua or C# function and rejects other callback values.
    static DynValue Callback(DynValue value, string label)
    {
        if (!value.IsNil() && value.Type != DataType.Function && value.Type != DataType.ClrFunction)
            throw new ArgumentException("Race " + label + " must be a function.");
        return value;
    }
}

// ChallengeRaceProgress tracks gate order independently of gate visibility and Unity callback timing.
public class ChallengeRaceProgress
{
    public int NextGate { get; private set; }
    public bool Failed { get; private set; }
    public bool Finished => this.NextGate == this.gateCount;
    readonly int gateCount;

    // ChallengeRaceProgress initializes progress for a course containing at least start and finish gates.
    public ChallengeRaceProgress(int gateCount)
    {
        if (gateCount < 2) { throw new ArgumentException("A race requires at least two gates."); }
        this.gateCount = gateCount;
    }

    // Pass advances only the next required gate and ignores duplicates or invalid attempts.
    public bool Pass(int index)
    {
        if (this.Failed || this.Finished || index != this.NextGate) { return false; }
        this.NextGate++;
        return true;
    }

    // Fail prevents a stopped attempt from accepting further gate crossings.
    public void Fail() => this.Failed = true;
}

// ChallengeRaceStartTiming defines the one-second settling hold followed by a three-second countdown.
public static class ChallengeRaceStartTiming
{
    public const double HoldSeconds = 1;
    public const int CountdownSeconds = 3;

    // Ready reports when both the settling hold and countdown have elapsed.
    public static bool Ready(double elapsed) => elapsed >= HoldSeconds + CountdownSeconds;

    // CountdownAt returns the visible countdown number or zero during the hold and after GO.
    public static int CountdownAt(double elapsed) => elapsed < HoldSeconds || Ready(elapsed)
        ? 0 : CountdownSeconds - (int)Math.Floor(elapsed - HoldSeconds);
}
