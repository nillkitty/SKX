using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using MonoGame.Extended.Graphics.Geometry;
using System.Text;
using MonoGame.Extended;
using System.Linq;

namespace SKX
{

    /// <summary>
    /// Abstract class representing any object (Dana, enemies, sparkle effects, etc.) 
    /// that may exist in an active level.  Note that public members are usually fields and not
    /// properties for performance reasons.  Statically positioned GameObjects are created from 
    /// ObjectPlacement's upon loading of a Layout, while others are created during gameplay by
    /// various components.
    /// </summary>
    
    public abstract class GameObject
    {

        public ObjType Type { get; set; }   // Type of object;  
                                            // Setting this to None will cause the Level to remove
                                            // the object
   
        public double X, Y;                 // World Position X / Y
        public int Width = 16, Height = 16; // Width and height
        public double TweakX, TweakY;       // Object-wide sprite tweak offset
        public double AniOffX, AniOffY;     // Animation-specific tweak offset (pulled from Animation)
        public uint AnimSpeed = 1;          // Animation counter multiplier
        public double Vx, Vy;               // Velocity X/Y
        public bool Initialized;            // Did we initialize?
        public bool NoWrap;                 // Don't wrap sprites on the X/Y
        public bool Friendly;               // Don't attack Dana and be docile and mellow
        public Color Color = Color.White;   // Diffuse color
        public bool Animate = true;         // Whether or not to cycle animation
        public bool HurtsPlayer = false;    // Whether or not to check collision with Dana/Adam
        public bool AdamImmune = false;     // Whether Adam is immune from HurtsPlayer damage
        public bool DanaImmune = false;     // Whether Dana is immune from HurtsPlayer damage
        public ColResult Collision;         // Generic collision storage variables
        public ColResult Collision2;        
        public Heading Direction = Heading.Right;   // The direction the object is facing/moving
        public ObjFlags Flags;              // Object instance flags
        public int TTL = -1;                // How many ticks until the object expires.  -1 == No expiration
        private Tile LastTile;              // Last tile displayed
        public bool GravityApplies;         // Whether or not gravity should be calculated in Move() calls
        public bool OnFloor;                // Is the object on the floor?
        public bool BlocksMagic = true;     // Block magic in occupying cell?
        public bool AcceptsMagic = false;   // Allow magic on the object? (e.g. Burns)
        public uint Routine;                // Generic routine counter for use by subclasses (shows in debug)
        public bool FlipX;                  // Draw X Flipped
        public bool FlipY;                  // Draw Y Flipped
        public bool Subframe;               // Sub-frame (8x8 tile)
        public uint Rotation = 0;           // Sprite Rotation (not really used)
        public bool NoReward;               // No reward for killing this with a fireball
        public bool Sparkle;                // Draw all disco-like (Superscript and fast StarCircles)
        public double? TermVelocity;        // Object-specific terminal velocity
        public World World { get; }         // Reference to the current World
        public Level Level { get; }         // Reference to the current level (if World is Level)
        public uint Timer { get; set; }     // Generic Timer variable that's decremented if non-zero
        public virtual int EnemyClass => 50; // Determines what jars make what enemies go boom and
                                             // other various things that enumerate enemies

        /// <summary>
        /// Position as a Point (in world space)
        /// </summary>
        public Point Position
        {
            get {
                var xx = X % World.WorldWidth;
                var yy = Y % World.WorldHeight;
                while (xx < 0) xx += World.WorldWidth;
                while (yy < 0) yy += World.WorldHeight;
                return new Point((int)xx, (int)yy);
                }
            set { X = value.X; Y = value.Y; }
        }

        /// <summary>
        /// Position as point with no wrapping
        /// </summary>
        public Point EffectivePosition
        {
            get
            {
                Point p = LastEffectivePosition;
                if (Position.X > 32 && Position.X < World.WorldWidth - 32) p.X = Position.X;
                if (Position.Y > 32 && Position.Y < World.WorldWidth - 32) p.Y = Position.Y;
                LastEffectivePosition = p;
                return p;
            }
        }

        /// <summary>
        /// Dana's position on the previous frame
        /// </summary>
        public Point LastPosition { get; protected set; }

        /// <summary>
        /// Dana's effective position on the previous frame
        /// </summary>
        public Point LastEffectivePosition { get; protected set; }

        /// <summary>
        /// Center of object (assuming its 16x16) as a Point (world)
        /// </summary>
        public Point Center => new Point((int)(X + (Width / 2)), (int)(Y + (Height / 2)));

        /// <summary>
        /// Center of the object's hitbox as a Point (world)
        /// </summary>
        public Point HitBoxCenter => ProjectedBox.Center;

        /// <summary>
        /// Cell position as a Point
        /// </summary>
        public Point CellPosition => Center.ToCell();

        /// <summary>
        /// Hit box used for collision and projected bounding box
        /// </summary>
        public virtual Rectangle HitBox { get; set; } = new Rectangle(0, 0, 16, 16);

        /// <summary>
        /// Hit box used for enemy hurt
        /// </summary>
        public virtual Rectangle HurtBox { get; set; }

        /// <summary>
        /// The projected hit box as a Rectangle (world)
        /// </summary>
        public Rectangle ProjectedBox => new Rectangle((int)X + HitBox.X, (int)Y + HitBox.Y, HitBox.Width, HitBox.Height);

        /// <summary>
        /// The projected hurt box as a Rectangle (world)
        /// </summary>
        public Rectangle ProjectedHurtBox => new Rectangle((int)X + HurtBox.X, (int)Y + HurtBox.Y, HurtBox.Width, HurtBox.Height);
        /// <summary>
        /// The actual 16x16 rectangle the object is positioned at
        /// </summary>
        public Rectangle ActualBox => new Rectangle((int)X, (int)Y, Width, Height);

        /// <summary>
        /// The current Tile to display based on the current animation and the instance's
        /// animation counter
        /// </summary>
        public Tile Tile
        {
            get
            {
                var t = Animation.GetTile(AnimationCounter, ref FlipX, ref FlipY, ref Subframe);
                if (t == Tile.AnimationEnd)
                {
                    Animate = false;
                    OnAnimationEnd();
                    return LastTile;

                }
                LastTile = t;
                return t;
            }
        }

