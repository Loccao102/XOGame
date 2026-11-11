using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using XOPlayable.Ads;
using XOPlayable.Core;
using XOPlayable.Visuals;

namespace XOPlayable.UI
{
    /// <summary>
    /// Coordinates active match lifecycle: board initialization, cell placement via <see cref="PlaceResult"/>,
    /// turn timer progression, Tower blind hazard events, and AI turn execution.
    /// </summary>
    public sealed class XOMatchSessionController
    {
        private readonly MonoBehaviour _host;
        private readonly XOCaroEngine _engine;
        private readonly XOGameStateStore _store;
        private readonly XOAudioManager _audio;
        private readonly XOBattlePresenter _battle;
        private readonly XOBattleEffectsPresenter _effects;
        private readonly XOTacticalSkillsController _skills;
        private readonly XOMatchOutcomeService _outcomes;
        private readonly Func<XOMatchConfig> _getConfig;
        private readonly Action<int, int, XOVariantRule> _updateConfigDimensions;
        private readonly Action _refreshAllHeaderAndLobby;
        private readonly Action _updateLayout;

        private Coroutine _aiCoroutine;

        public XOMatchSessionController(
            MonoBehaviour host,
            XOCaroEngine engine,
            XOGameStateStore store,
            XOAudioManager audio,
            XOBattlePresenter battle,
            XOBattleEffectsPresenter effects,
            XOTacticalSkillsController skills,
            XOMatchOutcomeService outcomes,
            Func<XOMatchConfig> getConfig,
            Action<int, int, XOVariantRule> updateConfigDimensions,
            Action refreshAllHeaderAndLobby,
            Action updateLayout)
        {
            _host = host;
            _engine = engine;
            _store = store;
            _audio = audio;
            _battle = battle;
            _effects = effects;
            _skills = skills;
            _outcomes = outcomes;
            _getConfig = getConfig;
            _updateConfigDimensions = updateConfigDimensions;
            _refreshAllHeaderAndLobby = refreshAllHeaderAndLobby;
            _updateLayout = updateLayout;
        }

        /// <summary>Stops any active AI or delayed modal coroutines cleanly.</summary>
        public void StopActiveRoutines()
        {
            if (_aiCoroutine != null) { _host.StopCoroutine(_aiCoroutine); _aiCoroutine = null; }
            _outcomes?.StopPendingModalCoroutines();
            _store.IsAIThinking = false;
        }

        /// <summary>Initializes a new match across Normal, 2P Local, Puzzle, or Tower modes.</summary>
        public void StartNewMatch(int requestedBoardSize)
        {
            StopActiveRoutines();
            XOMatchConfig cfg = _getConfig();
            _store.ResetMatchFlags(cfg.TurnTimeLimit);

            bool isTower = cfg.Mode == GameMode.TowerClimb;
            XORoguelikeRunManager.IsTowerMatchActive = isTower;

            if (cfg.Mode == GameMode.PuzzleStage)
            {
                _engine.Initialize(15, 5, spawnSpecialTiles: false, variant: XOVariantRule.ClassicCaro, isTowerClimb: false);
                _store.CurrentPuzzle = XOProgressionSystem.GeneratePuzzleStage(XOProgressionSystem.CurrentPuzzleStage, 15);
                _engine.LoadPuzzleStage(_store.CurrentPuzzle);
                _store.PuzzleMovesLeft = _store.CurrentPuzzle.maxPlayerMoves;
                _store.ActiveHintCell = _engine.CalculateBestHint(CellPiece.X, out _);
                _updateConfigDimensions(_engine.BoardSize, _engine.WinLength, _engine.VariantRule);
            }
            else if (isTower)
            {
                TowerFloorBlind blind = XORoguelikeRunManager.GetActiveBlind();
                XORoguelikeRunManager.ResetMatchState();
                int floorSeed = XORoguelikeRunManager.GetFloorSeed(blind.floorNumber);
                _engine.Initialize(
                    blind.boardSize,
                    5,
                    spawnSpecialTiles: true,
                    variant: blind.variantRule,
                    specialTilesSeed: floorSeed,
                    floorNumber: blind.floorNumber,
                    isTowerClimb: true,
                    runContext: XORoguelikeRunManager.DefaultRun);
                _engine.RequiredWinLines = blind.requiredWinLines;
                _updateConfigDimensions(_engine.BoardSize, _engine.WinLength, _engine.VariantRule);
            }
            else
            {
                bool spawnSpecials = cfg.Variant == XOVariantRule.ChaosMinefield;
                _engine.Initialize(requestedBoardSize, cfg.WinLength, spawnSpecialTiles: spawnSpecials, variant: cfg.Variant, isTowerClimb: false);
                _updateConfigDimensions(_engine.BoardSize, _engine.WinLength, _engine.VariantRule);
            }

            _effects.ClearTransientEffects();
            _battle.RebuildBoardCellsIfNeeded(_engine.BoardSize);
            _battle.RefreshAllCells(animateLastMove: false);
            _refreshAllHeaderAndLobby();
            _battle.RefreshTurnAndScore(cfg.Mode, cfg.TurnTimeLimit);
            _battle.RefreshBottomActionBadges();
            _battle.RefreshBattleRelicBar(cfg.Mode);
            _updateLayout();

            ShowMatchOpeningBubble(cfg.Mode);
        }

