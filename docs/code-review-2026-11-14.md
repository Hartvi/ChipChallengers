# Code review — 14 November 2026

This is the original review snapshot, before fixes. All 32 numbered findings and the identified helper bugs have since been addressed; see [fixes and verification](bugfix-progress.md) for current status.

There are correctness and reliability problems beyond onboarding and menu appearance. I would fix the build, model serialization, scripting, and multiplayer failures before treating the mechanics as settled.

This review changed no game code.

## Scope and evidence

Read all **153 C# files, 16,348 lines**, under `Assets` after excluding `Assets/Mirror`, `Assets/Plugins`, and `Assets/OBJImport`. The last folder is also third-party code: its README identifies Dummiesman's OBJ importer. I inspected a few dependency implementation details to check integration assumptions, but did not audit those libraries.

| Area | Files reviewed |
| --- | ---: |
| Common, including attributes | 30 |
| Datatypes | 1 |
| LuaStuff | 10 |
| Multiplayer | 2 |
| Playmode | 31 |
| StaticClasses | 2 |
| Tests | 5 |
| Ui | 72 |

Also inspected relevant Lua definitions, bundled vehicle data, prefabs, project settings, documentation, and the standalone test runner.

Verification completed:

- Existing standalone suite: **298 checks passed** using `dotnet run --project Tests/ScenarioRecording/ScenarioRecording.Tests.csproj`.
- Bundled challenges: **15 checks passed** with the same command followed by `-- --challenges-only`.
- An isolated temporary .NET harness reproduced the serialization, regex rewriting, numeric-input, redo, variable-initialization, backstep, key-discovery, and accelerometer issues below. It used original source files or unchanged extracted methods, the project's MoonSharp assembly, and small Unity stubs. This checks those methods' logic; it does not simulate Unity physics or Mirror.
- A narrow compilation check, with `UNITY_EDITOR` undefined and no editor assembly referenced, reproduced `CS0246` at `TestHealthEditor.cs:5`.

The Unity editor was already open. I did not interrupt it, run a full player build, execute Unity Edit/Play-mode tests, perform a two-instance multiplayer test, profile a live scene, or assess rendered menu appearance. Findings labelled **source trace** need those runtime checks to measure their full impact. The passing standalone suites mainly cover challenge metadata, records, race progression helpers, physics settings, and wheel math; they do not establish that the complete game loop works correctly.

P1 means high priority before release; P2 means a normal-priority functional defect; P3 means a smaller defect or polish issue. Some files have no independent finding.

## High-priority findings

**1. [P1] Player builds include an unguarded editor namespace.**

[TestHealthEditor.cs:5](C:/Users/Acer/ChipChallengers/Assets/Tests/TestHealthEditor.cs:5) imports `UnityEditor` before `#if UNITY_EDITOR`, and the file is outside an `Editor` folder. Guarding the class does not guard the using directive. The isolated compilation check fails with `CS0246`. Move the file into `Assets/Tests/Editor`, or put the editor import inside the conditional too. Verify with a real player build afterward.

**2. [P1] Saving decimal variable values can silently change them on reload.**

[VVar.cs:95](C:/Users/Acer/ChipChallengers/Assets/Playmode/Virtual/VVar.cs:95) formats floats using the current culture. Under `fr-FR` and `de-DE`, `defaultValue = 0.5` becomes `defaultValue = 0,5` in Lua. That is valid Lua table syntax, but the named field reloads as **0**, with 5 becoming a separate positional entry. The harness reproduced that silent corruption. Other number parsing/formatting also relies on current culture. Use invariant culture for persisted numbers, and round-trip tests that compare all model values across locales.

**3. [P1] Ordinary Lua block comments make a model impossible to serialize safely.**

[VModel.cs:178](C:/Users/Acer/ChipChallengers/Assets/Playmode/Virtual/VModel.cs:178) wraps the script in `[[...]]` without escaping or choosing a safe delimiter. A valid script containing `--[[ comment ]]` or its own long string terminates that wrapper early. The harness reproduced a syntax error. This serialization also carries edits to the server, so the failure affects editing and rebuilding as well as disk saves. Use escaped strings, a delimiter guaranteed absent from the content, or a structured model format with the script as data.

