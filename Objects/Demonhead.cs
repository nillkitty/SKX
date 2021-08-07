using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Text;

namespace SKX.Objects
{
    /// <summary>
    /// The ubiquitous demonhead / demonshead and his alternate graphics
    /// sister, "white demonhead".
    /// </summary>
    public class Demonhead : GameObject
    {
        /* Behavioral parameters */
        private static readonly double SlowSpeed = 0.3;
        private static readonly double NormalSpeed = 0.6;
        private static readonly double FastSpeed = 1.0;
        private static readonly double FasterSpeed = 1.5;

        private static readonly uint ReboundTicksNormal = 8;
        private static readonly uint ReboundTicksFast = 4;

        private double Speed;
        private uint ReboundTicks;
        private bool AltGraphics;
        protected override Cell[] RewardOptions { get; }
               = new[] { Cell.BagW5, Cell.BagR1, Cell.BagR2, Cell.RwCrystal };
        public override int EnemyClass => 0;

        public Demonhead(Level level) : base(level, ObjType.Demonhead)
        {
            HurtsPlayer = true;
            HitBox = new Rectangle(0, 0, 16, 16);
            HurtBox = new Rectangle(3, 0, 10, 16);
        }

        public override void Init()
        {
            // Set up our behaviors
            AltGraphics = Flags.HasFlag(ObjFlags.AltGraphics);
            Speed = SelectSpeed(SlowSpeed, NormalSpeed, FastSpeed, FasterSpeed);
            ReboundTicks = SelectSpeed(ReboundTicksNormal, ReboundTicksNormal, ReboundTicksFast, ReboundTicksFast);
            AnimSpeed = (uint)SelectSpeed(1, 1, 2, 2);

            if (Direction == default) Direction = Heading.Right;
            

            base.Init();
        }

        public override bool Wrapped(Heading overflowDirection)
        {
            if (overflowDirection == Heading.Down) { Remove(); return true; }

            return false;
        }

        public override void Update(GameTime gameTime)
        {
            if (TTL == 0)
            {
                // Remove if it's been alive too long per the spawn point's TTL
                Kill(KillType.Timeout);
                return;
            }

            switch (Routine)
            {
                case 0:
                    // Falling
                    GravityApplies = true;
                    Vx = 0;
                    Animation = AltGraphics ? Animation.Demonhead2Still : Animation.DemonheadStill;
                    Collision = CollideLevel(Heading.Down, ColTypes.Solid, 2, 0);
                    if (Collision.Solid)
                    {
                        // hit a ground
                        Vy = 0;
                        PushOut(Collision.Overlap);

                        // start moving
                        Routine = 1;
                    }
                    break;

                case 1:
                    // Normal
                    GravityApplies = false;
                    Vy = 0;
                    Animation = AltGraphics ? Animation.Demonhead2 : Animation.Demonhead;
                    Vx = Direction == Heading.Left ? -Speed : Speed;

                    Collision2 = CollideLevel(Direction, ColTypes.Solid | ColTypes.Breakable, -2, 0, true);
                    if (Collision2.Solid)
                    {
                        PushOut(Collision2.Overlap);

                        Timer = ReboundTicks;
                        TurnAround();
                        Routine = 2;
                    }

                    Collision = CollideLevel(Heading.Down, ColTypes.Solid, 2, 0);
                    if (!Collision.Solid)
                    {
                        // No ground!
                        PushOut(Collision.Overlap);
                        Routine = 0;
                    }

                    break;

                case 2:
                    // Rebound
                    Vx = 0;
                    if (Timer == 0)
                    {
                        if (Collision2.BreakableBlock.HasValue)
                        {
                            // Break block
                            Level.Layout.BreakBlock(Collision2.BreakableBlock.Value, Animation.BlockBreak);
                        }
                        Routine = 1;
                    }
                    break;

                case 3:
                    break;
            }

            Move();

            base.Update(gameTime);
        }

    }
}
