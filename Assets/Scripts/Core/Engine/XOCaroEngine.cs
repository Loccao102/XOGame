using System;
using System.Collections.Generic;

namespace XOPlayable.Core
{
    public class XOCaroEngine
    {
        public int BoardSize { get; private set; } = 15;
        public int WinLength { get; private set; } = 5;
        public XOVariantRule VariantRule { get; set; } = XOVariantRule.ClassicCaro;

        public CellPiece[,] Board { get; private set; }
        public int[,] PieceSizes { get; private set; }
        public int[,] FrozenTurns { get; private set; }
        public SpecialTileType[,] SpecialTiles { get; private set; }

        public int GobbleStockX { get; private set; } = 3;
        public int GobbleStockO { get; private set; } = 3;

        public CellPiece CurrentTurn { get; private set; } = CellPiece.X;
        public MatchState State { get; private set; } = MatchState.Playing;

        public List<MoveRecord> MoveHistory { get; private set; } = new List<MoveRecord>();
        public List<XOCellPos> WinningCells { get; private set; } = new List<XOCellPos>();

        public bool IsTowerClimb { get; set; }
        public int RequiredWinLines { get; set; } = 1;
        public List<List<XOCellPos>> CompletedWinLinesX { get; } = new List<List<XOCellPos>>();
        public List<List<XOCellPos>> CompletedWinLinesO { get; } = new List<List<XOCellPos>>();
        public int CollapsedRingsCount { get; private set; }

        private readonly List<XOCellPos> _activeQueueX = new List<XOCellPos>();
        private readonly List<XOCellPos> _activeQueueO = new List<XOCellPos>();
        private readonly XORandomStream _rng = new XORandomStream();
        private readonly XORoguelikeRunState _isolatedRunState = new XORoguelikeRunState(new XOMemoryStorage());

        private int _presetMovesCount;

        /// <summary>Optional override for relic lookups; tests can inject a fake without touching shared state.</summary>
        public Func<CaroRelicType, bool> RelicQuery { get; set; }

        /// <summary>Optional injected run/match state for roguelike relics; isolated per-engine when omitted or when RelicQuery is set.</summary>
        public XORoguelikeRunState RunContext { get; set; }

        private XORoguelikeRunState ActiveRunState
            => RunContext ?? (IsTowerClimb && RelicQuery == null ? XORoguelikeRunManager.DefaultRun : _isolatedRunState);

        private bool HasRelic(CaroRelicType type)
            => RelicQuery != null ? RelicQuery(type) : ActiveRunState.HasRelic(type, IsTowerClimb);

        public XOCaroEngine(int boardSize = 10, int winLength = 5)
        {
            Initialize(boardSize, winLength, spawnSpecialTiles: true, variant: XOVariantRule.ClassicCaro);
        }

        public void SetRandomSeed(int seed) => _rng.Reseed(seed);

        public void Initialize(
            int boardSize,
            int winLength = 5,
            bool spawnSpecialTiles = true,
            XOVariantRule variant = XOVariantRule.ClassicCaro,
            int? specialTilesSeed = null,
            int floorNumber = 0,
            bool isTowerClimb = false,
            XORoguelikeRunState runContext = null)
        {
            IsTowerClimb = isTowerClimb;
            VariantRule = variant;
            if (runContext != null) RunContext = runContext;
            if (specialTilesSeed.HasValue) _rng.Reseed(specialTilesSeed.Value);

            ResolveVariantDimensions(variant, ref boardSize, ref winLength);

            BoardSize = XOMath.Clamp(boardSize, 3, 19);
            WinLength = XOMath.Clamp(winLength, 3, BoardSize);
            Board = new CellPiece[BoardSize, BoardSize];
            PieceSizes = new int[BoardSize, BoardSize];
            FrozenTurns = new int[BoardSize, BoardSize];
            SpecialTiles = new SpecialTileType[BoardSize, BoardSize];
            GobbleStockX = (BoardSize >= 10) ? 6 : 3;
            GobbleStockO = (BoardSize >= 10) ? 6 : 3;
            CurrentTurn = CellPiece.X;
            State = MatchState.Playing;
            MoveHistory.Clear();
            WinningCells.Clear();
            RequiredWinLines = 1;
            CompletedWinLinesX.Clear();
            CompletedWinLinesO.Clear();
            CollapsedRingsCount = 0;
            _activeQueueX.Clear();
            _activeQueueO.Clear();
            _presetMovesCount = 0;

            if (spawnSpecialTiles)
                SpawnBalancedSpecialTiles(specialTilesSeed, floorNumber);
        }