**4. [P1] The regex variable transformation breaks basic valid Lua.**

[ExtraFunctions.cs:161](C:/Users/Acer/ChipChallengers/Assets/LuaStuff/ExtraFunctions.cs:161) treats an equals sign in `a == 0` as an assignment. It also consumes `end` in a one-line assignment and rewrites names inside strings. All these valid examples failed after transformation in the harness when `a` was a model variable:

```lua
if a == 0 then
    Print('yes', {})
end

if Key('w') then a = 1 end

local a = 1

Print("value a changed", {})
```

The last example becomes a string containing unescaped `GetVar("a")` text. Regex substitutions cannot preserve Lua syntax, scope, strings, and comments reliably. Expose variables through an explicit API/table, a suitable Lua environment/metatable, or a proper parser.

**5. [P1] A script error on a vehicle with sensors can escape error handling and strand rebuilding.**

The constructor catches `DoString` errors, but [LinkSensors at ExtraFunctions.cs:145](C:/Users/Acer/ChipChallengers/Assets/LuaStuff/ExtraFunctions.cs:145) executes the script again without a catch, once per sensor. [CoreChip.cs:546](C:/Users/Acer/ChipChallengers/Assets/Playmode/Chips/CoreChip.cs:546) calls that while rebuilding, before the bookkeeping that completes the rebuild. A syntax error therefore follows a second, unhandled path when sensors exist. Top-level initialization also executes repeatedly, and can refer to sensors not registered yet. **Source trace:** try committing an incomplete script on the hover model and verify recovery. Register every binding first, compile/initialize once under one error boundary, and keep the previous usable model if initialization fails.

**6. [P1] User Lua has no execution budget and can hang the game or host.**

[VModel.cs:195](C:/Users/Acer/ChipChallengers/Assets/Playmode/Virtual/VModel.cs:195), [ExtraFunctions.cs:63](C:/Users/Acer/ChipChallengers/Assets/LuaStuff/ExtraFunctions.cs:63), and the loop callbacks execute synchronously. A script such as `function Loop() while true do end end` never returns. A model table is also evaluated as executable Lua before validation, including when submitted through [CmdLoadString at CoreChip.cs:218](C:/Users/Acer/ChipChallengers/Assets/Playmode/Chips/CoreChip.cs:218). Exceptions cannot catch a loop that never yields. `SafeCode` does not protect the interpreter. Use execution limits/cooperative coroutine budgeting and bounded model sizes, and parse the model's structure as data. I did not execute an infinite loop in the running game. This finding concerns CPU/memory exhaustion; the installed MoonSharp Unity platform filters out its ordinary IO/system modules.

**7. [P1] Model validation can succeed before a missing core fails outside the load catch.**

[VModel.cs:238](C:/Users/Acer/ChipChallengers/Assets/Playmode/Virtual/VModel.cs:238) checks some parent relationships but does not require exactly one actual Core chip, unique IDs, and a connected acyclic tree. An empty chip table passes that constructor. [CoreChip.cs:272](C:/Users/Acer/ChipChallengers/Assets/Playmode/Chips/CoreChip.cs:272) then assigns the model outside its parsing catch, and the setter accesses `value.Core`, whose `First` throws. Disconnected cycles are also not rejected there. **Source trace:** loading malformed or edited files can reach partial/unhandled setup. Fully validate the model and its property domains before publishing it or destroying the current assembly.

**8. [P1] A debug movement boost ships on the camera-height key.**

[SandboxMenu.cs:483](C:/Users/Acer/ChipChallengers/Assets/Ui/Menus/Sandbox/SandboxMenu.cs:483) applies `AddForce(Vector3.forward, ForceMode.VelocityChange)` every rendered frame while Up Arrow is held. The surrounding editor-only directives are commented out. Later, that same key increases camera height. This gives host/single-player vehicles a free world-forward boost, with strength depending on FPS, including in recorded challenges. **Source trace:** remove it or restrict it to a development-only path that invalidates competitive attempts.

**9. [P1] Another player's disconnection sends remaining clients to the main menu.**

