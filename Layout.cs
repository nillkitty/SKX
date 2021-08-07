using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Text;

namespace SKX
{
    /// <summary>
    /// Represents a room's layout.   Loaded from disk or from an embedded asset
    /// at level start and then used to track the state of the room throughout gameplay.
    /// </summary>
    public class Layout : BundleItem
    {

        /* References -- these don't go into a room file */
        [JsonIgnore]
        public World World { get; set; }                    // Reference to the world that owns the layout
        [JsonIgnore]
        public Level Level => (Level)World;                 // For quick access

        /* Everything from here on goes in the JSON file */
        [JsonProperty]
        private Cell[] cells;       // All of the cells that make up the playfield

        [JsonProperty]
        public Tile[] Background;   // All of the tiles that make up the background

        [JsonProperty]
        public Tile[] SuperForeground;   // All of the tiles that make up the super foreground (optional)


        public string Name { get; set; }                    // Alternate room name

        public int Width { get; set; }                      // Width of the room in cells
        public int Height { get; set; }                     // Height of the room in cells
        public override int RoomNumber { get; set; }        // Room number -- used to identify the room both in a Bundle
                                                            // but also from within the 
        public override Story Story { get; set; }           // The story this room is intended for.  Used primarily in
                                                            // bundle loading.

        public CharacterMode Character { get; set; }        // If the room forces a specific character or not
        public int? StartLife { get; set; }                 // Starting life counter override
        public int Shrine { get; set; }                     // Shrine number (0 = Aries, etc.)
        public int NextRoom { get; set; }                   // Default next room number
        public int NextRoomWing { get; set; }               // Default next room when Dana has the golden wing
        public int NextRoomSecret { get; set; }             // Default next room through a "dark" door
        public Tile BackgroundTile { get; set; }            // Default background tile
        public bool DefaultBorders { get; set; } = true;    // Whether or not to render standard border blocks
        public Point CameraStart { get; set; } = new Point(8, 16);  // Where the camera should start
        public Point DanaStart { get; set; } = new Point(8, 6);      // Where Dana should start
        public bool DanaDirection { get; set; } = false;           // Whether Dana starts facing left or not
        public Point AdamStart { get; set; } = new Point(8, 6);      // Where Dana should start
        public bool AdamDirection { get; set; } = false;           // Whether Dana starts facing left or not

        public bool UseSFForBorders { get; set; } = false;          // Whether or not the SuperForeground should be 
                                                                    // used in Title Card and intro as part of the 
                                                                    // room borders

        public Point StartPos {
            set { DanaStart = value; AdamStart = value; }
        }
        public bool StartDirection => Game.Sesh.Apprentice ? AdamDirection : DanaDirection;
        public List<KeyInfo> Keys { get; set; } = new List<KeyInfo>();  // Extended key info
        public List<DoorInfo> Doors { get; set; } = new List<DoorInfo>();   // Extended door info
        public List<Spell> Spells { get; set; } = new List<Spell>();        // Spell scripts
        public Rectangle CameraBounds { get; set; } = new Rectangle(8, 16, 256 + 8, 216);   // Where the camera can go
        public List<Cell> RandomList { get; set; } = new List<Cell>();
        public int Music { get; set; } = 0;                                 // Music selection
        public List<Resize> Resizes { get; set; } = new List<Resize>();     // Dynamic camera
        public AudioEffect AudioEffect { get; set; } = AudioEffect.Normal;    // Dynamic audio
        public CameraMode CameraMode { get; set; }                          // Initial camera mode
        public List<ObjectPlacement> Objects { get; set; } 
            = new List<ObjectPlacement>();                                  // Initial object placements
        public List<Spawn> Spawns { get; set; } = new List<Spawn>();        // Spawn points
        public Color BackgroundColor { get; set; }                          // Room's background color
        public Color SuperForegroundColor { get; set; } = Color.White;      // Super foreground color

        public float SuperForegroundOpacity = 1.0f;                         // Super foreground opacity
        /* Alternate thank you text */
        public string ThankYouTextA { get; set; }
        public string ThankYouTextB { get; set; }
        public string ThankYouTextC { get; set; }

        /* End of JSON-serialized properties */

        /* Used for rendering */
        private static Color Translucent = Color.White * 0.66f;             
        private static Color MoreTranslucent = Color.White * 0.33f;
        private List<Cell> WorkingRandomList;



        /* Constructor */
        public Layout(World world, int w, int h, bool fg = false)
        {
            World = world;
            Width = w;
            Height = h;
            cells = new Cell[w * h];
            Background = new Tile[w * h];
            if (fg) SuperForeground = new Tile[w * h];
        }

        /// <summary>
        /// Finds all doors that a given key should open (visible or hidden,  open or closed)
        /// </summary>
        public IEnumerable<Point> FindDoorsForKey(Point key)
        {
            var l = new List<Point>();
            var keys = Keys.Where(x => x.KeyPosition == key);
            foreach (var k in keys)
            {
                if (k.Valid)
                {
                    Cell d = this[k.DoorPosition];
                    if (IsDoorAtAll(d))
                    {
                        l.Add(k.DoorPosition);
                    }
                }
            }
            if (l.Count == 0)
                return FindDoors(false, true, true);
            else
                return l;
        }

        /// <summary>
        /// Changes the item in a given cell without changing the modifier
        /// </summary>
        public static Cell ChangeItem(Cell input, Cell newItem)
        {
            return input.GetModifier() | newItem.GetContents();
        }

        /// <summary>
        /// Saves the layout to the default file name for this story
        /// </summary>
        public void SaveToFile(char storyID = default)
        {
            if (storyID == default) storyID = Game.Sesh.Story.ToStoryID();
            this.SaveFile($"room_{RoomNumber:X}{storyID}.json");
            Sound.Collect.Play();
            Game.StatusMessage("FILE SAVED.");
        }


        /// <summary>
        /// Tests if a cell contains a shrine (constellation symbol)
        /// </summary>
        public static bool IsShrine(Cell c)
        {
            return c switch
            {
                Cell _ when c >= Cell.ShrineMercury && c <= Cell.ShrineSun => true,
                Cell.ShrineAries => true,
                Cell.ShrineTaurus => true,
                Cell.ShrineGemini => true,
                Cell.ShrineCancer => true,
                Cell.ShrineLeo => true,
                Cell.ShrineVirgo => true,
                Cell.ShrineLibra => true,
                Cell.ShrineScorpio => true,
                Cell.ShrineSagittarius => true,
                Cell.ShrineCapricorn => true,
                Cell.ShrineAquarius => true,
                Cell.ShrinePisces => true,
                Cell.ShrineSolomon => true,
                Cell.ShrineSesquiquadrate => true,
                _ => false,
            };
        }

        /// <summary>
        /// Tests if a cell should be promoted to a reward
        /// (object to which gravity will apply)
        /// </summary>
        public static bool IsReward(Cell c)
        {
            return c switch
            {
                Cell.RwBell => true,
                Cell.RwCrystal => true,
                Cell.RwOneUp => true,
                Cell.RwScroll => true,
                Cell.BagR1 => true,
                Cell.BagR2 => true,
                Cell.BagR5 => true,
                Cell.BagW1 => true,
                Cell.BagW2 => true,
                Cell.BagW5 => true,
                Cell.BagG1 => true,
                Cell.BagG2 => true,
                Cell.BagG5 => true,
                _ => false,
            };
        }

        public void CheckPromote(Point cell)
        {
            var c = this[cell];
            if (IsReward(c))
            {
                var o = new Objects.Reward(Level, c, cell.ToWorld(), true);
                Level.AddObject(o);
                this[cell] = Cell.Empty;
            }
        }

        public void CheckPromote()
        {
            for (int y = 0; y < Height; y++)
                for (int x = 0; x < Width; x++)
                    CheckPromote(new Point(x, y));
        }

        public static bool IsSeal(Cell c)
        {
            return c switch
            {
                Cell _ when c >= Cell.SealJuno && c <= Cell.SealEris => true,
                Cell.SealSedna => true,
                Cell.SealCosmos => true,
                _ => false,
            };
        }