        private static void ResolveVariantDimensions(XOVariantRule variant, ref int boardSize, ref int winLength)
        {
            if (variant == XOVariantRule.Infinite3x3)
            {
                boardSize = 3;
                winLength = 3;
            }
            else if (variant == XOVariantRule.GobbletStack)
            {
                if (boardSize < 10) { boardSize = 4; winLength = 4; }
                else winLength = 5;
            }
            else if (variant == XOVariantRule.GravityDrop)
            {
                if (boardSize < 10) { boardSize = 8; winLength = 4; }
                else winLength = 5;
            }
        }

        public void SpawnBalancedSpecialTiles(int? seed = null, int floorNumber = 0)
            => XOSpecialTileSystem.SpawnBalancedSpecialTiles(this, _rng, seed, floorNumber, HasRelic(CaroRelicType.MinePlanter));

        public void LoadPuzzleStage(XOPuzzleStage stage)
        {
            Initialize(BoardSize, WinLength, spawnSpecialTiles: false, variant: XOVariantRule.ClassicCaro);
            if (stage == null || stage.presetMoves == null) return;

            foreach (MoveRecord m in stage.presetMoves)
            {
                if (IsValidCell(m.row, m.col) && Board[m.row, m.col] == CellPiece.Empty)
                {
                    Board[m.row, m.col] = m.piece;
                    MoveHistory.Add(m);
                }
            }

            _presetMovesCount = MoveHistory.Count;
            CurrentTurn = CellPiece.X;
            State = MatchState.Playing;
        }

        public int GetPlayerMadeMovesCount()
        {
            int count = 0;
            for (int i = _presetMovesCount; i < MoveHistory.Count; i++)
            {
                if (MoveHistory[i].piece == CellPiece.X && !MoveHistory[i].isBonusTwin)
                    count++;
            }
            return count;
        }

        public bool IsValidCell(int r, int c) => r >= 0 && r < BoardSize && c >= 0 && c < BoardSize;
        public bool IsCellFrozen(int r, int c) => IsValidCell(r, c) && FrozenTurns[r, c] > 0;
        public bool IsRockObstacle(int r, int c) => IsValidCell(r, c) && SpecialTiles[r, c] == SpecialTileType.RockObstacle;

        public bool IsPieceAboutToVanish(int r, int c)
        {
            if (VariantRule != XOVariantRule.Infinite3x3 || !IsValidCell(r, c)) return false;
            CellPiece p = Board[r, c];
            if (p == CellPiece.X && _activeQueueX.Count >= 3)
                return _activeQueueX[0].x == r && _activeQueueX[0].y == c;
            if (p == CellPiece.O && _activeQueueO.Count >= 3)
                return _activeQueueO[0].x == r && _activeQueueO[0].y == c;
            return false;
        }

        public int GetGravityLandingRow(int col)
        {
            if (col < 0 || col >= BoardSize) return -1;
            for (int r = BoardSize - 1; r >= 0; r--)
            {
                if (Board[r, col] != CellPiece.Empty) continue;
                if (SpecialTiles[r, col] == SpecialTileType.RockObstacle) continue;
                if (FrozenTurns[r, col] > 0) return -1;
                return r;
            }
            return -1;
        }

        public bool CanPlace(int r, int c) => CanPlaceForPiece(r, c, CurrentTurn);

        public bool HasAnyLegalMove()
        {
            if (State != MatchState.Playing) return false;
            if (VariantRule == XOVariantRule.GravityDrop)
            {
                for (int c = 0; c < BoardSize; c++)
                    if (GetGravityLandingRow(c) >= 0) return true;
                return false;
            }

            for (int r = 0; r < BoardSize; r++)
                for (int c = 0; c < BoardSize; c++)
                    if (CanPlace(r, c)) return true;
            return false;
        }

        public void PassTurn()
        {
            if (State != MatchState.Playing) return;
            TickDownFrozenCells();
            CurrentTurn = (CurrentTurn == CellPiece.X) ? CellPiece.O : CellPiece.X;
        }

        /// <summary>Forces the active turn piece (used for multi-action boss equipment like Thunder Hammer).</summary>
        public void ForceCurrentTurn(CellPiece turn) => CurrentTurn = turn;