[CoreChip.cs:768](C:/Users/Acer/ChipChallengers/Assets/Playmode/Chips/CoreChip.cs:768) calls `SwitchToMenu(MainMenu)` in every core's `OnStopClient`. That callback also runs when a remote player's core despawns, while the local player remains connected. **Source trace:** verify with a host and two clients, then disconnect one client. Put session navigation in the local connection lifecycle, or distinguish the local player's teardown from remote object removal.

## Mechanics, multiplayer, and performance

**10. [P2] Vehicle control behavior depends on rendering frame rate.**

[LoopScript.cs:39](C:/Users/Acer/ChipChallengers/Assets/LuaStuff/LoopScript.cs:39) applies `backstep` per call, and [CoreChip.cs:735](C:/Users/Acer/ChipChallengers/Assets/Playmode/Chips/CoreChip.cs:735) runs it from `Update`. A variable starting at 50 with backstep 1 became **35 at 30 FPS** and **0 at 120 FPS** after the same half second in the harness. Bundled vehicle scripts also increment steering/power by fixed amounts per loop. Rendering settings and hardware therefore affect response even when the physics timestep is identical. Choose a defined control tick or elapsed-time semantics, expose the timestep to scripts, and version/migrate existing models as needed.

**11. [P2] The accelerometer normally samples zero in the vehicle Lua loop.**

[SensorAspect.cs:39](C:/Users/Acer/ChipChallengers/Assets/Playmode/Chips/Aspects/SensorAspect.cs:39) copies the current velocity into `oldVelocity` during `RuntimeFunction`. Core runs those functions immediately before the Lua loop. [ReadAcceleration at line 75](C:/Users/Acer/ChipChallengers/Assets/Playmode/Chips/Aspects/SensorAspect.cs:75) then subtracts the just-copied value, with no physics step between them. The isolated ordering check returned `(0,0,0)`. Sample and cache acceleration across physics ticks; let Lua read that cached sample.

**12. [P2] Map and challenge world changes are local, while vehicle simulation is hosted.**

[VMap.cs:53](C:/Users/Acer/ChipChallengers/Assets/Common/VMap.cs:53) loads ordinary local GameObjects. [MapPanel.cs:56](C:/Users/Acer/ChipChallengers/Assets/Ui/Menus/Sandbox/MapPanel.cs:56) and [GameScript.cs:217](C:/Users/Acer/ChipChallengers/Assets/LuaStuff/GameScript.cs:217) do not distribute the selected map or challenge world state through Mirror. The default map can match initially, but changing maps can leave clients seeing different geometry from the host's collision world. **Source trace:** test a host map change and a joining client. Make world selection authoritative and replicated, with readiness/content checks, or restrict this UI in multiplayer until supported.

**13. [P2] Remote vehicle variables are not applied on observing clients.**

[CoreChip.cs:739](C:/Users/Acer/ChipChallengers/Assets/Playmode/Chips/CoreChip.cs:739) calls `SetSyncVariableValues` only for `isLocalPlayer`. A remote core's synchronized array arrives, but its local `VVar` callbacks are not updated. Networked transforms and `Omega` can conceal some of this, while variable-driven colors, gun firing/effects, and jet effects can disagree. **Source trace:** apply synchronized presentation values to all relevant client-side models, with generation/readiness checks and host duplication handled deliberately.

**14. [P2] Multiplayer mouse coordinates use the host's screen dimensions.**

[CoreChip.cs:718](C:/Users/Acer/ChipChallengers/Assets/Playmode/Chips/CoreChip.cs:718) transmits raw pixel coordinates. [ExtraFunctions.cs:229](C:/Users/Acer/ChipChallengers/Assets/LuaStuff/ExtraFunctions.cs:229) normalizes them using `Screen.width` and `Screen.height` where Lua executes: the host. **Source trace:** clients at different resolutions get incorrect normalized aim positions. Normalize on the sending client or transmit its viewport dimensions.

**15. [P2] Undo retains an abandoned redo branch, and non-host clients use an empty local history.**

