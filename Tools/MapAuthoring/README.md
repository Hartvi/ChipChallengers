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

## Race scenario

Select `industrial_test_track_race` in Scenarios to drive one lap with 13 ordered checkpoints. The vehicle spawns behind the start gate facing down the straight. Entering the start gate runs the existing hold/countdown; finishing the lap records the result using the existing persistence flow. The scenario requires 1000 Hz physics with 1 regular and 3 velocity solver iterations.

After generating the map, run `build_industrial_race.py` with the same Blender command to regenerate the Lua and numbered gate previews from the saved road centreline. It writes `race_overview.png`, `race_layout.png`, `race_start_straight.png` and `industrial_race_preview.blend` beside the map renders. Gate spheres are preview-only and never added to the OBJ. Rebuild the race after changing the map geometry.
