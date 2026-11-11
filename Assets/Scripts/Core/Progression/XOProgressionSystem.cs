using System.Collections.Generic;

namespace XOPlayable.Core
{
    public static class XOProgressionSystem
    {
        public static XOBotProfile[] Bots => XOProgressionCatalog.Bots;
        public static XOTowerFloor[] TowerFloors => XOProgressionCatalog.TowerFloors;

        public static int SelectedBotIndex
        {
            get => XOMath.Clamp(XOStorage.GetInt("XO_SelectedBot", 2), 0, Bots.Length - 1);
            set
            {
                XOStorage.SetInt("XO_SelectedBot", XOMath.Clamp(value, 0, Bots.Length - 1));
                XOStorage.Save();
            }
        }

        public static XOBotProfile GetActiveBot() => Bots[SelectedBotIndex];

        public static int TotalStars
        {
            get => XOStorage.GetInt("XO_RankStars", 1);
            set
            {
                XOStorage.SetInt("XO_RankStars", XOMath.Max(0, value));
                XOStorage.Save();
            }
        }

        public static int WinStreak
        {
            get => XOStorage.GetInt("XO_WinStreak", 0);
            set
            {
                int v = XOMath.Max(0, value);
                XOStorage.SetInt("XO_WinStreak", v);
                if (v > MaxWinStreakReached) MaxWinStreakReached = v;
                XOStorage.Save();
            }
        }

        public static int TotalMatchesWon
        {
            get => XOStorage.GetInt("XO_TotalWins", 0);
            set { XOStorage.SetInt("XO_TotalWins", XOMath.Max(0, value)); XOStorage.Save(); }
        }

        public static int TotalTacticalCallouts
        {
            get => XOStorage.GetInt("XO_TotalCallouts", 0);
            set { XOStorage.SetInt("XO_TotalCallouts", XOMath.Max(0, value)); XOStorage.Save(); }
        }

        public static int TotalSpecialTilesClaimed
        {
            get => XOStorage.GetInt("XO_TotalSpecialTiles", 0);
            set { XOStorage.SetInt("XO_TotalSpecialTiles", XOMath.Max(0, value)); XOStorage.Save(); }
        }

        public static int MaxWinStreakReached
        {
            get => XOStorage.GetInt("XO_MaxStreak", 0);
            set { XOStorage.SetInt("XO_MaxStreak", XOMath.Max(0, value)); XOStorage.Save(); }
        }

        public static bool IsBotConquered(int botIndex)
            => XOStorage.GetInt("XO_BotConquered_" + botIndex, 0) == 1;