[HistoryStack.cs:11](C:/Users/Acer/ChipChallengers/Assets/Ui/Menus/ChipEditor/HistoryStack.cs:11) never clears `futureEdits` on a new edit. The harness sequence `A → B → C → Undo → D → Redo` returned **C**. Separately, history is recorded in server-only `LoadString`, but [EditorMenu.cs:431](C:/Users/Acer/ChipChallengers/Assets/Ui/Menus/ChipEditor/EditorMenu.cs:431) reads `ClientCoreChip.history` locally. On a non-host client that history has no recorded edits, so undo returns the current state. Clear redo on a new branch and choose a consistent client- or server-owned history model.

**16. [P2] Building the virtual parent/child links does cubic work.**

[VModel.cs:49](C:/Users/Acer/ChipChallengers/Assets/Playmode/Virtual/VModel.cs:49) loops over every chip, loops over every chip again, and runs a full-array `Where` each inner iteration. The inner `k` is unused. This is approximately **n³ comparisons** whenever `chips` is assigned: 125 million at 500 chips. Every edit can then serialize, send, and rebuild the entire model. Remove the redundant loop and build an ID/parent lookup once; profile realistic larger vehicles afterward. This complexity follows directly from the source, independent of a timing benchmark.

**17. [P2] Rebuilding aspects leaves their AudioSource components behind.**

[BaseSoundAspect.cs:14](C:/Users/Acer/ChipChallengers/Assets/Playmode/Chips/Aspects/BaseSoundAspect.cs:14) adds sources directly to the chip's GameObject. Aspect replacement destroys the aspect component, but its inherited [OnDestroy at BaseAspect.cs:40](C:/Users/Acer/ChipChallengers/Assets/Playmode/Chips/Aspects/BaseAspect.cs:40) does not delete the pool. The sound subclasses also leave deletion commented out. Pose resets reuse chip GameObjects, so repeated resets can accumulate sources. **Source trace:** count AudioSources before/after repeated retries in Unity. Release the owned sources when their aspect is destroyed.

**18. [P2] Replacing a model retains the old runtime child tree.**

[GeometricChip.cs:165](C:/Users/Acer/ChipChallengers/Assets/Playmode/Chips/GeometricChip.cs:165) appends to `childChips`, but that list is not cleared when [GenerateChips at CoreChip.cs:443](C:/Users/Acer/ChipChallengers/Assets/Playmode/Chips/CoreChip.cs:443) destroys the old children and replaces `AllChildren`. The surviving core can retain old managed chip/model references. Death traversal still walks that list. **Source trace:** inspect the core's child count and retained objects across repeated model loads. Clear and rebuild both directions of the runtime tree together.

**19. [P2] Detached jets and wheels retain their last variable-driven command.**

[CommonChip.cs:519](C:/Users/Acer/ChipChallengers/Assets/Playmode/Chips/CommonChip.cs:519) marks descendants dead and removes variable callbacks, but does not reset their drive values. [JetAspect.cs:9](C:/Users/Acer/ChipChallengers/Assets/Playmode/Chips/Aspects/JetAspect.cs:9) and [WheelAspects.cs:269](C:/Users/Acer/ChipChallengers/Assets/Playmode/Chips/Aspects/WheelAspects.cs:269) continue applying those values; the gun explicitly checks `IsDead`. Releasing the controlling key can no longer update the detached engine. **Source trace:** define the intended detachment behavior and implement it consistently, whether it is shutdown or continued independent control.

**20. [P2] Gun power can request millions of pooled bullets during initialization.**

[GunAspect.cs:34](C:/Users/Acer/ChipChallengers/Assets/Playmode/Chips/Aspects/GunAspect.cs:34) derives pool size from `1000 / power * 5`. A valid small positive power of `0.001` requests roughly **five million Bullet objects** eagerly. Zero, the default for a new gun, enters a division-by-zero/infinite-rate path. Power is also captured only in `Awake`, although Value supports variables. **Source trace:** validate finite power/rate limits, bound or lazily grow the pool, and define which gun properties can change at runtime.

**21. [P2] Bullet impact momentum and gun recoil use different masses.**

