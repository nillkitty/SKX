using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using MonoGame.Extended;
using Microsoft.Xna.Framework.Audio;
using System.Text;
using System.Linq;
using System.Text.RegularExpressions;
using System.Runtime.InteropServices;
using System.Diagnostics;

/*  SKX, An open source remake
 *  URL:  https://github.com/nillkitty/SKX
 *  
 *      A note about the code in this project:
 *  
 *      Whether or not it actually matters post-optimization,
 *      public fields are used throughout the code base instead of 
 *      public auto properties for both performance and code-brevity.
 *      (Except in cases where a property is needed for other reasons, such as
 *      intentionally making a setter private or doing some serialization)
 *      
 *      There are also instances where one object is directly manipulating
 *      the internal state of another without an accessor or any validation.  
 *      The goal is to simplify the business logic of the game,  not to write
 *      an example of perfect OOP principles.  
 *      
 *      Due to many of the game mechanics being derived from an NES game
 *      with limited RAM, many values used in gameplay are necessarily cross-cutting
 *      and are shared among discrete components of the game, and introducing
 *      added functional calls and accessors that do nothing helpful at runtime,
 *      and only make the code slightly more OOP-elegant at compile/review time,
 *      does not provide any added benefit to the end user.  
 *      
 *      The structure of the game looks something like this.
 *      
 *      Game
 *       |- Options                 User preferences and options stored on disk.
 *       |- Sesh                    Save slot-specific session details saved on disk.
 *       |- Assets                  Embedded game assets loaded from the content pipeline.
 *       `- World                   A reference to whatever the current World (game mode) is.
 *          |- TitleScreen          
 *          |- SoundTest
 *          |- LevelSelect
 *          `- Level                'Level' is the subclass of World that runs most of the game.
 *              |- GameObject[s]    The level runs the GameObjects, which includes Dana
 *              `- Layout           As well as the Layout which handles the in-game level (room) itself
 *  
 *      How things work:
 *      
 *      Most of the constructs (Game, World, Level, GameObject, Layout) have the following functions
 *      which are instrumental to the operation of the game:
 *      
 *      * Init()        Called when that component should initialize,  typically after all of the
 *                      requisite properties/fields have been set.
 *                      
 *      * Update(time)  Called on each frame of gameplay to advance the game.  The elapsedTime is
 *                      passed into this function, but as a port from an NES game, SKX runs at a 
 *                      fixed frequency (slightly above 60Hz) so this value is almost never helpful.
 *                      
 *      * Render(batch) Called on each frame to render the graphics on screen.  A reference to the
 *                      current SpriteBatch is passed in.  The current construct can End the batch
 *                      as long as it begins a new one before returning control.
 *      
 *      Additionally, other constructs without a visual component (Sound, Input, etc..) have an 
 *      Init() and Update() method but no Render().
 *      
 *      Almost all hard-coded gameplay parameters are in a section commented as 'Behavioral Parameters'
 *      in the event you would like to tweak something to be more (or less) NES-accurate.
 *  
 */

namespace SKX
{
    /// <summary>
    /// The game itself (implicit singleton)
    /// </summary>
    public class Game : Microsoft.Xna.Framework.Game
    {

        /* Graphics stuff */
        private static GraphicsDeviceManager DeviceManager;           // Reference to GraphicsDeviceManager
        private static SpriteBatch Batch;                             // The SpriteBatch we re-use for rendering
        private static RenderTarget2D Native;                         // The game's native-resolution render target

        /* Probably never change these */
        public static Point NativeTileSize = new Point(16, 16);        // Native tile size
        public static Point NativeHalfTileSize = new Point(8, 8);      // Native half tile size
        public const int NativeWidth = 256;                            // Native game display width
        public const int NativeHeight = 240;                           // Native game display height
        public const uint DebounceTicks = 10;                          // Number of ticks to ignore input
                                                                       // after various state changes

        /* Behavioral parameters */
        public const int MinFireballRange = 16;                        // Shortest range a fireball will have

        /* Developer stuff */
        public const string Version = "BETA 0.21";              // Version string
        public static int DevA, DevB, DevC, DevD;               // Debugging variables settable from debug mode
        public static uint Ticks = 0;                           // Perpetual tick count
        const string DebugModeCheatCode = "DEBUG";              // Type this at pause menu
        const string LevelSelectCheatCode = "LEVEL";            // Type this at pause menu

        /* Global variables */
        public static Game Instance;               // Reference to the (hopefully only) instance of this class
        public static World World;                 // Reference to the World we're currently presenting
        public static Assets Assets;               // Reference to loaded Assets manager
        public static Sesh Sesh;                   // Reference to the player's session (saved game params)
        public static Options Options;             // Game options
        public static Menu Menu;                    // The current menu (null == no menu)
        public static Sesh[] SavedGames;            // Saved games loaded from disk

        public static float FPS;                   // Calculated FPS based on elapsed time of last frame
        public static bool DrawHUD = true;         // Should we be drawing the HUD?  (Usually yes if in a level)
        public static bool ShowCollision = false;  // Should we render debug collision boxes?
        public static bool ShowHitBoxes = false;   // Should we render debug hit boxes?
        public static bool ShowHurtBoxes = false;  // Should we render debug hurt boxes?
        public static bool ShowRoutines = false;   // Should we render debug routine counters?
        public static bool ShowMusic = false;      // Should we render debug music info?
        public static bool ShowMagic = false;      // Should we render debug magic/spells info?
        public static bool ShowInventory = false;  // Should we render debug inventory info?
        public static bool ShowTimers = false;     // Should we render debug timers?
        public static bool DebugMode = false;      // Debug mode enabled?
        public static bool Pause = false;          // Are we paused?
        public static bool DebugPause = false;     // Debug mode pause that doesn't bring up the menu
        public static bool Step = false;           // Are we advancing by a single frame of game play?
        public static string Message;              // Debug/editor message
        public static int MessageTimer;            // Debug/editor message time remaining
        public static string HelpText { get; private set; }    // Help text currently being displayed
        public static uint HelpTime { get; private set; }      // Ticks spent in help menu
        public static int HelpOffset { get; private set; }    // Scroll offset for help text

        public static string InputValue;            // Input value
        public static string InputCaption;          // Input prompt
        public static bool InputNumbers;            // Input numbers only
        public static Action<string> InputCallback; // Input callback

        public static string AppDirectory;        // Path of the directory where we save/load runtime data
        public static int LastDemoPlayed = 0;       // Track which demo we're on
        public static bool AutoStart;               // Whether auto-start is enabled for development
        public static int SuppressAutoStart = 0;    // Ticks to suppress auto start