        public static bool MarkBotConquered(int botIndex)
        {
            if (IsBotConquered(botIndex)) return false;
            XOStorage.SetInt("XO_BotConquered_" + botIndex, 1);
            XOStorage.Save();
            return true;
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

        public static int CurrentTowerFloor
        {
            get => XOMath.Clamp(XOStorage.GetInt("XO_TowerFloor", 1), 1, 10);
            set
            {
                XOStorage.SetInt("XO_TowerFloor", XOMath.Clamp(value, 1, 10));
                XOStorage.Save();
            }
        }

        public static int HighestTowerFloorCleared
        {
            get => XOStorage.GetInt("XO_TowerHighestCleared", 0);
            set
            {
                XOStorage.SetInt("XO_TowerHighestCleared", XOMath.Max(0, value));
                XOStorage.Save();
            }
        }

        public static bool IsTowerFloorUnlocked(int floor)
            => floor <= 1 || floor <= HighestTowerFloorCleared + 1;

        public static bool MarkTowerFloorCleared(int floor)
        {
            if (floor <= HighestTowerFloorCleared) return false;
            HighestTowerFloorCleared = floor;
            if (floor < 10) CurrentTowerFloor = floor + 1;
            XOStorage.Save();
            return true;
        }

        public static XOTowerFloor GetActiveTowerFloor()
            => TowerFloors[XOMath.Clamp(CurrentTowerFloor - 1, 0, TowerFloors.Length - 1)];

        public static List<XOAchievementState> GetAchievementsList()
        {
            int wins = TotalMatchesWon;
            int botsBeaten = GetConqueredBotsCount();
            int towerCleared = HighestTowerFloorCleared;
            int callouts = TotalTacticalCallouts;
            int specials = TotalSpecialTilesClaimed;
            int maxStreak = MaxWinStreakReached;

            return new List<XOAchievementState>
            {
                BuildAch("ach_first_win",    "First Victory",      "Win your first 5-in-a-row Caro match",         wins,         1,  30,  "+1 Hint"),
                BuildAch("ach_rival_slayer", "Rival Challenger",   "Defeat 3 different AI Rivals in Campaign",     botsBeaten,   3,  45,  "+1 Swap"),
                BuildAch("ach_tower_floor3", "Tower Adept",        "Clear Floor 3 in Tower of Champions",          towerCleared, 3,  50,  "+1 Freeze"),
                BuildAch("ach_tower_floor5", "Tower Warden",       "Clear Floor 5 in Tower of Champions",          towerCleared, 5,  75,  "+1 Bomb"),
                BuildAch("ach_tactician",    "Master Tactician",   "Execute 5 Critical Blocks or Double Threats",  callouts,     5,  40,  "+1 Freeze"),
                BuildAch("ach_tile_hunter",  "Treasure Seeker",    "Claim 5 Gold or Lightning Special Tiles",      specials,     5,  35,  "+1 Undo"),
                BuildAch("ach_streak_3",     "Unstoppable Streak", "Reach a 3-Win Streak in any mode",             maxStreak,    3,  45,  "+1 Swap"),
                BuildAch("ach_tower_clear",  "Tower Conqueror",    "Conquer Floor 10 Tower Overlord",              towerCleared, 10, 100, "+2 All")
            };
        }

        private static XOAchievementState BuildAch(string id, string title, string desc, int cur, int target, int coins, string bonusLabel)
        {
            return new XOAchievementState
            {
                id = id,
                title = title,
                description = desc,
                currentValue = XOMath.Clamp(cur, 0, target),
                targetValue = target,
                rewardCoins = coins,
                bonusItemLabel = bonusLabel,
                isClaimed = XOStorage.GetInt("XO_AchClaimed_" + id, 0) == 1
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
            XOStorage.SetInt("XO_AchClaimed_" + id, 1);
            XOStorage.Save();
        }

        public static int PlayerELO => 600 + TotalStars * 60 + WinStreak * 25;

        public static int ChestProgress
        {
            get => XOStorage.GetInt("XO_ChestProgress", 0);
            set
            {
                XOStorage.SetInt("XO_ChestProgress", XOMath.Clamp(value, 0, 3));
                XOStorage.Save();
            }
        }

        public static int CurrentPuzzleStage
        {
            get => XOStorage.GetInt("XO_PuzzleStage", 1);
            set
            {
                XOStorage.SetInt("XO_PuzzleStage", XOMath.Max(1, value));
                XOStorage.Save();
            }
        }

        public static int CurrentMissionIndex
        {
            get => XOStorage.GetInt("XO_MissionIdx", 0) % XOProgressionCatalog.Missions.Length;
            set
            {
                XOStorage.SetInt("XO_MissionIdx", value % XOProgressionCatalog.Missions.Length);
                XOStorage.Save();
            }
        }

        public static RankInfo GetCurrentRank(out int starsInTier, out int tierTargetStars)
        {
            var ranks = XOProgressionCatalog.Ranks;
            int s = TotalStars;
            for (int i = 0; i < ranks.Length; i++)
            {
                if (s < ranks[i].maxStars || i == ranks.Length - 1)
                {
                    starsInTier = s - ranks[i].minStars;
                    tierTargetStars = XOMath.Max(1, ranks[i].maxStars - ranks[i].minStars);
                    return ranks[i];
                }
            }
            starsInTier = s;
            tierTargetStars = 5;
            return ranks[0];
        }

        public static List<XOLeaderboardEntry> GetTop10Leaderboard(out int humanRankPosition)
        {
            var list = new List<XOLeaderboardEntry>();
            for (int i = 0; i < Bots.Length; i++)
            {
                XOBotProfile b = Bots[i];
                list.Add(new XOLeaderboardEntry
                {
                    playerName = b.botName,
                    titleTag = b.styleTitle,
                    eloRating = b.eloRating,
                    stars = XOMath.Max(1, (b.eloRating - 500) / 60),
                    isHumanPlayer = false,
                    botIndexIfRival = i
                });
            }

            list.Add(new XOLeaderboardEntry { playerName = "Alex_Grid99",  titleTag = "Silver Contender", eloRating = 720,  stars = 3,  isHumanPlayer = false, botIndexIfRival = 1 });
            list.Add(new XOLeaderboardEntry { playerName = "NovaBlade_XO", titleTag = "Gold Tactician",   eloRating = 1250, stars = 11, isHumanPlayer = false, botIndexIfRival = 3 });
            list.Add(new XOLeaderboardEntry { playerName = "ZenithKing",   titleTag = "Diamond Ace",      eloRating = 1750, stars = 19, isHumanPlayer = false, botIndexIfRival = 4 });

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

            list.Sort((a, b) => b.eloRating.CompareTo(a.eloRating));

            humanRankPosition = 10;
            for (int i = 0; i < list.Count; i++)
            {
                XOLeaderboardEntry entry = list[i];
                entry.rankPosition = i + 1;
                list[i] = entry;
                if (entry.isHumanPlayer)
                    humanRankPosition = i + 1;
            }
            return list;
        }

        public static XOMissionData GetActiveMission()
            => XOProgressionCatalog.Missions[CurrentMissionIndex];

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

            if (!passed) return false;
            CurrentMissionIndex = (CurrentMissionIndex + 1) % XOProgressionCatalog.Missions.Length;
            return true;
        }

        public static XOPuzzleStage GeneratePuzzleStage(int stageNumber, int boardSize)
            => XOPuzzleCatalog.GenerateStage(stageNumber, boardSize);
    }
}

