using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SKX
{
    /// <summary>
    /// This static class manages input.
    /// </summary>
    public static class Control
    {

        // Current and previous state buffers for the game pad, keyboard, and mouse
        public static GamePadState GamePadState, WorldGamePadState;
        public static GamePadState LastGamePadState, LastWorldGamePadState;
        public static KeyboardState KeyboardState, WorldKeyboardState;
        public static KeyboardState LastKeyboardState, LastWorldKeyboardState;
        public static MouseState MouseState, LastMouseState;

        // Reference to the game instance
        public static Game Game;
        
        // A static list of every binding that's been instantiated
        public static List<Binding> Bindings { get; } = new List<Binding>();

        // Whether or not the mouse buttons have just been pressed (as opposed to held)
        public static bool MouseLeft => MouseState.LeftButton == ButtonState.Pressed &&
                                        LastMouseState.LeftButton == ButtonState.Released;
        public static bool MouseRight => MouseState.RightButton == ButtonState.Pressed &&
                                        LastMouseState.RightButton == ButtonState.Released;
        public static bool MouseMiddle => MouseState.MiddleButton == ButtonState.Pressed &&
                                        LastMouseState.MiddleButton == ButtonState.Released;

        // Static bindings that never change;  used for menus
        public static Binding Up = new Binding() { Key = Keys.Up, Button = Buttons.DPadUp, ButtonBound = true };
        public static Binding Down = new Binding() { Key = Keys.Down, Button = Buttons.DPadDown, ButtonBound = true };
        public static Binding Enter = new Binding() { Key = Keys.Enter, Button = Buttons.Start, ButtonBound = true };
        public static Binding Space = new Binding() { Key = Keys.Enter, Button = Buttons.A, ButtonBound = true };
        public static Binding Esc = new Binding() { Key = Keys.Escape };

        // Gameplay bindings, can be customized
        public static Binding Left = new Binding("LEFT") { Key = Keys.Left, Button = Buttons.DPadLeft, ButtonBound = true };
        public static Binding Right = new Binding("RIGHT") { Key = Keys.Right, Button = Buttons.DPadRight, ButtonBound = true };
        public static Binding Jump = new Binding("JUMP") { Key = Keys.Up, Button = Buttons.DPadUp, ButtonBound = true };
        public static Binding Crouch = new Binding("CROUCH") { Key = Keys.Down, Button = Buttons.DPadDown, ButtonBound = true };
        public static Binding Magic = new Binding("MAGIC") { Key = Keys.S, Button = Buttons.A, ButtonBound = true };
        public static Binding Fireball = new Binding("FIREBALL") { Key = Keys.A, Button = Buttons.X, ButtonBound = true };
        public static Binding Pause = new Binding("PAUSE") { Key = Keys.Enter, Button = Buttons.Start, ButtonBound = true };
        
        // Used for the mouse in the level editor 
        public static bool MouseInWindow;
        public static Point MousePos => new Point((int)(MouseState.X / Game.Options.Scale), (int)(MouseState.Y / Game.Options.Scale));

        // The Game instance initializes us when it starts up
        public static void Init(Game game)
        {
            Game = game;
        }

        // Saves bindings to the Game.Options object for long term storage
        public static void SaveBindings()
        {
            Game.Options.Bindings.Clear();
            foreach(var b in Bindings)
            {
                if (string.IsNullOrEmpty(b.Name)) continue;
                Game.Options.Bindings.Add(new SavedBinding(b));
            }
        }


        public static void Clear()
        {
            LastWorldKeyboardState = KeyboardState;
            LastKeyboardState = KeyboardState;
            WorldKeyboardState = KeyboardState;
        }

        // Loads bindings from the Game.Options object
        public static void LoadBindings()
        {
            // Load bindings from Game.Options
            if (Game.Options.ControlLayout == ControlLayout.Custom)
            {
                foreach (SavedBinding sb in Game.Options.Bindings)
                {
                    sb.Restore();
                }
            } else
            {
                ApplyLayout(Game.Options.ControlLayout);
            }
        }

        // Applies a pre-defined layout (arrow keys or WASD)
        public static void ApplyLayout(ControlLayout layout)
        {
            if (layout == ControlLayout.Arrows)
            {
                Jump.Key = Keys.Up; Jump.Button = Buttons.DPadUp; Jump.ButtonBound = true;
                Crouch.Key = Keys.Down; Crouch.Button = Buttons.DPadDown; Crouch.ButtonBound = true;
                Left.Key = Keys.Left; Left.Button = Buttons.DPadLeft; Left.ButtonBound = true;
                Right.Key = Keys.Right; Right.Button = Buttons.DPadRight; Right.ButtonBound = true;

                Magic.Key = Keys.S; Magic.Button = Buttons.A; Magic.ButtonBound = true;
                Fireball.Key = Keys.A; Fireball.Button = Buttons.X; Fireball.ButtonBound = true;
                Pause.Key = Keys.Enter; Pause.Button = Buttons.Start; Pause.ButtonBound = true;

            }
            else if (layout == ControlLayout.WASD)
            {
                Jump.Key = Keys.W; Jump.Button = Buttons.DPadUp; Jump.ButtonBound = true;
                Crouch.Key = Keys.S; Crouch.Button = Buttons.DPadDown; Crouch.ButtonBound = true;
                Left.Key = Keys.A; Left.Button = Buttons.DPadLeft; Left.ButtonBound = true;
                Right.Key = Keys.D; Right.Button = Buttons.DPadRight; Right.ButtonBound = true;

                Magic.Key = Keys.RightShift; Magic.Button = Buttons.A; Magic.ButtonBound = true;
                Fireball.Key = Keys.Divide; Fireball.Button = Buttons.X; Fireball.ButtonBound = true;
                Pause.Key = Keys.Enter; Pause.Button = Buttons.Start; Pause.ButtonBound = true;

            }
        }

        /// <summary>
        /// Used when the user is setting up bindings; detects the first key the user presses
        /// </summary>
        public static Keys? GetAnyKey()
        {
            var k = KeyboardState.GetPressedKeys().Where(x => x != Keys.Escape 
            && LastKeyboardState.IsKeyUp(x)).ToArray();
            if (k.Length == 0) return null;
            return k[0];
        }

        /// <summary>
        /// Used when the user is setting up bindings; detects the first button the user presses
        /// </summary>
        public static Buttons? GetAnyButton()
        {
            foreach (Buttons b in Enum.GetValues(typeof(Buttons)))
            {
                if (GamePadState.IsButtonDown(b)) return b;
            }
            return null;
        }

        /// <summary>
        /// Called to update "world controls" -- the controls that the gameplay uses
        /// </summary>
        public static void UpdateWorld()
        {
            LastWorldGamePadState = WorldGamePadState;
            LastWorldKeyboardState = WorldKeyboardState;

            WorldGamePadState = GamePad.GetState(PlayerIndex.One);
            WorldKeyboardState = Keyboard.GetState();

        }

        /// <summary>
        /// Called to update "game controls" -- the controls that are always processed
        /// even when the game is paused or if the loaded world isn't handling input.
        /// Also handles the mouse update.
        /// </summary>
        public static void Update()
        {
            // Store last state so we can see if things changed
            LastGamePadState = GamePadState;
            LastKeyboardState = KeyboardState;
            LastMouseState = MouseState;

            // Get the new state
            GamePadState = GamePad.GetState(PlayerIndex.One);
            KeyboardState = Keyboard.GetState();
            MouseState = Mouse.GetState();

            // Is the mouse in the window?
            MouseInWindow = (MouseState.X > 0 && MouseState.Y > 0
                && MouseState.X < Game.Window.ClientBounds.Width
                && MouseState.Y < Game.Window.ClientBounds.Height);

        }

        public static BindingState FlatState()
        {
            BindingState b = BindingState.None;

            if (Left.Down()) b |= BindingState.Left;
            if (Right.Down()) b |= BindingState.Right;
            if (Jump.Down()) b |= BindingState.Jump;
            if (Crouch.Down()) b |= BindingState.Crouch;
            if (Magic.Down()) b |= BindingState.Magic;
            if (Fireball.Down()) b |= BindingState.Fireball;
            if (Pause.Down()) b |= BindingState.Pause;

            return b;
        }

        public static BindingState PrevState()
        {
            BindingState b = BindingState.None;

            if (Left.PrevDown()) b |= BindingState.Left;
            if (Right.PrevDown()) b |= BindingState.Right;
            if (Jump.PrevDown()) b |= BindingState.Jump;
            if (Crouch.PrevDown()) b |= BindingState.Crouch;
            if (Magic.PrevDown()) b |= BindingState.Magic;
            if (Fireball.PrevDown()) b |= BindingState.Fireball;
            if (Pause.PrevDown()) b |= BindingState.Pause;

            return b;
        }

        /// <summary>
        /// Gets a snapshot depicting the state of all bindings (current and previous frame)
        /// </summary>
        public static ControlState GetState() => new ControlState(FlatState(), PrevState());

        /// <summary>
        /// Returns true if Jump+Magic+Fireball have been pressed.  (Up+A+B)
        /// </summary>
        public static bool CheatPressed()
        {
            return (Jump.Down() && Fireball.Down() && Magic.Down());
        }

        public static bool WheelUp => LastMouseState.ScrollWheelValue < MouseState.ScrollWheelValue;
        public static bool WheelDown => LastMouseState.ScrollWheelValue > MouseState.ScrollWheelValue;

        /// <summary>
        /// Called by the Level to check keys in debug mode
        /// </summary>
        public static void CheckDebugKeys()
        {

            bool shift = KeyboardState.IsKeyDown(Keys.LeftShift) | KeyboardState.IsKeyDown(Keys.RightShift);
            bool ctrl = KeyboardState.IsKeyDown(Keys.LeftControl) | KeyboardState.IsKeyDown(Keys.RightControl);
            var l = (Game.World as Level);

            // Don't do these things in edit mode
            if (l != null && l.State == LevelState.Edit) return;

            if (KeyPressed(Keys.P) && !shift) { Game.DevD++; showDevValues(); }
            if (KeyPressed(Keys.P) && shift) { Game.DevD--; showDevValues(); }
            if (KeyPressed(Keys.O) && !shift) { Game.DevC++; showDevValues(); }
            if (KeyPressed(Keys.O) && shift) { Game.DevC--; showDevValues(); }
            if (KeyPressed(Keys.I) && !shift) { Game.DevB++; showDevValues(); }
            if (KeyPressed(Keys.I) && shift) { Game.DevB--; showDevValues(); }
            if (KeyPressed(Keys.U) && !shift) { Game.DevA++; showDevValues(); }
            if (KeyPressed(Keys.U) && shift) { Game.DevA--; showDevValues(); }
            if (KeyPressed(Keys.Y) && !shift) Game.SaveDev();

            if (KeyPressed(Keys.F1) && !shift && !ctrl) { if (l != null) { l.Restart(true); } }
            if (KeyPressed(Keys.F1) && shift && !ctrl) Game.Reset();
            if (KeyPressed(Keys.F1) && ctrl) l?.RecordDemo();

            if (KeyPressed(Keys.F2) && !shift && !ctrl) { Game.World.Dana?.GiveBlueHourglass(); Sound.Collect.Play(); }
            if (KeyPressed(Keys.F2) && ctrl) l?.StopRecordingDemo();

            if (KeyPressed(Keys.F3) && !shift && !ctrl) { if (l != null) { l.CameraMode = CameraMode.Unlocked; } }
            if (KeyPressed(Keys.F3) && shift && !ctrl) { if (l != null) { l.CameraMode = CameraMode.Locked; } }
            if (KeyPressed(Keys.F3) && ctrl)
            {
                try
                {
                    var file = $"demo_{Game.Sesh.RoomNumber}{Game.Sesh.Story.ToStoryID()}.json";
                    var demo = file.LoadFile<Demo>();
                    if (demo != null) { Game.StartDemo(demo, false); } else Game.StatusMessage("FAILED TO LOAD DEMO FILE");
                }
                catch { Game.StatusMessage("FAILED TO LOAD DEMO FILE"); }

            }

            if (KeyPressed(Keys.F4))
            {
                Sound.StopAll();
                Game.Story++;
                if (Game.Story > Story.Test) Game.Story = 0;
                Game.StartNewGame(Game.Story, Game.Sesh.RoomNumber, Game.Difficulty, false);
                Game.StatusMessage($"STORY: {Game.Story.ToString().ToUpper()}");
            }

            if (KeyPressed(Keys.F5) && !shift && !ctrl) { l?.Layout?.OpenDoors(0, true, true); Sound.Key.Play(); }
            if (KeyPressed(Keys.F5) && shift && !ctrl) { if (l != null) l.Life = 2100; }
            if (KeyPressed(Keys.F5) && ctrl) { l?.Dana?.EnterDoor(false, default); }

            if (KeyPressed(Keys.F6) && !shift && !ctrl)
            {
                Game.DebugMode = !Game.DebugMode;
                if (KeyPressed(Keys.F6) && shift && !ctrl)
                {
                    Game.InputPromptNumber("ENTER SCORE", i =>
                    {
                        Game.Sesh.Score = i;
                        Sound.Collect.Play();
                    }, Game.Sesh.Score.ToString());
                }
            }
            if (KeyPressed(Keys.F6) && ctrl)
            {
                if (shift)
                    Game.ToggleFullscreen();
                else
                {
                    if (Game.Options.Scale > 4)
                    {
                        Game.SetScale(3);
                    }
                    else
                    {
                        Game.SetScale(4.5);
                    }
                }
            }

            if (KeyPressed(Keys.F7) && !ctrl) { Game.DebugPause = true; Game.Pause = !Game.Pause; }
            if (KeyPressed(Keys.F7) && shift && ctrl) { Game.SwitchMode(new Modes.ClassicEnding()); }

            if (KeyPressed(Keys.F8) && ctrl) { Game.StatusMessage($"YOUR GDV: {Game.Sesh.CalculateGDV()}"); }
            if (KeyboardState.IsKeyDown(Keys.F8)) { Game.Step = true; }

            if (KeyPressed(Keys.F9)) { Game.Step = true; }

            if (KeyPressed(Keys.F10) && !shift && !ctrl) { Game.ShowCollision = !Game.ShowCollision; }
            if (KeyPressed(Keys.F10) && shift && !ctrl) { l?.ToggleDebugItem(); }
            if (KeyPressed(Keys.F10) && ctrl) { Game.Sesh.Apprentice = !Game.Sesh.Apprentice; }

            if (KeyPressed(Keys.F11) && !shift && !ctrl) { if (l != null) l.Edit(false); }
            if (KeyPressed(Keys.F11) && shift && !ctrl) { l.Edit(true); }
            if (KeyPressed(Keys.F11) && ctrl) { Game.SwitchMode(new Modes.LevelSelect()); }

            if (KeyPressed(Keys.F12) && !shift) { Game.World.Dana?.Die(); }
            if (KeyPressed(Keys.F12) && shift) { l?.TimeOver(); }

            if (KeyPressed(Keys.OemPlus)) { if (shift) l?.VapourModeOff(); else l?.VapourModeOn(); }
            if (KeyPressed(Keys.B) && !shift) { Game.ShowHitBoxes = !Game.ShowHitBoxes; }
            if (KeyPressed(Keys.B) && shift) { Game.ShowHurtBoxes = !Game.ShowHurtBoxes; }
            if (KeyPressed(Keys.R) && !shift) { Game.ShowRoutines = !Game.ShowRoutines; }
            if (KeyPressed(Keys.T) && !shift) { Game.ShowTimers = !Game.ShowTimers; }
            if (KeyPressed(Keys.M) && !shift) { Game.ShowMusic = !Game.ShowMusic; }
            if (KeyPressed(Keys.M) && shift) { Game.ShowMagic = !Game.ShowMagic; }
            if (KeyPressed(Keys.V) && !shift) { Game.ShowInventory = !Game.ShowInventory; }
            if (KeyPressed(Keys.C) && !shift) { if (l != null) l.Layout.CameraBounds = l.WorldRectangle; }

            void showDevValues()
            {
                Game.StatusMessage($"A{Game.DevA:X2} B{Game.DevB:X2} C{Game.DevC:X2} D{Game.DevD:X2}");
            }

        }
       

        public static bool ControlKeyDown()
        {
            return KeyboardState.IsKeyDown(Keys.LeftControl) || KeyboardState.IsKeyDown(Keys.RightControl);
        }

        public static bool ShiftKeyDown()
        {
            return KeyboardState.IsKeyDown(Keys.LeftShift) || KeyboardState.IsKeyDown(Keys.RightShift);
        }

        public static bool AltKeyDown()
        {
            return KeyboardState.IsKeyDown(Keys.LeftAlt) || KeyboardState.IsKeyDown(Keys.RightAlt);
        }

        /// <summary>
        /// Checks to see if a key was just pressed on the current tick
        /// </summary>
        public static bool KeyPressed(Keys k)
        {
            return (KeyboardState.IsKeyDown(k) && LastKeyboardState.IsKeyUp(k));
        }
    }

    /// <summary>
    /// Represents a key and/or button binding
    /// </summary>
    public class Binding
    {
        public Keys Key;                // Key binding.  Keys.None == unbound
        public bool ButtonBound;        // Is the button bound?
        public Buttons Button;          // Button value
        public string Name;             // Name of the binding (used in menus)

        public Binding(string name)
        {
            Control.Bindings.Add(this);
            Name = name;
        }
        public Binding() 
        {
            Control.Bindings.Add(this);
        }

        public void RemoveDuplicates()
        {
            foreach(var b in Control.Bindings)
            {
                if (b == this) continue;
                if (b.Key == Key) b.Key = Keys.None;
                if (b.ButtonBound && ButtonBound && b.Button == Button) b.ButtonBound = false;
            }
        }

        public override string ToString()
        {
            StringBuilder x = new StringBuilder();

            if (Key != Keys.None)
            {
                x.Append(Key.ToString() + ", ");
            }
            if (ButtonBound)
            {
                x.Append(Button.ToString());
            }
            if (x.Length == 0) return "Unbound";
            return x.ToString().Trim().TrimEnd(',');
            
        }

        /// <summary>
        /// Is the binding currently down?
        /// </summary>
        public bool Down()
        {
            if (Key != Keys.None && Control.WorldKeyboardState.IsKeyDown(Key)) return true;
            if (ButtonBound && Control.WorldGamePadState.IsButtonDown(Button)) return true;
            return false;
        }

        public bool PrevDown()
        {
            if (Key != Keys.None && Control.LastWorldKeyboardState.IsKeyDown(Key)) return true;
            if (ButtonBound && Control.LastWorldGamePadState.IsButtonDown(Button)) return true;
            return false;
        }

        /// <summary>
        /// Was the binding just pressed (and not held)?
        /// </summary>
        public bool Pressed(bool world)
        {
            var last_key = world ? Control.LastWorldKeyboardState : Control.LastKeyboardState;
            var now_key = world ? Control.WorldKeyboardState : Control.KeyboardState;

            var last_pad = world ? Control.LastWorldGamePadState : Control.LastGamePadState;
            var now_pad = world ? Control.WorldGamePadState : Control.GamePadState;


            if (Key != Keys.None && now_key.IsKeyDown(Key) && last_key.IsKeyUp(Key)) return true;
            if (ButtonBound && now_pad.IsButtonDown(Button) && last_pad.IsButtonUp(Button)) return true;
            return false;
        }


        public bool Up() => !Down();

    }
    
    /// <summary>
    /// Used to serialize key bindings into the options file
    /// </summary>
    public struct SavedBinding
    {
        public string Name;
        public Keys Key;
        public Buttons Button;
        public bool ButtonBound;

        public SavedBinding(Binding b)
        {
            Name = b.Name;
            Key = b.Key;
            Button = b.Button;
            ButtonBound = b.ButtonBound;
        }

        public void Restore()
        {
            var me = this;
            var b = Control.Bindings.FirstOrDefault(x => x.Name == me.Name);
            if (b != null)
            {
                b.Key = Key;
                b.Button = Button;
                b.ButtonBound = ButtonBound;
            }
        }

    }

    [Flags]
    public enum BindingState
    {
        None = 0,
        Left = 1,
        Right = 2,
        Jump = 4,
        Crouch = 8,
        Magic = 0x10,
        Fireball = 0x20,
        Pause = 0x40
    }

    public struct ControlState
    {
        public BindingState Current;
        public BindingState Previous;

        public ControlState(BindingState current, BindingState previous)
        {
            Current = current;
            Previous = previous;
        }

        public bool Down(BindingState controlToCheck)
        {
            return Current.HasFlag(controlToCheck);
        }

        public bool Up(BindingState controlToCheck)
        {
            return !Current.HasFlag(controlToCheck);
        }

        public bool Pressed(BindingState controlToCheck)
        {
            return Current.HasFlag(controlToCheck) && !Previous.HasFlag(controlToCheck);
        }

    }

}
