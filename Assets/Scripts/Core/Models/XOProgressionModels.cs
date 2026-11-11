using System;
using System.Collections.Generic;

namespace XOPlayable.Core
{
    [Serializable]
    public struct RankInfo
    {
        public string title;
        public int minStars;
        public int maxStars;
        public XOColor badgeColor;
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

    [Serializable]
    public class XOBotProfile
    {
        private static readonly XORandomStream FallbackRng = new XORandomStream();

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

        public string PickRandom(string[] pool, XORandomStream rng = null)
        {
            if (pool == null || pool.Length == 0) return "Good move!";
            XORandomStream stream = rng ?? FallbackRng;
            return pool[stream.Range(0, pool.Length)];
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
}

