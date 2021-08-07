using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace SKX.Modes
{

     /// <summary>
     /// The classic ending sequence
     /// </summary>
    public class ClassicEnding : World
    {
        private int Timer = 0;          // Timer counter
        private EndingState State;      // State
        private bool GoodEnding;
        private bool BestEnding;
        private bool AllowSkip;
        private new Objects.Twinkle Dana;
        private int Blue;
        private bool Music;
        private Rectangle CaveRect = new Rectangle(0, 416, 256, 96);
        private Point CaveShake;
        private bool CaveDestroyed;
        private readonly int TravelTime = 6 * 60;   // Time fairies come out and monsters go in for
        private readonly int WaitTime = 3 * 60;   // Time fairies come out and monsters go in for
        private readonly int ShakeTime = 3 * 60;   // Time fairies come out and monsters go in for

        public ClassicEnding() : base(17, 17)
        {
        }

        public override void Init()
        {
            BackgroundColor = Color.Black;
            Game.CameraPos = default;       // Reset camera to where we're drawing
            Game.DrawHUD = false;           // no HUD
            Sound.StopAll();                // Stop all sounds
            Game.UpdateTitle();

            GoodEnding = Game.Sesh.HasItem(Cell.PageSpace) && Game.Sesh.HasItem(Cell.PageTime);
            BestEnding = GoodEnding && Game.Sesh.Progress.HasFlag(Progress.SavedPrincess);
            AllowSkip = Game.DebugMode;

        }

        void Controls()
        {
            Control.Update();
            Control.UpdateWorld();

            // Debug -- Restart demo if DOWN is pressed
            if (Control.Crouch.Down() && Game.DebugMode)
            {
                Game.SwitchMode(new Modes.ClassicEnding());
                return;
            }

            if (Control.KeyPressed(Keys.F))
            {
                BestEnding = true;
                GoodEnding = true;
                Game.StatusMessage("BEST ENDING");
            } else if (Control.KeyPressed(Keys.G))
            {
                BestEnding = false;
                GoodEnding = true;
                Game.StatusMessage("GOOD ENDING");
            }

            if (AllowSkip)
            {
                var cs = Control.GetState();
                if ((cs.Current & (BindingState.Fireball | BindingState.Magic | BindingState.Pause)) > 0)
                {
                    // Save game and go to game over screen lol
                    EndEnding();
                    return;
                }
            }



        }

        // The NES game goes to the "Game Over" screen when the game is over (heh)
        // re-hydrate the last played level and go right to a game over that
        // doesn't allow the continue code.  
        //
        // Game Over is not its own mode (World) because it needs to show the level
        // border from the current level.
        void EndEnding()
        {
            Game.Sesh.Save();
            Sound.StopAll();
            var l = Game.Sesh.BuildLevel();
            l.DoFinalGameOver();
            Game.SwitchMode(l);
        }

        public override void Update(GameTime gameTime)
        {
            Controls();                      // Input
            ObjectMaintenance();             // Object adds/removes
            UpdateObjects(gameTime);         // Object updates

            if (Timer > 0) Timer--;         // Decrement timer

            switch (State)
            {
                case EndingState.Init:

                    // Load Dana
                    Dana = new Objects.Twinkle(this);
                    Dana.Animation = Animation.DanaRun;
                    Dana.Position = new Point(128, 240 - 32);
                    Dana.Vx = 0.5;
                    AddObject(Dana);

                    State = EndingState.RunOut;
                    break;

                case EndingState.RunOut:

                    // Run to right
                    // Progress when: Dana goes off screen

                    Dana.Move();
                    if (Dana.X >= WorldWidth)
                    {
                        Dana.Remove();
                        if (BestEnding)
                        {
                            Sound.Fairy.Play();
                            Timer = TravelTime;
                            State = EndingState.FairiesOut;
                        } else if (GoodEnding)
                        {
                            Sound.Rumble.Play();
                            Timer = TravelTime;
                            State = EndingState.MonstersIn;
                        } else
                        {
                            State = EndingState.Text;
                        }
                    }

                    break;

                case EndingState.FairiesOut:

                    // Fairies come out here
                    // Progress when:  timer runs out

                    if (Timer == 0)
                    {
                        Sound.Fairy.Stop();
                        if (GoodEnding)
                        {
                            Sound.Rumble.Play();
                            Timer = TravelTime;
                            State = EndingState.MonstersIn;
                        }
                        else
                        {
                            State = EndingState.Text;
                        }
                    }
                    break;

                case EndingState.MonstersIn:

                    // Monsters go in
                    // Progress when:  timer runs out

                    if (Timer == 0)
                    {
                        Timer = WaitTime;
                        Sound.Rumble.Stop();
                        Sound.Rumble2.Play();
                        State = EndingState.Wait;
                        break;
                    }

                    break;

                case EndingState.Wait:

                    // No visual change
                    // Progress when:  timer runs out

                    if (Timer == 0)
                    {
                        Timer = ShakeTime;
                        State = EndingState.Shake;
                        break;
                    }


                    break;

                case EndingState.Shake:

                    // Foreground shaking
                    // Progress when:  timer runs out

                    if (Timer == 0)
                    {
                        State = EndingState.Fall;
                        break;
                    }

                    break;

                case EndingState.Fall:

                    // Foreground falling slowly
                    // Progress when:  cave has fallen below ground

                    if (Timer == 0)
                    {
                        Sound.Rumble.Stop();
                        State = EndingState.Text;
                        break;
                    }

                    break;

                case EndingState.Text:

                    // Display text routine
                    // When finished, triggers forced GameOver

                    if (Music)
                    {
                        if (!Sound.Ending.IsPlaying)
                        {
                            EndEnding();        // Music is over,  go to Game Over screen
                            return;
                        }
                    } else
                    {
                        Sound.Ending.Play();    // Start music
                        Music = true;
                    }

                    // Fade in the "sky"
                    if (Blue < 240) Blue++; 
                    BackgroundColor = new Color(0, Blue, Blue);

                    break;

            }

            base.Update(gameTime);
        }

        protected override void RenderBackground(SpriteBatch batch)
        {
            // No background
            

        }

        protected override void RenderForeground(SpriteBatch batch)
        {
            if (!CaveDestroyed) drawCave(batch);        // Foreground
            RenderObjects(batch);                       // Dana, enemies, fairies, dust

            switch (State)
            {                
                case EndingState.Text:
                    renderText(batch);                  // Text
                    break;
            }


        }

        void drawCave(SpriteBatch batch)
        {
            var dest = new Rectangle(CaveShake + new Point(0, 144), new Point(256, 96));
            batch.Draw(Game.Assets.Blocks, dest, CaveRect, Color.White);
        }

        void renderText(SpriteBatch batch)
        {
            if (BestEnding)
            {
                batch.DrawShadowedStringCentered("BEST ENDING SEQUENCE TEXT", 88, Color.White);
                batch.DrawShadowedStringCentered("THANKS FOR PLAYING THE BETA", 88 + 16, Color.White);

            }
            else if (GoodEnding)
            {
                batch.DrawShadowedStringCentered("GOOD ENDING SEQUENCE TEXT", 88, Color.White);
                batch.DrawShadowedStringCentered("THANKS FOR PLAYING THE BETA", 88 + 16, Color.White);

            }
            else
            {
                batch.DrawShadowedStringCentered("NORMAL ENDING SEQUENCE TEXT", 88, Color.White);
                batch.DrawShadowedStringCentered("THANKS FOR PLAYING THE BETA", 88 + 16, Color.White);

            }
        }
    }

    public enum EndingState
    {
        Init,
        RunOut,
        FairiesOut,
        MonstersIn,
        Wait,
        Shake,
        Fall,
        Text
    }
}
