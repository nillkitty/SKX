using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MonoGame.Extended;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace SKX.Editor
{

    /// <summary>
    /// Base class for level editor modes
    /// </summary>
    public abstract class EditorMode
    {
        public LevelEditor Editor;                                      // A reference to the editor 
        public Level Level;                                             // A reference to the level 
        public Layout Layout => Level.Layout;                           // Shorthand for Level.Layout
        public Sesh Sesh => Game.Sesh;                                  // A reference to the Sesh 
        public EditorMode CurrentMode => Level.Editor.Mode;             // Current editor mode
        public List<Command> Commands { get; } = new List<Command>();   // Key Commands from Modes
        private List<Command> MyCommands { get; } = new List<Command>(); // Key Commands global
        public List<Tidbit> Tidbits { get; } = new List<Tidbit>();      // Non key binding help items
        public virtual bool OnEscPressed() { return false; }

        public EditorMode(LevelEditor editor)
        {
            Editor = editor;
            Level = editor.Level;

            // Editor Modes
            MyCommands.Add(new Command("EDITOR MODES", levelMode, "LAYOUT MODE", Keys.L, false, true, false));
            MyCommands.Add(new Command("EDITOR MODES", bgMode, "BG MODE", Keys.B, false, true, false));
            MyCommands.Add(new Command("EDITOR MODES", sfgMode, "SFG MODE", Keys.F, false, true, false));
            MyCommands.Add(new Command("EDITOR MODES", objMode, "OBJECT MODE", Keys.O, false, true, false));
            MyCommands.Add(new Command("EDITOR MODES", spawnsMode, "SPAWNS MODE", Keys.S, false, true, false));
            MyCommands.Add(new Command("EDITOR MODES", keysMode, "KEYS MODE", Keys.K, false, true, false));
            MyCommands.Add(new Command("EDITOR MODES", doorsMode, "DOORS MODE", Keys.D, false, true, false));
            MyCommands.Add(new Command("EDITOR MODES", magicMode, "MAGIC MODE", Keys.M, false, true, false));
            MyCommands.Add(new Command("EDITOR MODES", camsMode, "CAMERA MODE", Keys.C, false, true, false));
            MyCommands.Add(new Command("EDITOR MODES", tileMode, "CELL/TILE SELECT", Keys.OemTilde, false, false));

            MyCommands.Add(new Command("SHOW/HIDE LAYERS", toggleBg, "TOGGLE BG", Keys.D1, false, true));
            MyCommands.Add(new Command("SHOW/HIDE LAYERS", toggleFg, "TOGGLE FG", Keys.D2, false, true));
            MyCommands.Add(new Command("SHOW/HIDE LAYERS", toggleObj, "TOGGLE OBJECTS", Keys.D3, false, true));
            MyCommands.Add(new Command("SHOW/HIDE LAYERS", toggleSfg, "TOGGLE SFG", Keys.D4, false, true));


            MyCommands.Add(new Command("ROOM MANAGEMENT", reloadLevel, "RELOAD ROOM", Keys.F1, false, false));
            MyCommands.Add(new Command("EDITOR CONTROL", borders, "TOGGLE DEF BORDERS", Keys.F2, false, false));
            MyCommands.Add(new Command("ROOM MANAGEMENT", SaveFile, "SAVE TO FILE", Keys.F3, false, false));
            MyCommands.Add(new Command("ROOM MANAGEMENT", DelFile, "DELETE FILE", Keys.F3, true, false));
            MyCommands.Add(new Command("ROOM MANAGEMENT", SwapRooms, "SWAP ROOMS", Keys.F4, false, false));
            MyCommands.Add(new Command("ROOM MANAGEMENT", SaveRoomAs, "SAVE ROOM AS", Keys.F4, true, false));
            MyCommands.Add(new Command("ROOM MANAGEMENT", CopyToStory, "COPY ROOM", Keys.F4, false, true));
            MyCommands.Add(new Command("ROOM MANAGEMENT", chgShrine, "SHRINE +", Keys.F9, false, false));
            MyCommands.Add(new Command("ROOM MANAGEMENT", chgShrine, "SHRINE -", Keys.F9, true, false));
            MyCommands.Add(new Command("EDITOR CONTROL", ToggleCameraLock, "CAM LOCK", Keys.F10, false, false));
            MyCommands.Add(new Command("EDITOR CONTROL", Resume, "RESUME PLAY", Keys.F11, false, false));
            MyCommands.Add(new Command("EDITOR CONTROL", Test, "RESTART ROOM", Keys.F11, true, false));
            MyCommands.Add(new Command("ROOM MANAGEMENT", roomUp, "NEXT ROOM", Keys.F12, false, false));
            MyCommands.Add(new Command("ROOM MANAGEMENT", roomDown, "PREV ROOM", Keys.F12, true, false));
            MyCommands.Add(new Command("EDITOR CONTROL", ScrollUp, "SCROLL UP", Keys.OemQuotes, false, false, true));
            MyCommands.Add(new Command("EDITOR CONTROL", ScrollDown, "SCROLL DN", Keys.OemSemicolon, false, false, true));
            MyCommands.Add(new Command("EDITOR CONTROL", midClick, "MID CLICK", Keys.Space, false, false, true));
            MyCommands.Add(new Command("EDITOR CONTROL", showCoords, "SHOW COORDS", Keys.OemComma, false, false, false));
            MyCommands.Add(new Command("ROOM MANAGEMENT", GoToRoom, "GO TO ROOM", Keys.G, false, false, false));

            Tidbits.Add(new Tidbit("EDITOR CONTROL", "C+SHIFT", "REVEAL SECRETS"));

            void toggleBg() { Editor.RenderBG = !Editor.RenderBG; Game.StatusMessage("TOGGLED BG LAYER"); }
            void toggleFg() { Editor.RenderFG = !Editor.RenderFG; Game.StatusMessage("TOGGLED FG LAYER"); }
            void toggleObj() { Editor.RenderObj = !Editor.RenderObj; Game.StatusMessage("TOGGLED OBJ LAYER"); }
            void toggleSfg() { Editor.RenderSFG = !Editor.RenderSFG; Game.StatusMessage("TOGGLED SFG LAYER"); }
            void showCoords() { Editor.ShowCoordinates = !Editor.ShowCoordinates; }
            
            void magicMode() { Cleanup(); Editor.ChangeMode(EditMode.Magic); }
            void levelMode() { Cleanup(); Editor.ChangeMode(EditMode.Layout); }
            void bgMode() {  Cleanup(); Editor.ChangeMode(EditMode.Background); }
            void sfgMode() { Cleanup(); Editor.ChangeMode(EditMode.SuperForeground); }
            void objMode() {  Cleanup(); Editor.ChangeMode(EditMode.Objects); }
            void keysMode() { Cleanup(); Editor.ChangeMode(EditMode.Keys); }
            void doorsMode() { Cleanup(); Editor.ChangeMode(EditMode.Doors); }
            void spawnsMode() { Cleanup(); Editor.ChangeMode(EditMode.Spawns); }
            void camsMode() { Cleanup(); Editor.ChangeMode(EditMode.Cameras); }
            void reloadLevel() { Level.EditReload(false); }
            void chgShrine() { CtrlValue("SHRINE", 0, 41, () => Layout.Shrine, x => Layout.Shrine = x); } 
            void roomUp() { GoToRoom(Layout.RoomNumber + 1); }
            void roomDown() { GoToRoom(Layout.RoomNumber - 1); }
            void midClick() { MiddleClick(false); }
            void borders()
            {
                Layout.DefaultBorders = !Layout.DefaultBorders; Game.StatusMessage("DEFAULT BORDER " +
                    (Layout.DefaultBorders ? "ON" : "OFF"));
            }
            void tileMode() 
            {    
                if (Editor.CurrentMode == EditMode.TileSelect) return;

                Cleanup(); 
                if (Editor.Mode is BackgroundMode || Editor.Mode is SuperForegroundMode)
                {
                    Editor.CellSelectMode.Title = "SELECT TILE";
                    Editor.CellSelectMode.TileSelect = true;
                } else
                {
                    Editor.CellSelectMode.Title = "SELECT CELL CONTENTS";
                    Editor.CellSelectMode.TileSelect = false;
                }
                Editor.ChangeMode(EditMode.TileSelect); 
            }

        }

        // Virtual methods to be implemented by
        // derived editor mode classes
        public virtual void LeftClick(bool ctrl) { }        // The user left clicked
        public virtual void RightClick(bool ctrl) { }       // The user right clicked
        public virtual void MiddleClick(bool ctrl) { }      // The user middle clicked
        public virtual void MouseDrag() { }                 // The user is dragging
        public virtual void ScrollUp() { }                  // Mouse wheel up
        public virtual void ScrollDown() { }                // Mouse wheel down
        public virtual void OnEnterMode() { }               // Mode activated
        public virtual void OnExitMode() { }                // Mode exited
        public virtual void Controls(bool ctrl, bool shift) { } // Called to process input
        public virtual void Render(SpriteBatch batch) { }       // Called to draw things
        public virtual void HUD(SpriteBatch batch) { }          // Called to draw HUD
        public virtual bool HUDClick(bool ctrl) { return false; }   // Called when HUD is clicked

        // Constants for HUD
        protected const int line_d1 = 0;        // Top most line,  usually empty except debug info
        protected const int line_d2 = 8;        // Next line, usually empty except debug info
        protected const int line_1 = 16;        // First HUD line (e.g. SCORE, LIFE, etc.)
        protected const int line_2 = 24;        // Second HUD line (values)

        // Switch editor to specified room
        protected void GoToRoom(int room)
        {

            if (room < 0) return;           // Ignore invalid rooms

            OnExitMode();                   // Exit the current mode (because the whole editor will
                                            // be replaced with a new one)
            Game.Sesh.RoomNumber = room;    // Set the room number
            var l = Game.Sesh.BuildLevel(); // Build the level
            Game.World = l;                 // Switch the world
            l.Edit(false);          // Go right into edit
        }

        // Cleans up orphaned data structures that point to
        // cells that no longer contain keys/doors
        protected virtual void Cleanup()
        {
            CleanupKeys();
            CleanupDoors();
        }

        // Removes KeyInfos for cells that are no longer keys
        protected virtual void CleanupKeys()
        {
            Layout.Keys.RemoveAll(k =>
                !(Layout.IsKeyAtAll(Layout[k.KeyPosition]) &&
                Layout.IsDoorAtAll(Layout[k.DoorPosition])));

        }

        // Removes DoorInfos for cells that are no longer doors
        protected virtual void CleanupDoors()
        {
            Layout.Doors.RemoveAll(d => !Layout.IsDoorAtAll(Layout[d.Position]));
        }

        // Cycles through valid headings
        public static void ChangeDirection(IObjectDef item)
        {
            if (item is null) return;
            item.Direction = item.Direction switch
            {
                Heading.Up => Heading.Down,
                Heading.Right => Heading.Left,
                Heading.Left => Heading.Up,
                _ => Heading.Right,
            };
        }

        public void GoToRoom() { Game.InputPromptHex("GO TO ROOM", GoToRoom); }
        public void SaveFile() { Cleanup(); SaveToFile(); }
        public void DelFile() { DeleteFile(); }
        public void Test() 
        {
            OnExitMode(); 
            Level.Restart(true); 
        }

        // Adds the object manipulation commands to the command set for the current mode
        protected void AddObjectCommands()
        {

            // Controls
            Commands.Add(new Command("OBJECT MANIPULATION", speed, "TOGGLE SPEED", Keys.S, false, false));
            Commands.Add(new Command("OBJECT MANIPULATION", speed0, "SET SPEED (", Keys.D1, true, false));
            Commands.Add(new Command("OBJECT MANIPULATION", speed1, "SET SPEED )", Keys.D2, true, false));
            Commands.Add(new Command("OBJECT MANIPULATION", speed2, "SET SPEED {", Keys.D3, true, false));
            Commands.Add(new Command("OBJECT MANIPULATION", speed3, "SET SPEED }", Keys.D4, true, false));

            Commands.Add(new Command("OBJECT MANIPULATION", dirl, "DIR: LEFT", Keys.D5, true, false));
            Commands.Add(new Command("OBJECT MANIPULATION", dirr, "DIR: RIGHT", Keys.D6, true, false));
            Commands.Add(new Command("OBJECT MANIPULATION", diru, "DIR: UP", Keys.D7, true, false));
            Commands.Add(new Command("OBJECT MANIPULATION", dird, "DIR: DOWN", Keys.D8, true, false));
            Commands.Add(new Command("OBJECT MANIPULATION", dirn, "DIR: NONE", Keys.D0, true, false));


            Commands.Add(new Command("OBJECT MANIPULATION", fairy, "TOGGLE f FLAG", Keys.F, false, false));
            Commands.Add(new Command("OBJECT MANIPULATION", key, "TOGGLE k FLAG", Keys.K, false, false));
            Commands.Add(new Command("OBJECT MANIPULATION", clockwise, "TOGGLE r FLAG", Keys.C, false, false));
            Commands.Add(new Command("OBJECT MANIPULATION", altgr, "TOGGLE w FLAG", Keys.A, false, false));
            Commands.Add(new Command("OBJECT MANIPULATION", flip, "FLIP ALL SPARKIES", Keys.F, false, true));


            // Control handlers
            void speed0() => SetSpeed(0);
            void speed1() => SetSpeed(1);
            void speed2() => SetSpeed(2);
            void speed3() => SetSpeed(3);
            void dirl() => SetDirection(Heading.Left);
            void dirr() => SetDirection(Heading.Right);
            void diru() => SetDirection(Heading.Up);
            void dird() => SetDirection(Heading.Down);
            void dirn() => SetDirection(Heading.None);
            void speed() { var g = GetSpeed(); SetSpeed((g + 1) % 4); }
            void fairy() => ToggleFlag(ObjFlags.DropFairy);
            void key() => ToggleFlag(ObjFlags.DropKey);
            void clockwise() => ToggleFlag(ObjFlags.Clockwise);
            void altgr() => ToggleFlag(ObjFlags.AltGraphics);

            void flip()
            {
                int n = 0;
                foreach (var o in Layout.Objects)
                {
                    if (o.Type != ObjType.Sparky) continue;
                    if (o.Flags.HasFlag(ObjFlags.Clockwise))
                    {
                        o.Flags &= ~ObjFlags.Clockwise;
                    }
                    else
                    {
                        o.Flags |= ObjFlags.Clockwise;
                    }
                    n++;
                }
                Game.StatusMessage($"{n} SPARKIES FLIPPED.");
            }
        }

        IEnumerable<HelpItem> GetHelpItems()
        {
            foreach (var y in Tidbits) { yield return y; }
            foreach (var y in Commands) { yield return y; }
            foreach (var y in MyCommands) { yield return y; }
        }

        /// <summary>
        /// Builds all room files into a single constellation space archive resource
        /// </summary>
        public static string Build(bool merge)
        {
            var b = Bundle.Build(merge);
            string data = b.Store();
            string dir = @"c:\proj\sk00\skx\content\";
            string file = "cspace.bndl";
            if (Directory.Exists(dir))
            {
                file = Path.Combine(dir, file);
            } else 
            {
                file = Path.Combine(Game.AppDirectory, file);
            }
            try
            {
                File.WriteAllText(file, data);
                Game.StatusMessage("SAVED BUNDLE TO CSPACE.BNDL");
            } catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());
                Game.StatusMessage("SAVE FAILED");
            }
            Game.Pause = false;
            return data;
        }

        // Show procedurally-generated editor help 
        public void EditorHelp()
        {

            var sb = new StringBuilder();
            var byCat = from c in GetHelpItems()
                        where !c.HideFromHelp
                        group c by c.Category into g
                        select g;

            // Header
            sb.Append("EDITOR HELP:\n");

            // For each help category
            foreach (var cat in byCat)
            {
                // Display the category heading
                sb.Append($"[c=ff888888]_____________________________[c=ffffffff]\n");
                sb.Append($"{cat.Key}\n");

                // For each help item (by unique name) in the category
                var byName = from t in cat group t by t.Name into gg select gg;
                foreach (var cmdGroup in byName)
                { 
                    
                    // Create the key binding string
                    string k = string.Join("[c=ffff00] OR [c=ffff]", cmdGroup.Select(z => z.Binding.Replace("[", "[[")).ToArray());

                    // Write the line
                    sb.Append($"[c=ffff]{k,8}[c=0] {cmdGroup.First().Name}\n");
                }
                sb.Append("\n");
            }

            // Set the game's help text and it will display it
            Game.SetHelpText(sb.ToString());

        }

        /// <summary>
        /// Process input for the current level editor mode
        /// </summary>
        public void Controls()
        {

            bool shift = Control.KeyboardState.IsKeyDown(Keys.LeftShift) | Control.KeyboardState.IsKeyDown(Keys.RightShift);
            bool ctrl = Control.KeyboardState.IsKeyDown(Keys.LeftControl) | Control.KeyboardState.IsKeyDown(Keys.RightControl);

            // Handle "?" for the help
            if (Control.KeyPressed(Keys.OemQuestion) && shift)
            {
                EditorHelp();
                return;     // Don't let anything else handle the '?'
            } else if (Control.KeyPressed(Keys.Escape))
            {
                Game.SetHelpText(null);
            }

            foreach(var c in MyCommands.Union(Commands))
            {
                if (c.Key == Keys.None) continue;
                if (!Control.KeyPressed(c.Key)) continue;
                if (c.Shift != shift) continue;
                if (c.Ctrl != ctrl) continue;
                c.Handler?.Invoke();
            }

            // Per mode controls
            Controls(ctrl, shift);

        }
        

        /// <summary>
        /// Handles manipulation of an integer value such that
        /// Key: Increase value
        /// Shift + key: Decrease value
        /// Ctrl + key: Prompt for value input
        /// Alt + key: Copy value
        /// Alt + shift + key: Paste value
        /// </summary>
        /// <param name="name"></param>
        /// <param name="min"></param>
        /// <param name="max"></param>
        /// <param name="getter"></param>
        /// <param name="setter"></param>
        public void CtrlValue(string name, int min, int max, Func<int> getter, Action<int> setter)
        {
            bool ctrl = Control.ControlKeyDown();
            bool shift = Control.ShiftKeyDown();
            bool alt = Control.AltKeyDown();

            int val = getter.Invoke();

            if (alt && shift)
            {
                // Paste value
                var r = Editor.Paste<int>($"PASTED {name}");
                if (r.ok)
                {
                    val = r.value;
                    setter?.Invoke(val);
                }

            }
            else if (alt)
            {
                // Copy value
                Editor.Copy(val, $"COPIED {name}");

            }
            else if (ctrl)
            {
                // Input value
                Game.InputPromptNumber($"SET {name}:",
                    callback: setter,
                    defaultValue: getter.Invoke().ToString());
            }
            else if (shift)
            {
                // Decrease value
                val--;
                if (val < min) val = min;
                setter?.Invoke(val);
                Game.StatusMessage($"{name}: {val}");

            }
            else
            {
                // Increase value
                val++;
                if (val > max) val = min;
                setter?.Invoke(val);
                Game.StatusMessage($"{name}: {val}");
            }

        }


        void FixRoom()
        {
            // Convert the decimal room numbers to hex equivalents

            var rm = Layout.RoomNumber.ToString(); // interpret the decimal string representation as hex to fix room
            var nrm = int.Parse(rm, System.Globalization.NumberStyles.HexNumber);
            Layout.RoomNumber = nrm;
            SaveToFile();

        }

        public void SaveRoomAs()
        {
            // Get the new room number
            Game.InputPromptHex("ENTER ROOM NUMBER:", SaveAsRoom);
        }

        void SaveAsRoom(int i)
        {
            if (i < 0 || i > 0x2FF)
            {
                Game.StatusMessage("INVALID ROOM NUMBER");
                return;
            }

            Layout.RoomNumber = i;
            SaveToFile();
            GoToRoom(i);

        }

        public void CopyToStory()
        {
            // Get the new room number
            Game.InputPrompt("ENTER STORY: C X T OR P", CopyRoom);
        }

        void CopyRoom(string story)
        {
            if (string.IsNullOrEmpty(story)) return;
            Game.InputPromptHex("ENTER ROOM NUMBER:", n => CopyRoom(story, n));
        }

        void CopyRoom(string story, int i)
        {
            char c = story.ToLower()[0];

            if (i < 0 || i > 0x2FF)
            {
                Game.StatusMessage("INVALID ROOM NUMBER");
                return;
            }

            Layout.Story = c.ToStory();
            Layout.RoomNumber = i;
            SaveToFile(c);
            Game.StatusMessage($"ROOM MIGRATED TO {c.ToString().ToUpper()}{i:X}");

        }

        public void SwapRooms()
        {
            // Get the room number
            Game.InputPromptHex("ENTER ROOM NUMBER:", SwapRoom);

        }
        void SwapRoom(int i)
        {
            if (i < 0 || i > 256)
            {
                Game.StatusMessage("INVALID ROOM NUMBER");
                return;
            }

            var file = $"room_{Layout.RoomNumber:X}{Sesh.Story.ToStoryID()}.json";
            var nfile = $"room_{i}{Sesh.Story.ToStoryID()}.json";

            if (!nfile.FileExists())
            {
                Game.StatusMessage($"ROOM {i} FILE NOT FOUND");
                return;
            }

            // Load the other level into RAM
            var lev = new Level(17, 17);
            var l2 = Layout.LoadFile(nfile, lev);

            l2.RoomNumber = Layout.RoomNumber;      // Set its new dest room num

            DeleteFile();               // Delete the current file
            Layout.RoomNumber = i;      // Update our room #
            SaveToFile();               // Write our room to the target

            l2.SaveFile(file);          // Save the other room to our original room
            GoToRoom(i);
            Game.StatusMessage("SWAP COMPLETE");
        }

        protected void DeleteFile()
        {
            try
            {
                var path = $"room_{Sesh.RoomNumber}{Sesh.Story.ToStoryID()}.json";
                if (Game.AppDirectory != null) path = System.IO.Path.Combine(Game.AppDirectory, path);
                System.IO.File.Delete(path);
                Game.StatusMessage("FILE DELETED");
            }
            catch
            {
                Sound.Wince.Play();
                Game.StatusMessage("ERROR DELETING FILE");
            }
        }

        public void SaveToFile(char storyID = default)
        {
            if (storyID != default)
                Layout.Story = storyID.ToStory();
            else
                Layout.Story = Game.Sesh.Story;
            Layout.SaveToFile(storyID);
        }

        public virtual void Resume()
        {
            OnExitMode();
            Cleanup();
            if (Editor.ObjectsChanged) Layout.ReloadObjects();
            Level.PreRun(true);
            Game.UpdateTitle();
        }

        // Gets the current IObjectDef (an ObjectPlacement in objects mode or a SpawnItem in spawns mode)
        public IObjectDef GetObjDef()
        {
            if (Editor.Mode is SpawnsMode)
            {
                if (Editor.SpawnsMode.SelectedSpawn is null) return null;
                if (Editor.SpawnsMode.SelectedSpawn.SpawnItems.Count == 0) return null;
                if (Editor.SpawnsMode.SelectedSpawnSlot >= Editor.SpawnsMode.SelectedSpawn.SpawnItems.Count) return null;

                if (Editor.SpawnsMode.SelectedSpawnSlot != -1)
                    return Editor.SpawnsMode.SelectedSpawn.SpawnItems[Editor.SpawnsMode.SelectedSpawnSlot];

                return null;
            }
            else
            {
                return Editor.ObjectsMode.SelectedOP;
            }
        }

        protected int GetSpeed()
        {
            IObjectDef item = GetObjDef();
            if (item is null) return -1;

            if (item.Flags.HasFlag(ObjFlags.Slow)) return 0;
            if (item.Flags.HasFlag(ObjFlags.Faster)) return 3;
            if (item.Flags.HasFlag(ObjFlags.Fast)) return 2;
            return 1;
        }

        protected void SetDirection(Heading dir)
        {
            IObjectDef item = GetObjDef();
            if (item is null) return;
            item.Direction = dir;
            FixFlags(item);
        }

        protected void SetSpeed(int speed)
        {
            IObjectDef item = GetObjDef();
            if (item is null) return;


            item.Flags &= ~(ObjFlags.Fast | ObjFlags.Faster | ObjFlags.Slow);
            switch (speed)
            {
                case 0: item.Flags |= ObjFlags.Slow; break;
                case 1: break;
                case 2: item.Flags |= ObjFlags.Fast; break;
                case 3: item.Flags |= ObjFlags.Faster; break;
            }

            FixFlags(item);
        }

        protected void ToggleFlag(ObjFlags flag)
        {
            var item = GetObjDef();
            if (item == null) return;

            if (item.Flags.HasFlag(flag))
            {
                item.Flags &= ~flag;
            }
            else
            {
                item.Flags |= flag;
            }
            FixFlags(item);
        }

        public void FixFlags(IObjectDef item)
        {
            Editor.ObjectsChanged = true;
            if (item is null) return;

            bool canUp = false;
            bool canDown = false;
            bool canNone = false;
            bool canClockwise = false;
            bool canAlt = false;

            switch (item.Type)
            {
                case ObjType.None:
                    break;
                case ObjType.Fairy:
                case ObjType.Dragon:
                case ObjType.Gargoyle:
                case ObjType.Salamander:
                case ObjType.Dana:
                case ObjType.MightyBombJack:
                    break;
                case ObjType.Goblin:
                case ObjType.Droplet:
                case ObjType.Demonhead:
                    canAlt = true;
                    break;
                case ObjType.Sparky:
                    canNone = true;
                    canAlt = true;
                    canClockwise = true;
                    canUp = canDown = true;
                    break;
                case ObjType.Ghost:
                    canUp = canDown = true;
                    canAlt = true;
                    break;
                case ObjType.PanelMonster:
                    canUp = canDown = true;
                    canClockwise = true;
                    break;
                case ObjType.Fireball:
                    canUp = canDown = true;
                    canClockwise = true;
                    break;
                case ObjType.Burns:
                    canUp = canDown = true;
                    break;
            }

            if (!canUp)
            {
                if (item.Direction == Heading.Up)
                {
                    item.Direction = Heading.Right;
                }
            }
            if (!canDown)
            {
                if (item.Direction == Heading.Down)
                {
                    item.Direction = Heading.Right;
                }
            }
            if (!canAlt)
            {
                item.Flags &= ~ObjFlags.AltGraphics;
            }
            if (!canClockwise)
            {
                item.Flags &= ~ObjFlags.Clockwise;
            }
            if (!canNone)
            {
                if (item.Direction == Heading.None)
                    item.Direction = Heading.Right;
            }
        }

        public virtual void ClearLevel()
        {
            for (int y = 1; y < Level.TileHeight - 1; y++)
                for (int x = 1; x < Level.TileWidth - 1; x++)
                {
                    Layout[x, y] = Cell.Empty;
                }
            Layout.Doors.Clear();
            Layout.Keys.Clear();
            Layout.Objects.Clear();
            Game.StatusMessage("LEVEL CLEARED");

        }

        protected void ToggleCameraLock()
        {
            Sesh.EditorCameraLocked = !Sesh.EditorCameraLocked;
            Game.StatusMessage($"CAMERA {(Sesh.EditorCameraLocked ? "LOCKED" : "UNLOCKED")}");
            if (Sesh.EditorCameraLocked && Level.TileWidth <= 17 && Level.TileHeight <= 14)
            {
                Game.CameraPos = Layout.CameraStart;
            }
        }

    }

    public abstract class HelpItem
    {
        public string Name { get; set; }
        public bool HideFromHelp { get; set; }
        public string Category { get; set; }
        public virtual string Binding { get; set; }
    }

    public class Tidbit : HelpItem
    {      

        public Tidbit(string category, string binding, string name)
        {
            Category = category;
            Binding = binding;
            Name = name;
        }

    }

    public class Command : HelpItem
    {

        public Action Handler { get; set; }

        public Keys Key { get; set; }
        public bool Shift { get; set; }
        public bool Ctrl { get; set; }

        public Command(string category, Action handler, string name, Keys key, bool shift, bool ctrl, 
            bool hideHelp = false)
        {
            Category = category;
            Handler = handler;
            Name = name;
            Key = key;
            Shift = shift;
            Ctrl = ctrl;
            HideFromHelp = hideHelp;

        }

        public override string Binding
        {
            get
            {
                string k = Key.ToKeyName(Shift);
                if (Shift) k = $"S+{k}";
                if (Ctrl) k = $"C+{k}";
                return k;
            }
            set => throw new NotSupportedException();
        }

    }

    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public class EditorCommandAttribute : Attribute
    {
        public string Category { get; set; }
        public string Name { get; set; }
        public Keys Key { get; set; }
        public bool Shift { get; set; }
        public bool Ctrl { get; set; }

        public int? ParamValue { get; set; }

        public EditorCommandAttribute(string category, string name, Keys key, bool shift, bool ctrl)
        {
            Category = category;
            Name = name;
            Key = key;
            Shift = shift;
            Ctrl = ctrl;

        }

        public EditorCommandAttribute(string category, string name, Keys key, bool shift, bool ctrl, 
            int paramValue) : this(category, name, key, shift, ctrl)
        {
            ParamValue = paramValue;
        }
    }

}
