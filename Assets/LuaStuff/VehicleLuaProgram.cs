using System;
using MoonSharp.Interpreter;

public sealed class VehicleLuaProgram
{
    readonly Script script;
    readonly VehicleLuaEnvironment environment;
    readonly string source;
    bool initialized;
    public string Error { get; private set; }

    public VehicleLuaProgram(Script script, VehicleLuaEnvironment environment, string source)
    {
        this.script = script;
        this.environment = environment;
        this.source = source ?? string.Empty;
    }

    public static void ValidateSyntax(string source) => LuaExecution.Compile(LuaExecution.CreateScript(), source ?? string.Empty, null, "vehicle");

    public void Fail(Exception error)
    {
        this.Error = error is InterpreterException interpreter ? interpreter.DecoratedMessage ?? error.Message : error.Message;
    }

    public bool Initialize()
    {
        if (this.initialized || this.Error != null) { return this.Error == null; }
        this.initialized = true;
        try { LuaExecution.Run(this.script, this.source, this.environment.Globals, "vehicle"); }
        catch (Exception error) { this.Fail(error); }
        return this.Error == null;
    }

    public bool CallLoop()
    {
        if (!this.Initialize()) { return false; }
        try
        {
            var loop = this.environment.Get("Loop");
            if (!loop.IsNil()) { LuaExecution.Call(this.script, loop); }
        }
        catch (Exception error) { this.Fail(error); }
        return this.Error == null;
    }
}
