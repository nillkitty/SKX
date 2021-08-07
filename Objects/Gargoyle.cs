using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Text;

namespace SKX.Objects
{

    /// <summary>
    /// The Gargoyle looks like someone's aunt
    /// </summary>
    public class Gargoyle : GameObject
    {

        /* Behavioral parameters */
        private static double SlowSpeed = 0.2;
        private static double NormalSpeed = 0.4;
        private static double FastSpeed = 0.8;
        private static uint AttackWaitTicksSlow = 45;
        private static uint AttackWaitTicks = 30;
        private static uint AttackWaitTicksFast = 25;
        private static uint ReboundTicksNormal = 60;
        private static uint ReboundTicksFast = 15;
        private static uint BlockAttackDelay = 30;
        private static uint PostAttackWait = 180;
        private static int WalkShootDelay = 32;

        /* Internals */
        private double Speed;
        private uint AttackWait;
        private uint ReboundTicks;
        private bool InitialDrop;
        private bool Attacked;
        private int WalkShoot;
        private int PostShoot;

        public override int EnemyClass => 1;
        protected override Cell[] RewardOptions { get; }
            = new[] { Cell.BagR2, Cell.BagR5, Cell.BagG1, Cell.SpellUpgrade };

        public Gargoyle(Level level) : base(level, ObjType.Gargoyle)
        {
            HurtsPlayer = true;
            HitBox = new Rectangle(2, 1, 13, 14);
            HurtBox = new Rectangle(4, 8, 8, 8);
        }

        public override void Init()
        {
            // Select speed, attack delay, and wall delay based on speed flags
            Speed = SelectSpeed(SlowSpeed, NormalSpeed, FastSpeed, FastSpeed);
            AttackWait = SelectSpeed(AttackWaitTicksSlow, AttackWaitTicks, AttackWaitTicksFast, AttackWaitTicksFast);
            ReboundTicks = SelectSpeed(ReboundTicksNormal, ReboundTicksNormal, ReboundTicksFast, ReboundTicksFast);
        }

        public override void Update(GameTime gameTime)
        {
            if (PostShoot > 0) PostShoot--;     // Decrement post shoot delay counter

            switch (Routine)
            {
                case 0:
                    // Falling
                    GravityApplies = true;
                    Vx = 0;
                    FlipY = true;
                    Animation = Animation.GargFall;
                    Collision = CollideLevel(Heading.Down, ColTypes.Solid, 2, 1);
                    Move();

                    if (Collision.Solid)
                    {
                        // hit a ground
                        Vy = 0;
                        PushOut(Collision.Overlap);

                        if (InitialDrop)
                        {
                            Kill(KillType.Drop);
                            return;
                        }
                        InitialDrop = true;

                        // start moving
                        Routine = 1;
                    }
                    break;
                case 1:
                    // Walking
                    FlipY = false;
                    GravityApplies = false;
                    Vy = 0;
                    Animation = Animation.GargWalk;
                    Vx = Direction == Heading.Left ? -Speed : Speed;
                    FlipX = Direction == Heading.Left;


                    if (WalkShoot > WalkShootDelay)
                    {
                        WalkShoot = 0;
                        CheckFire();
                    }

                    Collision = CollideLevel(Heading.Down, ColTypes.Solid, 2, 1);
                    if (Collision.NumSensorsCollided == 0)
                    {
                        // Nothing under him
                        Routine = 0;
                        break;
                    }
                    else
                    {
                        /* On a floor */
                        if (Collision.OnEdge(Direction) && (X + Vx) % 16 > 0)
                        {
                            // At a ledge
                            Animation = Animation.GargStand;
                            Timer = ReboundTicks;
                            Vx = 0;
                            Attacked = false;
                            

                            if (!CheckDana())
                            {
                                TurnAround();
                                Routine = 4;
                                break;
                            } else
                            {
                                if (CheckFire()) break;
                            }
                        }
                        else
                        {
                            // Check for wall
                            Collision2 = CollideLevel(Direction, ColTypes.Solid | ColTypes.Breakable, 2, 2);
                            if (Collision2.Breakable)
                            {
                                // Attack
                                Timer = BlockAttackDelay;
                                Routine = 2;
                            } else if (Collision2.Solid)
                            {
                                PushOut(Collision2.Overlap);
                                // Turn around
                                Vx = 0;
                                Animation = Animation.GargStand;

                                Attacked = false;
                                if (CheckFire()) break;

                                Timer = ReboundTicks;
                                TurnAround();
                                Routine = 4;
                                break;
                            }
                        }
                    }

                    WalkShoot++;
                    Move();

                    break;
                case 2:
                    // Pre-attack
                    Attacked = true;
                    Animation = Animation.GargAttack;
                    if (Timer == 0)
                    {
                        // Shoot
                        if (Friendly)
                            Routine = 0;
                        else
                        {
                            Timer = AttackWait;
                            Routine = 4;
                            Sound.Shoot.Play();
                            PostShoot = (int)PostAttackWait;


                            var fb = new Objects.EnemyFireball(Level);
                            fb.Position = Position + Direction.RotateOffset(4, 2);
                            fb.Direction = Direction;
                            Level.AddObject(fb);
                        } 

                    }
                    break;
                case 3:
                    // Attack
                    Animation = Animation.GargStand;
                    if (Timer == 0)
                    {
                        Routine = 1;
                    }
                    break;
                case 4:
                    // Rebound
                    Animation = Animation.GargStand;
                    if (Timer == 0)
                    {
                        Routine = 1;
                        break;
                    }
                    break;
            }

            base.Update(gameTime);
        }

        // Check if Gargoyle should fire
        private bool CheckFire()
        {
            if (Friendly) return false;
            if (PostShoot > 0) return false;

            // Look for Dana at the same height or one block lower
            if (CheckDana())
            {
                Timer = AttackWait;
                Routine = 2;
                return true;
            }
            return false;
        }

        // Check if Dana is within range to fire at
        private bool CheckDana()
        {
            var dana = Level.Dana;
            if (dana != null && !Attacked)
            {
                if (dana.Y >= Position.Y - 16 && dana.Y < Position.Y + 32)
                {
                    if ((FlipX && dana.X < Position.X) ||
                            (!FlipX && dana.X > Position.X))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        public override bool Wrapped(Heading overflowDirection)
        {
            if (overflowDirection == Heading.Down)
            {
                Kill(KillType.Drop);
                return true;
            }

            return false;
        }

    }
}
