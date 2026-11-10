using System;
using System.Collections.Generic;
using UnityEngine;

namespace XOPlayable.Core
{
    public static class SafePrefs
    {
        private static readonly Dictionary<string, int> _mem = new Dictionary<string, int>();
        private static readonly Dictionary<string, string> _memStr = new Dictionary<string, string>();

        public static bool IsCLIEnvironment => System.AppDomain.CurrentDomain.FriendlyName.Contains("QARunner");

        public static int GetInt(string key, int def = 0)
        {
            if (IsCLIEnvironment) return _mem.TryGetValue(key, out int v) ? v : def;
            return CallUnityGetInt(key, def);
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private static int CallUnityGetInt(string key, int def)
        {
            try { return PlayerPrefs.GetInt(key, def); }
            catch { return _mem.TryGetValue(key, out int v) ? v : def; }
        }

        public static void SetInt(string key, int val)
        {
            _mem[key] = val;
            if (IsCLIEnvironment) return;
            CallUnitySetInt(key, val);
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private static void CallUnitySetInt(string key, int val)
        {
            try { PlayerPrefs.SetInt(key, val); } catch {}
        }

        public static string GetString(string key, string def = "")
        {
            if (IsCLIEnvironment) return _memStr.TryGetValue(key, out string v) ? v : def;
            return CallUnityGetString(key, def);
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private static string CallUnityGetString(string key, string def)
        {
            try { return PlayerPrefs.GetString(key, def); }
            catch { return _memStr.TryGetValue(key, out string v) ? v : def; }
        }

        public static void SetString(string key, string val)
        {
            _memStr[key] = val;
            if (IsCLIEnvironment) return;
            CallUnitySetString(key, val);
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private static void CallUnitySetString(string key, string val)
        {
            try { PlayerPrefs.SetString(key, val); } catch {}
        }

        public static void Save()
        {
            if (IsCLIEnvironment) return;
            CallUnitySave();
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private static void CallUnitySave()
        {
            try { PlayerPrefs.Save(); } catch {}
        }
    }

    [Serializable]
    public struct RankInfo
    {
        public string title;
        public int minStars;
        public int maxStars;
        public Color badgeColor;
    }

    [Serializable]
    public class XOMissionData
    {
        public int id;
        public string description;
        public int rewardCoins;
    }

    [Serializable]
    public class XOPuzzleStage
    {
        public int stageNumber;
        public string title;
        public string objectiveHint;
        public int maxPlayerMoves;
        public int rewardCoins;
        public List<MoveRecord> presetMoves = new List<MoveRecord>();
    }

    public enum BotEquipmentType
    {
        None = 0,
        MirrorAegis = 1,    // "Mirror Aegis" - Immune to Bomb & Swap skills
        FrostStaff = 2,     // "Frost Staff" - Automatically Freezes player's threatening open square for 3 turns!
        ThunderHammer = 3,  // "Thunder Hammer" - Every 5 turns, Bot triggers a double turn (places 2 pieces)!
        ThornArmor = 4,     // "Thorn Armor" - Locks/counters adjacent line when player builds threat!
        FlameBlade = 5,     // "Flame Blade" - Tactically launches a Bomb destroying 1 player piece once per match!
        OmniCrown = 6       // "Omni Crown" - Supreme dual relic: Cryo Freeze + Double Turn!
    }

    [Serializable]
    public class XOTowerFloor
    {
        public int floorNumber;
        public string bossName;
        public string bossTitle;
        public int bossElo;
        public AIDifficulty difficulty;
        public BotEquipmentType equipment;
        public string equipmentName;
        public string equipmentDesc;
        public int rewardCoins;
        public string rewardBonus;
        public int boardSize;
    }

    public enum BotCounterSkill
    {
        None = 0,
        TileHunter = 1,   // Prioritizes grabbing Gold & Lightning tiles
        CryoFreeze = 2,   // Can freeze 1 dangerous empty tile once per match
        WarlordStrike = 3 // Aggressively builds Double-3 & freezes/counters once per match
    }

    [Serializable]
    public class XOBotProfile
    {
        public int botIndex;
        public string botName;
        public int eloRating;
        public AIDifficulty baseDifficulty;
        public string styleTitle;
        public BotCounterSkill counterSkill;
        public float mistakeChance; // 0.0 = perfect, 0.35 = beginner mistakes
        public string[] greetingLines;
        public string[] onPlayerGoodMoveLines;
        public string[] onBotThreatLines;
        public string[] onPlayerSkillUsedLines;
        public string[] onBotCounterSkillLines;

        public string PickRandom(string[] pool)
        {
            if (pool == null || pool.Length == 0) return "Good move!";
            return pool[XORandom.Range(0, pool.Length)];
        }
    }

    [Serializable]
    public struct XOLeaderboardEntry
    {
        public int rankPosition;
        public string playerName;
        public string titleTag;
        public int eloRating;
        public int stars;
        public bool isHumanPlayer;
        public int botIndexIfRival; // -1 if not a playable bot rival
    }

    [Serializable]
    public struct XOAchievementState
    {
        public string id;
        public string title;
        public string description;
        public int currentValue;
        public int targetValue;
        public int rewardCoins;
        public string bonusItemLabel;
        public bool isClaimed;
        public bool IsReadyToClaim => !isClaimed && currentValue >= targetValue;
    }

    public static class XOProgressionSystem
    {
        private static readonly RankInfo[] Ranks = new RankInfo[]
        {
            new RankInfo { title = "Bronze Novice",  minStars = 0,  maxStars = 3,   badgeColor = new Color(0.80f, 0.50f, 0.20f) },
            new RankInfo { title = "Silver Knight",  minStars = 3,  maxStars = 7,   badgeColor = new Color(0.75f, 0.80f, 0.88f) },
            new RankInfo { title = "Gold Master",    minStars = 7,  maxStars = 12,  badgeColor = new Color(0.98f, 0.75f, 0.15f) },
            new RankInfo { title = "Diamond Elite",  minStars = 12, maxStars = 20,  badgeColor = new Color(0.22f, 0.83f, 0.98f) },
            new RankInfo { title = "Grandmaster",    minStars = 20, maxStars = 999, badgeColor = new Color(0.96f, 0.25f, 0.55f) }
        };

        private static readonly XOMissionData[] Missions = new XOMissionData[]
        {
            new XOMissionData { id = 0, description = "Win a match within 15 moves",             rewardCoins = 25 },
            new XOMissionData { id = 1, description = "Win with a 5-in-a-row diagonal line",     rewardCoins = 30 },
            new XOMissionData { id = 2, description = "Use Bomb, Freeze or Swap and win",        rewardCoins = 25 },
            new XOMissionData { id = 3, description = "Reach a 2+ Win Streak",                   rewardCoins = 35 },
            new XOMissionData { id = 4, description = "Clear a Tower Floor or 1300+ ELO Bot",    rewardCoins = 40 }
        };

        /// <summary>
        /// 6 Distinct AI Rivals from Easy (ELO 550) to Grandmaster (ELO 1900).
        /// Higher-tier bots play two-way Caro: contesting Special Tiles & triggering a 1-time Counter Skill!
        /// </summary>
        public static readonly XOBotProfile[] Bots = new XOBotProfile[]
        {
            new XOBotProfile
            {
                botIndex = 0,
                botName = "Rookie Leo",
                eloRating = 550,
                baseDifficulty = AIDifficulty.Easy,
                styleTitle = "Casual Beginner",
                counterSkill = BotCounterSkill.None,
                mistakeChance = 0.34f,
                greetingLines = new[] { "Hey! Go easy on me, I'm still learning!", "Let's have a fun 5-in-a-row match!" },
                onPlayerGoodMoveLines = new[] { "Whoa, I didn't see that line coming!", "Wait, you blocked me?!" },
                onBotThreatLines = new[] { "Aha! Look at my row!", "Wait, am I actually winning?!" },
                onPlayerSkillUsedLines = new[] { "Hey! Using power-ups is cheating haha!", "Whoa, nice gadget!" },
                onBotCounterSkillLines = new[] { "Watch my move!" }
            },
            new XOBotProfile
            {
                botIndex = 1,
                botName = "Scout Hannah",
                eloRating = 820,
                baseDifficulty = AIDifficulty.Easy,
                styleTitle = "Treasure Hunter",
                counterSkill = BotCounterSkill.TileHunter,
                mistakeChance = 0.16f,
                greetingLines = new[] { "Those Gold & Lightning tiles on the board are MINE!", "Race you to the special tiles!" },
                onPlayerGoodMoveLines = new[] { "Hey, I wanted that square!", "Sharp defense! You're pretty good." },
                onBotThreatLines = new[] { "Four in a row! Can you stop it?", "Too fast for you!" },
                onPlayerSkillUsedLines = new[] { "Ooh, flashy skill! My turn to strike back!", "Not fair, I liked that piece!" },
                onBotCounterSkillLines = new[] { "Snagged the bonus tile before you!" }
            },
            new XOBotProfile
            {
                botIndex = 2,
                botName = "Guardian Viktor",
                eloRating = 1120,
                baseDifficulty = AIDifficulty.Normal,
                styleTitle = "Iron Defender",
                counterSkill = BotCounterSkill.TileHunter,
                mistakeChance = 0.05f,
                greetingLines = new[] { "My defense is unbreakable. Show me your strategy.", "Every open-3 you build, I will shut down." },
                onPlayerGoodMoveLines = new[] { "Impressive tactical vision.", "A solid block. Let's see how you handle my next line." },
                onBotThreatLines = new[] { "Your defense has a gap! Check my line!", "Check! Block this or lose." },
                onPlayerSkillUsedLines = new[] { "Tactical weaponry? I will adapt.", "A clever disruption, but my formation holds." },
                onBotCounterSkillLines = new[] { "Securing key board territory!" }
            },
            new XOBotProfile
            {
                botIndex = 3,
                botName = "Frost Witch Lyra",
                eloRating = 1380,
                baseDifficulty = AIDifficulty.Normal,
                styleTitle = "Cryo Tactician (Can Freeze!)",
                counterSkill = BotCounterSkill.CryoFreeze,
                mistakeChance = 0.0f,
                greetingLines = new[] { "Careful—I can FREEZE squares on the board too!", "Let's see how you play when the board turns to ice!" },
                onPlayerGoodMoveLines = new[] { "Cold and calculated! Nice move!", "Hmph, you spotted my trap." },
                onBotThreatLines = new[] { "Winter closes in! Try blocking both ends!", "Four connected! Feel the chill?" },
                onPlayerSkillUsedLines = new[] { "Two can play the skill game!", "Bold move! Don't forget I have ice magic too." },
                onBotCounterSkillLines = new[] { "CRYO COUNTER! I just FROZE a key square!", "ICE LOCK! That square is frozen for 3 turns!" }
            },
            new XOBotProfile
            {
                botIndex = 4,
                botName = "Warlord Kael",
                eloRating = 1620,
                baseDifficulty = AIDifficulty.Hard,
                styleTitle = "Fork Master (Counter-Strike!)",
                counterSkill = BotCounterSkill.WarlordStrike,
                mistakeChance = 0.0f,
                greetingLines = new[] { "Only true tacticians survive my Double-Three forks!", "I hunt Lightning tiles and punish every mistake!" },
                onPlayerGoodMoveLines = new[] { "Worthy of a Warlord! Great read!", "You blocked my primary fork?!" },
                onBotThreatLines = new[] { "UNSTOPPABLE ATTACK! Block this if you can!", "Double threat! Which end will you save?" },
                onPlayerSkillUsedLines = new[] { "Smart use of power-ups! You'll need them!", "That only delays the inevitable!" },
                onBotCounterSkillLines = new[] { "WARLORD COUNTER! Locking down your attack lane!", "TACTICAL LOCKDOWN! Try getting past that!" }
            },
            new XOBotProfile
            {
                botIndex = 5,
                botName = "Grandmaster Zero",
                eloRating = 1900,
                baseDifficulty = AIDifficulty.Hard,
                styleTitle = "Supreme AI (Full Power)",
                counterSkill = BotCounterSkill.WarlordStrike,
                mistakeChance = 0.0f,
                greetingLines = new[] { "Calculating 10,000 board permutations. Make your move.", "Few humans have defeated me on a 15x15 grid." },
                onPlayerGoodMoveLines = new[] { "Optimal move detected. Recalculating...", "Fascinating. A 99th-percentile human move." },
                onBotThreatLines = new[] { "Win probability rising to 94%. Defend immediately.", "Lethal sequence initiated." },
                onPlayerSkillUsedLines = new[] { "External variable detected. Re-evaluating matrix.", "Clever anomaly. Adjusting strategy." },
                onBotCounterSkillLines = new[] { "PROTOCOL ZERO: Locking critical intersection!", "COUNTER-MEASURE DEPLOYED: Tile locked!" }
            }
        };

        public static int SelectedBotIndex
        {
            get => Mathf.Clamp(SafePrefs.GetInt("XO_SelectedBot", 2), 0, Bots.Length - 1);
            set
            {
                SafePrefs.SetInt("XO_SelectedBot", Mathf.Clamp(value, 0, Bots.Length - 1));
                SafePrefs.Save();
            }
        }

        public static XOBotProfile GetActiveBot()
        {
            return Bots[SelectedBotIndex];
        }

        public static int TotalStars
        {
            get => SafePrefs.GetInt("XO_RankStars", 1);
            set
            {
                SafePrefs.SetInt("XO_RankStars", Mathf.Max(0, value));
                SafePrefs.Save();
            }
        }

        public static int WinStreak
        {
            get => SafePrefs.GetInt("XO_WinStreak", 0);
            set
            {
                int v = Mathf.Max(0, value);
                SafePrefs.SetInt("XO_WinStreak", v);
                if (v > MaxWinStreakReached) MaxWinStreakReached = v;
                SafePrefs.Save();
            }
        }

        public static int TotalMatchesWon
        {
            get => SafePrefs.GetInt("XO_TotalWins", 0);
            set { SafePrefs.SetInt("XO_TotalWins", Mathf.Max(0, value)); SafePrefs.Save(); }
        }

        public static int TotalTacticalCallouts
        {
            get => SafePrefs.GetInt("XO_TotalCallouts", 0);
            set { SafePrefs.SetInt("XO_TotalCallouts", Mathf.Max(0, value)); SafePrefs.Save(); }
        }

        public static int TotalSpecialTilesClaimed
        {
            get => SafePrefs.GetInt("XO_TotalSpecialTiles", 0);
            set { SafePrefs.SetInt("XO_TotalSpecialTiles", Mathf.Max(0, value)); SafePrefs.Save(); }
        }

        public static int MaxWinStreakReached
        {
            get => SafePrefs.GetInt("XO_MaxStreak", 0);
            set { SafePrefs.SetInt("XO_MaxStreak", Mathf.Max(0, value)); SafePrefs.Save(); }
        }

        public static bool IsBotConquered(int botIndex)
        {
            return SafePrefs.GetInt("XO_BotConquered_" + botIndex, 0) == 1;
        }

        public static bool MarkBotConquered(int botIndex)
        {
            bool wasAlreadyConquered = IsBotConquered(botIndex);
            if (!wasAlreadyConquered)
            {
                SafePrefs.SetInt("XO_BotConquered_" + botIndex, 1);
                SafePrefs.Save();
                return true; // First-time victory bounty!
            }
            return false;
        }

        public static int GetConqueredBotsCount()
        {
            int count = 0;
            for (int i = 0; i < Bots.Length; i++)
            {
                if (IsBotConquered(i)) count++;
            }
            return count;
        }

        public static readonly XOTowerFloor[] TowerFloors = new XOTowerFloor[]
        {
            new XOTowerFloor
            {
                floorNumber = 1,
                bossName = "Gate Sentry Vane",
                bossTitle = "Floor 1 Guardian",
                bossElo = 700,
                difficulty = AIDifficulty.Easy,
                equipment = BotEquipmentType.None,
                equipmentName = "None",
                equipmentDesc = "Standard defense, no active equipment.",
                rewardCoins = 25,
                rewardBonus = "+1 Hint",
                boardSize = 15
            },
            new XOTowerFloor
            {
                floorNumber = 2,
                bossName = "Shield Knight Boris",
                bossTitle = "Floor 2 Defender",
                bossElo = 900,
                difficulty = AIDifficulty.Easy,
                equipment = BotEquipmentType.MirrorAegis,
                equipmentName = "Mirror Aegis",
                equipmentDesc = "Immune to player Bomb and Swap skills!",
                rewardCoins = 30,
                rewardBonus = "+1 Undo",
                boardSize = 15
            },
            new XOTowerFloor
            {
                floorNumber = 3,
                bossName = "Frost Sorceress Lyra",
                bossTitle = "Floor 3 Cryomancer",
                bossElo = 1150,
                difficulty = AIDifficulty.Normal,
                equipment = BotEquipmentType.FrostStaff,
                equipmentName = "Frost Staff",
                equipmentDesc = "Freezes your dangerous open square for 3 turns when you build a 3-in-a-row!",
                rewardCoins = 40,
                rewardBonus = "+1 Freeze",
                boardSize = 15
            },
            new XOTowerFloor
            {
                floorNumber = 4,
                bossName = "Spike Gladiator Thorne",
                bossTitle = "Floor 4 Brawler",
                bossElo = 1300,
                difficulty = AIDifficulty.Normal,
                equipment = BotEquipmentType.ThornArmor,
                equipmentName = "Thorn Armor",
                equipmentDesc = "Locks an adjacent connecting tile with thorny barriers when threatened!",
                rewardCoins = 50,
                rewardBonus = "+1 Bomb",
                boardSize = 15
            },
            new XOTowerFloor
            {
                floorNumber = 5,
                bossName = "Thunder Warden Raiden",
                bossTitle = "Floor 5 Stormcaller",
                bossElo = 1500,
                difficulty = AIDifficulty.Hard,
                equipment = BotEquipmentType.ThunderHammer,
                equipmentName = "Thunder Hammer",
                equipmentDesc = "Every 5 turns, unleashes a lightning double-turn (places 2 pieces)!",
                rewardCoins = 65,
                rewardBonus = "+1 Swap",
                boardSize = 15
            },
            new XOTowerFloor
            {
                floorNumber = 6,
                bossName = "Flame Warmaster Ignis",
                bossTitle = "Floor 6 Pyromancer",
                bossElo = 1680,
                difficulty = AIDifficulty.Hard,
                equipment = BotEquipmentType.FlameBlade,
                equipmentName = "Flame Blade",
                equipmentDesc = "Detonates a tactical bomb destroying 1 of your key pieces once per match!",
                rewardCoins = 80,
                rewardBonus = "+1 Bomb",
                boardSize = 15
            },
            new XOTowerFloor
            {
                floorNumber = 7,
                bossName = "Frostbite Dualist Zephyr",
                bossTitle = "Floor 7 Ice Vanguard",
                bossElo = 1850,
                difficulty = AIDifficulty.Hard,
                equipment = BotEquipmentType.FrostStaff,
                equipmentName = "Glacial Staff",
                equipmentDesc = "Deep freeze locks open squares on your attack lines!",
                rewardCoins = 95,
                rewardBonus = "+1 Freeze",
                boardSize = 15
            },
            new XOTowerFloor
            {
                floorNumber = 8,
                bossName = "Ironclad Warlord Kael",
                bossTitle = "Floor 8 Iron Monarch",
                bossElo = 2000,
                difficulty = AIDifficulty.Hard,
                equipment = BotEquipmentType.MirrorAegis,
                equipmentName = "Reflective Bastion",
                equipmentDesc = "Total skill immunity: deflects Bomb and Swap back!",
                rewardCoins = 115,
                rewardBonus = "+1 Swap",
                boardSize = 15
            },
            new XOTowerFloor
            {
                floorNumber = 9,
                bossName = "Shadow Archon Malakor",
                bossTitle = "Floor 9 Void Master",
                bossElo = 2150,
                difficulty = AIDifficulty.Hard,
                equipment = BotEquipmentType.FlameBlade,
                equipmentName = "Infernal Blade",
                equipmentDesc = "Tactical bomb detonation + aggressive double-three counter attacks!",
                rewardCoins = 140,
                rewardBonus = "+1 All",
                boardSize = 15
            },
            new XOTowerFloor
            {
                floorNumber = 10,
                bossName = "Tower Overlord Zero",
                bossTitle = "Apex Floor 10 Boss",
                bossElo = 2300,
                difficulty = AIDifficulty.Hard,
                equipment = BotEquipmentType.OmniCrown,
                equipmentName = "Omni Crown",
                equipmentDesc = "Supreme Overlord relic: Cryo Freeze + Double Turn on critical turns!",
                rewardCoins = 250,
                rewardBonus = "Piece Skin Unlock!",
                boardSize = 15
            }
        };

        public static int CurrentTowerFloor
        {
            get => Mathf.Clamp(SafePrefs.GetInt("XO_TowerFloor", 1), 1, 10);
            set
            {
                SafePrefs.SetInt("XO_TowerFloor", Mathf.Clamp(value, 1, 10));
                SafePrefs.Save();
            }
        }

        public static int HighestTowerFloorCleared
        {
            get => SafePrefs.GetInt("XO_TowerHighestCleared", 0);
            set
            {
                SafePrefs.SetInt("XO_TowerHighestCleared", Mathf.Max(0, value));
                SafePrefs.Save();
            }
        }

        public static bool IsTowerFloorUnlocked(int floor)
        {
            if (floor <= 1) return true;
            return floor <= HighestTowerFloorCleared + 1;
        }

        public static bool MarkTowerFloorCleared(int floor)
        {
            if (floor > HighestTowerFloorCleared)
            {
                HighestTowerFloorCleared = floor;
                if (floor < 10)
                {
                    CurrentTowerFloor = floor + 1;
                }
                SafePrefs.Save();
                return true; // First time floor clear!
            }
            return false;
        }

        public static XOTowerFloor GetActiveTowerFloor()
        {
            return TowerFloors[Mathf.Clamp(CurrentTowerFloor - 1, 0, TowerFloors.Length - 1)];
        }

        public static List<XOAchievementState> GetAchievementsList()
        {
            int wins = TotalMatchesWon;
            int botsBeaten = GetConqueredBotsCount();
            int towerCleared = HighestTowerFloorCleared;
            int stagesCleared = Mathf.Max(0, CurrentPuzzleStage - 1);
            int callouts = TotalTacticalCallouts;
            int specials = TotalSpecialTilesClaimed;
            int maxStreak = MaxWinStreakReached;

            return new List<XOAchievementState>
            {
                BuildAch("ach_first_win",     "First Victory",       "Win your first 5-in-a-row Caro match",            wins,         1,  30,  "+1 Hint"),
                BuildAch("ach_rival_slayer",  "Rival Challenger",    "Defeat 3 different AI Rivals in Campaign",        botsBeaten,   3,  45,  "+1 Swap"),
                BuildAch("ach_tower_floor3",  "Tower Adept",         "Clear Floor 3 in Tower of Champions",             towerCleared, 3,  50,  "+1 Freeze"),
                BuildAch("ach_tower_floor5",  "Tower Warden",        "Clear Floor 5 in Tower of Champions",             towerCleared, 5,  75,  "+1 Bomb"),
                BuildAch("ach_tactician",     "Master Tactician",    "Execute 5 Critical Blocks or Double Threats",     callouts,     5,  40,  "+1 Freeze"),
                BuildAch("ach_tile_hunter",   "Treasure Seeker",     "Claim 5 Gold or Lightning Special Tiles",         specials,     5,  35,  "+1 Undo"),
                BuildAch("ach_streak_3",      "Unstoppable Streak",  "Reach a 3-Win Streak in any mode",                maxStreak,    3,  45,  "+1 Swap"),
                BuildAch("ach_tower_clear",   "Tower Conqueror",     "Conquer Floor 10 Tower Overlord",                 towerCleared, 10, 100, "+2 All")
            };
        }

        private static XOAchievementState BuildAch(string id, string title, string desc, int cur, int target, int coins, string bonusLabel)
        {
            return new XOAchievementState
            {
                id = id,
                title = title,
                description = desc,
                currentValue = Mathf.Clamp(cur, 0, target),
                targetValue = target,
                rewardCoins = coins,
                bonusItemLabel = bonusLabel,
                isClaimed = SafePrefs.GetInt("XO_AchClaimed_" + id, 0) == 1
            };
        }

        public static bool HasAnyClaimableAchievement()
        {
            var list = GetAchievementsList();
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i].IsReadyToClaim) return true;
            }
            return false;
        }

        public static void MarkAchievementClaimed(string id)
        {
            SafePrefs.SetInt("XO_AchClaimed_" + id, 1);
            SafePrefs.Save();
        }

        public static int PlayerELO => 600 + TotalStars * 60 + WinStreak * 25;

        public static int ChestProgress
        {
            get => SafePrefs.GetInt("XO_ChestProgress", 0);
            set
            {
                SafePrefs.SetInt("XO_ChestProgress", Mathf.Clamp(value, 0, 3));
                SafePrefs.Save();
            }
        }

        public static int CurrentPuzzleStage
        {
            get => SafePrefs.GetInt("XO_PuzzleStage", 1);
            set
            {
                SafePrefs.SetInt("XO_PuzzleStage", Mathf.Max(1, value));
                SafePrefs.Save();
            }
        }

        public static int CurrentMissionIndex
        {
            get => SafePrefs.GetInt("XO_MissionIdx", 0) % Missions.Length;
            set
            {
                SafePrefs.SetInt("XO_MissionIdx", value % Missions.Length);
                SafePrefs.Save();
            }
        }

        public static RankInfo GetCurrentRank(out int starsInTier, out int tierTargetStars)
        {
            int s = TotalStars;
            for (int i = 0; i < Ranks.Length; i++)
            {
                if (s < Ranks[i].maxStars || i == Ranks.Length - 1)
                {
                    starsInTier = s - Ranks[i].minStars;
                    tierTargetStars = Mathf.Max(1, Ranks[i].maxStars - Ranks[i].minStars);
                    return Ranks[i];
                }
            }
            starsInTier = s;
            tierTargetStars = 5;
            return Ranks[0];
        }

        public static List<XOLeaderboardEntry> GetTop10Leaderboard(out int humanRankPosition)
        {
            List<XOLeaderboardEntry> list = new List<XOLeaderboardEntry>();

            // Add the 6 AI Rivals (players can challenge any of them directly from the Leaderboard!)
            for (int i = 0; i < Bots.Length; i++)
            {
                XOBotProfile b = Bots[i];
                list.Add(new XOLeaderboardEntry
                {
                    playerName = b.botName,
                    titleTag = b.styleTitle,
                    eloRating = b.eloRating,
                    stars = Mathf.Max(1, (b.eloRating - 500) / 60),
                    isHumanPlayer = false,
                    botIndexIfRival = i
                });
            }

            // Add 3 Arena Contenders to fill out a 10-player ladder
            list.Add(new XOLeaderboardEntry { playerName = "Alex_Grid99",  titleTag = "Silver Contender", eloRating = 720,  stars = 3,  isHumanPlayer = false, botIndexIfRival = 1 });
            list.Add(new XOLeaderboardEntry { playerName = "NovaBlade_XO", titleTag = "Gold Tactician",   eloRating = 1250, stars = 11, isHumanPlayer = false, botIndexIfRival = 3 });
            list.Add(new XOLeaderboardEntry { playerName = "ZenithKing",   titleTag = "Diamond Ace",      eloRating = 1750, stars = 19, isHumanPlayer = false, botIndexIfRival = 4 });

            // Add Human Player ("YOU")
            RankInfo myRank = GetCurrentRank(out _, out _);
            list.Add(new XOLeaderboardEntry
            {
                playerName = "YOU (Player)",
                titleTag = myRank.title,
                eloRating = PlayerELO,
                stars = TotalStars,
                isHumanPlayer = true,
                botIndexIfRival = -1
            });

            // Sort descending by ELO
            list.Sort((a, b) => b.eloRating.CompareTo(a.eloRating));

            humanRankPosition = 10;
            for (int i = 0; i < list.Count; i++)
            {
                XOLeaderboardEntry entry = list[i];
                entry.rankPosition = i + 1;
                list[i] = entry;
                if (entry.isHumanPlayer)
                {
                    humanRankPosition = i + 1;
                }
            }

            return list;
        }

        public static XOMissionData GetActiveMission()
        {
            return Missions[CurrentMissionIndex];
        }

        public static bool CheckAndAdvanceMission(
            int playerMovesCount,
            bool wonByDiagonal,
            bool usedPowerUpThisMatch,
            int newWinStreak,
            bool isPuzzleOrHardAI,
            out XOMissionData completedMission)
        {
            completedMission = GetActiveMission();
            bool passed = completedMission.id switch
            {
                0 => playerMovesCount <= 12,
                1 => wonByDiagonal,
                2 => usedPowerUpThisMatch,
                3 => newWinStreak >= 2,
                4 => isPuzzleOrHardAI,
                _ => true
            };

            if (passed)
            {
                CurrentMissionIndex = (CurrentMissionIndex + 1) % Missions.Length;
                return true;
            }
            return false;
        }

        public static XOPuzzleStage GeneratePuzzleStage(int stageNumber, int boardSize)
        {
            int mid = boardSize / 2;
            int patternId = (stageNumber - 1) % 6;

            XOPuzzleStage stage = new XOPuzzleStage
            {
                stageNumber = stageNumber,
                rewardCoins = 60 + Mathf.Min(stageNumber * 10, 140)
            };

            void AddX(int dr, int dc) => stage.presetMoves.Add(new MoveRecord(Mathf.Clamp(mid + dr, 0, boardSize - 1), Mathf.Clamp(mid + dc, 0, boardSize - 1), CellPiece.X));
            void AddO(int dr, int dc) => stage.presetMoves.Add(new MoveRecord(Mathf.Clamp(mid + dr, 0, boardSize - 1), Mathf.Clamp(mid + dc, 0, boardSize - 1), CellPiece.O));

            switch (patternId)
            {
                case 0:
                    stage.title = $"Stage {stageNumber}: Twin Blades";
                    stage.objectiveHint = "Create a double-threat fork at the center to win in 3 moves!";
                    stage.maxPlayerMoves = 3;
                    AddX(0, -1); AddX(0, -2); AddX(0, 1);
                    AddX(-1, 0); AddX(1, 0);
                    AddO(0, -3); AddO(-2, -1); AddO(2, 1); AddO(1, -1); AddO(-1, 2);
                    break;

                case 1:
                    stage.title = $"Stage {stageNumber}: Counter Strike";
                    stage.objectiveHint = "Block O's threat while extending your diagonal line!";
                    stage.maxPlayerMoves = 4;
                    AddO(-1, -1); AddO(-1, 0); AddO(-1, 1);
                    AddX(0, -1); AddX(1, 0); AddX(2, 1);
                    AddX(-2, -2); AddO(2, -1);
                    break;

                case 2:
                    stage.title = $"Stage {stageNumber}: Crossfire Trap";
                    stage.objectiveHint = "Connect your diagonal branches into an unstoppable open-4!";
                    stage.maxPlayerMoves = 4;
                    AddX(-2, -2); AddX(-1, -1); AddX(1, 1);
                    AddX(0, -2); AddX(0, -1);
                    AddO(-3, -3); AddO(2, 2); AddO(-1, 1); AddO(1, -1); AddO(0, 2);
                    break;

                case 3:
                    stage.title = $"Stage {stageNumber}: Siege Breaker";
                    stage.objectiveHint = "O surrounds the center! Use Bomb/Swap or find the winning gap!";
                    stage.maxPlayerMoves = 5;
                    AddO(0, -1); AddO(0, 0); AddO(0, 1);
                    AddO(-1, 0); AddO(1, 0);
                    AddX(-2, -1); AddX(-2, 0); AddX(-2, 1);
                    AddX(-1, -2); AddX(0, -2); AddX(1, -2);
                    break;

                case 4:
                    stage.title = $"Stage {stageNumber}: Golden Point";
                    stage.objectiveHint = "Find the winning intersection between the row and diagonal!";
                    stage.maxPlayerMoves = 3;
                    AddX(1, -2); AddX(1, -1); AddX(1, 1);
                    AddX(-1, -1); AddX(0, 0);
                    AddO(1, -3); AddO(-2, -2); AddO(0, 1); AddO(-1, 1); AddO(2, 0);
                    break;

                default:
                    stage.title = $"Stage {stageNumber}: Grandmaster Clash";
                    stage.objectiveHint = "Fierce mid-game formation! Win within 5 moves!";
                    stage.maxPlayerMoves = 5;
                    AddX(0, 0); AddX(0, 1); AddX(1, -1); AddX(-1, -1); AddX(1, 1);
                    AddO(-1, 0); AddO(-1, 1); AddO(0, -1); AddO(1, 0); AddO(2, 2);
                    break;
            }

            return stage;
        }
    }
}