        public static bool IsCartouche(Cell c)
        {
            return c switch
            {
                Cell _ when c >= Cell.Cartouche1 && c <= Cell.Cartouche4 => true,
                Cell.Cartouche6 => true,
                Cell.CartoucheLumania => true,
                _ => false,
            };
        }

        /// <summary>
        /// Tests if a cell contains a door (open or closed, any type, any state)
        /// </summary>
        public static bool IsDoorAtAll(Cell c)
        {
            c = c.GetContents();
            if (c == Cell.DoorClosed) return true;
            if (c == Cell.DoorBlue) return true;
            if (c == Cell.DoorOpenBlue) return true;
            if (c == Cell.DarkDoorClosed) return true;
            if (c == Cell.DoorOpen) return true;
            if (c == Cell.DarkDoorOpen) return true;
            if (c == Cell.InvisibleDoor) return true;
            if (c == Cell.PinkUmbrella) return true;
            if (c == Cell.BlueUmbrella) return true;
            return false;
        }

        /// <summary>
        /// Tests if a cell contains a key (any type, any state)
        /// </summary>
        public static bool IsKeyAtAll(Cell c)
        {
            if (c.GetContents() == Cell.Key) return true;
            return false;
        }

        /// <summary>
        /// Determines if the layout is the normal 17x14 room or not
        /// </summary>
        public bool IsDefaultSize()
        {
            return (Width <= 17 && Height <= 14);
        }

        /// <summary>
        /// Resizes the layout in the fly
        /// </summary>
        public void Resize(int w, int h)
        {
            // Create new arrays for the fg and bg grids
            var newCells = new Cell[w * h];
            var newBackground = new Tile[w * h];
            var newForeground = new Tile[w * h];

            // Copy the cells that make sense to copy
            for (int y = 0; y < Height - 1; y++)
                for (int x = 0; x < Width - 1; x++)
                {
                    int index = y * Width + x;
                    int newIndex = y * w + x;

                    if (index > cells.Length || newIndex > newCells.Length) continue;

                    newCells[newIndex] = cells[index];
                    newBackground[newIndex] = Background[index];
                    if (SuperForeground != null && SuperForeground.Length > index)
                    {
                        newForeground[newIndex] = SuperForeground[index];
                    }

                }
            

            // Swap the arrays
            cells = newCells;
            Background = newBackground;
            if (SuperForeground != null) SuperForeground = newForeground;

            // Update the layout dimensions
            Width = w;
            Height = h;

        }

        /// <summary>
        /// Initializes the random cell list (for hidden rooms, etc.)
        /// </summary>
        void InitRandomCell()
        {
            WorkingRandomList = RandomList.ToList();

            // See if we need to pad the list out by checking if we
            // have more random cells in our layout than we do items
            // in the random list
            var randoms = Spells.Where(x => x.Type == SpellType.RandomCell).Count();
            while (WorkingRandomList.Count < randoms)
            {
                // For every random cell we have oversubscribed,
                // add an empty cell to the random list.
                // This ensures that the random items are evenly distributed
                // otherwise they'd always go to the first N spells to get executed
                // (usually favoring the bottom or top of the level)
                WorkingRandomList.Add(Cell.Empty);
            }

        }

        /// <summary>
        /// Gets the next random cell value and removes it from the working random item
        /// list
        /// </summary>
        public Cell GetRandomCell()
        {
            if (WorkingRandomList is null) InitRandomCell();
            var c = Cell.Empty;

            if (WorkingRandomList.Count == 0) return c;
            if (WorkingRandomList.Count == 1)
            {
                c = WorkingRandomList[0];
                WorkingRandomList.Clear();
                return c;
            }

            var i = Game.Random.Next(0, WorkingRandomList.Count);
            c = WorkingRandomList[i];
            WorkingRandomList.RemoveAt(i);

            return c;
        }

        /// <summary>
        /// Gets the value of a given cell by index
        /// </summary>
        public Cell this[int index] {
            get => cells[index % cells.Length];
            set => cells[index] = value;
        }

        /// <summary>
        /// Gets the value of a given cell by X/Y cell coordinates
        /// </summary>
        public Cell this[int x, int y] {

            get
            {
                y %= Height;
                x %= Width;
                if (x < 0) x += Width;
                if (y < 0) y += Height;
                return cells[y * Width + x];
            }
            set
            {
                y %= Height;
                x %= Width;
                if (x < 0) x += Width;
                if (y < 0) y += Height;
                cells[y * Width + x] = value;
            }
        }

        /// <summary>
        /// Gets the value of a given cell by Point (cell coordinates)
        /// </summary>
        public Cell this[Point p]
        {
            get
            {
                if (p.X < 0) p.X += Width;
                if (p.Y < 0) p.Y += Height;
                p.Y %= Height;
                p.X %= Width;
                return cells[p.Y * Width + p.X];
            }
            set {
                p.Y %= Height;
                p.X %= Width;
                if (p.X < 0) p.X += Width;
                if (p.Y < 0) p.Y += Height;
                cells[p.Y * Width + p.X] = value;
            }
        }

        /// <summary>
        /// Removes a collectible item from a cell without changing the cell state
        /// </summary>
        public static Cell RemoveItem(Cell c)
        {
            Cell d = c.GetModifier();
            if (d == Cell.Covered) return Cell.Dirt;
            if (d == Cell.Hidden) return Cell.Empty;
            if (d == Cell.Cracked) return Cell.BlockCracked;
            return d;
        }

        /// <summary>
        /// Ensures the layout is large enough to fill the size of the Level
        /// </summary>
        public void EnsureSize()
        {
            // Make sure the room is large enough
            if (cells.Length < Width * Height)
            {
                var newCells = new Cell[Width * Height];
                Array.Copy(cells, 0, newCells, 0, cells.Length);
                cells = newCells;
            }
            if (Background.Length < Width * Height)
            {
                var newBG = new Tile[Width * Height];
                Background = newBG;
                FillBackground();
            }
        }

        /// <summary>
        /// Called by the Level when the layout needs to initialize objects
        /// and reset gameplay values
        /// </summary>

        public void OnLoaded()
        {
            EnsureSize();
            InitRandomCell();

            World.BackgroundColor = BackgroundColor;        // Move over some things
            World.BackgroundTile = BackgroundTile;
            
            Game.CameraPos = CameraStart;                   // Set camera position
            Level.CameraMode = CameraMode;
            
            if (DefaultBorders) BuildBorders();             // Build the borders if needed
            
            LoadObjects();                                  // Place the enemies
            LoadSpells();                                   // Reset spells

            // Remove any items Dana already has in his inventory
            var inv = Game.Sesh.Inventory.Where(x => x.FromRoom == RoomNumber);
            foreach (var i in inv)
            {
                var c = this[i.FromCell];
                if (c.GetContents() == i.Type)
                {
                    this[i.FromCell] = RemoveItem(c);
                }
            }

            // Remove any keys that do nothing
            CheckForOrphanedKeys();

            // Remove any first attempt items if Dana's a loser
            if (Game.Sesh.RoomAttempt > 0)
            {
                for (int i = 0; i < cells.Length; i++)
                {
                    Cell c = cells[i];
                    if (IsFirstTryItem(c))
                    {
                        cells[i] = RemoveItem(c);
                    }
                }
            }

            // Copy life over
            if (StartLife.HasValue)
            {
                Level.Life = StartLife.Value;
            }

        }

        /// <summary>
        /// Reloads all objects in the Level from their original placements
        /// </summary>
        public void ReloadObjects()
        {
            Level.ClearObjects();
            LoadObjects();
            LoadSpells();
            Level.LoadDana();
        }

        /// <summary>
        /// Loads objects into the Level from their initial object placements
        /// </summary>

        public void LoadObjects()
        {
            foreach (var o in Objects)
            {
                var obj = o.Instantiate(Level);
                if (obj is null) continue;
                Level.AddObject(obj);
                obj.Init();
            }
        }