        public static SafeList<Swap> Swaps = new SafeList<Swap>();  // List of active palette swaps

        public static float Fade = 1.0f;            // Fade to black
        public static float FadeStep = 0f;          // Amount +/- to fade each tick      
        public static int FadeHold = 0;             // How long to hold the fade before firing callback
        public static Action FadeCallback;          // Callback to fire (and clear) when fade finishes

        // Platform
        public static bool Windows, Mac, Linux;     // Which platform are we on?

        // Used by Menus
        private static Binding ConfigBinding;      // The control binding currently being changed in the menu
        private static bool ConfigPad;             // Whether we're detecting buttons (true) or keys (false)
      
        // Menu definitions
        public static Menu PauseMenu;         
        public static Menu OptionsMenu;
        public static Menu KeyBindingMenu;
        public static Menu PadBindingMenu;
        public static Menu ControlsMenu;
        public static Menu ListenForKeyMenu;
        public static Menu StartMenu;
        public static Menu QuitGameMenu;
        public static Menu StoryMenu;
        public static Menu SlotMenu;
        public static Menu DifficultyMenu;
        public static Menu ConfirmOverwriteMenu;
        public static Menu InputMenu;
        public static Menu EditorMenu;

        // Shortcut to the current Level Editor mode
        private static Editor.EditorMode EditorMode => (World as Level)?.Editor?.Mode;

        // Current/target story and difficulty
        public static Story Story;                 // Which set of levels we're playing
        public static Difficulty Difficulty;       // Which difficulty the user started on
        public static char StoryID => Story.ToStoryID();    // I don't know why this is a thing

        // Camera and screen variables
        public static Point CameraPos;                              // Camera current position
        public static Point CameraSize = new Point(256, 208);      // Camera size (game area rendered)
        public static Point CameraOffset = new Point(0, 32);       // Where in the window to draw the camera view
                                                            // (i.e. below the HUD)
        public static Rectangle CameraRect => new Rectangle(CameraPos, CameraSize);
        public static Point ScreenOffset = default;         // Used to center full-screen
        public static double Scale = 1;                   // Actual scale used

        
        // RNG
        public static Random Random = new Random(Environment.TickCount);   // Random number generator

        // Quick story checks
        public static bool IsSKX => Sesh.Story == Story.SKX;
        public static bool IsPlus => Sesh.Story == Story.Plus;
        public static bool IsClassic => Sesh.Story == Story.Classic;


        public Game()
        {
            // Make sure Options is never null.  Load defaults until we load the real
            // options from disk further down...
            Options = new Options();

            // Likewise, create a new default session so there's always one there
            Sesh = new Sesh();

            LogInfo($"SKX {Version} started");      

            // Check for auto start flag
            var arg = Environment.GetCommandLineArgs();
            AutoStart = arg.Any(x => x.ToLower() == "/s");

            // Who are we?  How did I get here?  This is not my beautiful house!
            Mac = RuntimeInformation.IsOSPlatform(OSPlatform.OSX);
            Linux = RuntimeInformation.IsOSPlatform(OSPlatform.Linux);
            Windows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

            // Set some references
            Instance = this;
            Content.RootDirectory = "Content";
            
            // Find out where we will store/load our files
            AppDirectory = FindWorkDirectory();

            // Load options (scale, volume, key bindings)
            LoadOptions();

            // Apply debug mode switch
            if (arg.Any(x => x.ToLower() == "/d")) Options.DebugMode = true;

            // Build the in-game menus
            BuildMenus();

            // Load assets
            Assets = new Assets(this);

            // Set up some XNA stuff
            DeviceManager = new GraphicsDeviceManager(this);
            IsMouseVisible = true;
            Window.TextInput += TextInputHandler;

            // Force every frame to be the same duration as it was on the NES
            // (the CRT didn't paint frames slower if the NES was lagging)
            IsFixedTimeStep = true;
            // 60 FPS (NES frequency)
            TargetElapsedTime = new System.TimeSpan(0, 0, 0, 0, 16);

        }

        // This is called when someone presses a key when we have an 
        // input prompt open
        private void TextInputHandler(object sender, TextInputEventArgs e)
        {
            // Are we displaying the input box?
            if (Menu == InputMenu)
            {
                if (e.Key == Keys.Back)
                {
                    // Backspace
                    if ((InputValue?.Length ?? 0) > 0)
                    {
                        InputValue = InputValue.Substring(0, InputValue.Length - 1);
                    }
                    return;
                }

                if (InputNumbers && !char.IsNumber(e.Character)) return;    // Enforce numbers only
                InputValue += e.Character;  // Append the character,  too minor to bother using a StringBuilder
            }
        }

        // Loads the Options object from disk
        private static void LoadOptions()
        {
            // Try to load the options file -- if we can't then just 
            // keep the defaults
         
            try
            {
                var o = Extensions.LoadFile<Options>("options.json");
                if (o != null) Options = o;

                DebugMode = Options.DebugMode;
                Control.LoadBindings();
                o.UpdateVolume();

                LogInfo("Loaded options");
            }
            catch (Exception ex) 
            {
                LogError($"Failed to load options.json: {ex}");
            }
        }




        /// <summary>
        /// Updates the help text display.  Pass in <see langword="null" /> to clear help text.
        /// Resets the debounce timer and the Y-scroll offset.
        /// </summary>
        /// <param name="text"></param>
        public static void SetHelpText(string text)
        {
            if (text != null) Pause = false;
            HelpOffset = 0;
            HelpTime = 0;
            HelpText = text;
        }

        /// <summary>
        /// Scrolls the help text display up or down, clamped appropriately
        /// </summary>
        public static void ScrollHelp(int offset)
        {
            int max = HelpText.GetLineCount() * 8 - CameraRect.Height;
            if (max < 0) max = 0;

            HelpOffset += offset;
            if (HelpOffset < 0) HelpOffset = 0;
            if (HelpOffset > max) HelpOffset = max;
        }

        /// <summary>
        /// Finds the directory where we save/load files from on this computer
        /// </summary>
        private static string FindWorkDirectory()
        {
            string appdata = null;

            // Figure out the base directory in which we will find/create
            // an 'SKX' directory.

            if (Mac)
            {
                appdata = "~/Library/Application Support";
            } else if (Linux)
            {
                appdata = "~/";
            } else
            {
                appdata = Environment.GetFolderPath(
                    Environment.SpecialFolder.ApplicationData);

            }

            // No clue how this could ever happen...
            if (appdata is null || !System.IO.Directory.Exists(appdata)) 
                appdata = Environment.CurrentDirectory; 

            // Add SKX to the path
            var folder = System.IO.Path.Combine(appdata, "SKX");
            if (!System.IO.Directory.Exists(folder))
            {
                // Try to create it ..
                try
                {
                    System.IO.Directory.CreateDirectory(folder);
                    return folder;
                } catch (Exception ex)
                {
                    LogError($"Failed to create SKX directory: {ex}");
                    // Just use the current directory then
                    return Environment.CurrentDirectory;  
                }
            } else
            {
                return folder;
            }

        }

