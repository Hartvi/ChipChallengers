using System;
using System.Collections.Generic;
using MoonSharp.Interpreter;

// Lua itself resolves variable access, preserving strings, comments, tables, and lexical local scope.
public sealed class VehicleLuaEnvironment
{
    readonly Script script;
    public Table Globals { get; }

    public VehicleLuaEnvironment(Script script, IDictionary<string, VVar> variables)
    {
        this.script = script;
        this.Globals = new Table(script);
        var meta = new Table(script);
        meta.Set("__index", DynValue.NewCallback((context, args) => {
            if (args[1].Type == DataType.String && variables.TryGetValue(args[1].String, out var variable))
                return DynValue.NewNumber(variable.currentValue);
            return script.Globals.Get(args[1]);
        }));
        meta.Set("__newindex", DynValue.NewCallback((context, args) => {
            if (args[1].Type == DataType.String && variables.TryGetValue(args[1].String, out var variable))
            {
                variable.currentValue = ModelNumbers.Read(args[2]);
                variable.hasChanged = true;
            }
            else { this.Globals.Set(args[1], args[2]); }
            return DynValue.Nil;
        }));
        this.Globals.MetaTable = meta;
        this.Globals.Set("_G", DynValue.NewTable(this.Globals));
    }

    public DynValue Get(string name)
    {
        var value = this.Globals.Get(name);
        return value.IsNil() ? this.script.Globals.Get(name) : value;
    }
}