        public bool CanPlaceForPiece(int r, int c, CellPiece mover)
        {
            if (State != MatchState.Playing || !IsValidCell(r, c) || FrozenTurns[r, c] > 0 || SpecialTiles[r, c] == SpecialTileType.RockObstacle)
                return false;

            if (Board[r, c] == CellPiece.Empty) return true;

            if (VariantRule == XOVariantRule.GobbletStack && mover != CellPiece.Empty)
            {
                CellPiece opponent = (mover == CellPiece.X) ? CellPiece.O : CellPiece.X;
                int stock = (mover == CellPiece.X) ? GobbleStockX : GobbleStockO;
                if (Board[r, c] == opponent && PieceSizes[r, c] < 3 && stock > 0)
                    return true;
            }

            return false;
        }

        public PlaceResult TryPlace(int r, int c)
        {
            if (VariantRule == XOVariantRule.GravityDrop)
            {
                int landingRow = GetGravityLandingRow(c);
                if (landingRow < 0) return PlaceResult.Rejected;
                r = landingRow;
            }

            CellPiece placed = CurrentTurn;
            CellPiece opponent = (placed == CellPiece.X) ? CellPiece.O : CellPiece.X;
            if (!CanPlaceForPiece(r, c, placed)) return PlaceResult.Rejected;

            var undo = new MoveUndoInfo
            {
                consumedTile = SpecialTiles[r, c],
                winLinesXBefore = CompletedWinLinesX.Count,
                winLinesOBefore = CompletedWinLinesO.Count
            };

            CellPiece gobbledPiece = CellPiece.Empty;
            int gobbledPrevSize = 0;
            int newPieceSize = 1;
            if (VariantRule == XOVariantRule.GobbletStack && Board[r, c] == opponent)
            {
                gobbledPiece = opponent;
                gobbledPrevSize = XOMath.Max(1, PieceSizes[r, c]);
                newPieceSize = XOMath.Min(3, gobbledPrevSize + 1);
                if (placed == CellPiece.X) GobbleStockX--;
                else GobbleStockO--;
            }

            long atkScore = EvaluatePoint(r, c, placed);
            long defScore = EvaluatePoint(r, c, opponent);

            TickDownFrozenCells(undo.tickedFrozen);

            SpecialTileType triggeredSpecial = SpecialTileType.None;
            if (SpecialTiles[r, c] != SpecialTileType.None && SpecialTiles[r, c] != SpecialTileType.RockObstacle)
            {
                triggeredSpecial = SpecialTiles[r, c];
                SpecialTiles[r, c] = SpecialTileType.None;
            }

            XOCellPos vanishedCell = PopVanishedInfinitePiece(placed);
            Board[r, c] = placed;
            PieceSizes[r, c] = newPieceSize;
            GetQueue(placed).Add(new XOCellPos(r, c));
            MoveHistory.Add(new MoveRecord(r, c, placed, vanishedCell.x, vanishedCell.y, gobbledPiece, gobbledPrevSize, isBonusTwin: false, undo: undo));

            if (triggeredSpecial == SpecialTileType.HiddenMine)
                XOSpecialTileSystem.ExecuteMineBlastAround(this, _rng, r, c, undo.blasted);

            XOCellPos twinPos = (triggeredSpecial == SpecialTileType.WarpPortal)
                ? XOSpecialTileSystem.ExecuteWarpPortalClone(this, _rng, r, c, placed)
                : XOCellPos.Invalid;

            string callout = null;
            int bonusCoins = 0;
            List<XOCellPos> finalWinLine = null;
            bool completedNewWinLine = false;
            bool blockedStrictFive = false;

            bool wonHere = EvaluateWinOutcome(r, c, placed, ref finalWinLine, ref completedNewWinLine, ref blockedStrictFive, ref callout, ref bonusCoins);
            if (!wonHere && twinPos.IsValid)
            {
                bool unusedBlocked = false;
                wonHere = EvaluateWinOutcome(twinPos.x, twinPos.y, placed, ref finalWinLine, ref completedNewWinLine, ref unusedBlocked, ref callout, ref bonusCoins);
            }

            if (wonHere)
            {
                State = (placed == CellPiece.X) ? MatchState.XWon : MatchState.OWon;
                return new PlaceResult(
                    placed: true, landingCell: new XOCellPos(r, c), placedPiece: placed, newPieceSize: newPieceSize,
                    gobbledPrevPiece: gobbledPiece, gobbledPrevSize: gobbledPrevSize, triggeredSpecial: triggeredSpecial,
                    tacticalCallout: callout, calloutBonusCoins: bonusCoins, vanishedCell: vanishedCell,
                    twinCell: twinPos, blastedPieces: undo.blasted, winningLine: finalWinLine,
                    completedNewWinLine: completedNewWinLine, strictBlockedFive: blockedStrictFive,
                    grantedExtraTurn: false, resultState: State);
            }

            EvaluateTacticalCallout(gobbledPiece, newPieceSize, blockedStrictFive, defScore, atkScore, ref callout, ref bonusCoins);
            ApplyPlayerRelicsOnPlace(r, c, placed, ref callout, ref bonusCoins);

            bool grantedExtraTurn = false;
            if (CountPlaceableEmptyCells() == 0 && !AnyGobbleAvailable())
            {
                State = MatchState.Draw;
            }
            else if (triggeredSpecial == SpecialTileType.ExtraTurnLightning)
            {
                grantedExtraTurn = true;
            }
            else if (IsTowerClimb && placed == CellPiece.X && TryCheckTwinThreat(r, c))
            {
                callout = "TWIN THREAT RELIC! +1 EXTRA TURN";
                bonusCoins += 15;
                CurrentTurn = CellPiece.X;
                grantedExtraTurn = true;
            }
            else if (IsTowerClimb && placed == CellPiece.X && TryCheckCounterstrike(defScore))
            {
                callout = "COUNTERSTRIKE! +1 EXTRA TURN";
                bonusCoins += 15;
                CurrentTurn = CellPiece.X;
                grantedExtraTurn = true;
            }
            else
            {
                CurrentTurn = opponent;
            }

            return new PlaceResult(
                placed: true, landingCell: new XOCellPos(r, c), placedPiece: placed, newPieceSize: newPieceSize,
                gobbledPrevPiece: gobbledPiece, gobbledPrevSize: gobbledPrevSize, triggeredSpecial: triggeredSpecial,
                tacticalCallout: callout, calloutBonusCoins: bonusCoins, vanishedCell: vanishedCell,
                twinCell: twinPos, blastedPieces: undo.blasted, winningLine: finalWinLine,
                completedNewWinLine: completedNewWinLine, strictBlockedFive: blockedStrictFive,
                grantedExtraTurn: grantedExtraTurn, resultState: State);
        }