        private void ShowMatchOpeningBubble(GameMode mode)
        {
            if (mode == GameMode.VsAI)
            {
                XOBotProfile bot = XOProgressionSystem.GetActiveBot();
                _effects.ShowBotReactionBubble($"{bot.botName}: \"{bot.PickRandom(bot.greetingLines)}\"");
            }
            else if (mode == GameMode.TowerClimb)
            {
                TowerFloorBlind blind = XORoguelikeRunManager.GetActiveBlind();
                _effects.ShowBotReactionBubble($"{blind.bossName}: \"Floor {blind.floorNumber}: {blind.blindName}! Prepare yourself!\"");
            }
            else
            {
                _effects.HideBotChatBubble();
            }
        }

        /// <summary>Advances turn countdown during live play and auto-moves/passes on expiration.</summary>
        public void TickTurnTimer(float deltaTime)
        {
            XOMatchConfig cfg = _getConfig();
            if (_engine.State != MatchState.Playing || cfg.TurnTimeLimit <= 0.01f) return;

            _store.TurnTimerRemaining -= deltaTime;
            _battle.UpdateTimerVisual(cfg.TurnTimeLimit);
            if (_store.TurnTimerRemaining > 0f) return;

            _store.TurnTimerRemaining = cfg.TurnTimeLimit;
            if (cfg.IsVsAI && _engine.CurrentTurn == CellPiece.X && !_store.IsAIThinking)
            {
                XOCellPos autoPos = _engine.CalculateBestHint(CellPiece.X, out _);
                if (autoPos.IsValid) OnBoardCellClicked(autoPos.x, autoPos.y);
            }
            else
            {
                _engine.PassTurn();
                _battle.RefreshTurnAndScore(cfg.Mode, cfg.TurnTimeLimit);
            }
        }

