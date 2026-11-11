using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using XOPlayable.Ads;
using XOPlayable.Core;
using XOPlayable.Visuals;

namespace XOPlayable.UI
{
    public partial class XOGameController
    {
        // =====================================================================
        // SKILLS & REWARDED AD ACTIONS: UNDO, HINT, BOMB, FREEZE, CHEST
        // =====================================================================

        private void OnUndoButtonClicked()
        {
            if (_isAIThinking) return;
            _skillTargeting = ActiveSkillTargeting.None;

            if (!_engine.HasUndoablePlayerMove())
            {
                _statusBannerText.text = "No moves to Undo yet!";
                PlaySfx("click");
                return;
            }

            if (UndoStock > 0)
            {
                UndoStock--;
                PerformUndoAction();
            }
            else
            {
                PlaySfx("click");
                XOPlayableAdsBridge.Instance.ShowRewardedAd(
                    "Get +2 Undo Moves & Undo Immediately (+25 Coins)",
                    onRewarded: () =>
                    {
                        PlaySfx("reward");
                        Coins += 25;
                        UndoStock += 1;
                        PerformUndoAction();
                    });
            }
        }

        private void PerformUndoAction()
        {
            if (_engine.UndoMove(currentGameMode, CellPiece.X))
            {
                if (currentGameMode == GameMode.PuzzleStage && _currentPuzzle != null)
                {
                    _puzzleMovesLeft = Mathf.Min(_currentPuzzle.maxPlayerMoves, _puzzleMovesLeft + 1);
                }

                PlaySfx("undo");
                _activeHintCell = new Vector2Int(-1, -1);
                _turnTimerRemaining = turnTimeLimit;
                if (_winStrikeLineImg != null)
                    _winStrikeLineImg.gameObject.SetActive(false);

                RefreshAllCells(animateLastMove: false);
                RefreshTurnAndScoreUI();
                _statusBannerText.text = "Move Undone! Your turn to play.";
            }
        }

        private void OnHintButtonClicked()
        {
            if (_isAIThinking || _engine.State != MatchState.Playing)
                return;

            _skillTargeting = ActiveSkillTargeting.None;

            if (HintStock > 0)
            {
                HintStock--;
                ActivateTacticalHint();
            }
            else
            {
                PlaySfx("click");
                XOPlayableAdsBridge.Instance.ShowRewardedAd(
                    "Get +2 AI Hints & Reveal Best Move (+25 Coins)",
                    onRewarded: () =>
                    {
                        PlaySfx("reward");
                        Coins += 25;
                        HintStock += 1;
                        ActivateTacticalHint();
                    });
            }
        }

        private void ActivateTacticalHint()
        {
            CellPiece targetPiece = _engine.CurrentTurn;
            Vector2Int bestCell = _engine.CalculateBestHint(targetPiece, out string reason);
            _activeHintCell = bestCell;

            PlaySfx("hint");
            RefreshAllCells(animateLastMove: false);
            _statusBannerText.text = $"AI HINT: {reason} (Flashing Gold Target)";
        }

        private void OnBombSkillClicked()
        {
            if (_isAIThinking || _engine.State != MatchState.Playing)
                return;

            if (_skillTargeting == ActiveSkillTargeting.BombDestroy)
            {
                _skillTargeting = ActiveSkillTargeting.None;
                RefreshAllCells(animateLastMove: false);
                RefreshTurnAndScoreUI();
                return;
            }

            if (BombStock > 0)
            {
                EnterBombTargetingMode();
            }
            else
            {
                PlaySfx("click");
                XOPlayableAdsBridge.Instance.ShowRewardedAd(
                    "Get +2 Bombs (Destroy any Opponent O piece on the board)",
                    onRewarded: () =>
                    {
                        PlaySfx("reward");
                        BombStock += 2;
                        EnterBombTargetingMode();
                    });
            }
        }

