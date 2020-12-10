# **Thank You Dana!  (Overview)**
***Note**:  If you're looking for an open source remake of the **arcade** version of Solomon's Key, see the [Open Solomon's Key](https://github.com/mdodis/OpenSolomonsKey) project.*

SKX is an open source port (with enhancements) of the 1986 Tecmo game Solomon's Key for the NES.  Graphics and sound effects are borrowed from the original,  however the code is completely fresh.   SKX is written in C# for .NET Core using the MonoGame (formerly XNA) framework for input, audio, and graphics and runs on Windows, Mac, and Linux.

**Features**
* Keyboard or gamepad input
* Automatic save feature using one of 8 save slots
* Easy, normal, and difficult modes
* Various levels of scale
* Multi-track dynamic music
* Fullscreen mode
* Classic mode -- the original 52 levels from the NES
* Classic+ mode -- the original 52 levels with added items and 20 new secret levels
* SKX mode -- a new set of 60 levels specifically designed with the new gameplay elements in mind
* New gameplay elements (see **New Gameplay Elements** below)
* Debug mode
* Integrated level editor

# **Usage**
SKX should run on any system that supports .NET core,  simply download the binary version of the game,  and either run **SKX.exe** (Windows), or run the command `dotnet skx.dll` from the appropriate directory.

Key/button bindings can be modified in the menu,  but the game comes with two sets of default controls for ease of use:

## **Default Controls (Arrows)**
| Binding | Keyboard | Gamepad |
|--|--|--|
| Up | Up Arrow | D-Pad Up
| Down | Down Arrow | D-Pad Down
| Left | Left Arrow | D-Pad Left
| Right | Right Arrow | D-Pad Right
| Magic | S | A button 
| Fireball | A | X button 
| Pause | Enter | Start button

## **Alternate Controls (WASD)**
| Binding | Keyboard | Gamepad |
|--|--|--|
| Up | W | D-Pad Up
| Down | S | D-Pad Down
| Left | A | D-Pad Left
| Right | D | D-Pad Right
| Magic | Right Shift | A button 
| Fireball | / | X button 
| Pause | Enter | Start button

# **Known Issues/Missing Functionality**

* Enemy UI needs some minor tweaking to be accurate to the original
* Dana's collision with the level might need some review
* Fairy UI is smoother but less accurate

# **A Note on Accuracy**
The goal of the project is to use NES-accurate physics and enemy behavior.  Any help correcting parameters to make the game behave more accurately is appreciated.  Most classes contain a section of (usually private) variables labelled "Behavioral Parameters" that can be adjusted to tweak the gameplay of those elements.

The mere act of implicitly removing technical limitations imposed by the NES changes the nature of the game.  Many rooms have slightly different enemy timimg as a result of the mere removal of lag and sprite limits.  

# **New Gameplay Elements**
## **New block types**
* **Frozen blocks** are items or empty space trapped in ice.  The ice can be melted by coming in contact with a fireball (either Dana's or an enemy's) or a Burns (flame enemy), or by picking up the **Blue Jar** which immediately melts all frozen blocks in the room.   Dana can stand on frozen blocks but cannot break them using magic.

* **Blue Doors** take Dana to a different part of the same room.   They must be opened first by finding a key.

## **New items**
| Item | Appearance | Reward |  
|:-----------:|:-----------:|-----------|  
| Super Medicine of Mapros | Blue bottle with an "E" on it | Rewards Dana with a 5-up. |  
| Copper Jewel | Red jewel | 5,100 points
| Opal Jewel | Rainbow jewel | 9,900 points
| Red Loot Block | Red diamond | 10,000 points,  or cast magic on it to switch between a Red Loot Block, Copper Jewel, Red Crystal of Rad, or Red Spell Jar |
| Red Spell Jar | Red-colored Jar of Magdora | Adds a death spell to Dana's scroll.  When used (using the Fireball button),  behaves identically to collecting a Red Medicine of Meltona |
| Red Crystal of Rad | Red and white crystal | Normally Dana can only shoot a fireball once all previous fireballs have been extinguished.  The Red Crystal of Rad increases Dana's capability to create simultaneously concurrent fireballs by one. |
| Red Medicine of Meltona | Jar filled with red liquid | Like the Orange Jar of Meltona (which kills Demonheads and Salamanders) but also kills Ghosts, Nuels, and Wyverns.
| Green Medicine of Meltona | Jar filled with green liquid | Like the Red Medicine of Meltona,  but also kills all Goblins, Wizards, and Panel Monsters
| Black Medicine of Meltona | Jar filled with black liquid | Quite rare-- immediately kills all enemies in the room
| Magdora Cocktail | Half-blue and half-orange Jar of Magdora | **Combination Item**: Adds both a super fireball and a regular fireball to Dana's scroll at the same time. |
| Time and Money | White Jar with Gold Jewel | **Combination Item**:  Same as collecting a full bottle of life (Medicine of Edlem) as well as a gold jewel at the same time.
| Scroll and Spell | Scroll and Blue Jar | **Combination Item**: Same as collecting a scroll and a fireball (Blue Jar of Magdora) at the same time. |
| Blue Medicine of Meltona | Jar filled with blue liquid | Immediately melts all ice/frozen blocks in the room 
| Hourglass of Peace | Purple/Pink Hourglass | Destroys all enemy-spawning mirrors
| Question Potion | Potion with a '?' | Executes room-specific functionality -- or maybe even nothing -- it's a mystery

## **New enemies**
* All enemies now come in four different speeds (Slow, Normal, Fast, and Faster).  The original had between 1-3 speeds depending on enemy type.
* Panel Monsters have a new variant (set the `Clockwise` flag), which breathe fire (like Dragons and Salamanders) when Dana or a brick is directly in front of them, instead of shooting fireballs periodically.

## **Miscellaneous Improvements (over the NES code)**
* The engine supports rooms larger than a single screen.   The camera will pan to follow Dana as he gets near the edge of the viewport if the room is larger than the current view.
* Rooms can have multiple doors (leading to different rooms or to the same room)
* Rooms can have multiple keys
* Keys can open multiple doors (if applicable)
* Rooms may contain more than two mirrors that spawn enemies
* Dana's starting position in a room may be influenced by which exit he used in the previous room.
* Rooms wrap vertically and horizontally -- just remove the walls.  (technically possible on the NES but not used in any actual rooms)
* Dynamic Audio -- Each room can have a different "Audio Effect" which manipulates the pan and fade of each of the tracks in the multitrack music as a function of dana's position, life remaining, etc.
* In game modes other than Classic, Dana can grow his scroll up to 18 slots (instead of 8).


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
| Classic+ | All 52 original levels with new item types added, and 20 additional secret levels
| SKX | 60 new levels making use of all of the new gameplay elements

## **Debug Mode and Level Editor**
The game features a fully featured level editor and diagnostics modes.  See the EditorReadMe.md for full documentation. 
