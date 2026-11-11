using System;
using System.Collections.Generic;
using UnityEngine;

namespace XOPlayable.Core
{
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
        CrownOfOmni
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

    public class CaroRelic
    {
        public CaroRelicType type;
        public string id;
        public string name;
        public string description;
        public CaroRelicRarity rarity;
        public int cost;
    }

    public class TowerFloorBlind
    {
        public int floorNumber;
        public string floorTitle;
        public string mapName;
        public string mapThemeId;
        public Color mapAccentColor = Color.white;
        public string bossName;
        public string bossTitle;
        public int bossElo;
        public AIDifficulty difficulty;
        public BotEquipmentType botEquipment;
        public BossBlindType blindType;
        public string blindName;
        public string blindDescription;
        public int maxPlayerMoves; // 0 = unlimited
        public int boardSize;
        public int coinReward;
        public XOVariantRule variantRule;
        public int requiredWinLines = 1; // 1 = standard 5-in-a-row; 2 = Twin 5 (2x5 to win!)
        public bool hasCollapsingPerimeter = false; // Arena perimeter crumbles into rubble!
        public bool hasCosmicShift = false; // Thunder strikes empty cells!
        public bool hasMidMatchShift = false; // Dynamic surprise mid-match rule or mode twist!
        public int midMatchShiftTurn = 0; // Turn/move at which surprise twist triggers (e.g. 10 or 12)
        public string midMatchShiftTitle = ""; // Title of surprise shift banner
        public string midMatchShiftDesc = ""; // Description shown when triggered
    }

    public static class XORoguelikeRunManager
    {
        public const int MaxLives = 1;
        public const int MaxRelicSlots = 4;

        public static int CurrentRunSeed
        {
            get
            {
                int s = SafePrefs.GetInt("XO_Rogue_RunSeed", 0);
                if (s == 0)
                {
                    s = XORandom.Range(10000, 99999);
                    SafePrefs.SetInt("XO_Rogue_RunSeed", s);
                    SafePrefs.Save();
                }
                return s;
            }
            set
            {
                SafePrefs.SetInt("XO_Rogue_RunSeed", value);
                SafePrefs.Save();
            }
        }

        public static string FormattedSeed => $"#{CurrentRunSeed}";

        public static int Lives
        {
            get => SafePrefs.GetInt("XO_Rogue_Lives", MaxLives);
            set => SafePrefs.SetInt("XO_Rogue_Lives", Mathf.Clamp(value, 0, MaxLives));
        }

        public static int CurrentFloor
        {
            get => SafePrefs.GetInt("XO_Rogue_Floor", 1);
            set => SafePrefs.SetInt("XO_Rogue_Floor", Mathf.Clamp(value, 1, 10));
        }

        public static bool IsRunActive
        {
            get => SafePrefs.GetInt("XO_Rogue_Active", 0) == 1;
            set => SafePrefs.SetInt("XO_Rogue_Active", value ? 1 : 0);
        }

        public static int RunTotalCoinsEarned
        {
            get => SafePrefs.GetInt("XO_Rogue_CoinsEarned", 0);
            set => SafePrefs.SetInt("XO_Rogue_CoinsEarned", value);
        }

        // Equipped Relics in current run
        private static List<CaroRelicType> _equippedRelics;

        public static List<CaroRelicType> EquippedRelics
        {
            get
            {
                if (_equippedRelics == null) LoadEquippedRelics();
                return _equippedRelics;
            }
        }

        // Per-match run status
        public static int PlayerMovesThisMatch { get; set; } = 0;
        public static bool TwinThreatTriggeredThisMatch { get; set; } = false;
        public static bool GuardianAegisTriggeredThisMatch { get; set; } = false;
        public static bool ShatterHammerTriggeredThisMatch { get; set; } = false;
        public static bool PhoenixSigilTriggeredThisMatch { get; set; } = false;
        public static CaroRelicType? DisabledRelicThisMatch { get; set; } = null;

        // =====================================================================
        // RELICS DATABASE (12 BALATRO/SHOTGUN-KING STYLE CARO JOKERS)
        // =====================================================================

        private static readonly Dictionary<CaroRelicType, CaroRelic> RelicDatabase = new()
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

        public static CaroRelic GetRelicData(CaroRelicType type)
        {
            return RelicDatabase.TryGetValue(type, out CaroRelic r) ? r : null;
        }

        // =====================================================================
        // 10 TOWER FLOOR BLINDS DATABASE (BALATRO BOSS BLINDS STYLE)
        // =====================================================================

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

        public static TowerFloorBlind GetActiveBlind()
        {
            int idx = Mathf.Clamp(CurrentFloor - 1, 0, FloorBlinds.Length - 1);
            return FloorBlinds[idx];
        }

        // =====================================================================
        // RUN LIFECYCLE & DRAFTING LOGIC
        // =====================================================================

