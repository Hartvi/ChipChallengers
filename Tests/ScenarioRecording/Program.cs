using System;
using System.IO;
using System.Linq;
using MoonSharp.Interpreter;

class Program
{
    static int checks;
    // Check asserts a test condition and counts the successful check.
    static void Check(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
        checks++;
    }
    // Reject verifies that an invalid operation throws an argument exception.
    static void Reject(Action action, string message)
    {
        try { action(); }
        catch (ArgumentException) { checks++; return; }
        throw new Exception(message);
    }
    // Run creates a sample run containing vehicle, rule, timing and physics snapshots.
    static ScenarioRunRecord Run(string id, double time, string model, int version = 1)
    {
        return new ScenarioRunRecord {
            runId = id, hasTime = true, timeSeconds = time, modelName = model,
            playerName = "Driver", finishedAtUtc = "2026-11-04T15:32:18Z",
            gameVersion = "test", scenarioLogicVersion = version,
            modelSnapshot = "{script=[[line one\nline two]]}",
            chipCounts = { new ScenarioChipCount { chipType = "Wheel", count = 4 } },
            rules = { maxChips = 30, forbiddenChipTypes = { "Jet" } },
            physics = { fixedDeltaTime = 0.002f, solverIterations = 6, solverVelocityIterations = 1 }
        };
    }
    // Main exercises Lua callback contracts and persistent records using disposable test saves.
    static void Main()
    {
        string directory = Path.Combine(Path.GetTempPath(), "chipchallengers-record-test-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        try
        {
            string path = Path.Combine(directory, "progress.json");
            var store = new ScenarioProgressStore(path);
            var metadata = new ScenarioMetadata { scenarioId = "permanent-id" };
            Check(store.Load().scenarios.Count == 0, "First launch should have no records");
            Check(store.Record(metadata, Run("a", 42.731234, "First car")), "First finish missing");
            Check(!store.Record(metadata, Run("a", 42.731234, "First car")), "Duplicate finish counted");
            store.Record(metadata, Run("b", 48, "Slower car"));
            var progress = store.Load().scenarios.Single();
            Check(progress.completionCount == 2 && progress.completed, "Completion count incorrect");
            Check(progress.bestRun.modelName == "First car" && progress.lastRun.modelName == "Slower car", "Slower finish overwrote best vehicle");
            Check(progress.bestRun.timeSeconds == 42.731234, "Timing precision lost");
            Check(progress.bestRun.modelSnapshot.Contains("\n") && progress.bestRun.chipCounts[0].count == 4, "Vehicle snapshot lost");
            Check(progress.bestRun.rules.forbiddenChipTypes[0] == "Jet" && progress.bestRun.physics.fixedDeltaTime == 0.002f, "Rules/settings lost");
            Check(progress.bestRun.playerName == "Driver" && progress.bestRun.finishedAtUtc.EndsWith("Z"), "Player/date lost");
            Check(File.Exists(path + ".bak") && !File.Exists(path + ".tmp"), "Atomic save/backup failed");
            store.Record(metadata, Run("c", 40, "Faster car"));
            Check(store.Load().scenarios.Single().bestRun.runId == "c", "New personal best missing");
            store.Record(metadata, Run("d", 50, "New processor", 2));
            Check(store.Load().scenarios.Count == 2, "Processor versions mixed");
            metadata.recordVersion++;
            store.Record(metadata, Run("e", 55, "New course"));
            Check(store.Load().scenarios.Count == 3, "Course versions mixed");
            store.Record(metadata, new ScenarioRunRecord { runId = "untimed", scenarioLogicVersion = 1, completionType = "survival", physics = ScenarioPhysicsSettings.CreateStandard() });
            progress = store.Load().scenarios.Last();
            Check(!progress.lastRun.hasTime && progress.bestRun.runId == "e", "Untimed finish became a zero-time best");
            string before = File.ReadAllText(path);
            Reject(() => store.Record(metadata, Run("bad", double.NaN, "Invalid")), "NaN accepted");
            Reject(() => store.Record(metadata, Run("bad", -1, "Invalid")), "Negative time accepted");
            Check(File.ReadAllText(path) == before, "Invalid result changed the save");
            File.WriteAllText(path, "{not valid JSON");
            bool failed = false;
            try { store.Record(metadata, Run("f", 20, "Car")); } catch (Newtonsoft.Json.JsonException) { failed = true; }
            Check(failed && File.ReadAllText(path) == "{not valid JSON", "Corrupt save overwritten");

            var lua = new Script();
            var table = lua.DoString("return {id='community-level', title='Hill', recordVersion=3, scenarioLogicVersion=1, rules={maxChips=30, forbiddenChipTypes={'Jet'}}}").Table;
            var parsed = ScenarioLuaMetadata.Parse(table, "fallback");
            Check(parsed.scenarioId == "community-level" && parsed.recordVersion == 3 && parsed.rules.maxChips == 30, "Lua metadata not parsed");
            Check(parsed.rules.forbiddenChipTypes.Single() == "Jet", "Lua restrictions lost");
            Check(ScenarioLuaMetadata.Parse(lua.DoString("return {}").Table, "legacy:race").scenarioId == "legacy:race", "Legacy fallback lost");
            Reject(() => ScenarioLuaMetadata.Parse(lua.DoString("return {id='x',scenarioLogicVersion=99}").Table, ""), "Unsupported processor accepted");
            Reject(() => ScenarioLuaMetadata.Parse(lua.DoString("return {id='x',recordVersion=1.5}").Table, ""), "Fractional version accepted");
            Reject(() => ScenarioLuaMetadata.Parse(lua.DoString("return {id='x',rules={maxChips=-2}}").Table, ""), "Invalid restriction accepted");
            Reject(() => ScenarioLuaMetadata.Parse(lua.DoString("return {}").Table, ""), "Missing identity accepted");
            var standard = ScenarioLuaMetadata.Parse(lua.DoString("return {id='standard'}").Table, "");
            Check(standard.rules.physics.fixedDeltaTime == 0.002f && standard.rules.physics.solverIterations == 6 &&
                standard.rules.physics.solverVelocityIterations == 1, "Missing physics rules did not use the shared preset");
            var custom = ScenarioLuaMetadata.Parse(lua.DoString("return {id='custom',rules={physics={fixedDeltaTime=0.01,solverIterations=12,solverVelocityIterations=4}}}").Table, "");
            Check(custom.rules.physics.fixedDeltaTime == 0.01f && custom.rules.physics.solverIterations == 12 &&
                custom.rules.physics.solverVelocityIterations == 4, "Explicit physics rules were not parsed");
            var partial = ScenarioLuaMetadata.Parse(lua.DoString("return {id='partial',rules={physics={solverIterations=8}}}").Table, "");
            Check(partial.rules.physics.fixedDeltaTime == 0.002f && partial.rules.physics.solverIterations == 8,
                "Partial physics rules inherited user settings instead of the standard preset");
            Reject(() => ScenarioLuaMetadata.Parse(lua.DoString("return {id='x',rules={physics='invalid'}}").Table, ""), "Non-table physics accepted");
            Reject(() => ScenarioLuaMetadata.Parse(lua.DoString("return {id='x',rules={physics={fixedDeltaTime=0}}}").Table, ""), "Zero timestep accepted");
            Reject(() => ScenarioLuaMetadata.Parse(lua.DoString("return {id='x',rules={physics={fixedDeltaTime=1/0}}}").Table, ""), "Infinite timestep accepted");
            Reject(() => ScenarioLuaMetadata.Parse(lua.DoString("return {id='x',rules={physics={fixedDeltaTime=0/0}}}").Table, ""), "NaN timestep accepted");
            Reject(() => ScenarioLuaMetadata.Parse(lua.DoString("return {id='x',rules={physics={fixedDeltaTime=0.0001}}}").Table, ""), "Excessive physics frequency accepted");
            Reject(() => ScenarioLuaMetadata.Parse(lua.DoString("return {id='x',rules={physics={solverIterations=0}}}").Table, ""), "Zero solver iterations accepted");
            Reject(() => ScenarioLuaMetadata.Parse(lua.DoString("return {id='x',rules={physics={solverIterations=256}}}").Table, ""), "Excessive solver iterations accepted");
            Reject(() => ScenarioLuaMetadata.Parse(lua.DoString("return {id='x',rules={physics={solverVelocityIterations=1.5}}}").Table, ""), "Fractional solver iterations accepted");
            var copied = custom.rules.physics.Copy();
            copied.solverIterations = 20;
            Check(custom.rules.physics.solverIterations == 12 && !copied.Matches(custom.rules.physics), "Physics snapshots share mutable state");
            var physicsStore = new ScenarioProgressStore(Path.Combine(directory, "physics.json"));
            var standardRun = Run("standard-run", 42, "Car");
            physicsStore.Record(standard, standardRun);
            var mismatched = Run("mismatch", 10, "Car");
            mismatched.physics.fixedDeltaTime = 0.01f;
            Reject(() => physicsStore.Record(standard, mismatched), "Result violating required physics was saved");
            standard.rules.physics.fixedDeltaTime = 0.01f;
            var alternate = Run("alternate", 10, "Car");
            alternate.physics.fixedDeltaTime = 0.01f;
            alternate.rules.physics = standard.rules.physics.Copy();
            physicsStore.Record(standard, alternate);
            Check(physicsStore.Load().scenarios.Count == 2, "Results using different required physics were mixed");
            var savedPhysics = physicsStore.Load().scenarios.Last().lastRun;
            Check(savedPhysics.physics.Matches(savedPhysics.rules.physics), "Required/actual physics did not survive serialization");
            int beginCalls = 0;
            double? finishedTime = null;
            Table receivedGates = null;
            DynValue finishCallback = DynValue.Nil;
            ScenarioMetadata declared = null;
            ScenarioLuaCallbacks.Register(lua,
                value => declared = ScenarioLuaMetadata.Parse(value, "legacy:test"),
                () => beginCalls++, value => finishedTime = value,
                () => finishedTime, (gates, callback) => { receivedGates = gates; finishCallback = callback; });
            Check(lua.DoString("return GetRaceResult()").IsNil(), "Unfinished Lua race returned a result");
            lua.DoString("Scenario({id='callback-level'}); BeginScenarioAttempt(); Race({{position={1,2,3}},{position={4,5,6}}}, function(seconds) callbackTime=seconds end)");
            Check(declared.scenarioId == "callback-level" && beginCalls == 1, "Lua configure/begin callbacks not forwarded");
            Check(receivedGates.Length == 2 && !finishCallback.IsNil(), "Lua race gates or callback not forwarded");
            lua.Call(finishCallback, 42.125);
            Check(lua.Globals.Get("callbackTime").Number == 42.125, "Finish callback did not receive exact time");
            lua.DoString("CompleteScenario(42.125)");
            Check(finishedTime == 42.125 && lua.DoString("return GetRaceResult()").Number == 42.125, "Completion/getter boundary lost time");
            lua.DoString("CompleteScenario()");
            Check(finishedTime == null, "Untimed Lua completion did not forward null");
            lua.DoString("Race({{},{}})");
            Check(finishCallback.IsNil(), "Legacy one-argument Race no longer supported");
            var race = ScenarioRaceDefinition.Parse(lua.DoString("return {start={0,0,0}, checkpoints={{10,0,0},{20,0,0}}, finish={30,0,0}, radius=4, color={255,0,0,120}, onStarted=function() started=true end, onCheckpoint=function(index,seconds) passed=index; split=seconds end, onFinished=function(seconds) result=seconds end}").Table, DynValue.Nil);
            Check(race.gates.Count == 4 && race.gates[0].position.x == 0 && race.gates[3].position.x == 30, "Named gate order incorrect");
            Check(race.gates.All(g => g.radius == 4 && g.color[0] == 255 && g.color[3] == 120), "Shared gate appearance lost");
            race.gates[0].color[0] = 0;
            Check(race.gates[1].color[0] == 255, "Gate colors unexpectedly share mutable arrays");
            lua.Call(race.onStarted);
            lua.Call(race.onCheckpoint, 2, 12.5);
            lua.Call(race.onFinished, 25.75);
            Check(lua.Globals.Get("started").Boolean && lua.Globals.Get("passed").Number == 2 && lua.Globals.Get("split").Number == 12.5 && lua.Globals.Get("result").Number == 25.75, "Named lifecycle callbacks lost arguments");
            var direct = ScenarioRaceDefinition.Parse(lua.DoString("return {start={x=0,y=1,z=2},finish={3,4,5}}").Table, DynValue.Nil);
            Check(direct.gates.Count == 2 && direct.gates[0].radius == 5 && direct.gates[0].position.y == 1, "Minimal race defaults invalid");
            var oldRace = ScenarioRaceDefinition.Parse(lua.DoString("return {{position={0,0,0},size={5}},{position={10,0,0},size={8}}}").Table, DynValue.Nil);
            Check(oldRace.gates[0].radius == 2.5f && oldRace.gates[1].radius == 4, "Original gate list changed its sizes");
            Reject(() => ScenarioRaceDefinition.Parse(lua.DoString("return {start={0,0,0}}").Table, DynValue.Nil), "Missing finish accepted");
            Reject(() => ScenarioRaceDefinition.Parse(lua.DoString("return {start={0,0},finish={1,2,3}}").Table, DynValue.Nil), "Incomplete position accepted");
            Reject(() => ScenarioRaceDefinition.Parse(lua.DoString("return {start={0,0,0},finish={1,2,3},radius=0}").Table, DynValue.Nil), "Zero radius accepted");
            Reject(() => ScenarioRaceDefinition.Parse(lua.DoString("return {start={0,0,0},finish={1,2,3},onFinished=42}").Table, DynValue.Nil), "Invalid named callback accepted");
            Reject(() => ScenarioRaceDefinition.Parse(lua.DoString("return {start={0,0,0},finish={1,2,3},checkpoints='bad'}").Table, DynValue.Nil), "Non-list checkpoints accepted");
            Reject(() => ScenarioRaceDefinition.Parse(lua.DoString("return {start={0,0,0},finish={1/0,2,3}}").Table, DynValue.Nil), "Infinite coordinate accepted");
            var progressState = new ScenarioRaceProgress(4);
            Check(!progressState.Pass(2) && progressState.NextGate == 0, "Out-of-order gate accepted");
            Check(progressState.Pass(0) && !progressState.Pass(0) && progressState.NextGate == 1, "Duplicate start accepted");
            progressState.Pass(1); progressState.Pass(2); progressState.Pass(3);
            Check(progressState.Finished && !progressState.Pass(3), "Finish was not one-shot");
            var retry = new ScenarioRaceProgress(4);
            Check(retry.NextGate == 0 && !retry.Finished, "Retry kept old progress");
            retry.Fail();
            Check(!retry.Pass(0), "Invalidated attempt accepted a gate");
            var center = new ScenarioRacePoint(0,0,0);
            Check(ScenarioRaceProgress.CrossesGate(new ScenarioRacePoint(-10,0,0),new ScenarioRacePoint(10,0,0),center,1), "Fast movement skipped the gate");
            Check(!ScenarioRaceProgress.CrossesGate(center,new ScenarioRacePoint(0.5f,0,0),center,1), "Spawning inside started the timer");
            Check(!ScenarioRaceProgress.CrossesGate(new ScenarioRacePoint(-10,2,0),new ScenarioRacePoint(10,2,0),center,1), "Near miss triggered a gate");
            Check(!ScenarioRaceProgress.CrossesGate(new ScenarioRacePoint(10,0,0),new ScenarioRacePoint(20,0,0),center,1), "Moving away triggered a gate");
            Check(Math.Abs(ScenarioRaceProgress.CrossingFraction(new ScenarioRacePoint(-10,0,0),new ScenarioRacePoint(10,0,0),center,1).Value - 0.45) < 0.000001, "Gate entry time interpolation incorrect");
            int restarts = 0;
            ScenarioLuaCallbacks.Register(lua, _ => {}, () => {}, _ => {}, () => null, (_,__) => {}, () => restarts++);
            lua.DoString("RestartRace()");
            Check(restarts == 1, "Lua retry callback not forwarded");
            var fastEnd = new ScenarioRacePoint(30,0,0);
            var fastFrom = new ScenarioRacePoint(-10,0,0);
            double firstEntry = ScenarioRaceProgress.CrossingFraction(fastFrom, fastEnd, center, 1).Value;
            var firstCrossing = new ScenarioRacePoint((float)(fastFrom.x + (fastEnd.x-fastFrom.x)*firstEntry), 0, 0);
            Check(ScenarioRaceProgress.CrossingFraction(firstCrossing, fastEnd, new ScenarioRacePoint(20,0,0), 1).HasValue,
                "A second forward gate could not be processed in the same movement segment");
            Check(!ScenarioRaceProgress.CrossingFraction(firstCrossing, fastEnd, new ScenarioRacePoint(-5,0,0), 1).HasValue,
                "A gate behind an already passed gate was accepted out of physical order");
            string scenarioDirectory = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../Assets/StreamingAssets/Scenarios"));
            foreach (string file in new[] { "circuit1_race.lua", "load_race.lua" })
            {
                var example = new Script();
                ScenarioRaceDefinition exampleRace = null;
                ScenarioLuaCallbacks.Register(example, value => ScenarioLuaMetadata.Parse(value, ""),
                    () => {}, _ => {}, () => null,
                    (configuration, callback) => exampleRace = ScenarioRaceDefinition.Parse(configuration, callback));
                example.Globals["LoadMap"] = (Action<string>)(_ => {});
                example.Globals["SetPosition"] = (Action<string,Table>)((_,__) => {});
                example.Globals["Spawn"] = (Func<string,Table,string>)((_,__) => "test-object");
                example.DoString(File.ReadAllText(Path.Combine(scenarioDirectory, file)));
                Check(exampleRace != null && exampleRace.gates.Count >= 2, "Bundled example failed to declare a race: " + file);
                Check(exampleRace.gates.All(g => g.radius == 2.5f), "Example gate size changed during migration: " + file);
            }
            var playContext = new GameplayContext();
            Check(!playContext.IsScenario && playContext.ScenarioName == null, "New gameplay context was not regular singleplayer");
            playContext.SelectScenario("circuit1_race");
            int firstSelection = playContext.Revision;
            Check(playContext.IsScenario && playContext.MatchesScenario("circuit1_race", firstSelection), "Scenario selection was not retained");
            Check(playContext.ScenarioName == "circuit1_race" && playContext.MatchesScenario("circuit1_race", firstSelection),
                "Reading the context consumed the scenario before a later editor return");
            Check(!playContext.MatchesScenario("load_race", firstSelection), "Load accepted the wrong scenario");
            playContext.SelectScenario("load_race");
            Check(!playContext.MatchesScenario("circuit1_race", firstSelection) && playContext.ScenarioName == "load_race",
                "New scenario selection kept an old queued load valid");
            int oldSelection = playContext.Revision;
            playContext.SelectScenario("load_race");
            Check(!playContext.MatchesScenario("load_race", oldSelection), "Reselecting a scenario did not invalidate earlier queued loads");
            playContext.SelectSingleplayer();
            Check(!playContext.IsScenario && playContext.ScenarioName == null && !playContext.MatchesScenario("load_race", oldSelection),
                "Explicit singleplayer selection kept scenario context");
            Reject(() => playContext.SelectScenario(" "), "Empty scenario selection accepted");
            Check(!playContext.IsScenario, "Invalid selection changed regular singleplayer context");
            Console.WriteLine("PASS: " + checks + " scenario metadata and persistent-record checks.");
        }
        finally { Directory.Delete(directory, true); }
    }
}
