using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Media;
using System;
using System.Collections.Generic;
using System.Text;

namespace SKX
{
    /// <summary>
    /// Class used as a manager to handle loaded assets
    /// </summary>
    public class Assets
    {
        /// <summary>
        /// Reference to the Game
        /// </summary>
        public Game Game { get; } 

        /// <summary>
        /// Texture containing the game graphics
        /// </summary>
        public Texture2D Blocks;

        /// <summary>
        /// Texture containing the game text
        /// </summary>
        public Texture2D Text;

        /// <summary>Time over flashing effect</summary>
        public Effect TimeOver;
        /// <summary>Paused grayscale effect</summary>
        public Effect Pause;
        /// <summary>Palette swap effect</summary>
        public Effect Swap;
        /// <summary>Vaporwave background effect</summary>
        public Effect Vapour;
        /// <summary>Character map of source rectangles for the text</summary>
        public Dictionary<char, Rectangle> CharMap = new Dictionary<char, Rectangle>();
        public Bundle Bundle;

        /// <summary>Creates a new Assets</summary>
        public Assets(Game game)
        {
            Game = game;
        }

       
        /// <summary>
        /// Loads all the Assets -- probably only ever call this once
        /// </summary>
        public void Load()
        {
            // Load textures
            Blocks = Game.Content.Load<Texture2D>("blocks");
            Text = Game.Content.Load<Texture2D>("text");

            // Load shaders
            TimeOver = Game.Content.Load<Effect>("timeover");
            Pause = Game.Content.Load<Effect>("pausefx");
            Swap = Game.Content.Load<Effect>("swap");
            Vapour = Game.Content.Load<Effect>("vapour");

            try
            {

                string json = @"SKX.Content.cspace.bndl".GetResource();
                if (json != null)
                {
                    Bundle = Bundle.Load(json);
                }
            } 
            catch {  }
            if (Bundle is null)
            {
                Game.StatusMessage("FAILED TO LOAD BUNDLE");
            }

            // Build the font character map
            BuildFont();

            // The sound assets are handled in the Sound manager
            Sound.LoadSounds(Game);
        }

        /// <summary>
        /// Finds the source rectangle for a specific tile
        /// </summary>
        public Rectangle BlockSourceTileRect(Tile tile)
        {
            int tx = (int)tile % 16;
            int ty = (int)tile / 16;

            return new Rectangle(new Point(tx * Game.NativeTileSize.X, 
                                           ty * Game.NativeTileSize.Y), Game.NativeTileSize);
        }

        /// <summary>
        /// Finds the source rectangle for a piece of the scroll's HUD graphics
        /// </summary>
        public Rectangle ScrollSourceTileRect(byte offset)
        {
            int tx = offset * 8;
            int ty = 48;

            return new Rectangle(new Point(tx, ty), new Point(8, 16));
        }

        // Build the font mappings based on the characters in the "text" texture.
        void BuildFont()
        {
            CharMap.Clear();        // In case this gets called more than once for some reason

            int size = 8;

            void Char(char c, int x, int y) {
                CharMap.Add(c, new Rectangle(x * size, y * size, size, size));
            }

            int y = 0, x = 0;

            // Row 0 (first row),  0-F
            Char('0', x++, y);
            Char('1', x++, y);
            Char('2', x++, y);
            Char('3', x++, y);
            Char('4', x++, y);
            Char('5', x++, y);
            Char('6', x++, y);
            Char('7', x++, y);
            Char('8', x++, y);
            Char('9', x++, y);
            Char('A', x++, y);
            Char('B', x++, y);
            Char('C', x++, y);
            Char('D', x++, y);
            Char('E', x++, y);
            Char('F', x++, y);

            y = 1; x = 0;

            // Row 1 (2nd row), G-V
            Char('G', x++, y);
            Char('H', x++, y);
            Char('I', x++, y);
            Char('J', x++, y);
            Char('K', x++, y);
            Char('L', x++, y);
            Char('M', x++, y);
            Char('N', x++, y);
            Char('O', x++, y);
            Char('P', x++, y);
            Char('Q', x++, y);
            Char('R', x++, y);
            Char('S', x++, y);
            Char('T', x++, y);
            Char('U', x++, y);
            Char('V', x++, y);

            
            y = 2; x = 0;

            // Row 2 (3rd row), X-hyphen
            Char('W', x++, y);
            Char('X', x++, y);
            Char('Y', x++, y);
            Char('Z', x++, y);
            Char(' ', x++, y);
            Char(',', x++, y);
            Char('™', x++, y);
            Char('!', x++, y);
            Char('"', x++, y);
            Char('.', x++, y);
            Char('c', x++, y);
            Char('h', x++, y);
            Char('d', x++, y);
            Char('f', x++, y);
            Char('x', x++, y);
            Char('-', x++, y);

            y = 3; x = 0;

            // Row 3 (4th row), 
            Char('[', x++, y);
            Char(']', x++, y);
            Char('a', x++, y);
            Char('}', x++, y);
            Char('*', x++, y);
            Char('|', x++, y);
            Char('t', x++, y);
            Char(':', x++, y);
            Char('k', x++, y);
            Char('r', x++, y);
            x++; // Unused
            x++; // Unused
            x++; // Unused
            Char('(', x++, y);
            Char(')', x++, y);
            Char('{', x++, y);

            y = 4; x = 0;
            // Row 4 (5th row), `-?
            Char('`', x++, y);
            Char('\'', x++, y);
            Char(';', x++, y);
            Char('/', x++, y);
            Char('_', x++, y);
            Char('w', x++, y);
            Char('>', x++, y);
            Char('+', x, y);        // 'p' and '+' are the same
            Char('p', x++, y);
            Char('?', x++, y);

        }