        /// <summary>Processes cell taps for either active skill targeting or standard piece placement.</summary>
        public void OnBoardCellClicked(int row, int col)
        {
            XOMatchConfig cfg = _getConfig();
            if (_store.IsAIThinking || _engine.State != MatchState.Playing) return;

            if (_skills.TryHandleTargetedCellClick(row, col, cfg.Mode)) return;

            if (_engine.VariantRule == XOVariantRule.GravityDrop)
            {
                int landingRow = _engine.GetGravityLandingRow(col);
                if (landingRow < 0)
                {
                    _battle.StatusBannerText.text = "Column full! Tap another column to drop your piece.";
                    _audio.PlaySfx("click");
                    return;
                }
                row = landingRow;
            }

            if (_engine.IsRockObstacle(row, col))
            {
                _battle.StatusBannerText.text = "Rock Obstacle! Nobody can place on a rock square.";
                _audio.PlaySfx("click");
                return;
            }

            if (_engine.IsCellFrozen(row, col))
            {
                _battle.StatusBannerText.text = "This square is frozen! Choose another square.";
                _audio.PlaySfx("click");
                return;
            }

            if (!_engine.CanPlace(row, col)) return;
            if (cfg.IsVsAI && _engine.CurrentTurn != CellPiece.X) return;

            ExecuteMove(row, col);

            if (cfg.Mode == GameMode.PuzzleStage && _engine.State == MatchState.Playing)
            {
                _store.PuzzleMovesLeft--;
                if (_store.PuzzleMovesLeft <= 0)
                {
                    _outcomes.PromptOutOfPuzzleMovesRevive();
                    return;
                }
            }
            else if (cfg.Mode == GameMode.TowerClimb && _engine.State == MatchState.Playing)
            {
                XORoguelikeRunManager.PlayerMovesThisMatch++;
                TowerFloorBlind blind = XORoguelikeRunManager.GetActiveBlind();
                if (blind.maxPlayerMoves > 0 && XORoguelikeRunManager.PlayerMovesThisMatch >= blind.maxPlayerMoves)
                {
                    _audio.PlaySfx("lose");
                    _battle.StatusBannerText.text = $"OUT OF MOVES! Limit of {blind.maxPlayerMoves} moves reached!";
                    _outcomes.ScheduleDelayedModal(0.6f, _outcomes.ProcessPlayerDefeatConsequences);
                    return;
                }
            }

            if (_engine.State == MatchState.Playing && cfg.IsVsAI && _engine.CurrentTurn == CellPiece.O)
            {
                _aiCoroutine = _host.StartCoroutine(AITurnRoutine());
            }
        }

        private void ExecuteMove(int row, int col)
        {
            XOMatchConfig cfg = _getConfig();
            CellPiece mover = _engine.CurrentTurn;
            _store.ActiveHintCell = XOCellPos.Invalid;
            _store.TurnTimerRemaining = cfg.TurnTimeLimit;

            PlaceResult result = _engine.TryPlace(row, col);
            if (!result.Placed) return;

            int landR = result.LandingCell.x;
            int landC = result.LandingCell.y;
            bool isGobble = !string.IsNullOrEmpty(result.TacticalCallout) && result.TacticalCallout.StartsWith("GOBBLED");
            _audio.PlaySfx(isGobble ? "gobble" : (mover == CellPiece.X ? "place_x" : "place_o"));
            if (cfg.HapticsEnabled) XOPlayableAdsBridge.Instance?.TriggerHaptic(isGobble ? 35 : 15);

            DispatchSpecialTileFeedback(result, cfg.Mode);
            HandleTowerPostMoveEvents(landR, landC, mover, cfg.Mode);

            if (result.StrictBlockedFive && _engine.State == MatchState.Playing)
            {
                _effects.SpawnTacticalCalloutBanner("STRICT BLOCK RULE!", "5-in-a-row blocked on both ends does NOT win!", XOProceduralAssets.ColMuted);
            }

            if (_engine.RequiredWinLines > 1 && _engine.State == MatchState.Playing)
            {
                if (mover == CellPiece.X && _engine.CompletedWinLinesX.Count == 1 && !_store.AnnouncedFirstFiveX)
                {
                    _store.AnnouncedFirstFiveX = true;
                    _audio.PlaySfx("reward");
                    _effects.SpawnTacticalCalloutBanner("1ST FIVE LOCKED! (1/2)", "Connect a 2ND distinct 5-in-a-row to claim victory!", XOProceduralAssets.ColSun);
                }
                else if (mover == CellPiece.O && _engine.CompletedWinLinesO.Count == 1 && !_store.AnnouncedFirstFiveO)
                {
                    _store.AnnouncedFirstFiveO = true;
                    _audio.PlaySfx("lose");
                    _effects.SpawnTacticalCalloutBanner("BOSS LOCKED 1ST FIVE! (1/2)", "Boss needs only 1 more line to win!", XOProceduralAssets.ColCoral);
                }
            }

            _battle.RefreshAllCells(animateLastMove: true);
            _battle.RefreshBottomActionBadges();

            if (_engine.State != MatchState.Playing)
            {
                _outcomes.HandleMatchEnded(_engine.State);
            }
            else
            {
                _battle.RefreshTurnAndScore(cfg.Mode, cfg.TurnTimeLimit);
                if (result.GrantedExtraTurn && mover == CellPiece.X)
                {
                    _battle.StatusBannerText.text = "LIGHTNING BONUS TURN! Place one more X immediately!";
                }
            }
        }

