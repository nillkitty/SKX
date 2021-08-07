using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MonoGame.Extended;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using EditorMode = SKX.Editor.EditorMode;

namespace SKX
{
    // .. inside the Level class
    public partial class Level
    {

        public LevelEditor Editor;          // A reference to the level editor

        void EditInit()                     // Called to initialize editing
        {
            Editor = new LevelEditor(this);
            Game.Sesh.EditorCameraLocked = (TileWidth <= 17 && TileHeight <= 14);
        }


        /// <summary>
        /// Enters level editor mode
        /// </summary>
        /// <param name="preserve">Whether or not to preserve the existing level and object layout (true)
        /// or to reload it from disk</param>
        public void Edit(bool preserve)
        {

            if (Editor.Mode is null) EditInit();    // Init the editor if needed
            
            Game.Sesh.RoomAttempt = 0;          // Don't delete any items from the room
            if (!preserve) EditReload(false);   // Reload the level layout if appropriate
            RoutineTimer = 0;                   // Clear any timer we have going on
            State = LevelState.Edit;            // Set the level state
            Game.Swaps.Clear();                 // Turn off any Chromakey bob's doing in the control room
            VapourMode = false;                 // Turn off the fuzzy feel
            Game.Pause = false;                 // Unpause the game
            Game.Menu = null;                   // Clear menu
            Game.UpdateTitle();
            Sound.StopAll();
            Life = Layout.StartLife ?? 10_000;  // Reset life
            Ticks = 0;                          // Reset ticks
            RunTicks = 0;                       // Reset run ticks
            FrozenCracked = default;            // Reset frozen tile
            FrozenCount = 0;

            // Force a new control update so that the F11 key being pressed isn't immediately
            // picked up by the level editor control routine (which would just immediately resume
            // gameplay)
            Control.Update();
            Editor.Mode.OnEnterMode();
        }

        // Called by Level.Update when we're in the editor
        void EditUpdate()
        {
            Editor.Mouse();     // Update editor mouse
            Editor.Controls();  // Update editor controls
        }

        // Reloads everything fresh for edit mode
        public void EditReload(bool and_run)
        {
            Ticks = 0;
            RunTicks = 0;
            ClearObjects();
            ResetSpawns();
            RenderEnemies = true;
            Game.Sesh.Inventory.RemoveAll(x => x.FromRoom == Game.Sesh.RoomNumber);
            Game.Sesh.ResetLayout(this);
            Game.Sesh.DoorsOpened.Clear();
            Init();
       
            if (and_run)
            {
                Layout.ReloadObjects();
            }
            State = and_run ? LevelState.Loading : LevelState.Edit;
        }

    }

    /// <summary>
    /// The level editor;  manages the various edit modes and other
    /// editor-wide properties
    /// </summary>
    public class LevelEditor
    {
        public Level Level;                             // Reference to the Level
        public Layout Layout => Level.Layout;           // Short for Level.Layout
        public EditorMode Mode;                         // The current editor mode instance
        public EditorMode LastMode;                     // The mode before this one

        public Tile SelectedTile = Tile.Empty;             // Selected art tile

        // Layer enable/
        public bool RenderBG = true;
        public bool RenderFG = true;
        public bool RenderObj = true;
        public bool RenderSFG = true;

        // Statics
        internal static string ClipboardJson;
        internal static dynamic ClipboardObject;

        // Mode instances
        internal Editor.LayoutMode LevelMode;
        internal Editor.BackgroundMode BackgroundMode;
        internal Editor.ObjectsMode ObjectsMode;
        internal Editor.KeysMode KeysMode;
        internal Editor.DoorsMode DoorsMode;
        internal Editor.SpawnsMode SpawnsMode;
        internal Editor.MagicMode MagicMode;
        internal Editor.CamerasMode CamerasMode;
        internal Editor.CellSelect CellSelectMode;
        internal Editor.SuperForegroundMode SuperForeground;

        // Mouse stuff
        public Point MouseCell;                            // Mouse pos cell
        public Point MouseWorld;                           // Mouse pos world
        public Point MouseScreenCell;                      // Mouse screen pos

        public Point LastClick;                            // Last cell to be left clicked
        public bool Dragging;                              // User is dragging
        public bool ObjectsChanged = false;                // Any of the objects changed?
        public static Rectangle EditSweetSpot = new Rectangle(32, 32, 256 - 64, 240 - 64);  // Area outside which panning happens
        public static int EditPanSpeed = 2;                    // Panning speed
        public bool ShowCoordinates;