        /// <summary>
        /// Use to determine which reward options exist for a given Enemy. 
        /// Return null to indicate no reward exists for burning this object.
        /// </summary>
        protected virtual Cell[] RewardOptions => null;


        /// <summary>
        /// Factory method used to create enemies/fairies from ObjectPlacements
        /// </summary>
        public static GameObject Create(ObjType type, 
                                        Level level, 
                                        Heading direction, 
                                        ObjFlags flags, 
                                        Point position)
        {
            GameObject o = null;
            switch (type)
            {
                case ObjType.Ghost: o = new Objects.Ghost(level); break;
                case ObjType.Goblin: o = new Objects.Goblin(level); break;
                case ObjType.Fireball: o = new Objects.Fireball(level, position, direction, false, Cell.SuperFireballJar); break;
                case ObjType.Sparky: o = new Objects.Sparky(level); break;
                case ObjType.Burns: o = new Objects.Burns(level); break;
                case ObjType.Fairy: o = new Objects.Fairy(level); break;
                case ObjType.PanelMonster: o = new Objects.PanelMonster(level); break;
                case ObjType.Dragon: o = new Objects.Dragon(level); break;
                case ObjType.Demonhead: o = new Objects.Demonhead(level); break;
                case ObjType.Salamander: o = new Objects.Salamander(level); break;
                case ObjType.Gargoyle: o = new Objects.Gargoyle(level); break;
                case ObjType.MightyBombJack: o = new Objects.Jack(level); break;
                case ObjType.Droplet: o = new Objects.Droplet(level, DropletType.Blue); break;
                default: return null;
            }

            o.Direction = direction;
            o.Flags = flags;
            o.Position = position;
            return o;
        }


        /// <summary>
        /// Gets or sets the object's current Animation.  Setting an animation
        /// only has impact if it's different from the current Animation.  If it is,
        /// the AnimationCounter is reset, the Animation's X/Y offset is pulled in,
        /// and Animate is reset to true.
        /// </summary>
        public Animation Animation
        {
            get => _anim;
            set
            {
                if (_anim != value || _anim.Frames is null || !Animate)
                {
                    AniOffX = value.Xoffset;
                    AniOffY = value.Yoffset;
                    _anim = value;
                    AnimationCounter = 0;
                    Animate = true;
                }
            }
        }
        private Animation _anim;

        /// <summary>
        /// Object-specific animation counter;  reset when Animation is changed;
        /// increased automatically when Animate is set to true.
        /// </summary>
        protected uint AnimationCounter;

        /// <summary>
        /// Raised when the current animation ends (if it ends)
        /// </summary>
        public virtual void OnAnimationEnd() { }

        /// <summary>
        /// Raised when Dana casts magic on an object that `AcceptsMagic`
        /// </summary>
        public virtual void MagicCasted() { }

        /// <summary>
        /// Raised when an object wraps around to the other side of the world
        /// </summary>
        public virtual bool Wrapped(Heading overflowDirection) { return false; }

        /// <summary>
        /// Raised when the current object collides with Dana.  Return true
        /// to stop processing other collision events (like if you kill Dana).
        /// </summary>
        public virtual bool CollidedWithDana() { return false;  }

        /// <summary>
        /// Raised when object flags (such as speed) are changed post-initialization,
        /// by default, just re-runs Init();
        /// </summary>
        public virtual void FlagsChanged() { Init(); }

        /// <summary>
        /// Moves the object persuant to velocity and gravity (if `GravityApplies`)
        /// </summary>
        /// 
        public void Move()
        {
            X = Math.Round(X + Vx, 2);      // Holy shit it took me hours to figure out that these 
            Y = Math.Round(Y + Vy, 2);      // Round()'s needed to be here.  Objects that are constantly 
                                            // moving but never clipped against a walls (like sparkies)
                                            // would desync in groups based on their initial X/Y position
                                            // due to what I can only assume is cumulative precision error.

                                            // I probably should just be using floats instead of doubles, but
                                            // oh well.

            if (GravityApplies)
            {
                var tv = TermVelocity ?? World.TermVelocity;
                if (!OnFloor)
                {
                    Vy += World.Gravity;
                    if (Vy > tv) Vy = tv;
                } 
                else
                {
                    Vy = 0;
                }
            }
        }

        /// <summary>
        /// Selects a random item from a list of items based on RNG
        /// </summary>
        public static Cell GetReward(params Cell[] rewards)
        {
            if (rewards == null || rewards.Length == 0) return Cell.Empty;

            return rewards[Game.Random.Next(0, rewards.Length)];
        }

        /// <summary>
        /// Tests whether the center of the object is in the current
        /// camera viewport or not
        /// </summary>
        public bool IsOnScreen()
        {
            return Game.CameraRect.Contains(Center);
        }

        /// <summary>
        /// Gets a random, valid reward for killing this game object (enemy)
        /// or Cell.Empty if this item doesn't provide a reward for burning it.
        /// </summary>
        public Cell GetReward()
        {
            if (NoReward) return Cell.Empty;

            if (Flags.HasFlag(ObjFlags.DropFairy))
            {
                // Drop a bell
                return Cell.RwBell;  
            }
            if (Flags.HasFlag(ObjFlags.DropKey))
            {
                // Should we drop a key or a bell?
                if (Level.Layout.FindDoors(false, true, false, false).Count() == 0)
                {
                    return Cell.RwBell;
                }
                return Cell.Key;    
            }

            // Yeah, this could be done via a virtual method call...
            var x = RewardOptions;
            if (x is null || x.Length == 0) return Cell.Empty;

            return GetReward(RewardOptions);

        }

        /// <summary>
        /// Constrains the object to the bounds of the level, minus a short margin
        /// </summary>
        public void Constrain(int xmargin = 16, int ymargin = 16)
        {
            if (X < xmargin) X = xmargin + Game.Random.Next(0,1);
            if (X > Level.WorldWidth - xmargin) X = Level.WorldWidth - xmargin;
            if (Y < ymargin) Y = ymargin + Game.Random.Next(0, 1);
            if (Y > Level.WorldHeight - ymargin) Y = Level.WorldHeight - ymargin;
        }

