using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Text;

namespace SKX.Objects
{
    /// <summary>
    /// The flame enemy thing
    /// </summary>
    public class Burns : GameObject
    {

        /* Behavioral parameters */
        private static Rectangle NormalHurtBox = new Rectangle(4, 2, 8, 14);
        private static Rectangle SmallHurtBox = new Rectangle(4, 10, 8, 6);
        private static uint RedBurnsDeathTicks = 60;
        private static uint MagicTimeout = 200;

        /* Internals */
        private bool InitialLand = false;

        private Animation AnimNormal => Type switch { BurnsType.Red => Animation.BurnsRed,
                                                      BurnsType.Green => Animation.BurnsGreen,
                                                      BurnsType.DarkBlue => Animation.BurnsDarkBlue,
                                                      _ => Animation.BurnsBlue };
        private Animation AnimSmall => Type switch {  BurnsType.Red => Animation.BurnsRedS,
                                                      BurnsType.Green => Animation.BurnsGreenS,
                                                      BurnsType.DarkBlue => Animation.BurnsRainbowS,
                                                      _ => Animation.BurnsBlueS };

        private Animation AnimDying = Animation.BurnsRedDie;

        public new BurnsType Type = BurnsType.Blue;
        public enum BurnsType { Blue, Red, Green, DarkBlue }

        public Burns(Level level) : base(level, ObjType.Burns)
        {
            HitBox = NormalHurtBox;
            HurtBox = NormalHurtBox;
            TweakY = 0;
            HurtsPlayer = true;
            AcceptsMagic = true;
            BlocksMagic = false;
            Animation = AnimNormal;
        }

        // This is called when Dana tries to cast magic on the object 
        // (since we set AcceptsMagic to true)
        public override void MagicCasted()
        {
            if (Routine != 0) return;   // Don't let Dana make it small while it's falling or burning out

            HurtBox = SmallHurtBox;
            Animation = AnimSmall;
            Timer = MagicTimeout;
            base.MagicCasted();
        }

        public override void Init()
        {

            base.Init();
            Type = Direction switch
            {
                Heading.Left => BurnsType.Red,
                Heading.Right => BurnsType.Blue,
                Heading.Up => BurnsType.Green,
                Heading.Down => BurnsType.DarkBlue,
                _ => BurnsType.Blue
            };
            if (Type == BurnsType.DarkBlue) AdamImmune = true;
            if (Type == BurnsType.Green) DanaImmune = true;
            Animation = AnimNormal;
        }

        public override void Update(GameTime gameTime)
        {

            // Handle flame shrinkage timeout
            if (Timer == 0)
            {
                HurtBox = NormalHurtBox;
                Animation = AnimNormal;
            }

            // Do the collision check (used later)
            Collision = CollideLevel(Heading.Down, ColTypes.Solid, 0, 0, true);

            switch(Routine)
            {
                case 0:
                    // Normal
                    if (!Collision.Solid)
                    {
                        Routine = 1;        // Falling
                        GravityApplies = true;
                    }
                    break;
                case 1:
                    // Falling
                    PushOut(Collision.Overlap);
                    if (Collision.Solid)
                    {
                        // Hit a floor
                        Routine = 0;        // Normal
                        GravityApplies = false;

                        if (Collision.SolidBlock.HasValue)
                        {
                            // Melt ice
                            if (Level.Layout.Melt(Collision.SolidBlock.Value))
                            {
                                break;
                            }
                        }
                        
                        if (!InitialLand)
                        {
                            InitialLand = true;     // The red flame can hit the floor one time without
                                                    // it dying so that it can sit itself on the floor
                                                    // when the level begins (no matter how high it was 
                                                    // placed above the ground)

                            if (Direction == Heading.Left)
                            {
                                // kill if it's red
                                Animation = AnimDying;
                                Timer = RedBurnsDeathTicks;
                                Routine = 3;
                            }

                        }

                    } else
                    {
                        // No floor
                        Move();
                    }
                    break;
                case 3:
                    // Extinguishing
                    if (Timer == 0)
                    {
                        Remove();
                    }
                    break;
            }

            base.Update(gameTime);
        }

        public void FlameOut()
        {
            if (Routine == 3) return;
            Routine = 3;
            Animation = AnimDying;
            Timer = RedBurnsDeathTicks;
        }

        public override bool Wrapped(Heading overflowDirection)
        {
            // It shouldn't fall indefinitely
            if (overflowDirection == Heading.Down) 
            { 
                Remove(); 
                return true; 
            }

            return false;
        }

    }
}