        // HUD Y-offsets
        const int line_d1 = 0;        // Top most line,  usually empty except debug info
        const int line_d2 = 8;        // Next line, usually empty except debug info
        const int line_1 = 16;        // First HUD line (e.g. SCORE, LIFE, etc.)
        const int line_2 = 24;

        // Preset background colors
        public static Color[] BGColors = new Color[] 
        {
                    new Color(86, 29, 0),           // From NES - Brown
                    new Color(0, 128, 136),         // From NES - Blue
                    new Color(149, 31, 169),        // From NES - Pink/Purple
                    new Color(0, 68, 0),            // From NES - Green
                    new Color(116, 116, 116),       // From NES - Gray
                    new Color(0, 46, 85),           // New - Dark blue
                    new Color(0, 57, 36),           // New - Dark green
                    new Color(103, 44, 3),          // New - Chocolate
                    new Color(68, 68, 68),          // New - Dark gray
        };

        // All controls are handled by the current editor mode;   global editor controls are
        // handled in the EditorMode base class
        public void Controls()
        {
            if (Game.HelpText != null && Game.HelpTime > Game.DebounceTicks)
            {
                if (Control.KeyPressed(Keys.Up))
                {
                    Game.ScrollHelp(-16);
                }
                else if (Control.KeyPressed(Keys.Down))
                {
                    Game.ScrollHelp(16);
                }
                else if (Control.GetAnyKey().HasValue)
                {
                    Game.SetHelpText(null);
                    return;
                }            
            }
            
            Mode.Controls();
        }

        // The Editor handles the mouse and sends discrete LeftClick/RightClick/MiddleClick, ScrollUp,
        // ScrollDown, etc. events to the current EditorMode.
        public void Mouse()
        {
            var ctrl = Control.KeyboardState.IsKeyDown(Keys.LeftControl) 
                        || Control.KeyboardState.IsKeyDown(Keys.RightControl);

            // Cell detection
            if (Control.MouseInWindow)
            {
                // Translate the mouse into the world
                var scroll = new Point();
                if (Mode is Editor.CellSelect cm)
                {
                    scroll.Y = cm.ScrollOffset;
                }
                MouseWorld = Control.MousePos - Game.CameraOffset + scroll + Game.CameraPos;
                MouseCell = MouseWorld.ToCell();
                MouseScreenCell = (Control.MousePos - Game.CameraOffset).ToCell();
            }
            else
            {
                MouseWorld = new Point(-1, -1);
                MouseCell = MouseWorld;
                MouseScreenCell = MouseWorld;
            }

            // Pan and tilt
            if (!EditSweetSpot.Contains(Control.MousePos) && !Game.Sesh.EditorCameraLocked)
            {
                // Don't pan/tilt if the mouse isn't near the edge of the window
                if (!Control.MouseInWindow)
                {
                    if (Control.MousePos.X < -32) return;
                    if (Control.MousePos.Y < -32) return;
                    if (Control.MousePos.X > Level.WorldWidth + 32) return;
                    if (Control.MousePos.Y > Level.WorldHeight + 32) return;
                }

                // Don't pan in tile select palette
                if (!(Mode is Editor.CellSelect))
                {

                    if (Control.MousePos.X < 32 && Game.CameraPos.X > 0)
                    {
                        // Pan left
                        Game.CameraPos.X = Math.Max(0, Game.CameraPos.X - EditPanSpeed);
                    }
                    if (Control.MousePos.Y < 32 && Game.CameraPos.Y > 0)
                    {
                        // Pan up
                        Game.CameraPos.Y = Math.Max(0, Game.CameraPos.Y - EditPanSpeed);
                    }
                    if (Control.MousePos.X > 256 - 32 && Game.CameraPos.X + Game.CameraSize.X < Level.WorldWidth)
                    {
                        // Pan right
                        Game.CameraPos.X = Math.Min(Level.WorldWidth, Game.CameraPos.X + EditPanSpeed);
                    }
                    if (Control.MousePos.Y > 240 - 32 && Game.CameraPos.Y + Game.CameraSize.Y < Level.WorldHeight)
                    {
                        // Pan down
                        Game.CameraPos.Y = Math.Min(Level.WorldHeight, Game.CameraPos.Y + EditPanSpeed);
                    }
                }
            }

            // Handle scrolling
            if (Game.HelpText != null)
            {
                Game.ScrollHelp((Control.LastMouseState.ScrollWheelValue - Control.MouseState.ScrollWheelValue) / 8);
            }
            else
            {
                if (Control.WheelUp)
                {
                    Mode.ScrollUp();
                }
                else if (Control.WheelDown)
                {
                    Mode.ScrollDown();
                }
            }

            // Handle clicking
            if (Control.MouseInWindow)
            {
                if (Control.MouseLeft)
                {


                    if (EditHUDClick(ctrl)) return;
                    if (Control.MousePos.Y > Game.CameraOffset.Y)
                    {
                        Mode.LeftClick(ctrl);
                        LastClick = MouseCell;

                    }
                }

                if (Control.MouseRight 
                        && Control.MousePos.Y > Game.CameraOffset.Y 
                        && MouseCell.X >= 0)

                    Mode.RightClick(ctrl);

                if (Control.MouseMiddle) Mode.MiddleClick(ctrl);
            }

            if (Dragging)
            {
                if (Control.MouseState.LeftButton == ButtonState.Released)
                {
                    Dragging = false;
                }
                else
                {
                    Mode.MouseDrag();
                }
            }

        }

