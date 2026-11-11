using System;
using System.Collections;
using UnityEngine;
using XOPlayable.Ads;
using XOPlayable.Core;
using XOPlayable.Visuals;

namespace XOPlayable.UI
{
    /// <summary>
    /// Processes Win/Loss/Draw outcomes, Phoenix Sigil intercepts, Second-Chance prompts,
    /// progression updates (Stars, Streak, Quests, Bot Bounties), and delayed Result modal display.
    /// </summary>
    public sealed class XOMatchOutcomeService
    {
        private readonly MonoBehaviour _host;
        private readonly XOCaroEngine _engine;
        private readonly XOGameStateStore _store;
        private readonly XOAudioManager _audio;
        private readonly XOBattlePresenter _battle;
        private readonly XOBattleEffectsPresenter _effects;
        private readonly XOResultModalsPresenter _resultModals;
        private readonly Func<XOMatchConfig> _getConfig;
        private readonly Action _refreshAllHeaderAndLobby;

        private Coroutine _modalDelayCoroutine;

        public XOMatchOutcomeService(
            MonoBehaviour host,
            XOCaroEngine engine,
            XOGameStateStore store,
            XOAudioManager audio,
            XOBattlePresenter battle,
            XOBattleEffectsPresenter effects,
            XOResultModalsPresenter resultModals,
            Func<XOMatchConfig> getConfig,
            Action refreshAllHeaderAndLobby)
        {
            _host = host;
            _engine = engine;
            _store = store;
            _audio = audio;
            _battle = battle;
            _effects = effects;
            _resultModals = resultModals;
            _getConfig = getConfig;
            _refreshAllHeaderAndLobby = refreshAllHeaderAndLobby;
        }

        public void StopPendingModalCoroutines()
        {
            if (_modalDelayCoroutine != null)
            {
                _host.StopCoroutine(_modalDelayCoroutine);
                _modalDelayCoroutine = null;
            }
        }

        public void PromptOutOfPuzzleMovesRevive()
        {
            _audio.PlaySfx("lose");
            _store.IsOutOfMovesPuzzleRevive = true;
            _resultModals.ShowRevivePrompt(
                "OUT OF MOVES!",
                $"You ran out of moves on {_store.CurrentPuzzle?.title}!\nWatch a short ad to get +3 EXTRA MOVES and a winning Hint!",
                "WATCH AD: +3 MOVES & HINT!");
        }

        public void HandleMatchEnded(MatchState endState)
        {
            XOMatchConfig cfg = _getConfig();
            _effects.DrawWinStrikeLineIfApplicable(_engine);

            if (endState == MatchState.XWon)
            {
                _store.ScoreX++;
                _audio.PlaySfx("win");
                if (cfg.HapticsEnabled) XOPlayableAdsBridge.Instance?.TriggerHaptic(50);
                _effects.SpawnVictoryConfettiBurst();
                _battle.StatusBannerText.text = $"AWESOME! Player X connected {_engine.WinLength} in a row!";
                ScheduleDelayedModal(0.65f, ProcessPlayerVictoryConsequences);
            }
            else if (endState == MatchState.OWon)
            {
                HandleOpponentWin(cfg);
            }
            else if (endState == MatchState.Draw)
            {
                _audio.PlaySfx("undo");
                _battle.StatusBannerText.text = "DRAW GAME! Well played by both sides!";
                ScheduleDelayedModal(0.55f, () => _resultModals.ShowMatchResult(MatchState.Draw, cfg.Mode, _engine.BoardSize, _store.CurrentPuzzle, ""));
            }

            _battle.RefreshScoresOnly();
        }

        private void HandleOpponentWin(XOMatchConfig cfg)
        {
            if (cfg.HapticsEnabled) XOPlayableAdsBridge.Instance?.TriggerHaptic(40);

            if (cfg.Mode == GameMode.TowerClimb && XORoguelikeRunManager.HasRelic(CaroRelicType.VampireFang) && !XORoguelikeRunManager.PhoenixSigilTriggeredThisMatch)
            {
                XORoguelikeRunManager.PhoenixSigilTriggeredThisMatch = true;
                if (_engine.IncinerateLastWinningPiece(out XOCellPos burntTile, freezeTurns: 2))
                {
                    _audio.PlaySfx("powerup");
                    if (cfg.HapticsEnabled) XOPlayableAdsBridge.Instance?.TriggerHaptic(75);
                    _effects.SpawnCellVFXBurst(burntTile.x, burntTile.y, XOProceduralAssets.ColCoral, XOProceduralAssets.ColSun, 24);
                    _effects.SpawnTacticalCalloutBanner("PHOENIX SIGIL!", "Holy flames incinerated the Bot's winning piece! Run saved!", XOProceduralAssets.ColSun);
                    _battle.RefreshAllCells(animateLastMove: false);
                    _battle.RefreshTurnAndScore(cfg.Mode, cfg.TurnTimeLimit);
                    _battle.RefreshBattleRelicBar(cfg.Mode);
                    return;
                }
            }

            if (cfg.Mode != GameMode.TowerClimb && cfg.IsVsAI && !_store.ReviveUsedThisMatch)
            {
                _audio.PlaySfx("lose");
                _store.IsOutOfMovesPuzzleRevive = false;
                _battle.StatusBannerText.text = $"AI connected {_engine.WinLength}! Use Second Chance to block it now!";
                ScheduleDelayedModal(0.6f, () => _resultModals.ShowRevivePrompt(
                    "SECOND CHANCE!",
                    $"AI just connected {_engine.WinLength} in a row!\nWatch a short ad to UNDO its winning move\nand highlight the exact square to block!",
                    "WATCH AD: REVIVE & BLOCK NOW!"));
            }
            else
            {
                _store.ScoreO++;
                _audio.PlaySfx(cfg.Mode == GameMode.TwoPlayerLocal ? "win" : "lose");
                ScheduleDelayedModal(0.65f, ProcessPlayerDefeatConsequences);
            }
        }

