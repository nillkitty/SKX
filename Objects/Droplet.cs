using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SKX.Objects
{
    public class Droplet : GameObject
    {
        /* Behavioral parameters */
        private static double SlowSpeed = 0.8;
        private static double NormalSpeed = 1.2;
        // private static double FastSpeed = 1.8;
        private static uint ReboundTicksSlow = 3;
        private static uint ReboundTicksNormal = 2;
        private static uint ReboundTicksFast = 1;
        private static uint ErrodeTicks = 20;
        private static uint AbsorbTicks = 4;                    // How long absorb anim plays
        private static double TermVelocityMultiplier = 0.8;     // Reduce TV for frozen drops
        private static double SpeedFuzzFactor = 0.077;          // Factor to fuzz speed by
        private static uint DormantTicks = 120;                 // How long idle droplet sits
        private static uint FrozenTicks = 60;                   // How long until water -> ice
        private static double VerticalHorizontalTransfer = 0.8; // Y->X velocity when hit ground
        private static double SlowFactorNormal = 0.6;           // How much droplets slow by
        private static double SlowFactorIce = 0.96;           // ... on ice
        private static double SlowFactorDirt = 0.99;           // ... on dirt
        private Point ErrodeDirt;
        private int ErrodeTimer;

        public DropletType DropletType { get; private set; }

        private double Speed;
        private uint Dormant;
        private uint ReboundTicks;
        private uint Frozen;
        private Animation AnimNormal, AnimFall, AnimAbsorb;
        public override int EnemyClass => 10;

        public void Absorb(bool flash)
        {
            Sparkle = flash;
            Routine = 3;
            Timer = AbsorbTicks;
        }
        public static double GetDropSpeed(DropletType dt) => dt switch
        {
            DropletType.Blue => NormalSpeed,
            DropletType.Frozen => SlowSpeed,
            DropletType.Slime => NormalSpeed,
            DropletType.Pink => NormalSpeed,
            _ => 1.2
        };
        public static Tile GetDropTile(DropletType dt) => dt switch
        {
            DropletType.Blue => Tile.Droplet,
            DropletType.Frozen => Tile.DropletS,
            DropletType.Slime => Tile.DropletG,
            DropletType.Pink => Tile.DropletP,
            _ => Tile.Droplet
        };
        public static uint GetDropRebound(DropletType dt) => dt switch
        {
            DropletType.Blue => ReboundTicksNormal,
            DropletType.Frozen => ReboundTicksSlow,
            DropletType.Slime => ReboundTicksNormal,
            DropletType.Pink => ReboundTicksNormal,
            _ => ReboundTicksFast
        };
        public static Animation GetDropAnim(DropletType dt) => dt switch
        {
            DropletType.Blue => Animation.Droplet,
            DropletType.Frozen => Animation.DropletS,
            DropletType.Slime => Animation.DropletG,
            DropletType.Pink => Animation.DropletP,
            _ => Animation.Droplet
        };
        public static Animation GetFallAnim(DropletType dt) => dt switch
        {
            DropletType.Blue => Animation.DropletFall,
            DropletType.Frozen => Animation.DropletSFall,
            DropletType.Slime => Animation.DropletGFall,
            DropletType.Pink => Animation.DropletPFall,
            _ => Animation.DropletFall
        };
        public static Animation GetAbsorbAnim(DropletType dt) => dt switch
        {
            DropletType.Blue => Animation.DropletAbsorb,
            DropletType.Frozen => Animation.DropletSAbsorb,
            DropletType.Slime => Animation.DropletGAbsorb,
            DropletType.Pink => Animation.DropletPAbsorb,
            _ => Animation.DropletAbsorb
        };


        public Droplet(Level level, DropletType type) : base(level, ObjType.Droplet)
        {
            Width = 6;
            Height = 8;
            HurtsPlayer = false;
            HitBox = new Rectangle(0, 0, 6, 8);
            HurtBox = HitBox;
            DropletType = type;
            UpdateType();
        }

        protected void UpdateType()
        {
            AnimNormal = GetDropAnim(DropletType);
            AnimFall = GetFallAnim(DropletType);
            AnimAbsorb = GetAbsorbAnim(DropletType);
            if (DropletType == DropletType.Frozen)
            {
                TermVelocity = Level.TermVelocity * TermVelocityMultiplier;
            } else
            {
                TermVelocity = null;
            }
            HurtsPlayer = DropletType == DropletType.Slime;
            Speed = GetDropSpeed(DropletType);
            ReboundTicks = GetDropRebound(DropletType);

        }

        public override void Init()
        {
            // Set up our behaviors
            Subframe = true;
            RandomDirection();
            UpdateType();

            // Fuzz the speed so all the droplets don't
            // wind up in the same place
            var spz = Game.Random.Next(0, 5) * SpeedFuzzFactor;
            Speed += spz;

            base.Init();
        }

        public override bool Wrapped(Heading overflowDirection)
        {
            if (overflowDirection == Heading.Down) { Remove(); return true; }

            return false;
        }

        void CollideEnemies()
        {
            foreach(var o in Level.Objects)
            {
                switch(o)
                {
                    case Droplet dr:
                        // Droplet-droplet interaction
                        if (dr.DropletType == DropletType.Blue &&
                            DropletType == DropletType.Slime && dr.ProjectedBox.Intersects(ProjectedBox))
                        {
                            dr.DropletType = DropletType.Pink;
                            dr.UpdateType();
                            DropletType = DropletType.Pink;
                            UpdateType();
                        }                            
                        break;
                    case Burns b:
                        if (b.Routine != 3 && b.ProjectedBox.Contains(Center))
                        {
                            b.FlameOut();
                        }
                        break;
                    case Dragon d:
                        if (o.ProjectedBox.Contains(Center) && d.Routine == 3)
                        {
                            Absorb(false);
                            return;
                        }
                        break;
                    case Salamander s:
                        if (o.ProjectedBox.Contains(Center) && s.Routine == 4)
                        {
                            Absorb(false);
                            return;
                        }
                        break;
                    case Tongue _:
                    case EnemyFireball _:
                    case Fireball _:
                        if (o.ProjectedBox.Contains(Center))
                        {
                            Absorb(false);
                            return;
                        }
                        break;
                }
            }
        }

        void Errode(Point p)
        {
            if (p != ErrodeDirt)
            {
                ErrodeDirt = p;
                ErrodeTimer = (int)ErrodeTicks;
            }
            if (ErrodeTimer <= 0)
            {
                
                ErrodeDirt = default;
                
                if (Layout.IsCracked(Level.Layout[p]))
                    Level.Layout.BreakBlock(p, Animation.BlockBreak, true);
                else
                    Level.Layout.HeadHit(p);
            }
            else
            {
                ErrodeTimer--;
            }

        }

        void UpdateAnimSpeed()
        {
            if (DropletType == DropletType.Frozen)
            {
                AnimSpeed = 0;
                return;
            }

            AnimSpeed = Speed switch
            {
                double d when d == 0 => 0,
                double d when d < 0.01 => 10,
                double d when d < 0.05 => 5,
                double d when d < 0.25 => 4,
                double d when d < 0.50 => 3,
                double d when d < 1 => 2,
                _ => 1
            };

        }

        public void CollideItems()
        {
            var i = Center.ToCell();

            if (i.X < 0 || i.X > Level.WorldWidth) return;
            if (i.Y < 0 || i.Y > Level.WorldHeight) return;

            var c = Level.Layout[i];

            if (Layout.IsItem(c))
            {
                var cc = c.GetContents();
                if (Layout.IsDoorAtAll(c))
                {
                    var di = Level.Layout.Doors.FirstOrDefault(d => d.Position == i);
                    if (di != null)
                    {
                        // Door info, see if it's a warp
                        if (di.Type == DoorType.Warp)
                        {
                            Position = di.Target.ToWorld() + new Point(8,8);
                            return;
                        }
                    }
                }
                Level.Layout[i] = Level.Dana.GiveItem(c, i, out bool collect, out bool sound);
                if (sound) Sound.Collect.Play();
                if (collect) Level.Layout.DrawSparkle(i, Animation.Collect, true, false);
            }
        }

        public override void Update(GameTime gameTime)
        {
            if (TTL == 0 && Routine < 3)
            {
                // Remove if it's been alive too long per the spawn point's TTL
                Absorb(false);
            }

            // If it's still..
            if (Speed == 0)
            {
                // And it's not frozen droplets...
                if (DropletType != DropletType.Frozen)
                {
                    // Remove if it's been still for a certain amount of time
                    if (Dormant > DormantTicks && Routine != 3)
                    {
                        Absorb(false);
                    }
                    else
                    {
                        Dormant++;
                    }

                    // Check to see if it's frozen
                    if (Frozen > FrozenTicks)
                    {
                        DropletType = DropletType.Frozen;
                        UpdateType();
                    }
                } 
            }

            // Check collision with enemies and other droplets
            if (Routine != 3) CollideEnemies();

            // Do things
            switch (Routine)
            {
                case 0:
                    // Falling

                    GravityApplies = true;
                    Vx = 0;
                    Animation = AnimFall;
                    Collision = CollideLevel(Heading.Down, ColTypes.Solid, 2, 1);
                    if (Collision.Solid && Collision.BlockCell != Cell.Tiles)
                    {
                        // hit a ground
                        Vx = Vy * VerticalHorizontalTransfer;
                        Vy = 0;
                        PushOut(Collision.Overlap);

                        // Randomize direction
                        RandomDirection();
                        Vx = Direction == Heading.Left ? -Math.Abs(Vx) : Math.Abs(Vx);

                        // start moving
                        Routine = 1;
                    }
                    break;

                case 1:
                    // Normal movement

                    GravityApplies = false;
                    Vy = 0;
                    Animation = AnimNormal;
                    Vx = Direction == Heading.Left ? -Speed : Speed;

                    if (DropletType == DropletType.Pink)
                    {
                        CollideItems();
                    }

                    Collision2 = CollideLevel(Direction, ColTypes.Solid | ColTypes.Breakable, 2, 1, true);
                    if (Collision2.Solid && Collision2.BlockCell != Cell.Tiles)
                    {
                        PushOut(Collision2.Overlap);

                        Timer = ReboundTicks;   // Set up the delay
                        TurnAround();           // Change direction
                        Speed *= SlowFactorNormal;           // Slow it down
                        Routine = 2;            // Go into waiting
                    }

                    Collision = CollideLevel(Heading.Down, ColTypes.Solid, 2, 1);
                    if (!Collision.Solid || Collision.BlockCell == Cell.Tiles)
                    {
                        // No ground!
                        PushOut(Collision.Overlap);
                        Routine = 0;
                    }
                    else if (Layout.IsFrozen(Collision.BlockCell))
                    {
                        // We're on ice,  slow down...
                        Speed *= SlowFactorIce;
                        if (Speed < 0.1)
                        {
                            Frozen++;
                            Speed = 0;
                        }
                    }
                    else if (Layout.IsBreakable(Collision.BlockCell))
                    {
                        if (DropletType == DropletType.Pink)
                        {
                            // Start tracking this block
                            Errode(Collision.Block.Value);
                        }
                        else
                        {
                            // We're on dirt, slow down...
                            Speed *= SlowFactorDirt;
                            if (Speed < 0.1) Speed = 0;
                        }
                    }

                    break;

                case 2:
                    // Rebound delay
                    Vx = 0;
                    if (Timer == 0)
                    {
                        Routine = 1;
                    }
                    break;
                
                case 3:
                    // Absorbing/decaying

                    Animation = AnimAbsorb;
                    if (Timer == 0)
                    {
                        Remove();
                    }
                    break;

                default:
                    Routine = 1;
                    break;
            }

            UpdateAnimSpeed();
            Move();

            base.Update(gameTime);
        }

        private void RandomDirection()
        {
            Direction = (Game.Random.Next(0, 10)) < 6 ? Heading.Left
                                          : Heading.Right;

        }
    }
}
