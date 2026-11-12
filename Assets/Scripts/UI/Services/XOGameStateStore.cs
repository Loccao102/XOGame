using System;
using UnityEngine;
using XOPlayable.Core;

namespace XOPlayable.UI
{
    public enum ActiveSkillTargeting
    {
        None = 0,
        BombDestroy = 1,
        FreezeTile = 2,
        SwapPiece = 3
    }

    public enum ScreenState
    {
        Lobby = 0,
        Battle = 1
    }

    /// <summary>Snapshot of match configuration parameters shared across match controllers.</summary>
    public readonly struct XOMatchConfig
    {
        public readonly GameMode Mode;
        public readonly XOVariantRule Variant;
        public readonly int BoardSize;
        public readonly int WinLength;
        public readonly AIDifficulty Difficulty;
        public readonly float TurnTimeLimit;
        public readonly bool HapticsEnabled;

        public XOMatchConfig(GameMode mode, XOVariantRule variant, int boardSize, int winLength, AIDifficulty difficulty, float turnTimeLimit, bool hapticsEnabled)
        {
            Mode = mode;
            Variant = variant;
            BoardSize = boardSize;
            WinLength = winLength;
            Difficulty = difficulty;
            TurnTimeLimit = turnTimeLimit;
            HapticsEnabled = hapticsEnabled;
        }

        public bool IsVsAI => Mode == GameMode.VsAI || Mode == GameMode.PuzzleStage || Mode == GameMode.TowerClimb;
    }

    /// <summary>
    /// Persistent player wallet, tactical skill inventory, active match flags, and cosmetic theme/skin state.
    /// </summary>
    public sealed class XOGameStateStore
    {
        private const string KeyCoins = "XO_Coins";
        private const string KeyHintStock = "XO_HintStock";
        private const string KeyUndoStock = "XO_UndoStock";
        private const string KeyBombStock = "XO_BombStock";
        private const string KeyFreezeStock = "XO_FreezeStock";
        private const string KeySwapStock = "XO_SwapStock";
        private const string KeyFreeSpinAvailable = "XO_FreeSpinAvailable";

        public event Action OnEconomyChanged;

        public int ScoreX { get; set; }
        public int ScoreO { get; set; }
        public bool IsAIThinking { get; set; }
        public bool ReviveUsedThisMatch { get; set; }
        public bool UsedPowerUpThisMatch { get; set; }
        public bool BotUsedCounterSkillThisMatch { get; set; }
        public int BotSkillUses { get; set; }
        public int BotLastSkillPly { get; set; }
        public bool AnnouncedFirstFiveX { get; set; }
        public bool AnnouncedFirstFiveO { get; set; }
        public bool MidMatchShiftTriggered { get; set; }
        public bool IsOutOfMovesPuzzleRevive { get; set; }
        public int LostStreakToRestore { get; set; }
        public bool ClaimedDoubleReward { get; set; }
        public int LastMatchRewardCoins { get; set; }
        public int PuzzleMovesLeft { get; set; }
        public float TurnTimerRemaining { get; set; }
        public ActiveSkillTargeting SkillTargeting { get; set; } = ActiveSkillTargeting.None;
        public XOCellPos ActiveHintCell { get; set; } = XOCellPos.Invalid;
        public XOPuzzleStage CurrentPuzzle { get; set; }

        public XOThemePalette ActiveTheme { get; private set; }
        public XOPieceSkin ActivePieceSkin { get; private set; }

        public XOGameStateStore()
        {
            ReloadCosmetics();
        }

        public int Coins
        {
            get => XOStorage.GetInt(KeyCoins, 150);
            set => SetStoredInt(KeyCoins, Mathf.Max(0, value));
        }

        public int HintStock
        {
            get => XOStorage.GetInt(KeyHintStock, 2);
            set => SetStoredInt(KeyHintStock, Mathf.Max(0, value));
        }

        public int UndoStock
        {
            get => XOStorage.GetInt(KeyUndoStock, 2);
            set => SetStoredInt(KeyUndoStock, Mathf.Max(0, value));
        }

        public int BombStock
        {
            get => XOStorage.GetInt(KeyBombStock, 1);
            set => SetStoredInt(KeyBombStock, Mathf.Max(0, value));
        }

        public int FreezeStock
        {
            get => XOStorage.GetInt(KeyFreezeStock, 1);
            set => SetStoredInt(KeyFreezeStock, Mathf.Max(0, value));
        }

        public int SwapStock
        {
            get => XOStorage.GetInt(KeySwapStock, 1);
            set => SetStoredInt(KeySwapStock, Mathf.Max(0, value));
        }

        public bool FreeSpinAvailable
        {
            get => XOStorage.GetInt(KeyFreeSpinAvailable, 1) == 1;
            set => SetStoredInt(KeyFreeSpinAvailable, value ? 1 : 0);
        }

        /// <summary>Reloads currently selected theme palette and piece skin from <see cref="XOThemeDatabase"/>.</summary>
        public void ReloadCosmetics()
        {
            var themes = XOThemeDatabase.Themes;
            int themeIdx = Mathf.Clamp(XOThemeDatabase.GetSelectedThemeIndex(), 0, themes.Length - 1);
            ActiveTheme = themes[themeIdx];

            var skins = XOThemeDatabase.PieceSkins;
            int skinIdx = Mathf.Clamp(XOThemeDatabase.GetSelectedPieceSkinIndex(), 0, skins.Length - 1);
            ActivePieceSkin = skins[skinIdx];
        }

        /// <summary>Resets per-match runtime flags when a new game starts.</summary>
        public void ResetMatchFlags(float turnTimeLimit)
        {
            IsAIThinking = false;
            ReviveUsedThisMatch = false;
            UsedPowerUpThisMatch = false;
            BotUsedCounterSkillThisMatch = false;
            BotSkillUses = 0;
            BotLastSkillPly = 0;
            AnnouncedFirstFiveX = false;
            AnnouncedFirstFiveO = false;
            MidMatchShiftTriggered = false;
            IsOutOfMovesPuzzleRevive = false;
            SkillTargeting = ActiveSkillTargeting.None;
            ActiveHintCell = XOCellPos.Invalid;
            TurnTimerRemaining = turnTimeLimit;
        }

        private void SetStoredInt(string key, int value)
        {
            XOStorage.SetInt(key, value);
            XOStorage.Save();
            OnEconomyChanged?.Invoke();
        }
    }
}
