using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended;
using System;
using System.Collections.Generic;
using System.Text;

namespace SKX.Objects
{
    /// <summary>
    /// The dreaded Salamander (aka "Saramandor" in the manual)
    /// </summary>
    public class Salamander : FireBreather
    {

        /* Behavioral parameters */
        private static double SlowSpeed = 0.3;
        private static double NormalSpeed = 0.6;
        private static double FastSpeed = 1.0;
        private static double FasterSpeed = 1.5;

        private static uint ReboundTicksNormal = 8;
        private static uint ReboundTicksFast = 4;
        private new static uint DanaAttackDelay = 30;    // How long he flashes before burning Dana
        private static uint BlockAttackDelay = 30;       // How long he flashes before burning blocks

        /* Internals */
        private double Speed;
        private uint ReboundTicks;
        private bool DanaSearchActive;

        protected override Cell[] RewardOptions { get; }
                       = new[] { Cell.BagR5, Cell.BagR1, Cell.BagR2, Cell.SpellUpgrade };
        public override int EnemyClass => 0;

        public Salamander(Level level) : base(level, ObjType.Salamander)
        {
            HurtsPlayer = true;
            HitBox = new Rectangle(2, 1, 13, 14);
            HurtBox = new Rectangle(4, 8, 8, 8);            
            base.DanaAttackDelay = DanaAttackDelay;         // Set the FireBreather.DanaAttackDelay
            base.TongueOffset = new Point(0, 3);            // Line our tongue up with our mouth
        }

        public override void Init()
        {
            // Set up our behaviors
            Speed = SelectSpeed(SlowSpeed, NormalSpeed, FastSpeed, FasterSpeed);
            ReboundTicks = SelectSpeed(ReboundTicksNormal, ReboundTicksNormal, ReboundTicksFast, ReboundTicksFast);
            AnimSpeed = (uint)SelectSpeed(1, 1, 2, 2);

            if (Direction == default) Direction = Heading.Right;
            
            base.Init();
        }

        public override bool Wrapped(Heading overflowDirection)
        {
            // We don't want these falling from the sky
            if (overflowDirection == Heading.Down) { Remove(); return true; }

            return false;
        }


        public override void Update(GameTime gameTime)
        {
            DanaSearchActive = false;   // We'll set this in CheckBreatheOnDana
                                        // if we actually check;  reset every frame

            // Face left if we're facing left
            FlipX = Direction == Heading.Left;

            // Check for expiration
            if (TTL == 0)
            {
                Kill(KillType.Timeout);
                return;
            }

            switch (Routine)
            {
                case 0:
                    // Falling
                    GravityApplies = true;
                    Vx = 0;
                    Animation = Animation.SalamanderStill;
                    Collision = CollideLevel(Heading.Down, ColTypes.Solid, 2, 1);
                    if (Collision.Solid)
                    {
                        // hit a ground
                        Vy = 0;
                        PushOut(Collision.Overlap);

                        // Can attack again
                        Tongued = false;

                        // turn to face Dana
                        Direction = (Level.Dana.X < Center.X) ? Heading.Left : Heading.Right;

                        // start moving
                        Routine = 1;
                    }
                    break;

                case 1:
                    // Normal, walking
                    GravityApplies = false;
                    Vy = 0;
                    Animation = Animation.Salamander;
                    Vx = Direction == Heading.Left ? -Speed : Speed;

                    // Look for blocks to breathe on
                    Collision2 = CollideLevel(Direction, ColTypes.Solid | ColTypes.Breakable, -2, 1, true);
                    if (Collision2.Solid)
                    {
                        PushOut(Collision2.Overlap);
                        if (Collision2.Breakable)
                        {
                            // If it's a breakable block,  attack it
                            Timer = BlockAttackDelay;
                            Routine = 3;
                        } else if (Layout.IsFrozen(Collision2.BlockCell))
                        {
                            // If it's a frozen block,  attack it
                            Timer = BlockAttackDelay;
                            Routine = 3;
                        }
                        else
                        {
                            // If it's a solid brick just turn around
                            TurnAround();
                            Tongued = false; // can attack again
                        }
                        
                    }

                    // Collide with floors
                    Collision = CollideLevel(Heading.Down, ColTypes.Solid, 2, 1);
                    if (!Collision.Solid)
                    {
                        // No ground!
                        PushOut(Collision.Overlap);
                        Routine = 0;

                        // It's important that we don't break or return here
                        // because one of the novelties about the enemies in this
                        // game are that they will pause in mid-air to breathe 
                        // fire on you before they fall and die
                    }

                    // Check for Dana
                    if (!Tongued) {
                        DanaSearchActive = true;
                        if (CheckBreatheOnDana()) break;
                    }
                    
                    break;
                case 2:
                    // Unused
                    Routine = 3;
                    break;

                case 3:
                    // pre-attack flash
                    Vx = 0;
                    Tongued = true;
                    Animation = Animation.SalamanderAttack;
                    if (Timer == 0)
                    {
                        // Break the block if there was one
                        if (Collision2.Block.HasValue) 
                            Level.Layout.BreakOrMeltBlock(Collision2.Block.Value);

                        // Burninate
                        Breathe();

                        // Go to attacking
                        Routine = 4;
                    }
                    break;

                case 4:
                    // Attacking
                    Vx = 0;
                    Animation = Animation.SalamanderStill;
                    if (Tongue == null || Tongue.Type == ObjType.None)
                    {
                        Routine = 1;
                    }
                    break;

                default:
                    break;
            }

            Move();

            base.Update(gameTime);
        }

      

        public override void Render(SpriteBatch batch)
        {
            if (Game.ShowHitBoxes && DanaSearchActive)
            {
                batch.DrawRectangle(areaB.ToRectangleF(), Color.Cyan, 1, 0);
                batch.DrawRectangle(areaF.ToRectangleF(), Color.Yellow, 1, 0);
            }
            base.Render(batch);
        }

    }
}
