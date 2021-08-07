using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Text;

namespace SKX.Modes
{

     /// <summary>
     /// The Title Screen 
     /// </summary>
    public class TitleScreen : World
    {

        const int CopyrightTimeout = 180;
        const int TitleScreenTimeout = 300;

        public int Timer;
        public int Routine;

        private Rectangle CaveRect = new Rectangle(0, 416, 256, 96);
        private Rectangle LogoRect = new Rectangle(112, 352, 144, 64);
        private string PlayTime;

        public TitleScreen() : base(17, 17)
        {
            Routine = 0;
            Timer = CopyrightTimeout;
            var ts = new TimeSpan(0, 0, Game.Options.Duration);
            if (ts.Days > 0)
                PlayTime = $"{ts.Days}D {ts.ToString(@"hh\:mm\:ss")}";
            else 
                PlayTime = ts.ToString("c");

        }

        public override void Init()
        {
            Game.CameraPos = default;       // Reset camera to where we're drawing
            Game.DrawHUD = false;
            Sound.StopAll();                // Stop all sounds
            Game.UpdateTitle();
        }

        public override void Update(GameTime gameTime)
        {
            
            // Check or Enter or Start
            if ((Control.Pause.Pressed(false) || Control.Enter.Pressed(false)))
            {
                if (Routine > 0)
                {
                    // Enter or start on title screen opens menu
                    Game.Menu = Game.StartMenu;
                }
                else if (Ticks > 30)
                {
                    // Enter or start on copyright screen goes to title screen
                    Routine = 1;
                    Timer = TitleScreenTimeout;
                }
            }


            if (Timer == 0)
            {
                switch (Routine)
                {
                    case 0:
                         Routine = 1;
                         Timer = TitleScreenTimeout;
                         break;
                    case 1:
                        PlayDemo();
                        break;
                }
            } else
            {
                Timer--;
            }

            base.Update(gameTime);
        }

        /// <summary>
        /// Plays the next demo in the bundle;  if there are no demos,
        /// or the demo can't play,  restart the title screen.
        /// </summary>
        private void PlayDemo()
        {
            var demos = Game.Assets?.Bundle?.Demos;
            if (demos is null || demos.Count < 1)
            {
                // No demos in bundle
                Routine = 0;
                Timer = CopyrightTimeout;
                return;
            }

            var d = demos[Game.LastDemoPlayed % demos.Count];
            try
            {
                Game.LastDemoPlayed++;
                Game.StartDemo(d, true);
            } catch (Exception ex)
            {
                Game.LogError($"Failed to start demo {d}: {ex}");
                Routine = 0;
                Timer = CopyrightTimeout;
            }
            
        }

        protected override void RenderBackground(SpriteBatch batch)
        {
            switch (Routine)
            {
                case 0:
                    BackgroundColor = Color.Black;
                    return;
                case 1:
                    BackgroundColor = Color.SkyBlue;
                    return;
            }
        }

        protected override void RenderForeground(SpriteBatch batch)
        {
            switch(Routine)
            {
                case 0:
                    batch.DrawStringCentered($"SOLOMON'S KEY X", 48, Color.White);
                    batch.DrawStringCentered($"VER {Game.Version}", 64, Color.White);
                    batch.DrawStringCentered($"BASED ON SOLOMONS KEY™", 88, Color.White);
                    batch.DrawStringCentered($"™ AND c 1987 TECMO,LTD", 88 + 8, Color.White);
                    batch.DrawStringCentered($"REWRITTEN BY THE OPEN SOURCE", 88 + 32, Color.White);
                    batch.DrawStringCentered($"COMMUNITY", 88 + 40, Color.White);
                    return;

                case 1:
                    drawCave(batch);
                    drawLogo(batch);
                    batch.DrawShadowedString($"SCORE    HI GDV  TIME PLAYED", new Point(16, 24), Color.White);
                    batch.DrawShadowedString($"{Game.Sesh.Score,-7} {Game.Options.HiGDV,3}      {PlayTime}", new Point(16, 24 + 8), Color.White);
                    batch.DrawShadowedStringCentered($"c TECMO,LTD 1987 ET AL", 120, Color.White);
                    batch.DrawShadowedStringCentered($"PRESS START/ENTER", 130, Color.White);
                    return;
            }

        }

        void drawCave(SpriteBatch batch)
        {
            var dest = new Rectangle(new Point(0, 144), new Point(256, 96));
            batch.Draw(Game.Assets.Blocks, dest, CaveRect, Color.White);
        }


        void drawLogo(SpriteBatch batch)
        {
            var dest = new Rectangle(new Point(56, 49), new Point(144, 64));
            batch.Draw(Game.Assets.Blocks, dest, LogoRect, Color.White);
        }
    }
}