        private void DispatchSpecialTileFeedback(PlaceResult result, GameMode mode)
        {
            int r = result.LandingCell.x;
            int c = result.LandingCell.y;
            CellPiece mover = result.PlacedPiece;

            switch (result.TriggeredSpecial)
            {
                case SpecialTileType.GoldBonus:
                    _effects.SpawnCellVFXBurst(r, c, XOUIColors.Amber400, Color.white, 14);
                    if (mover == CellPiece.X)
                    {
                        XOProgressionSystem.TotalSpecialTilesClaimed++;
                        int goldGain = XORoguelikeRunManager.HasRelic(CaroRelicType.BountyHunter) ? 40 : 20;
                        _store.Coins += goldGain;
                        _effects.SpawnTacticalCalloutBanner("GOLD TILE CLAIMED!", $"+{goldGain} Bonus Coins!", XOUIColors.Amber600);
                    }
                    else if (mode == GameMode.VsAI)
                    {
                        XOBotProfile bot = XOProgressionSystem.GetActiveBot();
                        _effects.ShowBotReactionBubble($"{bot.botName}: \"Haha! Snagged the Gold Tile before you!\"");
                    }
                    break;

                case SpecialTileType.ExtraTurnLightning:
                    _audio.PlaySfx("lightning");
                    _effects.SpawnCellVFXBurst(r, c, XOUIColors.Yellow300, XOUIColors.Cyan400, 18);
                    if (mover == CellPiece.X) XOProgressionSystem.TotalSpecialTilesClaimed++;
                    _effects.SpawnTacticalCalloutBanner(
                        mover == CellPiece.X ? "LIGHTNING BONUS TURN!" : "BOT BONUS TURN!",
                        mover == CellPiece.X ? "You get to place 1 MORE piece right now!" : "Bot claimed a Lightning Tile & goes again!",
                        mover == CellPiece.X ? XOUIColors.Sky500 : XOUIColors.Red500);
                    break;

                case SpecialTileType.HiddenMine:
                    _audio.PlaySfx("bomb");
                    _effects.SpawnCellVFXBurst(r, c, XOUIColors.Red500, XOUIColors.Orange500, 20);
                    if (mover == CellPiece.X) XOProgressionSystem.TotalSpecialTilesClaimed++;
                    _effects.SpawnTacticalCalloutBanner("MINE BLAST TRIGGERED!", "Blasted adjacent pieces around the mine!", XOUIColors.Red600);
                    break;

                case SpecialTileType.WarpPortal:
                    _audio.PlaySfx("swap");
                    _effects.SpawnCellVFXBurst(r, c, XOUIColors.Purple500, XOUIColors.Cyan400, 20);
                    if (result.TwinCell.IsValid)
                        _effects.SpawnCellVFXBurst(result.TwinCell.x, result.TwinCell.y, XOUIColors.Purple500, Color.white, 14);
                    if (mover == CellPiece.X) XOProgressionSystem.TotalSpecialTilesClaimed++;
                    _effects.SpawnTacticalCalloutBanner("WARP PORTAL CLONE!", "Spawned a BONUS Twin Piece on the linked Portal!", XOUIColors.Purple600);
                    break;

                default:
                    if (!string.IsNullOrEmpty(result.TacticalCallout) && _engine.State == MatchState.Playing)
                    {
                        DispatchTacticalCallout(result, mover, mode);
                    }
                    break;
            }
        }