        public bool PlacePiece(int r, int c, out SpecialTileType triggeredSpecial, out string tacticalCallout, out int calloutBonusCoins)
        {
            PlaceResult res = TryPlace(r, c);
            triggeredSpecial = res.TriggeredSpecial;
            tacticalCallout = res.TacticalCallout;
            calloutBonusCoins = res.CalloutBonusCoins;
            return res.Placed;
        }

        public bool PlacePiece(int r, int c) => TryPlace(r, c).Placed;

        private bool EvaluateWinOutcome(
            int r,
            int c,
            CellPiece placed,
            ref List<XOCellPos> finalWinLine,
            ref bool completedNewWinLine,
            ref bool blockedStrictFive,
            ref string callout,
            ref int bonusCoins)
        {
            if (!CheckWinFromCell(r, c, placed, out List<XOCellPos> winLine, out bool blocked))
            {
                blockedStrictFive |= blocked;
                return false;
            }

            if (!RegisterWinLineIfValid(winLine, placed, out bool won)) return false;

            completedNewWinLine = true;
            finalWinLine = winLine;
            if (won) return true;

            callout = $"1ST FIVE LOCKED! (1/{RequiredWinLines})";
            bonusCoins = 25;
            return false;
        }

        private void EvaluateTacticalCallout(
            CellPiece gobbledPiece,
            int newPieceSize,
            bool blockedStrictFive,
            long defScore,
            long atkScore,
            ref string callout,
            ref int bonusCoins)
        {
            if (gobbledPiece != CellPiece.Empty)
            {
                callout = $"GOBBLED ENEMY! (LV.{newPieceSize})";
                bonusCoins = 12;
            }
            else if (blockedStrictFive)
            {
                callout = "BLOCKED BOTH ENDS! (NO WIN)";
                bonusCoins = 5;
            }
            else if (defScore >= 14000)
            {
                callout = "CRITICAL BLOCK!";
                bonusCoins = 10;
            }
            else if (atkScore >= 35000)
            {
                callout = "DOUBLE THREAT FORK!";
                bonusCoins = 15;
            }
            else if (atkScore >= 20000 && WinLength >= 4)
            {
                callout = (WinLength == 4) ? "THREE IN A ROW!" : "FOUR IN A ROW!";
                bonusCoins = 8;
            }
        }

