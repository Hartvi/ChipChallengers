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
    static ChallengeRunRecord Run(string id, double time, string model, int version = 1)
    {
        return new ChallengeRunRecord {
            challengeHash = ChallengeContentHash.Compute(System.Text.Encoding.UTF8.GetBytes("test challenge"), Array.Empty<byte>()),
            challengeFileName = "test_challenge.lua",
            runId = id, hasTime = true, timeSeconds = time, modelName = model,
            playerName = "Driver", finishedAtUtc = "2026-11-04T15:32:18Z",
            gameVersion = "test", challengeLogicVersion = version,
            modelSnapshot = "{script=[[line one\nline two]]}",
            chipCounts = { new ChallengeChipCount { chipType = "Wheel", count = 4 } },
            rules = { maxChips = 30, forbiddenChipTypes = { "Jet" } },
            physics = { fixedDeltaTime = 0.001f, solverIterations = 6, solverVelocityIterations = 1 }
        };
    }
    // SimulateWheelSpin integrates one second of driven wheel motion under constant tire load across two physics rates.
    static float SimulateWheelSpin(int firstRate, int secondRate, float inverseInertia)
    {
        float omega = 20f;
        foreach (int rate in new[] { firstRate, secondRate })
        {
            float seconds = 1f / rate;
            for (int tick = 0; tick < rate / 2; ++tick)
            {
                omega += WheelPhysics.ContactSpinDelta(10f, 0.5f, inverseInertia, seconds);
                omega = WheelPhysics.IntegrateSpin(omega, 40f, 2f, inverseInertia, seconds);
            }
        }
        return omega;
    }

    // CheckWheelSupport checks first contact, oblique short rays, rebound and force limits at both physics rates.
    static void CheckWheelSupport()
    {
        const float maxDepenetrationVelocity = 2f;
        foreach (float length in new[] { 0.125f, 0.25f, 0.3125f, 0.5f, 0.5625f, 0.75f })
        {
            foreach (float normalAlignment in new[] { 1f, 0.4472136f, 0.1f })
            {
                Check(WheelPhysics.NormalCompression(length, length, -normalAlignment) == 0,
                    "Wheel produced compression at first contact");
                float depth = 0.005f;
                float hitDistance = length - depth / normalAlignment;
                float compression = WheelPhysics.NormalCompression(length, hitDistance, -normalAlignment);
                Check(Math.Abs(compression - depth) < 1e-6f,
                    "Short or oblique wheel ray exaggerated normal penetration");
                float support = WheelPhysics.LimitSupportForce(1000f * 4f * 20f * compression,
                    4f, 0f, maxDepenetrationVelocity, 0.001f);
                Check(Math.Abs(support - 400f) < 0.1f,
                    "Equal normal penetration produced different wheel support forces");
            }
            Check(WheelPhysics.NormalCompression(length, length + 0.01f, -1f) == 0,
                "A surface beyond the ray produced support compression");
            Check(WheelPhysics.NormalCompression(length, length * 0.5f, 0f) == 0,
                "A grazing ray produced normal compression");
        }

        foreach (float seconds in new[] { 0.001f, 0.002f })
        {
            foreach (float mass in new[] { 2f, 3f, 4f })
            {
                foreach (float normalVelocity in new[] { -5f, 0f, 1.9f, 2f, 4f })
                {
                    float force = WheelPhysics.LimitSupportForce(1e9f, mass, normalVelocity, maxDepenetrationVelocity, seconds);
                    Check(force >= 0f, "Wheel support pulled into the ground");
                    if (normalVelocity < maxDepenetrationVelocity)
                    {
                        Check(Math.Abs(normalVelocity + force / mass * seconds - maxDepenetrationVelocity) < 1e-4f,
                            "Wheel support exceeded its depenetration speed limit");
                    }
                    else
                    {
                        Check(force == 0f, "Wheel accelerated outward beyond its depenetration limit");
                    }
                }
                Check(WheelPhysics.LimitSupportForce(-100f, mass, 1f, maxDepenetrationVelocity, seconds) == 0f,
                    "Rebound damping pulled the wheel into the ground");
                Check(WheelPhysics.LimitSupportForce(40f, mass, 0f, maxDepenetrationVelocity, seconds) == 40f,
                    "Force limiting changed normal low-load wheel support");
            }
        }
    }

    // CheckBundledChallenges executes bundled Lua declarations against the real metadata and race parsers.
    static void CheckBundledChallenges()
    {
        string challengeDirectory = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../Assets/StreamingAssets/Challenges"));
        foreach (string file in new[] { "dev_short_race.lua", "01_industrial_test_track_race.lua", "03_karst_flight_race.lua", "02_quarry_climb_race.lua" })
        {
            var example = new Script();
            ChallengeRaceDefinition exampleRace = null;
            ChallengeLuaCallbacks.Register(example, value => ChallengeLuaMetadata.Parse(value, ""),
                () => {}, _ => {}, () => null,
                (configuration, callback) => exampleRace = ChallengeRaceDefinition.Parse(configuration, callback));
            example.Globals["LoadMap"] = (Action<string>)(_ => {});
            example.Globals["SetPosition"] = (Action<string,Table>)((_,__) => {});
            example.Globals["SetRotation"] = (Action<string,Table>)((_,__) => {});
            example.Globals["Spawn"] = (Func<string,Table,string>)((_,__) => "test-object");
            example.DoString(File.ReadAllText(Path.Combine(challengeDirectory, file)));
            Check(exampleRace != null && exampleRace.gates.Count >= 2, "Bundled example failed to declare a race: " + file);
            float expectedRadius = file == "03_karst_flight_race.lua" ? 27f :
                file == "01_industrial_test_track_race.lua" || file == "02_quarry_climb_race.lua" ? 8f : 2.5f;
            Check(exampleRace.gates.All(g => g.radius == expectedRadius), "Bundled challenge gate size incorrect: " + file);
            if (file == "01_industrial_test_track_race.lua")
            {
                Check(exampleRace.gates.Count == 15, "Industrial circuit checkpoint count incorrect");
                var start = exampleRace.gates.First().position;
                var finish = exampleRace.gates.Last().position;
                Check(start.x == finish.x && start.y == finish.y && start.z == finish.z,
                    "Industrial lap did not finish at its start line");
            }
            if (file == "03_karst_flight_race.lua")
            {
                Check(exampleRace.gates.Count == 15, "Flight course did not declare 13 checkpoints plus start and finish");
                Check(exampleRace.gates.All(gate => gate.color[3] == 180f), "Flight gate opacity was not increased");
                Check(exampleRace.gates.Skip(1).All(gate => gate.position.y - gate.radius > 0),
                    "An airborne flight gate intersects sea level");
            }
            if (file == "02_quarry_climb_race.lua")
            {
                Check(exampleRace.gates.Count == 4, "Quarry course should have two route-merge checkpoints");
                Check(exampleRace.gates.Last().position.y > exampleRace.gates.First().position.y + 30,
                    "Quarry finish was not at the summit");
            }
        }
    }

    // Main exercises wheel timing, Lua callback contracts and persistent records using disposable test saves.
    static void Main(string[] args)
    {
        if (args.Contains("--challenges-only"))
        {
            CheckBundledChallenges();
            Console.WriteLine("PASS: " + checks + " bundled challenge checks.");
            return;
        }
        string directory = Path.Combine(Path.GetTempPath(), "chipchallengers-record-test-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        try
        {
            CheckWheelSupport();
            float baseWheelInertia = WheelPhysics.DiskInverseInertia(2f, 0.25f);
            float doubleRadiusInertia = WheelPhysics.DiskInverseInertia(2f, 0.5f);
            float doubleMassInertia = WheelPhysics.DiskInverseInertia(4f, 0.25f);
            float baseDrivenSpin = WheelPhysics.IntegrateSpin(0, 40, 0, baseWheelInertia, 0.001f);
            Check(Math.Abs(1f / baseWheelInertia - 0.0625f) < 1e-6f, "Default wheel did not use solid-disk inertia");
            Check(Math.Abs(WheelPhysics.IntegrateSpin(0, 40, 0, doubleRadiusInertia, 0.001f) * 4 - baseDrivenSpin) < 1e-6f,
                "Doubling wheel radius did not quadruple inertia under drive torque");
            Check(Math.Abs(WheelPhysics.IntegrateSpin(0, 40, 0, doubleMassInertia, 0.001f) * 2 - baseDrivenSpin) < 1e-6f,
                "Doubling wheel mass did not double inertia under drive torque");
            Check(Math.Abs(WheelPhysics.ContactSpinDelta(10, 1f, doubleRadiusInertia, 0.001f) * 2 -
                WheelPhysics.ContactSpinDelta(10, 0.5f, baseWheelInertia, 0.001f)) < 1e-6f,
                "Larger wheel contact response did not account for both lever arm and inertia");
            foreach (float inverseInertia in new[] { baseWheelInertia, doubleRadiusInertia, doubleMassInertia })
            {
                float at500 = SimulateWheelSpin(500, 500, inverseInertia);
                float at1000 = SimulateWheelSpin(1000, 1000, inverseInertia);
                float switched = SimulateWheelSpin(500, 1000, inverseInertia);
                float tolerance = 0.001f * Math.Max(1f, Math.Abs(at1000));
                Check(Math.Abs(at500 - at1000) < tolerance, "Wheel spin differed by more than 0.1 percent between 500 and 1000 Hz");
                Check(Math.Abs(switched - at1000) < tolerance, "Runtime physics-rate change altered wheel spin by more than 0.1 percent");
            }
            float coasting500 = 100f, coasting1000 = 100f;
            for (int i = 0; i < 500; ++i) coasting500 = WheelPhysics.IntegrateSpin(coasting500, 0, 0, 1, 1f / 500);
            for (int i = 0; i < 1000; ++i) coasting1000 = WheelPhysics.IntegrateSpin(coasting1000, 0, 0, 1, 1f / 1000);
            Check(Math.Abs(coasting500 - coasting1000) < 0.005f, "Wheel resistance doubled with tick frequency");
            float coastingTwoSeconds = 100f;
            for (int i = 0; i < 2000; ++i)
                coastingTwoSeconds = WheelPhysics.IntegrateSpin(coastingTwoSeconds, 0, 0, 16f, 0.001f);
            Check(coastingTwoSeconds > 48f && coastingTwoSeconds < 51f, "Free wheel spin did not halve in about two seconds");
            Check(WheelPhysics.IntegrateSpin(1, 0, 1000, 1, 0.01f) == 0 &&
                WheelPhysics.IntegrateSpin(-1, 0, 1000, 1, 0.01f) == 0, "Braking reversed wheel direction");
            foreach (int fps in new[] { 30, 60, 120 })
            {
                float angle = 0f;
                for (int frame = 0; frame < fps; ++frame) angle += WheelPhysics.SpinDegrees(2f * (float)Math.PI, 1f / fps);
                Check(Math.Abs(angle - 360f) < 0.001f, "Wheel animation depends on rendering frame rate");
            }
            byte[] luaBytes = System.Text.Encoding.UTF8.GetBytes("return 1");
            byte[] mapBytes = System.Text.Encoding.UTF8.GetBytes("v 0 0 0");
            string contentHash = ChallengeContentHash.Compute(luaBytes, mapBytes);
            Check(contentHash.Length == 64 && contentHash == ChallengeContentHash.Compute(luaBytes, mapBytes),
                "Challenge content hash was not a deterministic SHA-256 digest");
            Check(contentHash != ChallengeContentHash.Compute(System.Text.Encoding.UTF8.GetBytes("return 2"), mapBytes),
                "Lua changes did not affect the challenge hash");
            Check(contentHash != ChallengeContentHash.Compute(luaBytes, System.Text.Encoding.UTF8.GetBytes("v 1 0 0")),
                "Map changes did not affect the challenge hash");
            Check(ChallengeContentHash.Compute(new byte[] { 1 }, new byte[] { 2, 3 }) !=
                ChallengeContentHash.Compute(new byte[] { 1, 2 }, new byte[] { 3 }),
                "Lua/map boundaries were ambiguous in the challenge hash");
            Check(contentHash != ChallengeContentHash.Compute(mapBytes, luaBytes), "Lua/map order did not affect the hash");
            Reject(() => ChallengeContentHash.Compute(null, mapBytes), "Null Lua contents accepted");
            Reject(() => ChallengeContentHash.Compute(luaBytes, null), "Null map contents accepted");
            string path = Path.Combine(directory, "progress.json");
            var store = new ChallengeProgressStore(path);
            var metadata = new ChallengeMetadata { challengeId = "permanent-id" };
            Check(store.Load().challenges.Count == 0, "First launch should have no records");
            Check(store.Record(metadata, Run("a", 42.731234, "First car")), "First finish missing");
            Check(!store.Record(metadata, Run("a", 42.731234, "First car")), "Duplicate finish counted");
            store.Record(metadata, Run("b", 48, "Slower car"));
            var progress = store.Load().challenges.Single();
            Check(progress.completionCount == 2 && progress.completed, "Completion count incorrect");
            Check(progress.bestRun.modelName == "First car" && progress.lastRun.modelName == "Slower car", "Slower finish overwrote best vehicle");
            Check(progress.bestRun.timeSeconds == 42.731234, "Timing precision lost");
            Check(progress.bestRun.modelSnapshot.Contains("\n") && progress.bestRun.chipCounts[0].count == 4, "Vehicle snapshot lost");
            Check(progress.bestRun.rules.forbiddenChipTypes[0] == "Jet" && progress.bestRun.physics.fixedDeltaTime == 0.001f, "Rules/settings lost");
            Check(progress.bestRun.playerName == "Driver" && progress.bestRun.finishedAtUtc.EndsWith("Z"), "Player/date lost");
            Check(File.Exists(path + ".bak") && !File.Exists(path + ".tmp"), "Atomic save/backup failed");
            store.Record(metadata, Run("c", 40, "Faster car"));
            Check(store.Load().challenges.Single().bestRun.runId == "c", "New personal best missing");
            store.Record(metadata, Run("d", 50, "New processor", 2));
            Check(store.Load().challenges.Count == 2, "Processor versions mixed");
            metadata.recordVersion++;
            store.Record(metadata, Run("e", 55, "New course"));
            Check(store.Load().challenges.Count == 3, "Course versions mixed");
            var untimed = Run("untimed", 0, "Survival car");
            untimed.hasTime = false;
            untimed.completionType = "survival";
            store.Record(metadata, untimed);
            progress = store.Load().challenges.Last();
            Check(!progress.lastRun.hasTime && progress.bestRun.runId == "e", "Untimed finish became a zero-time best");
            string before = File.ReadAllText(path);
            Reject(() => store.Record(metadata, Run("bad", double.NaN, "Invalid")), "NaN accepted");
            Reject(() => store.Record(metadata, Run("bad", -1, "Invalid")), "Negative time accepted");
            Check(File.ReadAllText(path) == before, "Invalid result changed the save");
            File.WriteAllText(path, "{not valid JSON");
            bool failed = false;
            try { store.Record(metadata, Run("f", 20, "Car")); } catch (Newtonsoft.Json.JsonException) { failed = true; }
            Check(failed && File.ReadAllText(path) == "{not valid JSON", "Corrupt save overwritten");

            var hashStore = new ChallengeProgressStore(Path.Combine(directory, "hash-progress.json"));
            var identityMetadata = new ChallengeMetadata();
            var originalRun = Run("original", 10, "Car");
            hashStore.Record(identityMetadata, originalRun);
            var renamedRun = Run("renamed", 11, "Car");
            renamedRun.challengeFileName = "renamed.lua";
            hashStore.Record(identityMetadata, renamedRun);
            var hashProgress = hashStore.Load().challenges.Single();
            Check(hashProgress.completionCount == 2 && hashProgress.challengeHash == originalRun.challengeHash &&
                hashProgress.challengeFileName == "renamed.lua", "Renaming split identical challenge contents or lost filename");
            Check(hashProgress.bestRun.challengeFileName == "test_challenge.lua" &&
                hashProgress.lastRun.challengeFileName == "renamed.lua", "Run filenames were overwritten by a rename");
            var editedRun = Run("edited", 9, "Car");
            editedRun.challengeHash = ChallengeContentHash.Compute(System.Text.Encoding.UTF8.GetBytes("changed Lua"), Array.Empty<byte>());
            hashStore.Record(identityMetadata, editedRun);
            Check(hashStore.Load().challenges.Count == 2, "Changed Lua contents shared progress with the original");
            var missingHash = Run("missing-hash", 1, "Car");
            missingHash.challengeHash = "";
            Reject(() => hashStore.Record(identityMetadata, missingHash), "Missing challenge hash accepted");
            var missingFileName = Run("missing-filename", 1, "Car");
            missingFileName.challengeFileName = "";
            Reject(() => hashStore.Record(identityMetadata, missingFileName), "Missing challenge filename accepted");
            string hashJson = File.ReadAllText(Path.Combine(directory, "hash-progress.json"));
            Check(hashJson.Contains("\"challengeHash\"") && hashJson.Contains("\"challengeFileName\"") &&
                !hashJson.Contains("\"challengeId\""), "Saved identity fields were not replaced");
            string oldPath = Path.Combine(directory, "old-progress.json");
            string oldJson = "{\"saveVersion\":1,\"challenges\":[{\"challengeId\":\"old-id\",\"completed\":true,\"completionCount\":2}]}";
            File.WriteAllText(oldPath, oldJson);
            var oldStore = new ChallengeProgressStore(oldPath);
            oldStore.Record(identityMetadata, originalRun);
            var upgraded = oldStore.Load();
            Check(upgraded.saveVersion == 2 && upgraded.challenges.Count == 2 &&
                upgraded.challenges[0].challengeHash == "" && upgraded.challenges[0].completionCount == 2,
                "Old records were lost or assigned an unverified hash");
            Check(File.ReadAllText(oldPath + ".v1.bak") == oldJson, "Original ID-based save was not preserved");

            var lua = new Script();
            var table = lua.DoString("return {id='community-level', title='Hill', recordVersion=3, challengeLogicVersion=1, rules={maxChips=30, forbiddenChipTypes={'Jet'}}}").Table;
            var parsed = ChallengeLuaMetadata.Parse(table, "fallback");
            Check(parsed.challengeId == "community-level" && parsed.recordVersion == 3 && parsed.rules.maxChips == 30, "Lua metadata not parsed");
            Check(parsed.rules.forbiddenChipTypes.Single() == "Jet", "Lua restrictions lost");
            Check(ChallengeLuaMetadata.Parse(lua.DoString("return {}").Table, "legacy:race").challengeId == "legacy:race", "Legacy fallback lost");
            Reject(() => ChallengeLuaMetadata.Parse(lua.DoString("return {id='x',challengeLogicVersion=99}").Table, ""), "Unsupported processor accepted");
            Reject(() => ChallengeLuaMetadata.Parse(lua.DoString("return {id='x',recordVersion=1.5}").Table, ""), "Fractional version accepted");
            Reject(() => ChallengeLuaMetadata.Parse(lua.DoString("return {id='x',rules={maxChips=-2}}").Table, ""), "Invalid restriction accepted");
            Reject(() => ChallengeLuaMetadata.Parse(lua.DoString("return {}").Table, ""), "Missing identity accepted");
            var standard = ChallengeLuaMetadata.Parse(lua.DoString("return {id='standard'}").Table, "");
            Check(standard.rules.physics.fixedDeltaTime == 0.001f && standard.rules.physics.solverIterations == 6 &&
                standard.rules.physics.solverVelocityIterations == 1, "Missing physics rules did not use the shared preset");
            var custom = ChallengeLuaMetadata.Parse(lua.DoString("return {id='custom',rules={physics={fixedDeltaTime=0.01,solverIterations=12,solverVelocityIterations=4}}}").Table, "");
            Check(custom.rules.physics.fixedDeltaTime == 0.01f && custom.rules.physics.solverIterations == 12 &&
                custom.rules.physics.solverVelocityIterations == 4, "Explicit physics rules were not parsed");
            var partial = ChallengeLuaMetadata.Parse(lua.DoString("return {id='partial',rules={physics={solverIterations=8}}}").Table, "");
            Check(partial.rules.physics.fixedDeltaTime == 0.001f && partial.rules.physics.solverIterations == 8,
                "Partial physics rules inherited user settings instead of the standard preset");
            Reject(() => ChallengeLuaMetadata.Parse(lua.DoString("return {id='x',rules={physics='invalid'}}").Table, ""), "Non-table physics accepted");
            Reject(() => ChallengeLuaMetadata.Parse(lua.DoString("return {id='x',rules={physics={fixedDeltaTime=0}}}").Table, ""), "Zero timestep accepted");
            Reject(() => ChallengeLuaMetadata.Parse(lua.DoString("return {id='x',rules={physics={fixedDeltaTime=1/0}}}").Table, ""), "Infinite timestep accepted");
            Reject(() => ChallengeLuaMetadata.Parse(lua.DoString("return {id='x',rules={physics={fixedDeltaTime=0/0}}}").Table, ""), "NaN timestep accepted");
            Reject(() => ChallengeLuaMetadata.Parse(lua.DoString("return {id='x',rules={physics={fixedDeltaTime=0.0001}}}").Table, ""), "Excessive physics frequency accepted");
            Reject(() => ChallengeLuaMetadata.Parse(lua.DoString("return {id='x',rules={physics={solverIterations=0}}}").Table, ""), "Zero solver iterations accepted");
            Reject(() => ChallengeLuaMetadata.Parse(lua.DoString("return {id='x',rules={physics={solverIterations=256}}}").Table, ""), "Excessive solver iterations accepted");
            Reject(() => ChallengeLuaMetadata.Parse(lua.DoString("return {id='x',rules={physics={solverVelocityIterations=1.5}}}").Table, ""), "Fractional solver iterations accepted");
            var copied = custom.rules.physics.Copy();
            copied.solverIterations = 20;
            Check(custom.rules.physics.solverIterations == 12 && !copied.Matches(custom.rules.physics), "Physics snapshots share mutable state");
            var physicsStore = new ChallengeProgressStore(Path.Combine(directory, "physics.json"));
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
            Check(physicsStore.Load().challenges.Count == 2, "Results using different required physics were mixed");
            var savedPhysics = physicsStore.Load().challenges.Last().lastRun;
            Check(savedPhysics.physics.Matches(savedPhysics.rules.physics), "Required/actual physics did not survive serialization");
            int beginCalls = 0;
            double? finishedTime = null;
            Table receivedGates = null;
            DynValue finishCallback = DynValue.Nil;
            ChallengeMetadata declared = null;
            ChallengeLuaCallbacks.Register(lua,
                value => declared = ChallengeLuaMetadata.Parse(value, "legacy:test"),
                () => beginCalls++, value => finishedTime = value,
                () => finishedTime, (gates, callback) => { receivedGates = gates; finishCallback = callback; });
            Check(lua.DoString("return GetRaceResult()").IsNil(), "Unfinished Lua race returned a result");
            lua.DoString("Challenge({id='callback-level'}); BeginChallengeAttempt(); Race({{position={1,2,3}},{position={4,5,6}}}, function(seconds) callbackTime=seconds end)");
            Check(declared.challengeId == "callback-level" && beginCalls == 1, "Lua configure/begin callbacks not forwarded");
            Check(receivedGates.Length == 2 && !finishCallback.IsNil(), "Lua race gates or callback not forwarded");
            lua.Call(finishCallback, 42.125);
            Check(lua.Globals.Get("callbackTime").Number == 42.125, "Finish callback did not receive exact time");
            lua.DoString("CompleteChallenge(42.125)");
            Check(finishedTime == 42.125 && lua.DoString("return GetRaceResult()").Number == 42.125, "Completion/getter boundary lost time");
            lua.DoString("CompleteChallenge()");
            Check(finishedTime == null, "Untimed Lua completion did not forward null");
            lua.DoString("Race({{},{}})");
            Check(finishCallback.IsNil(), "Legacy one-argument Race no longer supported");
            var race = ChallengeRaceDefinition.Parse(lua.DoString("return {start={0,0,0}, checkpoints={{10,0,0},{20,0,0}}, finish={30,0,0}, radius=4, color={255,0,0,120}, onStarted=function() started=true end, onCheckpoint=function(index,seconds) passed=index; split=seconds end, onFinished=function(seconds) result=seconds end}").Table, DynValue.Nil);
            Check(race.gates.Count == 4 && race.gates[0].position.x == 0 && race.gates[3].position.x == 30, "Named gate order incorrect");
            Check(race.gates.All(g => g.radius == 4 && g.color[0] == 255 && g.color[3] == 120), "Shared gate appearance lost");
            race.gates[0].color[0] = 0;
            Check(race.gates[1].color[0] == 255, "Gate colors unexpectedly share mutable arrays");
            lua.Call(race.onStarted);
            lua.Call(race.onCheckpoint, 2, 12.5);
            lua.Call(race.onFinished, 25.75);
            Check(lua.Globals.Get("started").Boolean && lua.Globals.Get("passed").Number == 2 && lua.Globals.Get("split").Number == 12.5 && lua.Globals.Get("result").Number == 25.75, "Named lifecycle callbacks lost arguments");
            var direct = ChallengeRaceDefinition.Parse(lua.DoString("return {start={x=0,y=1,z=2},finish={3,4,5}}").Table, DynValue.Nil);
            Check(direct.gates.Count == 2 && direct.gates[0].radius == 5 && direct.gates[0].position.y == 1, "Minimal race defaults invalid");
            var oldRace = ChallengeRaceDefinition.Parse(lua.DoString("return {{position={0,0,0},size={5}},{position={10,0,0},size={8}}}").Table, DynValue.Nil);
            Check(oldRace.gates[0].radius == 2.5f && oldRace.gates[1].radius == 4, "Original gate list changed its sizes");
            Reject(() => ChallengeRaceDefinition.Parse(lua.DoString("return {start={0,0,0}}").Table, DynValue.Nil), "Missing finish accepted");
            Reject(() => ChallengeRaceDefinition.Parse(lua.DoString("return {start={0,0},finish={1,2,3}}").Table, DynValue.Nil), "Incomplete position accepted");
            Reject(() => ChallengeRaceDefinition.Parse(lua.DoString("return {start={0,0,0},finish={1,2,3},radius=0}").Table, DynValue.Nil), "Zero radius accepted");
            Reject(() => ChallengeRaceDefinition.Parse(lua.DoString("return {start={0,0,0},finish={1,2,3},onFinished=42}").Table, DynValue.Nil), "Invalid named callback accepted");
            Reject(() => ChallengeRaceDefinition.Parse(lua.DoString("return {start={0,0,0},finish={1,2,3},checkpoints='bad'}").Table, DynValue.Nil), "Non-list checkpoints accepted");
            Reject(() => ChallengeRaceDefinition.Parse(lua.DoString("return {start={0,0,0},finish={1/0,2,3}}").Table, DynValue.Nil), "Infinite coordinate accepted");
            var progressState = new ChallengeRaceProgress(4);
            Check(!progressState.Pass(2) && progressState.NextGate == 0, "Out-of-order gate accepted");
            Check(progressState.Pass(0) && !progressState.Pass(0) && progressState.NextGate == 1, "Duplicate start accepted");
            progressState.Pass(1); progressState.Pass(2); progressState.Pass(3);
            Check(progressState.Finished && !progressState.Pass(3), "Finish was not one-shot");
            var retry = new ChallengeRaceProgress(4);
            Check(retry.NextGate == 0 && !retry.Finished, "Retry kept old progress");
            retry.Fail();
            Check(!retry.Pass(0), "Invalidated attempt accepted a gate");
            int restarts = 0;
            ChallengeLuaCallbacks.Register(lua, _ => {}, () => {}, _ => {}, () => null, (_,__) => {}, () => restarts++);
            lua.DoString("RestartRace()");
            Check(restarts == 1, "Lua retry callback not forwarded");
            CheckBundledChallenges();
            var intro = new DrivingIntroductionProgress();
            intro.Observe(false, false, 0, 5f, true, true, true);
            Check(intro.Current == DrivingIntroductionProgress.Lesson.Accelerate, "Introduction advanced before countdown finished");
            intro.Observe(true, false, 1, 5f, false, false, false);
            Check(intro.Current == DrivingIntroductionProgress.Lesson.Accelerate, "Gate alone skipped acceleration lesson");
            intro.Observe(true, false, 1, 5f, true, false, false);
            Check(intro.Current == DrivingIntroductionProgress.Lesson.Steer, "Driving through first gate did not teach steering");
            intro.Observe(true, false, 2, 5f, true, true, false);
            Check(intro.Current == DrivingIntroductionProgress.Lesson.Brake, "Steering through bend did not teach braking");
            intro.Observe(true, false, 2, 5f, false, false, true);
            Check(intro.Current == DrivingIntroductionProgress.Lesson.Brake, "Brake key alone completed braking lesson");
            intro.Observe(true, false, 2, 4f, false, false, true);
            Check(intro.Current == DrivingIntroductionProgress.Lesson.Finish, "Slowing under braking did not advance lesson");
            intro.Observe(true, true, 13, 4f, false, false, false);
            Check(intro.Current == DrivingIntroductionProgress.Lesson.Complete, "Finishing lap did not complete introduction");
            var shortcutIntro = new DrivingIntroductionProgress();
            shortcutIntro.Observe(true, false, 13, 0f, false, false, false);
            Check(shortcutIntro.Current != DrivingIntroductionProgress.Lesson.Complete, "Checkpoints completed introduction without finish");
            shortcutIntro.Observe(true, true, 13, 2f, false, false, false);
            Check(shortcutIntro.Current == DrivingIntroductionProgress.Lesson.Complete, "Finishing left tutorial stuck on an earlier hint");

            var playContext = new GameplayContext();
            playContext.SelectIntroduction("intro_track");
            Check(playContext.IsIntroduction && playContext.IsChallenge, "Introduction did not select its challenge");
            playContext.Retry(_ => { }, () => { throw new Exception("Introduction retry reset sandbox"); });
            Check(playContext.IsIntroduction, "Retry cleared introduction mode");
            playContext.SelectChallenge("intro_track");
            Check(!playContext.IsIntroduction && playContext.IsChallenge, "Normal race retained tutorial mode");
            playContext.SelectIntroduction("intro_track");
            playContext.SelectSandbox();
            Check(!playContext.IsIntroduction, "Leaving challenge retained tutorial mode");
            Check(!playContext.IsChallenge && playContext.ChallengeName == null, "New gameplay context was not regular sandbox");
            playContext.SelectChallenge("circuit1_race");
            int firstSelection = playContext.Revision;
            Check(playContext.IsChallenge && playContext.MatchesChallenge("circuit1_race", firstSelection), "Challenge selection was not retained");
            Check(playContext.ChallengeName == "circuit1_race" && playContext.MatchesChallenge("circuit1_race", firstSelection),
                "Reading the context consumed the challenge before a later editor return");
            Check(!playContext.MatchesChallenge("load_race", firstSelection), "Load accepted the wrong challenge");
            playContext.SelectChallenge("load_race");
            Check(!playContext.MatchesChallenge("circuit1_race", firstSelection) && playContext.ChallengeName == "load_race",
                "New challenge selection kept an old queued load valid");
            int oldSelection = playContext.Revision;
            playContext.SelectChallenge("load_race");
            Check(!playContext.MatchesChallenge("load_race", oldSelection), "Reselecting a challenge did not invalidate earlier queued loads");
            playContext.SelectSandbox();
            Check(!playContext.IsChallenge && playContext.ChallengeName == null && !playContext.MatchesChallenge("load_race", oldSelection),
                "Explicit sandbox selection kept challenge context");
            Reject(() => playContext.SelectChallenge(" "), "Empty challenge selection accepted");
            Check(!playContext.IsChallenge, "Invalid selection changed regular sandbox context");
            int challengeRetries = 0, modelResets = 0;
            string retriedChallenge = null;
            playContext.Retry(name => { challengeRetries++; retriedChallenge = name; }, () => modelResets++);
            Check(modelResets == 1 && challengeRetries == 0, "Regular retry did not reset only the model");
            playContext.SelectChallenge("circuit1_race");
            int retryRevision = playContext.Revision;
            playContext.Retry(name => { challengeRetries++; retriedChallenge = name; }, () => modelResets++);
            Check(challengeRetries == 1 && retriedChallenge == "circuit1_race" && modelResets == 1,
                "Challenge retry fell back to a model-only reset");
            playContext.Retry(name => { challengeRetries++; retriedChallenge = name; }, () => modelResets++);
            Check(challengeRetries == 2 && playContext.MatchesChallenge("circuit1_race", retryRevision),
                "Repeated retries lost challenge selection");
            playContext.SelectChallenge("custom_survival");
            playContext.Retry(name => retriedChallenge = name, () => modelResets++);
            Check(retriedChallenge == "custom_survival" && modelResets == 1,
                "Non-race challenge retry depended on an active race");
            var currentPhysics = ChallengePhysicsSettings.CreateStandard();
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
            var originalPhysics = ChallengePhysicsSettings.CreateStandard();
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
            Check(!ChallengeRaceStartTiming.Ready(0) && ChallengeRaceStartTiming.CountdownAt(0) == 0,
                "Race started immediately at gate entry");
            Check(!ChallengeRaceStartTiming.Ready(0.999) && ChallengeRaceStartTiming.CountdownAt(0.999) == 0,
                "Countdown started before the one-second settling hold");
            Check(ChallengeRaceStartTiming.CountdownAt(1) == 3 && !ChallengeRaceStartTiming.Ready(1),
                "Settling hold did not transition to countdown 3");
            Check(ChallengeRaceStartTiming.CountdownAt(2) == 2 && !ChallengeRaceStartTiming.Ready(2),
                "Countdown 2 timing incorrect");
            Check(ChallengeRaceStartTiming.CountdownAt(3) == 1 && !ChallengeRaceStartTiming.Ready(3),
                "Countdown 1 timing incorrect");
            Check(ChallengeRaceStartTiming.CountdownAt(3.999) == 1 && !ChallengeRaceStartTiming.Ready(3.999),
                "Race released before countdown completion");
            Check(ChallengeRaceStartTiming.Ready(4) && ChallengeRaceStartTiming.CountdownAt(4) == 0,
                "Race did not release at GO");
            Check(ChallengeRaceStartTiming.CountdownAt(5) == 0 && ChallengeRaceStartTiming.Ready(5),
                "Completed countdown produced an invalid negative number");
            Console.WriteLine("PASS: " + checks + " challenge metadata and persistent-record checks.");
        }
        finally { Directory.Delete(directory, true); }
    }
}