        private void DispatchTacticalCallout(PlaceResult result, CellPiece mover, GameMode mode)
        {
            if (mover == CellPiece.X)
            {
                XOProgressionSystem.TotalTacticalCallouts++;
                _audio.PlaySfx("callout");
                _store.Coins += result.CalloutBonusCoins;
                _effects.SpawnTacticalCalloutBanner(result.TacticalCallout, $"+{result.CalloutBonusCoins} Tactical Bonus Coins!", XOUIColors.Green600);
                if (mode == GameMode.VsAI)
                {
                    XOBotProfile bot = XOProgressionSystem.GetActiveBot();
                    _effects.ShowBotReactionBubble($"{bot.botName}: \"{bot.PickRandom(bot.onPlayerGoodMoveLines)}\"");
                }
                else if (mode == GameMode.TowerClimb)
                {
                    TowerFloorBlind blind = XORoguelikeRunManager.GetActiveBlind();
                    _effects.ShowBotReactionBubble($"{blind.bossName}: \"Impressive move! But can you conquer the floor?\"");
                }
            }
            else if (mode == GameMode.VsAI)
            {
                XOBotProfile bot = XOProgressionSystem.GetActiveBot();
                _effects.ShowBotReactionBubble($"{bot.botName}: \"{bot.PickRandom(bot.onBotThreatLines)}\"");
            }
            else if (mode == GameMode.TowerClimb)
            {
                TowerFloorBlind blind = XORoguelikeRunManager.GetActiveBlind();
                _effects.ShowBotReactionBubble($"{blind.bossName}: \"Feel the power of Floor {blind.floorNumber}!\"");
            }
        }

        private void HandleTowerPostMoveEvents(int row, int col, CellPiece mover, GameMode mode)
        {
            if (mode != GameMode.TowerClimb) return;

            if (mover == CellPiece.X && XORoguelikeRunManager.HasRelic(CaroRelicType.ShatterHammer) && !XORoguelikeRunManager.ShatterHammerTriggeredThisMatch)
            {
                if (_engine.TryExecuteShatterHammer(row, col, out XOCellPos shatteredCell))
                {
                    _audio.PlaySfx("bomb");
                    _effects.SpawnCellVFXBurst(shatteredCell.x, shatteredCell.y, XOProceduralAssets.ColCoral, XOProceduralAssets.ColSun, 20);
                    _effects.SpawnTacticalCalloutBanner("SHATTER HAMMER!", "Shattered and destroyed adjacent Bot piece!", XOProceduralAssets.ColCoral);
                }
            }

            if (_engine.State != MatchState.Playing) return;
            TowerFloorBlind blind = XORoguelikeRunManager.GetActiveBlind();
            if (blind == null) return;

            if (blind.hasMidMatchShift && !_store.MidMatchShiftTriggered && _engine.MoveHistory.Count >= blind.midMatchShiftTurn)
            {
                _store.MidMatchShiftTriggered = true;
                _audio.PlaySfx("powerup");
                _effects.ShakeBoard(0.35f, 12f);
                _effects.SpawnTacticalCalloutBanner(blind.midMatchShiftTitle, blind.midMatchShiftDesc, XOProceduralAssets.ColCoral);
                ApplyMidMatchBlindEffect(blind);
            }

            if (blind.hasCollapsingPerimeter && _engine.MoveHistory.Count > 0 && (_engine.MoveHistory.Count == 18 || (blind.blindType == BossBlindType.OverlordApex && _engine.MoveHistory.Count % 10 == 0)))
            {
                if (_engine.CollapseNextPerimeterRing(out List<XOCellPos> collapsed))
                {
                    _audio.PlaySfx("bomb");
                    foreach (XOCellPos cell in collapsed)
                        _effects.SpawnCellVFXBurst(cell.x, cell.y, XOProceduralAssets.ColCoral, XOProceduralAssets.ColSun, 10);
                    _effects.SpawnTacticalCalloutBanner("ARENA COLLAPSED!", "Outer perimeter crumbled into rubble!", XOProceduralAssets.ColCoral);
                }
            }

            if (blind.hasCosmicShift && _engine.MoveHistory.Count == 16)
            {
                if (_engine.ExecuteCosmicShift(out List<XOCellPos> struck))
                {
                    _audio.PlaySfx("lightning");
                    foreach (XOCellPos cell in struck)
                        _effects.SpawnCellVFXBurst(cell.x, cell.y, XOProceduralAssets.ColSky, Color.white, 14);
                    _effects.SpawnTacticalCalloutBanner("STORM TEMPEST!", "Second wave of lightning struck!", XOProceduralAssets.ColSky);
                }
            }
        }