        /// <summary>
        /// Resets all spells
        /// </summary>
        public void LoadSpells()
        {
            foreach(var s in Spells)
            {
                s.Finished = false;
            }
        }

        /// <summary>
        /// Tests if a given cell is frozen
        /// </summary>
        public static bool IsFrozen(Cell c) => (c & Cell.Frozen) > 0;

        /// <summary>
        /// Tests if a given cell is covered
        /// </summary>
        public static bool IsCovered(Cell c) => (c & Cell.Covered) > 0;

        /// <summary>
        /// Tests if a given cell contains a hidden item
        /// </summary>
        public static bool IsHidden(Cell c) => (c & Cell.Hidden) > 0;

        /// <summary>
        /// Tests if a given cell is covered with a cracked block
        /// </summary>
        public static bool IsCracked(Cell c) => (c == Cell.BlockCracked) || (c & Cell.Cracked) > 0;

        /// <summary>
        /// Persistent items do not return back to the level layout when Dana dies or
        /// returns to the room later.  Keys, Seals, Pages are examples of items
        /// that are collected once regardless of deaths, game overs, continues, etc.
        /// </summary>
        public static bool IsPersistent(Cell c)
        {
            return c switch
            {
                Cell.Key => true,
                Cell.Seal => true,
                Cell.PageSpace => true,
                Cell.PageTime => true,
                _ => false
            };
        }

        /// <summary>
        /// Tests if a given cell is currently solid
        /// </summary>

        public static bool IsSolid(Cell c) => c switch 
        {
            _ when (c & Cell.Covered) > 0   => true,
            _ when (c & Cell.Cracked) > 0   => true,
            _ when (c & Cell.Frozen) > 0    => true,
            _ when (c & Cell.TempBlock) > 0 => true,
            Cell.Concrete                   => true,
            Cell.Dirt                       => true,
            Cell.Ash                        => true,
            Cell.FakeConcrete               => true,
            Cell.Wick                       => true,
            Cell.Tiles                      => true,
            Cell.InvisibleBlock             => true,
            Cell.BlockCracked               => true,
            Cell.PuzzleUnsolved             => true,
            Cell.PuzzleSolved               => true,
            Cell.BobbleGround               => true,
            _ when c >= Cell.BrickLargeA && c <= Cell.BrickLargeD => true,
            Cell.ToggleBlock                => !(Game.World as Level)?.MagicDown ?? true,
            _                               => false
        };

        public static bool IsSolidNotMesh(Cell c) => c switch
        {
            Cell.Tiles => false,
            _ => IsSolid(c)
        };

        public static bool IsConcrete(Cell c) => c switch
        {
            Cell.Concrete => true,
            _ when (c >= Cell.BrickLargeA && c <= Cell.BrickLargeD) => true,
            _ => false
        };

        /// <summary>
        /// Tests if a given cell is currently solid
        /// </summary>
        public bool IsSolid(Point pos)
        {
            if (pos.X < 0 || pos.X > Width) return false;
            if (pos.Y < 0 || pos.Y > Height) return false;

            Cell c = this[pos.X, pos.Y];
            return IsSolid(c);
        }

        /// <summary>
        /// Tests if a cell contains an item that should only appear on the 
        /// first room attempt
        /// </summary>
        public static bool IsFirstTryItem(Cell c) => c.GetContents()switch
        {
            Cell.ExtraLife          => true,
            Cell.ExtraLife5         => true,
            Cell.Lamp               => true,
            Cell.Rabbit             => true,
            Cell.RabbitHidden       => true,
            Cell.RabbitGray         => true,
            Cell.Sphinx             => true,
            _                       => false
        };

        /// <summary>
        /// Tests if a cell value is an item Dana can collect or trigger via collision
        /// </summary>
        public static bool IsItem(Cell c) => c switch
        {
            _ when IsSolid(c) => false,
            _ when ((c & Cell.Hidden) > 0) => false,
            Cell.Empty                  => false,
            Cell.Mirror                 => false,
            Cell.Bat                    => false,
            Cell.DarkDoorClosed         => false,
            Cell.DoorClosed             => false, 
            Cell.DoorBlue               => false,
            Cell.SolBook                => false,
            Cell.SolBookOpenA           => false, 
            Cell.SolBookOpenB           => false,
            Cell.RabbitGray             => false,
            Cell.RabbitHidden           => false,
            Cell.Grass                  => false,
            Cell.PuzzleUnsolved         => false, 
            Cell.PuzzleSolved           => false, 
            Cell.InvisibleDoor          => false,
            Cell.ToggleBlock            => false,
            _                           => true
        };

        public static bool IsEmpty(Cell c)
        {
            if (c == Cell.Empty) return true;
            if (c == Cell.Grass) return true;
            if (c == Cell.RabbitGray) return true;      // Allow Dana to build a block over the rabbit
            if (c == Cell.RabbitHidden) return true;      // Allow Dana to build a block over the rabbit
            if (c.HasFlag(Cell.Hidden)) return true;
            if (c == Cell.InvisibleDoor) return true;   // Allow Dana to build a block over an invisible door
            return false;
        }

        /// <summary>
        /// Tests if a given item would prevent Dana from breaking a block containing it
        /// with his head
        /// </summary>
        public static bool IsDense(Cell c)
        {
            if (c == Cell.Key) return true;
            if (c == Cell.DoorOpen) return true;
            if (c == Cell.DoorOpenBlue) return true;
            if (c == Cell.DoorBlue) return true;
            if (c == Cell.DoorClosed) return true;
            if (c == Cell.DarkDoorClosed) return true;
            if (c == Cell.DarkDoorOpen) return true;
            if (c == Cell.Mirror) return true;
            if (c == Cell.Bat) return true;
            if (c == Cell.PuzzleUnsolved) return true;
            if (c == Cell.PuzzleSolved) return true;

            return false;
        }

        /// <summary>
        /// Tests if a cell contains a covered (or cracked) item that prevents Dana from
        /// breaking it with his head
        /// </summary>
        public static bool IsBreakableWithHead(Cell c)
        {
            return (IsBreakable(c) && !IsDense(c));
        }

        /// <summary>
        /// Tests if a given cell is breakable via block magic
        /// </summary>
        public static bool IsBreakable(Cell c)
        {
            if ((c & Cell.Covered) > 0) return true;
            if ((c & Cell.Cracked) > 0) return true;
            if (c == Cell.Dirt) return true;
            if (c == Cell.BlockCracked) return true;
            if (c == Cell.FakeConcrete) return true;
            if (c == Cell.TempBlock) return true;
            if (c == Cell.Ash) return true;

            return false;
        }


        /// <summary>
        /// Tests if a given cell is breakable via block magic
        /// </summary>
        public bool IsBreakable(Point pos)
        {
            if (pos.X < 0 || pos.X > Width) return false;
            if (pos.Y < 0 || pos.Y > Height) return false;

            Cell c = this[pos.X, pos.Y];
            return IsBreakable(c);
        }

        /// <summary>
        /// Finds the first visible door that a fairy can escape from
        /// </summary>
        public bool FindVisibleDoor(out Point p)
        {
            var doors = FindDoors(false, true, false, true);
            foreach(var d in doors)
            {
                if (IsItemVisible(this[d.X, d.Y]))
                {
                    p = new Point(d.X, d.Y);
                    return true;
                }
            }
            p = default;
            return false;
        }

        public bool BreakOrMeltBlock(Point pos)
        {
            var c = this[pos];
            if (IsFrozen(c))
            {
                Melt(pos);
                return true;
            }
            else return BreakBlock(pos, Animation.Empty);
        }

        /// <summary>
        /// Destroys this block and all adjacent blocks of the same type
        /// </summary>
        /// <param name="pos"></param>
        public void CascadeDestroy(Point pos, Cell c, Animation animation)
        {

            var l = pos + new Point(-1, 0);
            if (pos.X > 1 && this[l] == c) BreakBlock(l, animation, false);
            l = pos + new Point(1, 0);
            if (pos.X < Width - 1 && this[l] == c) BreakBlock(l, animation, false);
            l = pos + new Point(0, -1);
            if (pos.Y > 1 && this[l] == c) BreakBlock(l, animation, false);
            l = pos + new Point(0, 1);
            if (pos.Y < Height - 1 && this[l] == c) BreakBlock(l, animation, false);
            BreakBlock(pos, animation, false);
        }

