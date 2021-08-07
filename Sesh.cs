using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;

namespace SKX
{
    /// <summary>
    /// Represents a game session;  stored on disk as a saved game, but also holds
    /// other data that persists between levels.
    /// </summary>
    public class Sesh
    {
        /// <summary>
        /// The number of the save slot (arbitrary)
        /// </summary>
        public int SaveSlot { get; set; } = 1;    
        
        /// <summary>
        /// The story this game is using
        /// </summary>
        public Story Story { get; set; } = Story.Classic;    
        
        /// <summary>
        /// Starting difficulty
        /// </summary>
        public Difficulty Difficulty { get; set; } = Difficulty.Normal;

        /* Dana/Adam's Properties */
        /// <summary>
        /// Whether or not Dana (false) or Adam (true) is currently selected
        /// </summary>
        public bool Apprentice { get; set; } = false;     // Adam or Dana?
        
        public int DanaLives { get; set; } = 3;
        public int AdamLives { get; set; } = 3;
        public int Score { get; set; } = 0;
        public int LastShrine { get; set; } = 0;
        public int Fairies { get; set; }
        public int RoomNumber { get; set; } = 1;
        public int RoomAttempt { get; set; } = 0;
        /// <summary>
        /// List of inventory items obtained
        /// </summary>
        public List<InventoryItem> Inventory { get; set; } = new List<InventoryItem>();
        /// <summary>
        /// Global list of all spells executed
        /// </summary>
        public List<SpellExecuted> SpellsExecuted { get; set; } = new List<SpellExecuted>();
 
       

        public DateTime SaveTime { get; set; }
        public bool ScrollDisabled { get; set; } = false;
        /// <summary>
        /// This flag (set via Spell) causes a normal exit door
        /// to use the "next secret room number" as its destination
        /// </summary>
        public bool SecretExit { get; set; } = false;
        public int FireballRange { get; set; } = Game.MinFireballRange;
        /// <summary>
        /// How many simultaneous fireballs can be launched
        /// </summary>
        public int MaxFireballs { get; set; } = 1;

        internal bool NoPoints = false;     // Don't tally Dana's life on the next
                                            // thank you dana screen

        

        /* Characters' Scrolls */
        public int DanaScrollSize { get; set; } = 3;
        public int AdamScrollSize { get; set; } = 3;
        public List<Cell> DanaScroll { get; } = new List<Cell>();
        public List<Cell> AdamScroll { get; } = new List<Cell>();

        /* GDV Stats (both characters) */
        public int PickupCount { get; set; }
        public int KillCount { get; set; }
        public int RoomsCleared { get; set; }
        public int HardRoomsCleared { get; set; }
        public Progress Progress { get; set; }
        public int TotalFairies { get; set; }

        /* Things used between rooms */
        /// <summary>
        /// Used internally to determine if a demo is playing back organically
        /// (from the title screen).
        /// </summary>
        [JsonIgnore] public bool DemoPlayback { get; set; }
        public bool FastStars { get; set; }
        public bool SkipThankYou { get; set; }
        public Point WarpTo { get; set; }
        public int ReturnToRoom { get; set; } = 0;              // Used after a "hidden"
        public Point ReturnToWarpTo { get; set; } = default;    // Used after a "hidden"
        public bool BonusRoomQueued { get; set; } = false;      // Whether to do a hidden after next room
        public List<OpenDoor> DoorsOpened { get; set; } = new List<OpenDoor>();     // Doors opened in the current room

        /* Things stored for the level editor */
        public bool EditorCameraLocked { get; set; } = true;
        public Cell EditorCell { get; set; }
        public EditMode LastEditMode { get; set; } = EditMode.Layout;

        /* Computed properties */
        [JsonIgnore]
        public string PlayerName => Apprentice ? "ADAM" : "DANA";
        [JsonIgnore]
        public List<Cell> ScrollItems => Apprentice ? AdamScroll : DanaScroll;
        [JsonIgnore]
        public int ScrollSize
        {
            get => Apprentice ? AdamScrollSize : DanaScrollSize;
            set { if (Apprentice) AdamScrollSize = value; else DanaScrollSize = value; }
        }
        /// <summary>
        /// Gets or sets how many lives the current character has
        /// </summary>
        public int Lives
        {
            get => Apprentice ? AdamLives : DanaLives;
            set
            {
                if (Apprentice) AdamLives = value; else DanaLives = value;
            }
        }

        /* Constructors */
        public Sesh() { }

