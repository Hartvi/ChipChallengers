using System;
using MoonSharp.Interpreter;

// ScenarioLuaCallbacks registers Lua functions that delegate scenario configuration, race control and result recording to C#.
public static class ScenarioLuaCallbacks
{
    // Register exposes scenario configuration, attempt lifecycle, race and result callbacks to Lua.
    public static void Register(Script script, Action<Table> configure, Action begin,
        Action<double?> complete, Func<double?> getResult, Action<Table, DynValue> race, Action restartRace = null)
    {
        if (restartRace != null) { script.Globals["RestartRace"] = (Action)restartRace; }
        script.Globals["Scenario"] = (Action<Table>)configure;
        script.Globals["BeginScenarioAttempt"] = (Action)begin;
        script.Globals["CompleteScenario"] = DynValue.NewCallback((context, args) => {
            double? seconds = null;
            if (args.Count > 0 && !args[0].IsNil())
            {
                if (args[0].Type != DataType.Number)
                    throw new ArgumentException("CompleteScenario expects seconds or no argument.");
                seconds = args[0].Number;
            }
            complete(seconds);
            return DynValue.Nil;
        });
        script.Globals["GetRaceResult"] = DynValue.NewCallback((context, args) => {
            var seconds = getResult();
            return seconds.HasValue ? DynValue.NewNumber(seconds.Value) : DynValue.Nil;
        });
        script.Globals["Race"] = DynValue.NewCallback((context, args) => {
            if (args.Count == 0 || args[0].Type != DataType.Table)
                throw new ArgumentException("Race expects a configuration table.");
            var callback = args.Count > 1 ? args[1] : DynValue.Nil;
            if (!callback.IsNil() && callback.Type != DataType.Function && callback.Type != DataType.ClrFunction)
                throw new ArgumentException("Race finish callback must be a function.");
            race(args[0].Table, callback);
            return DynValue.Nil;
        });
    }
}
