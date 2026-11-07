# Scenario recording checks

Wheel timing checks also integrate driven and coasting spin at 500 Hz, 1000 Hz and across a runtime rate switch, verify braking does not reverse spin, and compare a full visual revolution at 30/60/120 FPS. Ground contacts, axle-turning reactions and full vehicle handling require Unity Play-mode testing.

Requires .NET 9 and the project's Unity-generated `Library/ScriptAssemblies/Assembly-CSharp-firstpass.dll` plus restored Newtonsoft package.

Run from the repository root:

```powershell
dotnet run --project Tests/ScenarioRecording/ScenarioRecording.Tests.csproj
```

Exercises the actual metadata parser and file store with temporary saves. Checks duplicate finishes, best/last vehicle retention, timing precision, nested snapshots, atomic replacement, corrupt-save preservation, untimed results, record/processor version separation, required physics parsing, preset defaults, physics snapshot copying, and rejection/separation of incompatible physics results. Unity gate triggering and UI still require Play-mode verification.

Race checks also exercise the named and original Lua forms, callback arguments, ordered one-shot progress, and both bundled time-trial scripts with scene operations stubbed out.

Content identity checks cover Lua/map hash sensitivity, filename changes without splitting identical contents, separate results for changed Lua, per-run filenames, required identity fields and preservation of old ID-based saves during format upgrades.

Gameplay context checks cover regular/scenario selection, repeated reads across editor returns, replacement selections, explicit clearing, and rejection of stale queued loads.

Physics callback checks cover before/write/after ordering, unchanged-setting suppression, snapshot isolation, target/reason propagation, restoration, unsubscribe, validation, exception isolation, reentrancy prevention, and invalidation before writing.

Race-start timing checks cover the full one-second settling hold, 3-2-1 boundaries, GO release, and post-countdown values. Rigidbody freezing and countdown cancellation still require Play-mode verification.
