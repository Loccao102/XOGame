using System;
using UnityEngine;
using XOPlayable.Ads;
using XOPlayable.Core;
using XOPlayable.Visuals;

namespace XOPlayable.UI
{
    /// <summary>
    /// Handles player tactical skills (Hint, Undo, Bomb, Freeze, Swap), targeting clicks on the board,
    /// gold chest/free coin rewards, and Rewarded-Ad Revive flows.
    /// </summary>
    public sealed class XOTacticalSkillsController
    {
        private readonly XOCaroEngine _engine;
        private readonly XOGameStateStore _store;
        private readonly XOAudioManager _audio;
        private readonly XOBattlePresenter _battle;
        private readonly XOBattleEffectsPresenter _effects;
        private readonly XOResultModalsPresenter _resultModals;
        private readonly Func<XOMatchConfig> _getConfig;
        private readonly Action _refreshAllHeaderAndLobby;
        private readonly Action<MatchState> _onMatchEnded;

        public XOTacticalSkillsController(
            XOCaroEngine engine,
            XOGameStateStore store,
            XOAudioManager audio,
            XOBattlePresenter battle,
            XOBattleEffectsPresenter effects,
            XOResultModalsPresenter resultModals,
            Func<XOMatchConfig> getConfig,
            Action refreshAllHeaderAndLobby,
            Action<MatchState> onMatchEnded)
        {
            _engine = engine;
            _store = store;
            _audio = audio;
            _battle = battle;
            _effects = effects;
            _resultModals = resultModals;
            _getConfig = getConfig;
            _refreshAllHeaderAndLobby = refreshAllHeaderAndLobby;
            _onMatchEnded = onMatchEnded;
        }

        /// <summary>Handles cell taps when a Bomb/Freeze/Swap targeting skill is active; returns true if handled.</summary>
        public bool TryHandleTargetedCellClick(int row, int col, GameMode mode)
        {
            if (HandleTowerSkillImmunity(mode)) return true;

            switch (_store.SkillTargeting)
            {
                case ActiveSkillTargeting.BombDestroy:
                    if (_engine.Board[row, col] == CellPiece.O)
                    {
                        _store.BombStock--;
                        _store.UsedPowerUpThisMatch = true;
                        _store.SkillTargeting = ActiveSkillTargeting.None;
                        _engine.BombDestroyPiece(row, col, CellPiece.O);
                        _audio.PlaySfx("bomb");
                        _effects.SpawnCellVFXBurst(row, col, XOUIColors.Red500, XOUIColors.Amber400, 16);
                        _battle.RefreshAllCells(animateLastMove: false);
                        _battle.RefreshBottomActionBadges();
                        _battle.StatusBannerText.text = "BOOM! Destroyed Opponent's O piece! Still your turn.";
                        TriggerBotSkillReaction(mode);
                    }
                    else
                    {
                        _battle.StatusBannerText.text = "Tap any pulsing Opponent (O) piece to DESTROY it!";
                        _audio.PlaySfx("click");
                    }
                    return true;

                case ActiveSkillTargeting.FreezeTile:
                    if (_engine.CanPlace(row, col))
                    {
                        _store.FreezeStock--;
                        _store.UsedPowerUpThisMatch = true;
                        _store.SkillTargeting = ActiveSkillTargeting.None;
                        _engine.FreezeEmptyCell(row, col, 3);
                        _audio.PlaySfx("freeze");
                        _effects.SpawnCellVFXBurst(row, col, XOUIColors.Sky400, Color.white, 14);
                        _battle.RefreshAllCells(animateLastMove: false);
                        _battle.RefreshBottomActionBadges();
                        _battle.StatusBannerText.text = "Tile frozen for 3 turns! Nobody can place on it.";
                        TriggerBotSkillReaction(mode);
                    }
                    else
                    {
                        _battle.StatusBannerText.text = "Tap any open EMPTY tile to freeze it!";
                        _audio.PlaySfx("click");
                    }
                    return true;

                case ActiveSkillTargeting.SwapPiece:
                    if (_engine.Board[row, col] == CellPiece.O)
                    {
                        _store.SwapStock--;
                        _store.UsedPowerUpThisMatch = true;
                        _store.SkillTargeting = ActiveSkillTargeting.None;
                        _engine.SwapOpponentPiece(row, col, CellPiece.O, CellPiece.X);
                        _audio.PlaySfx("swap");
                        _effects.SpawnCellVFXBurst(row, col, XOUIColors.Purple500, XOUIColors.Cyan400, 18);
                        _effects.SpawnTacticalCalloutBanner("PIECE SWAPPED! (O -> X)", "Converted enemy piece into your X!", XOUIColors.Purple600);
                        _battle.RefreshAllCells(animateLastMove: true);
                        _battle.RefreshBottomActionBadges();
                        if (_engine.State != MatchState.Playing)
                        {
                            _onMatchEnded?.Invoke(_engine.State);
                            return true;
                        }
                        _battle.StatusBannerText.text = "SWAPPED! Converted Opponent's O into your X! Still your turn.";
                        TriggerBotSkillReaction(mode);
                    }
                    else
                    {
                        _battle.StatusBannerText.text = "Tap any pulsing purple Opponent (O) piece to swap into X!";
                        _audio.PlaySfx("click");
                    }
                    return true;

                default:
                    return false;
            }
        }