        public Sesh(Difficulty difficulty, Story story, int slot)
        {
            Game.LogInfo($"Session created ({difficulty}, {story}, slot {slot})");

            Difficulty = difficulty;
            Story = story;
            SaveSlot = slot;
            RoomNumber = 1;
            if (story == Story.Test) RoomNumber = 0;
            switch (difficulty)
            {
                case Difficulty.Normal:
                    break;
                case Difficulty.Easy:
                    Lives = 5;
                    FireballRange = 20;
                    ScrollSize = 5;
                    break;
                case Difficulty.Hard:
                    Lives = 3;
                    ScrollSize = 1;
                    break;
            }
        }

       
        /// <summary>
        /// Clears anything non-persistent from the current rooms inventory
        /// </summary>
        public void ClearTempInventory()
        {
            Inventory.RemoveAll(x => !Layout.IsPersistent(x.Type) && x.FromRoom == RoomNumber);
        }

        /// <summary>
        /// Loads a room layout for the current game type, room, etc.
        /// </summary>
        private Layout BuildLayout()
        {
            Layout l;
            Game.LogInfo($"Building layout for room {RoomNumber:X}{Story.ToStoryID()}"); 

            // 1. Look to see if a room file exists.  Use this instead of the 
            // internal room if it exists
            var file = $"room_{RoomNumber:X}{Story.ToStoryID()}.json";
            if (file.FileExists())
            {
                Game.LogInfo($"Loading room {RoomNumber:X}{Story.ToStoryID()} from {file}");
                return Layout.LoadFile(file, null);
            }

            // 2. Attempt to load the room from the embedded bundle
            if (Game.Assets.Bundle != null)
            { 
                l = Game.Assets.Bundle.LoadRoom(Story, RoomNumber);
                Game.LogInfo($"Loaded room {RoomNumber:X}{Story.ToStoryID()} from bundle");
                if (l != null) return l;
            }

            // 3. If the ROM is available import the level from the ROM
            file = @"c:\nes\sk02.nes";
            var hex = RoomNumber.ToString("X");
            if (!int.TryParse(hex, out int rm))
            {
                rm = 0;
            }
            if (file.FileExists()) {
                l = Layout.ImportLegacy(file, rm, null);
                l.RoomNumber = RoomNumber;
                Game.LogInfo($"Loaded room {RoomNumber:X}{Story.ToStoryID()} from legacy ROM");
                return l;
            }

            // 4. Return a blank layout lol
            Game.LogInfo($"Cannot find a layout for {RoomNumber:X}{Story.ToStoryID()}");
            return Layout.BlankLayout(RoomNumber, null);

        }

        /// <summary>
        /// Sets the room number for the hidden level and sets the "return to" room
        /// to be the previous value
        /// </summary>
        public void SetUpHiddenRoom()
        {
            Game.LogInfo($"Entering hidden room");
            ReturnToRoom = RoomNumber;
            ReturnToWarpTo = WarpTo;
            switch (Story)
            {
                case Story.Classic:
                    RoomNumber = 0;
                    break;
                case Story.Plus:
                    RoomNumber = 0;
                    break;
                case Story.SKX:
                    RoomNumber = 0x101;
                    break;
            }
        }

        /// <summary>
        /// Checks if a specific spell has executed (at any time during the game)
        /// </summary>

        public bool HasSpell(SpellExecuted spell)
        {
            if (spell.FromRoom == 0)
            {
                return SpellsExecuted.Any(s => s.SpellID == spell.SpellID);
            }
            else return SpellsExecuted.Contains(spell);
        }

        /// <summary>
        /// Checks if Dana/Adam's inventory contains an item.  Set the items FromRoom to 0 to ignore the room number 
        /// it was obtained in.
        /// </summary>
        public int InventoryContains(InventoryItem i)
        {
            if (i.FromRoom < 1)
            {
                return Inventory.Count(x => x.Type == i.Type);
            } else
            {
                return Inventory.Count(x => x.Type == i.Type && x.FromRoom == i.FromRoom);
            }
        }

        /// <summary>
        /// Checks if a specific item type is in Dana's inventory for the current room
        /// </summary>
        public bool HasItemFromThisRoom(Cell c)
        {
            return Inventory.Any(x => x.Type == c && x.FromRoom == RoomNumber);
        }

        /// <summary>
        /// Checks if a specific item is in Dana's inventory for a specific room
        /// </summary>
        public bool HasItem(Cell c, int roomNumber)
        {
            return Inventory.Any(x => x.Type == c && x.FromRoom == roomNumber);
        }

        /// <summary>
        /// Checks if a specific item is in Dana's inventory (any room)
        /// </summary>
        public bool HasItem(Cell c)
        {
            return Inventory.Any(x => x.Type == c);
        }