        private void ApplyMidMatchBlindEffect(TowerFloorBlind blind)
        {
            if (blind.blindType == BossBlindType.TheArm) _engine.VariantRule = XOVariantRule.StrictCaro;
            else if (blind.blindType == BossBlindType.TwinDragon) _engine.RequiredWinLines = 2;
            else if (blind.blindType == BossBlindType.FogOfWar) FreezeRandomStrategicTiles(3, 2);
            else if (blind.blindType == BossBlindType.CollapsingArena && _engine.CollapseNextPerimeterRing(out List<XOCellPos> collapsed))
            {
                foreach (XOCellPos c in collapsed) _effects.SpawnCellVFXBurst(c.x, c.y, XOProceduralAssets.ColCoral, XOProceduralAssets.ColSun, 10);
            }
            else if (blind.blindType == BossBlindType.StormColosseum && _engine.ExecuteCosmicShift(out List<XOCellPos> struck))
            {
                foreach (XOCellPos c in struck) _effects.SpawnCellVFXBurst(c.x, c.y, XOProceduralAssets.ColSky, Color.white, 14);
            }
        }

        private void FreezeRandomStrategicTiles(int count, int duration)
        {
            List<XOCellPos> candidates = _engine.GetCandidateCells(2);
            int frozen = 0;
            while (candidates.Count > 0 && frozen < count)
            {
                int idx = UnityEngine.Random.Range(0, candidates.Count);
                XOCellPos pos = candidates[idx];
                candidates.RemoveAt(idx);
                if (_engine.FreezeEmptyCell(pos.x, pos.y, duration))
                {
                    _effects.SpawnCellVFXBurst(pos.x, pos.y, XOUIColors.Sky400, Color.white, 12);
                    frozen++;
                }
            }
        }

        private IEnumerator AITurnRoutine()
        {
            _store.IsAIThinking = true;
            XOMatchConfig cfg = _getConfig();
            _battle.RefreshTurnAndScore(cfg.Mode, cfg.TurnTimeLimit);

            XOBotProfile activeBot = (cfg.Mode == GameMode.VsAI) ? XOProgressionSystem.GetActiveBot() : null;
            TowerFloorBlind blind = (cfg.Mode == GameMode.TowerClimb) ? XORoguelikeRunManager.GetActiveBlind() : null;
            AIDifficulty aiDiff = (cfg.Mode == GameMode.PuzzleStage)
                ? AIDifficulty.Hard
                : (cfg.Mode == GameMode.TowerClimb && blind != null) ? blind.difficulty
                : (activeBot != null ? activeBot.baseDifficulty : cfg.Difficulty);

            while (_engine.State == MatchState.Playing && _engine.CurrentTurn == CellPiece.O)
            {
                yield return new WaitForSeconds(0.32f);

                if (!_store.BotUsedCounterSkillThisMatch && cfg.Mode == GameMode.VsAI && activeBot != null)
                {
                    if (_engine.TryExecuteBotCounterFreeze(activeBot, out XOCellPos frozenCell))
                    {
                        _store.BotUsedCounterSkillThisMatch = true;
                        _audio.PlaySfx("freeze");
                        _effects.SpawnCellVFXBurst(frozenCell.x, frozenCell.y, XOUIColors.Sky400, Color.white, 15);
                        _battle.RefreshAllCells(animateLastMove: false);
                        _effects.SpawnTacticalCalloutBanner($"{activeBot.botName.ToUpper()} COUNTER!", "Bot FROZE your open square for 3 turns!", XOUIColors.Sky600);
                        _effects.ShowBotReactionBubble($"{activeBot.botName}: \"{activeBot.PickRandom(activeBot.onBotCounterSkillLines)}\"");
                        yield return new WaitForSeconds(0.45f);
                    }
                }

                if (_engine.TryExecuteGuardianAegis(out XOCellPos _))
                {
                    _audio.PlaySfx("shield");
                    _battle.RefreshAllCells(animateLastMove: false);
                    _effects.SpawnTacticalCalloutBanner("GUARDIAN AEGIS!", "Relic auto-froze AI's winning threat for 2 turns!", XOProceduralAssets.ColLilac);
                    yield return new WaitForSeconds(0.45f);
                }

                if (cfg.Mode == GameMode.TowerClimb && blind != null)
                {
                    yield return TryExecuteBossEquipmentRoutine(blind);
                }

                XOCellPos aiMove = _engine.CalculateAIMove(CellPiece.O, aiDiff, activeBot);
                if (!aiMove.IsValid) break;
                ExecuteMove(aiMove.x, aiMove.y);

                if (cfg.Mode == GameMode.TowerClimb && blind != null && _engine.State == MatchState.Playing)
                {
                    if ((blind.botEquipment == BotEquipmentType.ThunderHammer || blind.botEquipment == BotEquipmentType.OmniCrown) && _engine.MoveHistory.Count % 8 == 0)
                    {
                        _audio.PlaySfx("lightning");
                        _effects.SpawnTacticalCalloutBanner("THUNDER HAMMER!", $"{blind.bossName} triggers a lightning DOUBLE MOVE!", XOProceduralAssets.ColSun);
                        yield return new WaitForSeconds(0.35f);
                        XOCellPos extraMove = _engine.CalculateAIMove(CellPiece.O, aiDiff, activeBot);
                        if (extraMove.IsValid) ExecuteMove(extraMove.x, extraMove.y);
                    }
                }
            }

            _store.IsAIThinking = false;
            if (_engine.State == MatchState.Playing)
            {
                _battle.RefreshTurnAndScore(cfg.Mode, cfg.TurnTimeLimit);
            }
        }