        public void OnUndoClicked()
        {
            if (_store.IsAIThinking) return;
            _store.SkillTargeting = ActiveSkillTargeting.None;

            if (!_engine.HasUndoablePlayerMove())
            {
                _battle.StatusBannerText.text = "No moves to Undo yet!";
                _audio.PlaySfx("click");
                return;
            }

            if (_store.UndoStock > 0)
            {
                _store.UndoStock--;
                PerformUndoAction();
            }
            else
            {
                _audio.PlaySfx("click");
                XOPlayableAdsBridge.Instance.ShowRewardedAd(
                    "Get +2 Undo Moves & Undo Immediately (+25 Coins)",
                    onRewarded: () => { _audio.PlaySfx("reward"); _store.Coins += 25; _store.UndoStock += 1; PerformUndoAction(); });
            }
        }

        public void OnHintClicked()
        {
            if (_store.IsAIThinking || _engine.State != MatchState.Playing) return;
            _store.SkillTargeting = ActiveSkillTargeting.None;

            if (_store.HintStock > 0)
            {
                _store.HintStock--;
                ActivateTacticalHint();
            }
            else
            {
                _audio.PlaySfx("click");
                XOPlayableAdsBridge.Instance.ShowRewardedAd(
                    "Get +2 AI Hints & Reveal Best Move (+25 Coins)",
                    onRewarded: () => { _audio.PlaySfx("reward"); _store.Coins += 25; _store.HintStock += 1; ActivateTacticalHint(); });
            }
        }

        public void OnBombClicked()
            => ToggleTargetingSkill(ActiveSkillTargeting.BombDestroy, _store.BombStock, HasOpponentPiece,
                "No Opponent (O) pieces on the board to destroy yet!",
                "BOMB MODE: Tap any pulsing Opponent (O) piece to DESTROY it!",
                "Get +2 Bombs (Destroy any Opponent O piece on the board)",
                () => _store.BombStock += 2);

        public void OnSwapClicked()
            => ToggleTargetingSkill(ActiveSkillTargeting.SwapPiece, _store.SwapStock, HasOpponentPiece,
                "No Opponent (O) pieces on the board to swap yet!",
                "SWAP MODE: Tap any pulsing purple Opponent (O) piece to convert into YOUR X!",
                "Get +1 Swap Piece Skill (Convert any Opponent O into your X!)",
                () => _store.SwapStock += 1);

