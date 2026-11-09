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

## Karst Coast Flight

Select `karst_flight_race` in Scenarios, or load `karst_coast` from the map panel for free flight. Bring a model capable of taking off; the scenario does not restrict chip types or automatically choose a plane.

The airfield has a 36 m runway, hangars beside the start, and two orange start pillars. The countdown trigger sits between the pillars. After GO, take off and follow 13 airborne checkpoints, then return through the finish hoop above the airfield. The widened revision uses a 1.5x landscape/course scale: airborne hoops have a 36 m clear opening and the existing spherical race triggers are also 36 m across. Hangars, the operations hut, their doors and roofs retain their original dimensions. Triggers detect entry into the sphere; this uses the existing race API, not a new directional hoop-crossing test.

Twenty-one main limestone-style sea cliffs, smaller outcrops and vegetated summits surround the course. Their horizontal footprints are checked against the flight corridors, and every straight flight leg is raycast against the actual generated meshes. The blue sea surface has been removed completely, including its collider; the cliffs now stand in open space. This map does not introduce water dynamics or crash rules.

Run `build_karst_flight.py` using the Blender command above to generate `karst_coast.obj`, `.mtl`, and `karst_flight_race.lua`. It saves `karst_coast.blend`, `karst_flight_preview.blend`, the numbered route overview, runway/flight renders and `summary.json` to `C:/Users/Acer/BlenderTests/karst-flight`. Preview route lines and labels are excluded from the gameplay OBJ. The scenario uses 1000 Hz physics, 1 regular solver iteration and 3 velocity solver iterations. In-game takeoff, aircraft clearance and trigger behavior still need flight testing.