        /// <summary>
        /// Logs an error to the console
        /// </summary>
        public static void LogError(string text)
        {
            if (Mac || Linux)
            {
                Console.WriteLine($"[Error] {text}");
            } else
            {
                Debug.WriteLine($"[Error] {text}");
            }
        }

        /// <summary>
        /// Logs status text to the console
        /// </summary>
        public static void LogInfo(string text)
        {
            if (Mac || Linux)
            {
                Console.WriteLine($"[Info] {text}");
            }
            else
            {
                Debug.WriteLine($"[Info] {text}");
            }
        }

        /// <summary>
        /// Selects an item at random from the provided list of options
        /// </summary>
        public static T SelectRandom<T>(params T[] items)
        {
            return items[Random.Next(0, items.Length - 1)];
        }

        /// <summary>
        /// Loads a saved game
        /// </summary>
        public static void Load(Sesh s)
        {
            Sesh = s;
            Story = s.Story;
            Difficulty = s.Difficulty;
            Menu = null;
            World = Sesh.BuildLevel();
            World.Init();
        }

        /// <summary>
        /// Toggles fullscreen mode
        /// </summary>
        public static void ToggleFullscreen()
        {
            if (DeviceManager.IsFullScreen)
            {
                // Exit full screen
                DeviceManager.ToggleFullScreen();
                ScreenOffset = default;    
            }
            else
            {
                // Enter full screen
                DeviceManager.ToggleFullScreen();
                SetupFullscreen();
            }

            UpdateTitle();                  // Force update window
            SetScale(Options.Scale);        // Force update Scale

            // Save preference to disk
            Options.FullScreen = DeviceManager.IsFullScreen;   
            SaveOptions();

        }

        /// <summary>
        /// Normally the native draw surface is the size of the window which
        /// is always an even linear scaling;  but for fullscreen we set
        /// the draw surface to the entire screen, and set ScreenOffset to
        /// center the largest possible scale that fits the entire game
        /// on the screen without distorting.   MonoGame will not distort
        /// the display either way but it will draw it off-center (at 0,0)
        /// if the draw surface doesn't account for this by filling the
        /// screen and centering the game visual.
        /// </summary>
        static void SetupFullscreen()
        {

            // Screen (viewport) size
            var vpw = DeviceManager.GraphicsDevice.Viewport.Width;
            var vph = DeviceManager.GraphicsDevice.Viewport.Height;

            // Make the surface the entire size of the screen
            DeviceManager.PreferredBackBufferWidth = vpw;
            DeviceManager.PreferredBackBufferHeight = vph;
            DeviceManager.ApplyChanges();

            // Calculate the scale
            Scale = Math.Floor(Math.Min((double)vpw / NativeWidth, vph / NativeHeight));

            // Figure out where to paint the game
            var vc = vpw / 2;
            var vh = vph / 2;
            var ox = vc - (int)(NativeWidth * Scale / 2);
            var oy = vh - (int)(NativeHeight * Scale / 2);
            ScreenOffset = new Point(ox, oy);

        }

        /// <summary>
        /// Saves development variables to disk
        /// </summary>
        public static void SaveDev()
        {
            dynamic o = new System.Dynamic.ExpandoObject();
            o.a = DevA;
            o.b = DevB;
            o.c = DevC;
            o.d = DevD;
            Extensions.SaveFile(o, "dev.json");
        }

        /// <summary>
        /// Quits an active game back to the title screen.
        /// </summary>
        static void QuitGame()
        {
            Pause = false;
            Sesh = new Sesh();
            SwitchMode(new Modes.TitleScreen());
        }

        /// <summary>
        /// Reloads the game from the last save in the current slot
        /// </summary>
        static void ReloadSaveSlot()
        {
            StatusMessage("RELOAD SLOT");

        }

        /// <summary>
        ///   Loads the development variables on startup
        /// </summary>
        public static void LoadDev()
        {
            try
            {
                dynamic o = Extensions.LoadFile<System.Dynamic.ExpandoObject>("dev.json");
                DevA = (int)o.a;
                DevB = (int)o.b;
                DevC = (int)o.c;
                DevD = (int)o.d;
            }
            catch { return; }
        }

        /// <summary>
        /// Prints a message to the screen
        /// </summary>
        public static void StatusMessage(string x)
        {
            Message = x;
            MessageTimer = 150;
        }

        /// <summary>
        /// Prompts the player for text
        /// </summary>
        /// <param name="caption">Caption to show in the window</param>
        /// <param name="callback">Callback to fire upon pressing Enter</param>
        /// <param name="defaultValue">Default value</param>
        public static void InputPrompt(string caption, Action<string> callback, string defaultValue = null)
        {
            InputValue = defaultValue;
            InputNumbers = false;
            InputCallback = callback;
            InputCaption = caption;
            Menu = InputMenu;
            Control.Clear();
        }

        /// <summary>
        /// Prompts the player for a decimal number (int)
        /// </summary>
        /// <param name="caption">Caption to show in the window</param>
        /// <param name="callback">Callback to fire upon pressing Enter</param>
        /// <param name="defaultValue">Default value</param>
        public static void InputPromptNumber(string caption, Action<int> callback, string defaultValue = null)
        {
            InputValue = defaultValue;
            InputNumbers = true;
            InputCallback = (x) => callback?.Invoke(x.ToInt());
            InputCaption = caption;
            Menu = InputMenu;
            Control.Clear();
        }

        /// <summary>
        /// Prompts the player for a hexadecimal number
        /// </summary>
        /// <param name="caption">Caption to show in the window</param>
        /// <param name="callback">Callback to fire upon pressing Enter</param>
        /// <param name="defaultValue">Default value</param>
        public static void InputPromptHex(string caption, Action<int> callback, int? defaultValue = null)
        {
            InputValue = defaultValue.HasValue ? defaultValue.Value.ToString("X") : "";
            InputNumbers = false;
            InputCallback = (x) => callback?.Invoke(x.ToInt(true));
            InputCaption = caption;
            Menu = InputMenu;
            Control.Clear();
        }

