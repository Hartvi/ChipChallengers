# Industrial test track

Load `industrial_test_track` from the in-game map panel. This is a free-driving map, not a scripted race scenario; it does not add gates, rules or saved results.

The circuit has a 12 m carriageway, broad runoff, a start straight at world origin, flowing bends, a raised section and simple industrial landmarks. Road markings are material assignments on the driving surface, so they do not add small collision bumps.

## Generate and render

Run from the repository root with Blender 3.6:

```powershell
& 'C:/Program Files/Blender Foundation/Blender 3.6/blender.exe' --background --factory-startup --python Tools/MapAuthoring/build_industrial_test_track.py
```

The script writes `industrial_test_track.obj` and `.mtl` into `Assets/StreamingAssets/Maps`. The editable `.blend`, three rendered views and a mesh summary go to `C:/Users/Acer/BlenderTests/industrial-test-track` (change `OUT` in the script to use another location). It checks for folded road/runoff faces before export. Earlier numbered previews document the render/review iterations.

Blender uses Z up; OBJ export uses Y up for the game. The existing OBJ importer also mirrors X, so the circuit appears mirrored relative to the Blender previews. Lighting and cameras are preview-only and are not exported. No external textures are required.

The geometry has been rendered and inspected, but full vehicle handling, runtime materials and collision behavior still need an in-game driving test.
