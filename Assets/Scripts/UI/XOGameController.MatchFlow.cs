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
        // BOARD LOGIC, AI RIVALS, SPECIAL TILES & TACTICAL SKILLS
        // =====================================================================

        public void StartNewMatch(int boardSize)
        {
            StopAllCoroutines();
            _isAIThinking = false;
            _reviveUsedThisMatch = false;
            _usedPowerUpThisMatch = false;
            _botUsedCounterSkillThisMatch = false;
            _announcedFirstFiveX = false;
            _announcedFirstFiveO = false;
            _midMatchShiftTriggered = false;
            _isOutOfMovesPuzzleRevive = false;
            _skillTargeting = ActiveSkillTargeting.None;
            _activeHintCell = new Vector2Int(-1, -1);
            _turnTimerRemaining = turnTimeLimit;

            bool isTower = (currentGameMode == GameMode.TowerClimb);
            XORoguelikeRunManager.IsTowerMatchActive = isTower;

            if (currentGameMode == GameMode.PuzzleStage)
            {
                currentBoardSize = 15;
                winLength = 5;
                _engine.Initialize(currentBoardSize, winLength, spawnSpecialTiles: false, variant: XOVariantRule.ClassicCaro, isTowerClimb: false);
                _currentPuzzle = XOProgressionSystem.GeneratePuzzleStage(XOProgressionSystem.CurrentPuzzleStage, currentBoardSize);
                _engine.LoadPuzzleStage(_currentPuzzle);
                _puzzleMovesLeft = _currentPuzzle.maxPlayerMoves;
                _activeHintCell = _engine.CalculateBestHint(CellPiece.X, out _);
            }
            else
            {
                currentBoardSize = boardSize;
                // Only spawn Special Tiles in Tower of Anomalies mode or ChaosMinefield variant!
                // Normal matches are pure, competitive Gomoku (No coin tiles, no buggy extra-turns)
                bool spawnSpecials = isTower || (currentVariant == XOVariantRule.ChaosMinefield);
                int? specialSeed = isTower ? XORoguelikeRunManager.GetFloorSeed(XORoguelikeRunManager.CurrentFloor) : (int?)null;
                int floorNum = isTower ? XORoguelikeRunManager.CurrentFloor : 0;
                _engine.Initialize(currentBoardSize, winLength, spawnSpecialTiles: spawnSpecials, variant: currentVariant, specialTilesSeed: specialSeed, floorNumber: floorNum, isTowerClimb: isTower);
                currentBoardSize = _engine.BoardSize;
                winLength = _engine.WinLength;
            }

            if (_winStrikeLineImg != null)
                _winStrikeLineImg.gameObject.SetActive(false);

            RebuildBoardCellsIfNeeded(currentBoardSize);
            RefreshAllCells(animateLastMove: false);
            RefreshSelectorButtonsUI();
            RefreshHeaderAndProgressionUI();
            RefreshTurnAndScoreUI();
            RefreshBottomActionBadges();
            RefreshBattleRelicBarUI();
            UpdateResponsiveLayout(force: true);

            if (currentGameMode == GameMode.VsAI)
            {
                XOBotProfile bot = XOProgressionSystem.GetActiveBot();
                ShowBotReactionBubble($"{bot.botName}: \"{bot.PickRandom(bot.greetingLines)}\"");
            }
            else if (currentGameMode == GameMode.TowerClimb)
            {
                TowerFloorBlind blind = XORoguelikeRunManager.GetActiveBlind();
                ShowBotReactionBubble($"{blind.bossName}: \"Floor {blind.floorNumber} challenge: {blind.blindName}! Let's battle!\"");
            }
            else if (_botChatBubbleObj != null)
            {
                _botChatBubbleObj.SetActive(false);
            }
        }

        private void RebuildBoardCellsIfNeeded(int size)
        {
            int targetCount = size * size;

            if (_cellViews.Count != targetCount)
            {
                foreach (var cv in _cellViews)
                {
                    if (cv != null) Destroy(cv.gameObject);
                }
                _cellViews.Clear();

                for (int r = 0; r < size; r++)
                {
                    for (int c = 0; c < size; c++)
                    {
                        GameObject cellObj = new GameObject($"Cell_{r}_{c}", typeof(RectTransform), typeof(Image), typeof(Button), typeof(XOCellView));
                        cellObj.transform.SetParent(_boardGridContainerRT, false);

                        Image cellImg = cellObj.GetComponent<Image>();
                        cellImg.sprite = XOProceduralAssets.SoftCellSprite;
                        cellImg.type = Image.Type.Sliced;

                        XOCellView view = cellObj.GetComponent<XOCellView>();
                        view.Initialize(r, c, _uiFont, OnBoardCellClicked);
                        _cellViews.Add(view);
                    }
                }
            }

            ResizeBoardGridCells();
        }

        private void ResizeBoardGridCells()
        {
            if (_boardGridLayout == null || _boardOuterFrameRT == null) return;
            int size = Mathf.Max(1, currentBoardSize);
            float boardPixelWidth = _boardOuterFrameRT.sizeDelta.x - 28f;
            float spacing = (size <= 10) ? 5f : 3.5f;
            float totalSpacing = spacing * (size - 1);
            float cellSize = Mathf.Floor((boardPixelWidth - totalSpacing) / size);

            _boardGridLayout.spacing = new Vector2(spacing, spacing);
            _boardGridLayout.cellSize = new Vector2(cellSize, cellSize);
            _boardGridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            _boardGridLayout.constraintCount = size;
        }

        private void OnBoardCellClicked(int row, int col)
        {
            if (_isAIThinking || _engine.State != MatchState.Playing)
                return;

            // Check Boss Mirror Aegis / Omni Crown Immunity in Tower Mode
            if (currentGameMode == GameMode.TowerClimb)
            {
                TowerFloorBlind blind = XORoguelikeRunManager.GetActiveBlind();
                if (_engine.IsBotImmuneToSkill(blind.botEquipment, "Bomb") && _skillTargeting == ActiveSkillTargeting.BombDestroy)
                {
                    PlaySfx("lose");
                    SpawnTacticalCalloutBanner("MIRROR AEGIS!", $"{blind.bossName} is immune to Bomb!", XOProceduralAssets.ColCoral);
                    _skillTargeting = ActiveSkillTargeting.None;
                    RefreshAllCells(animateLastMove: false);
                    RefreshTurnAndScoreUI();
                    return;
                }
                if (_engine.IsBotImmuneToSkill(blind.botEquipment, "Swap") && _skillTargeting == ActiveSkillTargeting.SwapPiece)
                {
                    PlaySfx("lose");
                    SpawnTacticalCalloutBanner("MIRROR AEGIS!", $"{blind.bossName} is immune to Swap!", XOProceduralAssets.ColLilac);
                    _skillTargeting = ActiveSkillTargeting.None;
                    RefreshAllCells(animateLastMove: false);
                    RefreshTurnAndScoreUI();
                    return;
                }
            }

            // 1. Bomb Targeting Mode
            if (_skillTargeting == ActiveSkillTargeting.BombDestroy)
            {
                if (_engine.Board[row, col] == CellPiece.O)
                {
                    BombStock--;
                    _usedPowerUpThisMatch = true;
                    _skillTargeting = ActiveSkillTargeting.None;
                    _engine.BombDestroyPiece(row, col, CellPiece.O);
                    PlaySfx("bomb");
                    SpawnCellVFXBurst(row, col, XOThemeDatabase.Hex("#EF4444"), XOThemeDatabase.Hex("#FBBF24"), 16);
                    RefreshAllCells(animateLastMove: false);
                    RefreshBottomActionBadges();
                    _statusBannerText.text = "BOOM! Destroyed Opponent's O piece! Still your turn.";

                    if (currentGameMode == GameMode.VsAI)
                    {
                        XOBotProfile bot = XOProgressionSystem.GetActiveBot();
                        ShowBotReactionBubble($"{bot.botName}: \"{bot.PickRandom(bot.onPlayerSkillUsedLines)}\"");
                    }
                }
                else
                {
                    _statusBannerText.text = "Tap on any pulsing red Opponent (O) piece on the board to destroy it!";
                    PlaySfx("click");
                }
                return;
            }

            // 2. Freeze Targeting Mode
            if (_skillTargeting == ActiveSkillTargeting.FreezeTile)
            {
                if (_engine.CanPlace(row, col))
                {
                    FreezeStock--;
                    _usedPowerUpThisMatch = true;
                    _skillTargeting = ActiveSkillTargeting.None;
                    _engine.FreezeEmptyCell(row, col, 3);
                    PlaySfx("freeze");
                    SpawnCellVFXBurst(row, col, XOThemeDatabase.Hex("#38BDF8"), Color.white, 14);
                    RefreshAllCells(animateLastMove: false);
                    RefreshBottomActionBadges();
                    _statusBannerText.text = "Tile frozen for 3 turns! Nobody can place on it.";

                    if (currentGameMode == GameMode.VsAI)
                    {
                        XOBotProfile bot = XOProgressionSystem.GetActiveBot();
                        ShowBotReactionBubble($"{bot.botName}: \"{bot.PickRandom(bot.onPlayerSkillUsedLines)}\"");
                    }
                }
                else
                {
                    _statusBannerText.text = "Tap on an open EMPTY tile on the board to freeze it!";
                    PlaySfx("click");
                }
                return;
            }

            // 3. Swap Piece Targeting Mode (Convert Opponent O into Player X!)
            if (_skillTargeting == ActiveSkillTargeting.SwapPiece)
            {
                if (_engine.Board[row, col] == CellPiece.O)
                {
                    SwapStock--;
                    _usedPowerUpThisMatch = true;
                    _skillTargeting = ActiveSkillTargeting.None;
                    _engine.SwapOpponentPiece(row, col, CellPiece.O, CellPiece.X);
                    PlaySfx("swap");
                    SpawnCellVFXBurst(row, col, XOThemeDatabase.Hex("#A855F7"), XOThemeDatabase.Hex("#22D3EE"), 18);
                    SpawnTacticalCalloutBanner("PIECE SWAPPED! (O -> X)", "Converted enemy piece into your X!", XOThemeDatabase.Hex("#9333EA"));
                    RefreshAllCells(animateLastMove: true);
                    RefreshBottomActionBadges();

                    if (_engine.State != MatchState.Playing)
                    {
                        HandleMatchEnded(_engine.State);
                        return;
                    }

                    _statusBannerText.text = "SWAPPED! Converted Opponent's O into your X! Still your turn.";
                    if (currentGameMode == GameMode.VsAI)
                    {
                        XOBotProfile bot = XOProgressionSystem.GetActiveBot();
                        ShowBotReactionBubble($"{bot.botName}: \"{bot.PickRandom(bot.onPlayerSkillUsedLines)}\"");
                    }
                }
                else
                {
                    _statusBannerText.text = "Tap on any pulsing purple Opponent (O) piece to convert it into your X!";
                    PlaySfx("click");
                }
                return;
            }

            // 4. Normal Move Placement (Supports Gravity Drop column landing!)
            if (_engine.VariantRule == XOVariantRule.GravityDrop)
            {
                int landingRow = _engine.GetGravityLandingRow(col);
                if (landingRow < 0)
                {
                    _statusBannerText.text = "This column is full! Tap another column to drop your piece.";
                    PlaySfx("click");
                    return;
                }
                row = landingRow;
            }

            if (_engine.IsRockObstacle(row, col))
            {
                _statusBannerText.text = "Rock Obstacle! Nobody can place on a rock square.";
                PlaySfx("click");
                return;
            }

            if (_engine.IsCellFrozen(row, col))
            {
                _statusBannerText.text = "This square is frozen! Choose another square.";
                PlaySfx("click");
                return;
            }

            if (!_engine.CanPlace(row, col))
                return;

            if (IsVsAIMode && _engine.CurrentTurn != CellPiece.X)
                return;

            ExecuteMove(row, col);

            if (currentGameMode == GameMode.PuzzleStage && _engine.State == MatchState.Playing)
            {
                _puzzleMovesLeft--;
                if (_puzzleMovesLeft <= 0)
                {
                    PromptOutOfPuzzleMovesRevive();
                    return;
                }
            }
            else if (currentGameMode == GameMode.TowerClimb && _engine.State == MatchState.Playing)
            {
                XORoguelikeRunManager.PlayerMovesThisMatch++;
                TowerFloorBlind blind = XORoguelikeRunManager.GetActiveBlind();
                if (blind.maxPlayerMoves > 0 && XORoguelikeRunManager.PlayerMovesThisMatch >= blind.maxPlayerMoves)
                {
                    PlaySfx("lose");
                    _statusBannerText.text = $"OUT OF MOVES! Limit of {blind.maxPlayerMoves} moves reached!";
                    StartCoroutine(ShowModalAfterDelay(0.6f, () => ProcessPlayerDefeatConsequences()));
                    return;
                }
            }

            if (_engine.State == MatchState.Playing
                && IsVsAIMode
                && _engine.CurrentTurn == CellPiece.O)
            {
                StartCoroutine(AITurnRoutine());
            }
        }

        private void ExecuteMove(int row, int col)
        {
            if (_engine.VariantRule == XOVariantRule.GravityDrop)
            {
                int landR = _engine.GetGravityLandingRow(col);
                if (landR >= 0) row = landR;
            }

            CellPiece mover = _engine.CurrentTurn;
            _activeHintCell = new Vector2Int(-1, -1);
            _turnTimerRemaining = turnTimeLimit;

            PlaceResult placeResult = _engine.TryPlace(row, col);
            if (!placeResult.Placed)
                return;

            bool isGobbleMove = !string.IsNullOrEmpty(placeResult.TacticalCallout) && placeResult.TacticalCallout.StartsWith("GOBBLED");
            PlaySfx(isGobbleMove ? "gobble" : (mover == CellPiece.X ? "place_x" : "place_o"));
            if (enableHaptics) XOPlayableAdsBridge.Instance?.TriggerHaptic(isGobbleMove ? 35 : 15);

            // Handle Special Board Tile effects (Gold, Lightning, Hidden Mine & Warp Portal!)
            if (placeResult.TriggeredSpecial == SpecialTileType.GoldBonus)
            {
                SpawnCellVFXBurst(row, col, XOThemeDatabase.Hex("#FBBF24"), Color.white, 14);
                if (mover == CellPiece.X)
                {
                    XOProgressionSystem.TotalSpecialTilesClaimed++;
                    int goldGain = XORoguelikeRunManager.HasRelic(CaroRelicType.BountyHunter) ? 40 : 20;
                    Coins += goldGain;
                    string bountyText = XORoguelikeRunManager.HasRelic(CaroRelicType.BountyHunter) ? "+40 Coins (Bounty Hunter 2x!)" : "+20 Bonus Coins!";
                    SpawnTacticalCalloutBanner("GOLD TILE CLAIMED!", bountyText, XOThemeDatabase.Hex("#D97706"));
                }
                else if (currentGameMode == GameMode.VsAI)
                {
                    XOBotProfile bot = XOProgressionSystem.GetActiveBot();
                    ShowBotReactionBubble($"{bot.botName}: \"Haha! Snagged the Gold Tile before you!\"");
                }
            }
            else if (placeResult.TriggeredSpecial == SpecialTileType.ExtraTurnLightning)
            {
                PlaySfx("lightning");
                SpawnCellVFXBurst(row, col, XOThemeDatabase.Hex("#FDE047"), XOThemeDatabase.Hex("#22D3EE"), 18);
                if (mover == CellPiece.X)
                {
                    XOProgressionSystem.TotalSpecialTilesClaimed++;
                    SpawnTacticalCalloutBanner("LIGHTNING BONUS TURN!", "You get to place 1 MORE piece right now!", XOThemeDatabase.Hex("#0EA5E9"));
                }
                else
                {
                    SpawnTacticalCalloutBanner("BOT BONUS TURN!", "Bot claimed a Lightning Tile & goes again!", XOThemeDatabase.Hex("#EF4444"));
                }
            }
            else if (placeResult.TriggeredSpecial == SpecialTileType.HiddenMine)
            {
                PlaySfx("bomb");
                SpawnCellVFXBurst(row, col, XOThemeDatabase.Hex("#EF4444"), XOThemeDatabase.Hex("#F97316"), 20);
                if (mover == CellPiece.X) XOProgressionSystem.TotalSpecialTilesClaimed++;
                SpawnTacticalCalloutBanner("MINE BLAST TRIGGERED!", "Blasted adjacent pieces around the mine!", XOThemeDatabase.Hex("#DC2626"));
            }
            else if (placeResult.TriggeredSpecial == SpecialTileType.WarpPortal)
            {
                PlaySfx("swap");
                SpawnCellVFXBurst(row, col, XOThemeDatabase.Hex("#A855F7"), XOThemeDatabase.Hex("#22D3EE"), 20);
                if (mover == CellPiece.X) XOProgressionSystem.TotalSpecialTilesClaimed++;
                SpawnTacticalCalloutBanner("WARP PORTAL CLONE!", "Spawned a BONUS Twin Piece on the linked Portal!", XOThemeDatabase.Hex("#9333EA"));
            }
            else if (!string.IsNullOrEmpty(placeResult.TacticalCallout) && _engine.State == MatchState.Playing)
            {
                if (mover == CellPiece.X)
                {
                    XOProgressionSystem.TotalTacticalCallouts++;
                    PlaySfx("callout");
                    Coins += placeResult.CalloutBonusCoins;
                    SpawnTacticalCalloutBanner(placeResult.TacticalCallout, $"+{placeResult.CalloutBonusCoins} Tactical Bonus Coins!", XOThemeDatabase.Hex("#16A34A"));
                    if (currentGameMode == GameMode.VsAI)
                    {
                        XOBotProfile bot = XOProgressionSystem.GetActiveBot();
                        ShowBotReactionBubble($"{bot.botName}: \"{bot.PickRandom(bot.onPlayerGoodMoveLines)}\"");
                    }
                    else if (currentGameMode == GameMode.TowerClimb)
                    {
                        TowerFloorBlind blind = XORoguelikeRunManager.GetActiveBlind();
                        ShowBotReactionBubble($"{blind.bossName}: \"Impressive move! But can you conquer the floor?\"");
                    }
                }
                else if (currentGameMode == GameMode.VsAI)
                {
                    XOBotProfile bot = XOProgressionSystem.GetActiveBot();
                    ShowBotReactionBubble($"{bot.botName}: \"{bot.PickRandom(bot.onBotThreatLines)}\"");
                }
                else if (currentGameMode == GameMode.TowerClimb)
                {
                    TowerFloorBlind blind = XORoguelikeRunManager.GetActiveBlind();
                    ShowBotReactionBubble($"{blind.bossName}: \"Feel the power of Floor {blind.floorNumber}!\"");
                }
            }

            // ROGUELIKE SHATTER HAMMER RELIC: Once per match, shatter adjacent Bot piece
            if (mover == CellPiece.X && currentGameMode == GameMode.TowerClimb
                && XORoguelikeRunManager.HasRelic(CaroRelicType.ShatterHammer)
                && !XORoguelikeRunManager.ShatterHammerTriggeredThisMatch)
            {
                if (_engine.TryExecuteShatterHammer(row, col, out Vector2Int shatteredCell))
                {
                    PlaySfx("bomb");
                    SpawnCellVFXBurst(shatteredCell.x, shatteredCell.y, XOProceduralAssets.ColCoral, XOProceduralAssets.ColSun, 20);
                    SpawnTacticalCalloutBanner("SHATTER HAMMER!", "Shattered and destroyed adjacent Bot piece!", XOProceduralAssets.ColCoral);
                }
            }

            // Tower Hazards: Collapsing Arena perimeter, Cosmic Shift lightning & Mid-Match Surprise Shifts
            if (currentGameMode == GameMode.TowerClimb && _engine.State == MatchState.Playing)
            {
                TowerFloorBlind activeBlind = XORoguelikeRunManager.GetActiveBlind();
                if (activeBlind != null)
                {
                    // Dynamic mid-match surprise shift
                    if (activeBlind.hasMidMatchShift && !_midMatchShiftTriggered && _engine.MoveHistory.Count >= activeBlind.midMatchShiftTurn)
                    {
                        _midMatchShiftTriggered = true;
                        PlaySfx("powerup");
                        StartCoroutine(ShakeBoardRoutine(0.35f, 12f));
                        SpawnTacticalCalloutBanner(activeBlind.midMatchShiftTitle, activeBlind.midMatchShiftDesc, XOProceduralAssets.ColCoral);

                        if (activeBlind.blindType == BossBlindType.TheArm)
                        {
                            _engine.VariantRule = XOVariantRule.StrictCaro;
                        }
                        else if (activeBlind.blindType == BossBlindType.TwinDragon)
                        {
                            _engine.RequiredWinLines = 2;
                        }
                        else if (activeBlind.blindType == BossBlindType.FogOfWar)
                        {
                            FreezeRandomStrategicTiles(3, 2);
                        }
                        else if (activeBlind.blindType == BossBlindType.CollapsingArena)
                        {
                            if (_engine.CollapseNextPerimeterRing(out var collapsed))
                            {
                                foreach (var cell in collapsed)
                                    SpawnCellVFXBurst(cell.x, cell.y, XOProceduralAssets.ColCoral, XOProceduralAssets.ColSun, 10);
                            }
                        }
                        else if (activeBlind.blindType == BossBlindType.StormColosseum)
                        {
                            if (_engine.ExecuteCosmicShift(out var struck))
                            {
                                foreach (var cell in struck)
                                    SpawnCellVFXBurst(cell.x, cell.y, XOProceduralAssets.ColSky, Color.white, 14);
                            }
                        }
                    }

                    // Secondary arena collapse (e.g. Move 18 or Floor 10 periodic)
                    if (activeBlind.hasCollapsingPerimeter && _engine.MoveHistory.Count > 0 && (_engine.MoveHistory.Count == 18 || (activeBlind.blindType == BossBlindType.OverlordApex && _engine.MoveHistory.Count % 10 == 0)))
                    {
                        if (_engine.CollapseNextPerimeterRing(out var collapsed))
                        {
                            PlaySfx("bomb");
                            foreach (var cell in collapsed)
                            {
                                SpawnCellVFXBurst(cell.x, cell.y, XOProceduralAssets.ColCoral, XOProceduralAssets.ColSun, 10);
                            }
                            SpawnTacticalCalloutBanner("ARENA COLLAPSED!", "Outer perimeter crumbled into rubble! Ring destroyed!", XOProceduralAssets.ColCoral);
                        }
                    }

                    // Secondary cosmic shift at Move 16
                    if (activeBlind.hasCosmicShift && _engine.MoveHistory.Count == 16)
                    {
                        if (_engine.ExecuteCosmicShift(out var struck))
                        {
                            PlaySfx("lightning");
                            foreach (var cell in struck)
                            {
                                SpawnCellVFXBurst(cell.x, cell.y, XOProceduralAssets.ColSky, Color.white, 14);
                            }
                            SpawnTacticalCalloutBanner("STORM TEMPEST!", "Second wave of lightning struck! Extra-Turn tiles charged!", XOProceduralAssets.ColSky);
                        }
                    }
                }
            }

            // Twin 5-in-a-Row Callouts (Floor 8 & 10)
            if (_engine.RequiredWinLines > 1 && _engine.State == MatchState.Playing)
            {
                if (mover == CellPiece.X && _engine.CompletedWinLinesX.Count == 1 && !_announcedFirstFiveX)
                {
                    _announcedFirstFiveX = true;
                    PlaySfx("reward");
                    SpawnTacticalCalloutBanner("1ST FIVE LOCKED! (1/2)", "Now connect a 2ND distinct 5-in-a-row to claim victory!", XOProceduralAssets.ColSun);
                }
                else if (mover == CellPiece.O && _engine.CompletedWinLinesO.Count == 1 && !_announcedFirstFiveO)
                {
                    _announcedFirstFiveO = true;
                    PlaySfx("lose");
                    SpawnTacticalCalloutBanner("BOSS LOCKED 1ST FIVE! (1/2)", "Boss needs only 1 more line to win! Block aggressively!", XOProceduralAssets.ColCoral);
                }
            }

            RefreshAllCells(animateLastMove: true);
            RefreshBottomActionBadges();

            if (_engine.State != MatchState.Playing)
            {
                HandleMatchEnded(_engine.State);
            }
            else
            {
                RefreshTurnAndScoreUI();
                if (placeResult.TriggeredSpecial == SpecialTileType.ExtraTurnLightning && mover == CellPiece.X)
                {
                    _statusBannerText.text = "LIGHTNING BONUS TURN! Place one more X immediately!";
                }
            }
        }

        private void FreezeRandomStrategicTiles(int count, int duration)
        {
            List<Vector2Int> candidates = _engine.GetCandidateCells(2);
            System.Random rnd = new System.Random();
            int frozen = 0;
            while (candidates.Count > 0 && frozen < count)
            {
                int idx = rnd.Next(candidates.Count);
                Vector2Int pos = candidates[idx];
                candidates.RemoveAt(idx);
                if (_engine.FreezeEmptyCell(pos.x, pos.y, duration))
                {
                    SpawnCellVFXBurst(pos.x, pos.y, XOThemeDatabase.Hex("#38BDF8"), Color.white, 12);
                    frozen++;
                }
            }
        }

        private IEnumerator AITurnRoutine()
        {
            _isAIThinking = true;
            RefreshTurnAndScoreUI();

            XOBotProfile activeBot = (currentGameMode == GameMode.VsAI) ? XOProgressionSystem.GetActiveBot() : null;
            TowerFloorBlind blind = (currentGameMode == GameMode.TowerClimb) ? XORoguelikeRunManager.GetActiveBlind() : null;
            AIDifficulty aiDiff = (currentGameMode == GameMode.PuzzleStage)
                ? AIDifficulty.Hard
                : (currentGameMode == GameMode.TowerClimb && blind != null)
                ? blind.difficulty
                : (activeBot != null ? activeBot.baseDifficulty : currentDifficulty);

            // Loop while it is O's turn (supports Bot Extra-Turn if Bot claims a Lightning Tile!)
            while (_engine.State == MatchState.Playing && _engine.CurrentTurn == CellPiece.O)
            {
                yield return new WaitForSeconds(0.32f);

                // Two-Way Bot Counter-Freeze (High-tier bots can freeze 1 dangerous player threat square once per match!)
                if (!_botUsedCounterSkillThisMatch && currentGameMode == GameMode.VsAI && activeBot != null)
                {
                    if (_engine.TryExecuteBotCounterFreeze(activeBot, out Vector2Int frozenCell))
                    {
                        _botUsedCounterSkillThisMatch = true;
                        PlaySfx("freeze");
                        SpawnCellVFXBurst(frozenCell.x, frozenCell.y, XOThemeDatabase.Hex("#38BDF8"), Color.white, 15);
                        RefreshAllCells(animateLastMove: false);
                        SpawnTacticalCalloutBanner($"{activeBot.botName.ToUpper()} COUNTER!", "Bot FROZE your open square for 3 turns!", XOThemeDatabase.Hex("#0284C7"));
                        ShowBotReactionBubble($"{activeBot.botName}: \"{activeBot.PickRandom(activeBot.onBotCounterSkillLines)}\"");
                        yield return new WaitForSeconds(0.45f);
                    }
                }

                // Relic: Guardian Aegis (Auto-freeze AI winning square)
                if (_engine.TryExecuteGuardianAegis(out Vector2Int aegisFrozenCell))
                {
                    PlaySfx("shield");
                    RefreshAllCells(animateLastMove: false);
                    SpawnTacticalCalloutBanner("GUARDIAN AEGIS!", "Relic auto-froze AI's winning threat for 2 turns!", XOProceduralAssets.ColLilac);
                    yield return new WaitForSeconds(0.45f);
                }

                // Tower Mode Boss Equipment Logic!
                if (currentGameMode == GameMode.TowerClimb && blind != null)
                {
                    BotEquipmentType bossEquip = blind.botEquipment;

                    // 1. Frost Staff / Omni Crown (Freeze Threat)
                    if (!_botUsedCounterSkillThisMatch && (bossEquip == BotEquipmentType.FrostStaff || bossEquip == BotEquipmentType.OmniCrown))
                    {
                        if (_engine.TryExecuteBotCounterFreeze(new XOBotProfile { counterSkill = BotCounterSkill.CryoFreeze }, out Vector2Int frozenCell))
                        {
                            _botUsedCounterSkillThisMatch = true;
                            PlaySfx("freeze");
                            SpawnCellVFXBurst(frozenCell.x, frozenCell.y, XOProceduralAssets.ColSky, Color.white, 15);
                            RefreshAllCells(animateLastMove: false);
                            SpawnTacticalCalloutBanner($"{blind.bossName.ToUpper()} RELIC!", "Frost Staff FROZE your open threat for 3 turns!", XOProceduralAssets.ColSky);
                            yield return new WaitForSeconds(0.45f);
                        }
                    }

                    // 2. Flame Blade (Bomb Player Piece)
                    if (!_botUsedCounterSkillThisMatch && bossEquip == BotEquipmentType.FlameBlade)
                    {
                        if (_engine.TryExecuteBotFlameBlade(out Vector2Int bombedCell))
                        {
                            _botUsedCounterSkillThisMatch = true;
                            PlaySfx("bomb");
                            SpawnCellVFXBurst(bombedCell.x, bombedCell.y, XOProceduralAssets.ColCoral, XOProceduralAssets.ColSun, 18);
                            RefreshAllCells(animateLastMove: false);
                            SpawnTacticalCalloutBanner($"{blind.bossName.ToUpper()} RELIC!", "Flame Blade BOMBED your critical piece!", XOProceduralAssets.ColCoral);
                            yield return new WaitForSeconds(0.45f);
                        }
                    }

                    // 3. Thorn Armor (Spawns a thorn rock obstacle)
                    if (!_botUsedCounterSkillThisMatch && bossEquip == BotEquipmentType.ThornArmor)
                    {
                        if (_engine.TryExecuteBotThornArmor(out Vector2Int thornCell))
                        {
                            _botUsedCounterSkillThisMatch = true;
                            PlaySfx("shield");
                            RefreshAllCells(animateLastMove: false);
                            SpawnTacticalCalloutBanner($"{blind.bossName.ToUpper()} RELIC!", "Thorn Armor blocked an attack corridor!", XOProceduralAssets.ColMuted);
                            yield return new WaitForSeconds(0.45f);
                        }
                    }
                }

                Vector2Int aiMove = _engine.CalculateAIMove(CellPiece.O, aiDiff, activeBot);
                ExecuteMove(aiMove.x, aiMove.y);

                // Boss Relic: Thunder Hammer / Omni Crown Double Move
                if (currentGameMode == GameMode.TowerClimb && blind != null && _engine.State == MatchState.Playing)
                {
                    if ((blind.botEquipment == BotEquipmentType.ThunderHammer || blind.botEquipment == BotEquipmentType.OmniCrown) && _engine.MoveHistory.Count % 8 == 0)
                    {
                        PlaySfx("lightning");
                        SpawnTacticalCalloutBanner("THUNDER HAMMER!", $"{blind.bossName} triggers a lightning DOUBLE MOVE!", XOProceduralAssets.ColSun);
                        yield return new WaitForSeconds(0.35f);
                        Vector2Int extraMove = _engine.CalculateAIMove(CellPiece.O, aiDiff, activeBot);
                        ExecuteMove(extraMove.x, extraMove.y);
                    }
                }
            }

            _isAIThinking = false;
            if (_engine.State == MatchState.Playing)
            {
                RefreshTurnAndScoreUI();
            }
        }

        private void PromptOutOfPuzzleMovesRevive()
        {
            PlaySfx("lose");
            _isOutOfMovesPuzzleRevive = true;
            _reviveTitleText.text = "OUT OF MOVES!";
            _reviveDescText.text = $"You ran out of moves on {_currentPuzzle.title}!\nWatch a short ad to get +3 EXTRA MOVES and a winning Hint!";
            _reviveActionBtnText.text = "WATCH AD: +3 MOVES & HINT!";

            _reviveModalRoot.transform.SetAsLastSibling();
            _reviveModalRoot.SetActive(true);
        }

        private void HandleMatchEnded(MatchState endState)
        {
            DrawWinStrikeLineIfApplicable();

            if (endState == MatchState.XWon)
            {
                _scoreX++;
                PlaySfx("win");
                if (enableHaptics) XOPlayableAdsBridge.Instance?.TriggerHaptic(50);
                SpawnVictoryConfettiBurst();
                _statusBannerText.text = $"AWESOME! Player X connected {_engine.WinLength} in a row!";
                StartCoroutine(ShowModalAfterDelay(0.65f, () => ProcessPlayerVictoryConsequences()));
            }
            else if (endState == MatchState.OWon)
            {
                if (enableHaptics) XOPlayableAdsBridge.Instance?.TriggerHaptic(40);

                // ROGUELIKE PHOENIX SIGIL INTERCEPT: Divine fire incinerates winning piece once per match!
                if (currentGameMode == GameMode.TowerClimb
                    && XORoguelikeRunManager.HasRelic(CaroRelicType.VampireFang)
                    && !XORoguelikeRunManager.PhoenixSigilTriggeredThisMatch)
                {
                    XORoguelikeRunManager.PhoenixSigilTriggeredThisMatch = true;
                    if (_engine.IncinerateLastWinningPiece(out Vector2Int burntTile, freezeTurns: 2))
                    {
                        PlaySfx("powerup");
                        if (enableHaptics) XOPlayableAdsBridge.Instance?.TriggerHaptic(75);
                        SpawnCellVFXBurst(burntTile.x, burntTile.y, XOProceduralAssets.ColCoral, XOProceduralAssets.ColSun, 24);
                        SpawnTacticalCalloutBanner("PHOENIX SIGIL!", "Holy flames incinerated the Bot's winning piece and froze the tile for 2 turns! Run saved!", XOProceduralAssets.ColSun);
                        RefreshAllCells(animateLastMove: false);
                        RefreshTurnAndScoreUI();
                        RefreshBattleRelicBarUI();
                        return;
                    }
                }

                if (currentGameMode != GameMode.TowerClimb && IsVsAIMode && !_reviveUsedThisMatch)
                {
                    PlaySfx("lose");
                    _isOutOfMovesPuzzleRevive = false;
                    _reviveTitleText.text = "SECOND CHANCE!";
                    string opponentName = (currentGameMode == GameMode.TowerClimb) ? "Boss" : "AI";
                    _reviveDescText.text = $"{opponentName} just connected {_engine.WinLength} in a row!\nWatch a short ad to UNDO its winning move\nand highlight the exact square to block!";
                    _reviveActionBtnText.text = "WATCH AD: REVIVE & BLOCK NOW!";

                    _statusBannerText.text = $"{opponentName} connected {_engine.WinLength}! Use Second Chance to block it now!";
                    StartCoroutine(ShowModalAfterDelay(0.6f, () =>
                    {
                        _reviveModalRoot.transform.SetAsLastSibling();
                        _reviveModalRoot.SetActive(true);
                    }));
                }
                else
                {
                    _scoreO++;
                    PlaySfx(currentGameMode == GameMode.TwoPlayerLocal ? "win" : "lose");
                    StartCoroutine(ShowModalAfterDelay(0.65f, () => ProcessPlayerDefeatConsequences()));
                }
            }
            else if (endState == MatchState.Draw)
            {
                PlaySfx("undo");
                _statusBannerText.text = "DRAW GAME! Well played by both sides!";
                StartCoroutine(ShowModalAfterDelay(0.55f, () => ShowMatchResultModal(MatchState.Draw, "")));
            }

            RefreshScoreTextsOnly();
        }

        private void ProcessPlayerVictoryConsequences()
        {
            XOProgressionSystem.TotalMatchesWon++;
            XOProgressionSystem.WinStreak++;
            int streak = XOProgressionSystem.WinStreak;
            int starsEarned = (streak >= 3) ? 2 : 1;
            XOProgressionSystem.TotalStars += starsEarned;

            XOProgressionSystem.ChestProgress = Mathf.Min(3, XOProgressionSystem.ChestProgress + 1);

            string extraNotes = $"+{starsEarned} Rank Star(s)   |   Win Streak: {streak}";

            if (currentGameMode == GameMode.VsAI)
            {
                XOBotProfile bot = XOProgressionSystem.GetActiveBot();
                if (XOProgressionSystem.MarkBotConquered(XOProgressionSystem.SelectedBotIndex))
                {
                    Coins += 80;
                    extraNotes += $"\nFirst-Time Victory vs {bot.botName}: +80 Bounty Coins! ({XOProgressionSystem.GetConqueredBotsCount()}/6 Rivals Conquered)";
                }
            }
            else if (currentGameMode == GameMode.TowerClimb)
            {
                TowerFloorBlind blind = XORoguelikeRunManager.GetActiveBlind();
                XOProgressionSystem.MarkTowerFloorCleared(blind.floorNumber);
                extraNotes += $"\nFloor #{blind.floorNumber} ({blind.blindName}) Conquered!";
                if (XORoguelikeRunManager.HasRelic(CaroRelicType.CrownOfOmni))
                {
                    Coins += 25;
                    extraNotes += "\nCrown of Omni: +25 Divine Coins!";
                }
            }
            else if (currentGameMode == GameMode.PuzzleStage)
            {
                int clearedStage = XOProgressionSystem.CurrentPuzzleStage;
                XOProgressionSystem.CurrentPuzzleStage++;
                extraNotes += $"\nStage #{clearedStage} Cleared! Unlocked Stage #{XOProgressionSystem.CurrentPuzzleStage}!";
            }

            bool isDiag = _engine.IsWinningLineDiagonal();
            bool isHardOrPuzzle = (currentGameMode == GameMode.PuzzleStage || currentDifficulty == AIDifficulty.Hard);
            if (XOProgressionSystem.CheckAndAdvanceMission(
                _engine.GetPlayerMadeMovesCount(),
                isDiag,
                _usedPowerUpThisMatch,
                streak,
                isHardOrPuzzle,
                out XOMissionData doneMission))
            {
                Coins += doneMission.rewardCoins;
                extraNotes += $"\nQuest Complete '{doneMission.description}': +{doneMission.rewardCoins} Coins!";
            }

            RefreshHeaderAndProgressionUI();
            RefreshSelectorButtonsUI();
            XOPlayableAdsBridge.Instance?.ReportPlayableScore(XOProgressionSystem.TotalStars * 100 + Coins);
            ShowMatchResultModal(MatchState.XWon, extraNotes);
        }

        private void ProcessPlayerDefeatConsequences()
        {
            if (currentGameMode == GameMode.TowerClimb)
            {
                TowerFloorBlind blind = XORoguelikeRunManager.GetActiveBlind();
                bool runOver = XORoguelikeRunManager.LoseHeart();
                string towerNote = runOver
                    ? $"Run Ended (0 Hearts)! Defeated on Floor {blind.floorNumber} by {blind.bossName}.\nSeed: {XORoguelikeRunManager.FormattedSeed} • Relics Lost: {XORoguelikeRunManager.EquippedRelics.Count} • Run Coins: +{XORoguelikeRunManager.RunTotalCoinsEarned}\nRelics & Floor progression reset for next run."
                    : $"Lost 1 Heart! ({XORoguelikeRunManager.Lives}/{XORoguelikeRunManager.MaxLives} remaining)\nTap Rematch to fight {blind.bossName} again!";
                ShowMatchResultModal(MatchState.OWon, towerNote);
                return;
            }

            _lostStreakToRestore = XOProgressionSystem.WinStreak;
            if (XOProgressionSystem.TotalStars > 0)
            {
                XOProgressionSystem.TotalStars--;
            }
            XOProgressionSystem.WinStreak = 0;

            RefreshHeaderAndProgressionUI();
            string note = (_lostStreakToRestore > 0)
                ? $"Lost your {_lostStreakToRestore}-Win Streak & -1 Rank Star!\n(Use Rank Shield below to restore them!)"
                : "-1 Rank Star. Use Rank Shield below to protect your star!";

            ShowMatchResultModal(MatchState.OWon, note);
        }

        private IEnumerator ShowModalAfterDelay(float delay, System.Action action)
        {
            yield return new WaitForSeconds(delay);
            action?.Invoke();
        }
    }
}
