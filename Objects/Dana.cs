using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MonoGame.Extended;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SKX.Objects
{

    /// <summary>
    /// Dana represents the player object (whether it's actually "Dana" or "Adam")
    /// 
    /// Dana gets added to the Level as a normal GameObject,
    /// but the Level holds a reference to Dana in the 'Dana' property.
    /// Ideally you should create Dana via Level.LoadDana().
    /// </summary>
   
    public class Dana : GameObject
    {
        /* Savable parameters are stored in the 'Sesh' */
        private Sesh Sesh;

        /* Behavioral Parameters */
        public static uint JumpAcc = 12;                 // Dana's jump acceleration
        public static uint JumpHeight = 12;             // Max jump height  
        public static uint LandingDelayTicks = 5;       // How long to pause after Dana lands on the ground
        public static uint PostDeathDelayTicks = 60;    // How long to pause after death
        public static double RunSpeed = 0.8;            // Dana's running speed
        private static uint HeadHitTicks = 10;           // How long to pause when Dana hits his head

        // When collecting a half jar of life
        public static int HalfJarLifeSpeed = 8;
        public static int HalfJarLifeStep = 20;
        public static int HalfJarLifeMultiplier = 2;

        // When collecting a full jar of life
        public static int FullJarLifeSpeed = 8;
        public static int FullJarLifeStep = 50;
        public static int FullJarLifeMultiplier = 5;

        // Miscellaneous
        public static uint FairiesPerExtraLife = 9;
        public Heading MagicDirection;      // The direction Dana was facing when he last used magic
        public Point MagicCell;             // Cell Dana last cast magic on
        public Point HeadCell;             // Cell Dana last hit his head on

        /* Dana's internals */
        public int LifeSpeed = 7;          // How fast life is currently counting down
        public int LifeStep = 10;          // How many life are subtracted
        public bool JumpButton;             // Jump button held 
        public bool Crouching;              // Dana is crouching
        public bool Jumping;                // Dana is jumping
        public bool Falling;                // Dana is falling
        private uint JumpStep = 0;          // Counter into Dana's jump
        private int LifeStepTimer = 10;    // How many ticks remaining until the next life tick
        public DanaState State;            // Dana's current state
        private bool AnimationEnded;        // Whether or not the current animation ended
        private uint LandCount = 0;         // Landing pause ticks
        private uint DeadStep = 0;          // Counter into dying animation
        public uint HeadTimer = 0;         // Counter into hitting head

        /* Animation indirection */
        // Note:  While it's possible to achieve the same effect with a palette Swap (see Swap class),
        // I'm intentionally using separate art so that an artist can give Adam his own unique appearance
        // it was never my intention to use Tecmo's Copyright NES art forever,  except potentially in the Classic story
        public static Animation AnimStand => Game.Sesh.Apprentice ? Animation.AdamStand : Animation.DanaStand;
        public static Animation AnimCrouch => Game.Sesh.Apprentice ? Animation.AdamCrouch : Animation.DanaCrouch;
        public static Animation AnimRun => Game.Sesh.Apprentice ? Animation.AdamRun : Animation.DanaRun;
        public static Animation AnimMagic => Game.Sesh.Apprentice ? Animation.AdamMagic : Animation.DanaMagic;
        public static Animation AnimDead => Game.Sesh.Apprentice ? Animation.AdamDead : Animation.DanaDead;
        public static Animation AnimDeadDead => Game.Sesh.Apprentice ? Animation.AdamDeadDead : Animation.DanaDeadDead;
        public static Animation AnimLand => Game.Sesh.Apprentice ? Animation.AdamLand : Animation.DanaLand;
        public static Animation AnimJump => Game.Sesh.Apprentice ? Animation.AdamJump : Animation.DanaJump;
        public static Animation AnimJump2 => Game.Sesh.Apprentice ? Animation.AdamJump2 : Animation.DanaJump2;
        public static Animation AnimHead => Game.Sesh.Apprentice ? Animation.AdamHead : Animation.DanaHead;
        public static Animation AnimCrouchWalk => Game.Sesh.Apprentice ? Animation.AdamCrouchWalk : Animation.DanaCrouchWalk;

        /// <summary>
        /// Dana's constructor
        /// </summary>
        public Dana(Level level) : base(level, ObjType.Dana)
        {
            Sesh = Game.Sesh;
            HitBox = new Rectangle(4, 4, 8, 12);   // Don't ever touch this (unless it's wrong)
            HurtBox = new Rectangle(4, 4, 8, 8);   // Don't ever touch this (unless it's wrong)
            Animation = AnimStand;     // Start standing
            TweakY = 1;                          // Minor adjustment
            Direction = Heading.Right;           // The Level will override this if Dana needs to face left
            GravityApplies = true;               // One of the few things in this game that has gravity
            BlocksMagic = false;                 // This would get silly
            OnFloor = true;                      // Set this to avoid Dana "hitting the floor" when the level begins
        }

        /// <summary>
        /// Gives Dana a full life jar 
        /// </summary>
        public void GiveFullTimeBottle()
        {
            LifeSpeed = FullJarLifeSpeed;
            LifeStep = FullJarLifeStep;
            Level.Life *= FullJarLifeMultiplier;
        }

        /// <summary>
        /// Gives Dana a half life jar
        /// </summary>
        public void GiveHalfTimeBottle()
        {
            LifeSpeed = HalfJarLifeSpeed;
            LifeStep = HalfJarLifeStep;
            Level.Life *= HalfJarLifeMultiplier;
        }

        /// <summary>
        /// Resets Dana's life to 10K
        /// </summary>
        public void GiveBlueHourglass()
        {
            Level.Life = (Level.Layout.StartLife ?? 10_000);
            Level.CheckMusic();
        }

        /// <summary>
        /// Explodes all the spawn mirrors
        /// </summary>
        public bool DestroySpawnPoints()
        {
            bool success = false;
            foreach(var s in Level.Layout.Spawns)
            {
                if (s.Phase0 > 0 || s.Phase1 > 0 || s.DropletRate > 0)
                {
                    if (s.SpawnItems.Count > 0 || s.DropletRate > 0)
                    {
                        if (Level.Layout[s.Position] == Cell.Mirror)
                        {
                            s.Disabled = true;
                            success = true;
                            Level.Layout[s.Position] = Cell.Empty;
                            Level.AddObject(new Remains(Level, Cell.RwBell, s.Position.ToWorld(), false));
                        }
                    }
                }
            }
            if (success)
            {
                Sound.Burn.Play();
            }
            return success;
        }

        /// <summary>
        /// Resets Dana's life to 5K
        /// </summary>
        public void GiveHourglassGold()
        {
            Level.Life = (Level.Layout.StartLife ?? 10_000) / 2;
            Level.CheckMusic();
        }

        /// <summary>
        /// When Dana collects a fairy
        /// </summary>
        public void GiveFairy()
        {
            Sound.Fairy.Play();
            Sesh.Fairies++;
            Sesh.TotalFairies++;
            if (Sesh.Fairies > FairiesPerExtraLife)
            {
                Sesh.Fairies = 0;
                Sesh.Lives++;
                Sound.ExtraLife.Play();
                Level.AddObject(new Superscript(Level, Tile.FxOneUp) { Position = Position });
            }
        }

        /// <summary>
        /// Kills Dana
        /// </summary>
        public void Die()
        {
            Vx = 0;                     // Stop moving
            Vy = 0;
            DeadStep = 0;               // Death animation counter = 0
            Level.VapourModeOff();      // Vapour off
            Level.StopMusic();          // Music off
            Sound.Die.Play();           // Death sound on
            Level.State = LevelState.Dying; // Start dying
            State = DanaState.Dead;     // Update the level state
            Sesh.CheckReturn();         // Check if we need to return from a bonus room
        }

        // This all could probably be replaced by a simple call to set
        // Dana's Y velocity to some negative value and let it diminish like
        // how jumping works, but I don't want to break it since it looks right
        // and I have other things to work on...
        // Feel free to experiment

        private bool DanaDyingMove()
        {
            if (DeadStep++ < 16) return false;
            else if(DeadStep < 24) { Vy = -1; }
            else if (DeadStep < 32) { Vy = -0.5; }
            else if (DeadStep < 40) { Vy = 0.5; }
            else if (DeadStep < 48) { Vy = 1;  }
            else { Vy = 1.5; }

            Move();

            return (DeadStep > 48);
        }

        // Called while Dana is dying to update his dying animation
        private void DanaDying()
        {

            Animation = AnimDead;
            if (DanaDyingMove()) { 
             
                Vy = 1;
                Move();

                // Look for solid floor
                var c = CollideLevel(ColSensor.Down);
                if (c.Solid || Y >= Level.WorldHeight)
                {
                    Sesh.Lives--;
                    Level.State = LevelState.Dead;
                }
            }
        }

        // Updates Dana's life
        private void DanaControlUpdateLife()
        {
            /* Count down life */
            if (--LifeStepTimer < 1)
            {
                LifeStepTimer = LifeSpeed;
                Level.Life -= LifeStep;
                if (Level.Life < 1)
                {
                    Level.Life = 0;
                    Level.TimeOver();
                }
                else
                {
                    Level.CheckMusic();
                }
            }
        }

        /* Finds the block that Dana will perform magic on */
        public Point FindTargetBlock()
        {
            var c = Center;                  // Get Dana's center point
            c.X += FlipX ? -2 : +2;          // Add 2 pixels in the direction he's facing
            var p = c.ToCell();              // Find that cell
            p.X += FlipX ? -1 : +1;          // Move one cell over
            if (Crouching) p.Y++;            // If he's crouching, move down a row

            return p;

        }

        private void CharTransfer()
        {
            Level.ResetTitlescreenTimer();

            // Don't do this in classic
            if (Sesh.Lives > 0 && !Game.IsClassic)
            {
                if (Sesh.Apprentice)
                {
                    Sesh.AdamLives--;
                    Sesh.DanaLives++;
                }
                else
                {
                    Sesh.AdamLives++;
                    Sesh.DanaLives--;
                }
                Sound.Break.Play();
                if (Sesh.Lives < 1)
                {
                    // Swap to the other character if we have no more lives
                    // now...
                    Sesh.Apprentice = !Sesh.Apprentice;
                    Animation = AnimStand;
                }
                return;
            }

            Sound.Wince.Play();
            return;
        }

        private void CharSwitch()
        {
            Level.ResetTitlescreenTimer();


            if (Level.Layout.Character == CharacterMode.Any && !Game.IsClassic)
            {
                // Check lives
                if (Sesh.Apprentice && Sesh.DanaLives < 1)
                {
                    Sound.Wince.Play();
                    return;
                }
                if (!Sesh.Apprentice && Sesh.AdamLives < 1)
                {
                    Sound.Wince.Play();
                    return;
                }

                // Switch character
                Sesh.Apprentice = !Sesh.Apprentice;
                Animation = AnimStand;
                Sound.Collect.Play();
            }
            else
            {
                // Can't switch
                Sound.Wince.Play();
            }
        }

        /* Dana's controls */
        private bool Controls()
        {
            bool animSet = false;

            /* If we're on the title screen we check for the MAGIC button
             * to switch characters */
            if (Level.State == LevelState.TitleScreen)
            {
                if (Control.Magic.Pressed(true))
                {
                    CharSwitch();
                }
                if (Control.Fireball.Pressed(true))
                {
                    CharTransfer();
                }
                return true;
            }

            /* Update life */
            DanaControlUpdateLife();

            // Get controls
            ControlState input;
            if (Level.Playback != null)
            {
                input = new ControlState(Level.CurrentDemoFrame.s, Level.LastDemoFrame.s);
            } else
            {
                input = Control.GetState();
            }

            // If Dana is jumping or falling he needs to move no matter what
            var move = Jumping | Falling;

            if (Game.DebugMode && Control.KeyboardState.IsKeyDown(Keys.LeftControl)
                && Control.MouseState.LeftButton == ButtonState.Pressed 
                && Control.LastMouseState.LeftButton == ButtonState.Released
                && Level.WorldRectangle.Contains(Control.MousePos))
            {
                // Warp Dana
                Level.Warp((Control.MousePos - Game.CameraOffset + Game.CameraPos).ToCell());
                return true;
            }

            // If Dana is hitting his head on something then don't do anything else
            // until it's over except for checking for enemy collision
            if (HeadTimer > 0)
            {
                if (!input.Down(BindingState.Jump)) JumpButton = false;
                HeadTimer--;
                DanaCollideObjects();
                return true;
            }

            // If Dana is casting magic then don't do anything else until that
            // animation finishes except checking for enemy collision
            if (Animation == AnimMagic)
            {
                if (AnimationEnded)
                {
                    AnimationEnded = false;
                    Animation = AnimStand;
                }

                // Check for enemy collision even though we're suspending everything
                // else -- otherwise Dana is invincible while mashing the Magic button
                DanaCollideObjects();
                return true;
            }

            Animation newAnim = Animation;

            // If Dana is jumping, then handle all of that
            if (JumpStep > 0)
            {
                JumpStep--;
                if (JumpStep > JumpHeight - 4)
                {
                    newAnim = AnimLand;
                    animSet = true;
                } else
                {
                    newAnim = AnimStand;
                    Vy -= 0.476;
                }
            }
            else if (Vy > 0)
            {
                // Dana isn't jumping any more, so clear that flag
                Jumping = false;
            }

            // If Dana is falling but he's hit the floor then clear
            // the falling flag and start his landing delay
            if (Falling && OnFloor)
            {
                LandCount = LandingDelayTicks;
                Falling = false;
            } 

            // If Dana isn't on the floor and he's not jumping
            // then he's falling
            if (!OnFloor && !Jumping)
            {
                /* Falling */
                Falling = true;
                newAnim = AnimStand;
            }
            
            // If Dana isn't jumping, the jump button wasn't held before, the jump button is
            // now pressed, and he's on the floor, and he's not still landing, then start a jump
            if (!Jumping && !JumpButton && input.Down(BindingState.Jump) && OnFloor && LandCount == 0)
            {
                /* Jump */
                move = true;
                JumpButton = true;
                newAnim = AnimJump;
                Jumping = true;
                JumpStep = JumpHeight;
            } else if (OnFloor && Control.Jump.Up())
            {
                JumpButton = false;
            }

            // If Dana is landing ...
            if (LandCount > 0)
            {
                LandCount--;
                newAnim = AnimLand;
                animSet = true;
            }
            else if (input.Down(BindingState.Left))
            {
                // Run to the left
                move = true;
                FlipX = true;
                Direction = Heading.Left;
                newAnim = !OnFloor ? AnimJump2 : AnimRun;
                Vx = -RunSpeed;
            }
            else if (input.Down(BindingState.Right))
            {
                // Run to the right
                move = true;
                FlipX = false;
                Direction = Heading.Right;
                newAnim = !OnFloor ? AnimJump2 : AnimRun;
                Vx = RunSpeed;
            }
            else
            {
                // Dana isn't running
                Vx = 0;
                
            }
            if (input.Down(BindingState.Crouch) && OnFloor)
            {
                // Dana is crouching
                move = true;
                animSet = true;
                newAnim = Vx == 0 ? AnimCrouch : AnimCrouchWalk;
                Crouching = true;
            }
            else
            {
                Crouching = false;
            }

            // Used for toggle blocks

            var md = input.Down(BindingState.Magic);
            if (Level.MagicDown && !md)
            {
                if (Level.Layout[Center.ToCell()] == Cell.ToggleBlock)
                {
                    Die();
                    Level.MagicDown = md;
                    return true;
                }
            }
            Level.MagicDown = md;

            // Check MAGIC button
            if (input.Pressed(BindingState.Magic))
            {
                MagicDirection = Direction;                 // Use this for later
                newAnim = AnimMagic;                        // Set animation
                animSet = true;                             // Don't just stand there
                CastMagic();                                // Magical side effects
            } else
            {
                MagicCell = default;                        // Revert the cell Dana is performing magic on
            }

            // Check FIREBALL button
            if (input.Pressed(BindingState.Fireball) && !Sesh.ScrollDisabled)
            {
                newAnim = AnimMagic;        // Set animation
                animSet = true;             // Don't just stand there
                CastScrollItem();           // Magical side effect
            }

            // Move Dana as appropriate based on velocity and other things
            if (move)
            {
                Move();
                animSet = true;             // Don't stand if he's moving
            }

            // If we haven't done anything to warrant a better animation
            // then set the standing animation
            if (!animSet) newAnim = AnimStand;
            
            // Only once we've computer everything do we update Animation
            // since multiple writes to this property with varying values
            // will reset the animation counter
            Animation = newAnim;

            // Return false to process collision
            return false;

        }

        /// <summary>
        /// Called by the base class when our animation ends
        /// </summary>
        public override void OnAnimationEnd()
        {
            AnimationEnded = true;
        }

        // Try to cast magic
        private void CastMagic()
        {

            Point tgt = FindTargetBlock();              // Find block to cast
            MagicCell = tgt;

            if (Level.Layout.BreakBlock(tgt, Animation.BlockBreak))           // Try to break the block
            {
                // Block destroyed
                Sound.Break.Play();
                return;
            }
            var r = Level.Layout.MakeBlock(tgt);

            if (r == 0)            // Try to create a block
            {
                // Block created
                Sound.Make.Play();
                return;
            }
            else if (r == 2) return;    // Magic used on something else

            if (Level.Layout.MagicBlock(tgt))
            {
                return;
            }

            // Magic failed
            Level.Layout.DrawSparkle(tgt, Animation.MagicAttempt, false);
            Sound.Wince.Play();

        }

        // Try to use a fireball, etc.
        private void CastScrollItem()
        {

            if (Sesh.ScrollItems.Count == 0) return;    // Exit if we have no items
            if (Sesh.MaxFireballs != -1)                // Exit if we have too many fireballs going
            {
                var fbs = Level.Objects.Count(o => o is Fireball);
                if (fbs >= Sesh.MaxFireballs)
                {
                    return;
                }
            }

            var item = Sesh.ScrollItems[0];             // Get the next item in the scroll
            Sesh.ScrollItems.RemoveAt(0);               // Remove it
            Sound.Fire.Play();

            if (item == Cell.RedFireballJar)
            {
                Sound.Burn.Play();
                Level.Explode(Cell.RedFireballJar);     

                // There used to be a return here but the gameplay works
                // much better with out :)
            }

            var fb = new Objects.Fireball(Level, Position, FlipX ? Heading.Left : Heading.Right,
                                            Crouching, item);
            Level.AddObject(fb);

        }

        // Process all Dana collision
        private new void Collision()
        {
            DanaCollideLevel();         // Dana collision with the level
            DanaCollideItems();         // Dana collision with items

            // Stop processing things if the level is now doing something
            // else (like we went in a door).  Without this dana can die after
            // he's entered the door.
            if (State != DanaState.Control || Level.State != LevelState.Running) return;     

            DanaCollideObjects();       // Dana collision with enemies
        }

        // Process collision with other objects
        private void DanaCollideObjects()
        {
            foreach(var o in CollideObjects())
            {
                if (o.Friendly) continue;

                if (!o.HurtsPlayer)
                {
                    if (o.CollidedWithDana()) return;
                } else
                {
                    if (Sesh.Apprentice && o.AdamImmune) continue;
                    if (!Sesh.Apprentice && o.DanaImmune) continue;
                    Die();
                }
            }
        }

        // Collide with non-solids
        public void DanaCollideItems()
        {
            var i = Center.ToCell();

            if (i.X < 0 || i.X > Level.WorldWidth) return;
            if (i.Y < 0 || i.Y > Level.WorldHeight) return;

            var c = Level.Layout[i];

            if (Layout.IsItem(c))
            {
                Level.Layout[i] = GiveItem(c, i, out bool collect, out bool sound);
                if (sound) Sound.Collect.Play();
                if (collect) Level.Layout.DrawSparkle(i, Animation.Collect, true, false);
            }
        }

        // Called when Dana touches a key
        internal void GiveKey(Point i, bool world = false)
        {
            Point w;
            if (world)
            {
                w = i;
                i = i.ToCell();
            }  else
            {
                w = i.ToWorld();
            }
            var doors = Level.Layout.FindDoorsForKey(i);
            if (doors.Count() > 0)
            {
                Sound.Key.Interrupt();
                Level.KeyToDoorsAnimation(doors, w);
                Sesh.DoorsOpened.AddRange(doors.Select(x => new OpenDoor(x, Sesh.RoomNumber)));
            }
            else
            {
                // Do nothing -- no doors to open!  just collect the key
                Sound.Collect.Play();
            }
        }

        // Called when Dana exits the level
        public void EnterDoor(bool dark_door, DoorInfo doorinfo)
        {

            var go_hidden = Sesh.HasThisRoomsShrine() || Level.ForceHidden;
            var old = Sesh.RoomNumber;      // Current room number

            Sesh.WarpTo = new Point();

            if (doorinfo != null)
            {
                if (doorinfo.Type == DoorType.Warp)
                {
                    Level.Warp(doorinfo.Target);
                    return;
                }

                Sesh.FastStars = doorinfo.FastStars;
                Sesh.RoomNumber = doorinfo.RoomNumber;
                if (doorinfo.Type == DoorType.RoomAndWarp)
                {
                    Sesh.SkipThankYou = true;
                    Sesh.WarpTo = doorinfo.Target;
                } 

            }
            else if (dark_door && Level.Layout.NextRoomSecret > 0) 
            {
                // Default secret exit
                Sesh.FastStars = true;
                Sesh.RoomNumber = Level.Layout.NextRoomSecret;
            }
            else if (Sesh.ReturnToRoom > 0)
            {
                Sesh.CheckReturn();
            }
            else if (Sesh.HasItemFromThisRoom(Cell.GoldWing))
            {
                // Gold wing, skip some levels
                Sesh.FastStars = true;
                Sesh.RoomNumber = Level.Layout.NextRoomWing;
            }
            else if (Level.Layout.NextRoom > 0)
            {
                // Default normal exit
                Sesh.FastStars = false;
                Sesh.RoomNumber = Level.Layout.NextRoom;
            } else
            {
                // Just go to the next room number if all else fails
                Sesh.FastStars = false;
                Sesh.RoomNumber++;
            }

            if (Sesh.BonusRoomQueued && Sesh.WarpTo == default && !go_hidden)
            {
                Sesh.BonusRoomQueued = false;
                go_hidden = true;
            }

            // If we're going to a hidden room (constellation sign), then
            // set up the room number and ReturnTo room number
            if (go_hidden)
            {
                if (Sesh.WarpTo != default)
                    Sesh.BonusRoomQueued = true;
                else 
                    Sesh.SetUpHiddenRoom();
            }

            // If we're using a secret exit number it should always override the next room
            // even (and especially) if it's a hidden.  Leave the ReturnTo to its default.
            if (Sesh.SecretExit)
            {
                Sesh.RoomNumber = Level.Layout.NextRoomSecret;
            }

            if (old != Sesh.RoomNumber)        // Only increase the GDV if we actually 
                Sesh.AddRoomTally(old);         // went someplace
            else
                Sesh.NoPoints = true;           // Don't give Dana points for life remaining

            Level.DoDoorAnimation();
        }

        // Called when Dana exits the level through a visible door
        public void EnterDoor(Cell c, Point i)
        {
            Level.DoorExited = i;
            var doorinfo = Level.Layout.Doors.FirstOrDefault(x => x.Position == i);
            EnterDoor(c == Cell.DarkDoorOpen, doorinfo);
        }

        // Gives Dana a scroll item if he has the capacity
        public void GiveScrollItem(Cell c)
        {
            if (Sesh.ScrollItems.Count < Sesh.ScrollSize)
            {
                Sesh.ScrollItems.Add(c);
            }
        }

        // Extends the scroll if possible
        public void GiveScroll()
        {
            // Max scroll is 8 in classic
            // but it's 18 in every other mode.
            if (Sesh.ScrollSize < (Game.IsClassic ? 8 : 18))
            {
                Sesh.ScrollSize++;
            }
        }

        public void GiveVapour()
        {
            Level.VapourModeOn();
        }

        // Gives Dana an extra life
        public void GiveExtraLife(int numberOfLivesToAdd, Point x)
        {
            Sesh.Lives += numberOfLivesToAdd;
            Sound.ExtraLife.Play();
            Level.AddObject(new Superscript(Level, numberOfLivesToAdd == 5 ? Tile.FxFiveUp : Tile.FxOneUp) 
                            { Position = x.ToWorld() });
        }

        // Dana collected a question position
        private void GiveQuestionPotion()
        {
            foreach(var s in Level.Layout.Spells.Where(x => x.Trigger == SpellTrigger.Potion))
            {
                if (!s.Finished)
                {
                    s.Potion();
                }
            }
        }


        // Dana collected a spell upgrade (perfume bottle)
        public bool GiveSpellUpgrade()
        {
            for(int i = 0; i < Sesh.ScrollItems.Count(); i++)
            {
                if (Sesh.ScrollItems[i] == Cell.FireballJar)
                {
                    Sesh.ScrollItems.RemoveAt(i);
                    Sesh.ScrollItems.Insert(i, Cell.SuperFireballJar);
                    return true;
                }
                else if (Sesh.ScrollItems[i] == Cell.SnowballJar)
                {
                    Sesh.ScrollItems.RemoveAt(i);
                    Sesh.ScrollItems.Insert(i, Cell.SuperSnowballJar);
                    return true;
                }
            }
            return false;
        }


        /// <summary>
        /// Effect of getting the blue jar -- slows all enemies to speed I, 
        /// including enemies dispensed from spawn points
        /// </summary>
        void SlowAllEnemies()
        {
            foreach(var o in Level.Objects)
            {
                if (o.HurtsPlayer)
                {
                    o.Flags |= ObjFlags.Slow;
                    o.Flags &= ~(ObjFlags.Fast | ObjFlags.Faster);
                    o.FlagsChanged();
                    Level.Layout.DrawSparkleWorld(o.Position, Animation.ShortSparkle, false, false);
                }
            }
            foreach(var s in Level.Layout.Spawns)
            {
                if (s.Disabled) continue;
                if (s.Phase0 == 0 && s.Phase1 == 0) continue;
                foreach(var i in s.SpawnItems)
                {
                    i.Flags |= ObjFlags.Slow;
                    i.Flags &= ~(ObjFlags.Fast | ObjFlags.Faster);
                }
                Level.Layout.DrawSparkle(s.Position, Animation.ShortSparkle, false, false);
            }
        }

        /// <summary>
        /// Called when Dana collides with an item
        /// </summary>
        /// <param name="c">The type of item Dana is collecting</param>
        /// <param name="i">The cell location it was found</param>
        /// <param name="anim">Whether or not the collect animation should appear</param>
        /// <param name="sound">Whether or not the collect sound should play</param>
        /// <returns>The cell to leave behind in place of the original cell</returns>
        public Cell GiveItem(Cell c, Point i, out bool anim, out bool sound)
        { 
            void addInv() {
                Sesh.AddInventory(c, i);
            }

            anim = true;       // Whether or not to play the 'collect' animation
            sound = true;      // Whether or not to play the 'collect' sound

            // Shrines / seals / constellation signs
            if (Layout.IsShrine(c) || Layout.IsSeal(c) || Layout.IsCartouche(c))
            {
                addInv();
                Sesh.PickupCount++;
                return Cell.Empty;
            }

            switch (c)
            {
                case Cell.DarkBlueJar:
                    SlowAllEnemies();
                    break;
                case Cell.YellowJar:
                case Cell.ExplosionJar:
                case Cell.BlueJar:
                case Cell.RedJar:
                case Cell.BlackJar:
                case Cell.GreenJar:
                    sound = !Level.Explode(c);
                    break;

                case Cell.PurpleJar:
                case Cell.HourglassPurple:
                    sound = !DestroySpawnPoints();
                    break;

                case Cell.DarkDoorOpen:
                case Cell.DoorOpenBlue:
                case Cell.DoorOpen:
                    anim = false;
                    sound = false;
                    EnterDoor(c, i);
                    return c;   // Don't delete the door!

                case Cell.Key:
                    anim = false;
                    addInv();
                    GiveKey(i);
                    break;

                case Cell.LootBlue:
                    Sesh.Score += 100;
                    break;

                case Cell.LootGold:
                    Sesh.Score += 2000;
                    break;

                case Cell.IceCream:
                    Sesh.Score += 1_000_000;
                    break;

                case Cell.BluePop:
                    Sesh.Score += 100_000;
                    break;

                case Cell.GreenPop:
                    Sesh.Score += 500_000;
                    break;

                case Cell.Gold:
                case Cell.BagR1:
                    Sesh.Score += 1000;
                    break;

                case Cell.LootBlueGold2:
                case Cell.Gold2:
                case Cell.BagR2:
                    Sesh.Score += 2000;
                    break;

                case Cell.CoinGold:
                case Cell.BagR5:
                    Sesh.Score += 5000;
                    break;

                case Cell.CoinGold2:
                case Cell.BagG5:
                    Sesh.Score += 50_000;
                    break;

                case Cell.SpellUpgrade:
                    GiveSpellUpgrade();
                    break;

                case Cell.LootBlueCrystalGold:
                case Cell.CrystalGold:
                case Cell.RwCrystal:
                    Sesh.Score += 200;
                    Sesh.FireballRange += 2;
                    break;

                case Cell.LootGoldSuperFireballJar:
                case Cell.SuperFireballJar:
                    GiveScrollItem(Cell.SuperFireballJar);
                    break;

                case Cell.LootGoldScroll:
                case Cell.Scroll:
                case Cell.RwScroll:
                    GiveScroll();
                    break;

                case Cell.BlueBell:
                    Level.SpawnJack();
                    break;

                case Cell.LootGoldBell:
                case Cell.Bell:
                case Cell.RwBell:
                    Level.SpawnFairy();
                    break;

                case Cell.BagW1:
                case Cell.Silver:
                    Sesh.Score += 100;
                    break;

                case Cell.BagW2:
                case Cell.Silver2:
                    Sesh.Score += 200;
                    break;

                case Cell.BagW5:
                case Cell.CoinBlue:
                    Sesh.Score += 500;
                    break;

                case Cell.CrystalBlue:
                    Sesh.Score += 100;
                    Sesh.FireballRange += 1;
                    break;

                case Cell.LootRedCrystalRed:
                case Cell.CrystalRed:
                    Sesh.Score += 1000;
                    Sesh.MaxFireballs += 1;
                    break;

                case Cell.LootBlueFireballJar:
                case Cell.FireballJar:
                    GiveScrollItem(Cell.FireballJar);
                    break;

                case Cell.HalfLifeJar:
                    GiveHalfTimeBottle();
                    break;

                case Cell.FullLifeJar:
                    GiveFullTimeBottle();
                    break;

                case Cell.HourglassBlue:
                    GiveBlueHourglass();
                    break;

                case Cell.HourglassGold:
                    GiveHourglassGold();
                    break;

                case Cell.QuestionPotion:
                    GiveQuestionPotion();
                    break;

                case Cell.Seal:
                    addInv();
                    break;

                case Cell.Sphinx:
                    Sesh.Score += 500_000;
                    break;

                case Cell.BagG1:
                case Cell.Dollar:
                    Sesh.Score += 10_000;
                    break;

                case Cell.BagG2:
                case Cell.Dollar2:
                    Sesh.Score += 20_000;
                    break;

                case Cell.Crane:
                    Sesh.Score += 100_000;
                    break;

                case Cell.MightyCoin:
                    Sesh.Score += 200_000;
                    break;

                case Cell.KingTut:
                    Sesh.Score += 1_000_000;
                    break;

                case Cell.Vapour:
                    GiveVapour();
                    break;

                case Cell.Rabbit:
                case Cell.Lamp:
                    sound = false;
                    Sesh.Score += 500_000;
                    GiveExtraLife(1, i);
                    break;

                case Cell.RwOneUp:
                case Cell.ExtraLife:
                    sound = false;
                    GiveExtraLife(1, i);
                    break;

                case Cell.ExtraLife5:
                    sound = false;
                    GiveExtraLife(5, i);
                    break;

                case Cell.GoldWing:
                    addInv();
                    break;

                case Cell.LootRedCopper:
                case Cell.Copper:
                    Sesh.Score += 5_100;
                    break;

                case Cell.Opal:
                    Sesh.Score += 9_900;
                    break;

                case Cell.PageSpace:
                    addInv();
                    break;

                case Cell.PageTime:
                    addInv();
                    break;
        
                case Cell.LootRed:
                    Sesh.Score += 10_000;
                    break;

                case Cell.LootRedRedFireballJar:
                case Cell.RedFireballJar:
                    GiveScrollItem(Cell.RedFireballJar);
                    break;

                case Cell.SnowballJar:
                    Sesh.Score += 50_000;
                    GiveScrollItem(Cell.SnowballJar);
                    break;

                case Cell.SuperSnowballJar:
                    Sesh.Score += 100_000;
                    GiveScrollItem(Cell.SuperSnowballJar);
                    break;

                case Cell.BlueUmbrella:
                case Cell.PinkUmbrella:
                    EnterDoor(c, i);
                    break;
            }

            if (sound) Sesh.PickupCount++;
            return Cell.Empty;
        }


        // Collide with solids
        public void DanaCollideLevel(Heading true_up_dir = Heading.None)
        {

            // Collision1 is the left and right solid wall sensors
            base.Collision = CollideLevel(ColSensor.Left | ColSensor.Right,
                                     ColTypes.Solid, 
                                     2, 1);

            // Collision2 is the down or up sensor which doubles as our breakable 
            // ceiling detection so we know which of the two cells above Dana should
            // be targeted
            Collision2 = CollideLevel(Jumping ? ColSensor.Up : ColSensor.Down, 
                                      ColTypes.Solid | ColTypes.Breakable, 
                                      2, 1);


            Heading dir = true_up_dir;


            /* Left */
            if (base.Collision.LeftBlock.HasValue && (Vx < 0 || dir.HasFlag(Heading.Left)))
            {
                X += base.Collision.LeftOverlap;
            }

            /* Right */
            if (base.Collision.RightBlock.HasValue && (Vx > 0 || dir.HasFlag(Heading.Right)))
            {
                X -= base.Collision.RightOverlap;
            }

            /* Down */
            OnFloor = Collision2.DownBlock.HasValue;
            if (OnFloor && (Vy > 0 || dir.HasFlag(Heading.Down)))
            {
                Y -= Collision2.DownOverlap;
                Jumping = false;
                Vy = 0;
                if (Control.Crouch.Down() && Sesh.Apprentice)
                {
                    var dBlock = Level.Layout[Collision2.DownBlock.Value];
                    if (Layout.IsCracked(dBlock))
                    {
                        if (Layout.IsBreakableWithHead(dBlock))
                        {
                            Level.Layout.HeadHit(Collision2.DownBlock.Value);
                        }
                        HeadTimer = HeadHitTicks;
                        Sound.Head.Play();
                    }  
                    else if (dBlock == Cell.Ash)
                    {
                        HeadTimer = HeadHitTicks;
                        Sound.Head.Play();
                        Level.Layout.CascadeDestroy(Collision2.DownBlock.Value, dBlock, Animation.BlockCrack);
                    }
                    else if (Layout.IsBreakable(dBlock))
                    {
                        HeadTimer = HeadHitTicks;
                        Sound.Head.Play();
                        Level.Layout[Collision2.DownBlock.Value] = dBlock.ToCracked();
                    }
                }
            }

            /* Up */
            if (Collision2.SolidBlock.HasValue && (Vy < 0 || dir.HasFlag(Heading.Up)))
            {

                var ceil = ((Collision2.SolidBlock.Value.Y + 1) * Game.NativeTileSize.Y);
                if (Y < ceil)
                {
                    HeadCell = Collision2.SolidBlock.Value;
                    HeadTimer = HeadHitTicks;
                    Sound.Head.Play();
                    // Prioritize breakable blocks over concrete ones
                    if (Collision2.BreakableBlock.HasValue)
                    {
                        Level.Layout.HeadHit(Collision2.BreakableBlock.Value);
                    } else if (base.Collision.SolidBlock.HasValue)
                    {
                        Level.Layout.HeadHit(Collision2.SolidBlock.Value);
                    }
                    Vy = 0;
                    Animation = AnimHead;
                    Jumping = false;
                    JumpStep = 0;
                    Falling = true;
                    Y -= base.Collision.UpOverlap;
                }
                else
                {
                    HeadCell = default;
                }
            }
            else
            {
                HeadCell = default;
            }


        }

        /// <summary>
        /// Called when it's time to render Dana/Adam
        /// </summary>
        /// <param name="batch"></param>
        public override void Render(SpriteBatch batch)
        {
            switch(Level.State)
            {
                case LevelState.Dying:
                case LevelState.TimeOver:
                case LevelState.TimeOver2:
                    Animation = AnimDead;           // For these Dana/Adam is just dead
                    break;

                case LevelState.Dead:
                    Animation = AnimDeadDead;       // And now he's dead-dead
                    break;

                case LevelState.Running:
                    // This has to go here because Dana's ProjectedHurtBox is different
                    // from GameObject.ProjectedHurtBox.
                    if (Game.ShowCollision)
                    {
                        batch.DrawRectangle(ProjectedHurtBox, Color.Red, 1, 0);
                    }
                    // Draw Dana/Adam normally
                    break;

                case LevelState.Edit:
                case LevelState.KeyStars:
                case LevelState.OpeningDoor:
                case LevelState.PreRun:
                case LevelState.EndingA:
                    // Draw Dana/Adam normally
                    break;

                default:
                    // Don't draw Dana/Adam
                    return;
            }

            // Normal GameObject rendering (based on this.Animation)
            base.Render(batch);
        }

        /// <summary>
        /// Called once per Tick to handle Dana's controls, collision, movement, and other
        /// gameplay logic. 
        /// </summary>
        public override void Update(GameTime gameTime)
        {
            switch(State)
            {
                case DanaState.Warping:
                    if (Level.State != LevelState.Warping) State = DanaState.Control;
                    return;

                case DanaState.Dead:
                    DanaDying();
                    return;
                
                case DanaState.Control:
                    if (Level.State == LevelState.Running || Level.State == LevelState.TitleScreen)
                    {
                        // First we give control to Controls which handles Dana's input,  then if it 
                        // returns 'false', we go ahead and do the collision.  
                        if (Controls()) break;

                        // If Controls() returns true it doesn't want us to do anything else this 
                        // Update (and if it does it will do it itself).
                        Collision();                                                                     
                    }
                    break;
            }

            base.Update(gameTime);
        }

    }

    /// <summary>
    /// Valid states for Dana
    /// </summary>
    public enum DanaState
    {
        Control,
        Dead,
        Warping
    }

}
