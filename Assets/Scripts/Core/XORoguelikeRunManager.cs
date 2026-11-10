using System;
using System.Collections.Generic;
using UnityEngine;
using XOPlayable.Visuals;

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

        public Color GetRarityColor()
        {
            return rarity switch
            {
                CaroRelicRarity.Common => XOProceduralAssets.ColMuted,
                CaroRelicRarity.Uncommon => XOProceduralAssets.ColSky,
                CaroRelicRarity.Rare => XOProceduralAssets.ColLilac,
                CaroRelicRarity.Legendary => XOProceduralAssets.ColSun,
                _ => Color.white
            };
        }

        public Sprite GetIcon()
        {
            return type switch
            {
                CaroRelicType.DiagonalPrism  => XOProceduralAssets.IconRelic,
                CaroRelicType.TwinThreat     => XOProceduralAssets.IconSwords,
                CaroRelicType.KnightStep     => XOProceduralAssets.IconTrophy,
                CaroRelicType.GuardianAegis  => XOProceduralAssets.IconShield,
                CaroRelicType.AlchemistStone => XOProceduralAssets.IconCoin,
                CaroRelicType.MinePlanter    => XOProceduralAssets.IconBomb,
                CaroRelicType.ChronoPocket   => XOProceduralAssets.IconUndo,
                CaroRelicType.BountyHunter   => XOProceduralAssets.IconChest,
                CaroRelicType.ShatterHammer  => XOProceduralAssets.IconRock,
                CaroRelicType.Telescope      => XOProceduralAssets.IconHint,
                CaroRelicType.VampireFang    => XOProceduralAssets.IconHeart,
                CaroRelicType.CrownOfOmni    => XOProceduralAssets.IconCrown,
                _ => XOProceduralAssets.IconRelic
            };
        }
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
                    name = "Kính Quang Phổ (Prism Lens)",
                    description = "Chiến thắng bằng đường chéo 5 quân được thưởng thêm +50% Coins & +1 Undo!",
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
                    name = "Song Sát Ấn (Twin Threat)",
                    description = "Tạo thế cờ 3 mở (Open-3) lập tức nhận 1 nước đi thêm miễn phí! (1 lần/trận)",
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
                    name = "Thiên Long Kiếm (Dragon Edge)",
                    description = "Tạo thế 4 mở (Open-4) cho phép đánh tiếp 1 nước cờ ngay lập tức!",
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
                    name = "Hộ Thân Thuẫn (Guardian Aegis)",
                    description = "Lần đầu tiên Boss đe dọa nối 5 thắng, tự động đóng băng ô nguy hiểm đó 2 lượt!",
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
                    name = "Thạch Giả Kim (Alchemist Core)",
                    description = "Mỗi 4 nước cờ bạn đặt xuống, kho quỹ tự sinh thêm +10 Coins thưởng!",
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
                    name = "Chấn Địa Lôi (Mine Planter)",
                    description = "Rải 2 quả mìn bí mật mỗi trận. Quân cờ của Boss giẫm vào sẽ bị nổ tung!",
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
                    name = "Túi Thời Gian (Chrono Pocket)",
                    description = "Tặng thêm +2 lượt Đi Lại (Undo) miễn phí ở đầu mỗi tầng tháp!",
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
                    name = "La Bàn Tiên Tri (Oracle Compass)",
                    description = "Ăn ô đặc biệt nhận gấp đôi tiền thưởng & tự động chỉ điểm nước đi tối ưu!",
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
                    name = "Búa Phá Thạch (Rune Breaker)",
                    description = "Một lần mỗi trận, đặt cờ cạnh quân Boss sẽ đập vỡ và phá hủy quân đó!",
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
                    name = "Mắt Ưng (Hawk Telescope)",
                    description = "Phát hiện toàn bộ mìn ẩn và cổng dịch chuyển trên bàn cờ ngay từ nước đầu!",
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
                    name = "Lông Vũ Phượng Hoàng (Phoenix Sigil)",
                    description = "Một lần mỗi trận, nếu Boss sắp thắng, lửa thiêng thiêu rụi quân đó và cứu sống run!",
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
                    name = "Vương Miện Đế Vương (Overlord Crown)",
                    description = "Giảm 1 cấp độ thông minh của Boss AI và thưởng thêm +30 Coins mỗi trận thắng!",
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
                floorTitle = "Tầng 1 - Đồng Cỏ Khởi Nguyên",
                mapName = "Đồng Cỏ Khởi Nguyên (Verdant Plains)",
                mapThemeId = "plains",
                mapAccentColor = XOProceduralAssets.ColMint,
                bossName = "Tân Thủ Nova",
                bossTitle = "Tân Thủ Khởi Nghiệp",
                bossElo = 650,
                difficulty = AIDifficulty.Easy,
                botEquipment = BotEquipmentType.None,
                blindType = BossBlindType.TheApprentice,
                blindName = "Cờ Caro 15x15 Thuần Khiết",
                blindDescription = "Bàn cờ 15x15 chuẩn truyền thống. Nối 5 quân liên tiếp để chiến thắng!",
                maxPlayerMoves = 0,
                boardSize = 15,
                coinReward = 25,
                variantRule = XOVariantRule.ClassicCaro
            },
            new TowerFloorBlind
            {
                floorNumber = 2,
                floorTitle = "Tầng 2 - Bãi Mìn Rừng Rậm",
                mapName = "Bãi Mìn Rừng Rậm (Jungle Minefield)",
                mapThemeId = "jungle",
                mapAccentColor = XOThemeDatabase.Hex("#10B981"),
                bossName = "Vệ Binh Jax",
                bossTitle = "Thiết Vệ Rừng Sâu",
                bossElo = 800,
                difficulty = AIDifficulty.Easy,
                botEquipment = BotEquipmentType.None,
                blindType = BossBlindType.ChaosMinefield,
                blindName = "Trận Địa Mìn & Cổng Dịch Chuyển",
                blindDescription = "Bàn cờ 15x15 rải 3 quả mìn ẩn và 2 cổng dịch chuyển không gian!",
                maxPlayerMoves = 0,
                boardSize = 15,
                coinReward = 30,
                variantRule = XOVariantRule.ChaosMinefield
            },
            new TowerFloorBlind
            {
                floorNumber = 3,
                floorTitle = "Tầng 3 - Hẻm Núi Băng Giá",
                mapName = "Hẻm Núi Băng Giá (Glacial Chasm)",
                mapThemeId = "frost",
                mapAccentColor = XOThemeDatabase.Hex("#0EA5E9"),
                bossName = "Băng Hiệp Sĩ Freya",
                bossTitle = "Băng Kỵ Sĩ",
                bossElo = 950,
                difficulty = AIDifficulty.Normal,
                botEquipment = BotEquipmentType.FrostStaff,
                blindType = BossBlindType.TheNeedle,
                blindName = "Băng Trượng Hàn Khí",
                blindDescription = "15x15 Caro. Giới hạn 25 nước đi! Boss trang bị Băng Trượng định kỳ đóng băng ô đe dọa của bạn.",
                maxPlayerMoves = 25,
                boardSize = 15,
                coinReward = 35,
                variantRule = XOVariantRule.ClassicCaro
            },
            new TowerFloorBlind
            {
                floorNumber = 4,
                floorTitle = "Tầng 4 - Hố Đen Hư Không",
                mapName = "Hố Đen Hư Không (The Abyssal Void)",
                mapThemeId = "void",
                mapAccentColor = XOThemeDatabase.Hex("#8B5CF6"),
                bossName = "Thiết Giáp Boris",
                bossTitle = "Bức Tường Bất Khả Xâm Phạm",
                bossElo = 1100,
                difficulty = AIDifficulty.Normal,
                botEquipment = BotEquipmentType.MirrorAegis,
                blindType = BossBlindType.TheVoid,
                blindName = "Hư Không Vô Cực (Vùng Cấm 3x3)",
                blindDescription = "15x15 Caro. Trung tâm 3x3 là hố đen không thể đặt cờ! Triển khai thế công ở 4 cánh.",
                maxPlayerMoves = 0,
                boardSize = 15,
                coinReward = 40,
                variantRule = XOVariantRule.ClassicCaro
            },
            new TowerFloorBlind
            {
                floorNumber = 5,
                floorTitle = "Tầng 5 - Hỏa Diệm Sơn",
                mapName = "Hỏa Diệm Sơn (Pyro Volcano)",
                mapThemeId = "volcano",
                mapAccentColor = XOThemeDatabase.Hex("#EF4444"),
                bossName = "Cuồng Binh Ignis",
                bossTitle = "Hỏa Long Tướng",
                bossElo = 1250,
                difficulty = AIDifficulty.Hard,
                botEquipment = BotEquipmentType.FlameBlade,
                blindType = BossBlindType.TheArm,
                blindName = "Phong Ấn Gomoku Nghiêm Ngặt",
                blindDescription = "15x15 Caro. Nước 10 kích hoạt Phong Ấn: Hàng 5 bị chặn 2 đầu KHÔNG thắng, phải đánh thế mở!",
                maxPlayerMoves = 0,
                boardSize = 15,
                coinReward = 45,
                variantRule = XOVariantRule.ClassicCaro,
                hasMidMatchShift = true,
                midMatchShiftTurn = 10,
                midMatchShiftTitle = "ĐỘT BIẾN: PHONG ẤN GOMOKU!",
                midMatchShiftDesc = "Phong ấn kích hoạt! Hàng 5 bị chặn 2 đầu sẽ KHÔNG tính thắng! Phải đánh hở đầu!"
            },
            new TowerFloorBlind
            {
                floorNumber = 6,
                floorTitle = "Tầng 6 - Vực Thẳm Trọng Lực",
                mapName = "Vực Thẳm Trọng Lực (Gravity Abyss)",
                mapThemeId = "gravity",
                mapAccentColor = XOThemeDatabase.Hex("#F97316"),
                bossName = "Gai Sắt Vane",
                bossTitle = "Chiến Tướng Vực Sâu",
                bossElo = 1400,
                difficulty = AIDifficulty.Hard,
                botEquipment = BotEquipmentType.ThornArmor,
                blindType = BossBlindType.GravityDrop,
                blindName = "Trọng Lực Thả Cờ (15x15)",
                blindDescription = "Bàn cờ 15x15. Quân cờ rơi xuống đáy cột như Connect-4! Nối 5 quân liên tiếp để Thắng!",
                maxPlayerMoves = 0,
                boardSize = 15,
                coinReward = 50,
                variantRule = XOVariantRule.GravityDrop
            },
            new TowerFloorBlind
            {
                floorNumber = 7,
                floorTitle = "Tầng 7 - Võ Đài Nuốt Quân",
                mapName = "Võ Đài Nuốt Quân (Gobblet Colosseum)",
                mapThemeId = "gobblet",
                mapAccentColor = XOThemeDatabase.Hex("#3B82F6"),
                bossName = "Lôi Thần Raiden",
                bossTitle = "Thần Sấm Trận Địa",
                bossElo = 1550,
                difficulty = AIDifficulty.Hard,
                botEquipment = BotEquipmentType.ThunderHammer,
                blindType = BossBlindType.GobbletStack,
                blindName = "Võ Đài Nuốt Quân Gobblet (15x15)",
                blindDescription = "Bàn cờ 15x15. Quân cờ có 3 kích thước (Lv.1, Lv.2, Lv.3) có thể nuốt quân nhỏ hơn! Nối 5 để Thắng!",
                maxPlayerMoves = 0,
                boardSize = 15,
                coinReward = 60,
                variantRule = XOVariantRule.GobbletStack
            },
            new TowerFloorBlind
            {
                floorNumber = 8,
                floorTitle = "Tầng 8 - Song Long Sơn",
                mapName = "Song Long Sơn (Twin Dragon Mountain)",
                mapThemeId = "dragon",
                mapAccentColor = XOThemeDatabase.Hex("#14B8A6"),
                bossName = "Ảnh Kiếm Kage",
                bossTitle = "Sát Thủ Bóng Đêm",
                bossElo = 1750,
                difficulty = AIDifficulty.Hard,
                botEquipment = BotEquipmentType.FlameBlade,
                blindType = BossBlindType.TwinDragon,
                blindName = "Song Long Thức Tỉnh (2x5)",
                blindDescription = "15x15 Caro. Nước 10 Boss gọi rồng thứ 2: Phải tạo được HAI hàng 5 (2x5) mới Thắng!",
                maxPlayerMoves = 0,
                boardSize = 15,
                coinReward = 70,
                variantRule = XOVariantRule.ClassicCaro,
                hasMidMatchShift = true,
                midMatchShiftTurn = 10,
                midMatchShiftTitle = "ĐỘT BIẾN: SONG LONG THỨC TỈNH!",
                midMatchShiftDesc = "Boss triệu hồi con rồng thứ hai! Bạn bắt buộc phải tạo được HAI hàng 5 (2x5) mới Thắng!"
            },
            new TowerFloorBlind
            {
                floorNumber = 9,
                floorTitle = "Tầng 9 - Rừng Sương Mù Vĩnh Cửu",
                mapName = "Rừng Sương Mù Vĩnh Cửu (Mist Fog Citadel)",
                mapThemeId = "mist",
                mapAccentColor = XOThemeDatabase.Hex("#A855F7"),
                bossName = "Nữ Hoàng Băng Sylvan",
                bossTitle = "Băng Hậu Vĩnh Cửu",
                bossElo = 1950,
                difficulty = AIDifficulty.Hard,
                botEquipment = BotEquipmentType.FrostStaff,
                blindType = BossBlindType.FogOfWar,
                blindName = "Sương Mù & Bão Tuyết",
                blindDescription = "15x15 Caro. Quân Boss ẩn trong sương đến khi bạn đánh cạnh bên! Nước 12 bão tuyết đóng băng 3 ô.",
                maxPlayerMoves = 0,
                boardSize = 15,
                coinReward = 85,
                variantRule = XOVariantRule.ClassicCaro,
                hasMidMatchShift = true,
                midMatchShiftTurn = 12,
                midMatchShiftTitle = "ĐỘT BIẾN: BÃO TUYẾT SƯƠNG MÙ!",
                midMatchShiftDesc = "Bão tuyết đóng băng 3 ô chiến thuật trong 2 lượt! Sương mù vẫn đang che phủ!"
            },
            new TowerFloorBlind
            {
                floorNumber = 10,
                floorTitle = "Tầng 10 - Đỉnh Hoàng Gia Tối Thượng",
                mapName = "Đỉnh Hoàng Gia Tối Thượng (Overlord Celestial Summit)",
                mapThemeId = "apex",
                mapAccentColor = XOThemeDatabase.Hex("#F59E0B"),
                bossName = "Đế Vương Rex",
                bossTitle = "Đấng Toàn Năng",
                bossElo = 2200,
                difficulty = AIDifficulty.Hard,
                botEquipment = BotEquipmentType.OmniCrown,
                blindType = BossBlindType.OverlordApex,
                blindName = "Chung Kết Tối Thượng",
                blindDescription = "CHUNG KẾT TỐI THƯỢNG: Nối HAI hàng 5 (2x5) trong 30 nước đi trên bàn cờ sụp lún theo chu kỳ!",
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

        public static bool HasRelic(CaroRelicType type)
        {
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
