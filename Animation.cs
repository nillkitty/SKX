using System;
using System.Collections.Generic;
using System.Text;

namespace SKX
{
    /// <summary>
    /// Represents an animation script
    /// </summary>
    public struct Animation
    {
        private static int NextAnimID;      // Stores the next available animation ID
        public static List<Animation> Animations = new List<Animation>();

        public Tile[] Frames;               // Frames of the animation (or commands)
        public int Xoffset;                 // Animation-specific X nudge
        public int Yoffset;                 // Animation-specific Y nudge
        private int AnimID;                 // Animation ID (used for comparison)
        public bool Subframe;               // Whether or not the tiles are really 4 smaller 
                                            // 8x8 frames in one 

        public Animation(double speed, params Tile[] frames) : this(speed, 0, 0, frames) { }

        public Animation(double speed, int Xoff, int Yoff, params Tile[] frames) : this(speed, Xoff, Yoff, false, frames) { }

        public Animation(double speed, int Xoff, int Yoff, bool subFrame, params Tile[] frames)
        {
            Xoffset = Xoff;
            Yoffset = Yoff;
            Subframe = subFrame;

            // Compose a flat array we can easily index into to get the current
            // tile
            List<Tile> f = new List<Tile>();
            foreach(var fr in frames)
            {
                if (speed < 1) speed = 1;
                for(int i = 0; i < speed; i++)
                {
                    f.Add(fr);
                }
            }
            Frames = f.ToArray();
            AnimID = ++NextAnimID;
            Animations.Add(this);
        }

        /// <summary>
        /// Gets the current tile for the animation
        /// </summary>
        /// <param name="counter">Animation counter</param>
        /// <param name="FlipX">Flip horizontally flag</param>
        /// <param name="FlipY">Flip vertically flag</param>
        public Tile GetTile(uint counter, ref bool FlipX, ref bool FlipY, ref bool subframe)
        {
            if (Frames is null) return Tile.Empty;          // No frames?
            if (counter == 0) return Frames[0];             // First frame is always first frame
            subframe = Subframe;

            var t = Frames[counter % Frames.Length];        // Index into the frame array

            // Animation script data
            if ((int)t > 0x10000) {
                if (t.HasFlag(Tile.FlipXon)) FlipX = true; 
                if (t.HasFlag(Tile.FlipXoff)) FlipX = false;    
                if (t.HasFlag(Tile.FlipYon)) FlipY = true;
                if (t.HasFlag(Tile.FlipYoff)) FlipY = false;
                t = (Tile)((int)t & 0xFFFF);
            }
            return t;
        }

        /* Allows us to compare animations easily */
        public static bool operator ==(Animation a, Animation b) => a.AnimID == b.AnimID;
        public static bool operator !=(Animation a, Animation b) => a.AnimID != b.AnimID;
        public override bool Equals(object obj) => (obj is Animation a) ? a.AnimID == AnimID : false;
        public override int GetHashCode() => AnimID.GetHashCode();