        /// <summary>
        /// Updates the game's titlebar to show the current game type and room number
        /// </summary>
        public static void UpdateTitle()
        {
            var story = Story switch
            {
                Story.Classic => "SKX Classic",
                Story.Plus => "SKX Plus",
                _ => "SKX"
            };
            if (World is Level l)
            {
                string demo = "";
                if (Pause) {
                    demo = " -- PAUSED";
                } else if (Sesh.DemoPlayback)
                {
                    demo = " -- DEMO MODE";
                } else if (l.Recording != null)
                {
                    demo = " -- RECORDING DEMO";
                } else if (l.Playback != null)
                {
                    demo = " -- PLAYING DEMO";
                } else if (l.State == LevelState.Edit)
                {
                    demo = " -- Editing: " + l.Editor.ModeName;
                }
                Instance.Window.Title = $"{story} - Room {l.Layout.RoomNumber:X}{demo}";
            }
            else
            {
                Instance.Window.Title = "SKX";
            }
        }

        /// <summary>
        /// Called by XNA to initialize the game
        /// </summary>
        protected override void Initialize()
        {

            base.Initialize();

            // Load dev variables if possible
            LoadDev();

            // Set up the window
            Window.AllowUserResizing = false;
            Window.Title = "SKX";
            
            // Set up control (keyboard/gamepad)
            Control.Init(this);

            // Set the initial scale
            SetScale(Options.Scale);

            if (Options.FullScreen && !DeviceManager.IsFullScreen)
            {
                ToggleFullscreen();
            }
            
            // Reset (start) the game
            Reset();

        }

        /// <summary>
        /// Switches the current game mode (world)
        /// </summary>
        public static void SwitchMode(World newWorld)
        {
            if (World != newWorld)
            {
                Game.HelpText = null;
                Game.HelpOffset = 0;
                World.Unload();
                Swaps.Clear();
                World = newWorld;
                World.Init();
            }
        }

        /// <summary>
        /// Continues after a game over
        /// </summary>
        public static void Continue()
        {
            Sesh.OnContinue();
            World = Sesh.BuildLevel();
            World.Init();
        }

        /// <summary>
        /// Resets/starts the game
        /// </summary>
        public static void Reset()
        {
            /* Reset vector! */

            Sesh = new Sesh();

            // Create a new native draw surface
            Native = new RenderTarget2D(Instance.GraphicsDevice, NativeWidth, NativeHeight);

            if (Options.TestAutoStart())
            {
                StartNewGame(Story.Plus, 1, Difficulty.Normal, false);
            }
            else
            {
                World = new Modes.TitleScreen();
            }

        }

        /// <summary>
        /// Starts a new game in a given slot
        /// </summary>
        /// <param name="story">The selected story</param>
        /// <param name="slot">Which save slot (will overwrite!)</param>
        /// <param name="diff">Difficulty</param>
        public static void StartNewGame(Story story, int slot, Difficulty diff, bool save)
        {
            Sesh = new Sesh(diff, story, slot);
            Story = story;
            Difficulty = diff;
            Menu = null;
            Sound.StopAll();

            // TODO: Story setup
            World = Sesh.BuildLevel();
            World.Init();
            if (save) Sesh.Save();
           
        }

        /// <summary>
        /// Starts a no-save game for a specific story, difficulty, and room number
        /// </summary>
        public static void StartNoSave(Story story, Difficulty diff, int room)
        {
            Sesh = new Sesh(diff, story, 0);
            Story = story;
            Difficulty = diff;
            Sesh.RoomNumber = room;
            Menu = null;
            World = Sesh.BuildLevel();
            World.Init();
        }

        /// <summary>
        /// Starts a no-save game with a specific layout (on Normal difficulty)
        /// </summary>
        /// <param name="l"></param>
        public static void StartNoSave(Layout l)
        {
            Sesh = new Sesh(Difficulty.Normal, l.Story, 0);
            Story = l.Story;
            Difficulty = Difficulty.Normal;
            Sesh.RoomNumber = l.RoomNumber;
            Menu = null;
            World = Sesh.BuildLevel(l);
            World.Init();
        }

        /// <summary>
        /// Starts playing a canned demo
        /// </summary>
        /// <param name="demo">The demo to play</param>
        /// <param name="normal_playback">Set to <see langword="true"/> to play back as a normal demo
        /// (pressing a button goes to title screen) or <see langword="false"/> to play the Demo back
        /// as part of normal gameplay</param>
        public static void StartDemo(Demo demo, bool normal_playback)
        {
            Sesh = new Sesh(Difficulty.Normal, demo.Story, 0);
            Sesh.DemoPlayback = normal_playback;
            Sesh.RoomNumber = demo.RoomNumber;

            Story = demo.Story;
            Difficulty = Difficulty.Normal;
            Menu = null;

            var level = Sesh.BuildLevel(demo);
            World = level;
            World.Init();
        }

        /// <summary>
        /// Called by Level when a normally playing Demo has ended.
        /// </summary>
        internal static void DemoEnded()
        {
            Pause = false;
            DebugPause = false;

            // Return to Title Screen
            SwitchMode(new Modes.TitleScreen());
        }

        /// <summary>
        /// Changes the scale of the native game resolution to the game window
        /// </summary>
        public static void SetScale(double newScale)
        {
            Options.Scale = newScale;       // Save the requested value
            if (DeviceManager.IsFullScreen)
            {
                SetupFullscreen();
                return;
            }
            Scale = newScale;
            DeviceManager.PreferredBackBufferWidth = (int)(NativeWidth * newScale);
            DeviceManager.PreferredBackBufferHeight = (int)(NativeHeight * newScale);
            DeviceManager.ApplyChanges();
        }

        /// <summary>
        /// Attempt to save the options (scale, volume, key bindings) to file
        /// </summary>
        public static void SaveOptions()
        {
            Control.SaveBindings();
            if (!Options.SaveFile("options.json"))
            {
                StatusMessage("FAILED TO SAVE OPTIONS");
            }
        }

        /// <summary>
        /// Called by XNA to load our content
        /// </summary>
        protected override void LoadContent()
        {
            Batch = new SpriteBatch(GraphicsDevice);
            Assets.Load();
        }

        /// <summary>
        /// Begins a fade to black
        /// </summary>
        /// <param name="callback">Callback to fire upon fade finishing</param>
        /// <param name="hold">How many frames to hold before firing the callback</param>
        public static void FadeOut(Action callback = null, int hold = 0)
        {
            FadeStep = -0.08f;
            FadeCallback = callback;
            FadeHold = hold;
        }