        private void ApplyPlayerRelicsOnPlace(int r, int c, CellPiece placed, ref string callout, ref int bonusCoins)
        {
            if (placed != CellPiece.X || !IsTowerClimb) return;

            if (HasRelic(CaroRelicType.AlchemistStone) && _rng.Value < 0.25f)
            {
                SpecialTiles[r, c] = SpecialTileType.GoldBonus;
                callout = "ALCHEMIST STONE! (+10 GOLD TILE)";
                bonusCoins += 10;
            }

            if (HasRelic(CaroRelicType.KnightStep))
            {
                for (int m = 0; m < MoveHistory.Count - 1; m++)
                {
                    if (MoveHistory[m].piece != CellPiece.X) continue;
                    int drow = XOMath.Abs(r - MoveHistory[m].row);
                    int dcol = XOMath.Abs(c - MoveHistory[m].col);
                    if ((drow == 1 && dcol == 2) || (drow == 2 && dcol == 1))
                    {
                        callout = "KNIGHT'S LEAP! +10 COINS";
                        bonusCoins += 10;
                        break;
                    }
                }
            }
        }

        private XOCellPos PopVanishedInfinitePiece(CellPiece placed)
        {
            List<XOCellPos> activeQueue = GetQueue(placed);
            if (VariantRule != XOVariantRule.Infinite3x3 || activeQueue.Count < 3)
                return XOCellPos.Invalid;

            XOCellPos oldest = activeQueue[0];
            activeQueue.RemoveAt(0);
            if (Board[oldest.x, oldest.y] == placed)
            {
                Board[oldest.x, oldest.y] = CellPiece.Empty;
                PieceSizes[oldest.x, oldest.y] = 0;
                return oldest;
            }
            return XOCellPos.Invalid;
        }

        private List<XOCellPos> GetQueue(CellPiece piece) => (piece == CellPiece.X) ? _activeQueueX : _activeQueueO;

        internal void PlaceBonusTwinInternal(int r, int c, CellPiece piece)
        {
            var undo = new MoveUndoInfo
            {
                consumedTile = SpecialTiles[r, c],
                winLinesXBefore = CompletedWinLinesX.Count,
                winLinesOBefore = CompletedWinLinesO.Count
            };
            SpecialTiles[r, c] = SpecialTileType.None;
            Board[r, c] = piece;
            PieceSizes[r, c] = 1;
            MoveHistory.Add(new MoveRecord(r, c, piece, isBonusTwin: true, undo: undo));
        }

        internal void ClearCellInternal(int r, int c)
        {
            Board[r, c] = CellPiece.Empty;
            PieceSizes[r, c] = 0;
            RemoveFromQueuesInternal(new XOCellPos(r, c));
        }

        internal void RemoveFromQueuesInternal(XOCellPos pos)
        {
            _activeQueueX.Remove(pos);
            _activeQueueO.Remove(pos);
        }

        internal void TransferCellQueueOwnerInternal(XOCellPos pos, CellPiece newOwner)
        {
            _activeQueueX.Remove(pos);
            _activeQueueO.Remove(pos);
            GetQueue(newOwner).Add(pos);
        }

        internal void RemoveLastHistoryAtInternal(int r, int c)
        {
            for (int i = MoveHistory.Count - 1; i >= 0; i--)
            {
                if (MoveHistory[i].row == r && MoveHistory[i].col == c)
                {
                    MoveHistory.RemoveAt(i);
                    if (i < _presetMovesCount) _presetMovesCount--;
                    break;
                }
            }
        }

        internal void ReplaceLastHistoryOwnerInternal(int r, int c, CellPiece newOwner)
        {
            for (int i = MoveHistory.Count - 1; i >= 0; i--)
            {
                if (MoveHistory[i].row == r && MoveHistory[i].col == c)
                {
                    MoveHistory[i] = new MoveRecord(r, c, newOwner);
                    break;
                }
            }
        }

        internal void SetMatchStateInternal(MatchState state) => State = state;
        internal void IncrementCollapsedRingsInternal() => CollapsedRingsCount++;
        internal bool WouldWinImmediatelyInternal(int r, int c, CellPiece piece) => WouldWinImmediately(r, c, piece);
        internal bool HasRelicInternal(CaroRelicType type) => HasRelic(type);