        /// <summary>
        /// Kills an enemy (or fairy, or jack, etc...)
        /// </summary>
        public void Kill(KillType type)
        {
            GameObject f;
            Game.Sesh.KillCount++;
        
            // Check if we should drop a fairy
            switch (type)
            {
                case KillType.Drop:

                    // Draw cloud animation
                    Level.Layout.DrawSparkleWorld(Position, Animation.DropCloud, true, false);

                    // Do we drop a fairy?
                    if (Flags.HasFlag(ObjFlags.DropFairy))
                    {
                        f = new Objects.Fairy(Level);
                        f.Position = Position;
                        Level.AddObject(f);
                    } else if (Flags.HasFlag(ObjFlags.DropKey))
                    {
                        // Are all the doors open?
                        if (Level.Layout.FindDoors(false, true, false, false).Count() == 0)
                        {
                            // Spawn a fairy instead
                            f = new Objects.Fairy(Level);
                            f.Position = Position;
                            Level.AddObject(f);
                        } else
                        {
                            // Drop a key
                            f = new Objects.Reward(Level, Cell.Key, Position);
                            Level.AddObject(f);
                        }
                    }

                    break;
                case KillType.Timeout:

                    // Draw cloud animation
                    Level.Layout.DrawSparkleWorld(Position, Animation.DropCloud, true, false);
                    break;
                case KillType.Decay:

                    // Draw cloud animation
                    Level.Layout.DrawSparkleWorld(Position, Animation.FireballDecay, true, false);
                    break;
                case KillType.Fire:
                    {
                        var reward = GetReward();
                        Level.AddObject(new Objects.Remains(Level, reward, Position, false));
                        break;
                    }
                case KillType.Freeze:
                    {
                        var reward = GetReward();
                        Level.AddObject(new Objects.Remains(Level, reward, Position, true));
                        break;
                    }
                case KillType.Transform:
                    // Do nothing but silently disappear
                    break;
            }

            // Remove the enemy
            Remove();

        }

        /// <summary>
        /// Selects one of four generic values based on the object's speed flags
        /// </summary>
        protected T SelectSpeed<T>(T slow, T normal, T fast, T faster)
        {
            if (Flags.HasFlag(ObjFlags.Slow)) return slow;
            else if (Flags.HasFlag(ObjFlags.Fast)) return fast;
            else if (Flags.HasFlag(ObjFlags.Faster)) return faster;
            else return normal;
        }

        /// <summary>
        /// Base class constructor
        /// </summary>
        /// <param name="world">The world the object is being brought into (usually a Level)</param>
        public GameObject(World world, ObjType type)
        {
            World = world;
            Type = type;
            Level = world as Level;     // If it's a Level world,  set a reference to this so we're not
                                        // casting it a bunch
        }

        /// <summary>
        /// Object-specific initialization code
        /// </summary>
        public virtual void Init()
        {
            if (Direction == Heading.None) Direction = Heading.Right;   // None is an invalid direction
            Initialized = true;
        }

        /// <summary>
        /// Per-GameObject Render routine
        /// </summary>
        public virtual void Render(SpriteBatch batch)
        {

            if (Level is null && World != null)
            {
                World.RenderTileWorld(batch,
                                 (int)(X + TweakX + (FlipX ? -AniOffX : AniOffX)),
                                 (int)(Y + TweakY + (FlipY ? -AniOffY : AniOffY)),
                                 Tile,
                                 Color);
                return;
            }

            // Only Dana gets rendered during TimeOver2 (NES behavior)
            //
            // This is undoubtedly because they had to hide the rest of the level
            // in order to bank switch to the bank that contains all the text characters
            // as is also done during game over.  The NES bank that contains the level
            // bricks only contains enough letters for the HUD.
            //
            // While it looks cool to leave the level showing (like a normal death),
            // and even cooler to leave the level in grayscale (World.Flash == true),
            // I'm keeping this aspect original to the NES game.  ~Nill
            if (Level.State == LevelState.TimeOver2 && Type != ObjType.Dana) return;

            // Figure out flip X/Y
            SpriteEffects fx = SpriteEffects.None;
            if (FlipX) fx |= SpriteEffects.FlipHorizontally;
            if (FlipY) fx |= SpriteEffects.FlipVertically;

            // Render the object's current Tile
            if (Subframe)
            {
                var frame = (AnimSpeed == 0) ? 0
                                             : (Level.RunTicks / AnimSpeed) % 4;

                // 8x8 animated frame
                World.RenderSmallTileWorld(batch, frame,
                                     (int)(X + TweakX + (FlipX ? -AniOffX : AniOffX)),
                                     (int)(Y + TweakY + (FlipY ? -AniOffY : AniOffY)),
                                     Tile,
                                     Sparkle ? Level.SparkleColor : Color,
                                     fx,
                                     Rotation, !NoWrap && !Level.WorldRectangle.Contains(ActualBox));
            }
            else
            {
                // 16x16 static frame
                World.RenderTileWorld(batch,
                                     (int)(X + TweakX + (FlipX ? -AniOffX : AniOffX)),
                                     (int)(Y + TweakY + (FlipY ? -AniOffY : AniOffY)),
                                     Tile,
                                     Sparkle ? Level.SparkleColor : Color,
                                     fx,
                                     Rotation, !NoWrap && !Level.WorldRectangle.Contains(ActualBox));
            }


            // Render any debug collision the object has
            if (Game.ShowCollision)
            {
                var selected = (this == Level.DebugObject);
                if (Game.ShowHitBoxes)
                {
                    batch.DrawRectangle(ProjectedBox, selected ? Color.Yellow : Color.White, 1, 0);
                }
                if (Game.ShowHurtBoxes)
                {
                    batch.DrawRectangle(ProjectedHurtBox, Color.Red, 1, 0);
                }
                if (Game.ShowRoutines)
                {
                    batch.DrawShadowedString(Routine.ToString("X").Substring(0, 1), Center + new Point(-8, -8), Color.White);
                }
                if (Game.ShowTimers)
                {
                    batch.DrawShadowedString(Timer.ToString("X2").Substring(0, 1), Center + new Point(8, 8), Color.White);
                }
                Collision.Render(batch);
                Collision2.Render(batch);

            }

        }