        public static void StartNewRun(int? customSeed = null)
        {
            CurrentRunSeed = (customSeed.HasValue && customSeed.Value != 0) ? customSeed.Value : XORandom.Range(10000, 99999);
            XORandom.SetSeed(CurrentRunSeed); // every random roll of the run (mines, portals, relic procs, bot variance) now derives from the shown seed
            CurrentFloor = 1;
            Lives = MaxLives;
            IsRunActive = true;
            RunTotalCoinsEarned = 0;
            EquippedRelics.Clear();
            SaveEquippedRelics();
            ResetMatchState();
        }

        public static void ResetMatchState()
        {
            PlayerMovesThisMatch = 0;
            TwinThreatTriggeredThisMatch = false;
            GuardianAegisTriggeredThisMatch = false;
            ShatterHammerTriggeredThisMatch = false;
            PhoenixSigilTriggeredThisMatch = false;
            DisabledRelicThisMatch = null;

            // Handle The Arm Blind (disables 1 random relic)
            TowerFloorBlind blind = GetActiveBlind();
            if (blind.blindType == BossBlindType.TheArm && EquippedRelics.Count > 0)
            {
                int rIdx = XORandom.Range(0, EquippedRelics.Count);
                DisabledRelicThisMatch = EquippedRelics[rIdx];
            }
        }

        public static bool IsTowerMatchActive { get; set; } = false;

        public static bool HasRelic(CaroRelicType type)
        {
            // RELICS ARE 100% EXCLUSIVE TO TOWER CLIMB (ROGUELIKE MODE).
            // Normal game modes (Classic Caro, Vs AI, 2-Player Local, Puzzles, etc.)
            // are strictly pure variants without any relic effects or extra turn gimmicks!
            if (!IsTowerMatchActive)
                return false;

            if (DisabledRelicThisMatch.HasValue && DisabledRelicThisMatch.Value == type)
                return false;

            return EquippedRelics.Contains(type);
        }

        public static bool EquipRelic(CaroRelicType type)
        {
            if (EquippedRelics.Count >= MaxRelicSlots) return false;
            if (EquippedRelics.Contains(type)) return false;

            EquippedRelics.Add(type);
            SaveEquippedRelics();
            return true;
        }

        public static bool SellRelic(CaroRelicType type, out int refundCoins)
        {
            refundCoins = 0;
            if (!EquippedRelics.Contains(type)) return false;

            CaroRelic r = GetRelicData(type);
            refundCoins = (r != null) ? r.cost / 2 : 15;
            EquippedRelics.Remove(type);
            SaveEquippedRelics();
            return true;
        }

        public static bool LoseHeart()
        {
            Lives = Mathf.Max(0, Lives - 1);
            if (Lives <= 0)
            {
                IsRunActive = false;
                CurrentFloor = 1;
                EquippedRelics.Clear();
                SaveEquippedRelics();
                CurrentRunSeed = XORandom.Range(10000, 99999);
                SafePrefs.SetInt("XO_Rogue_Floor", 1);
                SafePrefs.SetInt("XO_Rogue_Lives", 0);
                SafePrefs.SetInt("XO_Rogue_Active", 0);
                SafePrefs.Save();
                return true; // Run Over
            }
            return false;
        }

        public static void RestoreHeart()
        {
            Lives = Mathf.Min(MaxLives, Lives + 1);
        }

        public static List<CaroRelic> GetRandomRelicDraft(int count = 3, int rerollCount = 0)
        {
            List<CaroRelic> pool = new List<CaroRelic>();
            foreach (var kvp in RelicDatabase)
            {
                if (!EquippedRelics.Contains(kvp.Key))
                {
                    pool.Add(kvp.Value);
                }
            }

            // Seeded deterministic draft based on Run Seed, Current Floor & Rerolls
            int seedHash = unchecked(CurrentRunSeed * 7919 + CurrentFloor * 313 + rerollCount * 1013 + 17);
            System.Random seededRnd = new System.Random(seedHash);

            for (int i = 0; i < pool.Count; i++)
            {
                int r = seededRnd.Next(i, pool.Count);
                (pool[i], pool[r]) = (pool[r], pool[i]);
            }

            int pickCount = Mathf.Min(count, pool.Count);
            return pool.GetRange(0, pickCount);
        }

        public static int GetFloorSeed(int floor)
        {
            return unchecked(CurrentRunSeed * 1013 + floor * 701 + 29);
        }

        private static void LoadEquippedRelics()
        {
            _equippedRelics = new List<CaroRelicType>();
            string saved = SafePrefs.GetString("XO_Rogue_EquippedRelics", "");
            if (string.IsNullOrEmpty(saved)) return;

            string[] tokens = saved.Split(',');
            foreach (string t in tokens)
            {
                if (Enum.TryParse(t.Trim(), out CaroRelicType relic))
                {
                    if (!_equippedRelics.Contains(relic) && _equippedRelics.Count < MaxRelicSlots)
                    {
                        _equippedRelics.Add(relic);
                    }
                }
            }
        }

        private static void SaveEquippedRelics()
        {
            List<string> tokens = new List<string>();
            foreach (CaroRelicType r in EquippedRelics)
            {
                tokens.Add(r.ToString());
            }
            SafePrefs.SetString("XO_Rogue_EquippedRelics", string.Join(",", tokens));
            SafePrefs.Save();
        }
    }
}