        private int CountPlaceableEmptyCells()
        {
            int count = 0;
            for (int r = 0; r < BoardSize; r++)
                for (int c = 0; c < BoardSize; c++)
                    if (Board[r, c] == CellPiece.Empty && SpecialTiles[r, c] != SpecialTileType.RockObstacle)
                        count++;
            return count;
        }

        public bool BombDestroyPiece(int r, int c, CellPiece targetPiece = CellPiece.O)
            => XOCaroSkillsAndRelics.BombDestroyPiece(this, r, c, targetPiece);

        public bool SwapOpponentPiece(int r, int c, CellPiece targetOpponent = CellPiece.O, CellPiece newOwner = CellPiece.X)
            => XOCaroSkillsAndRelics.SwapOpponentPiece(this, r, c, targetOpponent, newOwner);

        public bool FreezeEmptyCell(int r, int c, int turns = 3)
            => XOCaroSkillsAndRelics.FreezeEmptyCell(this, r, c, turns);

        public bool TryExecuteBotCounterFreeze(XOBotProfile bot, out XOCellPos frozenCell)
            => XOCaroSkillsAndRelics.TryExecuteBotCounterFreeze(this, bot, out frozenCell);

        public bool TryExecuteBotFlameBlade(out XOCellPos bombedCell)
            => XOCaroSkillsAndRelics.TryExecuteBotFlameBlade(this, out bombedCell);

        public bool TryExecuteBotThornArmor(out XOCellPos thornCell)
            => XOCaroSkillsAndRelics.TryExecuteBotThornArmor(this, out thornCell);

        public bool IsBotImmuneToSkill(BotEquipmentType equipment, string skillName)
            => XOCaroSkillsAndRelics.IsBotImmuneToSkill(equipment, skillName);

        public void ApplyCenterVoid(int radius = 1)
            => XOSpecialTileSystem.ApplyCenterVoid(this, radius);

        public bool TryCheckTwinThreat(int r, int c)
        {
            XORoguelikeRunState state = ActiveRunState;
            if (!IsTowerClimb || !HasRelic(CaroRelicType.TwinThreat) || state.TwinThreatTriggeredThisMatch)
                return false;

            if (!XOWinDetector.IsOpenThreeAtCell(Board, BoardSize, r, c, CellPiece.X))
                return false;

            state.TwinThreatTriggeredThisMatch = true;
            return true;
        }

        /// <summary>Counterstrike relic: occupying a cell that would have given the boss a four (or a fork) grants one extra turn per match.</summary>
        public bool TryCheckCounterstrike(long defScore)
        {
            XORoguelikeRunState state = ActiveRunState;
            if (!IsTowerClimb || !HasRelic(CaroRelicType.Counterstrike) || state.CounterstrikeTriggeredThisMatch)
                return false;
            if (defScore < 22000) return false;

            state.CounterstrikeTriggeredThisMatch = true;
            return true;
        }

        public bool TryExecuteGuardianAegis(out XOCellPos frozenCell)
            => XOCaroSkillsAndRelics.TryExecuteGuardianAegis(this, ActiveRunState, HasRelic(CaroRelicType.GuardianAegis), out frozenCell);

        public bool TryExecuteShatterHammer(int r, int c, out XOCellPos shatteredCell)
            => XOCaroSkillsAndRelics.TryExecuteShatterHammer(this, ActiveRunState, HasRelic(CaroRelicType.ShatterHammer), r, c, out shatteredCell);

        private void TickDownFrozenCells(List<XOCellPos> ticked = null)
        {
            for (int r = 0; r < BoardSize; r++)
            {
                for (int c = 0; c < BoardSize; c++)
                {
                    if (FrozenTurns[r, c] > 0)
                    {
                        FrozenTurns[r, c]--;
                        ticked?.Add(new XOCellPos(r, c));
                    }
                }
            }
        }

        public MoveRecord? GetLastMove()
            => MoveHistory.Count == 0 ? (MoveRecord?)null : MoveHistory[MoveHistory.Count - 1];

        public bool IsWinningLineDiagonal() => XOWinDetector.IsWinningLineDiagonal(WinningCells);
        public bool HasUndoablePlayerMove() => MoveHistory.Count > _presetMovesCount;

