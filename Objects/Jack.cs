using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Text;

namespace SKX.Objects
{
    /// <summary>
    /// Mighty Bomb Jack!
    /// </summary>
    public class Jack : GameObject
    {
        /* Behavioral parameters */
        private static double JumpAcceleration = 4.5;
        private static double HorizontalBounce = 0.3;
        public override int EnemyClass => 10;       // Can be burned


        public Jack(Level l) : base(l, ObjType.MightyBombJack)
        {
            HitBox = new Rectangle(4, 4, 8, 8);
            HurtBox = HitBox;
        }

        /// <summary>
        /// Called by the base class when we touch Dana
        /// </summary>
        public override bool CollidedWithDana()
        {
            // Remove Jack
            Remove();

            // Transform everything that's not Dana into a fairy
            foreach (var o in Level.Objects)
            {
                if (o is Dana) continue;        // Don't hurt Dana
                if (o is Jack) continue;        // Don't spawn a fairy where Jack was
                Level.AddObject(new Fairy(Level) { Position = o.Position });    // Spawn fairy
                o.Kill(KillType.Transform);     // Counts as a kill
            }
            foreach (var s in Level.Layout.Spawns)
            {
                if (s.Phase0 > 0 || s.Phase1 > 0 || s.DropletRate > 0)
                {
                    if (s.SpawnItems.Count > 0 || s.DropletRate > 0)
                    {
                        if (Level.Layout[s.Position] == Cell.Mirror)
                        {
                            s.Disabled = true;
                            Level.Layout[s.Position] = Cell.Empty;
                            Level.AddObject(new Fairy(Level) { Position = s.Position.ToWorld() });
                        }
                    }
                }
            }

                return true;
        }


        public override void Init()
        {
            GravityApplies = true;      // Physics = yes please
            Vy = -JumpAcceleration;     // Start out in an upward sprawl
            FaceDana();                 // Facing Dana

            base.Init();        
        }

        void FaceDana()
        {
            if (Level.Dana != null && Level.Dana.X > X)
            {
                Direction = Heading.Right;
            }
            else
            {
                Direction = Heading.Left;
            }
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);

            // Constraint position
            Constrain();

            // Use the right animation depending on whether Jack's going up or down
            Animation = Vy > 0 ? Animation.MightyA : Animation.MightyB;

            // Check ceiling collision
            if (Vy < 0)
            {
                Collision = CollideLevel(Heading.Up, ColTypes.Solid);
                if (Collision.Solid)
                {
                    PushOut(Heading.Up, Collision.UpOverlap);
                    Vy = -Vy;
                }
            }
            else
            {
                Collision = CollideLevel(Heading.Down, ColTypes.Solid);

                // Check floor collision when falling
                if (Vy > 0 && Collision.Solid)
                {
                    PushOut(Heading.Down, Collision.Overlap);

                    // If Jack hits a floor,  turn towards Dana, and jump diagonally
                    FaceDana();
                    Vx = Direction == Heading.Right ? HorizontalBounce : -HorizontalBounce;
                    Vy = -JumpAcceleration;
                }
                else
                {
                    // Check wall collision
                    Collision2 = CollideLevel(Direction, ColTypes.Solid);
                    if (Collision2.Solid)
                    {
                        // If Jack hits a wall,  just turn around
                        PushOut(Collision2.Overlap);
                        TurnAround();
                    }
                }
            }

            Move();
        }
    }
}
