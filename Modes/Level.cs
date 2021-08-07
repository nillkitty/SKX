using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SKX
{

    /// <summary>
    /// Represents the currently executing level (room in constellation space) with all the fixins'
    /// </summary>
    public partial class Level : World
    {

        public Layout Layout;                   // The level's layout
        public LevelState State;                // The state of the current level
        public Point DoorExited;                // Location of the door Dana last exited from
        public Point CameraTargetPos;           // The place the camera is panning to
        public int CameraSpeed = 5;             // How many pixels per frame the camera can pan
        public int WarpCameraSpeed = 10;        // How many pixels per frame camera pans while following a warp
        public bool ForceHidden;                // Force next room to be a hidden?
        public int EnemyCount = 0;              // Number of enemies in the level (updated by UpdateObjects)
        public bool MagicDown = false;          // Whether Dana is holding the Magic button,
                                                // used for toggle blocks

        public Rectangle WorldRectangle => new Rectangle(0, 0, WorldWidth, WorldHeight);
        public CameraMode CameraMode;           // Current camera mode

        // Sound references
        public Sound MusicLow;                  // The music that plays when Dana has < 2000 life
        public Sound Music;                     // The music that plays when Dana has >= 2000 life

        /* Behavioral Parameters */
        private static readonly uint TitleScreenTicks = 140;     // Duration of title screen
        private static readonly uint LightsOutTicks = 120;       // key and door intro
        private static readonly uint ThankYouDanaTicks = 180;    // delay after life/score transfer
        private static readonly uint PostDeathTicks = 120;       // delay after death sequence
        private static readonly uint TimeOverTicks = 120;        // time over sequence duration
        private static readonly uint TimeOverTicks2 = 120;       // delay after time over sequence
        private static readonly uint DanaAppearanceTicks = 30;   // short delay before level begins
        private static readonly uint OpenBookOfSolomonTicks = 30;    // delay after opening the book of Solomon
        internal static readonly uint RefreezeTicks = 80;        // Delay until a shade of ice cracking reverts
        private static readonly int MaxDroplets = 500;           // Total droplets that can be active at once

        /* Internals */
        private Objects.StarCircle Circle;      // The star circle (intro, outro)
        private Objects.StarPath Path;          // The star path (opening door, warping)
        private Sesh Sesh => Game.Sesh;         // Makes code tidier
        private bool RenderEnemies = true;      // Are we rendering enemies?
        private Tile[] OldBackground;           // Used to recover from vapourwave mode
        internal GameObject DebugObject;        // Selected debug object (Shift+F10)
        internal GameObject CameraTarget;       // What the camera is following (Dana? Star path?)
        internal bool VapourMode;
        internal bool FinalGameOver;            // Used when the game is over over (no continue -- restarts game)
        public uint RunTicks;                   // How many ticks we've been in the Running state for

        internal Demo Recording;                  // The demo, if we're recording one
        internal Demo Playback;                   // The demo, if we're playing one
        internal DemoFrame LastDemoFrame;         // Last frame recorded, previous frame played
        internal DemoFrame CurrentDemoFrame;      // Current frame playing
        private int LastDemoIndex;                // Last frame index played

        private static Point DefaultCamPosition = new Point(8, 16); // Default place to put the camera
        private int DebugIndex;                 // Used to index through the objects in the left (Shift+F10)
        private int QueuedFairies;              // How many fairies are queued up at the door
        private int QueuedJacks;                // How many mighty bomb jacks are queued up at the door
        private int FairyTimer;                 // How many ticks until the next fairy can come out
        private uint RoutineTimer = 0;          // Automatically decremented by Update()
        private int SpawnPhase = 0;             // Spawn phase (0 or 1)
        private int SpawnTick = 0;              // Spawn tick (0 to 31)
        private Color LowLifeColor = new Color((byte)228, (byte)229, (byte)148, (byte)255);
        private Sound LastMusic;                // Which music we last paused
        private Point SolomonCell;              // Used in the finale

        // Dana's ice picking ability
        internal Point FrozenCracked;            // Cell that Dana has been picking at
        internal uint FrozenCount;               // How many times Dana has picked at the frozen block
        internal uint FrozenTime;                // How many ticks remaining until we re-freeze a shade

        /* Constructor */
        public Level(int tWidth, int tHeight, Demo demo = null) : base(tWidth, tHeight)
        {

            // Musical defaults
            Music = Sound.Intro;
            MusicLow = Sound.LowTime;

            // Layout
            Layout = new Layout(this, tWidth, tHeight);
            
            // Init the editor
            EditInit();

            // Set up any demo we're playing
            if (demo != null)
            {
                Playback = demo;
            } else if (Game.Options.TestAutoStart())
            {
                // If we're not in a demo, see if we should skip the title screen
                // for developer ease of use :)
                RoutineTimer = DanaAppearanceTicks;
                State = LevelState.PreRun;
            }
        }

        public void PreRun(bool quiet)
        {
            RenderEnemies = true;
            UpdateSpells();             // Do this here so that anything that immediately changes does so prior
                                        // to the level displaying which would create a jump cut

            Layout.CheckPromote();

            State = LevelState.Running;
            if (!quiet) PlayMusic();
        }

        /// <summary>
        /// Obtains the proper pause menu based on whether we're in Edit or Run mode
        /// </summary>
        public override Menu PauseMenu => 
            State == LevelState.Edit ? Game.EditorMenu : Game.PauseMenu;

        public Color SparkleColor => (Game.Ticks % 20) switch { 
            uint t when t < 5 => Color.Pink,
            uint t when t < 10 => Color.LightBlue,
            uint t when t < 15 => Color.White,
            _ => Color.LightBlue
        };

        /// <summary>
        /// Resets the camera immediately to a given target
        /// </summary>
        public void ResetCamera(Point target)
        {
            CameraTarget = null;
            CameraCenterOn(target);
            Game.CameraPos = CameraTargetPos;
        }

        /// <summary>
        /// Resets the camera to default position
        /// </summary>
        public void ResetCamera()
        {
            CameraTarget = null;
            CameraTargetPos = Layout.CameraStart;
            Game.CameraPos = CameraTargetPos;
        }

        /// <summary>
        /// Starts the level at the title screen
        /// </summary>
        public void DoTitleScreen() {

            Sound.StopAll();
            ClearAudioEffect();
            RoutineTimer = TitleScreenTicks; 
            State = LevelState.TitleScreen;
            ResetCamera();
            CameraCenterOn(Sesh.Apprentice ? Layout.AdamStart : Layout.DanaStart);
            Game.FadeIn();
        }

        /// <summary>
        /// Starts the level in the lights out intro state
        /// </summary>
        void DoLightsOut() 
        { 
            RenderEnemies = false;  
            RoutineTimer = LightsOutTicks;
            Game.CameraPos = Layout.CameraStart;
            
            State = LevelState.LightsOut; 
        }

        /// <summary>
        /// Advances the level to the key animation state
        /// </summary>
        void DoKeyAnimation() 
        {

            var doors = new List<Point>();
            foreach(var d in Sesh.DoorsOpened.Where(x => x.Room == Sesh.RoomNumber))
            {
                var c = Layout[d.Door];
                if (!Layout.IsDoorAtAll(c))
                {
                    // This can happen if the door was positioned randomly
                    // or has moved since Dana got the key (edited?)
                    continue;
                }
                if (c == Cell.DoorBlue)
                {
                    // We don't animate these since it's not the exit door
                    Layout[d.Door] = Cell.DoorOpenBlue;
                    continue;
                } else if (Layout.IsHidden(c) || Layout.IsCovered(c) || Layout.IsCracked(c)) 
                {
                    // We don't animate these since they're not visible at the start of the level
                    Layout[d.Door] = c.GetModifier() | Cell.DoorOpen;
                } else
                {
                    doors.Add(d.Door);
                }
            }
            if (doors.Count > 0)
            {
                KeyToDoorsAnimation(doors, Dana.Position);
                State = LevelState.KeyStars;
            }
            else
            {
                /* No doors visible to open */
                RoutineTimer = DanaAppearanceTicks;
                State = LevelState.PreRun;
            }

        }

        /// <summary>
        /// Queues a fairy at the door
        /// </summary>
        public void SpawnFairy()
        {
            QueuedFairies++;
        }

        /// <summary>
        /// Queues a mighty bomb jack at the door
        /// </summary>
        public void SpawnJack()
        {
            QueuedJacks++;
        }

        /// <summary>
        /// Warps Dana to a destination cell
        /// </summary>
        public void Warp(Point dest)
        {

            Dana.State = SKX.Objects.DanaState.Warping;     // Put Dana in warp drive
            Dana.Vx = 0;                                    // Stop all movement
            Dana.Vy = 0;
            Dana.OnFloor = false;       
            Dana.Jumping = false;
            Dana.Falling = true;
            Dana.DanaCollideLevel(Heading.Down);            // Pre-collide Dana to the level
            State = LevelState.Warping;                     // Put Level in warp drive
            Path = new Objects.StarPath(this, Dana.Position, dest.ToWorld(), 30, false);
            AddObject(Path);                                // Make a pretty animation
            CameraTarget = Path;                            // Follow the twinkle
            Dana.Position = dest.ToWorld();                 // Move Dana

            if (!Game.CameraRect.Contains(Dana.Position) && !Layout.IsDefaultSize())  // If Dana's going off screen, unlock the tripod
            {
                CameraMode = CameraMode.Unlocked;
            }
            Sound.Warp.Interrupt();                         // Interrupt time and space with the warp sound

        }
        /// <summary>
        /// Gets the frozen tile graphic for a given
        /// frozen tile
        /// </summary>
        public (Tile tile, Color color, Color Color2) GetFrozenTile(int x, int y)
        {
            if (new Point(x, y) == FrozenCracked)
            {
                return FrozenCount switch
                {
                    uint i when i > 6 => (Tile.FrozenCrackedB, new Color(0.9f, 0.95f, 1.0f, 0.1f), Color.White * 0.65f),
                    uint i when i > 3 => (Tile.FrozenCrackedA, new Color(0.8f, 0.9f, 1.0f, 0.2f), Color.White * 0.60f),
                    3 => (Tile.FrozenCrackedA, new Color(0.7f, 0.85f, 1.0f, 0.3f), Color.White * 0.55f),
                    2 => (Tile.FrozenCrackedA, new Color(0.6f, 0.8f, 1.0f, 0.4f), Color.White * 0.50f),
                    1 => (Tile.FrozenCrackedA, new Color(0.5f, 0.75f, 1.0f, 0.5f), Color.White * 0.45f),
                    _ => (Tile.FrozenBlock, new Color(0.4f, 0.7f, 1.0f, 0.5f), Color.White * 0.40f)
                };
            }

            return (Tile.FrozenBlock, new Color(0.4f, 0.7f, 1.0f, 1.0f), Color.White * 0.33f);
        }

        /// <summary>
        /// Triggers the "door opening" sequence
        /// </summary>
        /// <param name="doors">List of door cells to animate to</param>
        /// <param name="from_pos">Key cell to animation from</param>
        public void KeyToDoorsAnimation(IEnumerable<Point> doors, Point from_pos)
        {
            foreach (var d in doors)
            {
                var c = Layout[d];
                if (c.GetContents() != Cell.InvisibleDoor && (Layout.IsHidden(c) || Layout.IsCovered(c) || Layout.IsCracked(c)))
                {
                    // Door is hidden or behind a block,  just open it immediately,  don't reveal where it is
                    Layout.OpenDoor(d);
                }
                else
                {
                    // Door is visible, or it's a hidden door that we're revealing, do the pretty animation
                    Path = new Objects.StarPath(this, from_pos, d.ToWorld(), 60, c == Cell.DarkDoorClosed);
                    Path.OnFinished = () => Layout.OpenDoor(d);
                    AddObject(Path);
                    State = LevelState.OpeningDoor;
                }

            }
        }

        /// <summary>
        /// Game logic that runs on every tick (including during Pause)
        /// </summary>
        public override void PauseUpdate(GameTime gameTime)
        {
            // Check for debug keys
            if (Game.DebugMode)
            {
                Control.CheckDebugKeys();
            }

            // Check for pause toggle
            if (Game.HelpText is null && (Control.Pause.Pressed(false) || Control.Esc.Pressed(false)))
            {
                // Check for demo exit
                if (Sesh.DemoPlayback && Playback != null)
                {
                    Game.DemoEnded();
                    return;
                }

                // Check editor for pause
                if (State == LevelState.Edit && Editor.OnEscPressed()) return;
 
                Game.DebugPause = false;
                Game.Pause = !Game.Pause;
                if (Game.Pause)
                {
                    Sound.Pause.Play();
                    Game.UpdateTitle();
                }
            }
        }

        /// <summary>
        /// Advance level state to Dana's intro animation
        /// </summary>
        void DoDanaAnimation() 
        {
            RenderEnemies = false;
            Sesh.SkipThankYou = false;

            Sound.Start.Play(); 
            State = LevelState.DanaStars;

            // Force update Dana's position, it might have changed when
            // the character changed etc.
            if (Sesh.WarpTo.X > 0)
                Dana.Position = Sesh.WarpTo.ToWorld();
            else
                Dana.Position = Sesh.Apprentice ? Layout.AdamStart.ToWorld() : Layout.DanaStart.ToWorld();

            Dana.Direction = Layout.StartDirection ? Heading.Left : Heading.Right;
            Dana.FlipX = Layout.StartDirection;

            Circle = SKX.Objects.StarCircle.InwardCircle(this, Dana.Position, Sesh.FastStars);

            AddObject(Circle);

            if (Layout.CameraMode == CameraMode.Unlocked || !Dana.IsOnScreen())
            {
                CameraTarget = Circle;  // Follow the circle
                UpdateCamera();
            }

        }

        /// <summary>
        /// Advance level state to "exit" animation.  Called by the Dana object.
        /// </summary>
        public void DoDoorAnimation() 
        {
            ClearAudioEffect();
            VapourModeOff();

            RenderEnemies = false;

            // Clear all objects
            ObjectsToRemove.AddRange(Objects);

            // Add the stars
            if (DoorExited.X == 0)
                Circle = SKX.Objects.StarCircle.OutwardCircle(this, Dana.Position, Sesh.FastStars);
            else
                Circle = SKX.Objects.StarCircle.OutwardCircle(this, DoorExited.ToWorld(), Sesh.FastStars);

            AddObject(Circle);

            StopMusic();
            Sound.Door.Play();

            State = LevelState.DoorStars;
            ExecuteExitSpells();    // Must be after state change

        }

        /// <summary>
        /// Stops the level's music
        /// </summary>
        public void StopMusic()
        {
            Music.Stop();
            MusicLow.Stop();
        }

        /// <summary>
        /// Pauses the level's music
        /// </summary>
        public void PauseMusic()
        {
            if (MusicLow.IsPlaying) LastMusic = MusicLow;
            else LastMusic = Music;

            LastMusic.Stop(true);
        }

        /// <summary>
        /// Resumes the level's music
        /// </summary>
        public void ResumeMusic()
        {
            if (LastMusic is null) LastMusic = Music;
            LastMusic.Resume();
        }

        /// <summary>
        /// Plays (or restarts) the level's music.
        /// </summary>
        public void PlayMusic()
        {
            InitMusic();
            StopMusic();
            ClearAudioEffect();
            if (Playback != null) return;
            ApplyAudioEffect();

            if (Life <= 2000)        // The level might start with less than 2000 life
                MusicLow.Play();
            else            
                Music.Play();
        }

        /// <summary>
        /// Begin recording a demo on the current level
        /// </summary>
        public void RecordDemo()
        {
            Recording = new Demo()
            {
                 RoomNumber = Layout.RoomNumber,
                 Story = Layout.Story
            };

            Restart(true, true);

        }

        /// <summary>
        /// Finishes a demo recording
        /// </summary>
        public void StopRecordingDemo()
        {
            if (Recording is null)
            {
                Game.StatusMessage("NOT RECORDING");
                return;
            }

            if (Recording.Frames.Count == 0)
            {
                Game.StatusMessage("NO KEY FRAMES TO SAVE");
                return;
            }

            Recording.Duration = Ticks;
            if (Recording.SaveFile($"demo_{Layout.RoomNumber}{Layout.Story.ToStoryID()}.json"))
            {
                Game.StatusMessage("DEMO FILE SAVED");
                Recording = null;
            }
            else {
                Game.StatusMessage("SAVE FAILED -- STILL RECORDING");
            }

        }

        /// <summary>
        /// Disables vaporwave mode
        /// </summary>
        public void VapourModeOff()
        {
            if (!VapourMode) return;
            VapourMode = false;

            StopMusic();            // Stop the local forecast music
            PlayMusic();            // Start the normal music
            Dana.LifeStep = 10;     // Fix the life thing
            Game.Swaps.Clear();     // Restore the "palette"
            BackgroundColor = Layout.BackgroundColor;   // Restore the background color and background
            if (OldBackground != null) Array.Copy(OldBackground, Layout.Background, OldBackground.Length);
            
            // Piss everyone off
            foreach (var x in Objects)
            {
                if (x.Friendly)
                {
                    x.Friendly = false;
                    x.AnimSpeed = 1;
                    x.Color = Color.White;
                }
            }
            
        }

        /// <summary>
        /// Turn on vaporwave mode
        /// </summary>
        public void VapourModeOn()
        {
            if (VapourMode) return;

            // Turn on the background shader
            VapourMode = true;

            if (Life < 2000) Life += 2000;          // Fix life
            Dana.DestroySpawnPoints();             // Explode the mirrors
            Explode(Cell.ExplosionJar);             // Explode the transient enemies
            StopMusic();                            // Turn off the establishment music
            ClearAudioEffect();                     // Turn off the fuzz
            Music = Sound.Vapour;                   // Turn on the a e s t h e t i c
            Music.Play();               
            Dana.LifeStep = 1;                      // Chill it out

            // Save the background
            OldBackground = Layout.Background;      
            Layout.Background = (Tile[])Layout.Background.Clone();
            for(int i = 0; i < Layout.Width * Layout.Height; i++)
            {
                Layout.Background[i] = Game.SelectRandom(Tile.Empty, Tile.Empty, Tile.Empty, Tile.Empty,
                    Tile.BgStars1, Tile.BgStars1, Tile.BgStars2, Tile.BgStars2, Tile.BgStars3, Tile.BgStars4);
            }

            // Smoke up all the enemies
            foreach (var x in Objects)
            {
                if (x.HurtsPlayer)
                {
                    x.Friendly = true;
                    x.AnimSpeed = 10;
                    x.Color = new Color(255, 200, 255);
                }
            }

            // Night mode
            BackgroundColor = Color.Black;
             
            // Hack the "palette"
            Swap.Register(new Color(75, 205, 222, 255), Color.Cyan, "vapor1");                  // Dana hat color
            Swap.Register(new Color(254, 129, 112, 255), Color.Magenta, "vapor2");              // Dana cape color
            Swap.Register(new Color(184, 248, 24, 255), Color.SkyBlue, "vapor3");               // 
            Swap.Register(new Color(248, 56, 0, 255), Color.HotPink, "vapor4");                 // 
            Swap.Register(Color.White, new Color(230, 217, 222), "vapor5");                     // Fuck up white balance
        }

        // Initializes the music references
        void InitMusic()
        {
            switch(Layout.Music)
            {
                // Song selection
                case 0:
                default:
                    Music = Sound.Intro;
                    MusicLow = Sound.LowTime;
                    break;
                case 1:
                    Music = Sound.HiddenIntro;
                    MusicLow = Sound.LowTime;
                    break;
                case 2:
                    Music = Sound.BB;
                    MusicLow = Sound.BBLow;
                    break;
                case 3:
                    Music = Sound.Bonzai;
                    MusicLow = Sound.LowTime;
                    break;
                case 4:
                    Music = Sound.MillIntro;
                    MusicLow = Sound.LowTime;
                    break;

            }

            // Reset the dynamic pan/fade which is probably still set from last time
            // it was used.
            if (Music is MultiTrack mt)
            {
                mt.ResetMix();
            }
        }

        /// <summary>
        /// Advance level state to "thank you Dana" screen
        /// </summary>
        void DoThankYouDana()
        {
            ResetCamera();
            // Clear out per-room session stuff
            Sesh.OnNextLevel();
            if (Sesh.SkipThankYou)
            {
                ReloadLevel();
                return;
            }
            StopMusic(); // Just in case
            Sound.ThankYou.Play();
            RoutineTimer = ThankYouDanaTicks; 
            State = LevelState.ThankYouDana; 
        }

        /// <summary>
        /// Sets up the level (called by Game -> World -> Level)
        /// </summary>
        public override void Init()
        {
            TileWidth = Layout.Width;
            TileHeight = Layout.Height;
            WorldWidth = TileWidth * Game.NativeTileSize.X;
            WorldHeight = TileHeight * Game.NativeTileSize.Y;
            Layout.OnLoaded();
            UpdateCharacter();
            Game.UpdateTitle();
            LoadDana();
            ExecutePreloadSpells();
            
        }

        /// <summary>
        /// Executes any spells with a PreLoad trigger
        /// before the level begins
        /// </summary>
        private void ExecutePreloadSpells()
        {
            foreach(var s in Layout.Spells)
            {
                if (s.Trigger != SpellTrigger.PreLoad) continue;
                s.Update();
            }
        }

        /// <summary>
        /// Executes any spells with an Exit trigger before
        /// the level unloads or before any door exits are
        /// processed
        /// </summary>
        private void ExecuteExitSpells()
        {
            foreach (var s in Layout.Spells)
            {
                if (s.Trigger != SpellTrigger.ExitLevel) continue;
                s.Update();
            }
        }

        /// <summary>
        /// Loads Dana at the start position if he's not loaded
        /// </summary>
        public void LoadDana()
        {
            UpdateCharacter();

            if (Objects.Any(x => x is Objects.Dana)) return;

            Dana = new Objects.Dana(this);
            AddObject(Dana);
            if (Sesh.WarpTo.X > 0)
                Dana.Position = Sesh.WarpTo.ToWorld();
            else
                Dana.Position = Sesh.Apprentice ? Layout.AdamStart.ToWorld() : Layout.DanaStart.ToWorld();

            // Set Dana's direction
            Dana.Direction = Layout.StartDirection ? Heading.Left : Heading.Right;
            Dana.FlipX = Layout.StartDirection;

            // Set Dana as the default debug (collision view) object
            DebugObject = Dana;

        }

        /// <summary>
        /// Clears all GameObjects in the level
        /// </summary>
        public void ClearObjects()
        {
            ObjectsToAdd.Clear();
            Objects.Clear();
        }

        /// <summary>
        /// Renders the HUD
        /// </summary>
        public override void RenderHUD(SpriteBatch batch)
        {
            if (State == LevelState.Edit) { Editor.HUD(batch); return; }

            const int line_d1 = 0;        // Top most line,  usually empty except debug info
            const int line_d2 = 8;        // Next line, usually empty except debug info
            const int line_1 = 16;        // First HUD line (e.g. SCORE, LIFE, etc.)
            const int line_2 = 24;        // Second HUD line (e.g. values for score, life, etc.)


            /* Render HUD contents */
            // SCORE
            var scoreText = Sesh.Score.ToString();
            batch.DrawString("SCORE", new Point(32, line_1), Color.White);
            batch.DrawString(scoreText, new Point(72 - 8 * scoreText.Length, line_2), Color.White);
            
            // LIFE
            Color lifeColor = (Life <= 2000 && State != LevelState.ThankYouDana) ? LowLifeColor : Color.White;
            var lifeText = Life.ToString();
            batch.DrawString("LIFE", new Point(96, line_1), lifeColor);
            batch.DrawString(lifeText, new Point(128 - 8 * lifeText.Length, line_2), lifeColor);

            // FAIRIES
            batch.DrawString("fx", new Point(136, line_1), Color.White);
            batch.DrawString(Sesh.Fairies.ToString(), new Point(136 + 16, line_1), Color.White);

            // LIVES
            Color livesColor = (Sesh.Lives < 2) ? LowLifeColor : Color.White;
            batch.DrawString(Sesh.Apprentice ? "ax" : "dx", new Point(136, line_2), livesColor);
            batch.DrawString(Sesh.Lives.ToString(), new Point(136 + 16, line_2), livesColor);

            // Debug music info
            if (Game.ShowMusic)
            {
                batch.DrawOutlinedString(Music.ToString().Trim(), new Point(16, 240 - 32), Color.White);
                batch.DrawOutlinedString(MusicLow.ToString().Trim(), new Point(16, 240 - 32 + 8), Color.White);
            }

            // Debug music info
            if (Game.ShowMagic)
            {
                int yy = 240 - 32;
                foreach(var s in Layout.Spells)
                {
                    var spid = s.ID.ToString("0000");
                    var trig = s.Trigger.ToString().ToUpper();
                    var fin = s.Finished ? "DONE" : "Rx" + s.RequireCount.ToString("00");
                    var type = s.Type.ToString().ToUpper();

                    batch.DrawShadowedString($"{s.ID,-4} {trig,11} {fin,4} {type}",
                    new Point(16, yy), Color.White);
                    yy -= 8;
                    if (yy < 32) break;
                }
            }


            // Debug inventory info
            if (Game.ShowInventory)
            {
                int y = 32;
                foreach(var i in Sesh.Inventory)
                {
                    batch.DrawShadowedString(i.ToString(), new Point(8, y), Color.White, 1);
                    y += 8;
                }
                foreach(var i in Sesh.SpellsExecuted)
                {
                    batch.DrawShadowedString(i.ToString(), new Point(8, y), Color.White, 1);
                    y += 8;
                }
            }

            if (Game.DebugMode && Dana != null)
            {
                // Debug mode

                // FPS
                batch.DrawString("FPS " + Game.FPS.ToString("0.00"), new Point(0, line_d2), Color.Gray);

                // Camera Pos
                batch.DrawString($"{Game.CameraPos.X:X2}.{Game.CameraPos.Y:X2}", new Point(16 * 8, line_d1), Color.AliceBlue);

                // Spawn phase:tick
                batch.DrawString($"SP{SpawnPhase}:{SpawnTick}", new Point(0, line_d1), Color.AliceBlue);

                // Obj Count
                batch.DrawString($"OBJ:{Objects.Count}", new Point(56, line_d1), Color.AliceBlue);

                // Dana flags
                batch.DrawString("U", new Point(176, line_d1), Dana.Collision2.UpBlock.HasValue ?
                    Color.Yellow : Color.White);
                batch.DrawString("D", new Point(176 + 8, line_d1), Dana.Collision2.DownBlock.HasValue ?
                    Color.Green : Color.White);
                batch.DrawString("L", new Point(176 + 16, line_d1), Dana.Collision.LeftBlock.HasValue ?
                    Color.Red : Color.White);
                batch.DrawString("R", new Point(176 + 24, line_d1), Dana.Collision.RightBlock.HasValue ?
                    Color.Blue : Color.White);
                batch.DrawString("J", new Point(176 + 32, line_d1), Dana.Jumping ?
                    Color.Red : Color.White);
                batch.DrawString("F", new Point(176 + 40, line_d1), Dana.Falling ?
                    Color.Red : Color.White);
                batch.DrawString("C", new Point(176 + 48, line_d1), Dana.Crouching ?
                    Color.Red : Color.White);
                batch.DrawString("O", new Point(176 + 56, line_d1), Dana.OnFloor ?
                    Color.Red : Color.White);

                if (!Game.ShowCollision)
                {
                    batch.DrawString($"X {(int)DebugObject.X,3} Y {(int)DebugObject.Y}", 
                        new Point(152, line_d2), Color.White);

                }


            }


            if (Game.ShowCollision) { 
                // Object X pos/velocity
                batch.DrawString("X", new Point(176, line_d2), Color.White);
                batch.DrawString(Math.Round(DebugObject.Vx, 2).ToString(), new Point(176, line_1), Color.Cyan);
                batch.DrawString(((int)DebugObject.X).ToString(), new Point(176, line_2), Color.White);
                
                // Object Y pos/velocity
                batch.DrawString("Y", new Point(176 + 32, line_d2), Color.White);
                batch.DrawString(Math.Round(DebugObject.Vy, 2).ToString(), new Point(176 + 32, line_1), Color.Cyan);
                batch.DrawString(((int)DebugObject.Y).ToString(), new Point(176 + 32, line_2), Color.White);

            }
            else
            {
                RenderScroll(batch);
            }

        }

        void RenderScroll(SpriteBatch batch)
        {

            // Draw scroll
            var size = Math.Max(Sesh.ScrollSize, Sesh.ScrollItems.Count);
            int x = 168;
            int y = size > 9 ? 4 : 16;
            int lines = 1;

            scroll(0, true);  // Beginning of scroll
            int n = 0;
            foreach (var i in Sesh.ScrollItems)
            {
                switch (i)
                {
                    case Cell.FireballJar:
                        scroll(2);
                        n++;
                        break;
                    case Cell.SuperFireballJar:
                        scroll(1);
                        n++;
                        break;
                    case Cell.RedFireballJar:
                        scroll(3);
                        n++;
                        break;
                    case Cell.SnowballJar:
                        scroll(4);
                        n++;
                        break;
                    case Cell.SuperSnowballJar:
                        scroll(5);
                        n++;
                        break;
                }
            }
            for (; n < Sesh.ScrollSize; n++)
            {
                scroll(13);  // Empty spot
            }
            scroll(14, true);  // The torn end
            scroll(15, true);  // The tip

            // Subroutine to blit out a chunk of the scroll onto the HUD
            void scroll(byte o, bool reentrant = false)
            {
                if (x > 240 && lines < 3 && !reentrant)
                {
                    lines++;            // we're on the next line
                    scroll(14, true);   // The torn end
                    scroll(15, true);   // The tip
                    x = 168;
                    y += 12;
                    scroll(0, true);
                }

                batch.Draw(Game.Assets.Text, new Rectangle(x, y, 8, 16), Game.Assets.ScrollSourceTileRect(o),
                    Sesh.ScrollDisabled ? Color.Gray : Color.White);
                x += 8;
            }
        }

        /// <summary>
        /// Checks to see if an object is active in the level
        /// </summary>
        public bool ContainsObject(GameObject o)
        {
            return Objects.Contains(o) || ObjectsToAdd.Contains(o);
        }


        /// <summary>
        /// Reloads whatever level Sesh.RoomNumber points at and starts it from scratch,
        /// typically after beating the previous level
        /// </summary>
        void ReloadLevel()
        {
            var newlevel = Game.Sesh.BuildLevel();
            Game.World = newlevel;
            newlevel.State = LevelState.Loading;
            newlevel.Init();
            if (Sesh.SkipThankYou)
            {
                newlevel.DoDanaAnimation();
            }
        }

        /// <summary>
        /// Updates CameraTargetPos to an appropriate value to follow Point p
        /// </summary>
        void CameraCenterOn(Point p)
        {

            CameraTargetPos.X = Clamp(
                (int)(p.X - Game.NativeWidth / 2),      // Where we want it to be
                (int)(Layout.CameraBounds.X),           // The furthest left it can be
                (int)(Layout.CameraBounds.Right - Game.NativeWidth)); // The furthest right it can be

            CameraTargetPos.Y = Clamp(
               (int)(p.Y + 32 - Game.NativeWidth / 2),  // Where we want it to be
               (int)(Layout.CameraBounds.Y),           // The furthest up it can be
               (int)(Layout.CameraBounds.Bottom - (Game.NativeHeight - 16))); // The furthest down it can be

         
        }

        // Define our own Clamp function that doesn't throw exceptions or suck
        int Clamp(int v, int min, int max)
        {
            if (v < min) return min;
            if (v > max) return max;
            return v;
        }

        /// <summary>
        /// Updates the camera as needed
        /// </summary>
        void UpdateCamera()
        {
            // Make sure camera target is a current object
            if (CameraTarget == null || !ContainsObject(CameraTarget))
            {
                CameraTarget = Dana;
            }
            if (CameraTarget == null || CameraTarget.Position == Layout.CameraStart)
            {
                return;
            }

            // Check for resize
            foreach(var v in Layout.Resizes)
            {
                if (v.Trigger.Contains(Dana.Position))
                {
                    Layout.CameraBounds = v.NewBounds;
                    break;
                }
            }

            // Check for camera lock
            if (CameraMode == CameraMode.Locked) return;
            if (CameraMode == CameraMode.LockedUntilNear 
                && Game.CameraRect.Contains(Dana.ProjectedBox)) return;

            CameraMode = CameraMode.Unlocked;
            CameraCenterOn(CameraTarget.Position);

            var speed = State == LevelState.Warping ? WarpCameraSpeed : CameraSpeed;

            // Now pan the camera over to the target if necessary...
            if (CameraTargetPos.X > Game.CameraPos.X)
            {
                Game.CameraPos.X += CameraSpeed;
                if (Game.CameraPos.X > CameraTargetPos.X) Game.CameraPos.X = CameraTargetPos.X;

            } else if (CameraTargetPos.X < Game.CameraPos.X)
            {
                Game.CameraPos.X -= CameraSpeed;
                if (Game.CameraPos.X < CameraTargetPos.X) Game.CameraPos.X = CameraTargetPos.X;
            }
            if (CameraTargetPos.Y > Game.CameraPos.Y && CameraTargetPos.Y < WorldHeight - 16)
            {
                Game.CameraPos.Y += CameraSpeed;
                if (Game.CameraPos.Y > CameraTargetPos.Y) Game.CameraPos.Y = CameraTargetPos.Y;
            }
            else if (CameraTargetPos.Y < Game.CameraPos.Y && CameraTargetPos.Y > 0)
            {
                Game.CameraPos.Y -= CameraSpeed;
                if (Game.CameraPos.Y < CameraTargetPos.Y) Game.CameraPos.Y = CameraTargetPos.Y;
            }

        }

        /// <summary>
        /// Spawn any fairies if needed
        /// </summary>
        void UpdateFairyQueue()
        {
            if (FairyTimer > 0) FairyTimer--;
            if (FairyTimer > 0) return;

            if (Layout.FindVisibleDoor(out Point p))
            {
                if (QueuedFairies > 0)
                {
                    AddObject(new Objects.Fairy(this) { Position = p.ToWorld() });
                    FairyTimer = 30;
                    QueuedFairies--;
                }
                if (QueuedJacks > 0)
                {
                    AddObject(new Objects.Jack(this) { Position = p.ToWorld() });
                    FairyTimer = 30;
                    QueuedJacks--;
                }
            }

        }

        /// <summary>
        /// Process spells
        /// </summary>
        void UpdateSpells()
        {
            foreach(var s in Layout.Spells)
            {
                s.Update();
            }
        }

        /// <summary>
        /// Process spawns
        /// </summary>
        void UpdateSpawns()
        {

            // Hard coded droplet limit
            var drop_count = Objects.Count(o => o.Type == ObjType.Droplet);
            if (drop_count < MaxDroplets)
            {

                // Handle water droplets (ever tick)
                foreach(var s in Layout.Spawns)
                {
                    if (s.Disabled) continue;                   // Check disabled
                    if (s.DropletRate < 1) continue;            // No droplets
                    if (Ticks % s.DropletRate > 0) continue;    // Not time yet
                    var rng = Game.Random.Next(0, 100);         
                    if (rng > s.DropletChance) continue;        // Apply chance


                    var pt = new Point(s.X, s.Y).ToWorld() + new Point(6,10);    // Offset center
                    var drop = new Objects.Droplet(this, s.DropletType);
                    drop.Position = pt;
                    drop.TTL = s.TTL;

                    AddObject(drop);

                }
            }

            // Handle conventional spawns
            // One spawn tick is 60 game ticks (about one second)
            if (Ticks % 60 == 0)
            {
                // The first 32 ticks use Phase0;  after that we use Phase1
                if (SpawnTick > 31)
                {
                    SpawnPhase = 1;
                    SpawnTick = 0;
                }
            
                foreach(var s in Layout.Spawns)
                {
                    // If this spawn has anything to offer...
                    if (s.SpawnItems.Count > 0 && !s.Disabled)
                    {
                        // Pull the corresponding bitmask for this spawn phase
                        var mask = SpawnPhase == 0 ? s.Phase0 : s.Phase1;
                        // And figure out what bit we need to test
                        var current = 0x80000000 >> (SpawnTick);
                        if ((mask & current) > 0)
                        {
                            // Need to spawn something
                            var st = s.Dispense();

                            // Make sure it's not an empty object
                            if (st.Type == ObjType.None) continue;

                            // Check to make sure we are allowed to have another one of this type in the room
                            if (st.MaxInstances > 0)
                            {
                                var count = Objects.Count(x => x.Type == st.Type);
                                if (count >= st.MaxInstances) continue;
                            }

                            // Now let's birth it
                            var pt = new Point(s.X, s.Y).ToWorld();
                            var o = GameObject.Create(st.Type, this, st.Direction, st.Flags, pt);

                            if (o != null)
                            {
                                o.TTL = s.TTL;
                                AddObject(o);
                                if (o.Type != ObjType.Droplet)
                                {
                                    // Cloud animation
                                    Layout.DrawSparkleWorld(pt, Animation.SpawnCloud, true, false); 
                                }
                            }
    
                        }   
                    }
                }
                SpawnTick++;
            }
        }

        /// <summary>
        /// Handle demo recording and playback, per-tick
        /// </summary>
        private void UpdateDemo()
        {
            if (Recording != null)
            {
                RecordDemoTick();
            }
            if (Playback != null)
            {
                PlayDemoTick();
            }
        }

        /// <summary>
        /// Called on every tick when a demo is recording
        /// </summary>
        private void RecordDemoTick()
        {
            DemoFrame d;
            d.t = Ticks;
            d.s = Control.FlatState();

            if (LastDemoFrame.t == 0 || LastDemoFrame.s != d.s)
            {
                Recording.Frames.Add(d);
                LastDemoFrame = d;
                if (Game.DebugMode) 
                    Game.StatusMessage($"REC'D {Recording.Frames.Count} KEY FRAMES");
            }

        }

        /// <summary>
        /// Called on every tick when a demo is playing
        /// </summary>
        private void PlayDemoTick()
        {
            if (LastDemoIndex >= Playback.Frames.Count)
            {
                EndDemoPlayback();
                return;
            }

            var f = Playback.Frames[LastDemoIndex];
            if (Ticks >= f.t)
            {
                LastDemoFrame = CurrentDemoFrame;
                CurrentDemoFrame = f;
                LastDemoIndex++;
                if (Game.DebugMode)
                    Game.StatusMessage($"KEY FRAME {LastDemoIndex}");
            }

        }

        /// <summary>
        /// Called when demo playback ends
        /// </summary>
        private void EndDemoPlayback()
        {
            if (Game.DebugMode)
                Game.StatusMessage("DEMO PLAYBACK ENDED.");
            Playback = null;
            if (Sesh.DemoPlayback)
            {
                Game.DemoEnded();
            }
        }

        /// <summary>
        /// Update tick for the entire level
        /// </summary>
        public override void Update(GameTime gameTime)
        {

            Game.DrawHUD = true;

            // Tick up recorded play time
            if (Ticks % 60 == 0) Game.Options.Duration++;

            if (Game.HelpText != null)
            {
                // If help text is up during normal game play then pause the game
                if (State != LevelState.Edit)
                {
                    Game.Pause = true;
                    return;
                }
            }

            if (State != LevelState.Edit) UpdateDemo();

            switch (State)
            {
                
                case LevelState.Edit:
                    EditUpdate();
                    break;
                case LevelState.Loading:
                    DoTitleScreen();
                    break;
                case LevelState.TitleScreen:

                    UpdateObjects(gameTime);    // Required for Dana's controls on the title screen
                    if (--RoutineTimer == 0)
                    {
                        DoLightsOut();
                    }
                    break;

                case LevelState.LightsOut:

                    if (--RoutineTimer == 0)
                    {
                        DoDanaAnimation();
                    }

                    break;

                case LevelState.Warping:

                    if (Path is null || Path.State == 0)
                    {
                        CameraTarget = Dana;
                        State = LevelState.Running;
                    }
                    UpdateCamera();
                    UpdateObjects(gameTime);
                    break;

                case LevelState.KeyStars:

                    if (Path is null || Path.State == 0)
                    {
                        RenderEnemies = true;
                        PlayMusic();
                        State = LevelState.Running;
                    }
                    UpdateCamera();
                    UpdateObjects(gameTime);
                    break;

                case LevelState.DanaStars:

                    if (Circle is null || Circle.State < 1)
                    {
                        // Do we start the level up,  or has dana opened any doors?
                        if (Game.Sesh.DoorsOpened.Count() > 0)
                        {
                            DoKeyAnimation();
                        }
                        else
                        {
                            RoutineTimer = DanaAppearanceTicks;
                            State = LevelState.PreRun;
                        }
                    }

                    UpdateObjects(gameTime);        // For the stars
                    UpdateCamera();                 // For the stars

                    break;

                case LevelState.PreRun:

                    // Brief delay between Dana appearing (when he doesn't have the key)
                    // and the level starting.  Could probably be done with a more clever
                    // LevelState.KeyStars but whatever.

                    if (--RoutineTimer == 0)
                    {
                        PreRun(false);
                    }
                    break;

                case LevelState.OpeningDoor:
                    int count = 0;

                    UpdateCamera();
                    ObjectMaintenance();
                    foreach(var o in Objects)
                    {
                        if (o is Objects.StarPath sp)
                        {
                            if (sp.State == 0) continue;
                            count++;
                            o.Update(gameTime);
                        }
                    }

                    if (count == 0) State = LevelState.Running;
                    break;

                case LevelState.EndingA:

                    if (--RoutineTimer == 0)
                    {
                        DoEndingB();
                        break;
                    }
                    UpdateObjects(gameTime);
                    break;

                case LevelState.EndingB:
                    if (Path is null || Path.State == 0)
                    {
                        Sound.Start.Stop();
                        // Close book
                        Layout[SolomonCell] = Cell.SolBook;
                        Layout[new Point(SolomonCell.X - 1, SolomonCell.Y)] = Cell.Empty;
                        RoutineTimer = OpenBookOfSolomonTicks;
                        State = LevelState.EndingC;
                        break;
                    }
                    UpdateObjects(gameTime);
                    break;
                case LevelState.EndingC:
                    if (--RoutineTimer == 0)
                    {
                        Game.SwitchMode(new Modes.ClassicEnding());
                        break;
                    }
                    break;
                case LevelState.Running:
                    RunTicks++;
                    UpdateSpawns();
                    UpdateSpells();
                    UpdateCamera();
                    UpdateFairyQueue();
                    UpdateFrozen();
                    UpdateObjects(gameTime);
                    break;

                case LevelState.DoorStars:
                    if (Circle is null || Circle.State < 1)
                    {
                        DoThankYouDana();
                    }
                    UpdateCamera();
                    UpdateObjects(gameTime);
                    break;

                case LevelState.ThankYouDana:

                    // Short delay first
                    if (RoutineTimer > ThankYouDanaTicks - 20)
                    {
                        RoutineTimer--;
                        break;
                    }

                    // Count down life next (and transfer to score)
                    if (!Sesh.NoPoints)
                    {
                        if (Life > 0)
                        {
                            if (Ticks % 2 == 0)
                            {
                                Life -= 80;
                                Sesh.Score += 80;
                                if (Life < 80)
                                {
                                    Sesh.Score += Life;
                                    Life = 0;
                                }
                            }
                            break;
                        }
                    }

                    // Check for end of sequence
                    if (--RoutineTimer == 0)
                    {
                        Sesh.NoPoints = false;
                        if (Game.IsClassic)
                        {
                            // Just load the next level,  no fades
                            ReloadLevel();
                            Sound.ThankYou.Stop();
                        } else
                        {
                            // Do a pretty fade
                            Game.FadeOut(() =>
                            {
                                ReloadLevel();

                                // It's weird if you stop the fairies from singing before
                                // the fade, so stop it just before fade in
                                Sound.ThankYou.Stop();  

                            }, 30);
                        }
                    }

                    break;

                case LevelState.Dying:
                    UpdateObjects(gameTime);    // Level will go to Dead status when Dana's done dying
                    RoutineTimer = PostDeathTicks;
                    break;

                case LevelState.Dead:
                    if (--RoutineTimer == 0)
                    {
                        Restart();
                    }
                    break;

                case LevelState.TimeOver:
                    RoutineTimer--;
                    if (RoutineTimer == 0)
                    {
                        RoutineTimer = TimeOverTicks2;
                        State = LevelState.TimeOver2;
                        Flash = false;
                        break;
                    }
                    Flash = (RoutineTimer % 12) < 6;
                    break;

                case LevelState.TimeOver2:
                    RoutineTimer--;
                    if (RoutineTimer == 0)
                    {
                        Restart();
                    }
                    break;
                case LevelState.GameOver:

                    if (Control.CheatPressed())
                    {
                        // User pressed 'Continue' cheat code (aka insert coin)

                        if (FinalGameOver)
                        {
                            // Reset immediately if there are
                            // no more levels to play!
                            Game.Reset();
                            return;
                        }

                        Sound.StopAll();
                        Game.Continue();
                        return;
                    }

                    if (Sound.GameOver.IsPlaying) return;
                    Game.Reset();
                    return;
            }
            
            base.Update(gameTime);
        }

        internal void DoFinalGameOver()
        {
            FinalGameOver = true;
            DoGameOver();
        }

        /// <summary>
        /// Slowly heal broken ice
        /// </summary>
        private void UpdateFrozen()
        {
            if (FrozenTime > 0)
            {
                FrozenTime--;
                if (FrozenTime == 0 && FrozenCount > 0)
                {
                    FrozenCount--;
                    if (FrozenCount > 0) FrozenTime = RefreezeTicks;
                }
            }
        }

        /// <summary>
        /// Renders the level's background
        /// </summary>
        protected override void RenderBackground(SpriteBatch batch)
        {

            if (VapourMode)
            {
                float ticks = (float)(Ticks % 60) / 60f;
                Game.Assets.Vapour.Parameters["time"].SetValue(ticks);
                Game.Assets.Vapour.Parameters["ticks"].SetValue((float)Ticks);
                Game.Assets.Vapour.CurrentTechnique.Passes[0].Apply();
            }

            switch (State)
            {
                // These all get a black background:
                case LevelState.Loading:
                case LevelState.TitleScreen:
                case LevelState.LightsOut:
                case LevelState.KeyStars:
                case LevelState.DanaStars:
                case LevelState.PreRun:
                    batch.GraphicsDevice.Clear(Color.Black);
                    break;
                // These all get the actual background:
                case LevelState.Running:
                case LevelState.Warping:
                case LevelState.OpeningDoor:
                case LevelState.DoorStars:
                case LevelState.Dying:
                case LevelState.Dead:
                case LevelState.TimeOver:
                case LevelState.EndingA:
                case LevelState.EndingB:
                case LevelState.EndingC:
                    Layout.RenderBackground(batch);
                    break;
                case LevelState.Edit:
                    if (Editor.RenderBG) Layout.RenderBackground(batch);
                    break;
                // Thank you Dana text goes on the background.  Why?  Just does.
                case LevelState.ThankYouDana:
                    batch.GraphicsDevice.Clear(BackgroundColor);
                    break;
                    
            }

   

        }

        /// <summary>
        ///  Called in the classic ending sequence to summon the demons into the book
        /// </summary>
        void DoEndingB()
        {
            State = LevelState.EndingB;
            foreach(var o in Objects)
            {
                Path = new Objects.StarPath(this, o.Position, SolomonCell.ToWorld(), 30, false);
                AddObject(Path);
                o.Remove();
            }
            Sound.StopAll();
            Sound.Start.Play();
        }

        /// <summary>
        /// Render Thank You Dana text and fairies
        /// </summary>
        /// <param name="batch"></param>
        void RenderThankYouDana(SpriteBatch batch)
        {
            int y = 56;

            Tile ft = (Ticks % 16 < 8) ? Tile.FairyA : Tile.FairyB;

            // Draw a background rectangle in the event the level's superforeground is getting in
            // the way
            batch.FillRectangle(new RectangleF(16, y - 8, 240, 64), BackgroundColor);

            RenderTileWorld(batch, 48, y - 8, ft, Color.White, SpriteEffects.None);
            RenderTileWorld(batch, 200, y - 8, ft, Color.White, SpriteEffects.FlipHorizontally);

            var yo1 = Game.IsClassic ? 24 : 20;
            var yo2 = Game.IsClassic ? 40 : 40;

            if (Layout.ThankYouTextA is null)
            {
                string excl = Game.IsClassic ? " " : "!";
                batch.DrawShadowedStringCentered($" THANK YOU {Sesh.PlayerName}{excl}", y, Color.White);
                batch.DrawShadowedStringCentered("YOU RELEASED THIS ROOM", yo1 + y, Color.White);
                batch.DrawShadowedStringCentered("TRY NEXT ROOM", yo2 + y, Color.White);
            } else
            {
                batch.DrawShadowedStringCentered(Layout.ThankYouTextA, y, Color.White);
                batch.DrawShadowedStringCentered(Layout.ThankYouTextB, yo1 + y, Color.White);
                batch.DrawShadowedStringCentered(Layout.ThankYouTextC, yo2 + y, Color.White);
            }

        }

        /// <summary>
        /// Invoke Game Over sequence and music
        /// </summary>
        void DoGameOver()
        {
            Sound.GameOver.Play();
            State = LevelState.GameOver;
        }

        /// <summary>
        /// Draws title screen content
        /// </summary>
        /// <param name="batch"></param>
        void RenderTitleScreen(SpriteBatch batch)
        {

            var name = Layout.Name ?? $"ROOM {Layout.RoomNumber,2:X}";
            int shrine = Layout.Shrine < 0 ? Sesh.LastShrine : Layout.Shrine;
            Point offset = new Point(-8, -16);

            /* Draw text */
            batch.DrawStringCentered($"SHRINE   ", 7 * 8, Color.White, true, offset);
            batch.DrawStringCentered(name, 10 * 8, Color.White, true, offset);
            batch.DrawStringCentered($"   x {Game.Sesh.Lives}", 13 * 8, Color.White, true, offset);

            /* Draw shrine */
            RenderTileScreen(batch, 20 * 8 + offset.X, 6 * 8 + 2 + offset.Y, (Tile)Layout.GetShrine(shrine), Color.White);

            /* Draw dana */
            RenderTileScreen(batch, 14 * 8 + offset.X, 12 * 8 + 4 + offset.Y, Sesh.Apprentice ? Tile.Adam : Tile.Dana, Color.White);
            if (!Game.IsClassic)
            {
                batch.DrawStringCentered($" {(Sesh.Apprentice ? "d" : "a")} x {(Sesh.Apprentice ? Game.Sesh.DanaLives : Game.Sesh.AdamLives)}", 
                    15 * 8, Color.White, true, offset);
            }
        }

        /// <summary>
        /// Render the level's foreground and sprites
        /// </summary>
        protected override void RenderForeground(SpriteBatch batch)
        {

            switch (State)
            {
                case LevelState.Loading:
                    // Render nothing here
                    break;

                case LevelState.TitleScreen:
                    Layout.RenderForeground(batch, DrawPhase.Borders);          // Borders
                    RenderTitleScreen(batch);                                   // Title screen
                    if (Layout.UseSFForBorders) 
                        Layout.RenderSuperForeground(batch, DrawPhase.Borders);     // Super FG
                    break;

                case LevelState.LightsOut:                    
                case LevelState.KeyStars:
                case LevelState.DanaStars:
                case LevelState.PreRun:
                    Layout.RenderForeground(batch, DrawPhase.BordersKeyDoor);   // Borders, keys, doors
                    RenderObjects(batch);                                       // Stars, Dana, etc.
                    
                    Layout.RenderSuperForeground(batch, DrawPhase.BordersKeyDoor);     // Super FG
                    break;

                case LevelState.DoorStars:
                case LevelState.OpeningDoor:
                case LevelState.Running:
                case LevelState.Warping:
                case LevelState.Dying:
                case LevelState.Dead:
                case LevelState.TimeOver:
                case LevelState.EndingA:
                case LevelState.EndingB:
                case LevelState.EndingC:
                    Layout.RenderForeground(batch, DrawPhase.Everything);       // The level layout
                    RenderObjects(batch);                                       // Sprites
                    Layout.RenderSuperForeground(batch, DrawPhase.Everything);     // Super FG
                    break;

                case LevelState.TimeOver2:
                    Layout.RenderForeground(batch, DrawPhase.Borders);          // Borders
                    RenderObjects(batch);                                       // Need this for Dana
                    Layout.RenderSuperForeground(batch, DrawPhase.Borders);     // Super FG
                    RenderTimeOverText(batch);                                  // "Time Over"
                    break;

                case LevelState.ThankYouDana:
                    Layout.RenderForeground(batch, DrawPhase.Borders);          // Borders
                    RenderThankYouDana(batch);
                    break;

                case LevelState.GameOver:
                    Layout.RenderForeground(batch, DrawPhase.Borders);          // Borders
                    RenderGameOverText(batch);                                  // Game over screen
                    break;

                case LevelState.Edit:
                    if (!(Editor.Mode is Editor.BackgroundMode))                
                    {
                        if (Editor.RenderFG) Layout.RenderForeground(batch, DrawPhase.Everything);   // Draw level only if not in BG mode
                        if (Editor.RenderObj) RenderObjects(batch);
                    }
                    if (Editor.RenderSFG) Layout.RenderSuperForeground(batch, DrawPhase.Everything);     // Super FG
                    Editor.Render(batch);
                    break;
            }

        }

        /// <summary>
        /// Shows game session statistics
        /// </summary>
        public void Statistics()
        {
            var sb = new StringBuilder();

            sb.Append("STATISTICS\n");
            sb.Append($"[c=ff888888]_____________________________[c=ffffffff]\n");
            sb.Append($"ROOMS CLEARED      [c=ffff00]{Sesh.RoomsCleared}[c=ffffffff]\n");
            sb.Append($"FAIRIES SAVED      [c=ffff00]{Sesh.TotalFairies}[c=ffffffff]\n");
            sb.Append($"ENEMIES DEFEATED   [c=ffff00]{Sesh.KillCount}[c=ffffffff]\n");
            sb.Append($"FIREBALL RANGE     [c=ffff00]{Sesh.FireballRange}[c=ffffffff]\n");
            sb.Append($"SCROLL SIZE        [c=ffff00]{Sesh.ScrollSize}[c=ffffffff]\n");
            sb.Append($"ITEMS COLLECTED    [c=ffff00]{Sesh.PickupCount}[c=ffffffff]\n");
            sb.Append($"[c=ff888888]_____________________________[c=ffffffff]\n");
            sb.Append($"YOUR G.D.V.        [f=ffff00]{Sesh.CalculateGDV()}[c=ffffffff]\n\n");
            sb.Append($"[c=ff888888]_____________________________[c=ffffffff]\n");
            sb.Append($"INVENTORY ITEMS    [c=ffff00]{Sesh.Inventory.Count}[c=ffffffff]\n\n");

            var g = from i in Sesh.Inventory
                    group i by i.Type into x
                    orderby x.Count() descending 
                    select x;

            int n = 0;
            foreach(var x in g)
            {
                var t = Layout.CellToTile(x.Key);
                var cnt = x.Count() > 1 ? $"[o=1]{x.Count()}[o=0]" : " ";
                sb.Append($"[t={(int)t}][y=+8][x=+8]{cnt}[y=-8] ");
                n++;
                if (n >= 9)
                {
                    n = 0;
                    sb.Append("\n\n");
                }
            }

            Game.SetHelpText(sb.ToString());
        }

        /// <summary>
        /// Resize the level to the specific width/height in cells
        /// </summary>
        public override void Resize(int width, int height)
        {
            base.Resize(width, height);

            // Update the layout
            Layout.Resize(width, height);

        }

        /// <summary>
        /// Renders text on the time over screen
        /// </summary>
        void RenderTimeOverText(SpriteBatch batch)
        {
            batch.DrawStringCentered("TIME OVER", 100, LowLifeColor, true, DefaultCamPosition);
        }

        /// <summary>
        /// Renders text on the game over screen
        /// </summary>
        void RenderGameOverText(SpriteBatch batch)
        {
            batch.DrawStringCentered("GAME OVER", 88, Color.White);
            batch.DrawStringCentered($"YOUR GDV {Game.Sesh.CalculateGDV()}", 88 + 16, Color.White);
        }

        /// <summary>
        /// Melts (burns) all frozen blocks in the level
        /// </summary>
        public bool MeltIce()
        {
            bool r = false;
            for (int i = 0; i < Layout.Width * Layout.Height; i++)
            {
                var c = Layout[i];
                if (Layout.IsFrozen(c))
                {
                    Layout[i] &= ~Cell.Frozen;
                    r = true;

                    AddObject(new Objects.Remains(this, Cell.Empty, 
                        new Point(i % Layout.Width, i / Layout.Width).ToWorld(), true));
                }
            }
            if (r) Sound.Burn.Play();
            return r;
        }

        /// <summary>
        /// Burns (turns to ash) all non-filled non-concrete blocks 
        /// including fake concrete -- also melts ice
        /// </summary>
        public bool BurnBricks()
        {
            // Melt the ice too
            bool r = false; //= MeltIce();
            
            for (int i = 0; i < Layout.Width * Layout.Height; i++)
            {
                var c = Layout[i];
                switch (c)
                {
                    case Cell.Ash:
                        ruin(GameObject.GetReward(Cell.BagW1, Cell.BagW2, Cell.BagW5));
                        break;
                    case Cell.Dirt:
                    case Cell.BlockCracked:
                        ruin(GameObject.GetReward(Cell.BagR1, Cell.BagR2, Cell.BagR5));
                        break;
                    case Cell.FakeConcrete:
                    case Cell.ToggleBlock:
                        ruin(GameObject.GetReward(Cell.BagG1, Cell.BagG2, Cell.BagG5));
                        break;
                    case Cell.Bat:
                        ruin(GameObject.GetReward(Cell.RwBell, Cell.RwOneUp, Cell.RwScroll));
                        break;
                }

                void ruin(Cell reward)
                {
                    Layout[i] = Cell.Empty;
                    r = true;

                    AddObject(new Objects.Remains(this, reward,
                        new Point(i % Layout.Width, i / Layout.Width).ToWorld(), false));
                }
            }
            if (r) Sound.Burn.Play();
            return r;
        }

        /// <summary>
        /// Toggles which item shows collision and X/Y data in debug mode.   Defaults to Dana
        /// </summary>
        public void ToggleDebugItem()
        {
            DebugIndex++;
            if (DebugIndex > Objects.Count - 1) DebugIndex = 0;
            try
            {
                DebugObject = Objects[DebugIndex];
            } catch
            {
                DebugObject = Objects.FirstOrDefault();
            }
        }

        /// <summary>
        /// Causes an explosion of certain types of enemies based on the explosion jar type
        /// </summary>
        public bool Explode(Cell type)
        {
            if (type == Cell.BlueJar) return MeltIce();
            if (type == Cell.YellowJar) return BurnBricks();

            IEnumerable<GameObject> obj = null;
            switch(type)
            {
                case Cell.ExplosionJar:
                    obj = Objects.Where(o => o.EnemyClass == 0 || o is Objects.Droplet);
                    break;
                case Cell.RedJar:
                case Cell.RedFireballJar:
                    obj = Objects.Where(o => o.EnemyClass <= 1 || o is Objects.Droplet);
                    break;
                case Cell.GreenJar:
                    obj = Objects.Where(o => o.EnemyClass <= 2 || o is Objects.Droplet);
                    break;
                case Cell.BlackJar:
                    obj = Objects.Where(o => o.EnemyClass <= 3 || o is Objects.Droplet);
                    break;
            }
            if (obj is null) return false;
            if (obj.Count() == 0) return false;
            foreach(var o in obj)
            {
                if (o.HurtsPlayer) o.Kill(KillType.Fire);     // Kill it with fire
                else if (o is Objects.Droplet drop) drop.Absorb(true);   // Suicide it
            }
            Sound.Burn.Play();
            return true;
        }

        /// <summary>
        /// Resets spawn sequencing
        /// </summary>
        void ResetSpawns()
        {
            Layout.Spawns.ForEach(s => s.Current = 0);
            Layout.Spawns.ForEach(s => s.Disabled = false);
            SpawnPhase = 0;
            SpawnTick = 0;
        }

        /// <summary>
        /// Potentially updates the current character (Sesh.Apprentice) based on
        /// the value of Layout.Character
        /// </summary>
        public void UpdateCharacter()
        {
            if (Layout.Character == CharacterMode.ForceAdam) Sesh.Apprentice = true;
            if (Layout.Character == CharacterMode.ForceDana) Sesh.Apprentice = false;
        }

        /// <summary>
        /// Restarts the level (after a death, etc.)
        /// </summary>
        public void Restart(bool force = false, bool revert_inventory = false)
        {
            // Used by the level editor to allow keys to re-appear, etc.
            if (revert_inventory)
            {
                Sesh.Inventory.RemoveAll(x => x.FromRoom == Sesh.RoomNumber);
                Sesh.SpellsExecuted.RemoveAll(x => x.FromRoom == Sesh.RoomNumber);
                Sesh.DoorsOpened.Clear();
                Sesh.RoomAttempt = 0;
            }

            // Clear level specific stuff
            Game.Swaps.Clear();
            Sesh.ClearTempInventory();
            Ticks = 0;
            RunTicks = 0;
            Flash = false;
            Objects.Clear();
            ObjectsToAdd.Clear();
            ForceHidden = false;
            ResetSpawns();
            QueuedFairies = 0;
            RoutineTimer = 0;
            Playback = null;
            LastDemoIndex = 0;
            FrozenCracked = default;
            FrozenCount = 0;
            FrozenTime = 0;
            if (force)
            {
                Sesh.WarpTo = default;
            }

            // Fixes bugs when going in and out of editor
            Life = Layout.StartLife ?? 10_000;

            if (!force)
            {
                // Is the current player is out of lives?
                if (Game.Sesh.Lives < 1)
                {
                    // Are both players out of lives?
                    if (Game.Sesh.AdamLives < 1 && Game.Sesh.DanaLives < 1)
                    {
                        DoGameOver();
                        return;
                    }

                    // Switch character to the one with lives
                    Game.Sesh.Apprentice = !Game.Sesh.Apprentice;
                }

                Game.Sesh.RoomAttempt++;
                Life = Layout.StartLife ?? 10_000;

            }

            // Reset the layout
            Game.Sesh.ResetLayout(this);
            Layout.CheckForOrphanedKeys();
            
            // Start everything up again
            State = LevelState.Loading;
            Init();

        }

        /// <summary>
        /// Resets the title screen timer (when player presses button to switch characters)
        /// </summary>
        public void ResetTitlescreenTimer()
        {
            if (State == LevelState.TitleScreen)
            {
                RoutineTimer = TitleScreenTicks;
            }
        }

        /// <summary>
        /// Invokes the ending sequence in which Dana opens the book
        /// </summary>
        public void EndingSequence(Point pos)
        {
            Game.Sesh.Progress |= Progress.FoundSolomonsKey;
            VapourModeOff();
            SolomonCell = pos;
            Layout[pos] = Cell.SolBookOpenB;
            Layout[new Point(pos.X - 1, pos.Y)] = Cell.SolBookOpenA;
            RoutineTimer = OpenBookOfSolomonTicks;
            State = LevelState.EndingA;
        }

        /// <summary>
        /// Updates all of the active game objects
        /// </summary>
        protected override void UpdateObjects(GameTime gameTime)
        {
            ObjectMaintenance();

            EnemyCount = 0;

            foreach (var o in Objects)
            {
                if (o.HurtsPlayer && !o.Friendly && o.EnemyClass < 10) EnemyCount++;

                if (o.Type == ObjType.None)
                {
                    ObjectsToRemove.Add(o);
                    continue;
                }

                // If Dana died he still needs to update but nothing else does.
                if ((State == LevelState.Dying || State == LevelState.Dead) 
                    && o != Dana) continue;

                // When doing stars, don't update anything else
                if (State == LevelState.DanaStars || State == LevelState.DoorStars || 
                    State == LevelState.KeyStars || State == LevelState.Warping)
                {
                    if (o.Type != ObjType.Effect) continue;
                }

                if (State == LevelState.TitleScreen && o != Dana) continue;     // Only process Dana on the title screen

                o.Update(gameTime);
            }


        }

        /// <summary>
        /// Clears the audio effect
        /// </summary>
        public void ClearAudioEffect()
        {
            var m1 = (MultiTrack)Music;
            var m2 = (MultiTrack)MusicLow;
            m1.MixPlan = default;
            m2.MixPlan = default;
            m1.ResetMix();
            m2.ResetMix();
        }

        /// <summary>
        /// Applies the audio effect
        /// </summary>
        public void ApplyAudioEffect()
        {
            var m1 = (MultiTrack)Music;
            var m2 = (MultiTrack)MusicLow;

            var mix = new MixPlan();
            var mix2 = new MixPlan();

            switch (Layout.AudioEffect)
            {
                case AudioEffect.Normal:
                    mix2.Track0Fade = () => Life.Massage(2000, 0, 1.0f);
                    mix2.Track1Pan = () => Life.Massage(2000, 0, 1.0f, -1.0f);
                    mix2.Track2Fade = () => Life.Massage(2000, 0, 1.5f, 1.0f, true);
                    break;

                case AudioEffect.SpecialRoomA:

                    // Music
                    mix.Track0Fade = () => Dana.EffectivePosition.ToWorldLerp().X;
                    mix.Track1Fade = () => Dana.EffectivePosition.ToWorldLerp().Y;
                    mix.Track2Fade = () => Sound.Intro.Finished ? 0.75f : 1.0f;
                    // Low time
                    mix2.Track0Fade = () => Life.Massage(2000, 0, 1.5f, 0.0f, true);
                    mix2.Track1Fade = () => Life.Massage(2000, 0, 1.5f, 1.0f, true);
                    mix2.Track1Pan = () => Life.Massage(2000, 0, 1.0f, -1.0f);
                    mix2.Track2Fade = () => Life.Massage(2000, 0, 1.5f, 1.0f, true);
                    break;

                case AudioEffect.SpecialRoomB:

                    // Music
                    mix.Track0Fade = () => Life.Massage(10000, 2000, 1.0f, 0.0f, false);
                    mix.Track1Fade = () => Life.Massage(10000, 2000, 1.0f, 0.0f, true);
                    mix.Track2Fade = () => 0.75f;
                    // Low time
                    mix2.Track0Fade = () => Life.Massage(2000, 0, 1.5f, 0.0f, true);
                    mix2.Track1Fade = () => Life.Massage(2000, 0, 1.5f, 1.0f, true);
                    mix2.Track1Pan = () => Life.Massage(2000, 0, 1.0f, -1.0f);
                    mix2.Track2Fade = () => Life.Massage(2000, 0, 1.5f, 1.0f, true);
                    break;

                case AudioEffect.SpecialRoomC:

                    // Music
                    mix.Track0Fade = () => Life.Massage(10000, 2000, 1.0f, 0.0f, true);
                    mix.Track1Fade = () => Life.Massage(10000, 2000, 1.0f, 0.0f, false);
                    mix.Track2Fade = () => 0.75f;
                    // Low time
                    mix2.Track0Fade = () => Life.Massage(2000, 0, 1.5f, 0.0f, true);
                    mix2.Track1Fade = () => Life.Massage(2000, 0, 1.5f, 1.0f, true);
                    mix2.Track1Pan = () => Life.Massage(2000, 0, 1.0f, -1.0f);
                    mix2.Track2Fade = () => Life.Massage(2000, 0, 1.5f, 1.0f, true);
                    break;

                case AudioEffect.SpecialHidden:

                    // Music
                    mix.Track0Fade = () => Life.Massage(10000, 2000, 1.0f, 0.2f, true);
                    mix.Track1Fade = () => Life.Massage(10000, 2000, 1.0f, 0.2f, false);
                    mix.Track2Fade = () => 0.75f;
                    // Low time
                    mix2.Track0Fade = () => Life.Massage(2000, 0, 1.5f, 0.0f, true);
                    mix2.Track1Fade = () => Life.Massage(2000, 0, 1.5f, 1.0f, true);
                    mix2.Track1Pan = () => Life.Massage(2000, 0, 1.0f, -1.0f);
                    mix2.Track2Fade = () => Life.Massage(2000, 0, 1.5f, 1.0f, true);
                    break;
                case AudioEffect.FullVolume:
                    break;

            }

            m1.MixPlan = mix;
            m2.MixPlan = mix2;

        }


        /// <summary>
        /// Checks to see if the music should change to the low-life warning or vice versa
        /// </summary>
        internal void CheckMusic()
        {
            if (State != LevelState.Running) return;
            if (Life > 2000 || VapourMode)
            {
                if (MusicLow.IsPlaying)
                {
                    MusicLow.Stop();
                    Music.Play();
                } 
            } else
            {
                if (!MusicLow.IsPlaying)
                {
                    Music.Stop();
                    MusicLow.Play();
                }
            }
        }

        /// <summary>
        /// Draws the appropriate game objects to the screen based on the current Level.State
        /// </summary>
        protected override void RenderObjects(SpriteBatch batch)
        {
            if (State != LevelState.Edit)
            {
                foreach (var o in Objects)
                {
                    if (o.Type == ObjType.Dana) continue;
                    if (o.Type != ObjType.Effect && !RenderEnemies) continue;
                    o.Render(batch);
                }

                // Dana always goes in front of everything else
                if (Dana != null) Dana.Render(batch);

                if (Game.ShowCollision)
                {
                    batch.DrawRectangle(new RectangleF(new Point2((float)DebugObject.X, (float)DebugObject.Y),
                    new Size2(16, 16)),
                        Color.White, 1.0f);
                }
            }
        }

        /// <summary>
        /// Invokes a Time Over state
        /// </summary>
        public void TimeOver()
        {
            if (Dana is null) return;

            Dana.Die();
            RoutineTimer = TimeOverTicks;
            State = LevelState.TimeOver;
        }

    }

    /// <summary>
    /// Valid values for Level.State
    /// </summary>
    public enum LevelState
    {
        Loading,
        TitleScreen,
        LightsOut,
        KeyStars,
        DanaStars,
        Running,
        DoorStars,
        ThankYouDana,
        OpeningDoor,
        Dying,
        Dead,
        TimeOver,
        TimeOver2,
        GameOver,
        Edit,
        PreRun,
        Warping,
        EndingA,
        EndingB,
        EndingC
    }

    /// <summary>
    /// Valid audio effect options
    /// </summary>
    public enum AudioEffect
    {
        Normal,
        SpecialRoomA,
        SpecialRoomB,
        SpecialRoomC,
        SpecialHidden,
        FullVolume,
        LastEntry = FullVolume
    }

}