        /// <summary>
        /// Attempts block magic to break a block.  Returns true if successful
        /// </summary>
        public bool BreakBlock(Point pos, Animation animation, bool no_cascade = false)
        {
            Cell c = this[pos];

            void sparkle()
            {
                if (animation != Animation.Empty) DrawSparkle(pos, animation, true);
            }

            switch(c)
            {
                case Cell _ when IsCovered(c) || IsCracked(c):
                    if (c == (Cell.Covered | Cell.RabbitGray)) c = Cell.Empty;   // Lost the rabbit
                    if (c == (Cell.Covered | Cell.RabbitHidden)) c = Cell.RabbitGray; // Start the rabbit
                    this[pos.X, pos.Y] = c.ToVisible();
                    sparkle();
                    CheckPromote(pos);
                    return true;
                case Cell.Dirt:
                case Cell.BlockCracked:
                case Cell.FakeConcrete:
                case Cell.Ash:
                    this[pos.X, pos.Y] = Cell.Empty;
                    if (IsSpreadBreak(c) && !no_cascade) CascadeDestroy(pos, c, animation);
                    sparkle();
                    return true;
     
                default: 
                    return false;

            }

        }

        /// <summary>
        /// Determines if this type of cell will cascade breakage to all adjacent
        /// cells of that type
        /// </summary>
        /// <param name="c"></param>
        /// <returns></returns>
        public static bool IsSpreadBreak(Cell c)
        {
            return (c == Cell.Ash);
        }

        /// <summary>
        /// Checks if block magic can be cast on a given cell
        /// 0 = Not blocked
        /// 1 = Blocked
        /// 2 = Magic invoked object
        /// </summary>
        public int CastMagicCheckObjects(Point pos)
        {
            var rect = new Rectangle(pos.ToWorld(), Game.NativeTileSize);
            foreach (var o in Level.Objects)
            {

                if (o.ProjectedBox.Intersects(rect))
                {
                    if (o.BlocksMagic) return 1;
                    if (o.AcceptsMagic)
                    {
                        o.MagicCasted();
                        return 2;
                    }
                }
            }
            return 0;
        }

        public void UnhideAll()
        {
            Sound.Reveal.Play();
            for (int y = 0; y < Height; y++)
                for (int x = 0; x < Width; x++)
                {
                    Cell c = this[x, y];
                    if (IsHidden(c))
                    {
                        this[x, y] = c.GetContents();
                        DrawSparkle(new Point(x, y), Animation.ShortSparkle, true, false);
                    }
                }
        }