        private IEnumerator TryExecuteBossEquipmentRoutine(TowerFloorBlind blind)
        {
            if (_store.BotUsedCounterSkillThisMatch) yield break;

            if ((blind.botEquipment == BotEquipmentType.FrostStaff || blind.botEquipment == BotEquipmentType.OmniCrown)
                && _engine.TryExecuteBotCounterFreeze(new XOBotProfile { counterSkill = BotCounterSkill.CryoFreeze }, out XOCellPos frozenCell))
            {
                _store.BotUsedCounterSkillThisMatch = true;
                _audio.PlaySfx("freeze");
                _effects.SpawnCellVFXBurst(frozenCell.x, frozenCell.y, XOProceduralAssets.ColSky, Color.white, 15);
                _battle.RefreshAllCells(animateLastMove: false);
                _effects.SpawnTacticalCalloutBanner($"{blind.bossName.ToUpper()} RELIC!", "Frost Staff FROZE your threat for 3 turns!", XOProceduralAssets.ColSky);
                yield return new WaitForSeconds(0.45f);
            }
            else if (blind.botEquipment == BotEquipmentType.FlameBlade && _engine.TryExecuteBotFlameBlade(out XOCellPos bombedCell))
            {
                _store.BotUsedCounterSkillThisMatch = true;
                _audio.PlaySfx("bomb");
                _effects.SpawnCellVFXBurst(bombedCell.x, bombedCell.y, XOProceduralAssets.ColCoral, XOProceduralAssets.ColSun, 18);
                _battle.RefreshAllCells(animateLastMove: false);
                _effects.SpawnTacticalCalloutBanner($"{blind.bossName.ToUpper()} RELIC!", "Flame Blade BOMBED your critical piece!", XOProceduralAssets.ColCoral);
                yield return new WaitForSeconds(0.45f);
            }
            else if (blind.botEquipment == BotEquipmentType.ThornArmor && _engine.TryExecuteBotThornArmor(out XOCellPos _))
            {
                _store.BotUsedCounterSkillThisMatch = true;
                _audio.PlaySfx("shield");
                _battle.RefreshAllCells(animateLastMove: false);
                _effects.SpawnTacticalCalloutBanner($"{blind.bossName.ToUpper()} RELIC!", "Thorn Armor blocked an attack corridor!", XOProceduralAssets.ColMuted);
                yield return new WaitForSeconds(0.45f);
            }
        }
    }
}
