# Isolated Unity and multiplayer validation

Keep these two C# helpers outside `Assets` in the ordinary project. They were used with Unity 2021.3.16f1 to validate the review fixes in a separate worktree, without interrupting an open editor.

Create a detached validation checkout and install the helpers into that copy only:

```powershell
$reviewRoot = Join-Path $env:TEMP ("ChipChallengersReview-" + [guid]::NewGuid().ToString("N"))
git worktree add --detach $reviewRoot HEAD
New-Item -ItemType Directory -Path "$reviewRoot/Assets/Editor" -Force
Copy-Item Tests/UnityValidation/ReviewValidationBuild.cs "$reviewRoot/Assets/Editor/"
Copy-Item Tests/UnityValidation/ReviewSmokeProbe.cs "$reviewRoot/Assets/"
$unity = "C:/Program Files/Unity/Hub/Editor/2021.3.16f1/Editor/Unity.exe"
```

The installed editor must match the project version. The copy needs its packages resolved; copying the ordinary project's `Library/PackageCache` to the equivalent path in the copy can avoid downloading them again.

Run the native tests, allowing Unity to finish before starting the build:

```powershell
$testArgs = @('-batchmode', '-nographics', '-projectPath', ('"{0}"' -f $reviewRoot),
    '-runTests', '-testPlatform', 'EditMode', '-testResults', ('"{0}/tests.xml"' -f $reviewRoot),
    '-logFile', ('"{0}/tests.log"' -f $reviewRoot))
$testProcess = Start-Process -FilePath $unity -ArgumentList $testArgs -WindowStyle Hidden -Wait -PassThru
if ($testProcess.ExitCode -ne 0) { throw "Native tests failed; inspect tests.xml and tests.log" }
$buildArgs = @('-batchmode', '-nographics', '-projectPath', ('"{0}"' -f $reviewRoot),
    '-executeMethod', 'ReviewValidationBuild.Run', '-logFile', ('"{0}/build.log"' -f $reviewRoot))
$buildProcess = Start-Process -FilePath $unity -ArgumentList $buildArgs -WindowStyle Hidden -Wait -PassThru
if ($buildProcess.ExitCode -ne 0) { throw "Player build failed; inspect build.log" }
```

`ReviewValidationBuild.Run` produces `Builds/Validation/ChipChallengers.exe` in the copy. It changes only the copy's company/product name to isolate test preferences, model migration and saved work from normal player data.

Use a new empty results directory for each multiplayer run. Launch the host with these arguments, substituting absolute paths:

```text
-batchmode -nographics -noaudio -logFile <results>/host.log --review-smoke host <results>
```

After `<results>/host.ready` appears, launch two more copies with roles `client1` and `client2` and corresponding log files. Each process quits after its checks, or fails after 150 seconds. All three `<role>.result.txt` files must begin with `PASS:`. The test uses local port **17991** and overrides headless auto-start before opening the session. Quote filesystem arguments containing spaces. When launching background processes from PowerShell, use `Start-Process -WindowStyle Hidden`.

The probe checks host map synchronization, remote undo/redo, stable AudioSource/aspect counts through three resets per client, rejection of multiplayer challenges, and keeping the host/remaining client in sandbox after the other client disconnects. It waits for deferred Unity destruction and new aspect initialization before recording resource counts. It does not assess graphics/audio output or internet latency; null-graphics shader warnings are expected.

Initial review validation: **31/31 native tests**, successful Windows x64 build and **3/3 multiplayer probe results**. The bullet knockback follow-up passed **33/33 native tests**, including normal shots launching a rolling challenge ball at 500 and 1000 Hz. The variable-panel Enter follow-up passed **38/38 native tests**, including add/update submission from all five fields. The draft, sensor-paste, option-preservation and bundled-example redo passed **51/51 native tests**. Standalone suites are documented in the adjacent VehicleRuntime and ScenarioRecording directories.