        public bool OnEscPressed()
        {
            return Mode.OnEscPressed();
        }



        // Some modes (spawns mode) needs to know when the HUD is being clicked on
        bool EditHUDClick(bool ctrl)
        {
            // If the mouse isn't in the window, its not in the HUD
            if (!Control.MouseInWindow) return true;
            // If the mouse is in the game area, it's not in the HUD
            if (Control.MousePos.Y > Game.CameraOffset.Y) return false;

            return Mode.HUDClick(ctrl);
        }

        /// <summary>
        /// Called by Level.Render to render the editor scene.
        /// </summary>
        public void Render(SpriteBatch batch)
        {
            if (Mode is null) Mode = LevelMode;         // If we have no mode, then it's level mode
            Mode.Render(batch);                         // Have the current editor mode render the scene

            var reveal = Control.ShiftKeyDown() && Control.ControlKeyDown();
            if (RenderObj && !(reveal)) 
                RenderObjectPlacements(batch);  // We render object placements here since many modes
                                                           // need them

            // We also are in charge of drawing the red square around the currently hovering cell
            if (MouseCell.X != -1)
            {
                if (Mode is Editor.CellSelect)
                {
                    batch.DrawRectangleScreen(MouseScreenCell.ToWorld() + Game.CameraOffset, 
                        new Size2(16, 16), Color.Red, 1);
                }
                else
                {
                    var scroll = new Point();
                    if (Mode is Editor.CellSelect cm)
                    {
                        scroll.Y = cm.ScrollOffset;
                    }
                    batch.DrawRectangle(new RectangleF(MouseCell.ToWorld() - scroll, new Size2(16, 16)), Color.Red, 1);
                }
            }

            // And the mouse coordinates if they're enabled
            if (ShowCoordinates)
            {
                batch.DrawOutlinedString($"{MouseCell.X} {MouseCell.Y}", new Point(8, 200), Color.White, 1);
            }

        }

        /// <summary>
        /// Used to render the layout's object placements in edit mode
        /// </summary>
        public void RenderObjectPlacements(SpriteBatch batch)
        {
            // Don't render any objects in these modes
            if (Mode is Editor.BackgroundMode) return;
            if (Mode is Editor.CellSelect) return;

            // Render starting position
            if (Layout.AdamStart != default)
                Level.RenderTile(batch, Layout.AdamStart.X, Layout.AdamStart.Y, Tile.Adam, Color.White * 0.7f,
                    Layout.AdamDirection ? SpriteEffects.FlipHorizontally : SpriteEffects.None);

            if (Layout.DanaStart != default)
                Level.RenderTile(batch, Layout.DanaStart.X, Layout.DanaStart.Y, Tile.Dana, Color.White * 0.7f, 
                                Layout.DanaDirection ? SpriteEffects.FlipHorizontally : SpriteEffects.None);

            
            foreach (var o in Layout.Objects)
            {
                // Render object tile
                RenderObjectPlacement(batch, o);
                if (Mode is Editor.ObjectsMode)
                {
                    // bounding rectangles in object mode
                    var color = (o == ObjectsMode.SelectedOP) ? Color.Yellow : Color.White;
                    batch.DrawRectangle(new RectangleF(o.Position.ToWorld().ToVector2(), new Size2(16, 16)),
                            color, 1);
                }
            }
        }

        private void RenderObjectPlacement(SpriteBatch batch, ObjectPlacement o)
        {
            // Render object tile
            var or = GetObjTile(o);
            var color = Color.White;
            if (!(Mode is Editor.LayoutMode || Mode is Editor.ObjectsMode)) color *= 0.5f;
            Level.RenderTile(batch, o.X, o.Y, or.Tile, color, or.Effect);
        }

