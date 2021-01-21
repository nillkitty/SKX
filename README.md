# **Thank You Dana!  (Overview)**
***Note**:  If you're looking for an open source remake of the **arcade** version of Solomon's Key, see the [Open Solomon's Key](https://github.com/mdodis/OpenSolomonsKey) project.*

SKX is an open source port (with enhancements) of the 1986 Tecmo game Solomon's Key for the NES.  Graphics and sound effects are borrowed from the original,  however the code is completely fresh.   SKX is written in C# for .NET Core using the MonoGame (formerly XNA) framework for input, audio, and graphics and runs on Windows, Mac, and Linux.

Check out the [Changelog](https://github.com/nillkitty/SKX/wiki/Changelog) or the [Wiki](https://github.com/nillkitty/SKX/wiki/) for additional information about the current status of the beta builds.  The full source code and downloads will be available on this repo as soon as all major milestones are complete (1Q2021 estimated).

**Features**
* Keyboard or gamepad input
* Automatic save feature using one of 8 save slots
* Easy, normal, and difficult modes
* Various levels of scale
* Multi-track dynamic music
* Fullscreen mode
* Classic mode -- the original 52 levels from the NES
* Plus mode -- the original 52 levels with added items and 200 added levels (and Dana gets an apprentice)
* New gameplay elements (see **New Gameplay Elements** below)
* Integrated level editor

# **Usage**
SKX should run on any system that supports .NET core.  Self-contained Windows, MacOS, and Linux executables are available, as well as the .NET core version (.dll) which can be run from any platform using the command `dotnet SKX` from within the unzipped directory.

Key/button bindings can be modified in the menu,  but the game comes with two sets of default controls for ease of use:

# **Known Issues/Missing Functionality**
* Enemy UI needs some minor tweaking to be accurate to the original
* Dana's collision with the level might need some review, especially when jumping diagnally at stair-shaped structures
* Fairy UI is smoother but less accurate
* Ending sequence not yet implemented
* Dana's crouch-walking animation is not yet implemented

# **A Note on Accuracy**
The goal of the project is to use NES-accurate physics and enemy behavior.  Any help correcting parameters to make the game behave more accurately is appreciated.  Most classes contain a section of (usually private) variables labelled "Behavioral Parameters" that can be adjusted to tweak the gameplay of those elements.

The mere act of implicitly removing technical limitations imposed by the NES changes the nature of the game.  Many rooms (Room 7, for example) have slightly different enemy timimg as a result of the mere removal of lag and sprite limits.  

# New Gameplay Elements
* New items
* New enemies
* New gameplay mechanics

## **Game Difficulty**
| Difficulty | Starting Lives | Scroll Size | Fireball Range
|--|--|--|--|
| Normal | 3 | 3 | 16 half-blocks
| Easy | 5 | 5 | 20 half-blocks
| Difficult | 3 | 1 | 16 half-blocks

## **Game Modes**
| Mode | Description |
|--|--|
| Classic | All 52 original levels with all of the original gameplay logic |
| Classic+ | A massive expansion of the original story;  256 levels with new items and new enemies, incorporating the original 52 levels with updates, added secrets, and interesting new gameplay mechanics.

## **Debug Mode and Level Editor**
The game features a fully featured level editor and diagnostics modes.  See the [Wiki](https://github.com/nillkitty/SKX/wiki) for more information.

## **Miscellaneous Engine Improvements (over the NES code)**
* The engine supports rooms larger than a single screen.   The camera will pan to follow Dana as he gets near the edge of the viewport if the room is larger than the current view.
* Rooms can have multiple doors (leading to different rooms or to the same room)
* Rooms can have multiple keys
* Keys can open multiple doors (if applicable)
* Rooms may contain more than two mirrors that spawn enemies
* Dana's starting position in a room may be influenced by which exit he used in the previous room.
* Rooms wrap vertically and horizontally -- just remove the walls.  (technically possible on the NES but not used in any actual rooms)
* Dynamic Audio -- Each room can have a different "Audio Effect" which manipulates the pan and fade of each of the tracks in the multitrack music as a function of dana's position, life remaining, etc.
* In game modes other than Classic, Dana can grow his scroll up to 18 slots (instead of 8).
* Pixel shader backgrounds
* Full aetheitic control over the background layer and the super foreground layer which are independent from the normal foreground layer Dana stands on
* Demo recording and playback support
* Unlimited spawn points and spawn types per room
* Special behaviors in certain rooms (17, 39, 20, 44, 49-53) which were hard-coded in the NES version have all been ported to "Magic Spells" -- lightweight editable scripts that are contained in the level data as objects
* 
