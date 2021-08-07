using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Text;

namespace SKX.Objects
{
    /// <summary>
    /// The fiery remains of enemies, fairies, just about anything,
    /// including ice.  May or may not spawn a reward object upon
    /// animation ending.
    /// </summary>
    class Remains : GameObject
    {
        public Cell Reward;
        private bool IsFrozen;

        public Remains(Level l, Cell reward, Point position, bool cold) : base(l, ObjType.Effect)
        {
            IsFrozen = cold;
            Animation = IsFrozen ? Animation.EnemyFrozen : Animation.EnemyBurned;
            Reward = reward;
            Position = position;
        }

        public override void Update(GameTime gameTime)
        {
            if (Routine < 8)
            {
                // Going up (and slightly to the right)
                Vy = -1;
                Vx = 0.25 * Direction.XUnit();
            }
            else 
            {
                // Going down...
                Animation = IsFrozen ? Animation.EnemyFrozen2 : Animation.EnemyBurned2;
                GravityApplies = true;
            }

            Move();

            if (Routine > 8)
            {
                // Collide with floor
                Collision = CollideLevel(Heading.Down);
            }
            if (Collision.Solid)
            {
                // Push out of the floor
                PushOut(Collision.Overlap);

                Finish();
                return;
            } else
            {
                Routine++;
                if (Routine > 30) Finish();
            }

            base.Update(gameTime);
        }

        void Finish()
        {
            // Do we spawn a reward?
            if (Reward != Cell.Empty)
            {
                // You get a car!
                Level.AddObject(new Reward(Level, Reward, Position));
            }

            Remove();
        }

    }

}