        /// <summary>
        /// Begins a fade in from black
        /// </summary>
        /// <param name="callback">Callback to fire upon fade finishing</param>
        public static void FadeIn(Action callback = null)
        {
            FadeStep = 0.05f;
            FadeCallback = callback;
        }
        
        /* Updates an in-progress fade */
        void UpdateFade()
        {

            if (FadeStep != 0f)
            {
                Fade += FadeStep;
                if (FadeStep > 0 && Fade >= 1.0f) 
                { 
                    Fade = 1.0f; 
                    FadeStep = 0; 
                    FadeCallback?.Invoke(); 
                    FadeCallback = null; 
                }
                if (FadeStep < 0 && Fade <= 0.0f) 
                { 
                    Fade = 0.0f; 
                    if (FadeHold == 0)
                    {
                        FadeStep = 0;
                        FadeCallback?.Invoke(); 
                        FadeCallback = null;
                    } else
                    {
                        FadeHold--;
                    }
                }
            }
        }

        /// <summary>
        /// Called by XNA to update our game (1/60th of a second, ideally)
        /// </summary>
        protected override void Update(GameTime gameTime)
        {
            // Calculate the FPS
            FPS = 1 / (float)gameTime.ElapsedGameTime.TotalSeconds;
            Ticks++;

            // Update game-level input (game option controls)
            Control.Update();

            // We have the help text overlay open
            if (HelpText != null)
            {
                HelpTime++; 
                if (HelpTime > DebounceTicks)
                {
                    // Check controls
                    if (Control.Up.Pressed(false))
                    {
                        ScrollHelp(-8);
                    } 
                    else if (Control.Down.Pressed(false))
                    {
                        ScrollHelp(8);
                    } 
                    else if (Control.GetAnyKey().HasValue ||
                        Control.MouseLeft || Control.MouseRight || Control.Esc.Pressed(false))
                    {
                        HelpText = null;
                        Control.UpdateWorld();
                        Control.Update();   // Update again to eat up the key/mouse press
                    }
                }
            }

            // Decrement timers
            if (SuppressAutoStart > 0) SuppressAutoStart--;

            // Update fade effect
            UpdateFade();

            if (Menu != null)
            {
                HelpText = null;
                Menu.Update(gameTime);
            } 
            else 
            {
                // If we're paused,  bring up the menu
                if (Pause && !DebugPause && HelpText is null) Menu = World.PauseMenu;

                // Process things in the world that happen even when paused
                World.PauseUpdate(gameTime);

                // Update the world's main stuff if we're not paused
                if (!Pause || Step)
                {
                    // Update world-level input (world controls)
                    Control.UpdateWorld();
                    Sound.GoSound();
                    World.Update(gameTime);
                    Step = false;
                } else
                {
                    Sound.PauseSound();
                }

            }

            base.Update(gameTime);
        }

        /// <summary>
        /// Calculates the maximum string width taking control characters
        /// and markup into account
        /// </summary>
        public static int StringWidth(string x)
        {
            if (x is null) return 0;
            var s = x.Split('\n');
            return s.Max(z => width(z));

            int width(string w)
            {
                string regex = @"\[.+?\]";
                return Regex.Replace(w, regex, "").Length;
            }
        }

        /// <summary>
        /// Called by XNA to render things
        /// </summary>
        protected override void Draw(GameTime gameTime)
        {

            /* Rendering technique: 
             
                RenderTarget:
                    World.Surface       Surface containing the entire level or game mode 
                                        -- viewport based on CameraPos --
                    Native              Surface containing the game's visuals at native NES resolution
                                        -- scaling based on Game.Scale --
                    null                Default surface (the actual game window)
             
             */


            GraphicsDevice.Clear(Color.Black);
            World.Render();

            // Render world view to native
            GraphicsDevice.SetRenderTarget(Native);
            Batch.Begin(samplerState: SamplerState.PointClamp, sortMode: SpriteSortMode.Immediate);
            
            if (Swaps.Count > 0)
            {
                Swap.GetEffect().CurrentTechnique.Passes[0].Apply();
            }

            if (World.Flash)
            {
                // Flashing for time over (recreates NES "grayscale" PPU mode)
                Assets.TimeOver.CurrentTechnique.Passes[0].Apply();
            } else if (Pause && !DebugPause && HelpText is null)
            {
                // Grayscale for pause (new in SKX)
                Assets.Pause.CurrentTechnique.Passes[0].Apply();
            }

            if (DrawHUD)
            {
                Batch.Draw(World.Surface,
                      new Rectangle(CameraOffset, CameraSize),
                      new Rectangle(CameraPos, CameraSize),
                      Color.White * Fade);

                World.RenderHUD(Batch);

            }
            else
            {
                Batch.Draw(World.Surface,
                     new Rectangle(0, 0, NativeWidth, NativeHeight),
                     new Rectangle(CameraPos, new Point(NativeWidth, NativeHeight)),
                     Color.White * Fade);
            }

            // Help screens
            if (Game.HelpText != null)
            {
                Batch.FillRectangle(new RectangleF(0, 0, Game.NativeWidth + 16, Game.NativeHeight),
                                    Color.Black * 0.8f);

                Batch.DrawShadowedString(HelpText, new Point(16, 32 - HelpOffset), Color.White, 1);
            }

            // Status bar messages
            if (Message != null)
            {
                if (MessageTimer-- == 0)
                    Message = null;
                else
                    Batch.DrawOutlinedString(Message, new Point(8, 224), Color.White, 1);
            }
            Batch.End();

            // Are we showing a menu?
            if (Menu != null)
            {
                Batch.Begin(samplerState: SamplerState.PointClamp, sortMode: SpriteSortMode.Immediate);
                Menu.Render(Batch);
                Batch.End();
            }

            // Render native to the window
            GraphicsDevice.SetRenderTarget(null);
            Batch.Begin(samplerState: SamplerState.PointClamp);
            Batch.Draw(Native,
                new Rectangle(ScreenOffset.X, ScreenOffset.Y, 
                (int)(NativeWidth * Scale),
                (int)(NativeHeight * Scale)),
                new Rectangle(0, 0, NativeWidth, NativeHeight), Color.White);
            Batch.End();

            base.Draw(gameTime);


        }

        #region "Don't look in here"
        