        private void EnterBombTargetingMode()
        {
            bool hasOpponentPiece = false;
            for (int r = 0; r < _engine.BoardSize; r++)
                for (int c = 0; c < _engine.BoardSize; c++)
                    if (_engine.Board[r, c] == CellPiece.O) hasOpponentPiece = true;

            if (!hasOpponentPiece)
            {
                _statusBannerText.text = "No Opponent (O) pieces on the board to destroy yet!";
                PlaySfx("click");
                return;
            }

            _skillTargeting = ActiveSkillTargeting.BombDestroy;
            PlaySfx("click");
            RefreshAllCells(animateLastMove: false);
            _statusBannerText.text = "BOMB MODE: Tap any pulsing red Opponent (O) piece to DESTROY it!";
        }

        private void OnFreezeSkillClicked()
        {
            if (_isAIThinking || _engine.State != MatchState.Playing)
                return;

            if (_skillTargeting == ActiveSkillTargeting.FreezeTile)
            {
                _skillTargeting = ActiveSkillTargeting.None;
                RefreshAllCells(animateLastMove: false);
                RefreshTurnAndScoreUI();
                return;
            }

            if (FreezeStock > 0)
            {
                _skillTargeting = ActiveSkillTargeting.FreezeTile;
                PlaySfx("click");
                _statusBannerText.text = "FREEZE MODE: Tap any empty square to LOCK it for 3 turns!";
            }
            else
            {
                PlaySfx("click");
                XOPlayableAdsBridge.Instance.ShowRewardedAd(
                    "Get +2 Freeze Skills (Lock empty squares to block AI)",
                    onRewarded: () =>
                    {
                        PlaySfx("reward");
                        FreezeStock += 2;
                        _skillTargeting = ActiveSkillTargeting.FreezeTile;
                        _statusBannerText.text = "FREEZE MODE: Tap any empty square to LOCK it for 3 turns!";
                    });
            }
        }

        private void OnSwapSkillClicked()
        {
            if (_isAIThinking || _engine.State != MatchState.Playing)
                return;

            if (_skillTargeting == ActiveSkillTargeting.SwapPiece)
            {
                _skillTargeting = ActiveSkillTargeting.None;
                RefreshAllCells(animateLastMove: false);
                RefreshTurnAndScoreUI();
                return;
            }

            if (SwapStock > 0)
            {
                EnterSwapTargetingMode();
            }
            else
            {
                PlaySfx("click");
                XOPlayableAdsBridge.Instance.ShowRewardedAd(
                    "Get +1 Swap Piece Skill (Convert any Opponent O into your X!)",
                    onRewarded: () =>
                    {
                        PlaySfx("reward");
                        SwapStock += 1;
                        EnterSwapTargetingMode();
                    });
            }
        }

        private void EnterSwapTargetingMode()
        {
            bool hasOpponentPiece = false;
            for (int r = 0; r < _engine.BoardSize; r++)
                for (int c = 0; c < _engine.BoardSize; c++)
                    if (_engine.Board[r, c] == CellPiece.O) hasOpponentPiece = true;

            if (!hasOpponentPiece)
            {
                _statusBannerText.text = "No Opponent (O) pieces on the board to swap yet!";
                PlaySfx("click");
                return;
            }

            _skillTargeting = ActiveSkillTargeting.SwapPiece;
            PlaySfx("click");
            RefreshAllCells(animateLastMove: false);
            _statusBannerText.text = "SWAP MODE: Tap any pulsing purple Opponent (O) piece to convert it into YOUR X!";
        }