        /// <summary>
        /// Gets the current editor mode name text
        /// </summary>
        public string ModeName => Mode is Editor.SuperForegroundMode ? "SUPERFG" : Mode.GetType().Name.Replace("Mode", "").ToUpper();

        /// <summary>
        /// Called by Level.RenderHUD to draw the editor HUD
        /// </summary>
        public void HUD(SpriteBatch batch)
        {
            // Draw the current mode name and room number
            batch.DrawString($"MODE: {ModeName}", new Point(8, line_d1), Color.White);
            
            // These don't fit
            if (!(Mode is Editor.SpawnsMode))
            {
                batch.DrawString($"RM: {Layout.RoomNumber:X}{Layout.Story.ToStoryID()}", new Point(8 * 18, line_d1), Color.White);
                Level.RenderTileWorld(batch, 232, line_d1, (Tile)Layout.GetShrine(Layout.Shrine), Color.White);
            }

            // Now have the current EditorMode render its bits
            Mode.HUD(batch);

        }

        /// <summary>
        /// Gets the appropriate tile to use in the editor for a given object type, direction, and flags
        /// </summary>
        public static ObjectPlacementRender GetObjTile(IObjectDef o)
        {

            var or = new ObjectPlacementRender();
            switch (o.Type)
            {
                case ObjType.Dana:
                    or.Tile = Tile.Dana;
                    if (o.Direction == Heading.Left) or.Effect = SpriteEffects.FlipHorizontally;
                    break;
                case ObjType.None:
                    or.Tile = Tile.Empty;
                    break;
                case ObjType.Fairy:
                    if (o.Flags.HasFlag(ObjFlags.DropKey))
                        or.Tile = Tile.PrincessA;
                    else
                        or.Tile = Tile.FairyA;
                    if (o.Direction == Heading.Left) or.Effect = SpriteEffects.FlipHorizontally;
                    break;
                case ObjType.Sparky:
                    or.Tile = o.Flags.HasFlag(ObjFlags.AltGraphics) ? Tile.SparkyAA : Tile.SparkyB;
                    break;
                case ObjType.Ghost:
                    if (o.Flags.HasFlag(ObjFlags.AltGraphics))
                        or.Tile = Tile.WyvernFlyA;
                    else
                        or.Tile = Tile.GhostA;
                    if (o.Direction == Heading.Up || o.Direction == Heading.Down)
                        or.Tile = Tile.NuelA;

                    if (o.Direction == Heading.Left) or.Effect = SpriteEffects.FlipHorizontally;
                    break;
                case ObjType.Dragon:
                    or.Tile = Tile.DragonB;
                    if (o.Direction == Heading.Left) or.Effect = SpriteEffects.FlipHorizontally;
                    break;
                case ObjType.Goblin:
                    or.Tile = o.Flags.HasFlag(ObjFlags.AltGraphics) ? Tile.WizardA : Tile.GoblinA;
                    if (o.Direction == Heading.Left) or.Effect = SpriteEffects.FlipHorizontally;
                    break;
                case ObjType.Gargoyle:
                    or.Tile = Tile.GargA;
                    if (o.Direction == Heading.Left) or.Effect = SpriteEffects.FlipHorizontally;
                    break;
                case ObjType.Demonhead:
                    or.Tile = o.Flags.HasFlag(ObjFlags.AltGraphics) ? Tile.Demon2B: Tile.DemonB;
                    if (o.Direction == Heading.Left) or.Effect = SpriteEffects.FlipHorizontally;
                    break;
                case ObjType.Salamander:
                    or.Tile = Tile.SalamanderC;
                    if (o.Direction == Heading.Left) or.Effect = SpriteEffects.FlipHorizontally;
                    break;
                case ObjType.Burns:
                    or.Tile = Tile.BurnB;
                    if (o.Direction == Heading.Left) or.Tile = Tile.BurnC;
                    if (o.Direction == Heading.Up) or.Tile = Tile.BurnG;
                    if (o.Direction == Heading.Down) or.Tile = Tile.BurnZ;
                    break;
                case ObjType.PanelMonster:
                    switch (o.Direction)
                    {
                        case Heading.Left: or.Tile = Tile.PanelA; or.Effect = SpriteEffects.FlipHorizontally; break;
                        case Heading.Down: or.Tile = Tile.PanelAY; break;
                        case Heading.Up: or.Tile = Tile.PanelAY; or.Effect = SpriteEffects.FlipVertically; break;
                        default: or.Tile = Tile.PanelA; break;
                    }
                    break;
                case ObjType.MightyBombJack:
                    or.Tile = Tile.MightyA;
                    break;
                case ObjType.Droplet:
                    or.Tile = Tile.Droplet;
                    break;
                case ObjType.Fireball:
                    switch (o.Direction)
                    {
                        case Heading.Right: or.Tile = Tile.Fireball; break;
                        case Heading.Left: or.Tile = Tile.Fireball; or.Effect = SpriteEffects.FlipHorizontally; break;
                        case Heading.Up: or.Tile = Tile.FireballY; break;
                        case Heading.Down: or.Tile = Tile.FireballY; or.Effect = SpriteEffects.FlipVertically; break;
                    }
                    break;
                case ObjType.Effect:
                    or.Tile = Tile.Sparkle;
                    break;
            }

            return or;
        }