        /// <summary>
        /// Gets color used in Collision view for the selected object's collision
        /// </summary>
        public Color GetDebugColor(int x, int y, Point dtb)
        {

            var pt = new Point(x, y);

            // if (pt == dtb) return Color.Purple;

            if (Collision.LCc == pt) return Color.Maroon;
            if (Collision.LUc == pt) return Color.Pink;
            if (Collision.LLc == pt) return Color.Red;

            if (Collision.RCc == pt) return Color.DarkBlue;
            if (Collision.RUc == pt) return Color.LightBlue;
            if (Collision.RLc == pt) return Color.Blue;

            if (Collision.DCc == pt) return Color.DarkGreen;
            if (Collision.DLc == pt) return Color.LightGreen;
            if (Collision.DRc == pt) return Color.Green;

            if (Collision.UCc == pt) return Color.Orange;
            if (Collision.ULc == pt) return Color.Gold;
            if (Collision.URc == pt) return Color.Yellow;

            if (Collision.Cc == pt) return Color.Gray;


            return Color.White;
        }

        /// <summary>
        /// Returns a list of game objects this object is colliding with.  You can specify an alternate
        /// hit box (world space), otherwise ProjectedBox (translated HitBox) is used.
        /// </summary>
        /// <param name="HitBox">Alternate hit box;  default = use this.ProjectedBox</param>
        /// <returns></returns>
        public IEnumerable<GameObject> CollideObjects()
        {
            if (HitBox == default) HitBox = ProjectedBox;
            if (HurtBox == default) HurtBox = HitBox;
            return Level.Objects.Where(o => (o.HurtsPlayer ? ProjectedHurtBox : ProjectedBox).Intersects(o.HurtsPlayer ? o.ProjectedHurtBox : o.ProjectedBox));
        }

        /// <summary>
        /// Flips the Direction property opposite of its current heading
        /// </summary>
        public virtual void TurnAround()
        {
            switch(Direction)
            {
                case Heading.Right: Direction = Heading.Left; break;
                case Heading.Left: Direction = Heading.Right; break;
                case Heading.Down: Direction = Heading.Up; break;
                case Heading.Up: Direction = Heading.Down; break;
            }
        }


        /// <summary>
        /// Rotates the Direction property based on object flags
        /// </summary>
        public virtual void Rotate(bool opposite = false)
        {
            var cw = Flags.HasFlag(ObjFlags.Clockwise);
            switch (Direction)
            {
                case Heading.Right: Direction = cw ? Heading.Down : Heading.Up; break;
                case Heading.Left: Direction = cw ? Heading.Up : Heading.Down; break;
                case Heading.Down: Direction = cw ? Heading.Left : Heading.Right; break;
                case Heading.Up: Direction = cw ? Heading.Right : Heading.Left; break;
            }
            if (opposite) TurnAround();
        }

        /// <summary>
        /// Pushes an object out of a colliding wall in the opposite direction from
        /// what its facing
        /// </summary>
        public virtual void PushOut(Heading direction, int offset)
        {
            switch (direction)
            {
                case Heading.Right: X -= offset; break;
                case Heading.Left: X += offset; break;
                case Heading.Down: Y -= offset; break;
                case Heading.Up: Y += offset; break;
            }
        }

        /// <summary>
        /// Pushes an object in the reverse of the direction it's facing
        /// by the specified offset.  Mainly used to adjust post-collision 
        /// overlap.
        /// </summary>
        public void PushOut(int offset)
        {
            PushOut(Direction, offset);
        }

        /// <summary>
        /// Pushes the object out (based on its hit box) of whatever
        /// cell the world point provided describes
        /// </summary>
        /// <param name="p"></param>
        public virtual void PushOutWorld(Point p)
        {
            var block = new Rectangle(p, new Point(16, 16));        // size of a cell block

            /* Going up */
            if (block.Intersects(ProjectedBox) && ProjectedBox.Top < block.Bottom && ProjectedBox.Bottom > block.Bottom) Y += block.Bottom - ProjectedBox.Top;

            /* Going down */
            if (block.Intersects(ProjectedBox) && ProjectedBox.Bottom > block.Top && ProjectedBox.Top < block.Top) Y -= ProjectedBox.Bottom - block.Top;

            /* Going Left */
            if (block.Intersects(ProjectedBox) && ProjectedBox.Left < block.Right && ProjectedBox.Right > block.Right) X += block.Right - ProjectedBox.Left;

            /* Right */
            if (block.Intersects(ProjectedBox) && ProjectedBox.Left < block.Left && ProjectedBox.Right > block.Left) X -= ProjectedBox.Right - block.Left;

        }

        /// <summary>
        /// Collide with the level in a specific direction only
        /// </summary>
        /// <param name="Direction">The direction to check collision</param>
        /// <param name="Types">Types of collision to check</param>
        /// <param name="hmargin">Number of extra pixels in front of the object to check horizontally</param>
        /// <param name="vmargin">Number of extra pixels in front of the object to check vertically</param>
        public ColResult CollideLevel(Heading Direction, ColTypes Types = ColTypes.Solid | ColTypes.Breakable, 
            int hmargin = 2, int vmargin = 0, bool centerOnly = false)
        {
            if (centerOnly)
            {
                switch (Direction)
                {
                    case Heading.Right: return CollideLevel(ColSensor.RightCenter, Types, hmargin, vmargin, Direction);
                    case Heading.Left: return CollideLevel(ColSensor.LeftCenter, Types, hmargin, vmargin, Direction);
                    case Heading.Up: return CollideLevel(ColSensor.UpCenter, Types, hmargin, vmargin, Direction);
                    case Heading.Down: return CollideLevel(ColSensor.DownCenter, Types, hmargin, vmargin, Direction);
                    default: return CollideLevel(ColSensor.RightCenter, Types, hmargin, vmargin, Direction);
                }
            }
            switch (Direction)
            {
                case Heading.Right:  return CollideLevel(ColSensor.Right, Types, hmargin, vmargin, Direction);
                case Heading.Left:  return CollideLevel(ColSensor.Left, Types, hmargin, vmargin, Direction);
                case Heading.Up:    return CollideLevel(ColSensor.Up, Types, hmargin, vmargin, Direction);
                case Heading.Down:  return CollideLevel(ColSensor.Down, Types, hmargin, vmargin, Direction);
            }

            var r = CollideLevel(ColSensor.Center, Types, hmargin, vmargin);
            r.TestDirection = Direction;
            return r;
        }

