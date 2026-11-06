# Scenario completion records

Declare metadata before setting up gameplay. `id` is permanent (use a UUID for a new player-made scenario); keep it when renaming files. Increment `recordVersion` when changing the course or restrictions. `scenarioLogicVersion` names the required processor/API; the supported version is currently 1.

```lua
Scenario({
    id = "b4220d19-b894-4275-9614-84921cbfebde",
    title = "Circuit 1",
    author = "Your name",
    recordVersion = 1,
    scenarioLogicVersion = 1,
    completionType = "timeTrial",
    rules = {
        forbiddenChipTypes = { "Jet" },
        maxChips = 30,
        physics = { fixedDeltaTime = 0.002, solverIterations = 6, solverVelocityIterations = 1 }
    }
})

LoadMap("circuit1")
SetPosition("player1", {0, 23, 0})

Race({
    start = {-1, 22, 10},
    checkpoints = {
        {-3, 22, 50},
        {-17, 25, 364}
    },
    finish = {-1, 22, 10}
})
```

`Race` supplies green spherical gates with radius 5 by default. `radius` and `color` optionally set the shared appearance. `checkpoints` is optional, so a two-gate race only needs `start` and `finish`. Each position accepts either `{x,y,z}` values or `{x=...,y=...,z=...}` named coordinates. Gate positions and physics settings use world units.

The first gate starts timing when the vehicle enters it from outside; spawning inside does not immediately start the attempt. The author should place the vehicle outside the start gate or expect the player to leave and re-enter. Intermediate checkpoints must be crossed in their declared order, and the final gate finishes and saves automatically. Gates remain spherical and nondirectional; the engine checks the swept movement segment between physics samples to avoid missing a gate at high speed. A newly active gate that already contains the vehicle requires leaving and re-entering, so intermediate gates should be spaced apart.

The author does not need to call `BeginScenarioAttempt`, `CompleteScenario`, or define `Loop` for an ordinary race. Advanced scripts can attach callbacks inside the same table:

```lua
Race({
    radius = 5,
    color = {0, 150, 0, 100},
    start = {0, 0, 0},
    checkpoints = {{20, 0, 0}, {40, 0, 0}},
    finish = {60, 0, 0},
    onStarted = function()
        Print("Go!", {t=1})
    end,
    onCheckpoint = function(index, seconds)
        -- index starts at 1 for the first intermediate checkpoint.
    end,
    onFinished = function(seconds)
        -- The result is already saved; this callback adds optional reactions.
    end
})
```

`GetRaceResult()` returns nil before finishing and the frozen final seconds afterward. Ctrl+R or Lua's `RestartRace()` returns the model to its original race spawn, clears motion and progress, waits for model rebuilding, and rearms the start gate. A retry uses the current model after checking the scenario rules again at the start. Repeating `Race(...)` replaces the old course and disables its gates before creating new ones. The original `Race(gateList, optionalFinishCallback)` format remains supported.

Metadata-free older scenarios still load and record under `legacy:<filename>`; explicit IDs preserve records when files are renamed or shared.

For a custom objective, explicitly call the begin and complete bindings from your own start/finish logic:

```lua
BeginScenarioAttempt()         -- capture the starting vehicle/settings
StartTimer("custom")
-- Later, when your objective's finish condition is satisfied:
CompleteScenario(GetTime("custom"))
-- For an untimed objective, use CompleteScenario() instead.
```

The built-in race saves automatically; do not also begin a custom attempt for the same race. Duplicate completion calls after a successful finish are ignored. A reset, changed model/physics settings, or leaving play mode invalidates an attempt. Ctrl+R restarts a built-in race; custom objectives can call BeginScenarioAttempt again when ready. Forbidden chip types and the total chip limit are checked at attempt start; menu-side previews and rejecting invalid model loads are not implemented here.

Records are written to `Path.Combine(Application.persistentDataPath, "scenario-progress.json")` with a `.bak` of the prior save and temporary-file replacement. Unreadable saves are reported instead of overwritten. Records are grouped by scenario ID, course/rules record version, and actual processor version. A slower finish updates the last run without replacing the best run or its vehicle. Untimed completions never become zero-time bests.

Vehicle Lua snapshots, chip counts and physics settings are captured at the start. The finish captures UTC time and `PlayerPrefs.GetString("PlayerName", "Player")`; a player-name entry screen is not yet implemented. Game version comes from `Application.version`. The C# `GameScript.ScenarioCompleted` event fires after persistence succeeds. Scenario result rendering is a separate integration.

## Required physics

`rules.physics` defines required simulation settings, not player-selectable preferences. Omitted fields use a shared preset: `fixedDeltaTime = 0.002` (500 Hz), `solverIterations = 6`, `solverVelocityIterations = 1`. Steps must be between 0.0005 and 0.02 seconds (2000 to 50 Hz); solver counts must be integers from 1 to 255.

Scenario loading applies the rules to the global timestep/default solvers and existing rigidbodies. Rebuilt chip bodies receive the same solvers. Each run stores both the required rule snapshot and actual physics snapshot. Changing physics during an attempt invalidates it; the store rejects mismatched required/actual settings and keeps results from different physics presets separate.

Leaving singleplayer unloads the active scenario and restores the prior timestep and surviving bodies' previous solvers without rewriting player preferences. Replacing or failing to load a scenario also releases its override. The loaded map remains available after scenario unloading.

## Play mode and editor transitions

The main menu's Scenarios screen selects and launches a scenario. Regular Singleplayer no longer contains a scenario picker or scenario-loading shortcuts. Choosing Singleplayer explicitly clears the scenario selection; joining a multiplayer server clears it too.

The shared `GameManager.PlayContext` remembers the chosen scenario while the editor is open. Leaving play mode unloads the scenario runtime and restores player physics settings, but does not erase the selection. Returning from the editor through Play/Ctrl+P rebuilds the edited vehicle and reloads the complete selected scenario with fresh gates, timers, objects and attempt state. Regular singleplayer returns to regular singleplayer. Queued scenario loads are cancelled when leaving play mode or selecting a different context, and a scenario reload happens only once per play entry.
