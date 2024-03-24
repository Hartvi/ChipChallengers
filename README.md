# ChipChallengers
ChipChallengers - physics chips and programming all in one
# Website
https://lotramus.itch.io/chip-challengers

![alt text](https://img.itch.zone/aW1hZ2UvMjQ0MzQ5My8xNDQ3NTM1MC5wbmc=/347x500/hVpN0B.png "editor") ![alt text](https://img.itch.zone/aW1hZ2UvMjQ0MzQ5My8xNDQ3NTMzNi5wbmc=/347x500/Oo7eQt.png "plane") ![alt text](https://img.itch.zone/aW1hZ2UvMjQ0MzQ5My8xNDQ3NTM5MS5wbmc=/347x500/9v%2Ftpf.png "lua") ![alt text](https://img.itch.zone/aW1hZ2UvMjQ0MzQ5My8xNDQ3NTM3MC5wbmc=/347x500/gybjsd.png "weird_car")


### TODO
- `+` fix lua throwing an exception when the code is incomplete
  - `+` autofill available functions??
  - `+` provide documentation for available lua functions
- `++` add water
- `++` add dropdowns for `Option`, `Type` and other fields as necessary
- `+` Create 3 example maps
- `+` settings menu
  - volume, number of particles
  - maybe do sliders
- `+` add particles
  - jet smoke
  - wheel dust
  - gun
- `+` add sounds
- `+` options menu
  - master sound level, sound effect level, music level
  - particle effects - 0-100 %
  - physics iteration speed
- `+++` implement multiplayer
- `+` game modes: pool, snake

- `++` add dropdowns for `Option`, `Type` and other fields as necessary
- `+` add backgrounds to all menus:
  - settings menu, load panel, map panel, save panel
- `+` intro tutorial
 - create a nice controls menu
 - show introductory screenshots(?) that depict how to find out the controls for a given model
- `+` rotate the camera around the currently selected object or add an option for it
- `+` fix the gun having different recoil values at different times in the game
- `+` find out why the physics timing makes wheels speed up cars differently 

### Done
- `+++` add sensors
- `+` when deleting characters in input boxes, ignore all other input listeners
  - already began in chip panel
- `+` disable adding other chips to cowls
- `+` remove cowl collider
- `+` stop the camera shaking - e.g. when flying the plane the camera shakes back and forth every frame
- `+` make the camera follow the player also in direction of looking, so when player goes down, camera also looks down
- `+` singleplayer - text at the top to show F1 and ESC
- `+` freeze the model when not in singleplayer mode
- `+` stop listening to LUA controls when not in singleplayer mode
- `+` TODO fix crash when I change Core type to something other
  - Core type MUST NOT BE CHANGEABLE
- `+` when freshly downloading the game, then check if the settings are at default values, otherwise set them to default
- `+` move the LUA listener to the singleplayer InputHandler
- `+` Create nice example models
- `+++` fix build bugs in how it doesn't show runtime variables and switching between editor and playmode doesn't work
- `+` fix camera
- `+` add F1 controls
- `+` hide empty variable in singleplayer and editor
- `+` hide extensions in all panel loaders
- `+` hide `.mtl` files in map loader
- `+` Basic linking and setting of setting values
- `+` make resetting to default location a function and implement in CommonChip as well
- `+` LoadPanel - make the scrolling not disappear the items wtffffff
- `++` implement token passing to register `esc` presses in load menus, so it closes them and doesn't go to the main menu
- `+` when loading a new map, place the model at the default location
- `+` remove position from HUD
- `+` when loading a new model, place the camera close to the model
- `+++` add OBJ map loading
  - **menu**
  - assimp library
- `+` Clean up bullet objects after gun is destroyed
- `++` reduce physics frequency when number of objects increases
- `+` test whether gun actually removes variable value changed callbacks
- `+` implement gun
  - add callback in SleepyTime/Die to remove dead chips from variableChangedCallbacks
- `+++` fix guns not triggering 'ApplyDamage' on chips
- `+` implement wheel brakes
- `+++` split HUD into:
  - left side: display currently variable values
    - hide the place-holder variable
  - right side: display velocity and position
- `++` HUD in playmode displaying current variable values
- `++` get rid of `instanceProperties` in `VChip`
- `+` Fixed changing chip type to display right away
- `+` copy paste in editor - add the option to paste mirror-wise `ctrl+shift+v`
- `+` fix cosmetics - explicitly label which objects are targets of colouring through *layers* or *object names* or otherwise
- `+++` fix bug in `EditorMenu`; line: 111 
  - Solution: never set the selected chip to null. ChipPanel was still visible and it wanted to edit the values of the currently selected chip, but since I clicked away and the panel remained active, it meant I had callbacks pointing to a null trying to modify it as a regular chip
- `+` test jet 
- `+` orbiting camera mode in editor
- `+` ctrl+v in editor
- `++` createchippanel must be completely on screen when activated
- `+++` LUA should only show error on screen when it encounters an error in the editor and playmode. Right now it throws an exception that stops the whole program
