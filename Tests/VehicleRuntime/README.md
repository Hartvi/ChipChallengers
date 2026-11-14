# Vehicle runtime regressions

From the repository root, after Unity has imported the project:

```powershell
dotnet run --project Tests/VehicleRuntime/VehicleRuntime.Tests.csproj
```

These checks compile production number/string serialization, literal model parsing, and Lua execution/environment code. Only VVar's unused VModel owner reference is stubbed. They do not simulate Unity physics or Mirror.

Vehicle and challenge Lua callbacks have an instruction budget and a 65,536-character source limit. Dynamic code loading and user-created coroutines are unavailable. Vehicle files contain literal tables rather than executable Lua and are limited to 1 MiB. Existing literal-table vehicle files remain supported.
