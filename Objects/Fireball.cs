using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Text;

namespace SKX.Objects
{
    /// <summary>
    /// Dana's fireballs
    /// </summary>
    public class Fireball : Wallhugger
    {
        public int Life;            // How long it has remaining
        public bool IsSuper;        // Good for multiple enemies?
        public bool IsSnowball;     // Cold vs hot

        /* Behavioral parameters */
        private static double FireballSpeed = 2.0;
        
        public Fireball(World world, Point position, Heading direction, bool onGround, Cell type) 
            : base(world, ObjType.Fireball)
        {
            HitBox = new Rectangle(4, 4, 8, 8);
            Position = position;                    // Fireball starts at Dana's position
            Direction = direction;                  // Initial direction is  Dana's direction
            ColTest = Layout.IsSolidNotMesh;
            if (onGround)
            {                                       // If it's cast while crouching ...
                Y = position.Y + HitBox.Y;          // Put it down on the ground
                HugDirection = Heading.Down;        // Hugging the ground,  and Dana's orientation 
                                                    // determines if it's gonna hug CW or CCW.
                if (direction == Heading.Left) Flags |= ObjFlags.Clockwise;    
            }

            switch(type)
            {
                case Cell.SuperFireballJar:  IsSuper = true; break;
                case Cell.SnowballJar:       IsSnowball = true; break;
                case Cell.SuperSnowballJar:  IsSnowball = IsSuper = true; break;
            }                  

            // How many rad crystals did we stock up on?
            Life = Math.Max(Game.Sesh.FireballRange, Game.MinFireballRange) * 16;
            XAnimation = IsSnowball ? Animation.Snowball : Animation.Fireball;      // Animation for left/right
            YAnimation = IsSnowball ? Animation.SnowballY : Animation.FireballY;    // Animation for up/down
            Animation = XAnimation;             
            BlocksMagic = true;
            Speed = FireballSpeed;
        }

 

        public override void Update(GameTime gameTime)
        {

            // Used when a normal fireball hits its first target
            if (Timer == 1) { Remove(); return; }

            // Wall hugging and movement stuff
            base.Update(gameTime);

            // Snowballs reveal secrets
            if (IsSnowball)
            {
                var c = Level.Layout[CellPosition];
                if (Layout.IsHidden(c))
                {
                    Level.Layout[CellPosition] = c.GetContents();       // Unhide item
                    Level.Layout.DrawSparkle(CellPosition, Animation.ShortSparkle, false, false);   // Effect
                    Sound.Reveal.Play();
                }
            }


            // Countdown fireball's life and play decay animation when done.
            if (Life > 0)
            {
                Life--;
                collideEnemies();
            } else
            {
                // Stop the WallHugger class from updating animation and moving
                Hugging = false;        

                // Show the decay animation which also triggers removal when done
                Animation = IsSnowball ? Animation.SnowballDecay : Animation.FireballDecay;    
            }
        }

        public override void OnAnimationEnd()
        {
            Remove();               // Kill the fireball when the decay animation ends
            base.OnAnimationEnd();
        }

        // Check collision with enemies or other flammable things
        private void collideEnemies()
        {
            foreach(var o in CollideObjects())
            {
                if (o is Fireball) continue;    
                if (o is Droplet d)
                {
                    d.Remove();
                    continue;
                }
                if (o.EnemyClass <= 10)  
                {
                    o.Kill(IsSnowball ? KillType.Freeze : KillType.Fire);
                    Sound.Burn.Play();
                    if (!IsSuper)
                    {
                        Timer = 4;
                        Animation = Animation.FireballDecay;
                    }
                }
            }
        }

        public override void HugBlockUpdate(Point cell)
        {
            // Melt ice
            if (!IsSnowball && Layout.IsFrozen(Level.Layout[cell]))
            {
                Level.Layout.Melt(cell);
            }
        }

    }
}