        // I'm sorry...  But at least it's all in one place.
        private static void BuildMenus()
        {

            int SelectedSlot = 1;
            Story SelectedStory = Story.Classic;
            Difficulty SelectedDifficulty = Difficulty.Normal;
            bool SlotSelectNewGame = false;     // If selecting a slot is a new game or reload
            string GetScaleValue() => "SCALE:      " + Math.Round(Options.Scale, 1).ToString();
            string GetVolumeValue() => "MASTER VOL: " + Math.Round(Options.Volume * 100).ToString();
            string GetVolumeSValue() => "SFX VOL:    " + Math.Round(Options.SoundVol * 100).ToString();
            string GetVolumeMValue() => "MUSIC VOL:  " + Math.Round(Options.MusicVol * 100).ToString();
            string GetFullscreen() => "FULLSCREEN: " + (DeviceManager.IsFullScreen ? "YES" : "NO");
            void ScaleReset() { Options.Scale = 2.0; SetScale(Options.Scale); }
            void ScaleDown() { Options.Scale -= 0.5; if (Options.Scale < 0.5) Options.Scale = 0.5; SetScale(Options.Scale); }
            void ScaleUp() { Options.Scale += 0.5; if (Options.Scale > 8.0) Options.Scale = 8.0; SetScale(Options.Scale); }
            void VolumeDefault() { Options.Volume = 0.5; Options.UpdateVolume(); Sound.Wince.Play(); }
            void VolumeUp() { Options.Volume += 0.05; if (Options.Volume > 1.0) Options.Volume = 1.0; Options.UpdateVolume(); Sound.Wince.Play(); }
            void VolumeDown() { Options.Volume -= 0.05; if (Options.Volume < 0) Options.Volume = 0; Options.UpdateVolume(); Sound.Wince.Play(); }
            void NewGame() => StartNewGame(SelectedStory, SelectedSlot, SelectedDifficulty, true);
            void VolumeSDefault() { Options.SoundVol = 0.5; Options.UpdateVolume(); Sound.Wince.Play(); }
            void VolumeSUp() { Options.SoundVol += 0.05; if (Options.SoundVol > 1.0) Options.SoundVol = 1.0; Options.UpdateVolume(); Sound.Wince.Play(); }
            void VolumeSDown() { Options.SoundVol -= 0.05; if (Options.SoundVol < 0) Options.SoundVol = 0; Options.UpdateVolume(); Sound.Wince.Play(); }
            void VolumeMDefault() { Options.MusicVol = 0.5; Options.UpdateVolume(); Sound.ExtraLife.Play(); }
            void VolumeMUp() { Options.MusicVol += 0.05; if (Options.MusicVol > 1.0) Options.MusicVol = 1.0; Options.UpdateVolume(); Sound.ExtraLife.Play(); }
            void VolumeMDown() { Options.MusicVol -= 0.05; if (Options.MusicVol < 0) Options.MusicVol = 0; Options.UpdateVolume(); Sound.ExtraLife.Play(); }


            string GetLayoutValue() => "LAYOUT:   " + Options.ControlLayout.ToString().ToUpper();
            string GetEditBindings() => Options.ControlLayout == ControlLayout.Custom ? "EDIT KEY BINDINGS" : "";
            string GetEditBindings2() => Options.ControlLayout == ControlLayout.Custom ? "EDIT GAMEPAD BINDINGS" : "";

            void LayoutUp()
            {
                Options.ControlLayout++;
                if (Options.ControlLayout > ControlLayout.Custom) Options.ControlLayout = ControlLayout.Arrows;
                Control.ApplyLayout(Options.ControlLayout);
            }
            void LayoutDown()
            {
                Options.ControlLayout--;
                if (Options.ControlLayout < 0) Options.ControlLayout = ControlLayout.Custom;
                Control.ApplyLayout(Options.ControlLayout);
            }
            void EditKeyBindings() { Menu.GoTo(KeyBindingMenu); }
            void EditPadBindings() { Menu.GoTo(PadBindingMenu); }
            string GetKeyName(Keys k) => k.ToKeyName(false);
            string GetBind(Binding b) => $"{b.Name}:".PadRight(10) + GetKeyName(b.Key);
            string GetBindP(Binding b) => $"{b.Name}:".PadRight(10) + (b.ButtonBound ? b.Button.ToString().ToUpper() : "NONE");
            string GetSelectedBinding() => "PRESS THE " + (ConfigPad ? "BUTTON" : "KEY") + " FOR " + (ConfigBinding is null ? "NONE" : ConfigBinding.Name.ToUpper());

            void StoryClassic() { SelectedStory = Story.Classic; SlotSelectNewGame = true; StoryMenu.GoTo(SlotMenu); }
            void StoryClassicPlus() { SelectedStory = Story.Plus; SlotSelectNewGame = true; StoryMenu.GoTo(DifficultyMenu); }
            // void StorySKX() { SelectedStory = Story.SKX; SlotSelectNewGame = true; StoryMenu.GoTo(DifficultyMenu); }
            // void StoryTest() { SelectedStory = Story.Test; SlotSelectNewGame = true; StoryMenu.GoTo(DifficultyMenu); }
            void SelectDiff(Difficulty d) { SelectedDifficulty = d; DifficultyMenu.GoTo(SlotMenu); }

            string GetSlot(int i)
            {
                var x = $"*{i} {SelectedStory.ToStoryID()} --- EMPTY ---";
                x = Sesh.GetSlotStatus(i, SelectedStory) ?? x;
                return x;
            }

            void SoundTest()
            {
                SwitchMode(new Modes.SoundTest());
            }

            void SelectSlot(int i)
            {
                SelectedSlot = i;

                if (SlotSelectNewGame)
                {
                    if (Sesh.GetSlotStatus(SelectedSlot, SelectedStory) != null)
                    {
                        SlotMenu.GoTo(ConfirmOverwriteMenu);
                    }
                    else
                    {
                        NewGame();
                    }
                }
            }

            StringBuilder smk = new StringBuilder(10);
            bool PauseMenuKeyDown(Keys k)
            {
                smk.Append(k.ToString());
                if (smk.Length > 10) smk.Remove(0, 1);

                // DEBUG MODE Cheat Code
                if (smk.ToString().EndsWith(DebugModeCheatCode))
                {
                    Sound.Key.Play();
                    DebugMode = true;
                    Options.DebugMode = true;
                    StatusMessage("DEBUG MODE ON");
                    return true;
                }
                else if (smk.ToString().EndsWith(LevelSelectCheatCode))
                {
                    Sound.Key.Play();
                    SwitchMode(new Modes.LevelSelect());
                    Pause = false;
                    Menu = null;
                    return true;
                }
                return false;
            }
            void Statistics()
            {
                if (World is Level l) { l.Statistics(); }
            }
            void GoLoadMenu()
            {
                Menu = Sesh.BuildLoadGameMenu();
            }
            void Bind(Binding b)
            {
                ConfigBinding = b;
                ConfigPad = false;
                Menu.GoTo(ListenForKeyMenu);
            }
            void BindP(Binding b)
            {
                ConfigBinding = b;
                ConfigPad = true;
                Menu.GoTo(ListenForKeyMenu);
            }
            bool KeyPressed(Keys k)
            {
                if (ConfigPad) return false;
                if (ConfigBinding is null) return false;

                ConfigBinding.Key = k;
                ConfigBinding.RemoveDuplicates();
                Menu.GoBack();
                return true;
            }
            bool ButtonPressed(Buttons b)
            {
                if (!ConfigPad) return false;
                if (ConfigBinding is null) return false;

                ConfigBinding.Button = b;
                ConfigBinding.ButtonBound = true;
                ConfigBinding.RemoveDuplicates();

                Menu.GoBack();
                return true;

            }

            void InputMenuGo()
            {
                InputMenu.GoBack();         // Remove the menu
                Control.Clear();            // Clear input cache
                InputCallback?.Invoke(Assets.SafeString(InputValue)); // Execute the handler (if any)
            }
            string InputMenuUpdate()
            {
                return ":" + Assets.SafeString(InputValue);
            }
            void levelEdit()
            {
                if (World is Level l) l.Edit(false);
            }
            string levelEditUpdate()
            {
                if (DebugMode) return "LEVEL EDITOR"; else return null;
            }

            ListenForKeyMenu = new Menu("  PRESS THE BUTTON FOR XXXXXXXX  ",
                new MenuItem(),
                new MenuItem("PRESS ESC TO CANCEL", action: null));
            ListenForKeyMenu.Updater = GetSelectedBinding;
            ListenForKeyMenu.OnKey = (k) => KeyPressed(k);
            ListenForKeyMenu.OnButton = (b) => ButtonPressed(b);

            ControlsMenu = new Menu("CONTROLS",
                                new MenuItem("LAYOUT:  XXXXXX", LayoutUp, LayoutDown, LayoutUp, GetLayoutValue),
                                new MenuItem(),
                                new MenuItem("EDIT KEY BINDINGS", EditKeyBindings, GetEditBindings),
                                new MenuItem("EDIT GAMEPAD BINDINGS", EditPadBindings, GetEditBindings2),
                                new MenuItem("BACK TO OPTIONS", true));

            // Create the menus
            OptionsMenu = new Menu("OPTIONS",
                                new MenuItem("SCALE:      XX", ScaleReset, ScaleDown, ScaleUp, GetScaleValue),
                                new MenuItem("VOLUME:     XXX", VolumeDefault, VolumeDown, VolumeUp, GetVolumeValue),
                                new MenuItem("VOLUME:     XXX", VolumeSDefault, VolumeSDown, VolumeSUp, GetVolumeSValue),
                                new MenuItem("VOLUME:     XXX", VolumeMDefault, VolumeMDown, VolumeMUp, GetVolumeMValue),
                                new MenuItem("FULLSCREEN: XXX", ToggleFullscreen, ToggleFullscreen, ToggleFullscreen, GetFullscreen),
                                new MenuItem("CONTROLS", ControlsMenu),
                                new MenuItem(), // Separator
                                new MenuItem("EXIT OPTIONS", true));
            OptionsMenu.OnExit = SaveOptions;

            KeyBindingMenu = new Menu("KEY BINDINGS",
                                new MenuItem("LEFT:     XXXXXXXXXXXX", () => Bind(Control.Left), () => GetBind(Control.Left)),
                                new MenuItem("RIGHT:    XXXXXXXXXXXX", () => Bind(Control.Right), () => GetBind(Control.Right)),
                                new MenuItem("JUMP:     XXXXXXXXXXXX", () => Bind(Control.Jump), () => GetBind(Control.Jump)),
                                new MenuItem("CROUCH:   XXXXXXXXXXXX", () => Bind(Control.Crouch), () => GetBind(Control.Crouch)),
                                new MenuItem("MAGIC:    XXXXXXXXXXXX", () => Bind(Control.Magic), () => GetBind(Control.Magic)),
                                new MenuItem("FIREBALL: XXXXXXXXXXXX", () => Bind(Control.Fireball), () => GetBind(Control.Fireball)),
                                new MenuItem("PAUSE:    XXXXXXXXXXXX", () => Bind(Control.Pause), () => GetBind(Control.Pause)),
                                new MenuItem(), // Separator
                                new MenuItem("DONE", true));
            KeyBindingMenu.OnExit = SaveOptions;

            PadBindingMenu = new Menu("KEY BINDINGS",
                    new MenuItem("LEFT:     XXXXXXXXXXXX", () => BindP(Control.Left), () => GetBindP(Control.Left)),
                    new MenuItem("RIGHT:    XXXXXXXXXXXX", () => BindP(Control.Right), () => GetBindP(Control.Right)),
                    new MenuItem("JUMP:     XXXXXXXXXXXX", () => BindP(Control.Jump), () => GetBindP(Control.Jump)),
                    new MenuItem("CROUCH:   XXXXXXXXXXXX", () => BindP(Control.Crouch), () => GetBindP(Control.Crouch)),
                    new MenuItem("MAGIC:    XXXXXXXXXXXX", () => BindP(Control.Magic), () => GetBindP(Control.Magic)),
                    new MenuItem("FIREBALL: XXXXXXXXXXXX", () => BindP(Control.Fireball), () => GetBindP(Control.Fireball)),
                    new MenuItem("PAUSE:    XXXXXXXXXXXX", () => BindP(Control.Pause), () => GetBindP(Control.Pause)),
                    new MenuItem(), // Separator
                    new MenuItem("DONE", true));
            PadBindingMenu.OnExit = SaveOptions;

            StoryMenu = new Menu("GAME TYPE",
                    new MenuItem("PLUS", StoryClassicPlus),
                    new MenuItem("CLASSIC", StoryClassic)
                    
                    // , new MenuItem("SKX", StorySKX),
                    // new MenuItem("TEST", StoryTest, () => Game.DebugMode ? "TEST" : null)
                    );

            StartMenu = new Menu("MAIN MENU",
                                new MenuItem("NEW GAME", StoryMenu),
                                new MenuItem("LOAD GAME", GoLoadMenu),
                                new MenuItem("OPTIONS", OptionsMenu),
                                new MenuItem("SOUND TEST", SoundTest),
                                new MenuItem("EXIT SKX", Instance.Exit));


            DifficultyMenu = new Menu("SELECT DIFFICULTY",
                    new MenuItem("NORMAL", () => SelectDiff(Difficulty.Normal)),
                    new MenuItem("EASY", () => SelectDiff(Difficulty.Easy)),
                    new MenuItem("HARD", () => SelectDiff(Difficulty.Hard)));

            SlotMenu = new Menu("SELECT SAVE SLOT",
                    new MenuItem("XXXXXXXXXXXXXXXXXXXXXXXXXXXX", () => SelectSlot(1), () => GetSlot(1)),
                    new MenuItem("XXXXXXXXXXXXXXXXXXXXXXXXXXXX", () => SelectSlot(2), () => GetSlot(2)),
                    new MenuItem("XXXXXXXXXXXXXXXXXXXXXXXXXXXX", () => SelectSlot(3), () => GetSlot(3)),
                    new MenuItem("XXXXXXXXXXXXXXXXXXXXXXXXXXXX", () => SelectSlot(4), () => GetSlot(4)));

            QuitGameMenu = new Menu("QUIT?  ARE YOU SURE?",
                                new MenuItem("YES", QuitGame),
                                new MenuItem("NO", () => QuitGameMenu.GoBack()));

            ConfirmOverwriteMenu = new Menu("OVERWRITE?  ARE YOU SURE?",
                    new MenuItem("YES", NewGame),
                    new MenuItem("NO", () => ConfirmOverwriteMenu.GoBack()));


            PauseMenu = new Menu("- PAUSED -",
                                new MenuItem("CONTINUE", () => PauseMenu.GoBack()),
                                new MenuItem("LEVEL EDITOR", levelEdit, levelEditUpdate),
                                new MenuItem("RELOAD GAME", ReloadSaveSlot),
                                new MenuItem("STATISTICS", Statistics),
                                new MenuItem("OPTIONS", OptionsMenu),
                                new MenuItem("QUIT GAME", QuitGameMenu),
                                new MenuItem("EXIT SKX", Instance.Exit));

            PauseMenu.OnKey = PauseMenuKeyDown;
            PauseMenu.OnBack = () => { Pause = false; UpdateTitle(); SetScale(Options.Scale); };

            EditorMenu = new Menu("LEVEL EDITOR",
                                new MenuItem("[c=ffff]ESC    [c=ffffff]CONTINUE", () => PauseMenu.GoBack()),
                                new MenuItem("[c=ffff]F11    [c=ffffff]RESUME GAMEPLAY", () => { EditorMode.Resume(); Pause = false; }),
                                new MenuItem("[c=ffff]S+F11  [c=ffffff]TEST ROOM", () => { EditorMode.Test(); Pause = false; }),
                                new MenuItem("[c=ffff]F3     [c=ffffff]SAVE TO FILE", () => EditorMode.SaveFile()),
                                new MenuItem("[c=ffff]S+F4   [c=ffffff]SAVE ROOM AS", () => EditorMode.SaveRoomAs()),
                                new MenuItem("[c=ffff]C+F4   [c=ffffff]COPY TO STORY", () => EditorMode.CopyToStory()),
                                new MenuItem("[c=ffff]S+F3   [c=ffffff]DELETE FILE", () => EditorMode.DelFile()),
                                new MenuItem("[c=ffff]F4     [c=ffffff]SWAP ROOMS", () => EditorMode.SwapRooms()),
                                new MenuItem("[c=ffff]N      [c=ffffff]RENAME ROOM", () => (EditorMode as Editor.LayoutMode).EditName()),
                                new MenuItem("[c=ffff]G      [c=ffffff]GO TO ROOM", () => EditorMode.GoToRoom()),
                                new MenuItem("[c=ffff]C+F11  [c=ffffff]LEVEL SELECT", () => Game.SwitchMode(new Modes.LevelSelect())),
                                new MenuItem("[c=ffff]       [c=ffffff]BUILD MERGED CSPACE", () => Editor.EditorMode.Build(true)),
                                new MenuItem("[c=ffff]       [c=ffffff]BUILD NEW CSPACE", () => Editor.EditorMode.Build(false)),
                                new MenuItem("[c=ffff]?      [c=ffffff]HELP", () => EditorMode.EditorHelp()),
                                new MenuItem("[c=ffff]       [c=ffffff]QUIT GAME", QuitGameMenu),
                                new MenuItem("[c=ffff]A+F4   [c=ffffff]EXIT SKX", Instance.Exit));


            EditorMenu.OnKey = PauseMenuKeyDown;
            EditorMenu.OnBack = () => { Pause = false; Game.UpdateTitle(); };

            InputMenu = new Menu("ENTER INPUT              :",
                              new MenuItem(".", InputMenuGo, InputMenuUpdate));

            InputMenu.Updater = () => InputCaption ?? "ENTER VALUE:";
        }
        
