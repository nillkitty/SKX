using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Text;

namespace SKX.Objects
{
    public class EnemyFireball : GameObject
    {
        /* Behavioral parameters */
        private static double Speed = 2.0;
        private static uint DelayFrames = 10;
        private static int DefaultLife = 200;

        public override int EnemyClass => 10;       // Killable with fireballs

        public EnemyFireball(Level level) : base(level, ObjType.EnemyFireball)
        {
            NoWrap = true;
            HurtsPlayer = true;
            BlocksMagic = true;
            HurtBox = new Rectangle(3, 3, 10, 10);
            HitBox = HurtBox;
            Timer = DelayFrames;
            NoReward = true;
        }

        public override void Init()
        {
            switch (Direction)
            {
                case Heading.Right:
                    Vx = Speed;
                    Animation = Animation.EnemyFire;
                    break;
                case Heading.Left:
                    Vx = -Speed;
                    Animation = Animation.EnemyFire;
                    FlipX = true;
                    break;
                case Heading.Up:
                    Vy = -Speed;
                    Animation = Animation.EnemyFireY;
                    break;
                case Heading.Down:
                    Vy = Speed;
                    Animation = Animation.EnemyFireY;
                    FlipY = true;
                    break;
            }
            TTL = DefaultLife;
            base.Init();
        }

        public override bool Wrapped(Heading overflowDirection)
        {
            Remove(); 
            return true; 

        }


        public override void Update(GameTime gameTime)
        {
            if (TTL == 0)
            {
                Kill(KillType.Decay);
                return;
            }
            TTL--;

            /* Delay for a bit and then once our timer hits zero, we start moving */
            if (Timer == 0)
            {
                Move();

                Collision = CollideLevel(Direction, ColTypes.Solid | ColTypes.Breakable, 1, 1);
                if (Collision.Solid)
                {
                    if (Collision.Breakable)
                    {
                        Level.Layout.BreakBlock(Collision.Block.Value, Animation.BlockBreak);
                    }
                    if (Layout.IsFrozen(Collision.BlockCell))
                    {
                        // Melt ice
                        Sound.Burn.Play();
                        Level.Layout[Collision.Block.Value] &= ~Cell.Frozen;
                        Level.AddObject(new Objects.Remains(Level, Cell.Empty, Collision.Block.Value.ToWorld(), true));
                    }
                    Remove();
                }
            }

            base.Update(gameTime);
        }


    }
}