        /// <summary>
        /// Gets an inventory item of a specific type for a specific room
        /// </summary>
        public InventoryItem GetItem(Cell c, int roomNumber)
        {
            return Inventory.FirstOrDefault(x => x.Type == c && x.FromRoom == roomNumber);
        }

        /// <summary>
        /// Calculates Dana's GDV.  Algorithm RE'ed by https://github.com/pellsson
        /// 
        /// Warning: If this becomes at all an expensive calculation
        /// it should be cached someplace because right now it's being calculated on every frame
        /// of the game over screen.
        /// </summary>
        public int CalculateGDV()
        {

            int specials = Inventory.Count(x => x.Type == Cell.PageSpace || x.Type == Cell.PageTime);
            int seals = Inventory.Count(x => x.Type == Cell.Seal);
            int saved_princess = Progress.HasFlag(Progress.SavedPrincess) ? 1 : 0;
            int has_solomons_key = Progress.HasFlag(Progress.FoundSolomonsKey) ? 1 : 0;

            var exp = ((1 + specials + (TotalFairies / 10) + saved_princess) * 2 + RoomsCleared + seals) * 2 + HardRoomsCleared;       

            return 47
                   + has_solomons_key 
                   + exp / 8
                   + Math.Min(5, Score / 100000);

        }

        /// <summary>
        /// Used when the room changes
        /// </summary>
        public void OnNextLevel()
        {
            SecretExit = false;
            RoomAttempt = 0;
            Save();
        }

        /// <summary>
        /// Used when the continue code occurs at a Game Over
        /// </summary>
        public void OnContinue()
        {
            AdamLives = 3;
            DanaLives = 3;
            DoorsOpened.RemoveAll(x => x.Room == RoomNumber);

            if (Game.IsClassic && RoomNumber > 0x41)
            {
                Inventory.RemoveAll(x => x.FromRoom >= 0x41);
                RoomNumber = 0x41;
            } else
            {
                Inventory.RemoveAll(x => x.FromRoom == RoomNumber);
            }
        }

        /// <summary>
        /// Used after a player completes a level or dies,  if the player was on a
        /// hidden stage,  they are returned to the next room they were going to go to
        /// normally.
        /// </summary>
        public void CheckReturn()
        {

            if (ReturnToRoom > 0)
            {                
                // Remove all objects obtained in the hidden
                Inventory.RemoveAll(x => x.FromRoom == RoomNumber);

                RoomNumber = ReturnToRoom;
                WarpTo = ReturnToWarpTo;
                ReturnToRoom = 0;
                ReturnToWarpTo = default;
            }
        }

        /// <summary>
        /// Resets the layout for a specific room after a death or upon entering
        /// the level editor
        /// </summary>
        public void ResetLayout(Level l)
        {
            var layout = BuildLayout();
            l.Layout = layout;
            l.Layout.World = l;
            l.Layout.EnsureSize();
            SecretExit = false;
        }

        /// <summary>
        /// Checks if Dana has the shrine for the current room in his inventory.
        /// </summary>
        public bool HasThisRoomsShrine()
        {
            return Inventory.Any(x => x.FromRoom == RoomNumber 
            && Layout.IsShrine(x.Type));   // Only constellation and planetary signs
        }

        /// <summary>
        /// Increments the room tally metrics (used in GDV)
        /// </summary>
        public void AddRoomTally(int roomNum)
        {
            RoomsCleared++;
            if (roomNum > 0x10 && ReturnToRoom == 0) HardRoomsCleared++;
        }

        /// <summary>
        /// Builds a Level World based on the current room number
        /// </summary>
        public Level BuildLevel(Demo demo = null)
        {
            var layout = BuildLayout();
            var l = new Level(layout.Width, layout.Height, demo);
            l.Layout = layout;
            l.Layout.World = l;
            l.Layout.EnsureSize();
            if (l.Layout.Shrine >= 0) LastShrine = l.Layout.Shrine;
            return l;
        }

        /// <summary>
        /// Builds a Level World based on a provided Layout
        /// </summary>
        public Level BuildLevel(Layout layout)
        {
            var l = new Level(layout.Width, layout.Height);
            l.Layout = layout;
            l.Layout.World = l;
            l.Layout.EnsureSize();
            if (l.Layout.Shrine >= 0) LastShrine = l.Layout.Shrine;
            return l;
        }