        private void OnChestButtonClicked()
        {
            if (XOProgressionSystem.ChestProgress >= 3)
            {
                XOProgressionSystem.ChestProgress = 0;
                Coins += 100;
                BombStock += 1;
                FreezeStock += 1;
                SwapStock += 1;
                UndoStock += 1;
                HintStock += 1;
                PlaySfx("chest");
                RefreshHeaderAndProgressionUI();
                RefreshBottomActionBadges();
                _statusBannerText.text = "GOLD CHEST OPENED: +100 Coins, +1 Swap, +1 Bomb, +1 Freeze, +1 Undo & +1 Hint!";
            }
            else
            {
                PlaySfx("click");
                XOPlayableAdsBridge.Instance.ShowRewardedAd(
                    "Unlock Gold Chest Early (+100 Coins, +1 Swap, +1 Bomb, +1 Freeze)",
                    onRewarded: () =>
                    {
                        XOProgressionSystem.ChestProgress = 0;
                        Coins += 100;
                        BombStock += 1;
                        FreezeStock += 1;
                        SwapStock += 1;
                        UndoStock += 1;
                        PlaySfx("chest");
                        RefreshHeaderAndProgressionUI();
                        RefreshBottomActionBadges();
                        _statusBannerText.text = "CHEST UNLOCKED: +100 Coins, +1 Swap, +1 Bomb, +1 Freeze, +1 Undo!";
                    });
            }
        }

        private void OnConfirmReviveWithAd()
        {
            PlaySfx("click");

            if (_isOutOfMovesPuzzleRevive)
            {
                XOPlayableAdsBridge.Instance.ShowRewardedAd(
                    "Get +3 Extra Moves & Reveal Optimal Hint!",
                    onRewarded: () =>
                    {
                        _reviveModalRoot.SetActive(false);
                        _isOutOfMovesPuzzleRevive = false;
                        _puzzleMovesLeft += 3;
                        PlaySfx("reward");
                        ActivateTacticalHint();
                        RefreshTurnAndScoreUI();
                    });
                return;
            }

            XOPlayableAdsBridge.Instance.ShowRewardedAd(
                "Second Chance: Undo AI's Winning Move & Show Block!",
                onRewarded: () =>
                {
                    _reviveModalRoot.SetActive(false);
                    _reviveUsedThisMatch = true;
                    Coins += 25;

                    if (_engine.ReviveFromAILoss(out Vector2Int criticalCell))
                    {
                        PlaySfx("reward");
                        _activeHintCell = criticalCell;
                        if (_winStrikeLineImg != null)
                            _winStrikeLineImg.gameObject.SetActive(false);

                        RefreshAllCells(animateLastMove: false);
                        RefreshTurnAndScoreUI();
                        _statusBannerText.text = "REVIVED! Place on the flashing gold square now to block AI!";
                    }
                });
        }

