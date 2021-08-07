using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Text;

namespace SKX.Objects
{
    /// <summary>
    /// 1-up or 5-up superscript effect
    /// </summary>
    class Superscript : GameObject
    {
        public new Tile Tile { get; set; }

        public Superscript(Level l, Tile t) : base(l, ObjType.Effect)
        {
            Tile = t;
        }

        public override bool Wrapped(Heading overflowDirection)
        {
            if (overflowDirection == Heading.Down) { Remove(); return true; }

            return false;
        }

        public override void Update(GameTime gameTime)
        {
            if (Routine < 8)
            {
                // Going up...
                Vy = -1;
            } else if (Routine < 64)
            {
                // Going down...
                GravityApplies = true;
            } else
            {
                // De-spawn
                Remove();
                return;
            }
            Move();
            Routine++;
            base.Update(gameTime);
        }

        public override void Render(SpriteBatch batch)
        {
            
            Level.RenderTileWorld(batch, (int)X, (int)Y, Tile, Level.SparkleColor);
        }


    }
}
