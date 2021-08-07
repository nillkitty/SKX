using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Text;

namespace SKX.Objects
{
    /// <summary>
    /// Bonus rewards you get after you kill an enemy with fire or an explosion jar
    /// </summary>
    class Reward : GameObject
    {
        public Cell Item;
        public bool Permanent;

        /* Behavioral parameters */
        private static uint TimeToLive = 60 * 8;        // 8 seconds until it disappears

        public Reward(Level l, Cell reward, Point position, bool permanent = false) : base(l, ObjType.Effect)
        {
            Item = reward;
            Animation = new Animation(1, Layout.CellToTile(Item));
            Position = position;
            Timer = TimeToLive;
            Permanent = permanent;
            HitBox = new Rectangle(1, 1, 14, 13);
            HurtBox = HitBox;
            GravityApplies = true;
        }

        public override void Update(GameTime gameTime)
        {
            // If the timer's run out and we're not a key,  then delete ourselves
            if (!Permanent && Timer == 0 && Item != Cell.Key) { 
                Remove(); 
                return; 
            }

            // Check for floor
            Collision = CollideLevel(Heading.Down);
            if (!Collision.Solid)
            {
                // Fall
                Move();
            } else
            {
                // Land on floor
                Vy = 0;
            }

            base.Update(gameTime);
        }

        /// <summary>
        /// Called when Dana touches us
        /// </summary>
        public override bool CollidedWithDana()
        {
            // Give Dana the money
            if (Item == Cell.Key)
            {
                Level.Dana.GiveKey(Position, true);
            } else
            {
                Level.Dana.GiveItem(Item, Position.ToCell(), out bool collect, out bool sound);
                if (sound) Sound.Collect.Play();
                if (collect) Level.Layout.DrawSparkleWorld(Position, Animation.Collect, true, false);
            }

            // Remove me
            Remove();

            return base.CollidedWithDana();
        }


    }
}
