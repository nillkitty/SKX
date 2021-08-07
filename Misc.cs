using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended;
using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace SKX
{

    /// <summary>
    /// Represents a multi-tile object used in the background (or foreground!) of a World
    /// </summary>
    public struct LargeObject
    {
        public int X;           // X position
        public int Y;           // Y position
        public Tile Tile;       // Tile to start on (must be stored consecutively in the Blocks texture)
        public int TilesX;      // How many tiles wide
        public int TilesY;      // How many tiles high
        public bool Opaque;     // Whether or not to blank the background with World.BackgroundColor first
                                // (or to just draw this over the background tiles)
        private Layout Layout;    // Reference to the World it's in

        /// <summary>
        /// Obtains a constellation motif/poster/art for a given shrine number (1 = Aries, etc.)
        /// </summary>
        public static LargeObject Motif(Layout layout, int x, int y, int number)
        {
            Tile tile = 0;
            switch(number)
            {
                case 0: tile = Tile.AriesA; break;
                case 1: tile = Tile.TaurusA; break;
                case 2: tile = Tile.GeminiA; break;
                case 3: tile = Tile.CancerA; break;
                case 4: tile = Tile.LeoA; break;
                case 5: tile = Tile.ScorpioA; break;
                case 6: tile = Tile.LibraA; break;
                case 7: tile = Tile.VirgoA; break;
                case 8: tile = Tile.SaggitariusA; break;
                case 9: tile = Tile.CapricornA; break;
                case 10: tile = Tile.AquariusA; break;
                case 11: tile = Tile.PiscesA; break;
            }
            return new LargeObject(layout, x, y, tile, 3, 2, true);
        }

        /* Constructor */
        public LargeObject(Layout layout, int x, int y, Tile tile, int tx, int ty, bool opaque)
        {
            X = x;
            Y = y;
            Tile = tile;
            TilesX = tx;
            TilesY = ty;
            Opaque = opaque;
            Layout = layout;
        }

        /// <summary>
        /// Renders the LargeObject to a SpriteBatch at its proper world position
        /// </summary>
        public void Render(SpriteBatch batch)
        {

            if (Opaque)
            {
                batch.FillRectangle(Extensions.ToWorldRect(new Point(X, Y), TilesX, TilesY), Game.World.BackgroundColor);
            }
            for(int y = 0; y < TilesY; y++)
            for(int x = 0; x < TilesX; x++)
            {
                Game.World.RenderTile(batch, X + x, Y + y, Tile++);
            }
        }

    }


    /// <summary>
    /// Represents a door Dana/Adam have opened in the current room; used to
    /// track keys remaining and for the room intro sequence
    /// </summary>
    public struct OpenDoor
    {
        public Point Door;
        public int Room;
        public OpenDoor(Point door, int room)
        {
            Door = door;
            Room = room;
        }
    }


    /// <summary>
    /// Represents either an ObjectPlacement or a SpawnItem;  used to reduce duplicated code
    /// in the editor and other places where Direction, Flags, and Type are being manipulated
    /// in isolation
    /// </summary>
    public interface IObjectDef
    {
        ObjType Type { get; set; }              // Object type
        Heading Direction { get; set; }         // Initial direction
        ObjFlags Flags { get; set; }            // Object flags

        static StringBuilder fsb = new StringBuilder(16);   // For editor use

        public string GetFlags()
        {
            fsb.Clear();
            if (Flags.HasFlag(ObjFlags.AltGraphics)) fsb.Append("w");
            if (Flags.HasFlag(ObjFlags.Clockwise)) fsb.Append("r");
            if (Flags.HasFlag(ObjFlags.DropFairy)) fsb.Append("f");
            if (Flags.HasFlag(ObjFlags.DropKey)) fsb.Append("k");
            return fsb.ToString();
        }

        public string GetSpeed()
        {
            if (Flags.HasFlag(ObjFlags.Faster)) return "}";
            if (Flags.HasFlag(ObjFlags.Fast)) return "{";
            if (Flags.HasFlag(ObjFlags.Slow)) return "(";
            return ")";
        }
    }

    /// <summary>
    /// Used to store placement of objects to instantiate in a level layout
    /// (e.g. initial positions and attributes of enemies, etc.)
    /// </summary>
    public class ObjectPlacement : IObjectDef
    {
        public ObjType Type { get; set; }           // The object type to create
        public int X { get; set; }                  // X position
        public int Y { get; set; }                  // Y position
        public Heading Direction { get; set; }      // Direction (for enemies)
        public ObjFlags Flags { get; set; }         // Flags
        public Point Position                       // Position as a Point
        {
            get => new Point(X, Y);
            set { X = value.X; Y = value.Y; }
        }

        /// <summary>
        /// Creates a GameObject based on the placement data
        /// </summary>
        public GameObject Instantiate(Level level)
        {
            var pt = Position.ToWorld();
            return GameObject.Create(Type, level, Direction, Flags, pt);
        }

        /// <summary>
        /// Builds an ObjectPlacement from legacy NES data storage format
        /// </summary>
        /// <param name="type">NES object type ID</param>
        /// <param name="loc">NES location format (YX byte)</param>
        /// <param name="number">The index of this object into the object list (because
        /// object 0 will always release a fairy when killed, if it's a killable enemy)</param>
        public static ObjectPlacement FromLegacy(byte type, byte loc, int number)
        {
            var o = new ObjectPlacement();
            o.X = (loc & 0x0F) + 1;
            o.Y = loc >> 4;

            if (number == 0) o.Flags |= ObjFlags.DropFairy;

            switch(type)
            {
                case 0x1C: o.Type = ObjType.Fairy; break;
                case 0x1D: o.Type = ObjType.Fairy; o.Flags |= ObjFlags.DropKey; break; // Princess

                case 0x24: o.Type = ObjType.PanelMonster; o.Direction = Heading.Right; break;
                case 0x25: o.Type = ObjType.PanelMonster; o.Direction = Heading.Left; break;
                case 0x26: o.Type = ObjType.PanelMonster; o.Direction = Heading.Up;  break;
                case 0x27: o.Type = ObjType.PanelMonster; o.Direction = Heading.Down;  break;

                case 0x5: o.Type = ObjType.Sparky; o.Direction = Heading.Right; o.Flags |= ObjFlags.Slow; o.Flags = ObjFlags.Clockwise; break;
                case 0x28: o.Type = ObjType.Sparky; o.Direction = Heading.Right; o.Flags = ObjFlags.Clockwise; break;
                case 0x29: o.Type = ObjType.Sparky; o.Direction = Heading.Left; o.Flags = ObjFlags.Clockwise; break;
                case 0x2A: o.Type = ObjType.Sparky; o.Direction = Heading.Up; o.Flags = ObjFlags.Clockwise; break;
                case 0x2B: o.Type = ObjType.Sparky; o.Direction = Heading.Down; o.Flags = ObjFlags.Clockwise; break;
                case 0x2C: o.Type = ObjType.Sparky; o.Direction = Heading.Right; o.Flags |= ObjFlags.Fast | ObjFlags.Clockwise;  break;
                case 0x2D: o.Type = ObjType.Sparky; o.Direction = Heading.Left; o.Flags |= ObjFlags.Fast | ObjFlags.Clockwise; break;
                case 0x2E: o.Type = ObjType.Sparky; o.Direction = Heading.Up; o.Flags |= ObjFlags.Fast | ObjFlags.Clockwise; break;
                case 0x2F: o.Type = ObjType.Sparky; o.Direction = Heading.Down; o.Flags |= ObjFlags.Fast | ObjFlags.Clockwise; break;

                case 0x34: o.Type = ObjType.Ghost; o.Direction = Heading.Right;  break;
                case 0x35: o.Type = ObjType.Ghost; o.Direction = Heading.Right;  break;
                case 0x36: o.Type = ObjType.Ghost; o.Direction = Heading.Left;  break;
                case 0x37: o.Type = ObjType.Ghost; o.Direction = Heading.Left;  break;

                case 0x3C: o.Type = ObjType.Ghost; o.Direction = Heading.Right;  break;
                case 0x3D: o.Type = ObjType.Ghost; o.Direction = Heading.Right;  break;
                case 0x3E: o.Type = ObjType.Ghost; o.Direction = Heading.Left; break;
                case 0x3F: o.Type = ObjType.Ghost; o.Direction = Heading.Left;  break;

                case 0x44: o.Type = ObjType.Ghost; o.Direction = Heading.Right; o.Flags |= ObjFlags.Fast; break;
                case 0x45: o.Type = ObjType.Ghost; o.Direction = Heading.Right; o.Flags |= ObjFlags.Fast; break;
                case 0x46: o.Type = ObjType.Ghost; o.Direction = Heading.Left; o.Flags |= ObjFlags.Fast; break;
                case 0x47: o.Type = ObjType.Ghost; o.Direction = Heading.Left; o.Flags |= ObjFlags.Fast; break;
                case 0x4C: o.Type = ObjType.Ghost; o.Direction = Heading.Right; o.Flags |= ObjFlags.Faster; break;
                case 0x4D: o.Type = ObjType.Ghost; o.Direction = Heading.Right; o.Flags |= ObjFlags.Faster; break;
                case 0x4E: o.Type = ObjType.Ghost; o.Direction = Heading.Left; o.Flags |= ObjFlags.Faster; break;
                case 0x4F: o.Type = ObjType.Ghost; o.Direction = Heading.Left; o.Flags |= ObjFlags.Faster; break;

                case 0x30: o.Type = ObjType.Ghost; o.Direction = Heading.Up;  break;
                case 0x31: o.Type = ObjType.Ghost; o.Direction = Heading.Up;  break;
                case 0x32: o.Type = ObjType.Ghost; o.Direction = Heading.Down;  break;
                case 0x33: o.Type = ObjType.Ghost; o.Direction = Heading.Down;  break;
                case 0x38: o.Type = ObjType.Ghost; o.Direction = Heading.Up; break;
                case 0x39: o.Type = ObjType.Ghost; o.Direction = Heading.Up; break;
                case 0x3A: o.Type = ObjType.Ghost; o.Direction = Heading.Down; break;
                case 0x3B: o.Type = ObjType.Ghost; o.Direction = Heading.Down; break;

                case 0x40: o.Type = ObjType.Ghost; o.Direction = Heading.Up; break;
                case 0x41: o.Type = ObjType.Ghost; o.Direction = Heading.Up; break;
                case 0x42: o.Type = ObjType.Ghost; o.Direction = Heading.Down; break;
                case 0x43: o.Type = ObjType.Ghost; o.Direction = Heading.Down; break;

                case 0x48: o.Type = ObjType.Ghost; o.Direction = Heading.Up; o.Flags |= ObjFlags.Fast; break;
                case 0x49: o.Type = ObjType.Ghost; o.Direction = Heading.Up; o.Flags |= ObjFlags.Fast; break;
                case 0x4A: o.Type = ObjType.Ghost; o.Direction = Heading.Down; o.Flags |= ObjFlags.Fast; break;
                case 0x4B: o.Type = ObjType.Ghost; o.Direction = Heading.Down; o.Flags |= ObjFlags.Fast; break;

                case 0x68: o.Type = ObjType.Dragon; o.Direction = Heading.Right; break;
                case 0x69: o.Type = ObjType.Dragon; o.Direction = Heading.Left; break;
                case 0x6A: o.Type = ObjType.Dragon; o.Direction = Heading.Right; break;
                case 0x6B: o.Type = ObjType.Dragon; o.Direction = Heading.Left; break;
                case 0x6C: o.Type = ObjType.Dragon; o.Direction = Heading.Right; break;
                case 0x6D: o.Type = ObjType.Dragon; o.Direction = Heading.Left; break;
                case 0x6E: o.Type = ObjType.Dragon; o.Direction = Heading.Right; break;
                case 0x6F: o.Type = ObjType.Dragon; o.Direction = Heading.Left; break;

                case 0x70: o.Type = ObjType.Goblin; o.Direction = Heading.Right; break;
                case 0x71: o.Type = ObjType.Goblin; o.Direction = Heading.Left; break;
                case 0x72: o.Type = ObjType.Goblin; o.Direction = Heading.Right; break;
                case 0x73: o.Type = ObjType.Goblin; o.Direction = Heading.Left; break;
                case 0x74: o.Type = ObjType.Goblin; o.Direction = Heading.Right; o.Flags |= ObjFlags.Fast; break;
                case 0x75: o.Type = ObjType.Goblin; o.Direction = Heading.Left; o.Flags |= ObjFlags.Fast; break;
                case 0x76: o.Type = ObjType.Goblin; o.Direction = Heading.Right; o.Flags |= ObjFlags.Fast; break;
                case 0x77: o.Type = ObjType.Goblin; o.Direction = Heading.Left; o.Flags |= ObjFlags.Fast; break;

                case 0x78: o.Type = ObjType.Gargoyle; o.Direction = Heading.Right; break;
                case 0x79: o.Type = ObjType.Gargoyle; o.Direction = Heading.Left; break;
                case 0x7A: o.Type = ObjType.Gargoyle; o.Direction = Heading.Right; break;
                case 0x7B: o.Type = ObjType.Gargoyle; o.Direction = Heading.Left; break;
                case 0x7C: o.Type = ObjType.Gargoyle; o.Direction = Heading.Right; o.Flags |= ObjFlags.Fast; break;
                case 0x7D: o.Type = ObjType.Gargoyle; o.Direction = Heading.Left; o.Flags |= ObjFlags.Fast; break;
                case 0x7E: o.Type = ObjType.Gargoyle; o.Direction = Heading.Right; o.Flags |= ObjFlags.Fast; break;
                case 0x7F: o.Type = ObjType.Gargoyle; o.Direction = Heading.Left; o.Flags |= ObjFlags.Fast; break;

                case 0x80: o.Type = ObjType.Burns; o.Direction = Heading.Left; break;
                case 0x81: o.Type = ObjType.Burns; o.Direction = Heading.Right; break;
                case 0x82: o.Type = ObjType.Burns; o.Direction = Heading.Left; break;
                case 0x83: o.Type = ObjType.Burns; o.Direction = Heading.Right; break;
            }

            return o;

        }
    }

    /// <summary>
    /// Represents an entry in the list of things that can be spawned at a spawn point
    /// </summary>
    public class SpawnItem : IObjectDef
    {
        public ObjType Type { get; set; }        // The type of the object to spawn
        public ObjFlags Flags { get; set; }      // Flags for the object (speed, etc.)       
        public Heading Direction { get; set; }   // Direction
        public int MaxInstances { get; set; }    // Don't spawn if this many of this type of enemy
                                                 // already exist
    }

    /// <summary>
    /// Types of droplets
    /// </summary>

    public enum DropletType
    {
        Blue,
        Frozen,
        Slime,
        Pink,
        LastEditorType = Pink       // Determines at what point the editor wraps back to the item 0
    }

    /// <summary>
    /// Represents a definition for a timed/triggered spawn point (e.g. mirrors)
    /// </summary>
    public class Spawn
    {
        public int X { get; set; }                      // X position
        public int Y { get; set; }                      // Y position
        public List<SpawnItem> SpawnItems { get; set; } // List of things to spawn
                                = new List<SpawnItem>();
        public uint Phase0 { get; set; }                // Bitmask for phase 0 timing
        public uint Phase1 { get; set; }                // Bitmask for phase 1 timing (loops)
        public int TTL { get; set; }                    // TTL to give each spawned object
        public int Current { get; set; }                // Current index to dispense
        public bool Disabled { get; set; }              // Disabled by magic
        public DropletType DropletType { get; set; }    // Type of liquid dispensed
        public int DropletRate { get; set; }           // How many ticks between drops
        public int DropletChance { get; set; } = 100;  // Chance % that we will spawn drop
        public Point Position                           // Position as a Point property
        {
            get => new Point(X, Y);
            set { X = value.X; Y = value.Y; }
        }

        /// <summary>
        /// Retrieves the next item from the spawn list and advances the active slot
        /// </summary>
        public SpawnItem Dispense()
        {
            return SpawnItems[Current++ % SpawnItems.Count];
        }


        public Spawn(int x = 0, int y = 0)
        {
            SpawnItems = new List<SpawnItem>();
            X = x; Y = y; Phase0 = 0; Phase1 = 0;
            TTL = 60;
        }

        public Spawn(Point pos, int ttl)
        {
            SpawnItems = new List<SpawnItem>();
            X = pos.X; Y = pos.Y; Phase0 = 0; Phase1 = 0;
            TTL = ttl;
        }

        public Spawn() { }

    }


    /// <summary>
    /// Represents a 'palette swap' (achieved in pixel shader)
    /// </summary>
    public class Swap
    {
        public Color FromColor { get; set; }
        public Color ToColor { get; set; }
        public string Name { get; set; }

        /// <summary>
        /// Creates a palette swap
        /// </summary>
        /// <param name="from">Original color</param>
        /// <param name="to">New color</param>
        /// <param name="name">Swap name (used to remove it)</param>
        public Swap(Color from, Color to, string name)
        {
            FromColor = from;
            ToColor = to;
            Name = name;
        }

        /// <summary>
        /// Registers a swap and activates it
        /// </summary>
        /// <param name="s"></param>
        public static void Register(Swap s)
        {
            if (s is null) throw new ArgumentNullException(nameof(s));
            if (s.Name is null) throw new ArgumentNullException(nameof(s.Name));
            Remove(s.Name);
            Game.Swaps.Add(s);
            effect = null;      // Clear cache
        }

        /// <summary>
        /// Registers a swap and activates it
        /// </summary>
        public static void Register(Color from, Color to, string name) => Register(new Swap(from, to, name));

        /// <summary>
        /// Removes one more active swaps.
        /// </summary>
        /// <param name="name">The name of the swap to remove,  it may end with * to indicate all swaps beginning with
        /// a prefix (e.g. vapor_*) </param>
        public static void Remove(string name)
        {
            foreach(var x in Game.Swaps)
            {
                if (x.Name == name || name.EndsWith("*") && x.Name.StartsWith(name.TrimEnd('*')))
                {
                    Game.Swaps.Remove(x);
                }
            }
            effect = null;      // Clear cache
        }

        /// <summary>
        /// Removes a registered swap
        /// </summary>
        public static void Remove(Swap s) => Game.Swaps.Remove(s);

        // Stores the effect
        private static Effect effect;

        /// <summary>
        /// Gets the configured Effect described by the sum of all registered palette swaps
        /// </summary>
        public static Effect GetEffect()
        {
            if (effect is null) BuildEffect();
            return effect;
        }

       
        private static void BuildEffect()
        {
            var froms = Game.Swaps.Select(x => x.FromColor.ToVector4()).ToArray();
            var tos = Game.Swaps.Select(x => x.ToColor.ToVector4()).ToArray();

            effect = Game.Assets.Swap;
            effect.Parameters["FromColors"].SetValue(froms);
            effect.Parameters["ToColors"].SetValue(tos);
            effect.Parameters["NumColors"].SetValue(Game.Swaps.Count);
        }

    }

    /// <summary>
    /// Valid camera tracking modes
    /// </summary>
    public enum CameraMode
    {
        Locked,
        Unlocked,
        LockedUntilNear
    }

    /// <summary>
    /// Represents a triggered change of camera boundaries
    /// </summary>
    public class Resize
    {
        public Rectangle Trigger;
        public Rectangle NewBounds;
    }


    /// <summary>
    /// Flags for GameObjects, SpawnItems, and ObjectPlacements
    /// </summary>
    [Flags]
    public enum ObjFlags
    {
        None        = 0,        // Default value
        Slow        = 1,        // New shit:  Slower-than-normal enemy
        Fast        = 2,        // Faster-than-normal enemy
        AltGraphics = 4,        // Alternate skin (e.g. Ghost->Wyvern, Goblin->Wizard)
        DropFairy   = 8,        // Drops a fairy on death
        Clockwise   = 0x10,     // Determines crawl direction for things that cling to walls
        DropKey     = 0x20,     // New shit:  Drops a key on death 
        Faster      = 0x40      // Some enemy types in NES had a turbo speed
    }

    /// <summary>
    /// Method in which an enemy dies (determines animation)
    /// </summary>
    public enum KillType
    {
        Drop,       // Fell to death
        Timeout,    // TTL expired in transit XD
        Fire,       // Fireball/explosion jar
        Transform,   // Transmogrified into a fairy
        Freeze,
        Decay
    }

    /// <summary>
    /// Types of GameObjects
    /// </summary>
    public enum ObjType
    {
        None,               // Set to None to delete object
        Fairy,     
        Sparky,
        Ghost,              // and Nuel and Wyvern
        Dragon, 
        Goblin,             // and Wizard
        Gargoyle,
        Demonhead,
        Salamander,         // a.k.a. "Saramandor"
        Burns,
        PanelMonster,   
        Dana,               // Ideally only put one of these on a level!
        MightyBombJack, 
        Fireball,           // Shot by Dana
        Effect,             // Arbitrary visual effects
        EnemyFireball,      // Shot by panel monsters and gargoyles
        Droplet,
        LastEditType = MightyBombJack,      // The last object selectable in the editor
    }

    /// <summary>
    /// Relative directions used in the game
    /// </summary>
    [Flags]
    public enum Heading
    {
        None = 0,       // Default value
        Right = 1,      // 90 degrees anti-clockwise from down
        Left = 2,       // 90 degrees clockwise from down
        Up = 4,         // The direction opposing gravity
        Down = 8        // The direction of gravity
    }

}