[GunAspect.cs:26](C:/Users/Acer/ChipChallengers/Assets/Playmode/Chips/Aspects/GunAspect.cs:26) computes recoil using `projectileWeight = 0.0001 * power`, but never assigns that mass to the projectile Rigidbody. [Bullet.cs:64](C:/Users/Acer/ChipChallengers/Assets/Playmode/GunRelated/Bullet.cs:64) transfers momentum using the Rigidbody's mass, which is **1 kg** in the bundled prefab. At power 100, recoil uses 0.01 kg while impact uses 1 kg: a factor of 100 difference before other motion is considered. **Source trace:** share one projectile mass between launch/recoil and impact if momentum consistency is intended.

**22. [P2] Wheel support does not push the contacted body downward.**

[WheelAspects.cs:255](C:/Users/Acer/ChipChallengers/Assets/Playmode/Chips/Aspects/WheelAspects.cs:255) applies normal support to the wheel. The equal/opposite force in [ApplyForce at line 137](C:/Users/Acer/ChipChallengers/Assets/Playmode/Chips/Aspects/WheelAspects.cs:137) covers tangential tire forces, but omits that normal support. Static terrain masks this; a dynamic platform does not receive the corresponding vehicle load. **Source trace:** test a vehicle supported by a movable body and distribute support reactions to the actual contacts.

## Settings, saving, and interface behavior

**23. [P2] A fresh install defaults to mute and zero particles.**

[GameManager.cs:146](C:/Users/Acer/ChipChallengers/Assets/StaticClasses/GameManager.cs:146) and the particle getter use `PlayerPrefs.GetInt` without a supplied default. A missing preference returns 0, which is within both allowed ranges. The declared defaults of 20 and 100 are never selected. **Source trace:** use `GetInt(key, defaultIntSettings[key])` before range validation, as the solver getter already does.

**24. [P2] Clearing a numeric setting throws an exception.**

[SettingFields.cs:90](C:/Users/Acer/ChipChallengers/Assets/Ui/Menus/Settings/SettingFields.cs:90) repeatedly removes the last character until parsing succeeds. Empty input, `abc`, or `-` reaches `Substring(0, -1)`. All three threw `ArgumentOutOfRangeException` in the harness. Handle empty/invalid input explicitly and restore a valid value with feedback instead of silently truncating arbitrary text.

**25. [P2] Player-created vehicles are saved into the installed content directory.**

[IOHelpers.cs:135](C:/Users/Acer/ChipChallengers/Assets/Common/IOHelpers.cs:135) saves into `Application.streamingAssetsPath`. That ties user work to the game installation, which may be read-only and may be replaced during updates/reinstallation. Challenge progress already uses `persistentDataPath`; vehicle saves should have a similar user-data location, with bundled models kept as examples. **Source trace:** test saving from the actual distribution/install location and migrate existing user files.

**26. [P2] Vehicle saves overwrite directly, and the existing-file warning misses ordinary names.**

[IOHelpers.cs:138](C:/Users/Acer/ChipChallengers/Assets/Common/IOHelpers.cs:138) writes directly over the destination without a temporary replacement/backup. An interrupted or failed write can damage the only copy. Also, [SavePanel.cs:39](C:/Users/Acer/ChipChallengers/Assets/Ui/Menus/ChipEditor/SavePanel.cs:39) checks existence using the extensionless list label, so selecting `car` usually checks `Models/car`, not `Models/car.txt`. Saving itself does not require an overwrite decision, and the panel closes on failure. Normalize the filename once, make overwrites explicit, keep the panel open on errors, and use the atomic-write approach already present in `ChallengeProgressStore`.

**27. [P2] F1 help in the editor does not receive the input needed to close itself.**

[EditorMenu.cs:505](C:/Users/Acer/ChipChallengers/Assets/Ui/Menus/ChipEditor/EditorMenu.cs:505) activates `ControlsPanel` directly. The panel's close-on-key behavior is in its `InputReceiver.HandleInputs`, but the editor never transfers input ownership to it. Sandbox uses `UIManager.SwitchToMe`, which does transfer ownership. **Source trace:** use the same focus path and verify opening/dismissing help from each editor panel.

**28. [P2] Independent unlabelled messages overwrite one another.**