        private void ShowMatchResultModal(MatchState state, string extraNotes)
        {
            _claimedDoubleReward = false;

            if (state == MatchState.XWon)
            {
                _lastMatchRewardCoins = (currentGameMode == GameMode.PuzzleStage)
                    ? (_currentPuzzle != null ? _currentPuzzle.rewardCoins : 40)
                    : (currentGameMode == GameMode.TowerClimb)
                    ? XORoguelikeRunManager.GetActiveBlind().coinReward
                    : ((currentBoardSize == 15) ? 20 : 15);

                Coins += _lastMatchRewardCoins;
                if (_resultHeroIconImg != null) _resultHeroIconImg.sprite = XOProceduralAssets.IconTrophy;

                if (currentGameMode == GameMode.TowerClimb)
                {
                    TowerFloorBlind blind = XORoguelikeRunManager.GetActiveBlind();
                    bool firstClear = XOProgressionSystem.MarkTowerFloorCleared(blind.floorNumber);
                    XORoguelikeRunManager.RunTotalCoinsEarned += _lastMatchRewardCoins;

                    if (blind.floorNumber == 10)
                    {
                        XOThemeDatabase.UnlockPieceSkin("king_dragon");
                    }

                    _resultTitleText.text = $"FLOOR {blind.floorNumber} CLEARED!";
                    _resultSubText.text = $"Defeated: {blind.bossName}\nReward: +{_lastMatchRewardCoins} Coins!\n(Draft extra Relics at the Caravan)";

                    if (_resultPlayAgainBtnText != null)
                    {
                        _resultPlayAgainBtnText.text = (blind.floorNumber < 10) ? $"CONTINUE TO FLOOR {blind.floorNumber + 1}" : "RETURN TO HUB";
                    }
                    if (_resultPlayAgainBtn != null)
                    {
                        _resultPlayAgainBtn.GetComponent<Image>().color = XOProceduralAssets.ColMint;
                    }
                }
                else
                {
                    _resultTitleText.text = (currentGameMode == GameMode.PuzzleStage) ? "PUZZLE CLEARED!" : "VICTORY!";
                    _resultTitleText.color = XOThemeDatabase.Hex("#FBBF24");
                    _resultSubText.text = $"Match Reward: +{_lastMatchRewardCoins} Coins\n{extraNotes}";

                    if (_resultPlayAgainBtnText != null)
                    {
                        _resultPlayAgainBtnText.text = (currentGameMode == GameMode.PuzzleStage) ? "NEXT PUZZLE" : "PLAY AGAIN";
                    }
                    if (_resultPlayAgainBtn != null)
                    {
                        _resultPlayAgainBtn.GetComponent<Image>().color = XOProceduralAssets.ColMint;
                    }
                }
                _resultTitleText.color = XOThemeDatabase.Hex("#FBBF24");
                _shieldAdBtn.gameObject.SetActive(false);
            }
            else if (state == MatchState.OWon)
            {
                _lastMatchRewardCoins = 15;
                Coins += _lastMatchRewardCoins;
                if (_resultHeroIconImg != null) _resultHeroIconImg.sprite = XOProceduralAssets.IconSwords;

                if (currentGameMode == GameMode.TowerClimb)
                {
                    TowerFloorBlind blind = XORoguelikeRunManager.GetActiveBlind();
                    _resultTitleText.text = "RUN ENDED (0 HEARTS)";
                    _resultTitleText.color = XOProceduralAssets.ColCoral;
                    _resultSubText.text = $"Defeated by: {blind.bossName}\nYour run has ended. Relics & floors have been reset to Floor 1.";

                    _shieldAdBtn.gameObject.SetActive(false); // 1-life sudden death: no revives
                    if (_resultPlayAgainBtnText != null)
                    {
                        _resultPlayAgainBtnText.text = "START NEW RUN (FLOOR 1)";
                    }
                    if (_resultPlayAgainBtn != null)
                    {
                        _resultPlayAgainBtn.GetComponent<Image>().color = XOProceduralAssets.ColCoral;
                    }
                }
                else
                {
                    _resultTitleText.text = (currentGameMode == GameMode.TwoPlayerLocal) ? "PLAYER O WINS!" : "DEFEAT";
                    _resultTitleText.color = XOThemeDatabase.Hex("#38BDF8");
                    _resultSubText.text = $"Consolation Reward: +{_lastMatchRewardCoins} Coins\n{extraNotes}";

                    if (_resultPlayAgainBtnText != null)
                    {
                        _resultPlayAgainBtnText.text = "PLAY AGAIN";
                    }
                    if (_resultPlayAgainBtn != null)
                    {
                        _resultPlayAgainBtn.GetComponent<Image>().color = XOProceduralAssets.ColMint;
                    }

                    bool showShield = (currentGameMode != GameMode.TwoPlayerLocal);
                    _shieldAdBtn.gameObject.SetActive(showShield);
                    if (showShield)
                    {
                        _shieldAdBtn.interactable = true;
                        _shieldAdBtn.GetComponent<Image>().color = XOThemeDatabase.Hex("#8B5CF6");
                        _shieldAdBtnText.text = (_lostStreakToRestore > 0)
                            ? $"WATCH AD: RESTORE {_lostStreakToRestore} WIN STREAK & +1 STAR"
                            : "WATCH AD: RANK SHIELD (+1 STAR)";
                    }
                }
            }
            else
            {
                _lastMatchRewardCoins = 25;
                Coins += _lastMatchRewardCoins;
                if (_resultHeroIconImg != null) _resultHeroIconImg.sprite = XOProceduralAssets.IconShield;
                _resultTitleText.text = "STALEMATE DRAW!";
                _resultTitleText.color = XOThemeDatabase.Hex("#A855F7");
                _resultSubText.text = (currentGameMode == GameMode.TowerClimb)
                    ? $"Draw against {XORoguelikeRunManager.GetActiveBlind().bossName}!\nNo heart lost. Replay this floor to advance!\nReward: +{_lastMatchRewardCoins} Coins"
                    : $"Stalemate Draw!\nReward: +{_lastMatchRewardCoins} Coins";
                _shieldAdBtn.gameObject.SetActive(false);

                if (_resultPlayAgainBtnText != null)
                {
                    _resultPlayAgainBtnText.text = (currentGameMode == GameMode.TowerClimb)
                        ? $"REMATCH VS {XORoguelikeRunManager.GetActiveBlind().bossName.ToUpper()}"
                        : "REMATCH";
                }
                if (_resultPlayAgainBtn != null)
                {
                    _resultPlayAgainBtn.GetComponent<Image>().color = XOProceduralAssets.ColMint;
                }
            }

            _doubleRewardAdBtn.interactable = true;
            _doubleRewardAdBtn.GetComponent<Image>().color = XOThemeDatabase.Hex("#F59E0B");
            _doubleRewardAdBtnText.text = $"WATCH AD: 2X COINS (+{_lastMatchRewardCoins} COINS)";

            if (_resultThirdActionBtn != null)
            {
                _resultThirdActionBtn.gameObject.SetActive(false);
            }

            _resultModalRoot.transform.SetAsLastSibling();
            _resultModalRoot.SetActive(true);
        }

