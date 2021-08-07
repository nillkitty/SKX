using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Text;

namespace SKX.Objects
{

    /// <summary>
    /// Ghosts, Nuels, and Wyverns (all the same thing)
    /// </summary>
    public class Ghost : GameObject
    {

        /* Behavioral parameters */
        public static double NormalSpeed = 0.85;    // Flight speed
        public static double SlowSpeed = 0.5;
        public static double FastSpeed = 1.2;
        public static double FasterSpeed = 2.5;

        public static uint ReboundTicks = 15;      // Used for slow and normal ghosts/nuels
        public static uint FastReboundTicks = 8;   // Used for fast and faster ghosts/nuels

        protected override Cell[] RewardOptions { get; }
                       = new[] { Cell.BagW1, Cell.BagW2, Cell.BagR1, Cell.BagR2, Cell.RwCrystal };
        public override int EnemyClass => 2;

        /* Internal */
        private double Speed;
        private uint WallWait;
        private bool isNuel;
        private bool isWyvern;
        private Animation FlyAnim;
        private Animation WallAnim;

        public Ghost(Level level) : base(level, ObjType.Ghost)
        {
            HurtsPlayer = true;
            HitBox = new Rectangle(2, 1, 12, 14);
            HurtBox = new Rectangle(4, 2, 8, 12);
        }


        public override void Init()
        {
            // Select speed, delay, animation, etc. based on flags

            Speed = SelectSpeed(SlowSpeed, NormalSpeed, FastSpeed, FasterSpeed);
            WallWait = SelectSpeed(ReboundTicks, ReboundTicks, FastReboundTicks, FastReboundTicks);

            isWyvern = Flags.HasFlag(ObjFlags.AltGraphics);
            isNuel = (Direction == Heading.Up || Direction == Heading.Down);

            FlyAnim = isNuel ? Animation.NuelFly : (isWyvern ? Animation.WyvernFly : Animation.GhostFly);
            WallAnim = isNuel ? Animation.NuelWall : (isWyvern ? Animation.WyvernWall : Animation.GhostWall);

            base.Init();
        }

        // Update velocities and move
        private void move()
        {
            if (Friendly) Speed = SlowSpeed;    // This may get changed during gameplay

            switch (Direction)
            {
                case Heading.Left:
                    Vx = -Speed;
                    Vy = 0;
                    break;
                case Heading.Right:
                    Vx = Speed;
                    Vy = 0;
                    break;
                case Heading.Up:
                    Vx = 0;
                    Vy = -Speed;
                    break;
                default:
                    Vx = 0;
                    Vy = Speed;
                    break;
            }
            Move();
        }

        public override void Update(GameTime gameTime)
        {
            switch (Routine)
            {
                case 0:
                    // Flying

                    Animation = FlyAnim;

                    if (isNuel)
                    {
                        /* Nuels always face the direction Dana is in */
                        FlipX = (Center.X < Level.Dana.Center.X);
                    }
                    else
                    {
                        /* Ghosts and Wyverns always face the direction they're flying */
                        FlipX = (Direction == Heading.Left);
                    }

                    move();

                    // Check flight collision
                    Collision = CollideLevel(Direction);
                    if (Collision.Solid)
                    {
                        // Switch to "against wall" routine
                        Timer = WallWait;
                        Routine = 1;
                    }

                    break;
                
                case 1:
                    // Ghost colliding with wall (before breaking block)

                    Animation = WallAnim;
                    if (Timer <= 0)
                    {
                        // Turn around
                        if (Collision.Breakable)
                        {
                            // Break the block if possible
                            Level.Layout.BreakBlock(Collision.Block.Value, Animation.BlockBreak);
                            Animation = FlyAnim;
                        }
                        Routine = 2;
                        Timer = WallWait;
                    }
                    break;

                case 2:
                    // Ghost colliding with wall (after breaking block)
                    // Animation carried over from routine 1
                    if (Timer <= 0)
                    {
                        TurnAround();
                        Routine = 0;
                    }
                    break;
            }

            base.Update(gameTime);
        }
    }
}
