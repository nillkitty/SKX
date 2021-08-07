using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Text;

namespace SKX.Objects
{

    /// <summary>
    /// The "Sparkling Ball" aka Sparky aka Sparkler enemy.
    /// All the heavy lifting is done in the Wallhugger class
    /// </summary>
    public class Sparky : Wallhugger
    {

        /* Behavioral flags */
        public static double NormalSpeed = 0.8;
        public static double FastSpeed = 1.8;
        public static double FasterSpeed = 3.0;
        public static double SlowSpeed = 0.3;
        public override int EnemyClass => 2;



        protected override Cell[] RewardOptions { get; }
                     = new[] { Cell.BagW1, Cell.BagW2 };

        public Sparky(Level level) : base(level, ObjType.Sparky)
        {
            HitBox = new Rectangle(4, 4, 8, 8);
            HurtBox = new Rectangle(3, 3, 10, 10);

            HurtsPlayer = true;
            XAnimation = Animation.Sparky;          // Animation for left/right
            YAnimation = Animation.Sparky;          // Animation for up/down (same in this case)
            Animation = XAnimation;
            BlocksMagic = true;                    
        }

        public override void Init()
        {
            // Set up our properties based on flags
            Speed = SelectSpeed(SlowSpeed, NormalSpeed, FastSpeed, FasterSpeed);
            AnimSpeed = (uint)SelectSpeed(1, 1, 2, 2);
            if(Flags.HasFlag(ObjFlags.AltGraphics))
            {
                XAnimation = Animation.SparkyA;
                YAnimation = Animation.SparkyA;
                Animation = XAnimation;
                ColType = ColTypes.Concrete;
                ColTest = Layout.IsConcrete;
                BlocksMagic = false;
            }
        }

        public override void Update(GameTime gameTime)
        {
            if (Friendly) Speed = SlowSpeed;

            // Do some flips to make the animation more diverse
            if (Level.Ticks % 8 == 0) FlipX = !FlipX;
            if (Level.Ticks % 16 == 0) FlipY = !FlipY;
            base.Update(gameTime);
        }
    }
}
