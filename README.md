# ChipChallengers
ChipChallengers - physics chips and programming all in one

### TODO
- `+` implement wheel brakes
- `+` implement gun
- `+++` add OBJ map loading
  - menu
  - assimp library
- `+` add particle effects
  - jet smoke
  - wheel dust
  - gun
- `+` add sounds
- `+` options menu
  - master sound level, sound effect level, music level
  - particle effects - 0-100 %
  - physics iteration speed
- `+` rotate the camera around the currently selected object or add an option for it
- `+++` implement multiplayer
- `+` Add option to change units in HUD

### Done
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