        /* Generic animations */
        public static Animation Empty = new Animation(2, Tile.Empty);
        public static Animation ShortSparkle = new Animation(2, Tile.Sparkle, Tile.Sparkle2,
                                                        Tile.AnimationEnd);
        public static Animation SparkleLoop = new Animation(2, Tile.Sparkle, Tile.Sparkle2);
        public static Animation BlockBreak = new Animation(1, Tile.Sparkle, Tile.Sparkle2,
                                                        Tile.TanDust, Tile.AnimationEnd);
        public static Animation BlockBreakFast = new Animation(1, Tile.Sparkle, Tile.AnimationEnd);
        public static Animation BlockMake = new Animation(1, Tile.Sparkle, Tile.Sparkle2,
                                                        Tile.MagicAttempt, Tile.AnimationEnd);
        public static Animation BlockCrack= new Animation(1, Tile.Sparkle2, Tile.TanDust,
                                                        Tile.AnimationEnd);
        public static Animation MagicAttempt = new Animation(2, Tile.MagicAttempt, Tile.TanDust,
                                                        Tile.AnimationEnd);
        public static Animation Collect = new Animation(2, Tile.CollectFX,
                                                        Tile.AnimationEnd);
        public static Animation Fireball = new Animation(2, Tile.Fireball,
                                                        Tile.Fireball2);
        public static Animation FireballY = new Animation(2, Tile.FireballY,
                                                        Tile.FireballY2);
        public static Animation FireballDecay = new Animation(2, Tile.FireballDecay, Tile.AnimationEnd);
        public static Animation Snowball = new Animation(2, Tile.Snowball,
                                                        Tile.Snowball2);
        public static Animation SnowballY = new Animation(2, Tile.SnowballY,
                                                        Tile.SnowballY2);
        public static Animation SnowballDecay = new Animation(2, Tile.SnowballDecay, Tile.AnimationEnd);
        public static Animation DropCloud = new Animation(2, Tile.Cloud, Tile.Cloud2, Tile.Cloud3, Tile.AnimationEnd);
        public static Animation SpawnCloud = new Animation(2, Tile.Cloud, Tile.Cloud2, Tile.Cloud3, Tile.Cloud, 
                                                        Tile.Cloud2, Tile.Cloud3, Tile.AnimationEnd);
        public static Animation Sparkle = new Animation(3, Tile.Sparkle, Tile.Sparkle2);
        public static Animation DoorOpen = new Animation(2, Tile.DoorHalfOpen, Tile.DoorOpen, Tile.AnimationEnd);
        public static Animation EnemyFire = new Animation(2, Tile.EnemyFireA,
                                                        Tile.EnemyFireB);
        public static Animation EnemyFireY = new Animation(2, Tile.EnemyFireAY,
                                                        Tile.EnemyFireBY);
        public static Animation EnemyBurned = new Animation(2, Tile.FireballDecay, 
                                                               Tile.BurnSCX, Tile.BurnSCX, Tile.BurnSCX, Tile.BurnSCX);
        public static Animation EnemyFrozen = new Animation(2, Tile.SnowballDecay,
                                                               Tile.BurnSZX, Tile.BurnSZX, Tile.BurnSZX, Tile.BurnSZX);
        public static Animation EnemyBurned2 = new Animation(2, Tile.BurnCX, Tile.BurnC);
        public static Animation EnemyFrozen2 = new Animation(2, Tile.BurnZX, Tile.BurnZ);

        /* Dana */
        public static Animation DanaStand = new Animation(2, Tile.Dana);
        public static Animation DanaCrouch = new Animation(2, Tile.DanaCrouch);
        public static Animation DanaRun = new Animation(1, Tile.DanaRun, Tile.DanaRun2, Tile.DanaRun3);
        public static Animation DanaMagic = new Animation(2, 4, 1, Tile.DanaMagic2, Tile.DanaMagic, Tile.AnimationEnd);
        public static Animation DanaDead = new Animation(2, Tile.DanaDead);
        public static Animation DanaDeadDead = new Animation(2, Tile.CollectFX);
        public static Animation DanaLand = new Animation(4, 0, 1, Tile.DanaJump, Tile.DanaJump, Tile.AnimationEnd);
        public static Animation DanaJump = new Animation(4, 0, 1, Tile.DanaJump, Tile.Dana, Tile.AnimationEnd);
        public static Animation DanaJump2 = new Animation(2, Tile.DanaJump2);
        public static Animation DanaHead = new Animation(2, Tile.DanaHead);
        public static Animation DanaCrouchWalk = new Animation(2, Tile.DanaCrouch, Tile.DanaCrouchWalkA, Tile.DanaCrouchWalkB);

        /* Adam */
        public static Animation AdamStand = new Animation(2, Tile.Adam);
        public static Animation AdamCrouch = new Animation(2, Tile.AdamCrouch);
        public static Animation AdamRun = new Animation(1, Tile.AdamRun, Tile.AdamRun2, Tile.AdamRun3);
        public static Animation AdamMagic = new Animation(2, 4, 1, Tile.AdamMagic2, Tile.AdamMagic, Tile.AnimationEnd);
        public static Animation AdamDead = new Animation(2, Tile.AdamDead);
        public static Animation AdamDeadDead = new Animation(2, Tile.CollectFX);
        public static Animation AdamLand = new Animation(4, 0, 1, Tile.AdamJump, Tile.AdamJump, Tile.AnimationEnd);
        public static Animation AdamJump = new Animation(4, 0, 1, Tile.AdamJump, Tile.Adam, Tile.AnimationEnd);
        public static Animation AdamJump2 = new Animation(2, Tile.AdamJump2);
        public static Animation AdamHead = new Animation(2, Tile.AdamHead);
        public static Animation AdamCrouchWalk = new Animation(2, Tile.AdamCrouch, Tile.AdamCrouchWalkA, Tile.AdamCrouchWalkB);