        /// <summary>
        /// Saves the game (to disk)
        /// </summary>
        public void Save()
        {
            SaveTime = DateTime.Now;

            try
            {
                Game.SaveOptions();     // Save options, notably play timer
            } catch (Exception ex)
            {
                Game.LogError($"Failed to save options: {ex}");
            }

            if (!this.SaveFile($"save_{SaveSlot}{Game.StoryID}.json"))
            {
                Game.LogError($"Failed to save to save_{SaveSlot}{Game.StoryID}.json");
                Game.StatusMessage("ERROR SAVING GAME");
            }
            else
                Game.LogInfo($"Session saved");
        }

        /// <summary>
        /// Adds an item to Dana's inventory
        /// </summary>
        public void AddInventory(Cell type, Point fromCell, int? roomNumber = null)
        {
            roomNumber ??= RoomNumber;
            Game.LogInfo($"Session: Added {type} to inventory (room {roomNumber})");
            Inventory.Add(new InventoryItem()
            {
                FromRoom = roomNumber.Value,
                Type = type,
                FromCell = fromCell
            });



        }

        /// <summary>
        /// Builds the Load Game menu
        /// </summary>
        public static Menu BuildLoadGameMenu()
        {
            var m = new Menu("SELECT SAVED GAME");

            var games = GetSavedGames();
            if (games is null)
            {
                m.MenuItems.Add(new MenuItem("ERROR LOADING SAVE SLOTS"));
            } 
            else if (games.Count() == 0)
            {
                m.MenuItems.Add(new MenuItem("NO SAVED GAMES FOUND"));
            }
            else
            {
                foreach(var g in games.OrderByDescending(x => x.SaveTime))
                {
                    var mi = new MenuItem();
                   
                    mi.Text = g.GetSlotText();

                    mi.Action = () =>
                    {
                        Game.Load(g);
                    };
                    m.MenuItems.Add(mi);

                }
            }
            m.UpdateBounds();
            return m;
        }

        /// <summary>
        /// Gets all of the saved games in the game's directory
        /// </summary>
        public static IEnumerable<Sesh> GetSavedGames()
        {
            var l = new List<Sesh>();
            try
            {
                var files = System.IO.Directory.GetFiles(Game.AppDirectory, "save*.json");
                foreach(var f in files)
                {
                    try
                    {
                        var s = f.LoadFile<Sesh>();
                        if (s != null) l.Add(s);
                    }
                    catch { continue; }
                }
            } catch (Exception ex)
            {
                Game.LogError($"Failed to enumerate save files: {ex}");
                return null;
            }
            return l;
        }

        /// <summary>
        /// Gets the text for the save slots in the Menu system
        /// </summary>
       public string GetSlotText()
        {
            var rm = ReturnToRoom > 0 ? ReturnToRoom : RoomNumber;
            var name = Game.Assets.Bundle.GetRoomName(Story, rm) ?? $"ROOM {rm:X}";
            return $"*{SaveSlot} {(Apprentice ? "a" : "d")}x{Lives} {Story.ToStoryID()} {name}";
        }

        /// <summary>
        /// Gets the status of the specified save slot, used for the
        /// main menu
        /// </summary>
        /// <returns>Returns null if the slot is empty</returns>
        public string GetSlotStatus(int slot, Story story)
        {
            var file = $"save_{slot}{story.ToStoryID()}.json";
            if (!file.FileExists()) return null;
            try
            {
                var load = file.LoadFile<Sesh>();
                if (load != null)
                {
                    return load.GetSlotText();
                }

            } catch (Exception ex)
            {
                Game.LogError($"Failed to get save slot {slot} status: {ex}");
                return "FILE ERROR"; 
            }
            return null;
        }

    }

    /// <summary>
    /// When Dana collects certain items they will go into his inventory
    /// so that game logic later on can check for them
    /// </summary>
    public struct InventoryItem
    {
        public Cell Type;
        public Point FromCell;
        public int FromRoom;

        public override string ToString()
        {
            return $"{FromRoom,2}: {Type.ToString().ToUpper()} {FromCell.X} {FromCell.Y}";
        }
    }

    /// <summary>
    /// When a spell is executed it goes into Dana's history so it can be checked by
    /// game logic later on
    /// </summary>
    public struct SpellExecuted
    {
        public int FromRoom;
        public int SpellID;

        public override string ToString()
        {
            return $"{FromRoom,2}: SPELL {SpellID}";
        }
    }

    /// <summary>
    /// Progress flags used by the ending and GDV
    /// </summary>
    [Flags]
    public enum Progress
    {
        None = 0,
        SavedPrincess = 1,
        FoundSolomonsKey = 2,
        TrainedAdam = 4
    }



}
