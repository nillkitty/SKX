using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Text;

namespace SKX.Objects
{
    /// <summary>
    /// A circle of stars ...
    /// </summary>
    public class StarCircle : GameObject
    {

        public readonly int Stars;          // How many stars?
        public double Swirl;                
        public int Size;                    // Current radius
        public int MaxSize;
        public readonly bool Outward;       // Outward (true) or inward (false)?
        public readonly bool Fast;
        private double Speed;               // Calculated from desired ticks and size
        private int step;                   // How far we are into the animation
        private double arc;                 // Angle of each section
        private double sub;                 // Angle of each subsection
        public int State;                   // 2 = Circling, 1 = Finishing, 0 = Finished
        public Point Target;                // Target center point
        public Point StartPos;              // Origin center point
        public int FinishTicks;             // How many ticks after we're done but before we return control to the level

        public static StarCircle OutwardCircle(Level level, Point pos, bool fast)
        {
            return new StarCircle(level, 15, 128, pos, fast, 100, true, 15);
        }
        public static StarCircle InwardCircle(Level level, Point pos, bool fast)
        {
            return new StarCircle(level, 15, 128, pos, fast, 80, false, 20);
        }

        public StarCircle(Level level, int stars, int size, Point center, bool fast, int ticks,
            bool outward, int finish_delay) : base(level, ObjType.Effect)
        {
            Stars = stars;
            Speed = (double)ticks / (double)size;
            MaxSize = size;
            Outward = outward;
            Fast = fast;
            if (!Game.IsClassic) Sparkle = Fast;
            if (!Fast)
            {
                AnimSpeed = 6;
            }
            State = 2;
            FinishTicks = finish_delay;
            if (Outward)
            {
                Position = center;
                StartPos = Position;
                Target = Game.CameraPos + new Point(Game.NativeWidth / 2, Game.NativeHeight / 2);        // Center screen
            }
            else
            {
                Position = Game.CameraPos + new Point(Game.NativeWidth / 2, Game.NativeHeight / 2);      // Center screen
                StartPos = Position;
                Target = center;
            }

            arc = (2 * Math.PI) / stars;
            sub = arc / 5;
            if (outward)
            {
                step = 1;
            } else
            {
                step = (int)(Speed * MaxSize);
            }

        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);

            // step goes from 0 (smallest) to (Speed * MaxSize) (largest) or vice versa
            double max_step = Speed * MaxSize;      

            // Check to see if we're done-done.
            if (step < 1 && Timer == 0)
            {
                State = 0;
                Remove();
                return;
            } else if (step < 0)
            {
                return;
            }

            if (Swirl++ > 5) Swirl = 1;

            float nStep = (float)(step / max_step);
            // Normalize 
            X = MathHelper.Lerp(StartPos.X, Target.X, Outward ? nStep : 1-nStep);
            Y = MathHelper.Lerp(StartPos.Y, Target.Y, Outward ? nStep : 1-nStep);

            if (Outward)
            {
                step++;
                if (step > max_step)
                {
                    Finished();
                }
            } else
            {
                step--;
                if (step == 0) Finished();
            }

            Size = (int)(step / Speed);
        }

        void Finished()
        {
            // Done drawing the circle,  now just show the single sparkle for a short period if it's an inward
            // circle
            step = -1;
            AnimSpeed = 5;
            if (Outward)
            {
                Animation = Animation.Empty;
                Timer = (uint)FinishTicks;
                State = 1;
            } 
            else 
            { 
                Animation = Animation.SparkleLoop;
                Timer = (uint)FinishTicks;
                State = 1;
             }
        }


        public override void Render(SpriteBatch batch)
        {
            base.Render(batch);

            // Check to see if the star circle is done (and we're in single sprite mode)
            if (step < 0) return;

            double tx, ty;
            double angle;

            for (int s = 1; s <= Stars; s++)
            {
                angle = arc * s;

                tx = X + Math.Sin(angle + (Swirl * sub)) * Size;
                ty = Y + Math.Cos(angle + (Swirl * sub)) * Size;

                if (tx < 1 || ty < 1) continue;

                Tile t;
                if (Fast)
                    t = (Size & 1) > 0 ? Tile.Sparkle : Tile.Sparkle2;
                else
                    t = (Size & 8) > 0 ? Tile.Sparkle : Tile.Sparkle2;

                Level.RenderTileWorld(batch, (int)tx, (int)ty, t, Sparkle ? Level.SparkleColor : Color);

            }

        }


    }
}