[DisplaySingleton.cs:86](C:/Users/Acer/ChipChallengers/Assets/Common/DisplaySingleton.cs:86) uses the supplied ID directly, including the default empty string. The comment says an omitted ID creates a new message, but no ID is generated. Multiple Lua `Print` calls at different positions therefore share one text object, and frequent prints can overwrite a save/load/script error using that same default slot. Make the default behavior explicit: independent generated IDs with cleanup, or deliberately named reusable slots for each message channel.

**29. [P2] Personal bests survive changes to the actual map contents.**

[GameScript.ScenarioRecording.cs:38](C:/Users/Acer/ChipChallengers/Assets/LuaStuff/GameScript.ScenarioRecording.cs:38) and [ScenarioMenu.cs:116](C:/Users/Acer/ChipChallengers/Assets/Ui/Menus/Scenario/ScenarioMenu.cs:116) hash Lua with empty map bytes. Editing the course OBJ without changing Lua preserves the record identity, even if the route becomes easier. This limitation is already documented in the repository, but it means record comparability is unfinished. Include the loaded map and other gameplay-relevant content in the identity used by both recording and browsing.

**30. [P2] Key discovery accepts fewer inputs than the input API and can throw for uppercase letters.**

[CoreChip.cs:288](C:/Users/Acer/ChipChallengers/Assets/Playmode/Chips/CoreChip.cs:288) discovers only tightly formatted single-letter literals. The harness found no input for `Key('1')`, `Key(' ')`, or `Key( 'w' )`. `Key('W')` is discovered, but [InputHelper.cs:6](C:/Users/Acer/ChipChallengers/Assets/Common/InputHelper.cs:6) contains no uppercase mapping, and TransferControls indexes it directly. The ungrouped alternation also discovers a single-quoted call three times. Use a defined key vocabulary and explicit registration, or transmit a bounded supported input state without parsing script text.

**31. [P3] Variable initialization and validation are inconsistent.**

[VVar.cs:180](C:/Users/Acer/ChipChallengers/Assets/Playmode/Virtual/VVar.cs:180) sets `currentValue` before setting min/max, so it clamps against the fields' initial zero bounds. The harness produced default 5/current 0. A later rebuild often masks this, but the constructor itself is wrong. [VariableFields.cs:95](C:/Users/Acer/ChipChallengers/Assets/Ui/Menus/ChipEditor/VariablePanel/VariableFields.cs:95) displays an invalid-name warning and continues adding the variable. Finite values, nonnegative backstep, and `min <= default <= max` are not enforced consistently. Initialize validated bounds before the current value and reject invalid input before model mutation.

**32. [P3] Saving and loading use different filename rules.**

[SavePanel.cs:72](C:/Users/Acer/ChipChallengers/Assets/Ui/Menus/ChipEditor/SavePanel.cs:72) accepts a typed extension through `EnsureFileExtension`, while [LoadPanel.cs:98](C:/Users/Acer/ChipChallengers/Assets/Ui/Menus/ChipEditor/LoadPanel.cs:98) always appends `.txt`. Saving a typed `car.txt` remembers that full name, and reopening it can request `car.txt.txt`. Saving another extension also creates a file excluded from the normal model list. Normalize supported names/extensions in one place, reject invalid/path-escaping names, and update the selected model name only after a successful operation.

## Onboarding and visual design

The missing editor tutorial matters because the editor requires understanding a dependency chain: attachment/orientation → chip type/option → variable bounds/default/backstep → binding a property → Lua controls → testing → saving. F1 currently lists shortcuts; it does not teach that chain. I would make the first editor session build a tiny working vehicle through actions: attach a chip, select it, add a drive component, bind one variable, create a control, test, return, and save. Each step should recognize completion, point to the actual control, and provide a way to recover from mistakes.

For menu improvements, these are source-based recommendations, not claims from a visual playtest:

