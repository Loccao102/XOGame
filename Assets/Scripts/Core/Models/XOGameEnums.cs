namespace XOPlayable.Core
{
    public enum CellPiece
    {
        Empty = 0,
        X = 1,
        O = 2
    }

    public enum XOVariantRule
    {
        ClassicCaro = 0,    // Standard 5-in-a-Row (10x10 or 15x15) with Gold/Lightning/Rock tiles
        Infinite3x3 = 1,    // 3x3 Bolt (3-in-a-Row): Max 3 pieces per side; placing 4th piece vanishes oldest piece!
        GravityDrop = 2,    // 8x8 Connect-4 Gravity (4-in-a-Row): Pieces drop to the lowest empty row in the clicked column!
        StrictCaro = 3,     // 10x10 Blocked-Ends Caro: Exact 5-in-a-row blocked on BOTH ends by enemy does NOT win!
        ChaosMinefield = 4, // 10x10 Minefield & Warp Chaos: Adds Mine tiles (blasts adjacent pieces) & Warp Portals (spawns Twin piece!)
        GobbletStack = 5    // 4x4 Gobblet XO: Each side can Gobble (eat) smaller enemy pieces (Lv.1 -> Lv.2 -> Lv.3)!
    }

    public enum SpecialTileType
    {
        None = 0,
        GoldBonus = 1,          // Grants +20 Coins when claimed
        ExtraTurnLightning = 2, // Grants an immediate Bonus Turn (both Player X and Bot O can claim!)
        RockObstacle = 3,       // Impassable rock obstacle on the board
        HiddenMine = 4,         // Chaos Variant: Blasts up to 2 adjacent pieces around this cell when stepped on!
        WarpPortal = 5          // Chaos Variant: Spawns a Bonus Twin piece on the linked Warp Portal tile!
    }

    public enum GameMode
    {
        VsAI = 0,
        TwoPlayerLocal = 1,
        PuzzleStage = 2,
        TowerClimb = 3
    }

    public enum AIDifficulty
    {
        Easy = 0,
        Normal = 1,
        Hard = 2,
        Boss = 3    // Tower bosses only: search-based AI (VCF + 2-ply), strength scales with boss Elo
    }

    public enum MatchState
    {
        Playing = 0,
        XWon = 1,
        OWon = 2,
        Draw = 3
    }

    public enum BotEquipmentType
    {
        None = 0,
        MirrorAegis = 1,    // Immune to Bomb & Swap skills
        FrostStaff = 2,     // Freezes player's threatening open square for 3 turns
        ThunderHammer = 3,  // Triggers double turn every 5 turns
        ThornArmor = 4,     // Locks adjacent line with thorny obstacle when threatened
        FlameBlade = 5,     // Tactically bombs 1 player threat piece once per match
        OmniCrown = 6       // Supreme dual relic: Cryo Freeze + Double Turn
    }

    public enum BotCounterSkill
    {
        None = 0,
        TileHunter = 1,   // Prioritizes grabbing Gold & Lightning tiles
        CryoFreeze = 2,   // Can freeze 1 dangerous empty tile once per match
        WarlordStrike = 3 // Aggressively builds Double-3 & freezes/counters once per match
    }

    public enum CaroRelicRarity
    {
        Common,
        Uncommon,
        Rare,
        Legendary
    }

    public enum CaroRelicType
    {
        DiagonalPrism,
        TwinThreat,
        KnightStep,
        GuardianAegis,
        AlchemistStone,
        MinePlanter,
        ChronoPocket,
        BountyHunter,
        ShatterHammer,
        Telescope,
        VampireFang,
        CrownOfOmni,
        ThawCharm,      // Boss freezes on you last 1 turn instead of 3
        Counterstrike,  // Blocking a four / fork grants an extra turn (once per match)
        Hourglass       // +6 moves on floors that have a move limit
    }

    public enum BossBlindType
    {
        None = 0,
        TheApprentice = 1,
        ChaosMinefield = 2,
        TheNeedle = 3,
        TheVoid = 4,
        TheArm = 5,
        CollapsingArena = 6,
        StormColosseum = 7,
        TwinDragon = 8,
        FogOfWar = 9,
        OverlordApex = 10,
        GravityDrop = 11,
        GobbletStack = 12
    }
}