        #endregion

    }

    /// <summary>
    /// Valid values for which story we're playing
    /// </summary>
    public enum Story 
    {
        Classic,
        Plus,
        SKX,
        Test
    }

    /// <summary>
    /// Valid values for the starting difficulty of a save slot
    /// </summary>
    public enum Difficulty
    {
        Normal,
        Easy,
        Hard
    }

    /// <summary>
    /// Represents user-controlled game options which get saved to disk, such
    /// as window scale, volume, control bindings, etc.
    /// </summary>
    public class Options
    {
        public double Scale { get; set; } = 2.0;            // 0.5 to 4.5
        public double Volume { get; set; } = 1.0;           // 0.0 to 1.0
        public double MusicVol { get; set; } = 1.0;         // 0.0 to 1.0
        public double SoundVol { get; set; } = 1.0;         // 0.0 to 1.0
        public bool DebugMode { get; set; } = false;
        public bool FullScreen { get; set; } = false;       
        public List<SavedBinding> Bindings { get; set; } = new List<SavedBinding>();
        public ControlLayout ControlLayout { get; set; } = ControlLayout.Arrows;

        public int HiGDV { get; set; } = 47;
        public int Duration { get; set; }

        /// <summary>
        /// Pushes the Volume settings from Options into Sound
        /// </summary>
        internal void UpdateVolume()
        {
            SoundEffect.MasterVolume = (float)Volume;
            Sound.MusicVolume = MusicVol;
            Sound.SoundVolume = SoundVol;
            Sound.UpdateVolumes();
        }

        /// <summary>
        /// Checks to see if we should auto-start the game based on
        /// the /s argument and/or the use of left-shift to toggle the
        /// setting
        /// </summary>
        public bool TestAutoStart()
        {
             if (Game.SuppressAutoStart > 0) return false;

            var shift = Control.KeyboardState.IsKeyDown(Keys.LeftShift);
            return shift ? !Game.AutoStart : Game.AutoStart;
        }
    }

    public enum ControlLayout
    {
        Arrows,
        WASD,
        Custom
    }
}

