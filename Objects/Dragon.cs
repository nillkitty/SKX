using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended;
using System;
using System.Collections.Generic;
using System.Text;

namespace SKX.Objects
{
    public class Dragon : FireBreather
    {

        /* Behavioral parameters */
        private static double SlowSpeed = 0.2;
        private static double NormalSpeed = 0.4;
        private static double FastSpeed = 0.8;
        private static double FasterSpeed = 1.2;

        private static uint ReboundTicks = 15;       /* Normal and slow goblins */
        private static uint ReboundTicksFast = 8;    /* Fast goblins */
        private static uint ReboundTicksFaster = 8;  /* Faster goblins */

        private static uint BlockAttackDelay = 30;       // How long dragon flashes before burning block
        private new static uint DanaAttackDelay = 30;       // How long dragon flashes before burning Dana

        /* Internal */
        private double Speed;
        private uint ReboundWaitTicks;
        private bool InitialLand = false;
        public bool AltGraphics = false;

        protected override Cell[] RewardOptions { get; }
                         = new[] { Cell.BagW1, Cell.RwCrystal, Cell.RwScroll, Cell.RwOneUp };

        public override int EnemyClass => 3;

        public Dragon(Level l) : base(l, ObjType.Dragon)
        {
            HitBox = new Rectangle(1, 1, 14, 15);       // Set hit box
            HurtBox = new Rectangle(4, 6, 8, 10);      // Set hurt box
            HurtsPlayer = true;                           // Dragons hurt Dana
            MultiAttack = true;                         // Dragon can attack multiple times
        }

        public override void Init()
        {
            // Select movement speed and rebound time
            Speed = SelectSpeed(SlowSpeed, NormalSpeed, FastSpeed, FasterSpeed);
            ReboundWaitTicks = SelectSpeed(ReboundTicks, ReboundTicks, ReboundTicksFast, ReboundTicksFaster);
            base.DanaAttackDelay = DanaAttackDelay;

            base.Init();
        }


        public override bool Wrapped(Heading overflowDirection)
        {            
            if (overflowDirection == Heading.Down) { 
                Kill(KillType.Drop); 
                return true; 
            }

            return false;
        }


        public override void Update(GameTime gameTime)
        {
            // Don't do anything when the level isn't running
            if (Level.State != LevelState.Running) return;

            switch (Routine)
            {
                case 0:
                    // Think/pause state
                    Animation = Animation.DragonStand;
                    if (Timer < 1)
                    {
                        Routine = 1;
                    }
                    break;
                case 1:
                    // Dragon walking

                    Vy = 0;
                    FlipY = false;
                    Animation = Animation.DragonWalk;

                    move();

                    // Check for wall
                    Collision2 = CollideLevel(Direction, ColTypes.Solid | ColTypes.Breakable, 1, 2);
                    if (Collision2.Solid)
                    {
                        if (Friendly)
                        {
                            // We can't attack so just turn around
                            Timer = ReboundWaitTicks;
                            Routine = 5;
                            return;
                        }
                        if (Collision2.Breakable)
                        {
                            // Hit dirt -- attack
                            Timer = BlockAttackDelay;
                            Routine = 2;
                        }
                        else if (Layout.IsFrozen(Collision2.BlockCell))
                        {
                            // If it's a frozen block,  attack it
                            Timer = BlockAttackDelay;
                            Routine = 2;
                        }
                        else if (Collision.Solid)
                        {
                            // Hit wall -- turn around 
                            Timer = ReboundWaitTicks;
                            Routine = 5;
                        }

                    }

                    // Check for floor
                    Collision = CollideLevel(Heading.Down, ColTypes.Solid, 0, 0);
                    if (Collision.NumSensorsCollided == 0)
                    {
                        // Nothing under him

                        Animation = Animation.DragonFall;
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
                            Animation = Animation.DragonStand;
                            AnimationCounter = 3;
                            Timer = 20;
                            Routine = 5;                  // Turn around

                        }
                    }

                    // Check for Dana
                    if (CheckBreatheOnDana()) break;

                    break;
                case 2:
                    // pre-attack flash
                    if (Friendly) { Routine = 0; return; }

                    Vx = 0;
                    Animation = Animation.DragonAttack;
                    if (Timer == 0)
                    {                    
                        // Break the block if there was one
                        if (Collision2.Block.HasValue) Level.Layout.BreakOrMeltBlock(Collision2.Block.Value);

                        // Burninate
                        Breathe();

                        // Go to attacking
                        Routine = 3;
                    }
                    break;
                case 3:
                    // Attacking
                    if (Friendly) { Routine = 0; return; }

                    Vx = 0;
                    Animation = Animation.DragonStand;
                    if (Tongue == null || Tongue.Type == ObjType.None)
                    {
                        Routine = 1;
                    }
                    break;
                case 4:
                    // Falling

                    Timer = ReboundWaitTicks;
                    Animation = Animation.DragonFall;
                    Vx = 0;
                    Vy = 1.5;
                    Move();
                    FlipY = true;
                    Collision = CollideLevel(Heading.Down, ColTypes.Solid, 0, 0);
                    if (Collision.Solid)
                    {
                        if (!InitialLand)
                        {
                            InitialLand = true;
                            Routine = 1;
                            break;
                        }
                        else
                        {
                            Kill(KillType.Drop);
                        }
                    }
                    break;
                case 5:
                    // At ledge
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

        // Updates velocity
        private void move()
        {
            switch (Direction)
            {
                case Heading.Left:
                    Vx = -Speed;
                    Vy = 0;
                    break;
                default:
                    Vx = Speed;
                    Vy = 0;
                    break;
            }
            Move();
        }

        public override void Render(SpriteBatch batch)
        {
            // Flip X if facing left
            FlipX = (Direction == Heading.Left);

            // Bobbing effect
            if (Tile == Tile.DragonB)
                TweakY = 1;
            else
                TweakY = 0;

            if (Game.ShowHitBoxes)
            {
                batch.DrawRectangle(areaF.ToRectangleF(), Color.Yellow, 1);
                batch.DrawRectangle(areaB.ToRectangleF(), Color.Cyan, 1);
            }

            base.Render(batch);

        }


    }
}