        // Constructor
        public LevelEditor(Level level)
        {
            Level = level;
            LevelMode = new Editor.LayoutMode(this);
            BackgroundMode = new Editor.BackgroundMode(this);
            ObjectsMode = new Editor.ObjectsMode(this);
            KeysMode = new Editor.KeysMode(this);
            DoorsMode = new Editor.DoorsMode(this);
            SpawnsMode = new Editor.SpawnsMode(this);
            MagicMode = new Editor.MagicMode(this);
            CamerasMode = new Editor.CamerasMode(this);
            CellSelectMode = new Editor.CellSelect(this);
            SuperForeground = new Editor.SuperForegroundMode(this);
            Mode = LevelMode;
            ChangeMode(Game.Sesh.LastEditMode);
        }

        public void Copy<T>(T item, string msg = null)
        {
            ClipboardObject = item;
            ClipboardJson = Extensions.ToJSON(item);
            if (msg != null) Game.StatusMessage(msg);
        }

        public (T value, bool ok) Paste<T>(string msg = null) where T : new()
        {
            if (ClipboardObject == null && ClipboardJson == null)
            {
                Game.StatusMessage("CLIPBOARD EMPTY");
                return (default, false);
            }

            if (ClipboardObject is T t) 
            {
                if (msg != null) Game.StatusMessage(msg);
                return (t, true);
            }

            Game.StatusMessage($"TYPE MISMATCH: {ClipboardObject.GetType().Name.ToUpper()} - {typeof(T).Name.ToUpper()}");
            return (default, false);
        }

        public bool ClipboardHasContent => ClipboardJson != null;


        /// <summary>
        /// Changes editor modes
        /// </summary>
        public void ChangeMode(EditMode mode)
        {

            // Clear any help text
            Game.SetHelpText(null);

            // If we're already in the mode (like bittersweet symphony), do nothing
            if (mode == CurrentMode) return;

            // Inform the old mode
            Mode?.OnExitMode();

            // Switch it
            LastMode = Mode;
            Mode = (mode) switch
            {
                EditMode.Background => BackgroundMode,
                EditMode.Doors => DoorsMode,
                EditMode.Keys => KeysMode,
                EditMode.Layout => LevelMode,
                EditMode.Magic => MagicMode,
                EditMode.Objects => ObjectsMode,
                EditMode.Spawns => SpawnsMode,
                EditMode.TileSelect => CellSelectMode,
                EditMode.Cameras => CamerasMode,
                EditMode.SuperForeground => SuperForeground,
                _ => LevelMode,
            };

            Game.Sesh.LastEditMode = mode;
            Game.UpdateTitle();

            // Notify new mode
            Mode.OnEnterMode();
        }

        public EditMode CurrentMode => GetMode(Mode);
        public EditMode GetMode(EditorMode mode) => mode switch
        {
            Editor.BackgroundMode _ => EditMode.Background,
            Editor.DoorsMode _ => EditMode.Doors,
            Editor.KeysMode _ => EditMode.Keys,
            Editor.LayoutMode _ => EditMode.Layout,
            Editor.MagicMode _ => EditMode.Magic,
            Editor.ObjectsMode _ => EditMode.Objects,
            Editor.SpawnsMode _ => EditMode.Spawns,
            Editor.CellSelect _ => EditMode.TileSelect,
            Editor.CamerasMode _ => EditMode.Cameras,
            Editor.SuperForegroundMode _ => EditMode.SuperForeground,
            _ => EditMode.Layout
        };


    }


    /// <summary>
    /// Enum of valid editor modes
    /// </summary>
    public enum EditMode
    {
        Layout,
        Background,
        Objects,
        Keys,
        Doors,
        Spawns,
        Magic,
        TileSelect,
        Cameras,
        SuperForeground
    }

    public struct ObjectPlacementRender
    {
        public Tile Tile;
        public SpriteEffects Effect;
    }

}