        /* Ghosts, Wyverns, and Nuels */
        public static Animation GhostFly = new Animation(2, Tile.GhostA, Tile.GhostB);
        public static Animation GhostWall = new Animation(12, Tile.GhostWallB, Tile.GhostWallA);
        public static Animation WyvernFly = new Animation(2, Tile.WyvernFlyA, Tile.WyvernFlyB);
        public static Animation WyvernWall = new Animation(12, Tile.WyvernWallA, Tile.WyvernWallB);
        public static Animation NuelFly = new Animation(2, Tile.NuelA, Tile.NuelB);
        public static Animation NuelWall = new Animation(12, Tile.NuelWallA);

        /* Goblins */
        public static Animation GoblinWalk = new Animation(2, Tile.GoblinA, Tile.GoblinB, Tile.GoblinC);
        public static Animation GoblinFall = new Animation(1, Tile.GoblinA, Tile.GoblinB);
        public static Animation GoblinRun = new Animation(2, Tile.GoblinA, Tile.GoblinB, Tile.GoblinSwing, Tile.GoblinPunch);
        public static Animation GoblinPunch = new Animation(2, Tile.GoblinSwing, Tile.GoblinPunch, 
                                                                Tile.GoblinPunch, Tile.AnimationEnd);
        public static Animation GoblinThink = new Animation(2, Tile.GoblinB, Tile.GoblinB);

        public static Animation WizardWalk = new Animation(2, Tile.WizardA, Tile.WizardB, Tile.WizardC);
        public static Animation WizardFall = new Animation(1, Tile.WizardA, Tile.WizardB);
        public static Animation WizardRun = new Animation(2, Tile.WizardA, Tile.WizardB, Tile.WizardE, Tile.WizardD);
        public static Animation WizardPunch = new Animation(2, Tile.WizardE, Tile.WizardD,
                                                                Tile.WizardD, Tile.AnimationEnd);
        public static Animation WizardThink = new Animation(2, Tile.WizardB, Tile.WizardB);

        /* Dragon */
        public static Animation DragonWalk = new Animation(2, Tile.DragonA, Tile.DragonB, Tile.DragonA, Tile.DragonC);
        public static Animation DragonStand = new Animation(1, Tile.DragonA);
        public static Animation DragonAttack = new Animation(2, Tile.DragonA, Tile.DragonG);
        public static Animation DragonFall = new Animation(1, Tile.DragonA, Tile.DragonC);
        public static Animation Tongue = new Animation(2, Tile.Tongue, Tile.Tongue2, Tile.Tongue, Tile.Tongue3,
                                                          Tile.Tongue2, Tile.AnimationEnd);
        public static Animation TongueY = new Animation(2, Tile.TongueY, Tile.TongueY2, Tile.TongueY, Tile.TongueY3,
                                                          Tile.TongueY2, Tile.AnimationEnd);

        /* Demonhead */
        public static Animation Demonhead = new Animation(2, Tile.DemonA, Tile.DemonB, Tile.DemonC, Tile.DemonD);
        public static Animation DemonheadStill = new Animation(2, Tile.DemonA);
        public static Animation Demonhead2 = new Animation(2, Tile.Demon2A, Tile.Demon2B, Tile.Demon2C, Tile.Demon2D);
        public static Animation Demonhead2Still = new Animation(2, Tile.Demon2A);

        /* Fairy, Mighty Bomb Jack, and the Princess */
        public static Animation Fairy = new Animation(2, Tile.Fairy, Tile.Fairy2);
        public static Animation Princess = new Animation(2, Tile.PrincessA, Tile.PrincessB);
        public static Animation MightyA = new Animation(1, Tile.MightyA);
        public static Animation MightyB = new Animation(1, Tile.MightyB);

        /* Burns */
        public static Animation BurnsBlue = new Animation(1, Tile.BurnB, Tile.BurnAX, Tile.BurnB, Tile.BurnA, Tile.BurnBX, Tile.BurnB, Tile.BurnAX);
        public static Animation BurnsGreen = new Animation(1, Tile.BurnG, Tile.BurnAX, Tile.BurnG, Tile.BurnA, Tile.BurnGX, Tile.BurnG, Tile.BurnAX);
        public static Animation BurnsRed = new Animation(1, Tile.BurnC, Tile.BurnAX, Tile.BurnC, Tile.BurnA, Tile.BurnCX, Tile.BurnC, Tile.BurnAX);
        public static Animation BurnsDarkBlue = new Animation(1, Tile.BurnZ, Tile.BurnAX, Tile.BurnZ, Tile.BurnA, Tile.BurnZX, Tile.BurnZ, Tile.BurnAX);

