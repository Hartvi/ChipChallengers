# Vehicle runtime regressions

From the repository root, after Unity has imported the project:

```powershell
dotnet run --project Tests/VehicleRuntime/VehicleRuntime.Tests.csproj
```

These checks compile production number/string serialization, literal model parsing, and Lua execution/environment code. Only VVar's unused VModel owner reference is stubbed. They do not simulate Unity physics or Mirror.

Vehicle and challenge Lua callbacks have an instruction budget and a 65,536-character source limit. Dynamic code loading and user-created coroutines are unavailable. Vehicle files contain literal tables rather than executable Lua and are limited to 1 MiB. Existing literal-table vehicle files remain supported.

Vehicle `Loop()` and variable backstep run at 50 Hz, retaining the original default tuning. Lua's `DeltaTime` contains the control tick duration (0.02 seconds). Rendering and physics-rate changes do not change this rate; input press/release edges are consumed once by the next control tick.

Gun projectiles retain the original impact strength at the standard power: power 100 produces a 1 kg projectile at 333 m/s. Mass scales with power, and the gun's recoil is attenuated to 1% for gameplay. The runtime checks pin both the standard impact mass and the existing recoil scale; native BulletTests also launch a rolling challenge ball at 500 and 1000 Hz without overriding bullet mass.

Model names accept an optional final `.txt` suffix. Other dots are part of the vehicle name, so `car.v2` saves as `car.v2.txt` and reloads by either form. Paths, control characters and reserved device names are rejected. User files live in `Application.persistentDataPath/Models`; legacy installed files are copied once without replacing existing user work.
