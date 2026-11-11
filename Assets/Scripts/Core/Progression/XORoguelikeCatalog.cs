using System.Collections.Generic;

namespace XOPlayable.Core
{
    public static class XORoguelikeCatalog
    {
        public static readonly Dictionary<CaroRelicType, CaroRelic> RelicDatabase = new Dictionary<CaroRelicType, CaroRelic>
        {
            {
                CaroRelicType.DiagonalPrism,
                new CaroRelic
                {
                    type = CaroRelicType.DiagonalPrism,
                    id = "relic_diag_prism",
                    name = "Prism Lens",
                    description = "5-in-a-row diagonal wins grant +50% bonus Coins & +1 free Undo!",
                    rarity = CaroRelicRarity.Rare,
                    cost = 45
                }
            },
            {
                CaroRelicType.TwinThreat,
                new CaroRelic
                {
                    type = CaroRelicType.TwinThreat,
                    id = "relic_twin_threat",
                    name = "Twin Threat",
                    description = "Creating an Open-3 instantly grants +1 extra turn! (Once per match)",
                    rarity = CaroRelicRarity.Uncommon,
                    cost = 40
                }
            },
            {
                CaroRelicType.KnightStep,
                new CaroRelic
                {
                    type = CaroRelicType.KnightStep,
                    id = "relic_knight_step",
                    name = "Dragon Edge",
                    description = "Creating an Open-4 allows you to make an immediate follow-up move!",
                    rarity = CaroRelicRarity.Uncommon,
                    cost = 40
                }
            },
            {
                CaroRelicType.GuardianAegis,
                new CaroRelic
                {
                    type = CaroRelicType.GuardianAegis,
                    id = "relic_guardian_aegis",
                    name = "Guardian Aegis",
                    description = "When Boss threatens a winning 5, automatically freezes that threat cell for 2 turns!",
                    rarity = CaroRelicRarity.Rare,
                    cost = 55
                }
            },
            {
                CaroRelicType.AlchemistStone,
                new CaroRelic
                {
                    type = CaroRelicType.AlchemistStone,
                    id = "relic_alchemist_stone",
                    name = "Alchemist Core",
                    description = "Every 4 pieces you place automatically generates +10 bonus Coins!",
                    rarity = CaroRelicRarity.Common,
                    cost = 30
                }
            },
            {
                CaroRelicType.MinePlanter,
                new CaroRelic
                {
                    type = CaroRelicType.MinePlanter,
                    id = "relic_mine_planter",
                    name = "Mine Planter",
                    description = "Plants 2 hidden landmines each match. Any Boss piece stepping on them explodes!",
                    rarity = CaroRelicRarity.Common,
                    cost = 30
                }
            },
            {
                CaroRelicType.ChronoPocket,
                new CaroRelic
                {
                    type = CaroRelicType.ChronoPocket,
                    id = "relic_chrono_pocket",
                    name = "Chrono Pocket",
                    description = "Grants +2 free Undos at the start of every tower floor!",
                    rarity = CaroRelicRarity.Common,
                    cost = 25
                }
            },
            {
                CaroRelicType.BountyHunter,
                new CaroRelic
                {
                    type = CaroRelicType.BountyHunter,
                    id = "relic_bounty_hunter",
                    name = "Oracle Compass",
                    description = "Special tiles award double Coins & highlights optimal candidate moves!",
                    rarity = CaroRelicRarity.Common,
                    cost = 25
                }
            },
            {
                CaroRelicType.ShatterHammer,
                new CaroRelic
                {
                    type = CaroRelicType.ShatterHammer,
                    id = "relic_shatter_hammer",
                    name = "Rune Breaker",
                    description = "Once per match, placing a piece adjacent to an enemy piece shatters & destroys it!",
                    rarity = CaroRelicRarity.Rare,
                    cost = 50
                }
            },
            {
                CaroRelicType.Telescope,
                new CaroRelic
                {
                    type = CaroRelicType.Telescope,
                    id = "relic_telescope",
                    name = "Hawk Telescope",
                    description = "Reveals all hidden mines and warp portals across the board on move 1!",
                    rarity = CaroRelicRarity.Uncommon,
                    cost = 35
                }
            },
            {
                CaroRelicType.VampireFang,
                new CaroRelic
                {
                    type = CaroRelicType.VampireFang,
                    id = "relic_phoenix_sigil",
                    name = "Phoenix Sigil",
                    description = "Lethal Save: If Boss is about to win, sacred flames incinerate their piece and saves your run!",
                    rarity = CaroRelicRarity.Legendary,
                    cost = 70
                }
            },
            {
                CaroRelicType.CrownOfOmni,
                new CaroRelic
                {
                    type = CaroRelicType.CrownOfOmni,
                    id = "relic_crown_omni",
                    name = "Overlord Crown",
                    description = "Reduces Boss AI difficulty tier by 1 and grants +30 bonus Coins per win!",
                    rarity = CaroRelicRarity.Legendary,
                    cost = 80
                }
            }
        };