        public void OnFreezeClicked()
            => ToggleTargetingSkill(ActiveSkillTargeting.FreezeTile, _store.FreezeStock, null, string.Empty,
                "FREEZE MODE: Tap any empty square to LOCK it for 3 turns!",
                "Get +2 Freeze Skills (Lock empty squares to block AI)",
                () => _store.FreezeStock += 2);

        public void OnChestClicked()
        {
            Action grantChest = () =>
            {
                XOProgressionSystem.ChestProgress = 0;
                _store.Coins += 100;
                _store.BombStock += 1;
                _store.FreezeStock += 1;
                _store.SwapStock += 1;
                _store.UndoStock += 1;
                _store.HintStock += 1;
                _audio.PlaySfx("chest");
                _refreshAllHeaderAndLobby();
                _battle.StatusBannerText.text = "GOLD CHEST OPENED: +100 Coins & +1 All Tactical Skills!";
            };

            if (XOProgressionSystem.ChestProgress >= 3)
            {
                grantChest();
            }
            else
            {
                _audio.PlaySfx("click");
                XOPlayableAdsBridge.Instance.ShowRewardedAd("Unlock Gold Chest Early (+100 Coins & All Tactical Skills)", grantChest);
            }
        }

        public void OnFreeCoinsClicked()
        {
            _audio.PlaySfx("click");
            XOPlayableAdsBridge.Instance.ShowRewardedAd("Claim +100 Free Coins & +1 Bomb!", onRewarded: () =>
            {
                _audio.PlaySfx("reward");
                _store.Coins += 100;
                _store.BombStock += 1;
                _battle.StatusBannerText.text = "Claimed +100 Free Coins and +1 Bomb!";
            });
        }

        public void OnConfirmReviveWithAd()
        {
            _audio.PlaySfx("click");
            XOMatchConfig cfg = _getConfig();

            if (_store.IsOutOfMovesPuzzleRevive)
            {
                XOPlayableAdsBridge.Instance.ShowRewardedAd("Get +3 Extra Moves & Reveal Optimal Hint!", onRewarded: () =>
                {
                    _resultModals.ReviveModalRoot.SetActive(false);
                    _store.IsOutOfMovesPuzzleRevive = false;
                    _store.PuzzleMovesLeft += 3;
                    _audio.PlaySfx("reward");
                    ActivateTacticalHint();
                    _battle.RefreshTurnAndScore(cfg.Mode, cfg.TurnTimeLimit);
                });
                return;
            }

            XOPlayableAdsBridge.Instance.ShowRewardedAd("Second Chance: Undo AI's Winning Move & Show Block!", onRewarded: () =>
            {
                _resultModals.ReviveModalRoot.SetActive(false);
                _store.ReviveUsedThisMatch = true;
                _store.Coins += 25;

                if (_engine.ReviveFromAILoss(out XOCellPos criticalCell))
                {
                    _audio.PlaySfx("reward");
                    _store.ActiveHintCell = criticalCell;
                    _effects.HideWinStrikeLine();
                    _battle.RefreshAllCells(animateLastMove: false);
                    _battle.RefreshTurnAndScore(cfg.Mode, cfg.TurnTimeLimit);
                    _battle.StatusBannerText.text = "REVIVED! Place on the flashing gold square now to block AI!";
                }
            });
        }

        private bool HandleTowerSkillImmunity(GameMode mode)
        {
            if (mode != GameMode.TowerClimb) return false;
            TowerFloorBlind blind = XORoguelikeRunManager.GetActiveBlind();
            if (_store.SkillTargeting == ActiveSkillTargeting.BombDestroy && _engine.IsBotImmuneToSkill(blind.botEquipment, "Bomb"))
            {
                _audio.PlaySfx("lose");
                _effects.SpawnTacticalCalloutBanner("MIRROR AEGIS!", $"{blind.bossName} is immune to Bomb!", XOProceduralAssets.ColCoral);
                CancelTargeting();
                return true;
            }
            if (_store.SkillTargeting == ActiveSkillTargeting.SwapPiece && _engine.IsBotImmuneToSkill(blind.botEquipment, "Swap"))
            {
                _audio.PlaySfx("lose");
                _effects.SpawnTacticalCalloutBanner("MIRROR AEGIS!", $"{blind.bossName} is immune to Swap!", XOProceduralAssets.ColLilac);
                CancelTargeting();
                return true;
            }
            return false;
        }

