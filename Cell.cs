using System;
using System.Collections.Generic;
using System.Text;

namespace SKX
{

    /// <summary>
    /// Valid values for a foreground layout Cell
    /// </summary>
    public enum Cell
    {
        /* Modifier values; OR these with an Item Value
         * but do not use more than one modifier at a time */
        Covered = 0x1000,
        Hidden = 0x2000,
        Cracked = 0x4000,
        Frozen = 0x8000,
        TempBlock = 0x10000,

        // Default value for void space
        Default = 1,

        /* All of these items map 1-to-1 with the same value in the Tile enum
         * (e.g. (Tile)cell will give you the tile to draw */
        Empty = 0,          // Nothing
        Concrete,           // Gray block       
        Dirt,               // Tan block
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
        ExplosionJar,
        Seal,
        Sphinx,
        Dollar,
        Dollar2,

        Crane,
        MightyCoin,
        KingTut,
        Lamp,
        ExtraLife,
        ExtraLife5,
        GoldWing,
        Copper,
        Opal,
        BlockCracked,
        PageSpace,
        PageTime,
        ShrineAries,            // Constellation symbols
        ShrineTaurus,
        ShrineGemini,
        ShrineCancer,

        ShrineSolomon = 61,
        HourglassPurple = 80,
        QuestionPotion,
        Rabbit,
        RabbitGray,
        LootRed = 95,
        BrickLargeA,        // Border blocks
        BrickLargeB,
        BrickLargeC,
        BrickLargeD,

        ShrineLeo = 104,      // More zodiac
        ShrineVirgo,
        ShrineLibra,
        ShrineScorpio,
        ShrineSagittarius,
        ShrineCapricorn,
        ShrineAquarius,
        ShrinePisces,

        RedFireballJar,
        RedJar,
        BlackJar,
        GreenJar,
        SnowballJar,
        SuperSnowballJar,
        Ash,
        DarkDoorClosed,
        DarkDoorOpen,
        BlueJar = 207,
        CrystalRed = 218,
        DoorBlue = 240,
        DoorOpenBlue = 241,
        SolBook = 289,
        SolBookOpenA,
        SolBookOpenB,
        ShrineMercury,
        ShrineVenus,
        ShrineEarth,
        ShrineMars,
        ShrineJupiter,
        ShrineSaturn,
        ShrineUranus,
        ShrineNeptune,
        ShrinePluto,
        ShrineMoon,
        ShrineSun,
        SealJuno,
        SealVesta,
        SealHygeia,
        SealChiron,
        SealComet,
        SealUnity,
        SealAmtoudi,
        SealEris,
        Cartouche1,
        CartoucheRamesees,
        CartoucheRocco,
        Cartouche4,
        SealSedna,
        CartoucheLumania,
        Cartouche6,
        BagW1 = 263, BagW2, BagW5, BagR1, BagR2, BagR5, BagG1, BagG2, 
        BagG5, RwOneUp, SpellUpgrade, RwBell, RwScroll, RwCrystal,
        Wick = 321, Tiles, Grass, Vapour,
        PuzzleUnsolved = 329,
        PuzzleSolved = 330,
        ShrineSesquiquadrate = 346,
        SealCosmos = 347,
        ToggleBlock = 351,
        IceCream = 387, 
        BluePop = 389, 
        GreenPop = 388,
        PinkUmbrella = 368, 
        BlueUmbrella,
        BlueBell,
        BobbleGround,
        YellowJar = 403,
        PurpleJar,
        DarkBlueJar,

        /* Items below don't have a corresponding tile in the Tile enum with
         * the same value.  Use Layout.CellToTile(t) to get the proper graphic */

        LootBlueFireballJar = 0x200,
        LootBlueGold2,
        LootBlueCrystalGold,

        LootGoldSuperFireballJar,
        LootGoldScroll,
        LootGoldBell,

        LootRedRedFireballJar,
        LootRedCrystalRed,
        LootRedCopper,

        InvisibleBlock,
        FakeConcrete,
        RabbitHidden,
        InvisibleDoor,


    }

}
