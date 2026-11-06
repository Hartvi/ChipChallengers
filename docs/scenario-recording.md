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
        physics = { fixedDeltaTime = 0.001, solverIterations = 1, solverVelocityIterations = 1 }
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

Unity's `OnTriggerEnter` detects the local core entering each spherical gate. The start gate stops the whole vehicle, holds it for one second, and then displays 3-2-1 while keeping the vehicle frozen. GO releases the vehicle, captures the attempt and starts timing; the hold and countdown are excluded from the result. Intermediate checkpoints must be entered in their declared order, and the final gate finishes and saves automatically. Gate times use the trigger callback time; no custom movement sweep or time interpolation runs. Unity can report entry when a gate is enabled around an overlapping core.

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

`GetRaceResult()` returns nil before finishing and the frozen final seconds afterward. Ctrl+R unloads and reloads the entire selected scenario, recreating its Lua state, map, objects, timers and gates after the player model is reset. Lua's `RestartRace()` is a narrower operation that returns the model to its race spawn, clears race motion and progress, waits for rebuilding, and rearms the start gate without rerunning the scenario script. A retry uses the current model after checking the scenario rules again at the start. Repeating `Race(...)` replaces the old course and disables its gates before creating new ones. The original `Race(gateList, optionalFinishCallback)` format remains supported.

Metadata-free older scenarios still load and record under `legacy:<filename>`; explicit IDs preserve records when files are renamed or shared.

For a custom objective, explicitly call the begin and complete bindings from your own start/finish logic:

```lua
BeginScenarioAttempt()         -- capture the starting vehicle/settings
StartTimer("custom")
-- Later, when your objective's finish condition is satisfied:
CompleteScenario(GetTime("custom"))
-- For an untimed objective, use CompleteScenario() instead.
```

The built-in race saves automatically; do not also begin a custom attempt for the same race. Duplicate completion calls after a successful finish are ignored. A reset, changed model/physics settings, or leaving play mode invalidates an attempt. Ctrl+R restarts the whole scenario for both races and custom objectives; custom Lua logic can also call BeginScenarioAttempt again when ready. Forbidden chip types and the total chip limit are checked at attempt start; menu-side previews and rejecting invalid model loads are not implemented here.

Records are written to `Path.Combine(Application.persistentDataPath, "scenario-progress.json")` with a `.bak` of the prior save and temporary-file replacement. Unreadable saves are reported instead of overwritten. Records are grouped by scenario ID, course/rules record version, and actual processor version. A slower finish updates the last run without replacing the best run or its vehicle. Untimed completions never become zero-time bests.

Vehicle Lua snapshots, chip counts and physics settings are captured at the start. The finish captures UTC time and `PlayerPrefs.GetString("PlayerName", "Player")`; a player-name entry screen is not yet implemented. Game version comes from `Application.version`. The C# `GameScript.ScenarioCompleted` event fires after persistence succeeds. Scenario result rendering is a separate integration.

Attempt validity checks in `Update` compare only live player, reset, play-mode and global physics state. Physics ticks do not poll attempt validity. Full model serialization and per-body solver verification happen once before saving a finish; model edits that do not reset the vehicle are therefore detected at completion.

## Required physics

`rules.physics` defines required simulation settings, not player-selectable preferences. Omitted fields use a shared preset: `fixedDeltaTime = 0.001` (1000 Hz), `solverIterations = 1`, `solverVelocityIterations = 1`. Steps must be between 0.0005 and 0.02 seconds (2000 to 50 Hz); solver counts must be integers from 1 to 255.

Scenario loading applies the rules to the global timestep/default solvers and existing rigidbodies. Rebuilt chip bodies receive the same solvers. Each run stores both the required rule snapshot and actual physics snapshot. Changing physics during an attempt invalidates it; the store rejects mismatched required/actual settings and keeps results from different physics presets separate.

Leaving singleplayer unloads the active scenario and restores the prior timestep and surviving bodies' previous solvers without rewriting player preferences. Replacing or failing to load a scenario also releases its override. The loaded map remains available after scenario unloading.

## Play mode and editor transitions

The main menu's Scenarios screen selects and launches a scenario. Regular Singleplayer no longer contains a scenario picker or scenario-loading shortcuts. Choosing Singleplayer explicitly clears the scenario selection; joining a multiplayer server clears it too.

The shared `GameManager.PlayContext` remembers the chosen scenario while the editor is open. Leaving play mode unloads the scenario runtime and restores player physics settings, but does not erase the selection. Returning from the editor through Play/Ctrl+P rebuilds the edited vehicle and reloads the complete selected scenario with fresh gates, timers, objects and attempt state. Regular singleplayer returns to regular singleplayer. Queued scenario loads are cancelled when leaving play mode or selecting a different context, and a scenario reload happens only once per play entry.

## Scenario spawn positioning

Scenario loading resets the vehicle to the current map's default spawn before Lua initialization. `LoadMap(...)` resets it again after the new map's colliders are synchronized; the old map is disabled before destruction so spawn raycasts cannot hit it. A subsequent `SetPosition("player1", {...})` or `SetRotation(...)` supplies the authored scenario pose. Those operations and race retries freeze and clear motion across the complete vehicle before moving the core and rebuilding its children.

## Physics configuration callbacks

`PhysicsSettingsManager` is the central API for timestep/default solver changes and individual rigidbody solver changes. `Changing` runs before the write and `Changed` runs afterward. Both events provide `PhysicsSettingsChange` with independent `Previous`/`Next` snapshots, a `Reason`, and a `Target` (`null` for global settings or the affected Rigidbody). Reapplying identical settings sends no event. Invalid configurations are rejected before notifications; listener exceptions are logged without preventing the other listeners from running. Callbacks cannot recursively apply more physics settings.

```csharp
PhysicsSettingsManager.Changed += OnPhysicsChanged;
// OnPhysicsChanged recalculates any cached configuration-dependent values.
// The subscriber removes the same handler when disabled or destroyed.
PhysicsSettingsManager.Changed -= OnPhysicsChanged;
```

Player preferences, scenario rules, scenario restoration and rebuilt body solver writes now use this API. Active attempts subscribe to `Changing` and invalidate before any actual configuration write; completing, cancelling or unloading an attempt removes that subscription. Forces, steering, Lua variable updates, and ordinary physics ticks do not send configuration notifications. Direct external writes to Unity properties bypass this API; the existing validity checks remain a fallback.

## Race start staging

The start gate clears linear/angular velocities, wheel spin and cached wheel momentum before the settling hold. Core runtime forces and Lua vehicle controls pause during staging; the scenario Lua loop remains available. `onStarted()` runs at GO, not at the initial gate crossing. Gate progression remains blocked until GO. Restarting, replacing or unloading a scenario cancels its pending countdown and releases the held vehicle; resets and menu transitions during countdown cancel the start. The editor freeze flag remains separate from the temporary race-start hold.