        public const string ValidChars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789 :.-+?,!™_'`;+[]";

        /// <summary>
        /// Strips out characters we don't have in our character set
        /// </summary>
        public static string SafeString(string x)
        {
            // Handle null
            if (x is null) return null;

            // Letters to uppercase
            x = x.ToUpper();

            // Allocate buffer
            var sb = new StringBuilder(x.Length);

            foreach(var c in x)
            {
                if (ValidChars.Contains(c)) sb.Append(c);
            }

            return sb.ToString();

        }

    }

    /// <summary>
    /// Represents a single 16x16 tile (or whatever Game.NativeTileSize is set to) in the game graphics
    /// (Assets.Blocks texture)
    /// </summary>
    public enum Tile
    {
        // Used in animation scripts.  Must use bits in 0xFFFF0000 range to avoid being displayed.

        AnimationEnd = -1,      // Signals that the animation has ended and that the object
                                // needs to do something else now.  Causes GameObject.OnAnimationEnded()
                                // to be raised, and the last tile displayed will be used by the 
                                // GameObject renderer.

        // Bitwise OR these with a frame to execute them
        FlipXon = 0x10000,      // Turn on H mirror
        FlipYon = 0x20000,      // Turn on V mirror
        FlipXoff = 0x40000,     // Turn off H mirror
        FlipYoff = 0x80000,     // Turn off V mirror

        // Row 0
        Empty = 0,              // Blank tile
        Concrete,
        BrickTan,
        DoorClosed,
        DoorOpen,
        Mirror,
        Bat,
        Key,
        LootBlue,
        LootGold,
        Gold,
        Gold2,
        CoinGold,
        CoinGold2,
        CrystalGold,
        SuperFireballJar,

        // Row 1
        Scroll,
        Bell,
        Silver,
        Silver2,
        CoinBlue,
        CrystalBlue,
        FireballJar,
        HalfLifeJar,
        FullLifeJar,
        HourglassBlue,
        HourglassGold,
        OrangeJar,
        Seal,
        Sphynx,
        Dollar,
        Dollar2,

        // Row 2
        Crane,
        MightyCoin,
        KingTut,
        Lamp,
        ExtraLife,
        ExtraLife2,
        GoldWing,
        Copper,
        Opal,
        BrickCracked,
        PageSpace,
        PageTime,
        ShrineA,
        ShrineB,
        ShrineC,
        ShrineD,

        // Row 3
        Dana,
        Adam,           
        AdamMagic2,     
        AdamDead,        
        Sparkle,
        Sparkle2,
        MagicAttempt,
        Cloud3,
        BrickHollow,
        Cloud,
        Cloud2,
        DanaMagic,
        AdamMagic,      
        ShrineM,
        Fireball,
        Fireball2,

        // Row 4
        AdamJump,       
        AdamJump2,      
        FireballDecay,
        DanaDead,
        Fairy,
        Fairy2,
        AdamHead,       
        DanaRun,
        DanaCrouch,
        AdamRun,        
        AdamCrouch,     
        SalamanderA,
        DanaRun2,
        AdamRun2,       
        DanaRun3,
        AdamRun3,       

        // Row 5
        HourglassPurple,
        QuestionPotion,
        Rabbit,
        RabbitGray,
        Internal,
        TanDust,
        FireballY,
        FireballY2,
        Tongue,
        Tongue2,
        Tongue3,
        SalamanderB,
        SalamanderC,
        Snowball,
        Snowball2,
        LootRed,

        // Row 6
        BrickLargeA,
        BrickLargeB,
        BrickLargeC,
        BrickLargeD,
        FrozenCrackedA,
        FrozenCrackedB,
        HUDFairy,
        CollectFX,
        ShrineE,
        ShrineF,
        ShrineG,
        ShrineH,
        ShrineI,
        ShrineJ,
        ShrineK,
        ShrineL,

        // Row 7
        RedFireballJar,
        RedJar,
        BlackJar,
        GreenJar,
        SnowballJar,
        SuperSnowballJar,
        AshBlock,
        DarkDoorClosed,
        DarkDoorOpen,
        BrickBackground,
        AriesA, AriesB, AriesC, AriesD, AiresE, AiresF,
        
        // Row 8
        StuccoBackground,
        BlockBackground,
        CancerA, CancerB, CancerC, CancerD, CancerE, CancerF,
        GeminiA, GeminiB, GeminiC, GeminiD, GeminiE, GeminiF,
        GhostA, PanelMonstA,