        /// <summary>
        /// Casts magic on a specific cell to change an item;
        /// returns true if successful
        /// </summary>
        public bool MagicBlock(Point pos)
        {
            Cell c = this[pos];
            switch (c)
            {
                /* Blue loot block */
                case Cell.LootBlue:
                    this[pos] = Cell.LootBlueFireballJar;
                    return true;
                case Cell.LootBlueFireballJar:
                    this[pos] = Cell.LootBlueGold2;
                    return true;
                case Cell.LootBlueGold2:
                    this[pos] = Cell.LootBlueCrystalGold;
                    return true;
                case Cell.LootBlueCrystalGold:
                    this[pos] = Cell.LootBlue;
                    return true;

                /* Gold loot block */
                case Cell.LootGold:
                    this[pos] = Cell.LootGoldSuperFireballJar;
                    return true;
                case Cell.LootGoldSuperFireballJar:
                    this[pos] = Cell.LootGoldScroll;
                    return true;
                case Cell.LootGoldScroll:
                    this[pos] = Cell.LootGoldBell;
                    return true;
                case Cell.LootGoldBell:
                    this[pos] = Cell.LootGold;
                    return true;

                /* Red loot block */
                case Cell.LootRed:
                    this[pos] = Cell.LootRedCrystalRed;
                    return true;
                case Cell.LootRedCrystalRed:
                    this[pos] = Cell.LootRedRedFireballJar;
                    return true;
                case Cell.LootRedRedFireballJar:
                    this[pos] = Cell.LootRedCopper;
                    return true;
                case Cell.LootRedCopper:
                    this[pos] = Cell.LootRed;
                    return true;
                case Cell.SolBook:
                    Level.EndingSequence(pos);
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Attempts to use block magic to create a block in a given cell.
        /// 0 = Success
        /// 1 = Blocked
        /// 2 = Magic invoked object
        /// </summary>
        public int MakeBlock(Point pos)
        {
            Cell c = this[pos];

            var occ = CastMagicCheckObjects(pos);
            if (occ > 0) return occ;

            if ((c & Cell.Hidden) > 0 || IsEmpty(c))
            {
                // We can make a block

                // Set the cell to a temporary invisible block
                this[pos] = c | Cell.TempBlock;

                // The sparkle will create the dirt block when it's done
                DrawSparkle(pos, Animation.BlockMake, true, true);
                return 0;
            }
            else if (IsFrozen(c) && !Game.Sesh.Apprentice)
            {
                // If Dana is trying to use magic on a frozen block
                // then do his special ability...
                if (Level.FrozenCracked != pos)
                {
                    // Block has moved
                    Level.FrozenCracked = pos;
                    Level.FrozenCount = 1;
                    Level.FrozenTime = Level.RefreezeTicks;
                    return 1;  
                } else
                {
                    // Hitting same block
                    Level.FrozenCount++;
                    if (Level.FrozenCount < 8)
                    {
                        // Reset the ice melt timer
                        Level.FrozenTime = Level.RefreezeTicks;
                        return 1;
                    }
                    else
                    {
                        // Finally destroy the block
                        this[pos] = c.GetContents();
                        Sound.Head.Play();
                        DrawSparkle(pos, Animation.BlockBreak, true);
                        return 2;
                    }
                }
            }

            return 1;

        }

        /// <summary>
        /// Hits a cell with Dana's head
        /// </summary>
        public bool HeadHit(Point pos)
        {
            Cell c = this[pos];
            var cont = c.GetContents();

            if (!IsBreakableWithHead(c)) return false;

            if ((c & Cell.Cracked) > 0)
            {
                if (IsDense(cont)) return false;
                if (cont == Cell.RabbitGray) cont = Cell.Rabbit;    // Won the rabbit
                this[pos] = cont;
                DrawSparkle(pos, Animation.BlockCrack, true, false);
                CheckPromote(pos);  
                return true;

            } else if ((c & Cell.Covered) > 0)
            {
                this[pos] = cont | Cell.Cracked;
                return true;

            } else if (c == Cell.Dirt || c == Cell.FakeConcrete)
            {
                this[pos] = Cell.BlockCracked;
                return true;
            } else if (c == Cell.BlockCracked)
            {
                this[pos] = Cell.Empty;
                DrawSparkle(pos, Animation.BlockCrack, true, false);
                return true;
            } else if (c == Cell.Ash)
            {
                CascadeDestroy(pos, c, Animation.BlockCrack);
                this[pos] = Cell.Empty;
                return true;
            }

            return false;
        }

        /// <summary>
        /// Called when the block creation animation finishes to set the cell
        /// as containing a dirt block.
        /// </summary>
        public bool MakeBlockFinish(Point pos)
        {
            if (pos.X < 0 || pos.X > Width) return false;
            if (pos.Y < 0 || pos.Y > Height) return false;

            Cell c = this[pos] & ~Cell.TempBlock;

            if (c == Cell.RabbitGray)
            {
                this[pos] = Cell.RabbitGray | Cell.Covered;
            }
            else if (c == Cell.Empty)
            {
                this[pos] = Cell.Dirt;
            }
            else
            {
                this[pos] = Cell.Covered | c.GetContents();
            }

            Level.Dana.DanaCollideLevel(Level.Dana.MagicDirection);
            return true;

        }

        /// <summary>
        /// Draws an animation effect in a given cell
        /// </summary>
        public void DrawSparkle(Point pos, Animation anim, bool blocks_magic, bool make_block = false)
            => DrawSparkleWorld(pos.ToWorld(), anim, blocks_magic, make_block);

        /// <summary>
        /// Draws an animation effect at a given world coordinate
        /// </summary>
        public void DrawSparkleWorld(Point pos, Animation anim, bool blocks_magic, bool make_block = false)
        {
            var o = new Objects.Twinkle(Level, blocks_magic);
            if (make_block)
            {
                o.OnFinished = s => Level.Layout.MakeBlockFinish(s.Position.ToCell());
            }
            o.Animation = anim;
            o.Position = pos;
            Level.AddObject(o);
        }

        /// <summary>
        /// Sets appropriate values in the cell grid to draw the standard NES border
        /// blocks around the boundaries of a single-screen room
        /// </summary>
        private void BuildBorders()
        {
            if (DefaultBorders)
            {
                int right = Width - 1;
                int bottom = Height - 1;

                for (int y = 1; y + 1 < bottom; y++)
                {
                    this[0, y] = Cell.BrickLargeA;
                    this[0, ++y] = Cell.BrickLargeB;
                }
                for (int y = 1; y + 1 < bottom; y++)
                {
                    this[right, y] = Cell.BrickLargeA;
                    this[right, ++y] = Cell.BrickLargeB;
                }
                for (int x = 1; x < right; x++)
                {
                    this[x, bottom] = Cell.BrickLargeC;
                    this[++x, bottom] = Cell.BrickLargeD;
                }
                for (int x = 1; x < right; x++)
                {
                    this[x, 0] = Cell.BrickLargeC;
                    this[++x, 0] = Cell.BrickLargeD;
                }
                this[0] = Cell.BrickLargeD;
                this[0, bottom] = Cell.BrickLargeD;
            }
        }

        /// <summary>
        /// Gets the graphics tile that corresponds to a given Cell item value
        /// (does not look at modifiers)
        /// </summary>
        public static Tile CellToTile(Cell c)
        {
            var edit = (Game.World as Level)?.State == LevelState.Edit;

            if (c.HasFlag(Cell.TempBlock))
            {
                c = c.GetContents();
            }

            switch (c)
            {
                case Cell.TempBlock: return Tile.Empty;
                case Cell.LootBlueCrystalGold: return Tile.CrystalGold;
                case Cell.LootBlueFireballJar: return Tile.FireballJar;
                case Cell.LootBlueGold2: return Tile.Gold2;
                case Cell.LootGoldBell: return Tile.Bell;
                case Cell.LootGoldScroll: return Tile.Scroll;
                case Cell.LootGoldSuperFireballJar: return Tile.SuperFireballJar;
                case Cell.LootRedCopper: return Tile.Copper;
                case Cell.LootRedCrystalRed: return Tile.CrystalRed;
                case Cell.LootRedRedFireballJar: return Tile.RedFireballJar;
                case Cell.InvisibleBlock: return  Tile.Empty;
                case Cell.FakeConcrete: return Tile.Concrete;
                case Cell.RabbitHidden: return edit ? Tile.Internal : Tile.Empty;
                case Cell.InvisibleDoor: return edit ? Tile.Internal : Tile.Empty;
                default: return (Tile)c;
            }
        }

        /// <summary>
        /// Renders the level's background
        /// </summary>
        public void RenderBackground(SpriteBatch batch)
        {
            for (int y = 0; y < Height; y++)
                for (int x = 0; x < Width; x++)
                {
                    var t = Background[y * Width + x];

                    // Render opaque
                    World.RenderTile(batch, x, y, t, Color.White);

                }
        }

        /// <summary>
        /// Gets the draw phase for a given Cell and Cell location.
        /// </summary>
        public static DrawPhase GetDrawPhase(Cell c, Point p)
        {
            switch (c.GetContents())
            {
                case Cell.BrickLargeA:
                case Cell.BrickLargeB:
                case Cell.BrickLargeC:
                case Cell.BrickLargeD:
                    return DrawPhase.Borders;
                case Cell.Key:
                case Cell.DoorClosed:
                case Cell.PinkUmbrella:
                case Cell.BlueUmbrella:
                case Cell.DarkDoorClosed:
                case Cell.DarkDoorOpen:
                case Cell.DoorOpen:
                case Cell.SolBook:
                    return (IsCovered(c) || IsCracked(c) || IsHidden(c)) ? DrawPhase.Everything 
                                                                         : DrawPhase.BordersKeyDoor;
                default:
                    return DrawPhase.Everything;
            }
        }

        /// <summary>
        /// Opens a door at a specific cell location
        /// </summary>
        /// <returns>Returns true if opened, false if it was already opened or was not a door</returns>
        public bool OpenDoor(Point p)
        {
            var c = this[p];
            var m = c.GetModifier();
            c = c.GetContents();
            bool success = false;

            if (c == Cell.DoorClosed) { this[p] = Cell.DoorOpen | m; success = true; }
            if (c == Cell.DoorBlue) { this[p] = Cell.DoorOpenBlue | m; success = true; }
            if (c == Cell.DarkDoorClosed) { this[p] = Cell.DarkDoorOpen | m; success = true; }
            if (c == Cell.InvisibleDoor) { this[p] = Cell.DarkDoorOpen; m = Cell.Empty; success = true; }

            if (success && !IsFrozen(c | m) && !IsHidden(c | m) && !IsCovered(c | m) && !IsCracked(c | m))
            {
                /* Halfway door open "animation" */
                if (Level.State == LevelState.Running || Level.State == LevelState.KeyStars)
                {
                    var a = new Objects.Twinkle(Level);
                    a.Animation = Animation.DoorOpen;
                    a.Position = p.ToWorld();
                    Level.AddObject(a);
                }
            }

            if (success)
            {
                CheckForOrphanedKeys();
            }

            return success;
        }


        /// <summary>
        /// Melts/burns a frozen block (if frozen)
        /// </summary>
        public bool Melt(Point cell)
        {
            if (!IsFrozen(this[cell])) return false;

            // Melt ice
            Sound.Burn.Play();
            Level.Layout[cell] &= ~Cell.Frozen;
            Level.AddObject(new Objects.Remains(Level, Cell.Empty, cell.ToWorld(), true));
            return true;
        }

        /// <summary>
        /// Checks and removes any keys that no longer would open any remaining
        /// doors
        /// </summary>
        public void CheckForOrphanedKeys()
        {
            var keys = FindKeys();
            foreach (var k in keys)
            {
                var doors = FindDoorsForKey(k);
                int closed = 0;
                Cell c = this[k];

                foreach (var d in doors)
                {
                    if (Game.Sesh.DoorsOpened.Any(x => x.Room == Game.Sesh.RoomNumber && x.Door == d))
                    {
                        continue;
                    }
                    closed++;
                }
                if (closed == 0)
                {
                    var visible = IsItemVisible(c);
                    this[k] = RemoveItem(c);
                    if (visible && (Level.State == LevelState.Running || Level.State == LevelState.OpeningDoor))
                    {
                        DrawSparkle(k, Animation.DropCloud, false, false);
                    }
                }
            }
        }

        /// <summary>
        /// Tests if the item contained in a cell is currently visible (and not frozen)
        /// </summary>
        public bool IsItemVisible(Cell c) => !(IsHidden(c) || IsCovered(c) || IsCracked(c));

        /// <summary>
        /// Manually finds all cells with keys (any state)
        /// </summary>
        /// <returns></returns>
        public List<Point> FindKeys()
        {
            var l = new List<Point>();
            for (int y = 0; y < Height; y++)
                for (int x = 0; x < Width; x++)
                {
                    if (IsKeyAtAll(this[x, y]))
                        l.Add(new Point(x, y));
                }
            return l;
        }

        /// <summary>
        /// Manually finds all cells containing the item in question
        /// </summary>
        /// <returns></returns>
        public List<Point> FindCell(Cell c, bool exact = false)
        {
            var l = new List<Point>();
            for (int y = 0; y < Height; y++)
                for (int x = 0; x < Width; x++)
                {
                    var cmp = exact ? this[x, y] : this[x, y].GetContents();
                    if (c == cmp) l.Add(new Point(x, y));
                }
            return l;
        }


        /// <summary>
        /// Opens doors regardless of position
        /// </summary>
        /// <param name="limit">How many doors to open (0 = all)</param>
        /// <param name="dark">Open dark doors?</param>
        /// <param name="normal">Open normal doors?</param>
        public int OpenDoors(int limit, bool dark, bool normal)
        {
            int doors = 0;
            for (int y = 0; y < Height; y++)
                for (int x = 0; x < Width; x++)
                {
                    var c = this[x, y];
                    if (c == Cell.DoorClosed || c == Cell.DarkDoorClosed || c == Cell.DoorBlue)
                    {
                        if (OpenDoor(new Point(x, y)))
                        {
                            doors++;
                            if (limit != 0 && doors >= limit) return doors;
                        }
                    }
                }
            return doors;
        }

        /// <summary>
        /// Finds all doors that match the criteria
        /// </summary>
        /// <param name="dark">Include dark doors?</param>
        /// <param name="normal">Include normal doors?</param>
        /// <param name="warp">Include warp doors?</param>
        /// <param name="open">Include open doors?</param>
        public List<Point> FindDoors(bool dark, bool normal, bool warp, bool open = false)
        {
            var l = new List<Point>();
            for (int x = 0; x < Width; x++)
                for (int y = 0; y < Height; y++)
                {
                    var c = cells[y * Width + x].GetContents();

                    if (c == Cell.DoorClosed && normal) { l.Add(new Point(x, y)); }
                    if (c == Cell.DoorBlue && warp) { l.Add(new Point(x, y)); }
                    if (c == Cell.DarkDoorClosed && dark) { l.Add(new Point(x, y)); }
                    if (open)
                    {
                        if (c == Cell.DoorOpen && normal) { l.Add(new Point(x, y)); }
                        if (c == Cell.DoorOpenBlue && warp) { l.Add(new Point(x, y)); }
                        if (c == Cell.DarkDoorOpen && dark) { l.Add(new Point(x, y)); }
                    }
                }
            return l;
        }

        public void RenderSuperForeground(SpriteBatch batch, DrawPhase phase)
        {
            if (SuperForeground is null) return;
            var blackout = (phase != DrawPhase.Everything && !UseSFForBorders);

            Color color = blackout ? Color.Black : SuperForegroundColor;
            bool reveal = Level.State == LevelState.Edit && Control.ShiftKeyDown() && Control.ControlKeyDown();

            if (Level.VapourMode && !blackout)  color *= 0.5f;
            if (reveal) return;

            color *= SuperForegroundOpacity;

            // OK we have something to draw so force a new sprite batch
            batch.End();
            batch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.PointClamp);

            for (int y = 0; y < Height; y++)
                for (int x = 0; x < Width; x++)
                {
                    var t = SuperForeground[y * Width + x];

                    // Render opaque
                    World.RenderTile(batch, x, y, t, color);

                }

        }


        /// <summary>
        /// Render the layout's foreground blocks based on the current DrawPhase
        /// </summary>
        public void RenderForeground(SpriteBatch batch, DrawPhase phase)
        {

            // Editor stuff
            bool partial = false;
            bool reveal = false;
            var edit = Level.State == LevelState.Edit;
            if (edit)
            {
                if (Level.Editor.Mode is Editor.SpawnsMode) partial = true;
                if (Level.Editor.Mode is Editor.MagicMode) partial = true;
                reveal = Control.ControlKeyDown() && Control.ShiftKeyDown();
            }

            // Debug stuff
            Point dtb = new Point(0, 0);
            if (Game.ShowCollision)
            {
                dtb = Level.Dana.FindTargetBlock();
            }

            if (phase == DrawPhase.Everything)
            {
                /* Optional room numbers for doors -- do this behind blocks in case
                 * something goes on top of it */
                foreach (var d in Doors)
                {
                    if (!d.ShowRoomNumber) continue;                                     // no room number shown
                    if (!Layout.IsDoorAtAll(this[d.Position])) continue;                 // no longer a door
                    var rm = d.RoomNumber.ToString("X");                           // get hex room number
                    var pt = d.Position.ToWorld() + new Point(0, -8);               // calculate position
                    batch.DrawShadowedStringCentered(rm, pt, Color.White, 1);  // draw it
                }
            }

            for (int y = 0; y < Height; y++)
                for (int x = 0; x < Width; x++)
                {
                    var c = cells[y * Width + x];

                    if (phase < DrawPhase.Everything)
                    {
                        if (GetDrawPhase(c, new Point(x, y)) > phase)
                        {
                            continue;
                        }
                    }

                    Tile t = Tile.Empty;    // Draw this tile opaque
                    Tile u = Tile.Empty;    // Then draw this tile translucent

                    bool frozen = false;
                    bool hidden = false;
                    bool faded = false;
                    Color frozenColor = new Color(0.4f, 0.7f, 1.0f, 1.0f);
                    Color frozenColor2 = MoreTranslucent;

                    if ((c & Cell.Frozen) > 0)
                    {

                        frozen = true;
                        // Get frozen tile and color applicate from level
                        // based on what Dana's doing over there
                        (u, frozenColor, frozenColor2) = Level.GetFrozenTile(x, y);   
                        t = CellToTile(c.GetContents());
                    }
                    else if ((c & Cell.Covered) > 0)
                    {
                        t = Tile.BrickTan;
                        if (edit) u = CellToTile(c.GetContents());
                    }
                    else if ((c & Cell.Cracked) > 0)
                    {
                        t = Tile.BrickCracked;
                        if (edit) u = CellToTile(c.GetContents());
                    }
                    else if ((c & Cell.Hidden) > 0)
                    {
                        hidden = true;
                        t = Tile.Empty;
                        if (edit) u = CellToTile(c.GetContents());
                    }
                    else if (c == Cell.ToggleBlock)
                    {
                        t = CellToTile(c);
                        faded = Level.MagicDown;
                    }
                    else
                    {
                        t = CellToTile(c);
                    }

                    // Figure out the coloring
                    var color = Color.White;
                    if (frozen)
                    {
                        color = frozenColor;
                    }
                    
                    if (Game.ShowCollision && Level.Dana != null)
                    {
                        color = Level.DebugObject.GetDebugColor(x, y, dtb);
                    }

                    if (faded) color *= 0.5f;

                    // Render opaque
                    if (c == Cell.FakeConcrete && edit && reveal) color = Color.Blue;
                    World.RenderTile(batch, x, y, t, (partial || reveal) ? color * 0.5f : color);

                    // Render translucent overlay?
                    if (u != Tile.Empty)
                    {
                        color = partial ? Color.White * 0.5f : Color.White;
                        if (hidden) color = MoreTranslucent;
                        if (frozen) color = frozenColor2;
                        if (hidden && edit && reveal) color = Color.Lime;
                        World.RenderTile(batch, x, y, u, color);
                    }

                }


        }

        /// <summary>
        /// Render a given cell at a given world coordinate
        /// </summary>
        public void RenderCellWorld(SpriteBatch batch, Point pos, Cell c)
        {

            Tile t;    // Draw this tile opaque
            Tile u = Tile.Empty;    // Then draw this tile translucent

            bool frozen = false;
            bool hidden = false;
            bool faded = false;

            if (IsFrozen(c))
            {
                frozen = true;
                t = CellToTile(c.GetContents());
                u = Tile.FrozenBlock;
            }
            else if (IsCovered(c))
            {
                t = Tile.BrickTan;
                u = CellToTile(c.GetContents());
            }
            else if (IsCracked(c))
            {
                t = Tile.BrickCracked;
                u = CellToTile(c.GetContents());
            }
            else if (IsHidden(c))
            {
                hidden = true;
                t = Tile.Empty;
                u = CellToTile(c.GetContents());
            }
            else if (c == Cell.ToggleBlock)
            {
                t = Tile.ToggleBlock;
                faded = true;
            }
            else
            {
                t = CellToTile(c);
            }

            // Figure out the coloring
            var color = Color.White;
            if (frozen)
            {
                color = new Color(0.4f, 0.7f, 1.0f, 1.0f);
            }
            else if (faded)
            {
                color = Color.White * 0.5f;
            }

            // Render opaque
            Level.RenderTileWorld(batch, pos.X, pos.Y, t, color);

            // Render translucent overlay?
            if (u != Tile.Empty)
            {
                color = Color.White;
                if (frozen || hidden) color = MoreTranslucent;
                World.RenderTileWorld(batch, pos.X, pos.Y, u, color);
            }

        }

        /// <summary>
        /// Creates a blank level layout of the standard size
        /// </summary>
        public static Layout BlankLayout(int room, World world)
        {
            var l = new Layout(world, 17, 14);
            l.RoomNumber = room;
            l.Shrine = 12;
            l.BackgroundColor = LevelEditor.BGColors[4];
            l.BackgroundTile = Tile.BrickBackground;
            l.FillBackground();
            return l;
        }

        /// <summary>
        /// Imports a level layout from an expanded Solomon's Key ROM (saved with SKEdit.exe)
        /// </summary>
        public static Layout ImportLegacy(string filename, int room, World world)
        {
            if (room > 55) return BlankLayout(room, world);

            room--;
            int offset = (room * 0x100) + 0xC010;
            int poffset = (room * 0x100) + 0xC100;
            int moffset = (room * 0x010) + 0xF510;
            room++;

            var l = new Layout(world, 17, 14);
            l.DefaultBorders = true;
            l.RoomNumber = room;
            l.Shrine = room / 4;
            l.NextRoom = room + 1;
            l.NextRoomWing = room + 6;
            switch(room)
            {
                case 49: l.Name = "PRINCESS"; break;
                case 50: l.Name = "SOLOMON"; break;
                case 51: l.Name = "HIDDEN"; l.Shrine = -1;  break;
                case 52: l.Name = "HIDDEN"; l.Shrine = -1;  break;
                case 53: l.Name = "HIDDEN"; l.Shrine = -1;  break;
            }

            var file = File.ReadAllBytes(filename);
            var origin = offset;

            /* Fill gray blocks */
            for (int c = 0; c < 17 * 14; c++)
            {
                l[c] = Cell.Concrete;
            }

            /* Copy level layout */
            for (int y = 0; y < 12; y++)
                for (int x = 0; x < 16; x++)
                {
                    var d = file[offset++];
                    l[x + 1, y + 1] = FromLegacy(d);
                }

            offset += 4; /* Skip legacy mirror types */

            /* Key type */
            var flags = file[offset++];
            Cell key = Cell.Key;
            if ((flags & 0x40) > 0) key |= Cell.Covered;
            if ((flags & 0x80) > 0) key |= Cell.Hidden;

            /* Door */
            var doordata = file[offset++];
            l[(doordata & 0x0F) + 1, doordata >> 4] = Cell.DoorClosed;

            /* Key */
            var keydata = file[offset++];
            l[(keydata & 0x0F) + 1, keydata >> 4] = key;

            /* Dana */
            var dana = file[offset++];
            l.DanaStart = new Point((dana & 0x0F) + 1, dana >> 4);
            l.AdamStart = l.DanaStart;

            /* Other stuff */
            var m1 = file[offset++];
            var m2 = file[offset++];
            var motif = file[offset++];
            var motifloc = file[offset++];
            var ttl = file[offset++];

            /* Background */
            if ((motif & 0x8) > 0)
                l.BackgroundTile = Tile.StuccoBackground;
            else if ((motif & 0x2) > 0)
                l.BackgroundTile = Tile.BlockBackground;
            else
                l.BackgroundTile = Tile.BrickBackground;

            l.FillBackground();

            /* Background color */
            if (room > 48)
            {
                l.BackgroundColor = LevelEditor.BGColors[4];
            } else
            {
                l.BackgroundColor = LevelEditor.BGColors[((room - 1) / 4) % 4];
            }

            if ((motif & 0xF0) == 0xF0)
            {
                // Draw art
                l.DrawBGMotif(LargeObject.Motif(l, (motifloc & 0x0F) + 1, motifloc >> 4, motif & 0xF));
            }

            /* Static enemies */
            int eNum = 0;
            for (int e = 0; e < 26; e += 2)
            {
                var type = file[origin + e + 205];
                if (type == 0) continue;

                var loc = file[1 + e + origin + 205];

                ObjectPlacement enemy = ObjectPlacement.FromLegacy(type, loc, eNum++);
                if (enemy.Type != ObjType.None)
                {
                    l.Objects.Add(enemy);
                }

            }

            /* Spawning enemies */
            var mirror1 = new Spawn(LegacyToPoint(m1), ttl);
            var mirror2 = new Spawn(LegacyToPoint(m2), ttl);

            // Mirror 1
            for (int p = 0; p < 8; p++)
            {
                var md = p + poffset;
                var pd = file[md];
                if (pd == 90) break;

                // make a temporary object placement to grab the details
                var obj = ObjectPlacement.FromLegacy(pd, 0, 1);
                if (obj.Type != ObjType.None)
                {
                    var t = new SpawnItem();
                    t.Type = obj.Type;
                    t.Flags = obj.Flags;
                    t.Direction = obj.Direction;
                    mirror1.SpawnItems.Add(t);
                }

            }
            poffset += 8;
            // Mirror 2
            for (int p = 0; p < 8; p++)
            {
                var md = p + poffset;
                var pd = file[md];
                if (pd == 90) break;

                // make a temporary object placement to grab the details
                var obj = ObjectPlacement.FromLegacy(pd, 0, 1);
                if (obj.Type != ObjType.None)
                {
                    var t = new SpawnItem();
                    t.Type = obj.Type;
                    t.Flags = obj.Flags;
                    t.Direction = obj.Direction;
                    mirror2.SpawnItems.Add(t);
                }

            }

            // spawn frequencies
            mirror1.Phase0 = GetInt(file, moffset);
            mirror1.Phase1 = GetInt(file, moffset + 4);
            mirror2.Phase0 = GetInt(file, moffset + 8);
            mirror2.Phase1 = GetInt(file, moffset + 12);

            // add to layout
            if (mirror1.SpawnItems.Count > 0)
            {
                l.Spawns.Add(mirror1);
            }
            if (mirror2.SpawnItems.Count > 0)
            {
                l.Spawns.Add(mirror2);
            }

            return l;
        }

        /// <summary>
        /// Fills the entire background grid with the value of BackgroundTile
        /// </summary>
        public void FillBackground()
        {
            for (int i = 0; i < Background.Length; i++)
            {
                Background[i] = BackgroundTile;
            }
        }

        public void FillSuperForeground(Tile tile)
        {
            SuperFGCheck();
            for (int i = 0; i < SuperForeground.Length; i++)
            {
                SuperForeground[i] = tile;
            }
        }

        public void SuperFGCheck()
        {
            if (SuperForeground is null)
            {
                SuperForeground = new Tile[Width * Height];
            }
        }

        /// <summary>
        /// Draws a LargeObject to the background grid
        /// </summary>
        /// <param name="motif"></param>
        public void DrawBGMotif(LargeObject motif)
        {
            int i = 0;
            for (var y = motif.Y; y < motif.Y + motif.TilesY; y++)
                for (var x = motif.X; x < motif.X + motif.TilesX; x++)
                {
                    Background[y * Width + x] = (motif.Tile + i++);
                }

        }

        /// <summary>
        /// Gets a 32-bit integer from a buffer at a given offset
        /// </summary>
        private static uint GetInt(byte[] file, int offset)
        {
            byte[] bytes = new byte[4];
            Array.Copy(file, offset, bytes, 0, 4);
            Array.Reverse(bytes);
            return BitConverter.ToUInt32(bytes, 0);

        }

        /// <summary>
        /// Converts a legacy position (single byte in YX format) to a cell Point
        /// </summary>
        public static Point LegacyToPoint(byte pos)
        {
            return new Point((pos & 0x0F) + 1, pos >> 4);
        }

        /// <summary>
        /// Gets cell contents from legacy NES value
        /// </summary>
        private static Cell FromLegacy(byte x)
        {
            if (x == 0xF8) return Cell.Concrete;
            if (x == 0x90) return Cell.Dirt;

            Cell c = 0;
            switch (x & 0x3F)
            {
                case 0x04: c = Cell.Bat; break;
                case 0x05: c = Cell.Mirror; break;
                case 0x06: c = Cell.Key; break;
                case 0x07: c = Cell.DoorOpen; break;
                case 0x08: c = Cell.LootBlue; break;
                case 0x09: c = Cell.LootBlueFireballJar; break;
                case 0x0A: c = Cell.LootBlueGold2; break;
                case 0x0B: c = Cell.LootBlueCrystalGold; break;
                case 0x0C: c = Cell.LootGold; break;
                case 0x0D: c = Cell.LootGoldSuperFireballJar; break;
                case 0x0E: c = Cell.LootGoldScroll; break;
                case 0x0F: c = Cell.LootGoldBell; break;
                case 0x10: c = Cell.Empty; break;
                case 0x11: c = Cell.HalfLifeJar; break;
                case 0x12: c = Cell.FullLifeJar; break;
                case 0x13: c = Cell.HourglassBlue; break;
                case 0x14: c = Cell.HourglassGold; break;
                case 0x15: c = Cell.FireballJar; break;
                case 0x16: c = Cell.SuperFireballJar; break;
                case 0x17: c = Cell.Scroll; break;
                case 0x18: c = Cell.Bell; break;
                case 0x19: c = Cell.ExplosionJar; break;
                case 0x1A: c = Cell.SnowballJar; break;
                case 0x1B: c = Cell.CrystalBlue; break;
                case 0x1C: c = Cell.ShrineAries; break;
                case 0x1D: c = Cell.ShrineTaurus; break;
                case 0x1E: c = Cell.ShrineGemini; break;
                case 0x1F: c = Cell.ShrineCancer; break;
                case 0x20: c = Cell.Seal; break;
                case 0x22: c = Cell.GoldWing; break;
                case 0x25: c = Cell.Silver; break;
                case 0x26: c = Cell.Silver2; break;
                case 0x27: c = Cell.CoinBlue; break;
                case 0x28: c = Cell.Gold; break;
                case 0x29: c = Cell.Gold2; break;
                case 0x2A: c = Cell.CoinGold; break;
                case 0x2B: c = Cell.Dollar; break;
                case 0x2C: c = Cell.Dollar2; break;
                case 0x2D: c = Cell.CoinGold2; break;
                case 0x2E: c = Cell.Crane; break;
                case 0x2F: c = Cell.MightyCoin; break;
                case 0x30: c = Cell.Sphinx; break;
                case 0x31: c = Cell.KingTut; break;
                case 0x32: c = Cell.Lamp; break;
                case 0x33: c = Cell.ExtraLife; break;
                case 0x34: c = Cell.ExtraLife5; break;

            }
            if ((x & 0x40) > 0) c |= Cell.Hidden;
            if ((x & 0x80) > 0) c |= Cell.Covered;
            return c;
        }

        /// <summary>
        /// Creates a level layout from an external file
        /// </summary>
        public static Layout LoadFile(string fileName, World world)
        {
            string json = fileName.ReadJSON();
            dynamic read = json.To<ExpandoObject>();

            try
            {
                int w = (int)read.Width;
                int h = (int)read.Height;
                var l = new Layout(world, w, h);
                JsonConvert.PopulateObject(json, l);
                return l;

            } catch { return null; }

        }

        /// <summary>
        /// Get the constellation symbol graphic for a given shrine number
        /// </summary>
        public Cell GetShrine(int i)
        {
            switch (i)
            {
                // Constellation signs
                case 0: return Cell.ShrineAries;
                case 1: return Cell.ShrineTaurus;
                case 2: return Cell.ShrineGemini;
                case 3: return Cell.ShrineCancer;
                case 4: return Cell.ShrineLeo;
                case 5: return Cell.ShrineVirgo;
                case 6: return Cell.ShrineLibra;
                case 7: return Cell.ShrineScorpio;
                case 8: return Cell.ShrineSagittarius;
                case 9: return Cell.ShrineCapricorn;
                case 0xA: return Cell.ShrineAquarius;
                case 0xB: return Cell.ShrinePisces;
            }

            if (Game.IsClassic)
                return Cell.ShrineSolomon;

            switch(i) 
            { 
                // Solar system signs
                case 0xC: return Cell.ShrineMercury;
                case 0xD: return Cell.ShrineVenus;
                case 0xE: return Cell.ShrineEarth;
                case 0xF: return Cell.ShrineMars;
                case 0x10: return Cell.ShrineJupiter;
                case 0x11: return Cell.ShrineSaturn;
                case 0x12: return Cell.ShrineUranus;
                case 0x13: return Cell.ShrineNeptune;
                case 0x14: return Cell.ShrinePluto;
                case 0x15: return Cell.ShrineMoon;
                case 0x16: return Cell.ShrineSun;

                case 0x17: return Cell.SealJuno;
                case 0x18: return Cell.SealVesta;
                case 0x19: return Cell.SealHygeia;
                case 0x1A: return Cell.SealChiron;
                case 0x1B: return Cell.SealComet;
                case 0x1C: return Cell.SealUnity;
                case 0x1D: return Cell.SealAmtoudi;
                case 0x1E: return Cell.SealEris;
                case 0x1F: return Cell.Cartouche1;
                case 0x20: return Cell.CartoucheRamesees;
                case 0x21: return Cell.CartoucheRocco;
                case 0x22: return Cell.Cartouche4;
                case 0x23: return Cell.SealSedna;
                case 0x24: return Cell.CartoucheLumania;
                case 0x25: return Cell.Cartouche6;
                case 0x26: return Cell.ShrineSolomon;
                case 0x27: return Cell.ShrineSesquiquadrate;
                case 0x28: return Cell.SealCosmos;

                default: return Cell.ShrineSolomon;
            }
        }

    }