        public static Animation BurnsBlueS = new Animation(1, Tile.BurnSB, Tile.BurnSAX, Tile.BurnSB, Tile.BurnSA, Tile.BurnSBX, Tile.BurnSB, Tile.BurnSAX);
        public static Animation BurnsGreenS = new Animation(1, Tile.BurnSG, Tile.BurnSAX, Tile.BurnSG, Tile.BurnSA, Tile.BurnSGX, Tile.BurnSG, Tile.BurnSAX);
        public static Animation BurnsRedS = new Animation(1, Tile.BurnSC, Tile.BurnSAX, Tile.BurnSC, Tile.BurnSA, Tile.BurnSCX, Tile.BurnSC, Tile.BurnSAX);
        public static Animation BurnsRainbowS = new Animation(1, Tile.BurnSZ, Tile.BurnSAX, Tile.BurnSZ, Tile.BurnSA, Tile.BurnSZX, Tile.BurnSZ, Tile.BurnSAX);
        
        public static Animation BurnsRedDie = new Animation(1, Tile.BurnC, Tile.BurnSAX, Tile.BurnSC, Tile.BurnA, Tile.BurnCX, Tile.BurnSC, Tile.BurnSAX);

        /* Sparky */
        public static Animation Sparky = new Animation(2, Tile.SparkyA, Tile.SparkyB, Tile.SparkyC, Tile.SparkyB);
        public static Animation SparkyA = new Animation(2, Tile.SparkyAA | Tile.FlipXoff, Tile.SparkyAB, Tile.SparkyAC, 
                                                                 Tile.SparkyAB | Tile.FlipXon, Tile.SparkyAA, Tile.SparkyAC);

        /* Panel Monster */
        public static Animation PanelX = new Animation(2, Tile.PanelA);
        public static Animation PanelShootX = new Animation(1, Tile.PanelB, Tile.PanelA);
        public static Animation PanelY = new Animation(2, Tile.PanelAY);
        public static Animation PanelShootY = new Animation(1, Tile.PanelBY, Tile.PanelAY);

        /* Salamander */
        public static Animation Salamander = new Animation(2, Tile.SalamanderA, Tile.SalamanderC);
        public static Animation SalamanderAttack = new Animation(1, Tile.SalamanderA, Tile.SalamanderB);
        public static Animation SalamanderStill = new Animation(2, Tile.SalamanderA);

        /* Gargoyle */
        public static Animation GargWalk = new Animation(2, 0, 1, Tile.GargA, Tile.GargB);
        public static Animation GargFall = new Animation(1, 0, 1, Tile.GargA, Tile.GargB);
        public static Animation GargStand = new Animation(2, 0, 1, Tile.GargC);
        public static Animation GargAttack = new Animation(1, 0, 1, Tile.GargC, Tile.GargAttack);

        /* Water droplets */
        public static Animation Droplet = new Animation(2, 0, 0, true, Tile.Droplet);
        public static Animation DropletFall = new Animation(2, 0, 0, true, Tile.DropletFall);
        public static Animation DropletAbsorb = new Animation(2, 0, 0, true, Tile.DropletAbsorb);
        /* Frozen droplets */
        public static Animation DropletS = new Animation(2, 0, 0, true, Tile.DropletS);
        public static Animation DropletSFall = new Animation(2, 0, 0, true, Tile.DropletSFall);
        public static Animation DropletSAbsorb = new Animation(2, 0, 0, true, Tile.DropletSAbsorb);
        /* Slime droplets */
        public static Animation DropletG = new Animation(2, 0, 0, true, Tile.DropletG);
        public static Animation DropletGFall = new Animation(2, 0, 0, true, Tile.DropletGFall);
        public static Animation DropletGAbsorb = new Animation(2, 0, 0, true, Tile.DropletGAbsorb);

        /* Pink droplets */
        public static Animation DropletP = new Animation(2, 0, 0, true, Tile.DropletP);
        public static Animation DropletPFall = new Animation(2, 0, 0, true, Tile.DropletPFall);
        public static Animation DropletPAbsorb = new Animation(2, 0, 0, true, Tile.DropletPAbsorb);

    }
}
