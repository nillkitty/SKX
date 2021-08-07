using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Text;

namespace SKX.Modes
{

    /// <summary>
    /// Cheap animation test
    /// </summary>
    public class AnimTest : World
    {
        public AnimTest() : base(17, 17)
        {
            BackgroundColor = Color.DarkSlateGray;
            BackgroundTile = Tile.Empty;
        }

        public override void Init()
        {
            Sound.StopAll();
            Game.CameraPos = default;
            Game.UpdateTitle();
            base.Init();

            int x = 0;
            int y = 16;
            foreach(var a in Animation.Animations)
            {
                var o = new Objects.Twinkle(this);
                o.Animation = a;
                o.Position = new Point(x, y);
                o.AnimateForever = true;
                AddObject(o);

                x += 16;
                if (x > 240)
                {
                    x = 0;
                    y += 16;
                }
                
            }

        }

        public override void Update(GameTime gameTime)
        {
            Game.DrawHUD = false;
            ObjectMaintenance();
            UpdateObjects(gameTime);

            Control.UpdateWorld();
            var ctrl = Control.GetState();

            if (ctrl.Down(BindingState.Pause))
                Game.Reset();

            base.Update(gameTime);
        }

        protected override void RenderBackground(SpriteBatch batch)
        {
            RenderTileMultiDest(batch, BackgroundTile, 0, 0, TileWidth * TileHeight);
        }

        protected override void RenderForeground(SpriteBatch batch)
        {
            RenderObjects(batch);
        }
    }
}