        public static readonly TowerFloorBlind[] FloorBlinds = new TowerFloorBlind[]
        {
            new TowerFloorBlind
            {
                floorNumber = 1,
                floorTitle = "Floor 1 - Verdant Plains",
                mapName = "Verdant Plains",
                mapThemeId = "plains",
                mapAccentColor = XOThemeDatabase.Hex("#4ED6A4"),
                bossName = "Apprentice Nova",
                bossTitle = "Novice Challenger",
                bossElo = 650,
                difficulty = AIDifficulty.Easy,
                botEquipment = BotEquipmentType.None,
                blindType = BossBlindType.TheApprentice,
                blindName = "Pure 15x15 Caro",
                blindDescription = "Classic 15x15 Gomoku. Connect 5 pieces in a row to win!",
                maxPlayerMoves = 0,
                boardSize = 15,
                coinReward = 25,
                variantRule = XOVariantRule.ClassicCaro
            },
            new TowerFloorBlind
            {
                floorNumber = 2,
                floorTitle = "Floor 2 - Jungle Minefield",
                mapName = "Jungle Minefield",
                mapThemeId = "jungle",
                mapAccentColor = XOThemeDatabase.Hex("#10B981"),
                bossName = "Sentinel Jax",
                bossTitle = "Jungle Warden",
                bossElo = 800,
                difficulty = AIDifficulty.Easy,
                botEquipment = BotEquipmentType.None,
                blindType = BossBlindType.ChaosMinefield,
                blindName = "Blast Mines & Warp Portals",
                blindDescription = "15x15 board seeded with 3 hidden landmines & 2 warp portals!",
                maxPlayerMoves = 0,
                boardSize = 15,
                coinReward = 30,
                variantRule = XOVariantRule.ChaosMinefield
            },
            new TowerFloorBlind
            {
                floorNumber = 3,
                floorTitle = "Floor 3 - Glacial Chasm",
                mapName = "Glacial Chasm",
                mapThemeId = "frost",
                mapAccentColor = XOThemeDatabase.Hex("#0EA5E9"),
                bossName = "Cryo Knight Freya",
                bossTitle = "Frost Knight",
                bossElo = 950,
                difficulty = AIDifficulty.Normal,
                botEquipment = BotEquipmentType.FrostStaff,
                blindType = BossBlindType.TheNeedle,
                blindName = "The Needle (Frost Staff)",
                blindDescription = "Win in 25 moves! Boss equips Frost Staff to freeze your open threat cells.",
                maxPlayerMoves = 25,
                boardSize = 15,
                coinReward = 35,
                variantRule = XOVariantRule.ClassicCaro
            },
            new TowerFloorBlind
            {
                floorNumber = 4,
                floorTitle = "Floor 4 - The Abyssal Void",
                mapName = "The Abyssal Void",
                mapThemeId = "void",
                mapAccentColor = XOThemeDatabase.Hex("#8B5CF6"),
                bossName = "Ironclad Boris",
                bossTitle = "Impenetrable Bastion",
                bossElo = 1100,
                difficulty = AIDifficulty.Normal,
                botEquipment = BotEquipmentType.MirrorAegis,
                blindType = BossBlindType.TheVoid,
                blindName = "The Void (3x3 Dead Zone)",
                blindDescription = "Center 3x3 grid is a black hole! Boss has Mirror Aegis (skill immunity).",
                maxPlayerMoves = 0,
                boardSize = 15,
                coinReward = 40,
                variantRule = XOVariantRule.ClassicCaro
            },
            new TowerFloorBlind
            {
                floorNumber = 5,
                floorTitle = "Floor 5 - Pyro Volcano",
                mapName = "Pyro Volcano",
                mapThemeId = "volcano",
                mapAccentColor = XOThemeDatabase.Hex("#EF4444"),
                bossName = "Pyro Berserker Ignis",
                bossTitle = "Fire Dragon Champion",
                bossElo = 1250,
                difficulty = AIDifficulty.Hard,
                botEquipment = BotEquipmentType.FlameBlade,
                blindType = BossBlindType.TheArm,
                blindName = "Strict Gomoku Seal (The Arm)",
                blindDescription = "Turn 10 Mutation: Double-blocked 5-in-a-row does NOT win! Must win with open ends.",
                maxPlayerMoves = 0,
                boardSize = 15,
                coinReward = 45,
                variantRule = XOVariantRule.ClassicCaro,
                hasMidMatchShift = true,
                midMatchShiftTurn = 10,
                midMatchShiftTitle = "MUTATION: GOMOKU SEAL ACTIVATED!",
                midMatchShiftDesc = "Seal engaged! Double-blocked 5-in-a-row does NOT win! Only open ends count!"
            },
            new TowerFloorBlind
            {
                floorNumber = 6,
                floorTitle = "Floor 6 - Gravity Abyss",
                mapName = "Gravity Abyss",
                mapThemeId = "gravity",
                mapAccentColor = XOThemeDatabase.Hex("#F97316"),
                bossName = "Thornguard Vane",
                bossTitle = "Abyssal Warlord",
                bossElo = 1400,
                difficulty = AIDifficulty.Hard,
                botEquipment = BotEquipmentType.ThornArmor,
                blindType = BossBlindType.GravityDrop,
                blindName = "Gravity Drop (15x15)",
                blindDescription = "15x15 board. Pieces drop to lowest empty column cell! Connect 5 in a row to win!",
                maxPlayerMoves = 0,
                boardSize = 15,
                coinReward = 50,
                variantRule = XOVariantRule.GravityDrop
            },
            new TowerFloorBlind
            {
                floorNumber = 7,
                floorTitle = "Floor 7 - Gobblet Colosseum",
                mapName = "Gobblet Colosseum",
                mapThemeId = "gobblet",
                mapAccentColor = XOThemeDatabase.Hex("#3B82F6"),
                bossName = "Thunder Archon Raiden",
                bossTitle = "Thunder God",
                bossElo = 1550,
                difficulty = AIDifficulty.Hard,
                botEquipment = BotEquipmentType.ThunderHammer,
                blindType = BossBlindType.GobbletStack,
                blindName = "Gobblet Stack (15x15)",
                blindDescription = "15x15 board. Pieces stack up to Lv.3 & devour smaller pieces! Connect 5 to win!",
                maxPlayerMoves = 0,
                boardSize = 15,
                coinReward = 60,
                variantRule = XOVariantRule.GobbletStack
            },
            new TowerFloorBlind
            {
                floorNumber = 8,
                floorTitle = "Floor 8 - Twin Dragon Mountain",
                mapName = "Twin Dragon Mountain",
                mapThemeId = "dragon",
                mapAccentColor = XOThemeDatabase.Hex("#14B8A6"),
                bossName = "Shadowblade Kage",
                bossTitle = "Shadow Assassin",
                bossElo = 1750,
                difficulty = AIDifficulty.Hard,
                botEquipment = BotEquipmentType.FlameBlade,
                blindType = BossBlindType.TwinDragon,
                blindName = "Twin Dragon Awakening (2x5)",
                blindDescription = "Turn 10 Mutation: Boss summons twin dragons! You must build TWO 5-in-a-rows to win!",
                maxPlayerMoves = 0,
                boardSize = 15,
                coinReward = 70,
                variantRule = XOVariantRule.ClassicCaro,
                hasMidMatchShift = true,
                midMatchShiftTurn = 10,
                midMatchShiftTitle = "MUTATION: TWIN DRAGON AWAKENED!",
                midMatchShiftDesc = "Second dragon summoned! You must complete TWO separate 5-in-a-rows to win!"
            },
            new TowerFloorBlind
            {
                floorNumber = 9,
                floorTitle = "Floor 9 - Mist Fog Citadel",
                mapName = "Mist Fog Citadel",
                mapThemeId = "mist",
                mapAccentColor = XOThemeDatabase.Hex("#A855F7"),
                bossName = "Frost Empress Sylvan",
                bossTitle = "Eternal Frost Queen",
                bossElo = 1950,
                difficulty = AIDifficulty.Hard,
                botEquipment = BotEquipmentType.FrostStaff,
                blindType = BossBlindType.FogOfWar,
                blindName = "Fog of War & Blizzard",
                blindDescription = "Enemy pieces shrouded until you play adjacent. Turn 12 Blizzard freezes 3 cells!",
                maxPlayerMoves = 0,
                boardSize = 15,
                coinReward = 85,
                variantRule = XOVariantRule.ClassicCaro,
                hasMidMatchShift = true,
                midMatchShiftTurn = 12,
                midMatchShiftTitle = "MUTATION: BLIZZARD STRUCK!",
                midMatchShiftDesc = "Blizzard freezes 3 tactical cells for 2 turns! Fog of War remains active!"
            },
            new TowerFloorBlind
            {
                floorNumber = 10,
                floorTitle = "Floor 10 - Overlord Celestial Summit",
                mapName = "Overlord Celestial Summit",
                mapThemeId = "apex",
                mapAccentColor = XOThemeDatabase.Hex("#F59E0B"),
                bossName = "Overlord Rex",
                bossTitle = "The Almighty",
                bossElo = 2200,
                difficulty = AIDifficulty.Hard,
                botEquipment = BotEquipmentType.OmniCrown,
                blindType = BossBlindType.OverlordApex,
                blindName = "Overlord Apex (2x5 & Collapsing Arena)",
                blindDescription = "APEX FINALE: Connect TWO 5-in-a-rows within 30 moves on an inward collapsing board!",
                maxPlayerMoves = 30,
                boardSize = 15,
                coinReward = 120,
                variantRule = XOVariantRule.ClassicCaro,
                requiredWinLines = 2,
                hasCollapsingPerimeter = true
            }
        };

        public static CaroRelic GetRelicData(CaroRelicType type)
            => RelicDatabase.TryGetValue(type, out CaroRelic r) ? r : null;

        public static TowerFloorBlind GetFloorBlind(int floorNumber)
            => FloorBlinds[XOMath.Clamp(floorNumber - 1, 0, FloorBlinds.Length - 1)];
    }
}

