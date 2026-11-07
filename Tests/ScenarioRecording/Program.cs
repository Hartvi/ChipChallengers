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
            physics = { fixedDeltaTime = 0.001f, solverIterations = 1, solverVelocityIterations = 3 }
        };
    }
    // Main exercises Lua callback contracts and persistent records using disposable test saves.
    static void Main()
    {
        string directory = Path.Combine(Path.GetTempPath(), "chipchallengers-record-test-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        try
        {
            byte[] luaBytes = System.Text.Encoding.UTF8.GetBytes("return 1");
            byte[] mapBytes = System.Text.Encoding.UTF8.GetBytes("v 0 0 0");
            string contentHash = ScenarioContentHash.Compute(luaBytes, mapBytes);
            Check(contentHash.Length == 64 && contentHash == ScenarioContentHash.Compute(luaBytes, mapBytes),
                "Scenario content hash was not a deterministic SHA-256 digest");
            Check(contentHash != ScenarioContentHash.Compute(System.Text.Encoding.UTF8.GetBytes("return 2"), mapBytes),
                "Lua changes did not affect the scenario hash");
            Check(contentHash != ScenarioContentHash.Compute(luaBytes, System.Text.Encoding.UTF8.GetBytes("v 1 0 0")),
                "Map changes did not affect the scenario hash");
            Check(ScenarioContentHash.Compute(new byte[] { 1 }, new byte[] { 2, 3 }) !=
                ScenarioContentHash.Compute(new byte[] { 1, 2 }, new byte[] { 3 }),
                "Lua/map boundaries were ambiguous in the scenario hash");
            Check(contentHash != ScenarioContentHash.Compute(mapBytes, luaBytes), "Lua/map order did not affect the hash");
            Reject(() => ScenarioContentHash.Compute(null, mapBytes), "Null Lua contents accepted");
            Reject(() => ScenarioContentHash.Compute(luaBytes, null), "Null map contents accepted");
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
            Check(progress.bestRun.rules.forbiddenChipTypes[0] == "Jet" && progress.bestRun.physics.fixedDeltaTime == 0.001f, "Rules/settings lost");
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
            Check(standard.rules.physics.fixedDeltaTime == 0.001f && standard.rules.physics.solverIterations == 1 &&
                standard.rules.physics.solverVelocityIterations == 3, "Missing physics rules did not use the shared preset");
            var custom = ScenarioLuaMetadata.Parse(lua.DoString("return {id='custom',rules={physics={fixedDeltaTime=0.01,solverIterations=12,solverVelocityIterations=4}}}").Table, "");
            Check(custom.rules.physics.fixedDeltaTime == 0.01f && custom.rules.physics.solverIterations == 12 &&
                custom.rules.physics.solverVelocityIterations == 4, "Explicit physics rules were not parsed");
            var partial = ScenarioLuaMetadata.Parse(lua.DoString("return {id='partial',rules={physics={solverIterations=8}}}").Table, "");
            Check(partial.rules.physics.fixedDeltaTime == 0.001f && partial.rules.physics.solverIterations == 8,
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
            int restarts = 0;
            ScenarioLuaCallbacks.Register(lua, _ => {}, () => {}, _ => {}, () => null, (_,__) => {}, () => restarts++);
            lua.DoString("RestartRace()");
            Check(restarts == 1, "Lua retry callback not forwarded");
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
            int scenarioRetries = 0, modelResets = 0;
            string retriedScenario = null;
            playContext.Retry(name => { scenarioRetries++; retriedScenario = name; }, () => modelResets++);
            Check(modelResets == 1 && scenarioRetries == 0, "Regular retry did not reset only the model");
            playContext.SelectScenario("circuit1_race");
            int retryRevision = playContext.Revision;
            playContext.Retry(name => { scenarioRetries++; retriedScenario = name; }, () => modelResets++);
            Check(scenarioRetries == 1 && retriedScenario == "circuit1_race" && modelResets == 1,
                "Scenario retry fell back to a model-only reset");
            playContext.Retry(name => { scenarioRetries++; retriedScenario = name; }, () => modelResets++);
            Check(scenarioRetries == 2 && playContext.MatchesScenario("circuit1_race", retryRevision),
                "Repeated retries lost scenario selection");
            playContext.SelectScenario("custom_survival");
            playContext.Retry(name => retriedScenario = name, () => modelResets++);
            Check(retriedScenario == "custom_survival" && modelResets == 1,
                "Non-race scenario retry depended on an active race");
            var currentPhysics = ScenarioPhysicsSettings.CreateStandard();
            var notifications = new System.Collections.Generic.List<string>();
            var physicsEvents = new PhysicsSettingsDispatcher();
            object bodyTarget = new object();
            int writes = 0;
            Action<PhysicsSettingsChange> beforeChange = change => {
                Check(currentPhysics.fixedDeltaTime == 0.001f && change.Previous.fixedDeltaTime == 0.001f &&
                    change.Next.fixedDeltaTime == 0.01f, "Before callback saw already changed physics");
                Check(ReferenceEquals(change.Target, bodyTarget) && change.Reason == "test", "Change lost target or reason");
                change.Next.fixedDeltaTime = 99;
                notifications.Add("before");
            };
            Action<PhysicsSettingsChange> afterChange = change => {
                Check(currentPhysics.fixedDeltaTime == 0.01f && change.Next.fixedDeltaTime == 0.01f,
                    "After callback did not see applied settings or listener changed the snapshot");
                notifications.Add("after");
            };
            physicsEvents.Changing += beforeChange;
            physicsEvents.Changed += afterChange;
            var requestedPhysics = currentPhysics.Copy();
            requestedPhysics.fixedDeltaTime = 0.01f;
            Check(physicsEvents.Apply(() => currentPhysics, next => {
                notifications.Add("write"); currentPhysics = next; writes++;
            }, requestedPhysics, "test", bodyTarget), "Changed settings were ignored");
            Check(string.Join(",", notifications) == "before,write,after" && writes == 1,
                "Settings notification order or write count was incorrect");
            Check(!physicsEvents.Apply(() => currentPhysics, _ => writes++, requestedPhysics, "unchanged"),
                "Identical settings fired callbacks");
            Check(writes == 1 && notifications.Count == 3, "Identical settings caused extra notifications or writes");
            physicsEvents.Changing -= beforeChange;
            physicsEvents.Changed -= afterChange;
            var originalPhysics = ScenarioPhysicsSettings.CreateStandard();
            physicsEvents.Apply(() => currentPhysics, next => currentPhysics = next, originalPhysics, "restore");
            Check(currentPhysics.Matches(originalPhysics) && notifications.Count == 3,
                "Restoration failed or unsubscribed listeners were retained");
            var invalidPhysics = originalPhysics.Copy(); invalidPhysics.fixedDeltaTime = float.NaN;
            Reject(() => physicsEvents.Apply(() => currentPhysics, _ => writes++, invalidPhysics, "invalid"),
                "Invalid physics configuration reached the writer");
            Check(writes == 1, "Validation failure wrote physics settings");
            int reportedErrors = 0, survivingListeners = 0;
            var robustEvents = new PhysicsSettingsDispatcher(_ => reportedErrors++);
            robustEvents.Changing += _ => throw new InvalidOperationException("bad listener");
            robustEvents.Changing += _ => survivingListeners++;
            robustEvents.Changed += _ => survivingListeners++;
            robustEvents.Apply(() => currentPhysics, next => currentPhysics = next, requestedPhysics, "listener test");
            Check(reportedErrors == 1 && survivingListeners == 2 && currentPhysics.Matches(requestedPhysics),
                "One failing listener prevented other callbacks or the settings change");
            var guardedEvents = new PhysicsSettingsDispatcher(_ => reportedErrors++);
            guardedEvents.Changing += _ => guardedEvents.Apply(() => currentPhysics, __ => writes++, originalPhysics, "nested");
            guardedEvents.Apply(() => currentPhysics, next => currentPhysics = next, originalPhysics, "outer");
            Check(reportedErrors == 2 && currentPhysics.Matches(originalPhysics) && writes == 1,
                "Reentrant listener changed physics during an in-progress update");
            bool subscribedAttempt = true;
            var invalidationEvents = new PhysicsSettingsDispatcher();
            Action<PhysicsSettingsChange> invalidate = _ => subscribedAttempt = false;
            invalidationEvents.Changing += invalidate;
            invalidationEvents.Apply(() => currentPhysics, next => {
                Check(!subscribedAttempt, "Attempt invalidation happened after the settings write");
                currentPhysics = next;
            }, requestedPhysics, "attempt invalidation");
            invalidationEvents.Changing -= invalidate;
            Check(!ScenarioRaceStartTiming.Ready(0) && ScenarioRaceStartTiming.CountdownAt(0) == 0,
                "Race started immediately at gate entry");
            Check(!ScenarioRaceStartTiming.Ready(0.999) && ScenarioRaceStartTiming.CountdownAt(0.999) == 0,
                "Countdown started before the one-second settling hold");
            Check(ScenarioRaceStartTiming.CountdownAt(1) == 3 && !ScenarioRaceStartTiming.Ready(1),
                "Settling hold did not transition to countdown 3");
            Check(ScenarioRaceStartTiming.CountdownAt(2) == 2 && !ScenarioRaceStartTiming.Ready(2),
                "Countdown 2 timing incorrect");
            Check(ScenarioRaceStartTiming.CountdownAt(3) == 1 && !ScenarioRaceStartTiming.Ready(3),
                "Countdown 1 timing incorrect");
            Check(ScenarioRaceStartTiming.CountdownAt(3.999) == 1 && !ScenarioRaceStartTiming.Ready(3.999),
                "Race released before countdown completion");
            Check(ScenarioRaceStartTiming.Ready(4) && ScenarioRaceStartTiming.CountdownAt(4) == 0,
                "Race did not release at GO");
            Check(ScenarioRaceStartTiming.CountdownAt(5) == 0 && ScenarioRaceStartTiming.Ready(5),
                "Completed countdown produced an invalid negative number");
            Console.WriteLine("PASS: " + checks + " scenario metadata and persistent-record checks.");
        }
        finally { Directory.Delete(directory, true); }
    }
}