        private void OnWatchAdActivateRankShield()
        {
            PlaySfx("click");

            if (currentGameMode == GameMode.TowerClimb)
            {
                XOPlayableAdsBridge.Instance.ShowRewardedAd(
                    "Restore +1 Heart (Keep your Roguelike Run alive!)",
                    onRewarded: () =>
                    {
                        PlaySfx("reward");
                        XORoguelikeRunManager.RestoreHeart();
                        _shieldAdBtn.interactable = false;
                        _shieldAdBtn.GetComponent<Image>().color = XOProceduralAssets.ColMuted;
                        _shieldAdBtnText.text = $"HEART RESTORED! ({XORoguelikeRunManager.Lives}/{XORoguelikeRunManager.MaxLives} HEARTS)";
                    });
                return;
            }

            XOPlayableAdsBridge.Instance.ShowRewardedAd(
                "Activate Rank Shield: Restore Win Streak & Rank Star!",
                onRewarded: () =>
                {
                    PlaySfx("reward");
                    XOProgressionSystem.TotalStars += 1;
                    if (_lostStreakToRestore > 0)
                    {
                        XOProgressionSystem.WinStreak = _lostStreakToRestore;
                    }

                    RefreshHeaderAndProgressionUI();
                    _shieldAdBtn.interactable = false;
                    _shieldAdBtn.GetComponent<Image>().color = XOThemeDatabase.Hex("#475569");
                    _shieldAdBtnText.text = "STREAK & RANK STAR RESTORED!";
                });
        }

        private void OnWatchAdDoubleMatchReward()
        {
            if (_claimedDoubleReward) return;
            PlaySfx("click");

            XOPlayableAdsBridge.Instance.ShowRewardedAd(
                $"Double Match Reward (+{_lastMatchRewardCoins} Coins)",
                onRewarded: () =>
                {
                    _claimedDoubleReward = true;
                    Coins += _lastMatchRewardCoins;
                    PlaySfx("reward");

                    _doubleRewardAdBtn.interactable = false;
                    _doubleRewardAdBtn.GetComponent<Image>().color = XOThemeDatabase.Hex("#475569");
                    _doubleRewardAdBtnText.text = "2X REWARD CLAIMED!";
                });
        }

        private void OnWatchAdForFreeCoins()
        {
            PlaySfx("click");
            XOPlayableAdsBridge.Instance.ShowRewardedAd(
                "Claim +100 Free Coins & +1 Bomb!",
                onRewarded: () =>
                {
                    PlaySfx("reward");
                    Coins += 100;
                    BombStock += 1;
                    _statusBannerText.text = "Claimed +100 Free Coins and +1 Bomb!";
                });
        }
    }
}