    /// <summary>
    /// Used to store any extended door information (anything beyond a normal 
    /// door that goes to the default next room number)
    /// </summary>
    public class DoorInfo
    {
        public Point Position;
        public int RoomNumber;
        public Point Target;
        public DoorType Type;
        public bool ShowRoomNumber;
        public bool FastStars;

        /// <summary>
        /// Returns a description for the editor HUD
        /// </summary>
        public string Text()
        {
            switch (Type)
            {
                case DoorType.Room:
                    return "TARGET ROOM " + RoomNumber.ToString("X");
                case DoorType.Warp:
                    return $"X {Target.X} Y {Target.Y}";
                case DoorType.RoomAndWarp:
                    return $"TARGET ROOM {RoomNumber:X} X {Target.X} Y {Target.Y}";
            }
            return "UNKNOWN TYPE";
        }

    }

    /// <summary>
    /// Valid door types
    /// </summary>
    public enum DoorType
    {
        Room,
        Warp,
        RoomAndWarp,
    }

    /// <summary>
    /// Stores any extended information for a key (anything beyond opening all normal doors
    /// in a room)
    /// </summary>
    public struct KeyInfo
    {
        public Point KeyPosition;
        public Point DoorPosition;
        public bool Valid => KeyPosition.X > 0 && DoorPosition.X > 0;

    }

    /// <summary>
    /// Determines which cells in a layout are drawn to the screen
    /// </summary>
    public enum DrawPhase
    {
        Borders,
        BordersKeyDoor,
        Everything
    }

    public enum CharacterMode
    {
        Any = 0,
        ForceDana = 1,
        ForceAdam = 2
    }

}