- Give Build, Drive, Challenges, and Learn clear hierarchy and action labels. The current main menu gives seven actions similar weight.
- In the challenge browser, separate the display title, best time, vehicle, and chip count. [ScenarioMenu.cs:138](C:/Users/Acer/ChipChallengers/Assets/Ui/Menus/Scenario/ScenarioMenu.cs:138) combines everything into one filename-led line, which will become crowded. Parse/display descriptive metadata safely and show a persistent selection highlight.
- Keep the selected chip/type and the active editor tab obvious. Add contextual descriptions and units for Spring, Damper, Value, Brake, and Backstep. Show an option's descriptive name in the field, alongside any index needed internally.
- Explain the required physics settings before a challenge starts. Settings are silently replaced by challenge rules, and forbidden chips are only reported when starting the attempt.
- Treat readable contrast, spacing, type scale, and predictable dialog behavior as higher priority than more blur effects. [TopProp.cs:61](C:/Users/Acer/ChipChallengers/Assets/Ui/BaseProps/TopProp.cs:61) captures screen dimensions at construction and [UIUtils.cs:14](C:/Users/Acer/ChipChallengers/Assets/Ui/UIUtils.cs:14) derives font sizes from raw screen dimensions. Add resize/layout handling and test small windows, 16:9, ultrawide, and high-DPI displays.
- Route typing, camera shortcuts, and vehicle controls through an explicit focus policy. Vehicle Lua being active in the editor appears intentional from `TODOS.md`; raw key/mouse polling can still make typing or clicking UI trigger vehicle actions. Test that distinction rather than disabling all editor scripting.
- Correct misleading labels: the sixth sensor option is named **Compass** in `optionNames.lua`, but the corresponding `SensorType` and implementation return **velocity**. Clarify whether Rotation intentionally means a forward-direction vector, which cannot express roll.
- Use visible progress/errors for connecting, loading maps, rebuilding, and saving. Catching an exception and closing a dialog leaves the player unsure whether their work survived.

## Code structure and test priorities

The challenge metadata/store/physics-dispatcher code has useful validation, snapshots, exception isolation, atomic replacement, and focused tests. Those patterns are worth extending to vehicle editing and runtime assembly.

The largest structural concern is that `CoreChip`, `CommonChip`, and `VModel` mix editable data, validation, serialization, networking, reconstruction, input, and runtime state. A small property edit can rebuild every chip and all its aspects. Model names, variables, histories, and core objects also have separate server/client copies whose ownership is not consistently expressed. Introduce a clear validated model boundary and a serialized rebuild operation before considering a broad rewrite.

Other cleanup worth doing after the functional defects:

- Make menu entry/exit idempotent. [BaseMenu.cs:94](C:/Users/Acer/ChipChallengers/Assets/Ui/BaseProps/BaseMenu.cs:94) invokes selection/deselection callbacks even when the selected state has not changed; inactive menus can run teardown, backup, or global-state side effects on unrelated navigation.
- Give callbacks/events and allocated Unity objects explicit owners and teardown. Replace growing arrays of delegates and mutable public backing arrays where an event or collection with a clear lifecycle would be simpler.
- Replace parallel property arrays/string identifiers with typed definitions incrementally, starting at validation and serialization boundaries.
- Remove obsolete commented implementations and recurring runtime TODO logs after documenting remaining work. They obscure which execution paths are active.
- Mark/remove unfinished unused helpers. For example, `AudioManager.Instance` never assigns its static instance, and `ItemBase.Next` loops over this object's child count while indexing its parent's children. These are defects in helpers, but I did not find active game call sites for them, so they are not ranked as primary gameplay failures.
- Fix the standalone README's command: it points to `Tests/ChallengeRecording/...`; the actual project is `Tests/ScenarioRecording/ScenarioRecording.Tests.csproj`.

Highest-value additional checks are a real player build; model round trips across locales and arbitrary valid Lua strings; Lua comparison/comment/string/scope handling; script-error recovery with multiple sensors; undo/redo branches on host and remote client; repeated retry/load resource counts; and multiplayer joins, disconnects, map changes, and different client resolutions. Physics play tests should cover accelerometers, detached powered parts, projectile/recoil consistency, and wheels on dynamic platforms.

Fix build and save integrity first, then scripting and multiplayer state, then mechanics consistency and resource lifetime. Build the editor tutorial around the resulting stable workflow, and finish with visual polish.