        public bool UndoMove(GameMode mode, CellPiece humanPiece = CellPiece.X)
        {
            if (MoveHistory.Count <= _presetMovesCount) return false;
            State = MatchState.Playing;

            if (mode == GameMode.VsAI || mode == GameMode.PuzzleStage || mode == GameMode.TowerClimb)
            {
                while (MoveHistory.Count > _presetMovesCount && MoveHistory[MoveHistory.Count - 1].piece != humanPiece)
                    PopLogicalMove();
                if (MoveHistory.Count > _presetMovesCount && MoveHistory[MoveHistory.Count - 1].piece == humanPiece)
                    PopLogicalMove();
                CurrentTurn = humanPiece;
            }
            else
            {
                MoveRecord popped = PopLogicalMove();
                CurrentTurn = popped.piece;
            }

            RebuildWinningCells();
            return true;
        }

        public bool ReviveFromAILoss(out XOCellPos criticalBlockCell)
        {
            criticalBlockCell = XOCellPos.Invalid;
            if (MoveHistory.Count <= _presetMovesCount) return false;

            MoveRecord lastMove = MoveHistory[MoveHistory.Count - 1];
            criticalBlockCell = new XOCellPos(lastMove.row, lastMove.col);

            PopLogicalMove();
            RebuildWinningCells();
            State = MatchState.Playing;
            CurrentTurn = CellPiece.X;
            return true;
        }

        public bool IncinerateLastWinningPiece(out XOCellPos tilePos, int freezeTurns = 2)
        {
            tilePos = XOCellPos.Invalid;
            if (MoveHistory.Count <= _presetMovesCount) return false;

            MoveRecord lastMove = MoveHistory[MoveHistory.Count - 1];
            tilePos = new XOCellPos(lastMove.row, lastMove.col);

            PopLogicalMove();
            if (freezeTurns > 0 && IsValidCell(lastMove.row, lastMove.col))
                FrozenTurns[lastMove.row, lastMove.col] = freezeTurns;

            RebuildWinningCells();
            State = MatchState.Playing;
            CurrentTurn = CellPiece.X;
            return true;
        }

        private MoveRecord PopSingleMove()
        {
            int lastIdx = MoveHistory.Count - 1;
            MoveRecord rec = MoveHistory[lastIdx];
            MoveHistory.RemoveAt(lastIdx);

            List<XOCellPos> queue = GetQueue(rec.piece);
            queue.Remove(new XOCellPos(rec.row, rec.col));

            if (rec.gobbledPiece != CellPiece.Empty)
            {
                Board[rec.row, rec.col] = rec.gobbledPiece;
                PieceSizes[rec.row, rec.col] = XOMath.Max(1, rec.gobbledPrevSize);
                if (rec.piece == CellPiece.X) GobbleStockX++;
                else GobbleStockO++;
            }
            else
            {
                Board[rec.row, rec.col] = CellPiece.Empty;
                PieceSizes[rec.row, rec.col] = 0;
            }

            if (rec.vanishedRow >= 0 && rec.vanishedCol >= 0 && IsValidCell(rec.vanishedRow, rec.vanishedCol))
            {
                Board[rec.vanishedRow, rec.vanishedCol] = rec.piece;
                PieceSizes[rec.vanishedRow, rec.vanishedCol] = 1;
                queue.Insert(0, new XOCellPos(rec.vanishedRow, rec.vanishedCol));
            }

            MoveUndoInfo info = rec.undo;
            if (info != null)
            {
                if (IsValidCell(rec.row, rec.col)) SpecialTiles[rec.row, rec.col] = info.consumedTile;
                foreach (XOCellPos cell in info.tickedFrozen) FrozenTurns[cell.x, cell.y]++;

                foreach (BlastedPiece b in info.blasted)
                {
                    Board[b.row, b.col] = b.piece;
                    PieceSizes[b.row, b.col] = b.size;
                    GetQueue(b.piece).Add(new XOCellPos(b.row, b.col));
                }

                if (CompletedWinLinesX.Count > info.winLinesXBefore)
                    CompletedWinLinesX.RemoveRange(info.winLinesXBefore, CompletedWinLinesX.Count - info.winLinesXBefore);
                if (CompletedWinLinesO.Count > info.winLinesOBefore)
                    CompletedWinLinesO.RemoveRange(info.winLinesOBefore, CompletedWinLinesO.Count - info.winLinesOBefore);
            }

            return rec;
        }

        private MoveRecord PopLogicalMove()
        {
            MoveRecord rec = PopSingleMove();
            while (rec.isBonusTwin && MoveHistory.Count > _presetMovesCount)
                rec = PopSingleMove();
            return rec;
        }

