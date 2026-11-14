using System;
using System.Linq;
using MoonSharp.Interpreter;

public static class LuaExecution
{
    public const int MaximumSourceLength = 65536;
    public const int InstructionLimit = 100000;

    public static Script CreateScript()
    {
        // Loading arbitrary code and creating unbudgeted nested coroutines would bypass the host's execution boundary.
        var script = new Script(CoreModules.Preset_HardSandbox | CoreModules.Metatables |
            CoreModules.ErrorHandling | CoreModules.OS_Time);
        script.Options.DebugPrint = _ => { };
        var strings = script.Globals.Get("string").Table;
        var repeat = strings.Get("rep");
        strings.Set("rep", DynValue.NewCallback((context, args) => {
            string value = args.AsType(0, "rep", DataType.String, false).String;
            double count = args.AsType(1, "rep", DataType.Number, false).Number;
            string separator = args.Count > 2 ? args.AsType(2, "rep", DataType.String, false).String : "";
            if (double.IsNaN(count) || double.IsInfinity(count) || count > MaximumSourceLength ||
                count * (value.Length + (double)separator.Length) > MaximumSourceLength)
                throw new ScriptRuntimeException("Repeated string exceeds the script resource limit.");
            return context.Call(repeat, args.GetArray());
        }));
        strings.Set("dump", DynValue.Nil);
        return script;
    }

    public static DynValue Compile(Script script, string source, Table environment = null, string name = "script")
    {
        if (source == null || source.Length > MaximumSourceLength)
            throw new ArgumentException("Script must contain at most " + MaximumSourceLength + " characters.");
        return script.LoadString(source, environment, name);
    }

    public static DynValue Run(Script script, string source, Table environment = null, string name = "script") =>
        Call(script, Compile(script, source, environment, name));

    public static DynValue Call(Script script, DynValue function, params object[] arguments)
    {
        if (function.Type == DataType.ClrFunction) { return script.Call(function, arguments); }
        if (function.Type != DataType.Function) { throw new ScriptRuntimeException("Expected a Lua function."); }
        var coroutine = script.CreateCoroutine(function).Coroutine;
        coroutine.AutoYieldCounter = InstructionLimit;
        var result = coroutine.Resume(arguments.Select(argument => DynValue.FromObject(script, argument)).ToArray());
        if (coroutine.State != CoroutineState.Dead)
            throw new ScriptRuntimeException("Script exceeded its instruction budget; fix the loop and try again.");
        return result;
    }
}
