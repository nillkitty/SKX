using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Text;

namespace SKX.Objects
{
    /// <summary>
    /// A fairy!
    /// </summary>
    public class Fairy : GameObject
    {

        public bool IsPrincess;                 // Is this the Princess Lyra?

        /* Behavioral parameters */
        private static float Speed = 0.5f;
        private static uint SpawnTicks = 30;

        /* internals */
        private uint Ticks;
        private uint CreateCounter;
        private bool Collected;
        private Animation FlyingAnimation;
        public override int EnemyClass => 10;       // Can be burned

        public Fairy(Level level) : base(level, ObjType.Fairy)
        {
            HitBox = new Rectangle(2, 2, 12, 12);
            HurtBox = HitBox;
            Vx = 0.8;
            Vy = 0.8;
            NoReward = true;
        }

        public override bool CollidedWithDana()
        {
            // Already collected
            if (Collected) return false;    

            if (IsPrincess)
            {
                // Exit the level
                Game.Sesh.Progress |= Progress.SavedPrincess;
                Level.Dana.EnterDoor(false, null);
            }
            else
            {
                Level.Dana.GiveFairy();
            }

            Collected = true;
            Animation = Animation.ShortSparkle;     // Triggers removal
            return false;
        }

        public override void OnAnimationEnd()
        {
            Remove();
            base.OnAnimationEnd();
        }

        public override void Init()
        {
            IsPrincess = Flags.HasFlag(ObjFlags.DropKey);
            FlyingAnimation = IsPrincess ? Animation.Princess : Animation.Fairy;
            Animation = Animation.SparkleLoop;

            base.Init();
        }

        public override void Update(GameTime gameTime)
        {

            base.Update(gameTime);
            
            // Constrain to level space
            Constrain();

            // Short spawn delay when fairy is first birthed
            if (!IsPrincess && CreateCounter < SpawnTicks)
            {
                CreateCounter++;
                return;
            }

            if (!Collected && Level.Dana != null)
            {
                Animation = FlyingAnimation;
                var f_to_player = Vector2.Normalize(Level.Dana.Position.ToVector2() - Position.ToVector2());
                var dir = new Vector2();

                if (Routine == 0)
                {
                    // Move toward player
                    dir = f_to_player;
                    Vx = dir.X;
                    Vy = dir.Y;
                    FlipX = (Vx < 0);
                }
                else if (Routine < 4)
                {
                    dir = new Vector2((float)Vx, (float)Vy);
                }
                else
                {
                    float angle = Ticks;
                    dir = direction_from_rotation(angle) * 128 + Position.ToVector2();
                    dir = Vector2.Normalize(dir - Position.ToVector2());
                }

                X += Speed * dir.X;
                Y += Speed * dir.Y;

                bool switch_state = false;
                Vector2 ipos = Position.ToVector2();

                // Check level collision
                Collision = CollideLevel(ColSensor.RightCenter | ColSensor.LeftCenter | ColSensor.UpCenter | ColSensor.DownCenter, ColTypes.Solid, 0, 0,
                    Heading.None);

                if (Collision.Solid)
                {
                    // Push out of wall
                    X -= dir.X;
                    Y -= dir.Y;
                    switch_state = true;
                }

                if (switch_state)
                {
                    Routine = Math.Clamp(Routine + 1, 0, 5);
                    if (Routine != 5)
                    {
                        var newVelocity = Vector2.Normalize(f_to_player);
                        Vx = newVelocity.X;
                        Vy = newVelocity.Y;
                    }
                }

                if (Routine == 5) Ticks++;

                if (Ticks > 2)
                {
                    Ticks = 0;
                    Routine = 0;
                }
            }


        }

        private float random_float()
        {
            return (float)Game.Random.NextDouble();
        }

        private Vector2 direction_from_rotation(float theta)
        {
            return new Vector2((float)Math.Round(Math.Cos(theta)),  (float)Math.Round(Math.Sin(theta)));
        }

    }
}
