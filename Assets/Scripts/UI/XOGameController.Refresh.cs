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
        // UI STATE REFRESHERS & WINNING STRIKE LINE
        // =====================================================================

        private void OnSelectVariantAndSize(XOVariantRule variant, int newSize, int newWinLen)
        {
            PlaySfx("click");
            if (currentGameMode == GameMode.PuzzleStage || currentGameMode == GameMode.TowerClimb)
            {
                currentGameMode = GameMode.VsAI;
            }
            XORoguelikeRunManager.IsTowerMatchActive = false;
            currentVariant = variant;
            currentBoardSize = newSize;
            winLength = newWinLen;
            StartNewMatch(newSize);
        }

        private void OnCycleAIRivalButtonClicked()
        {
            PlaySfx("click");
            if (currentGameMode != GameMode.VsAI)
            {
                currentGameMode = GameMode.VsAI;
            }
            else
            {
                int nextBot = (XOProgressionSystem.SelectedBotIndex + 1) % XOProgressionSystem.Bots.Length;
                XOProgressionSystem.SelectedBotIndex = nextBot;
            }
            XORoguelikeRunManager.IsTowerMatchActive = false;

            XOBotProfile bot = XOProgressionSystem.GetActiveBot();
            currentDifficulty = bot.baseDifficulty;
            _scoreX = 0;
            _scoreO = 0;
            StartNewMatch(currentBoardSize);
            if (enableLiveMatchmaking)
            {
                SpawnTacticalCalloutBanner("MATCHMAKING FOUND!", $"Entering Arena vs {bot.botName} [ELO {bot.eloRating}]", XOThemeDatabase.Hex("#0EA5E9"));
            }
        }

        private void OnSelectMode(GameMode mode, AIDifficulty diff)
        {
            PlaySfx("click");
            currentGameMode = mode;
            currentDifficulty = diff;
            XORoguelikeRunManager.IsTowerMatchActive = (mode == GameMode.TowerClimb);
            _scoreX = 0;
            _scoreO = 0;
            if (mode == GameMode.VsAI || mode == GameMode.TwoPlayerLocal)
            {
                currentBoardSize = 15;
                winLength = 5;
                currentVariant = XOVariantRule.ClassicCaro;
            }
            StartNewMatch(currentBoardSize);
        }

        private void RefreshSelectorButtonsUI()
        {
            Color activeCol = XOThemeDatabase.Hex("#E0F2FE"); // Soft pastel sky highlight for selected mode
            Color inactiveCol = Color.white; // Clean porcelain card for inactive
            bool isPuzzle = (currentGameMode == GameMode.PuzzleStage);

            if (_size10Btn != null) _size10Btn.GetComponent<Image>().color = (!isPuzzle && currentVariant == XOVariantRule.ClassicCaro && currentBoardSize == 10) ? activeCol : inactiveCol;
            if (_size15Btn != null) _size15Btn.GetComponent<Image>().color = (!isPuzzle && currentVariant == XOVariantRule.ClassicCaro && currentBoardSize == 15) ? activeCol : inactiveCol;
            if (_varInfinite3Btn != null) _varInfinite3Btn.GetComponent<Image>().color = (!isPuzzle && currentVariant == XOVariantRule.Infinite3x3) ? activeCol : inactiveCol;
            if (_varGobbletBtn != null) _varGobbletBtn.GetComponent<Image>().color = (!isPuzzle && currentVariant == XOVariantRule.GobbletStack) ? activeCol : inactiveCol;
            if (_varGravityBtn != null) _varGravityBtn.GetComponent<Image>().color = (!isPuzzle && currentVariant == XOVariantRule.GravityDrop) ? activeCol : inactiveCol;
            if (_varStrictBtn != null) _varStrictBtn.GetComponent<Image>().color = (!isPuzzle && currentVariant == XOVariantRule.StrictCaro) ? activeCol : inactiveCol;
            if (_varChaosBtn != null) _varChaosBtn.GetComponent<Image>().color = (!isPuzzle && currentVariant == XOVariantRule.ChaosMinefield) ? activeCol : inactiveCol;

            if (_modeAiTierBtn != null) _modeAiTierBtn.GetComponent<Image>().color = (currentGameMode == GameMode.VsAI) ? activeCol : inactiveCol;
            if (_mode2PBtn != null) _mode2PBtn.GetComponent<Image>().color = (currentGameMode == GameMode.TwoPlayerLocal) ? activeCol : inactiveCol;
            if (_modePuzzleBtn != null) _modePuzzleBtn.GetComponent<Image>().color = isPuzzle ? XOThemeDatabase.Hex("#FEE2E2") : inactiveCol;

            if (_modeAiTierBtnText != null && currentGameMode == GameMode.VsAI)
            {
                XOBotProfile bot = XOProgressionSystem.GetActiveBot();
                string tierTag = bot.baseDifficulty switch
                {
                    AIDifficulty.Easy => "EASY",
                    AIDifficulty.Normal => "NORMAL",
                    _ => "HARD"
                };
                _modeAiTierBtnText.text = $"AI: {tierTag}\n[{bot.botName}]";
            }

            if (_modePuzzleBtnText != null)
            {
                _modePuzzleBtnText.text = $"BOSS STAGE #{XOProgressionSystem.CurrentPuzzleStage}";
            }

            RefreshLobbyHeroUI();
        }

        public void RefreshHeaderAndProgressionUI()
        {
            if (_coinText != null)
                _coinText.text = $"{Coins} Coins";

            if (_luckySpinHeaderBtn != null && _luckySpinHeaderBtnText != null)
            {
                if (FreeSpinAvailable)
                {
                    _luckySpinHeaderBtnText.text = "FREE SPIN!";
                    _luckySpinHeaderBtn.GetComponent<Image>().color = XOThemeDatabase.Hex("#22C55E");
                }
                else
                {
                    _luckySpinHeaderBtnText.text = "LUCKY SPIN";
                    _luckySpinHeaderBtn.GetComponent<Image>().color = XOThemeDatabase.Hex("#F59E0B");
                }
            }

            if (_rankBadgeText != null)
            {
                RankInfo r = XOProgressionSystem.GetCurrentRank(out int starsInTier, out int tierTarget);
                _rankBadgeText.text = $"{r.title} ({starsInTier}/{tierTarget})";
            }

            if (_streakText != null)
            {
                int st = XOProgressionSystem.WinStreak;
                _streakText.text = $"Streak: {st}";
            }

            if (_missionText != null)
            {
                XOMissionData m = XOProgressionSystem.GetActiveMission();
                _missionText.text = $"QUEST: {m.description} (+{m.rewardCoins})";
            }

            if (_towerBadgeText != null)
            {
                _towerBadgeText.text = $"F.{XORoguelikeRunManager.CurrentFloor}";
            }

            int cp = XOProgressionSystem.ChestProgress;
            if (_chestProgressFillImg != null)
            {
                _chestProgressFillImg.fillAmount = Mathf.Clamp01(cp / 3f);
            }

            if (_chestButtonText != null && _chestButton != null)
            {
                if (cp >= 3)
                {
                    _chestButtonText.text = "OPEN GOLD CHEST NOW!";
                    _chestButton.GetComponent<Image>().color = XOThemeDatabase.Hex("#22C55E");
                }
                else
                {
                    _chestButtonText.text = $"WIN CHEST ({cp}/3) — UNLOCK";
                    _chestButton.GetComponent<Image>().color = XOThemeDatabase.Hex("#D97706");
                }
            }
        }

        private void RefreshTurnAndScoreUI()
        {
            RefreshScoreTextsOnly();

            bool isXTurn = (_engine.CurrentTurn == CellPiece.X);
            _playerXCardImg.color = isXTurn ? Color.Lerp(_activeTheme.panelSurfaceColor, _activeTheme.xColor, 0.28f) : _activeTheme.panelSurfaceColor;
            _playerOCardImg.color = !isXTurn ? Color.Lerp(_activeTheme.panelSurfaceColor, _activeTheme.oColor, 0.28f) : _activeTheme.panelSurfaceColor;

            _playerXTitleText.color = _activeTheme.xColor;
            _playerOTitleText.color = _activeTheme.oColor;
            _scoreXText.color = _activeTheme.primaryTextColor;
            _scoreOText.color = _activeTheme.primaryTextColor;

            // Update Scoreboard Token Icons to match equipped Piece Skin
            string skinId = (_activePieceSkin != null) ? _activePieceSkin.id : "classic_xo";
            if (_playerXTokenIcon != null)
            {
                _playerXTokenIcon.sprite = XOProceduralAssets.GetPieceSkinSprite(skinId, true);
                _playerXTokenIcon.color = (skinId == "classic_xo") ? _activeTheme.xColor : Color.white;
            }
            if (_playerOTokenIcon != null)
            {
                _playerOTokenIcon.sprite = XOProceduralAssets.GetPieceSkinSprite(skinId, false);
                _playerOTokenIcon.color = (skinId == "classic_xo") ? _activeTheme.oColor : Color.white;
            }

            if (currentGameMode == GameMode.PuzzleStage && _currentPuzzle != null)
            {
                _playerXTitleText.text = $"YOU ({_puzzleMovesLeft} Left)";
                _playerOTitleText.text = $"BOSS #{_currentPuzzle.stageNumber}";

                if (_engine.State == MatchState.Playing && _skillTargeting == ActiveSkillTargeting.None)
                {
                    _statusBannerText.text = _isAIThinking
                        ? "Stage Boss is countering..."
                        : $"{_currentPuzzle.title}: {_currentPuzzle.objectiveHint}";
                }
            }
            else if (currentGameMode == GameMode.TowerClimb)
            {
                TowerFloorBlind blind = XORoguelikeRunManager.GetActiveBlind();
                string movesSub = (blind.maxPlayerMoves > 0)
                    ? $" ({Mathf.Max(0, blind.maxPlayerMoves - XORoguelikeRunManager.CurrentMatch.PlayerMoves)} Moves)"
                    : $" (HP: {XORoguelikeRunManager.Lives}/{XORoguelikeRunManager.MaxLives})";

                string twinX = (_engine.RequiredWinLines > 1) ? $" [{_engine.CompletedWinLinesX.Count}/{_engine.RequiredWinLines} 5s]" : "";
                string twinO = (_engine.RequiredWinLines > 1) ? $" [{_engine.CompletedWinLinesO.Count}/{_engine.RequiredWinLines} 5s]" : "";

                _playerXTitleText.text = $"YOU{movesSub}{twinX}";
                _playerOTitleText.text = $"F.{blind.floorNumber} {blind.bossName.ToUpper()}{twinO}";

                if (_engine.State == MatchState.Playing && _skillTargeting == ActiveSkillTargeting.None)
                {
                    if (_isAIThinking)
                    {
                        _statusBannerText.text = $"{blind.bossName} ({blind.blindName}) is thinking...";
                    }
                    else
                    {
                        if (_engine.RequiredWinLines > 1)
                        {
                            _statusBannerText.text = $"Your Turn (X) vs {blind.bossName} — TWIN DRAGON: Connect TWO 5-in-a-rows ({_engine.CompletedWinLinesX.Count}/2) to Win!";
                        }
                        else
                        {
                            string limitNotice = (blind.maxPlayerMoves > 0)
                                ? $"[{Mathf.Max(0, blind.maxPlayerMoves - XORoguelikeRunManager.CurrentMatch.PlayerMoves)} moves left]"
                                : "Connect 5 to advance";
                            _statusBannerText.text = $"Your Turn (X) vs {blind.bossName} — {blind.blindName}: {limitNotice}";
                        }
                    }
                }
            }
            else if (currentGameMode == GameMode.VsAI)
            {
                XOBotProfile bot = XOProgressionSystem.GetActiveBot();
                _playerXTitleText.text = $"YOU [{XOProgressionSystem.PlayerELO}]";
                _playerOTitleText.text = $"{bot.botName.ToUpper()} [{bot.eloRating}]";

                if (_engine.State == MatchState.Playing && _skillTargeting == ActiveSkillTargeting.None)
                {
                    if (_isAIThinking)
                    {
                        _statusBannerText.text = $"{bot.botName} is calculating next move...";
                    }
                    else
                    {
                        _statusBannerText.text = _engine.VariantRule switch
                        {
                            XOVariantRule.Infinite3x3    => "Infinite 3x3: Max 3 pieces per side! 4th move vanishes oldest [FADE] piece!",
                            XOVariantRule.GobbletStack   => $"4x4 Gobblet: Tap smaller enemy piece (Lv.1/Lv.2) to EAT it! ({_engine.GobbleStockX} Gobbles left)",
                            XOVariantRule.GravityDrop    => "Gravity Drop (Connect-4): Tap any column to drop your piece to the bottom!",
                            XOVariantRule.StrictCaro     => "Strict Caro (Blocked-Ends): 5-in-a-row blocked on BOTH ends does NOT win!",
                            XOVariantRule.ChaosMinefield => "Chaos Minefield: Watch out for Blast Mines & Twin Clone Portals!",
                            _                            => $"Your Turn (X) vs {bot.botName} — Connect 5 to win!"
                        };
                    }
                }
            }
            else
            {
                _playerXTitleText.text = "PLAYER 1";
                _playerOTitleText.text = "PLAYER 2";

                if (_engine.State == MatchState.Playing && _skillTargeting == ActiveSkillTargeting.None)
                {
                    _statusBannerText.text = isXTurn
                        ? "Player 1's Turn (X)"
                        : "Player 2's Turn (O)";
                }
            }

            RefreshBattleRelicBarUI();
        }

        private void RefreshScoreTextsOnly()
        {
            if (_scoreXText != null) _scoreXText.text = _scoreX.ToString();
            if (_scoreOText != null) _scoreOText.text = _scoreO.ToString();
        }

        public void RefreshBottomActionBadges()
        {
            UpdateSingleBadge(_undoBadgeBg, _undoBadgeText, UndoStock);
            UpdateSingleBadge(_hintBadgeBg, _hintBadgeText, HintStock);
            UpdateSingleBadge(_bombBadgeBg, _bombBadgeText, BombStock);
            UpdateSingleBadge(_freezeBadgeBg, _freezeBadgeText, FreezeStock);
            UpdateSingleBadge(_swapBadgeBg, _swapBadgeText, SwapStock);

            if (_leaderboardBadgeBg != null && _leaderboardBadgeText != null)
            {
                if (XOProgressionSystem.HasAnyClaimableAchievement())
                {
                    _leaderboardBadgeText.text = "CLAIM!";
                    _leaderboardBadgeText.color = Color.white;
                    _leaderboardBadgeBg.color = XOThemeDatabase.Hex("#22C55E");
                }
                else
                {
                    _leaderboardBadgeText.text = "RANK";
                    _leaderboardBadgeText.color = XOThemeDatabase.Hex("#0F172A");
                    _leaderboardBadgeBg.color = XOThemeDatabase.Hex("#FEF08A");
                }
            }
        }

        private void UpdateSingleBadge(Image bg, Text txt, int count)
        {
            if (bg == null || txt == null) return;
            if (count > 0)
            {
                txt.text = $"{count}";
                txt.color = Color.white;
                bg.color = new Color(0.16f, 0.21f, 0.40f, 0.95f);
            }
            else
            {
                txt.text = "+AD";
                txt.color = XOProceduralAssets.ColInk;
                bg.color = XOProceduralAssets.ColSun;
            }
        }

        private void RefreshAllCells(bool animateLastMove)
        {
            MoveRecord? lastMove = _engine.GetLastMove();
            HashSet<Vector2Int> winSet = new HashSet<Vector2Int>(_engine.WinningCells);
            bool isBombTargetMode = (_skillTargeting == ActiveSkillTargeting.BombDestroy);
            bool isSwapTargetMode = (_skillTargeting == ActiveSkillTargeting.SwapPiece);

            int size = _engine.BoardSize;
            for (int r = 0; r < size; r++)
            {
                for (int c = 0; c < size; c++)
                {
                    int idx = r * size + c;
                    if (idx >= _cellViews.Count) continue;

                    bool isLast = lastMove.HasValue && lastMove.Value.row == r && lastMove.Value.col == c;
                    bool isWin = winSet.Contains(new Vector2Int(r, c));
                    bool isHint = (_activeHintCell.x == r && _activeHintCell.y == c);
                    bool isVanishingNext = _engine.IsPieceAboutToVanish(r, c);
                    int gobbleSize = (_engine.VariantRule == XOVariantRule.GobbletStack) ? _engine.PieceSizes[r, c] : 0;

                    _cellViews[idx].SetState(
                        _engine.Board[r, c],
                        _engine.SpecialTiles[r, c],
                        _engine.FrozenTurns[r, c],
                        _activeTheme,
                        _activePieceSkin,
                        isLast,
                        isWin,
                        isHint,
                        isBombTargetMode,
                        isSwapTargetMode,
                        isVanishingNext: isVanishingNext,
                        gobbleSize: gobbleSize,
                        animatePop: animateLastMove && isLast
                    );
                }
            }
        }
    }
}
