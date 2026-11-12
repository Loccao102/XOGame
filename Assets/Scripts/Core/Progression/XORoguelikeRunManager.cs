using System.Collections.Generic;

namespace XOPlayable.Core
{
    /// <summary>
    /// Shared static convenience facade delegating to <see cref="DefaultRun"/> (<see cref="XORoguelikeRunState"/>).
    /// UI presenters and legacy callers may use this facade while XOCaroEngine operates purely on an injected RunContext.
    /// </summary>
    public static class XORoguelikeRunManager
    {
        public const int MaxLives = XORoguelikeRunState.MaxLives;
        public const int MaxRelicSlots = XORoguelikeRunState.MaxRelicSlots;

        public static XORoguelikeRunState DefaultRun { get; } = new XORoguelikeRunState();

        public static TowerFloorBlind[] FloorBlinds => XORoguelikeCatalog.FloorBlinds;

        public static int CurrentRunSeed
        {
            get => DefaultRun.CurrentRunSeed;
            set => DefaultRun.CurrentRunSeed = value;
        }

        public static string FormattedSeed => DefaultRun.FormattedSeed;

        public static int Lives
        {
            get => DefaultRun.Lives;
            set => DefaultRun.Lives = value;
        }

        public static int CurrentFloor
        {
            get => DefaultRun.CurrentFloor;
            set => DefaultRun.CurrentFloor = value;
        }

        public static bool IsRunActive
        {
            get => DefaultRun.IsRunActive;
            set => DefaultRun.IsRunActive = value;
        }

        public static int RunTotalCoinsEarned
        {
            get => DefaultRun.RunTotalCoinsEarned;
            set => DefaultRun.RunTotalCoinsEarned = value;
        }

        public static List<CaroRelicType> EquippedRelics => DefaultRun.EquippedRelics;

        public static int PlayerMovesThisMatch
        {
            get => DefaultRun.PlayerMovesThisMatch;
            set => DefaultRun.PlayerMovesThisMatch = value;
        }

        public static bool TwinThreatTriggeredThisMatch
        {
            get => DefaultRun.TwinThreatTriggeredThisMatch;
            set => DefaultRun.TwinThreatTriggeredThisMatch = value;
        }

        public static bool GuardianAegisTriggeredThisMatch
        {
            get => DefaultRun.GuardianAegisTriggeredThisMatch;
            set => DefaultRun.GuardianAegisTriggeredThisMatch = value;
        }

        public static bool ShatterHammerTriggeredThisMatch
        {
            get => DefaultRun.ShatterHammerTriggeredThisMatch;
            set => DefaultRun.ShatterHammerTriggeredThisMatch = value;
        }

        public static bool PhoenixSigilTriggeredThisMatch
        {
            get => DefaultRun.PhoenixSigilTriggeredThisMatch;
            set => DefaultRun.PhoenixSigilTriggeredThisMatch = value;
        }

        public static CaroRelicType? DisabledRelicThisMatch
        {
            get => DefaultRun.DisabledRelicThisMatch;
            set => DefaultRun.DisabledRelicThisMatch = value;
        }

        public static bool IsTowerMatchActive
        {
            get => DefaultRun.IsTowerMatchActive;
            set => DefaultRun.IsTowerMatchActive = value;
        }

        public static CaroRelic GetRelicData(CaroRelicType type) => XORoguelikeCatalog.GetRelicData(type);
        public static TowerFloorBlind GetActiveBlind() => DefaultRun.GetActiveBlind();
        public static int GetMoveLimit(TowerFloorBlind blind) => DefaultRun.GetMoveLimit(blind);
        public static BossOmen GetActiveOmen() => DefaultRun.GetActiveOmen();
        public static void StartNewRun(int? customSeed = null) => DefaultRun.StartNewRun(customSeed);
        public static void ResetMatchState() => DefaultRun.ResetMatchState();
        public static bool HasRelic(CaroRelicType type) => DefaultRun.HasRelic(type);
        public static bool EquipRelic(CaroRelicType type) => DefaultRun.EquipRelic(type);
        public static bool SellRelic(CaroRelicType type, out int refundCoins) => DefaultRun.SellRelic(type, out refundCoins);
        public static bool LoseHeart() => DefaultRun.LoseHeart();
        public static void RestoreHeart() => DefaultRun.RestoreHeart();
        public static List<CaroRelic> GetRandomRelicDraft(int count = 3, int rerollCount = 0) => DefaultRun.GetRandomRelicDraft(count, rerollCount);
        public static int GetFloorSeed(int floor) => DefaultRun.GetFloorSeed(floor);
    }
}