        /// <summary>
        /// Tests solid collision between a point and the level, and returns the cell
        /// collided if successful
        /// </summary>
        public Point? CollideLevelAbsolute(Point pos, Func<Cell, bool> test = null)
        {
            var c = pos.ToCell();
            if (test is null) test = Layout.IsSolid;
            if (test(Level.Layout[c]))
            {
                return c;
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Collide with the level using specific sensor positions
        /// </summary>
        /// <param name="Sensors">Which sensors to check collision for</param>
        /// <param name="Types">Types of collision to check</param>
        /// <param name="hmargin">Number of extra pixels in front of the object to check horizontally</param>
        /// <param name="vmargin">Number of extra pixels in front of the object to check vertically</param>
        public ColResult CollideLevel(ColSensor Sensors, ColTypes Types = ColTypes.Solid, 
                                        int hmargin = 2, int vmargin = 0, Heading TestDirection = 0)
        {
            ColResult r = new ColResult();
            r.TestDirection = TestDirection;

            Rectangle projected = new Rectangle((int)X + HitBox.X, (int)Y + HitBox.Y, HitBox.Width, HitBox.Height);
            Point C = new Point((projected.X + projected.X + projected.Width) / 2, (projected.Y + projected.Y + projected.Height) / 2);

            void ColTest(ColSensor sensor, Cell cell, Point p)
            {
                if (Types.HasFlag(ColTypes.Solid) && Layout.IsSolid(cell))
                {
                    r.SolidBlock = p;
                    r.Result |= ColTypes.Solid;
                    r.SensorsCollided |= sensor;
                    r.NumSensorsCollided++;
                }
                if (Types.HasFlag(ColTypes.Concrete) && Layout.IsConcrete(cell))
                {
                    r.SolidBlock = p;
                    r.SensorsCollided |= sensor;
                    r.Result |= ColTypes.Concrete;
                    r.NumSensorsCollided++;

                }
                if (Types.HasFlag(ColTypes.Breakable) && Layout.IsBreakable(cell))
                {
                    r.BreakableBlock = p;
                    r.Result |= ColTypes.Breakable;
                    r.SensorsCollided |= sensor;
                    r.NumSensorsCollided++;

                }
                if (Types.HasFlag(ColTypes.Items) && Layout.IsItem(cell))
                {
                    r.Result |= ColTypes.Items;
                    r.SensorsCollided |= sensor;
                    r.NumSensorsCollided++;

                }
                if (Types.HasFlag(ColTypes.HardItems) && Layout.IsDense(cell))
                {
                    r.Result |= ColTypes.HardItems;
                    r.SensorsCollided |= sensor;
                    r.NumSensorsCollided++;

                }
            }

            if (Sensors.HasFlag(ColSensor.DownLeft))
            {
                r.DL = new Point(projected.X + hmargin, projected.Y + projected.Height + vmargin);
                r.DLc = r.DL.ToCell();
                r.NumDownSensors++;

                r.DLp = Level.Layout[r.DLc];
                ColTest(ColSensor.DownLeft, r.DLp, r.DLc);
            }
            if (Sensors.HasFlag(ColSensor.DownCenter))
            {
                r.DC = new Point(C.X, projected.Y + projected.Height + vmargin);
                r.DCc = r.DC.ToCell();
                r.NumDownSensors++;

                r.DCp = Level.Layout[r.DCc];
                ColTest(ColSensor.DownCenter, r.DCp, r.DCc);
            }
            if (Sensors.HasFlag(ColSensor.DownRight))
            {
                r.DR = new Point(projected.X + projected.Width - hmargin, projected.Y + projected.Height + vmargin);
                r.DRc = r.DR.ToCell();
                r.NumDownSensors++;

                r.DRp = Level.Layout[r.DRc];
                ColTest(ColSensor.DownRight, r.DRp, r.DRc);
            }
            if (Sensors.HasFlag(ColSensor.UpLeft))
            {
                r.UL = new Point(projected.X + hmargin, projected.Y - vmargin);
                r.ULc = r.UL.ToCell();

                r.ULp = Level.Layout[r.ULc];
                ColTest(ColSensor.UpLeft, r.ULp, r.ULc);
            }
            if (Sensors.HasFlag(ColSensor.UpCenter))
            {
                r.UC = new Point(C.X, projected.Y - vmargin);
                r.UCc = r.UC.ToCell();

                r.UCp = Level.Layout[r.UCc];
                ColTest(ColSensor.UpCenter, r.UCp, r.UCc);
            }
            if (Sensors.HasFlag(ColSensor.UpRight))
            {
                r.UR = new Point(projected.X + projected.Width - hmargin, projected.Y - vmargin);
                r.URc = r.UR.ToCell();

                r.URp = Level.Layout[r.URc];
                ColTest(ColSensor.UpRight, r.URp, r.URc);
            }
            if (Sensors.HasFlag(ColSensor.LeftUpper))
            {
                r.LU = new Point(projected.X - hmargin, projected.Y + vmargin);
                r.LUc = r.LU.ToCell();

                r.LUp = Level.Layout[r.LUc];
                ColTest(ColSensor.LeftUpper, r.LUp, r.LUc);
            }
            if (Sensors.HasFlag(ColSensor.LeftCenter))
            {
                r.LC = new Point(projected.X - hmargin, C.Y);
                r.LCc = r.LC.ToCell();

                r.LCp = Level.Layout[r.LCc];
                ColTest(ColSensor.LeftCenter, r.LCp, r.LCc);
            }
            if (Sensors.HasFlag(ColSensor.LeftLower))
            {
                r.LL = new Point(projected.X - hmargin, projected.Y + projected.Height - vmargin);
                r.LLc = r.LL.ToCell();

                r.LLp = Level.Layout[r.LLc];
                ColTest(ColSensor.LeftLower, r.LLp, r.LLc);
            }
            if (Sensors.HasFlag(ColSensor.RightUpper))
            {
                r.RU = new Point(projected.X + projected.Width + hmargin, projected.Y + vmargin);
                r.RUc = r.RU.ToCell();

                r.RUp = Level.Layout[r.RUc];
                ColTest(ColSensor.RightUpper, r.RUp, r.RUc);
            }
            if (Sensors.HasFlag(ColSensor.RightCenter))
            {
                r.RC = new Point(projected.X + projected.Width + hmargin, C.Y);
                r.RCc = r.RC.ToCell();

                r.RCp = Level.Layout[r.RCc];
                ColTest(ColSensor.RightCenter, r.RCp, r.RCc);
            }
            if (Sensors.HasFlag(ColSensor.RightLower))
            {
                r.RL = new Point(projected.X + projected.Width + hmargin, projected.Y + projected.Height - vmargin);
                r.RLc = r.RL.ToCell();

                r.RLp = Level.Layout[r.RLc];
                ColTest(ColSensor.RightLower, r.RLp, r.RLc);
            }
            if (Sensors.HasFlag(ColSensor.Center))
            {
                r.C = C;
                r.Cc = C;
                r.Cp = Level.Layout[r.Cc];
                ColTest(ColSensor.Center, r.Cp, r.Cc);
            }

            return r; 
        }

        

       /// <summary>
       /// Per-GameObject Update logic
       /// </summary>
        public virtual void Update(GameTime gameTime)
        {
            if (Level != null)
            {
                // Centers of the world
                int hc = World.WorldWidth / 2;
                int vc = World.WorldHeight / 2;

                if (LastPosition != Position && LastPosition != default)
                {
                    // 256 to 0
                    var dx = Math.Abs(LastPosition.X - Position.X);
                    var dy = Math.Abs(LastPosition.Y - Position.Y);
                    if (LastPosition.X > hc && Position.X < hc && dx > hc) Wrapped(Heading.Right);
                    if (LastPosition.X < hc && Position.X > hc && dx > hc) Wrapped(Heading.Left);
                    if (LastPosition.Y < vc && Position.Y > vc && dy > hc) Wrapped(Heading.Up);
                    if (LastPosition.Y > vc && Position.Y < vc && dy > hc) Wrapped(Heading.Down);
                }

                LastPosition = Position;
                
                // Make sure the object is within the world
                if (X > Level.WorldWidth)
                {
                    if (!Wrapped(Heading.Right) && !NoWrap) X %= Level.WorldWidth;
                }
                if (Y > Level.WorldHeight)
                {
                    if (!Wrapped(Heading.Down) && !NoWrap) Y %= Level.WorldHeight;
                }
                while (X < 0)
                {
                    if (!Wrapped(Heading.Left) && !NoWrap) X += Level.WorldWidth; else break;
                }
                while (Y < 0)
                {
                    if (!Wrapped(Heading.Up) && !NoWrap) Y += Level.WorldHeight; else break;
                }
            }


            unchecked
            {
                // Animate if necessary (on every 4th tick (1/15th of a second))
                if (Animate)
                {
                    if ((World.Ticks % 4) == 0)
                        AnimationCounter += AnimSpeed;
                } 

                // Decrease generic timer value if necessary
                if (Timer > 0) Timer--;

                if (Level != null) { 
                // Decrease TTL value if necessary (mirror-born enemies)
                if (TTL > 0 && ((Level.RunTicks % 32) == 0)) 
                    TTL--;
                }
            }
        }

        /// <summary>
        /// Removes the game object from the level
        /// </summary>
        public void Remove()
        {
            Type = ObjType.None;
            Routine = 0;
            Animation = default;
        }

    }
    
    /// <summary>
    /// Valid values for collision sensor locations
    /// </summary>
    [Flags]
    public enum ColSensor
    {
        None        = 0,
        RightCenter = 1,
        LeftCenter  = 2,
        UpCenter    = 4,
        DownCenter  = 8,
        UpLeft      = 0x10,
        UpRight     = 0x20,
        DownLeft    = 0x40,
        DownRight   = 0x80,
        LeftUpper   = 0x100,
        LeftLower   = 0x200,
        RightUpper  = 0x400,
        RightLower  = 0x800,
        Center      = 0x1000,
        Down = DownLeft | DownRight,
        Up = UpLeft | UpRight,
        Left = LeftUpper | LeftLower,
        Right = RightUpper | RightLower
    }

    /// <summary>
    /// Valid types of collision to check for
    /// </summary>
    [Flags]
    public enum ColTypes
    {
        None        = 0,        // Default value
        Solid       = 1,        // Solid objects (concrete and breakable blocks)
        Breakable   = 2,        // Breakable blocks  
        Concrete    = 4,        // Concrete blocks
        Items       = 8,        // Items
        HardItems   = 0x10      // Hardened Items
    }

    /// <summary>
    /// Represents the result of a collision check
    /// </summary>
    public struct ColResult
    {
        public ColTypes Result;             // Collision type
        public ColSensor SensorsCollided;   // Which sensors collided
        public Heading TestDirection;       // Which direction (if CollideLevel(direction, ...)) the collision was in
        public int NumSensorsCollided;      // Number of collided sensors
        public Point? BreakableBlock;       // Cell grid location of the breakable block collided
        public Point? SolidBlock;           // Cell grid location of the solid block collided
        public int NumDownSensors;          // Number of "Down" sensors (0-3)

        /* Sensor locations (world) */
        public Point UL, UC, UR, LU, LC, LL, RU, RC, RL, DL, DC, DR, C;

        /* Sensor locations (cell) */
        public Point ULc, UCc, URc, LUc, RUc, LCc, Cc, RCc, LLc, RLc, DLc, DCc, DRc;

        /* Cell contents */
        public Cell ULp, UCp, URp, LUp, RUp, LCp, Cp, RCp, LLp, RLp, DLp, DCp, DRp;

        /// <summary>
        /// Whether or not the cell collided with (BlockCell) is solid
        /// </summary>
        public bool Solid => Layout.IsSolid(BlockCell);

        /// <summary>
        /// Whether or not the cell collided with (BlockCell) is breakable
        /// </summary>
        public bool Breakable => BreakableBlock.HasValue;

        /// <summary>
        /// Render collision sensors for debug use
        /// </summary>
        public void Render(SpriteBatch batch)
        {
            void RenderDot(Point p)
            {
                batch.FillRectangle(new Rectangle(p, new Point(1, 1)), Color.Cyan);
            }

            RenderDot(UL); RenderDot(UC); RenderDot(UR);
            RenderDot(DL); RenderDot(DC); RenderDot(DR);
            RenderDot(LL); RenderDot(LC); RenderDot(LU);
            RenderDot(RL); RenderDot(RC); RenderDot(RU);

        }

        /// <summary>
        /// Determines if the object is near, over, or off an edge
        /// </summary>
        public bool OnEdge() => NumSensorsCollided < NumDownSensors;

        /// <summary>
        /// Determines if the object is over or off the edge in a specific direction
        /// </summary>
        public bool OnEdge(Heading direction) 
        {
            if (direction == Heading.Left && !SensorsCollided.HasFlag(ColSensor.DownLeft)) return true;
            if (direction == Heading.Right && !SensorsCollided.HasFlag(ColSensor.DownRight)) return true;
            return false;
        }

        /// <summary>
        /// Determines how far the collision overlaps to the right
        /// </summary>
        public int RightOverlap
        {
            get
            {
                if (SensorsCollided.HasFlag(ColSensor.RightCenter))
                    return RC.X - RCc.X * Game.NativeTileSize.X;
                if (SensorsCollided.HasFlag(ColSensor.RightUpper))
                    return RU.X - RUc.X * Game.NativeTileSize.X;
                if (SensorsCollided.HasFlag(ColSensor.RightLower))
                    return RL.X - RLc.X * Game.NativeTileSize.X;
                return 0;
            }
        }

        /// <summary>
        /// Determines how far the collision overlaps to the left
        /// </summary>
        public int LeftOverlap
        {
            get
            {
                if (SensorsCollided.HasFlag(ColSensor.LeftCenter))
                    return (LCc.X + 1) * Game.NativeTileSize.X - LC.X;
                if (SensorsCollided.HasFlag(ColSensor.LeftUpper))
                    return (LUc.X + 1) * Game.NativeTileSize.X - LU.X;
                if (SensorsCollided.HasFlag(ColSensor.LeftLower))
                    return (LLc.X + 1) * Game.NativeTileSize.X - LL.X;
                return 0;
            }
        }

        /// <summary>
        /// Determines how far the collision overlaps downwards
        /// </summary>
        public int DownOverlap
        {
            get
            {
                if (SensorsCollided.HasFlag(ColSensor.DownCenter))
                    return DC.Y - DCc.Y * Game.NativeTileSize.Y;
                if (SensorsCollided.HasFlag(ColSensor.DownLeft))
                    return DL.Y - DLc.Y * Game.NativeTileSize.Y;
                if (SensorsCollided.HasFlag(ColSensor.DownRight))
                    return DR.Y - DRc.Y * Game.NativeTileSize.Y;
                return 0;
            }
        }

        /// <summary>
        /// Determines how far the collision overlaps upwards
        /// </summary>
        public int UpOverlap
        {
            get
            {
                if (SensorsCollided.HasFlag(ColSensor.UpCenter))
                    return (UCc.Y + 1) * Game.NativeTileSize.Y - UC.Y;
                if (SensorsCollided.HasFlag(ColSensor.UpLeft))
                    return (ULc.Y + 1) * Game.NativeTileSize.Y - UL.Y;
                if (SensorsCollided.HasFlag(ColSensor.UpRight))
                    return (URc.Y + 1) * Game.NativeTileSize.Y - UR.Y;
                return 0;
            }
        }

        /// <summary>
        /// Determines how far the collision overlaps in the direction tested
        /// </summary>
        public int Overlap
        {
            get
            {
                switch (TestDirection)
                {
                    case Heading.Right:
                        if (SensorsCollided.HasFlag(ColSensor.RightCenter)) 
                            return RC.X - RCc.X * Game.NativeTileSize.X;
                        if (SensorsCollided.HasFlag(ColSensor.RightUpper)) 
                            return RU.X - RUc.X * Game.NativeTileSize.X;
                        if (SensorsCollided.HasFlag(ColSensor.RightLower)) 
                            return RL.X - RLc.X * Game.NativeTileSize.X;
                        break;
                    case Heading.Left:
                        if (SensorsCollided.HasFlag(ColSensor.LeftCenter)) 
                            return (LCc.X + 1) * Game.NativeTileSize.X - LC.X;
                        if (SensorsCollided.HasFlag(ColSensor.LeftUpper)) 
                            return (LUc.X + 1) * Game.NativeTileSize.X - LU.X;
                        if (SensorsCollided.HasFlag(ColSensor.LeftLower)) 
                            return (LLc.X + 1) * Game.NativeTileSize.X - LL.X;
                        break;
                    case Heading.Up:
                        if (SensorsCollided.HasFlag(ColSensor.UpCenter)) 
                            return (UCc.Y + 1) * Game.NativeTileSize.Y - UC.Y;
                        if (SensorsCollided.HasFlag(ColSensor.UpLeft)) 
                            return (ULc.Y + 1) * Game.NativeTileSize.Y - UL.Y;
                        if (SensorsCollided.HasFlag(ColSensor.UpRight)) 
                            return (URc.Y + 1) * Game.NativeTileSize.Y - UR.Y;
                        break;
                    case Heading.Down:
                        if (SensorsCollided.HasFlag(ColSensor.DownCenter)) 
                            return DC.Y - DCc.Y * Game.NativeTileSize.Y;
                        if (SensorsCollided.HasFlag(ColSensor.DownLeft)) 
                            return DL.Y - DLc.Y * Game.NativeTileSize.Y;
                        if (SensorsCollided.HasFlag(ColSensor.DownRight)) 
                            return DR.Y - DRc.Y * Game.NativeTileSize.Y;
                        break;
                }
                return 0;
            }
        }

        /// <summary>
        /// Cell location of collided block
        /// </summary>
        public Point? LeftBlock
        {
            get
            {
                if (SensorsCollided.HasFlag(ColSensor.LeftCenter)) return LCc;
                if (SensorsCollided.HasFlag(ColSensor.LeftUpper)) return LUc;
                if (SensorsCollided.HasFlag(ColSensor.LeftLower)) return LLc;
                return null;
            }
        }

        /// <summary>
        /// Cell location of collided block
        /// </summary>
        public Point? RightBlock
        {
            get
            {
                if (SensorsCollided.HasFlag(ColSensor.RightCenter)) return RCc;
                if (SensorsCollided.HasFlag(ColSensor.RightUpper)) return RUc;
                if (SensorsCollided.HasFlag(ColSensor.RightLower)) return RLc;
                return null;
            }
        }

        /// <summary>
        /// Cell location of collided block
        /// </summary>
        public Point? DownBlock
        {
            get
            {
                if (SensorsCollided.HasFlag(ColSensor.DownCenter)) return DCc;
                if (SensorsCollided.HasFlag(ColSensor.DownLeft)) return DLc;
                if (SensorsCollided.HasFlag(ColSensor.DownRight)) return DRc;
                return null;
            }
        }

        /// <summary>
        /// Cell location of collided block
        /// </summary>
        public Point? UpBlock
        {
            get
            {
                if (SensorsCollided.HasFlag(ColSensor.UpCenter)) return UCc;
                if (SensorsCollided.HasFlag(ColSensor.UpLeft)) return ULc;
                if (SensorsCollided.HasFlag(ColSensor.UpRight)) return URc;
                return null;
            }
        }

        /// <summary>
        /// Cell location of first block collided with (might not be the one you think)
        /// If Collision was done in a direction -- that direction is prioritized,
        /// otherwise priority is: Center, Right, Left, Up, Down
        /// </summary>
        public Point? Block
        {
            get
            {
                switch (TestDirection)
                {
                    case Heading.Right:
                        if (SensorsCollided.HasFlag(ColSensor.RightCenter)) return RCc;
                        if (SensorsCollided.HasFlag(ColSensor.RightUpper)) return RUc;
                        if (SensorsCollided.HasFlag(ColSensor.RightLower)) return RLc;
                        break;
                    case Heading.Left:
                        if (SensorsCollided.HasFlag(ColSensor.LeftCenter)) return LCc;
                        if (SensorsCollided.HasFlag(ColSensor.LeftUpper)) return LUc;
                        if (SensorsCollided.HasFlag(ColSensor.LeftLower)) return LLc;
                        break;
                    case Heading.Up:
                        if (SensorsCollided.HasFlag(ColSensor.UpCenter)) return UCc;
                        if (SensorsCollided.HasFlag(ColSensor.UpLeft)) return ULc;
                        if (SensorsCollided.HasFlag(ColSensor.UpRight)) return URc;
                        break;
                    case Heading.Down:
                        if (SensorsCollided.HasFlag(ColSensor.DownCenter)) return DCc;
                        if (SensorsCollided.HasFlag(ColSensor.DownLeft)) return DLc;
                        if (SensorsCollided.HasFlag(ColSensor.DownRight)) return DRc;
                        break;
                    default:
                        if (SensorsCollided.HasFlag(ColSensor.Center)) return Cc;
                        if (SensorsCollided.HasFlag(ColSensor.RightCenter)) return RCc;
                        if (SensorsCollided.HasFlag(ColSensor.RightUpper)) return RUc;
                        if (SensorsCollided.HasFlag(ColSensor.RightLower)) return RLc;
                        if (SensorsCollided.HasFlag(ColSensor.LeftCenter)) return LCc;
                        if (SensorsCollided.HasFlag(ColSensor.LeftUpper)) return LUc;
                        if (SensorsCollided.HasFlag(ColSensor.LeftLower)) return LLc;
                        if (SensorsCollided.HasFlag(ColSensor.UpCenter)) return UCc;
                        if (SensorsCollided.HasFlag(ColSensor.UpLeft)) return ULc;
                        if (SensorsCollided.HasFlag(ColSensor.UpRight)) return URc;
                        if (SensorsCollided.HasFlag(ColSensor.DownCenter)) return DCc;
                        if (SensorsCollided.HasFlag(ColSensor.DownLeft)) return DLc;
                        if (SensorsCollided.HasFlag(ColSensor.DownRight)) return DRc;
                        break;
                }
                return null;
            }
        }

        /// <summary>
        /// Gets the value of the cell collided with (Block)
        /// </summary>
        public Cell BlockCell
        {
            get
            {
                switch (TestDirection)
                {
                    case Heading.Right:
                        if (SensorsCollided.HasFlag(ColSensor.RightCenter)) return RCp;
                        if (SensorsCollided.HasFlag(ColSensor.RightUpper)) return RUp;
                        if (SensorsCollided.HasFlag(ColSensor.RightLower)) return RLp;
                        break;
                    case Heading.Left:
                        if (SensorsCollided.HasFlag(ColSensor.LeftCenter)) return LCp;
                        if (SensorsCollided.HasFlag(ColSensor.LeftUpper)) return LUp;
                        if (SensorsCollided.HasFlag(ColSensor.LeftLower)) return LLp;
                        break;
                    case Heading.Up:
                        if (SensorsCollided.HasFlag(ColSensor.UpCenter)) return UCp;
                        if (SensorsCollided.HasFlag(ColSensor.UpLeft)) return ULp;
                        if (SensorsCollided.HasFlag(ColSensor.UpRight)) return URp;
                        break;
                    case Heading.Down:
                        if (SensorsCollided.HasFlag(ColSensor.DownCenter)) return DCp;
                        if (SensorsCollided.HasFlag(ColSensor.DownLeft)) return DLp;
                        if (SensorsCollided.HasFlag(ColSensor.DownRight)) return DRp;
                        break;
                    default:
                        if (SensorsCollided.HasFlag(ColSensor.Center)) return Cp;
                        if (SensorsCollided.HasFlag(ColSensor.RightCenter)) return RCp;
                        if (SensorsCollided.HasFlag(ColSensor.RightUpper)) return RUp;
                        if (SensorsCollided.HasFlag(ColSensor.RightLower)) return RLp;
                        if (SensorsCollided.HasFlag(ColSensor.LeftCenter)) return LCp;
                        if (SensorsCollided.HasFlag(ColSensor.LeftUpper)) return LUp;
                        if (SensorsCollided.HasFlag(ColSensor.LeftLower)) return LLp;
                        if (SensorsCollided.HasFlag(ColSensor.UpCenter)) return UCp;
                        if (SensorsCollided.HasFlag(ColSensor.UpLeft)) return ULp;
                        if (SensorsCollided.HasFlag(ColSensor.UpRight)) return URp;
                        if (SensorsCollided.HasFlag(ColSensor.DownCenter)) return DCp;
                        if (SensorsCollided.HasFlag(ColSensor.DownLeft)) return DLp;
                        if (SensorsCollided.HasFlag(ColSensor.DownRight)) return DRp;
                        break;
                }
                return Cell.Empty;
            }
        }

    }

}