        private void ProcessPlayerVictoryConsequences()
        {
            XOMatchConfig cfg = _getConfig();
            XOProgressionSystem.TotalMatchesWon++;
            XOProgressionSystem.WinStreak++;
            int streak = XOProgressionSystem.WinStreak;
            int starsEarned = (streak >= 3) ? 2 : 1;
            XOProgressionSystem.TotalStars += starsEarned;
            XOProgressionSystem.ChestProgress = Mathf.Min(3, XOProgressionSystem.ChestProgress + 1);

            string extraNotes = $"+{starsEarned} Rank Star(s)   |   Win Streak: {streak}";

            if (cfg.Mode == GameMode.VsAI)
            {
                XOBotProfile bot = XOProgressionSystem.GetActiveBot();
                if (XOProgressionSystem.MarkBotConquered(XOProgressionSystem.SelectedBotIndex))
                {
                    _store.Coins += 80;
                    extraNotes += $"\nFirst-Time Victory vs {bot.botName}: +80 Bounty Coins! ({XOProgressionSystem.GetConqueredBotsCount()}/6 Rivals)";
                }
            }
            else if (cfg.Mode == GameMode.TowerClimb)
            {
                TowerFloorBlind blind = XORoguelikeRunManager.GetActiveBlind();
                XOProgressionSystem.MarkTowerFloorCleared(blind.floorNumber);
                extraNotes += $"\nFloor #{blind.floorNumber} ({blind.blindName}) Conquered!";
                if (XORoguelikeRunManager.HasRelic(CaroRelicType.CrownOfOmni))
                {
                    _store.Coins += 25;
                    extraNotes += "\nCrown of Omni: +25 Divine Coins!";
                }
            }
            else if (cfg.Mode == GameMode.PuzzleStage)
            {
                int clearedStage = XOProgressionSystem.CurrentPuzzleStage;
                XOProgressionSystem.CurrentPuzzleStage++;
                extraNotes += $"\nStage #{clearedStage} Cleared! Unlocked Stage #{XOProgressionSystem.CurrentPuzzleStage}!";
            }

            bool isDiag = _engine.IsWinningLineDiagonal();
            bool isHardOrPuzzle = cfg.Mode == GameMode.PuzzleStage || cfg.Difficulty == AIDifficulty.Hard;
            if (XOProgressionSystem.CheckAndAdvanceMission(_engine.GetPlayerMadeMovesCount(), isDiag, _store.UsedPowerUpThisMatch, streak, isHardOrPuzzle, out XOMissionData doneMission))
            {
                _store.Coins += doneMission.rewardCoins;
                extraNotes += $"\nQuest Complete '{doneMission.description}': +{doneMission.rewardCoins} Coins!";
            }

            _refreshAllHeaderAndLobby();
            XOPlayableAdsBridge.Instance?.ReportPlayableScore(XOProgressionSystem.TotalStars * 100 + _store.Coins);
            _resultModals.ShowMatchResult(MatchState.XWon, cfg.Mode, _engine.BoardSize, _store.CurrentPuzzle, extraNotes);
        }

        public void ProcessPlayerDefeatConsequences()
        {
            XOMatchConfig cfg = _getConfig();
            if (cfg.Mode == GameMode.TowerClimb)
            {
                TowerFloorBlind blind = XORoguelikeRunManager.GetActiveBlind();
                bool runOver = XORoguelikeRunManager.LoseHeart();
                string towerNote = runOver
                    ? $"Run Ended (0 Hearts)! Defeated on Floor {blind.floorNumber} by {blind.bossName}.\nSeed: {XORoguelikeRunManager.FormattedSeed} • Relics Lost • Run Coins: +{XORoguelikeRunManager.RunTotalCoinsEarned}\nRelics & Floor progression reset for next run."
                    : $"Lost 1 Heart! ({XORoguelikeRunManager.Lives}/{XORoguelikeRunManager.MaxLives} remaining)\nTap Rematch to fight {blind.bossName} again!";
                _resultModals.ShowMatchResult(MatchState.OWon, cfg.Mode, _engine.BoardSize, _store.CurrentPuzzle, towerNote);
                return;
            }

            _store.LostStreakToRestore = XOProgressionSystem.WinStreak;
            if (XOProgressionSystem.TotalStars > 0) XOProgressionSystem.TotalStars--;
            XOProgressionSystem.WinStreak = 0;

            _refreshAllHeaderAndLobby();
            string note = (_store.LostStreakToRestore > 0)
                ? $"Lost your {_store.LostStreakToRestore}-Win Streak & -1 Rank Star!\n(Use Rank Shield below to restore them!)"
                : "-1 Rank Star. Use Rank Shield below to protect your star!";

            _resultModals.ShowMatchResult(MatchState.OWon, cfg.Mode, _engine.BoardSize, _store.CurrentPuzzle, note);
        }

        public void ScheduleDelayedModal(float delay, Action action)
        {
            StopPendingModalCoroutines();
            _modalDelayCoroutine = _host.StartCoroutine(ShowModalAfterDelayRoutine(delay, action));
        }

        private static IEnumerator ShowModalAfterDelayRoutine(float delay, Action action)
        {
            yield return new WaitForSeconds(delay);
            action?.Invoke();
        }
    }
}