        private void RebuildWinningCells()
        {
            WinningCells.Clear();
            foreach (var line in CompletedWinLinesX)
                foreach (var pt in line) if (!WinningCells.Contains(pt)) WinningCells.Add(pt);
            foreach (var line in CompletedWinLinesO)
                foreach (var pt in line) if (!WinningCells.Contains(pt)) WinningCells.Add(pt);
        }

        private bool AnyGobbleAvailable()
        {
            if (VariantRule != XOVariantRule.GobbletStack) return false;
            if (GobbleStockX <= 0 && GobbleStockO <= 0) return false;
            for (int r = 0; r < BoardSize; r++)
                for (int c = 0; c < BoardSize; c++)
                    if (Board[r, c] != CellPiece.Empty && (CanPlaceForPiece(r, c, CellPiece.X) || CanPlaceForPiece(r, c, CellPiece.O)))
                        return true;
            return false;
        }

        public bool CheckWinFromCell(int r, int c, CellPiece piece, out List<XOCellPos> winLine)
            => CheckWinFromCell(r, c, piece, out winLine, out _);

        public bool CheckWinFromCell(int r, int c, CellPiece piece, out List<XOCellPos> winLine, out bool blockedStrictFive)
            => XOWinDetector.CheckWinFromCell(
                Board, BoardSize, WinLength, VariantRule, IsTowerClimb, HasRelic(CaroRelicType.DiagonalPrism),
                r, c, piece, out winLine, out blockedStrictFive);

        public bool IsDistinctWinLine(List<XOCellPos> newLine, List<List<XOCellPos>> existingLines)
            => XOWinDetector.IsDistinctWinLine(newLine, existingLines);

        public bool RegisterWinLineIfValid(List<XOCellPos> winLine, CellPiece piece, out bool reachedTargetWins)
            => XOWinDetector.RegisterWinLineIfValid(
                winLine, piece, RequiredWinLines, CompletedWinLinesX, CompletedWinLinesO, WinningCells, out reachedTargetWins);

        public bool CollapseNextPerimeterRing(out List<XOCellPos> newlyCollapsed)
            => XOSpecialTileSystem.CollapseNextPerimeterRing(this, out newlyCollapsed);

        public bool ExecuteCosmicShift(out List<XOCellPos> affectedCells)
            => XOSpecialTileSystem.ExecuteCosmicShift(this, _rng, out affectedCells);

        public XOCellPos CalculateAIMove(CellPiece aiPiece, AIDifficulty difficulty, XOBotProfile botProfile = null)
            => XOHeuristicAI.CalculateAIMove(this, _rng, aiPiece, difficulty, botProfile, HasRelic(CaroRelicType.CrownOfOmni));

        public XOCellPos CalculateBestHint(CellPiece playerPiece, out string tacticalReason)
            => XOHeuristicAI.CalculateBestHint(this, playerPiece, out tacticalReason);

        private bool WouldWinImmediately(int r, int c, CellPiece piece)
        {
            if (!CanPlaceForPiece(r, c, piece)) return false;

            CellPiece prevOccupant = Board[r, c];
            XOCellPos tempVanished = XOCellPos.Invalid;
            List<XOCellPos> queue = GetQueue(piece);
            if (VariantRule == XOVariantRule.Infinite3x3 && queue.Count >= 3)
            {
                tempVanished = queue[0];
                if (Board[tempVanished.x, tempVanished.y] == piece)
                    Board[tempVanished.x, tempVanished.y] = CellPiece.Empty;
            }

            Board[r, c] = piece;
            bool createsLine = CheckWinFromCell(r, c, piece, out List<XOCellPos> winLine, out _);
            Board[r, c] = prevOccupant;

            if (tempVanished.IsValid) Board[tempVanished.x, tempVanished.y] = piece;
            if (!createsLine) return false;

            List<List<XOCellPos>> list = (piece == CellPiece.X) ? CompletedWinLinesX : CompletedWinLinesO;
            return list.Count >= RequiredWinLines - 1 && IsDistinctWinLine(winLine, list);
        }

        public List<XOCellPos> GetCandidateCells(int radius) => XOHeuristicAI.GetCandidateCells(this, radius);
        public long EvaluatePoint(int r, int c, CellPiece piece) => XOHeuristicAI.EvaluatePoint(this, r, c, piece);
    }
}

