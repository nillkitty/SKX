using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Text;

namespace SKX.Objects
{
    /// <summary>
    /// Goblins and Wizards
    /// </summary>
    public class Goblin : GameObject
    {

        /* Behavioral parameters */
        public static double NormalSpeed = 0.3;
        public static double SlowSpeed = 0.2;
        public static double FastSpeed = 0.8;
        public static double FasterSpeed = 1.2;

        public static uint ReboundTicksSlow = 35;   /* Slow */
        public static uint ReboundTicks = 20;       /* Normal */
        public static uint ReboundTicksFast = 8;    /* Fast goblins */
        public static uint ReboundTicksFaster = 8;  /* Faster goblins */
        
        public static double AngryMultiplier = 1.25; // How much faster they run when Dana is in sight
        public static uint SeesDanaPauseTicks = 8;   // How many ticks to pause after spotting Dana
        public static uint ReboundWaitTicks = 4;     // How many ticks to pause after turning around 
        public static uint LedgeWaitTicks = 20;      // How many ticks to pause at the edge of a ledge
 
        /* Internal */
        private double Speed;
        private uint WallWait;
        private bool Running;
        private bool AnimationEnded;
        private bool InitialLand = false;
        public bool IsWizard = false;

        private Animation WalkAnim, FallAnim, RunAnim, PunchAnim, ThinkAnim;

        protected override Cell[] RewardOptions { get; } =
            new[] { Cell.BagW5, Cell.BagR1, Cell.BagR2, Cell.SpellUpgrade };

        public override int EnemyClass => 3;

        public Goblin(Level level) : base(level, ObjType.Goblin)
        {
            Direction = Heading.Right;
            HurtsPlayer = true;
            HitBox = new Rectangle(4, 4, 8, 12);
            HurtBox = new Rectangle(4, 6, 8, 10);
        }

        /// <summary>
        /// Remove if it falls off the level
        /// </summary>
        public override bool Wrapped(Heading overflowDirection)
        {
            if (overflowDirection == Heading.Down) { Remove(); return true; }

            return false;
        }

        /// <summary>
        /// Called by base class when our animation ends
        /// </summary>
        public override void OnAnimationEnd()
        {
            AnimationEnded = true;
            base.OnAnimationEnd();
        }

        /// <summary>
        /// Set normal running speed and delay
        /// </summary>
        void NormalMode()
        {
            Running = false;
            Speed = SelectSpeed(SlowSpeed, NormalSpeed, FastSpeed, FasterSpeed);
            WallWait = SelectSpeed(ReboundTicksSlow, ReboundTicks, ReboundTicksFast, ReboundTicksFast);

        }

        /// <summary>
        /// Set angry running speed and delay
        /// </summary>
        void AngryMode()
        {
            NormalMode();
            Speed *= AngryMultiplier;
            Running = true;
        }


        /// <summary>
        /// Check to see if goblin sees Dana
        /// </summary>
        void CheckSeeDana()
        {
            if (Friendly) return;
            if (Running) return;
            if (Level.Dana is null) return;
            if (Level.Dana.CellPosition.Y == CellPosition.Y)
            {
                if (FlipX && Level.Dana.Position.X < Position.X ||
                    !FlipX && Level.Dana.Position.X > Position.X)
                {
                    AngryMode();
                    Timer = SeesDanaPauseTicks;
                    Routine = 0;
                }
            }
           
        }

        public override void Init()
        {
            // Set up animations and flags

            NormalMode();
            IsWizard = Flags.HasFlag(ObjFlags.AltGraphics);
            
            WalkAnim = IsWizard ? Animation.WizardWalk : Animation.GoblinWalk;
            RunAnim = IsWizard ? Animation.WizardRun : Animation.GoblinRun;
            FallAnim = IsWizard ? Animation.WizardFall : Animation.GoblinFall;
            ThinkAnim = IsWizard ? Animation.WizardThink : Animation.GoblinThink;
            PunchAnim = IsWizard ? Animation.WizardPunch : Animation.GoblinPunch;

            base.Init();
        }


        public override void Render(SpriteBatch batch)
        {
            if (Tile == Tile.GoblinA || Tile == Tile.WizardA)
                TweakY = 1;
            else
                TweakY = 0;

            base.Render(batch);
            
        }

        public override void Update(GameTime gameTime)
        {
            // Don't do anything when the level is running
            if (Level.State != LevelState.Running) return;

            // Set animation speed based on running or not
            AnimSpeed = (uint)(Running ? 2 : 1);

            switch (Routine)
            {
                case 0:
                    // Think/pause state
                    Animation = ThinkAnim;
                    if (Timer < 1)
                    {
                        AnimationEnded = false;
                        Routine = 1;
                    }
                    break;

                case 1:
                    // Goblin walking
                    FlipX = (Direction == Heading.Left);
                    FlipY = false;
                    Vx = FlipX ? -Speed : Speed;
                    Vy = 0;
                    Animation = Running ? RunAnim : WalkAnim;
                    Move();

                    if (!Running) CheckSeeDana();

                    // Collide with floor
                    Collision = CollideLevel(Heading.Down, ColTypes.Solid, 0, 0);
                    if (Collision.NumSensorsCollided == 0)
                    {
                        // Nothing under him

                        Animation = ThinkAnim;
                        Routine = 4;
                        break;
                    }
                    else
                    {
                        // On a floor
                        InitialLand = true;
                        if (Collision.OnEdge(Direction) && (X + Vx) % 16 > 0)
                        {
                            // At a ledge
                            Animation = ThinkAnim;
                            AnimationCounter = 3;
                            NormalMode();
                            Timer = LedgeWaitTicks;
                            Routine = 5;

                        }
                        else
                        {
                            // Check for wall
                            Collision2 = CollideLevel(Direction, ColTypes.Solid | ColTypes.Breakable, 2, 2);
                            if (Collision2.Solid)
                            {
                                // Hit a wall
                                NormalMode();
                                Timer = WallWait;
                                Routine = 2;
                            }
                        }
                    }
                    break;

                case 2:
                    // Punching wall

                    Animation = PunchAnim;
                    if (AnimationEnded)
                    {
                        AnimationEnded = false;
                        
                        Routine = 3;
                    }
                    break;

                case 3:
                    // After punching wall

                    Animation = WalkAnim;

                    if (Collision2.Breakable && !Friendly)
                    {
                        // Break the block if possible
                        Level.Layout.BreakBlock(Collision2.Block.Value, Animation.BlockBreak);
                    }
                    else if (Collision.Solid)
                    {
                        TurnAround();
                    }
                    Routine = 1;
                    break;

                case 4:
                    // Falling

                    Timer = ReboundWaitTicks;
                    Animation = FallAnim;
                    Vx = 0;
                    Vy = 1.5;
                    Move();
                    FlipY = true;

                    // Collide floors
                    Collision = CollideLevel(Heading.Down, ColTypes.Solid, 0, 0);
                    if (Collision.Solid)
                    {
                        if (!InitialLand)
                        {
                            // First time goblin is touching floor
                            InitialLand = true;
                            Routine = 1;
                            break;
                        }
                        else
                        {
                            // Died of defenestration
                            Kill(KillType.Drop);
                        }
                    }
                    break;

                case 5:
                    // At a ledge
                    if (Timer == 0)
                    {
                        TurnAround();
                        Routine = 1;
                    }
                    break;

                default:
                    Routine = 0;
                    break;
            }

            base.Update(gameTime);
        }
    }
}
