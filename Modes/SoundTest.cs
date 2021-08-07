using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Text;

namespace SKX.Modes
{
    /// <summary>
    /// The Sound Test
    /// </summary>
    public class SoundTest : World
    {
        public SoundTest() : base(17, 17)
        {
            BackgroundColor = Color.DarkBlue;
            BackgroundTile = Tile.BrickBackground;
        }

        public override void Init()
        {
            Sound.StopAll();            // Stop all sounds
            Game.CameraPos = default;   // Reset the camera to where we're drawing things
            Game.UpdateTitle();
            base.Init();                
        }

        public override void Update(GameTime gameTime)
        {
            Game.DrawHUD = false;       // No HUD needed

            if (Control.Esc.Pressed(false) || Control.Enter.Pressed(false))
            {
                Game.Menu = Game.StartMenu;     // Esc = Exit
            }

            var shift = Control.KeyboardState.IsKeyDown(Keys.LeftShift);

            // Key bindings
            if (Control.KeyPressed(Keys.H) && !shift) Sound.HiddenIntro.Play();
            if (Control.KeyPressed(Keys.H) && shift) Sound.HiddenIntro.Stop(true);
            if (Control.KeyPressed(Keys.I) && !shift) Sound.Intro.Play();
            if (Control.KeyPressed(Keys.I) && shift) Sound.Intro.Stop(true);
            if (Control.KeyPressed(Keys.M) && !shift) Sound.Music.Play();
            if (Control.KeyPressed(Keys.M) && shift) Sound.Music.Stop(true);
            if (Control.KeyPressed(Keys.L) && !shift) Sound.LowTime.Play();
            if (Control.KeyPressed(Keys.L) && shift) Sound.LowTime.Stop(true);
            if (Control.KeyPressed(Keys.K) && !shift) Sound.Key.Interrupt();
            if (Control.KeyPressed(Keys.W) && !shift) Sound.Warp.Interrupt();
            if (Control.KeyPressed(Keys.P) && !shift) Sound.Pause.Play();
            if (Control.KeyPressed(Keys.D1) && !shift) Sound.Break.Play();
            if (Control.KeyPressed(Keys.D2) && !shift) Sound.Burn.Play();
            if (Control.KeyPressed(Keys.D3) && !shift) Sound.Collect.Play();
            if (Control.KeyPressed(Keys.D4) && !shift) Sound.Die.Play();
            if (Control.KeyPressed(Keys.D5) && !shift) Sound.Door.Play();
            if (Control.KeyPressed(Keys.D6) && !shift) Sound.ExtraLife.Play();
            if (Control.KeyPressed(Keys.D7) && !shift) Sound.Fairy.Play();
            if (Control.KeyPressed(Keys.D8) && !shift) Sound.Fire.Play();
            if (Control.KeyPressed(Keys.D9) && !shift) Sound.GameOver.Play();
            if (Control.KeyPressed(Keys.D0) && !shift) Sound.Head.Play();
            if (Control.KeyPressed(Keys.D1) && shift) Sound.Hiss.Play();
            if (Control.KeyPressed(Keys.D2) && shift) Sound.Make.Play();
            if (Control.KeyPressed(Keys.D3) && shift) Sound.Rumble.Play();
            if (Control.KeyPressed(Keys.D4) && shift) Sound.Rumble2.Play();
            if (Control.KeyPressed(Keys.D5) && shift) Sound.Shoot.Play();
            if (Control.KeyPressed(Keys.D6) && shift) Sound.ThankYou.Play();
            if (Control.KeyPressed(Keys.D7) && shift) Sound.Wince.Play();
            if (Control.KeyPressed(Keys.D8) && shift) { }
            if (Control.KeyPressed(Keys.D9) && shift) { Game.SwitchMode(new LevelSelect()); }
            if (Control.KeyPressed(Keys.D0) && shift) { Game.SwitchMode(new AnimTest()); }

            if (Control.KeyPressed(Keys.S)) Sound.StopAll();
            if (Control.KeyPressed(Keys.OemTilde))
            {
                Sound.Music.ResetMix();
                Sound.Intro.ResetMix();
                Sound.LowTime.ResetMix();
            }

            base.Update(gameTime);
        }

        protected override void RenderBackground(SpriteBatch batch)
        {
            RenderTileMultiDest(batch, BackgroundTile, 0, 0, TileWidth * TileHeight);
        }

        protected override void RenderForeground(SpriteBatch batch)
        {
            batch.DrawStringCentered("- SOUND TEST -", 16, Color.White);
            var y = 32;
            foreach(var s in Sound.Sounds)
            {
                batch.DrawString(s.ToString(), new Point(0, y), Color.White);
                y += 8;
            }
            

        }
    }
}