        private void PerformUndoAction()
        {
            XOMatchConfig cfg = _getConfig();
            if (!_engine.UndoMove(cfg.Mode, CellPiece.X)) return;

            if (cfg.Mode == GameMode.PuzzleStage && _store.CurrentPuzzle != null)
            {
                _store.PuzzleMovesLeft = Mathf.Min(_store.CurrentPuzzle.maxPlayerMoves, _store.PuzzleMovesLeft + 1);
            }

            _audio.PlaySfx("undo");
            _store.ActiveHintCell = XOCellPos.Invalid;
            _store.TurnTimerRemaining = cfg.TurnTimeLimit;
            _effects.HideWinStrikeLine();
            _battle.RefreshAllCells(animateLastMove: false);
            _battle.RefreshTurnAndScore(cfg.Mode, cfg.TurnTimeLimit);
            _battle.StatusBannerText.text = "Move Undone! Your turn to play.";
        }

        private void ActivateTacticalHint()
        {
            _store.ActiveHintCell = _engine.CalculateBestHint(_engine.CurrentTurn, out string reason);
            _audio.PlaySfx("hint");
            _battle.RefreshAllCells(animateLastMove: false);
            _battle.StatusBannerText.text = $"AI HINT: {reason} (Flashing Gold Target)";
        }

        private void CancelTargeting()
        {
            _store.SkillTargeting = ActiveSkillTargeting.None;
            _battle.RefreshAllCells(animateLastMove: false);
            XOMatchConfig cfg = _getConfig();
            _battle.RefreshTurnAndScore(cfg.Mode, cfg.TurnTimeLimit);
        }

        private void ToggleTargetingSkill(
            ActiveSkillTargeting targetMode,
            int currentStock,
            Func<bool> precheck,
            string emptyWarning,
            string targetingPrompt,
            string adRewardDesc,
            Action grantStock)
        {
            if (_store.IsAIThinking || _engine.State != MatchState.Playing) return;
            if (_store.SkillTargeting == targetMode) { CancelTargeting(); return; }

            if (currentStock > 0)
            {
                if (precheck != null && !precheck())
                {
                    _battle.StatusBannerText.text = emptyWarning;
                    _audio.PlaySfx("click");
                    return;
                }
                _store.SkillTargeting = targetMode;
                _audio.PlaySfx("click");
                _battle.RefreshAllCells(animateLastMove: false);
                _battle.StatusBannerText.text = targetingPrompt;
            }
            else
            {
                _audio.PlaySfx("click");
                XOPlayableAdsBridge.Instance.ShowRewardedAd(adRewardDesc, onRewarded: () =>
                {
                    _audio.PlaySfx("reward");
                    grantStock();
                    if (precheck == null || precheck()) _store.SkillTargeting = targetMode;
                    _battle.RefreshAllCells(animateLastMove: false);
                    _battle.RefreshBottomActionBadges();
                    _battle.StatusBannerText.text = targetingPrompt;
                });
            }
        }

        private bool HasOpponentPiece()
        {
            for (int r = 0; r < _engine.BoardSize; r++)
                for (int c = 0; c < _engine.BoardSize; c++)
                    if (_engine.Board[r, c] == CellPiece.O) return true;
            return false;
        }

        private void TriggerBotSkillReaction(GameMode mode)
        {
            if (mode == GameMode.VsAI)
            {
                XOBotProfile bot = XOProgressionSystem.GetActiveBot();
                _effects.ShowBotReactionBubble($"{bot.botName}: \"{bot.PickRandom(bot.onPlayerSkillUsedLines)}\"");
            }
        }
    }
}