        // Row 9
        AquariusA, AquaraiusB, AquaraiusC, AquaraiusD, AquaraiusE, AquaraiusF,
        ScorpioA, ScorpioB, ScorpioC, ScorpioD, ScorpioE, ScorpioF,
        NuelA, GhostWallA, GhostB, GhostWallB,

        // Row A
        PiscesA, PiscesB, PiscesC, PiscesD, PiscesE, PiscesF,
        TaurusA, TaurusB, TaurusC, TaurusD, TaurusE, TaurusF, 
        NuelB, NuelWallA, DanaJump, DanaJump2,

        // Row B
        LeoA, LeoB, LeoC, LeoD, LeoE, LeoF,
        LibraA, LibraB, LibraC, LibraD, LibraE, LibraF,
        GoblinA, GoblinPunch, GoblinSwing, DanaHead,

        // Row C
        VirgoA, VirgoB, VirgoC, VirgoD, VirgoE, VirgoF,
        SaggitariusA, SaggitariusB, SaggitariusC, SaggitariusD, SaggitariusE, SaggitariusF,
        GoblinB, GoblinC, DanaMagic2, BlueJar,

        // Row D
        CapricornA, CapricornB, CapricornC, CapricornD, CapricornE, CapricornF, FrozenBlock,
        SparkyA, SparkyB, SparkyC, CrystalRed, WyvernFlyA, WyvernFlyB, WyvernWallA, WyvernWallB, FairyA,

        // Row E
        FairyB, DemonA, DemonB, DemonC, DemonD, MightyA, MightyB, BurnA, BurnB, BurnC, BurnAX, BurnBX, BurnCX,
        PrincessA, PrincessB, DoorHalfOpen,

        // Row F
        DoorBlue, DoorOpenBlue, EnemyFireA, EnemyFireB, EnemyFireAY, EnemyFireBY, PanelA, BurnSA, BurnSB, BurnSC, BurnSAX, BurnSBX, BurnSCX,
        PanelAY, PanelB, PanelBY,

        // Row 0x10
        WizardA, WizardB, WizardC, WizardD, WizardE, FxOneUp, FxFiveUp, BagW1, BagW2, BagW5, BagR1, BagR2, BagR5,
        BagG1, BagG2, BagG5,

        // Row 0x11
        RwOneUp, SpellUpgrade, RwBell, RwScroll, RwCrystal, DragonA, DragonB, DragonC, DragonG, GargA, 
        GargB, GargC, GargAttack, Demon2A, Demon2B, Demon2C,

        // Row 0x12
        Demon2D, SolBookClosed, SolBookOpenA, SolBookOpenB,
        ShrineMercury, ShrineVenus, ShrineEarth, ShrineMars,
        ShrineJupiter, ShrineSaturn, ShrineUranus, ShrineNeptune,
        ShrinePluto, ShrineMoon, ShrineSun, ShrineJuno,

        // Row 0x13
        ShrineVesta, ShrineHygeia, ShrineChiron, ShrineComet,
        ShrineSextile, ShrineTrine, ShrineEris, ShrinePeace,
        ShrineCardinal, ShrineRocco, ShrineOpposition, ShrineSedna,
        ShrineMill, ShrineWonder, TongueY, TongueY2, 
        
        // Row 0x14
        TongueY3, Wick, Tiles, Grass, Vapour, BgStars1, BgStars2, BgStars3, BgStars4,
        PuzzleUnsolved, PuzzleSolved, BurnG, BurnGX, BurnSG, BurnSGX, SnowballDecay,

        // Row 0x15
        BurnZ, BurnZX, BurnSZ, BurnSZX, SnowballY, SnowballY2, DanaCrouchWalkA, DanaCrouchWalkB,
        AdamCrouchWalkA, AdamCrouchWalkB, ShrineSesquiquadrate, SealCosmos,
        Droplet, DropletFall, DropletAbsorb, ToggleBlock,

        // Row 0x16
        DropletS, DropletSFall, DropletSAbsorb,
        DropletG, DropletGFall, DropletGAbsorb,
        DropletP, LogoA, LogoB, LogoC, LogoD, LogoE, LogoF, LogoG, LogoH, LogoI,

        // Row 0x17
        PinkUmbrella,
        BlueUmbrella, BlueBell, BobbleGround,
        DropletPFall, DropletPAbsorb, BgStars5, 
        LogoJ, LogoK, LogoL, LogoM, LogoN, LogoO, LogoP, LogoQ, LogoR,

        // Row 0x18
        BgStars6, BgStars7, BgStars8,
        IceCream, GreenPop, BluePop,
        Free0, LogoS, LogoT, LogoU, LogoV, LogoW, LogoX, LogoY, LogoZ, LogoAA,

        // Row 0x19
        SparkyAA, SparkyAB, SparkyAC,
        YellowJar, PurpleJar, DarkBlueJar,
        Free4, LogoAB, LogoAC, LogoAD, LogoAE, LogoAF, LogoAG, LogoAH, LogoAI, LogoAJ, UnusedZZ,

       
        // Remaining rows are large scale graphics and not tiles.

        // Set this to the last usable tile in the background/SFG editors
        LastEditTile = 512

    }
}

