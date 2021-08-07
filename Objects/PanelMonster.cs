using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended;
using System;
using System.Collections.Generic;
using System.Text;

namespace SKX.Objects
{
    /// <summary>
    /// The infamous Panel Monster enemy
    /// </summary>
    public class PanelMonster : FireBreather
    {
        public override int EnemyClass => 1;

        /* Behavioral parameters */
        private static uint NormalShootFreq = 240;          // Shoot frequencies in ticks
        private static uint SlowShootFreq = 480;
        private static uint FastShootFreq = 120;
        private static uint FasterShootFreq = 60;
        private static uint BlockAttackDelay = 30;          // How long panel flashes before burning block
        private new static uint DanaAttackDelay = 30;       // How long panel flashes before burning Dana

        private Animation NormalAnim, ShootAnim;
        private uint Speed;
        private bool Licker;
        private Point? ColTest;
        private Point ColPoint;

        protected override Cell[] RewardOptions { get; } 
            = new[] { Cell.BagR2, Cell.BagR5, Cell.BagG1, Cell.RwCrystal };

        public PanelMonster(Level l) : base(l, ObjType.PanelMonster)
        {
            HurtsPlayer = true;
            BlocksMagic = true;
            HitBox = new Rectangle(0, 0, 16, 16);
            HurtBox = new Rectangle(4, 4, 8, 8);
            base.DanaAttackDelay = DanaAttackDelay;
            MultiAttack = true;
            TongueOffset = new Point(0, 3);
        }

        public override void Init()
        {
            // Figure out what our animations and flip flags are
            // based on our orientation

            switch (Direction)
            {
                case Heading.Right:
                    Animation = Animation.PanelX;
                    NormalAnim = Animation;
                    ShootAnim = Animation.PanelShootX;
                    FlipX = false;
                    break;
                case Heading.Left:
                    Animation = Animation.PanelX;
                    NormalAnim = Animation;
                    ShootAnim = Animation.PanelShootX;
                    FlipX = true;
                    break;
                case Heading.Up:
                    Animation = Animation.PanelY;
                    NormalAnim = Animation;
                    ShootAnim = Animation.PanelShootY;
                    FlipY = true;
                    break;
                case Heading.Down:
                    Animation = Animation.PanelY;
                    NormalAnim = Animation;
                    ShootAnim = Animation.PanelShootY;
                    FlipY = false;
                    break;
            }

            // Panel monsters with the clockwise flag set will breathe fire instead of 
            // shooting fire
            Licker = Flags.HasFlag(ObjFlags.Clockwise);

            // Select speed and prime the timer
            Speed = SelectSpeed(SlowShootFreq, NormalShootFreq, FastShootFreq, FasterShootFreq);

            base.Init();
        }

        public override void Update(GameTime gameTime)
        {
            switch(Routine)
            {
                case 0:
                    if (Licker)
                    {
                        ColPoint = Center + Direction.RotateOffset(16, 0);
                        ColTest = CollideLevelAbsolute(ColPoint, Layout.IsBreakable);
                        if (ColTest.HasValue)
                        {
                            Timer = BlockAttackDelay;
                            Routine = 2;
                            return;
                        }
                        if (CheckBreatheOnDana(10, 16, 2)) return;
                        break;
                    }
                    else if (Timer == 0)
                    {
                        Animation = ShootAnim;
                        Timer = 30;
                        Routine = 1;
                    }
                    break;
                case 1:
                    if (Friendly) Routine = 0;
                    if (Timer == 0)
                    {
                        Animation = NormalAnim;
                        Timer = Speed;
                        Routine = 0;
                        Sound.Shoot.Play();

                        var fb = new Objects.EnemyFireball(Level);
                        fb.Position = Position + Direction.RotateOffset(4, 2);
                        fb.Direction = Direction;
                        Level.AddObject(fb);

                    }
                    break;
                case 2:
                    if (Friendly) Routine = 0;

                    // pre-attack flash
                    Vx = 0;
                    Tongued = true;
                    Animation = ShootAnim;
                    if (Timer == 0)
                    {
                        // Break the block if there was one
                        if (ColTest.HasValue) Level.Layout.BreakBlock(ColTest.Value, Animation.BlockBreak);

                        // Burninate
                        Breathe();

                        // Go to attacking
                        Routine = 3;
                    }
                    break;
                case 3:
                    if (Friendly) Routine = 0;

                    // Attacking
                    Vx = 0;
                    Animation = NormalAnim;
                    if (Tongue == null || Tongue.Type == ObjType.None)
                    {
                        Routine = 0;

                    }
                    break;
            }

            base.Update(gameTime);
        }

        public override void TurnAround()
        {
            // Like that Ace of Base song,  Don't Turn Around
            // (suppresses default action of FireBreather)
        }

        public override void Render(SpriteBatch batch)
        {
            // Debug display collision detection point
            if (Game.ShowCollision && ColPoint.X > 0)
            {
                batch.DrawRectangle(new RectangleF(ColPoint.ToVector2(), new Vector2(1, 1)), Color.Lime, 1);
            }

            base.Render(batch);
        }

    }
}
