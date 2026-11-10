using System;
using System.Collections.Generic;
using UnityEngine;

namespace XOPlayable.Core
{
    public enum CellPiece
    {
        Empty = 0,
        X = 1,
        O = 2
    }

    public enum XOVariantRule
    {
        ClassicCaro = 0,    // Standard 5-in-a-Row (10x10 or 15x15) with Gold/Lightning/Rock tiles
        Infinite3x3 = 1,    // 3x3 Bolt (3-in-a-Row): Max 3 pieces per side; placing 4th piece vanishes oldest piece!
        GravityDrop = 2,    // 8x8 Connect-4 Gravity (4-in-a-Row): Pieces drop to the lowest empty row in the clicked column!
        StrictCaro = 3,     // 10x10 Blocked-Ends Caro: Exact 5-in-a-row blocked on BOTH ends by enemy does NOT win!
        ChaosMinefield = 4, // 10x10 Minefield & Warp Chaos: Adds Mine tiles (blasts adjacent pieces) & Warp Portals (spawns Twin piece!)
        GobbletStack = 5    // 4x4 Gobblet XO: Each side can Gobble (eat) smaller enemy pieces (Lv.1 -> Lv.2 -> Lv.3)!
    }

    public enum SpecialTileType
    {
        None = 0,
        GoldBonus = 1,          // Grants +20 Coins when claimed
        ExtraTurnLightning = 2, // Grants an immediate Bonus Turn (both Player X and Bot O can claim!)
        RockObstacle = 3,       // Impassable rock obstacle on the board
        HiddenMine = 4,         // Chaos Variant: Blasts up to 2 adjacent pieces around this cell when stepped on!
        WarpPortal = 5          // Chaos Variant: Spawns a Bonus Twin piece on the linked Warp Portal tile!
    }

    public enum GameMode
    {
        VsAI = 0,
        TwoPlayerLocal = 1,
        PuzzleStage = 2,
        TowerClimb = 3
    }

    public enum AIDifficulty
    {
        Easy = 0,
        Normal = 1,
        Hard = 2
    }

    public enum MatchState
    {
        Playing = 0,
        XWon = 1,
        OWon = 2,
        Draw = 3
    }

    [Serializable]
    public struct MoveRecord
    {
        public int row;
        public int col;
        public CellPiece piece;
        public int vanishedRow;
        public int vanishedCol;
        public CellPiece gobbledPiece;
        public int gobbledPrevSize;

        public MoveRecord(int row, int col, CellPiece piece, int vanishedRow = -1, int vanishedCol = -1, CellPiece gobbledPiece = CellPiece.Empty, int gobbledPrevSize = 0)
        {
            this.row = row;
            this.col = col;
            this.piece = piece;
            this.vanishedRow = vanishedRow;
            this.vanishedCol = vanishedCol;
            this.gobbledPiece = gobbledPiece;
            this.gobbledPrevSize = gobbledPrevSize;
        }
    }

    internal static class XORandom
    {
        private static readonly System.Random _rnd = new System.Random();
        public static int Range(int min, int max) => (min >= max) ? min : _rnd.Next(min, max);
        public static float Range(float min, float max) => (min >= max) ? min : (float)(min + _rnd.NextDouble() * (max - min));
        public static float Value => (float)_rnd.NextDouble();
    }

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
        public List<Vector2Int> WinningCells { get; private set; } = new List<Vector2Int>();

        // Roguelike Multi-Line Win Conditions (e.g. Twin Dragon 2x5)
        public int RequiredWinLines { get; set; } = 1;
        public List<List<Vector2Int>> CompletedWinLinesX { get; } = new List<List<Vector2Int>>();
        public List<List<Vector2Int>> CompletedWinLinesO { get; } = new List<List<Vector2Int>>();
        public int CollapsedRingsCount { get; private set; } = 0;

        // Active queues for Infinite3x3 Vanishing Rule (Max 3 pieces per player)
        private readonly List<Vector2Int> _activeQueueX = new List<Vector2Int>();
        private readonly List<Vector2Int> _activeQueueO = new List<Vector2Int>();

        private int _presetMovesCount = 0;

        private static readonly int[] DirRow = { 0, 1, 1, 1 };
        private static readonly int[] DirCol = { 1, 0, 1, -1 };

        public XOCaroEngine(int boardSize = 10, int winLength = 5)
        {
            Initialize(boardSize, winLength, spawnSpecialTiles: true, variant: XOVariantRule.ClassicCaro);
        }

        public void Initialize(int boardSize, int winLength = 5, bool spawnSpecialTiles = true, XOVariantRule variant = XOVariantRule.ClassicCaro, int? specialTilesSeed = null, int floorNumber = 0)
        {
            VariantRule = variant;

            if (variant == XOVariantRule.Infinite3x3)
            {
                boardSize = 3;
                winLength = 3;
            }
            else if (variant == XOVariantRule.GobbletStack)
            {
                if (boardSize < 10)
                {
                    boardSize = 4;
                    winLength = 4;
                }
                else
                {
                    winLength = 5; // 15x15 Caro map with 5-in-a-row Gobblet stacking!
                }
            }
            else if (variant == XOVariantRule.GravityDrop)
            {
                if (boardSize < 10)
                {
                    boardSize = 8;
                    winLength = 4;
                }
                else
                {
                    winLength = 5; // 15x15 Caro map with 5-in-a-row Gravity Drop!
                }
            }

            BoardSize = Mathf.Clamp(boardSize, 3, 19);
            WinLength = Mathf.Clamp(winLength, 3, BoardSize);
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
            {
                SpawnBalancedSpecialTiles(specialTilesSeed, floorNumber);
            }
        }

        /// <summary>
        /// Spawns a balanced set of Special Tiles around the mid-board.
        /// In ChaosMinefield mode, also spawns 3 Mine tiles and 2 linked Warp Portal tiles!
        /// Supports deterministic seeding per run/floor!
        /// </summary>
        public void SpawnBalancedSpecialTiles(int? seed = null, int floorNumber = 0)
        {
            if (BoardSize <= 4)
            {
                // On 3x3 (Infinite) or 4x4 (Gobblet), spawn 1 Gold Bonus tile on a corner/edge so it never blocks lines
                SpecialTiles[0, BoardSize - 1] = SpecialTileType.GoldBonus;
                return;
            }

            int mid = BoardSize / 2;
            int minCoord = Mathf.Max(1, mid - 3);
            int maxCoord = Mathf.Min(BoardSize - 2, mid + 3);

            List<Vector2Int> pool = new List<Vector2Int>();
            for (int r = minCoord; r <= maxCoord; r++)
            {
                for (int c = minCoord; c <= maxCoord; c++)
                {
                    if (Mathf.Abs(r - mid) <= 1 && Mathf.Abs(c - mid) <= 1)
                        continue;
                    pool.Add(new Vector2Int(r, c));
                }
            }

            System.Random seededRnd = seed.HasValue ? new System.Random(seed.Value) : null;
            for (int i = 0; i < pool.Count; i++)
            {
                int swapIdx = (seededRnd != null) ? seededRnd.Next(i, pool.Count) : XORandom.Range(i, pool.Count);
                (pool[i], pool[swapIdx]) = (pool[swapIdx], pool[i]);
            }

            int cursor = 0;
            void PlaceSpecial(SpecialTileType type, int count)
            {
                for (int k = 0; k < count && cursor < pool.Count; k++, cursor++)
                {
                    Vector2Int pos = pool[cursor];
                    SpecialTiles[pos.x, pos.y] = type;
                }
            }

            if (floorNumber == 6)
            {
                // Floor 6 - Bramble Thorns: 6 Rock obstacles placed deterministically by Seed
                PlaceSpecial(SpecialTileType.RockObstacle, 6);
                PlaceSpecial(SpecialTileType.GoldBonus, 2);
            }
            else if (floorNumber == 7)
            {
                // Floor 7 - Storm Colosseum: 4 Lightning tiles placed deterministically by Seed
                PlaceSpecial(SpecialTileType.ExtraTurnLightning, 4);
                PlaceSpecial(SpecialTileType.GoldBonus, 2);
            }
            else if (VariantRule == XOVariantRule.GravityDrop)
            {
                // In GravityDrop, no rock obstacles so columns don't get permanently plugged; spawn Gold & Lightning
                PlaceSpecial(SpecialTileType.GoldBonus, 2);
                PlaceSpecial(SpecialTileType.ExtraTurnLightning, 2);
            }
            else if (VariantRule == XOVariantRule.ChaosMinefield || floorNumber == 2)
            {
                PlaceSpecial(SpecialTileType.RockObstacle, 1);
                PlaceSpecial(SpecialTileType.GoldBonus, 2);
                PlaceSpecial(SpecialTileType.ExtraTurnLightning, 1);
                PlaceSpecial(SpecialTileType.HiddenMine, 3);
                PlaceSpecial(SpecialTileType.WarpPortal, 2);
            }
            else
            {
                PlaceSpecial(SpecialTileType.RockObstacle, 2);
                PlaceSpecial(SpecialTileType.GoldBonus, 2);
                PlaceSpecial(SpecialTileType.ExtraTurnLightning, 1);
            }

            if (XORoguelikeRunManager.HasRelic(CaroRelicType.MinePlanter))
            {
                PlaceSpecial(SpecialTileType.HiddenMine, 2);
            }
        }

        public void LoadPuzzleStage(XOPuzzleStage stage)
        {
            Initialize(BoardSize, WinLength, spawnSpecialTiles: false, variant: XOVariantRule.ClassicCaro);
            if (stage == null || stage.presetMoves == null) return;

            foreach (var m in stage.presetMoves)
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
                if (MoveHistory[i].piece == CellPiece.X)
                    count++;
            }
            return count;
        }

        public bool IsValidCell(int r, int c)
        {
            return r >= 0 && r < BoardSize && c >= 0 && c < BoardSize;
        }

        public bool IsCellFrozen(int r, int c)
        {
            return IsValidCell(r, c) && FrozenTurns[r, c] > 0;
        }

        public bool IsRockObstacle(int r, int c)
        {
            return IsValidCell(r, c) && SpecialTiles[r, c] == SpecialTileType.RockObstacle;
        }

        /// <summary>
        /// In Infinite3x3 mode, returns true if this piece is the oldest of 3 active pieces for its owner,
        /// meaning it will vanish on that player's very next move!
        /// </summary>
        public bool IsPieceAboutToVanish(int r, int c)
        {
            if (VariantRule != XOVariantRule.Infinite3x3 || !IsValidCell(r, c))
                return false;

            CellPiece p = Board[r, c];
            if (p == CellPiece.X && _activeQueueX.Count >= 3)
                return _activeQueueX[0].x == r && _activeQueueX[0].y == c;
            if (p == CellPiece.O && _activeQueueO.Count >= 3)
                return _activeQueueO[0].x == r && _activeQueueO[0].y == c;

            return false;
        }

        /// <summary>
        /// For GravityDrop mode: returns the lowest available empty row in column `col` (or -1 if column is full).
        /// </summary>
        public int GetGravityLandingRow(int col)
        {
            if (col < 0 || col >= BoardSize) return -1;
            for (int r = BoardSize - 1; r >= 0; r--)
            {
                if (Board[r, col] == CellPiece.Empty
                    && FrozenTurns[r, col] <= 0
                    && SpecialTiles[r, col] != SpecialTileType.RockObstacle)
                {
                    return r;
                }
            }
            return -1;
        }

        public bool CanPlace(int r, int c)
        {
            return CanPlaceForPiece(r, c, CurrentTurn);
        }

        /// <summary>True if the side to move has at least one legal placement (guards against frozen-only soft-locks).</summary>
        public bool HasAnyLegalMove()
        {
            if (State != MatchState.Playing) return false;
            for (int r = 0; r < BoardSize; r++)
                for (int c = 0; c < BoardSize; c++)
                    if (CanPlace(r, c)) return true;
            return false;
        }

        /// <summary>Skips the current side's turn (used only when no legal move exists, e.g. every empty cell is frozen).</summary>
        public void PassTurn()
        {
            if (State != MatchState.Playing) return;
            TickDownFrozenCells();
            CurrentTurn = (CurrentTurn == CellPiece.X) ? CellPiece.O : CellPiece.X;
        }

        public bool CanPlaceForPiece(int r, int c, CellPiece mover)
        {
            if (State != MatchState.Playing || !IsValidCell(r, c) || FrozenTurns[r, c] > 0 || SpecialTiles[r, c] == SpecialTileType.RockObstacle)
                return false;

            if (Board[r, c] == CellPiece.Empty)
                return true;

            // GobbletStack Rule: A player with GobbleStock > 0 can Gobble ("Eat") an opponent piece of Size 1 or 2!
            if (VariantRule == XOVariantRule.GobbletStack && mover != CellPiece.Empty)
            {
                CellPiece opponent = (mover == CellPiece.X) ? CellPiece.O : CellPiece.X;
                int stock = (mover == CellPiece.X) ? GobbleStockX : GobbleStockO;
                if (Board[r, c] == opponent && PieceSizes[r, c] < 3 && stock > 0)
                {
                    return true;
                }
            }

            return false;
        }

        public bool PlacePiece(
            int r,
            int c,
            out SpecialTileType triggeredSpecial,
            out string tacticalCallout,
            out int calloutBonusCoins)
        {
            triggeredSpecial = SpecialTileType.None;
            tacticalCallout = null;
            calloutBonusCoins = 0;

            // In GravityDrop mode, clicking anywhere in column `c` drops the piece to the lowest empty row!
            if (VariantRule == XOVariantRule.GravityDrop)
            {
                int landingRow = GetGravityLandingRow(c);
                if (landingRow < 0) return false;
                r = landingRow;
            }

            CellPiece placed = CurrentTurn;
            CellPiece opponent = (placed == CellPiece.X) ? CellPiece.O : CellPiece.X;

            if (!CanPlaceForPiece(r, c, placed)) return false;

            // Check if this move is a GobbletStack "Gobble" attack on an opponent piece
            CellPiece gobbledPiece = CellPiece.Empty;
            int gobbledPrevSize = 0;
            int newPieceSize = 1;
            if (VariantRule == XOVariantRule.GobbletStack && Board[r, c] == opponent)
            {
                gobbledPiece = opponent;
                gobbledPrevSize = Mathf.Max(1, PieceSizes[r, c]);
                newPieceSize = Mathf.Min(3, gobbledPrevSize + 1);
                if (placed == CellPiece.X) GobbleStockX--;
                else GobbleStockO--;
            }

            // Pre-evaluate tactical impact before mutating board
            long atkScore = EvaluatePoint(r, c, placed);
            long defScore = EvaluatePoint(r, c, opponent);

            TickDownFrozenCells();

            // Check & consume Special Tile at (r, c)
            if (SpecialTiles[r, c] != SpecialTileType.None && SpecialTiles[r, c] != SpecialTileType.RockObstacle)
            {
                triggeredSpecial = SpecialTiles[r, c];
                SpecialTiles[r, c] = SpecialTileType.None;
            }

            // Infinite 3x3 Vanishing Rule: If player already has 3 active pieces, vanish their oldest piece!
            int vanishedR = -1;
            int vanishedC = -1;
            List<Vector2Int> activeQueue = (placed == CellPiece.X) ? _activeQueueX : _activeQueueO;
            if (VariantRule == XOVariantRule.Infinite3x3 && activeQueue.Count >= 3)
            {
                Vector2Int oldest = activeQueue[0];
                activeQueue.RemoveAt(0);
                if (Board[oldest.x, oldest.y] == placed)
                {
                    Board[oldest.x, oldest.y] = CellPiece.Empty;
                    PieceSizes[oldest.x, oldest.y] = 0;
                    vanishedR = oldest.x;
                    vanishedC = oldest.y;
                }
            }

            Board[r, c] = placed;
            PieceSizes[r, c] = newPieceSize;
            activeQueue.Add(new Vector2Int(r, c));
            MoveHistory.Add(new MoveRecord(r, c, placed, vanishedR, vanishedC, gobbledPiece, gobbledPrevSize));

            // Chaos Minefield Effect 1: HiddenMine blasts up to 2 adjacent pieces around (r, c)!
            if (triggeredSpecial == SpecialTileType.HiddenMine)
            {
                ExecuteMineBlastAround(r, c);
            }

            // Chaos Minefield Effect 2: WarpPortal spawns a Bonus Twin piece on the linked Portal (or adjacent empty cell)!
            Vector2Int twinPos = new Vector2Int(-1, -1);
            if (triggeredSpecial == SpecialTileType.WarpPortal)
            {
                twinPos = ExecuteWarpPortalClone(r, c, placed);
            }

            if (CheckWinFromCell(r, c, placed, out List<Vector2Int> winLine, out bool blockedStrictFive))
            {
                if (RegisterWinLineIfValid(winLine, placed, out bool won))
                {
                    if (won)
                    {
                        State = (placed == CellPiece.X) ? MatchState.XWon : MatchState.OWon;
                        return true;
                    }
                    else
                    {
                        tacticalCallout = $"1ST FIVE LOCKED! (1/{RequiredWinLines})";
                        calloutBonusCoins = 25;
                    }
                }
            }

            if (twinPos.x >= 0 && CheckWinFromCell(twinPos.x, twinPos.y, placed, out List<Vector2Int> twinWinLine, out _))
            {
                if (RegisterWinLineIfValid(twinWinLine, placed, out bool wonTwin))
                {
                    if (wonTwin)
                    {
                        State = (placed == CellPiece.X) ? MatchState.XWon : MatchState.OWon;
                        return true;
                    }
                    else
                    {
                        tacticalCallout = $"1ST FIVE LOCKED! (1/{RequiredWinLines})";
                        calloutBonusCoins = 25;
                    }
                }
            }

            if (gobbledPiece != CellPiece.Empty)
            {
                tacticalCallout = $"GOBBLED ENEMY! (LV.{newPieceSize})";
                calloutBonusCoins = 12;
            }
            else if (blockedStrictFive)
            {
                tacticalCallout = "BLOCKED BOTH ENDS! (NO WIN)";
                calloutBonusCoins = 5;
            }
            else if (defScore >= 14000)
            {
                tacticalCallout = "CRITICAL BLOCK!";
                calloutBonusCoins = 10;
            }
            else if (atkScore >= 35000)
            {
                tacticalCallout = "DOUBLE THREAT FORK!";
                calloutBonusCoins = 15;
            }
            else if (atkScore >= 20000 && WinLength >= 4)
            {
                tacticalCallout = (WinLength == 4) ? "THREE IN A ROW!" : "FOUR IN A ROW!";
                calloutBonusCoins = 8;
            }

            // --- ROGUELIKE RELIC HOOKS ---
            if (placed == CellPiece.X)
            {
                // 1. Alchemist Stone: 25% chance of converting placed tile to Gold Tile
                if (XORoguelikeRunManager.HasRelic(CaroRelicType.AlchemistStone) && (XORandom.Value < 0.25f))
                {
                    SpecialTiles[r, c] = SpecialTileType.GoldBonus;
                    tacticalCallout = "ALCHEMIST STONE! (+10 GOLD TILE)";
                    calloutBonusCoins += 10;
                }

                // 2. Knight's Leap: L-shape placement relative to any previous X move
                if (XORoguelikeRunManager.HasRelic(CaroRelicType.KnightStep))
                {
                    for (int m = 0; m < MoveHistory.Count - 1; m++)
                    {
                        if (MoveHistory[m].piece == CellPiece.X)
                        {
                            int drow = Mathf.Abs(r - MoveHistory[m].row);
                            int dcol = Mathf.Abs(c - MoveHistory[m].col);
                            if ((drow == 1 && dcol == 2) || (drow == 2 && dcol == 1))
                            {
                                tacticalCallout = "KNIGHT'S LEAP! +10 COINS";
                                calloutBonusCoins += 10;
                                break;
                            }
                        }
                    }
                }
            }

            if (CountPlaceableEmptyCells() == 0)
            {
                State = MatchState.Draw;
            }
            else if (triggeredSpecial != SpecialTileType.ExtraTurnLightning)
            {
                // 3. Twin Threat Relic: Forming Open-3 grants 1 free extra move
                if (placed == CellPiece.X && TryCheckTwinThreat(r, c))
                {
                    tacticalCallout = "TWIN THREAT RELIC! +1 EXTRA TURN";
                    calloutBonusCoins += 15;
                    CurrentTurn = CellPiece.X; // Free Extra Turn!
                }
                else
                {
                    CurrentTurn = opponent;
                }
            }

            return true;
        }

        public bool PlacePiece(int r, int c)
        {
            return PlacePiece(r, c, out _, out _, out _);
        }

        private void ExecuteMineBlastAround(int centerR, int centerC)
        {
            List<Vector2Int> neighbors = new List<Vector2Int>();
            for (int dr = -1; dr <= 1; dr++)
            {
                for (int dc = -1; dc <= 1; dc++)
                {
                    if (dr == 0 && dc == 0) continue;
                    int nr = centerR + dr;
                    int nc = centerC + dc;
                    if (IsValidCell(nr, nc) && Board[nr, nc] != CellPiece.Empty)
                    {
                        neighbors.Add(new Vector2Int(nr, nc));
                    }
                }
            }

            for (int i = 0; i < neighbors.Count; i++)
            {
                int swapIdx = XORandom.Range(i, neighbors.Count);
                (neighbors[i], neighbors[swapIdx]) = (neighbors[swapIdx], neighbors[i]);
            }

            int blastCount = Mathf.Min(2, neighbors.Count);
            for (int k = 0; k < blastCount; k++)
            {
                Vector2Int pos = neighbors[k];
                Board[pos.x, pos.y] = CellPiece.Empty;
                _activeQueueX.Remove(pos);
                _activeQueueO.Remove(pos);
            }
        }

        private Vector2Int ExecuteWarpPortalClone(int srcR, int srcC, CellPiece piece)
        {
            // First look for another WarpPortal on the board
            for (int r = 0; r < BoardSize; r++)
            {
                for (int c = 0; c < BoardSize; c++)
                {
                    if ((r != srcR || c != srcC)
                        && SpecialTiles[r, c] == SpecialTileType.WarpPortal
                        && CanPlace(r, c))
                    {
                        SpecialTiles[r, c] = SpecialTileType.None;
                        Board[r, c] = piece;
                        MoveHistory.Add(new MoveRecord(r, c, piece));
                        return new Vector2Int(r, c);
                    }
                }
            }

            // Fallback: clone to a random empty cell near the center
            List<Vector2Int> candidates = GetCandidateCells(1);
            if (candidates.Count > 0)
            {
                Vector2Int pick = candidates[XORandom.Range(0, candidates.Count)];
                SpecialTiles[pick.x, pick.y] = SpecialTileType.None;
                Board[pick.x, pick.y] = piece;
                MoveHistory.Add(new MoveRecord(pick.x, pick.y, piece));
                return pick;
            }

            return new Vector2Int(-1, -1);
        }

        private int CountPlaceableEmptyCells()
        {
            int count = 0;
            for (int r = 0; r < BoardSize; r++)
            {
                for (int c = 0; c < BoardSize; c++)
                {
                    if (Board[r, c] == CellPiece.Empty && SpecialTiles[r, c] != SpecialTileType.RockObstacle)
                        count++;
                }
            }
            return count;
        }

        /// <summary>
        /// BOMB SKILL: Destroys 1 opponent piece on the board.
        /// </summary>
        public bool BombDestroyPiece(int r, int c, CellPiece targetPiece = CellPiece.O)
        {
            if (State != MatchState.Playing || !IsValidCell(r, c))
                return false;

            if (Board[r, c] != targetPiece)
                return false;

            Board[r, c] = CellPiece.Empty;
            FrozenTurns[r, c] = 0;
            _activeQueueX.Remove(new Vector2Int(r, c));
            _activeQueueO.Remove(new Vector2Int(r, c));

            for (int i = MoveHistory.Count - 1; i >= 0; i--)
            {
                if (MoveHistory[i].row == r && MoveHistory[i].col == c)
                {
                    MoveHistory.RemoveAt(i);
                    if (i < _presetMovesCount) _presetMovesCount--;
                    break;
                }
            }

            return true;
        }

        /// <summary>
        /// SWAP PIECE SKILL: Converts 1 Opponent (O) piece directly into Player's (X) piece!
        /// </summary>
        public bool SwapOpponentPiece(int r, int c, CellPiece targetOpponent = CellPiece.O, CellPiece newOwner = CellPiece.X)
        {
            if (State != MatchState.Playing || !IsValidCell(r, c))
                return false;

            if (Board[r, c] != targetOpponent)
                return false;

            Board[r, c] = newOwner;
            FrozenTurns[r, c] = 0;
            Vector2Int pos = new Vector2Int(r, c);
            _activeQueueO.Remove(pos);
            if (!_activeQueueX.Contains(pos))
                _activeQueueX.Add(pos);

            for (int i = MoveHistory.Count - 1; i >= 0; i--)
            {
                if (MoveHistory[i].row == r && MoveHistory[i].col == c)
                {
                    MoveHistory[i] = new MoveRecord(r, c, newOwner);
                    break;
                }
            }

            if (CheckWinFromCell(r, c, newOwner, out List<Vector2Int> winLine, out _))
            {
                if (RegisterWinLineIfValid(winLine, newOwner, out bool won))
                {
                    if (won)
                    {
                        State = (newOwner == CellPiece.X) ? MatchState.XWon : MatchState.OWon;
                    }
                }
            }

            return true;
        }

        /// <summary>
        /// FREEZE SKILL: Locks 1 empty cell for `turns` turns so neither side can place on it.
        /// </summary>
        public bool FreezeEmptyCell(int r, int c, int turns = 3)
        {
            if (State != MatchState.Playing || !IsValidCell(r, c))
                return false;

            if (!CanPlace(r, c))
                return false;

            FrozenTurns[r, c] = turns;
            return true;
        }

        /// <summary>
        /// Two-Way Bot Counter-Skill: High-tier bots can freeze 1 dangerous open square on the player's line once per match!
        /// </summary>
        public bool TryExecuteBotCounterFreeze(XOBotProfile bot, out Vector2Int frozenCell)
        {
            frozenCell = new Vector2Int(-1, -1);
            if (VariantRule == XOVariantRule.Infinite3x3) return false;
            if (bot == null || (bot.counterSkill != BotCounterSkill.CryoFreeze && bot.counterSkill != BotCounterSkill.WarlordStrike))
                return false;

            List<Vector2Int> candidates = GetCandidateCells(2);
            Vector2Int bestThreatCell = new Vector2Int(-1, -1);
            long highestPlayerThreat = 0;

            foreach (var cell in candidates)
            {
                if (WouldWinImmediately(cell.x, cell.y, CellPiece.O))
                    return false;

                long playerThreat = EvaluatePoint(cell.x, cell.y, CellPiece.X);
                if (playerThreat > highestPlayerThreat)
                {
                    highestPlayerThreat = playerThreat;
                    bestThreatCell = cell;
                }
            }

            if (highestPlayerThreat >= 12000 && bestThreatCell.x >= 0)
            {
                FrozenTurns[bestThreatCell.x, bestThreatCell.y] = 3;
                frozenCell = bestThreatCell;
                return true;
            }

            return false;
        }

        /// <summary>
        /// Boss Relic: Flame Blade - Detonates a tactical bomb destroying 1 player threat piece!
        /// </summary>
        public bool TryExecuteBotFlameBlade(out Vector2Int bombedCell)
        {
            bombedCell = new Vector2Int(-1, -1);
            List<Vector2Int> candidates = GetCandidateCells(1);
            long maxThreat = 0;
            Vector2Int target = new Vector2Int(-1, -1);

            foreach (var cell in candidates)
            {
                if (Board[cell.x, cell.y] == CellPiece.X)
                {
                    long threat = EvaluatePoint(cell.x, cell.y, CellPiece.X);
                    if (threat > maxThreat)
                    {
                        maxThreat = threat;
                        target = cell;
                    }
                }
            }

            if (maxThreat >= 6000 && target.x >= 0)
            {
                Board[target.x, target.y] = CellPiece.Empty;
                PieceSizes[target.x, target.y] = 0;
                bombedCell = target;
                return true;
            }
            return false;
        }

        /// <summary>
        /// Boss Relic: Thorn Armor - Spawns a thorny rock obstacle on an empty adjacent cell when threatened!
        /// </summary>
        public bool TryExecuteBotThornArmor(out Vector2Int thornCell)
        {
            thornCell = new Vector2Int(-1, -1);
            List<Vector2Int> candidates = GetCandidateCells(1);
            long maxThreat = 0;
            Vector2Int target = new Vector2Int(-1, -1);

            foreach (var cell in candidates)
            {
                if (CanPlace(cell.x, cell.y))
                {
                    long threat = EvaluatePoint(cell.x, cell.y, CellPiece.X);
                    if (threat > maxThreat)
                    {
                        maxThreat = threat;
                        target = cell;
                    }
                }
            }

            if (maxThreat >= 8000 && target.x >= 0)
            {
                SpecialTiles[target.x, target.y] = SpecialTileType.RockObstacle;
                thornCell = target;
                return true;
            }
            return false;
        }

        /// <summary>
        /// Checks if the Boss's equipped relic protects them from a player skill.
        /// </summary>
        public bool IsBotImmuneToSkill(BotEquipmentType equipment, string skillName)
        {
            if (equipment == BotEquipmentType.MirrorAegis || equipment == BotEquipmentType.OmniCrown)
            {
                if (skillName == "Bomb" || skillName == "Swap") return true;
            }
            return false;
        }

        // =====================================================================
        // ROGUELIKE CARO RELICS & BOSS BLIND MECHANICS
        // =====================================================================

        public void ApplyCenterVoid(int radius = 1)
        {
            int mid = BoardSize / 2;
            for (int r = mid - radius; r <= mid + radius; r++)
            {
                for (int c = mid - radius; c <= mid + radius; c++)
                {
                    if (IsValidCell(r, c))
                    {
                        SpecialTiles[r, c] = SpecialTileType.RockObstacle;
                    }
                }
            }
        }

        public bool TryCheckTwinThreat(int r, int c)
        {
            if (!XORoguelikeRunManager.HasRelic(CaroRelicType.TwinThreat) || XORoguelikeRunManager.TwinThreatTriggeredThisMatch)
                return false;

            for (int d = 0; d < 4; d++)
            {
                int dr = DirRow[d];
                int dc = DirCol[d];
                List<Vector2Int> line = new List<Vector2Int> { new Vector2Int(r, c) };
                int nr = r + dr, nc = c + dc;
                while (IsValidCell(nr, nc) && Board[nr, nc] == CellPiece.X) { line.Add(new Vector2Int(nr, nc)); nr += dr; nc += dc; }
                Vector2Int endAfter = new Vector2Int(nr, nc);

                nr = r - dr; nc = c - dc;
                while (IsValidCell(nr, nc) && Board[nr, nc] == CellPiece.X) { line.Insert(0, new Vector2Int(nr, nc)); nr -= dr; nc -= dc; }
                Vector2Int endBefore = new Vector2Int(nr, nc);

                if (line.Count == 3)
                {
                    bool openAfter = IsValidCell(endAfter.x, endAfter.y) && Board[endAfter.x, endAfter.y] == CellPiece.Empty;
                    bool openBefore = IsValidCell(endBefore.x, endBefore.y) && Board[endBefore.x, endBefore.y] == CellPiece.Empty;
                    if (openAfter && openBefore)
                    {
                        XORoguelikeRunManager.TwinThreatTriggeredThisMatch = true;
                        return true;
                    }
                }
            }
            return false;
        }

        public bool TryExecuteGuardianAegis(out Vector2Int frozenCell)
        {
            frozenCell = new Vector2Int(-1, -1);
            if (!XORoguelikeRunManager.HasRelic(CaroRelicType.GuardianAegis) || XORoguelikeRunManager.GuardianAegisTriggeredThisMatch)
                return false;

            for (int r = 0; r < BoardSize; r++)
            {
                for (int c = 0; c < BoardSize; c++)
                {
                    if (Board[r, c] == CellPiece.Empty)
                    {
                        Board[r, c] = CellPiece.O;
                        bool aiWouldWin = CheckWinFromCell(r, c, CellPiece.O, out _);
                        Board[r, c] = CellPiece.Empty;

                        if (aiWouldWin)
                        {
                            FreezeEmptyCell(r, c, 2);
                            XORoguelikeRunManager.GuardianAegisTriggeredThisMatch = true;
                            frozenCell = new Vector2Int(r, c);
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        public bool TryExecuteShatterHammer(int r, int c, out Vector2Int shatteredCell)
        {
            shatteredCell = new Vector2Int(-1, -1);
            if (!XORoguelikeRunManager.HasRelic(CaroRelicType.ShatterHammer) || XORoguelikeRunManager.ShatterHammerTriggeredThisMatch)
                return false;

            for (int dr = -1; dr <= 1; dr++)
            {
                for (int dc = -1; dc <= 1; dc++)
                {
                    if (dr == 0 && dc == 0) continue;
                    int nr = r + dr, nc = c + dc;
                    if (IsValidCell(nr, nc) && Board[nr, nc] == CellPiece.O)
                    {
                        Board[nr, nc] = CellPiece.Empty;
                        XORoguelikeRunManager.ShatterHammerTriggeredThisMatch = true;
                        shatteredCell = new Vector2Int(nr, nc);
                        return true;
                    }
                }
            }
            return false;
        }

        private void TickDownFrozenCells()
        {
            for (int r = 0; r < BoardSize; r++)
            {
                for (int c = 0; c < BoardSize; c++)
                {
                    if (FrozenTurns[r, c] > 0)
                        FrozenTurns[r, c]--;
                }
            }
        }

        public MoveRecord? GetLastMove()
        {
            if (MoveHistory.Count == 0) return null;
            return MoveHistory[MoveHistory.Count - 1];
        }

        public bool IsWinningLineDiagonal()
        {
            if (WinningCells == null || WinningCells.Count < 2) return false;
            Vector2Int a = WinningCells[0];
            Vector2Int b = WinningCells[1];
            return Mathf.Abs(a.x - b.x) == 1 && Mathf.Abs(a.y - b.y) == 1;
        }

        public bool HasUndoablePlayerMove()
        {
            return MoveHistory.Count > _presetMovesCount;
        }

        public bool UndoMove(GameMode mode, CellPiece humanPiece = CellPiece.X)
        {
            if (MoveHistory.Count <= _presetMovesCount) return false;

            WinningCells.Clear();
            State = MatchState.Playing;

            if (mode == GameMode.VsAI || mode == GameMode.PuzzleStage)
            {
                while (MoveHistory.Count > _presetMovesCount && MoveHistory[MoveHistory.Count - 1].piece != humanPiece)
                {
                    PopSingleMove();
                }
                if (MoveHistory.Count > _presetMovesCount && MoveHistory[MoveHistory.Count - 1].piece == humanPiece)
                {
                    PopSingleMove();
                }
                CurrentTurn = humanPiece;
            }
            else
            {
                MoveRecord popped = PopSingleMove();
                CurrentTurn = popped.piece;
            }

            return true;
        }

        public bool ReviveFromAILoss(out Vector2Int criticalBlockCell)
        {
            criticalBlockCell = new Vector2Int(-1, -1);
            if (MoveHistory.Count <= _presetMovesCount) return false;

            MoveRecord lastMove = MoveHistory[MoveHistory.Count - 1];
            criticalBlockCell = new Vector2Int(lastMove.row, lastMove.col);

            PopSingleMove();
            WinningCells.Clear();
            State = MatchState.Playing;
            CurrentTurn = CellPiece.X;
            return true;
        }

        /// <summary>
        /// Phoenix Sigil: Incinerates the Bot's lethal winning piece, freezes the tile for N turns,
        /// and restores the turn to Player X to save the roguelike sudden-death run.
        /// </summary>
        public bool IncinerateLastWinningPiece(out Vector2Int tilePos, int freezeTurns = 2)
        {
            tilePos = new Vector2Int(-1, -1);
            if (MoveHistory.Count <= _presetMovesCount) return false;

            MoveRecord lastMove = MoveHistory[MoveHistory.Count - 1];
            tilePos = new Vector2Int(lastMove.row, lastMove.col);

            PopSingleMove();
            if (freezeTurns > 0 && IsValidCell(lastMove.row, lastMove.col))
            {
                FrozenTurns[lastMove.row, lastMove.col] = freezeTurns;
            }
            WinningCells.Clear();
            State = MatchState.Playing;
            CurrentTurn = CellPiece.X;
            return true;
        }

        private MoveRecord PopSingleMove()
        {
            int lastIdx = MoveHistory.Count - 1;
            MoveRecord rec = MoveHistory[lastIdx];
            MoveHistory.RemoveAt(lastIdx);

            List<Vector2Int> queue = (rec.piece == CellPiece.X) ? _activeQueueX : _activeQueueO;
            queue.Remove(new Vector2Int(rec.row, rec.col));

            // Restore gobbled piece if this move was a GobbletStack eat move
            if (rec.gobbledPiece != CellPiece.Empty)
            {
                Board[rec.row, rec.col] = rec.gobbledPiece;
                PieceSizes[rec.row, rec.col] = Mathf.Max(1, rec.gobbledPrevSize);
                if (rec.piece == CellPiece.X) GobbleStockX++;
                else GobbleStockO++;
            }
            else
            {
                Board[rec.row, rec.col] = CellPiece.Empty;
                PieceSizes[rec.row, rec.col] = 0;
            }

            // Restore vanished piece if this move was in Infinite3x3 mode
            if (rec.vanishedRow >= 0 && rec.vanishedCol >= 0 && IsValidCell(rec.vanishedRow, rec.vanishedCol))
            {
                Board[rec.vanishedRow, rec.vanishedCol] = rec.piece;
                PieceSizes[rec.vanishedRow, rec.vanishedCol] = 1;
                queue.Insert(0, new Vector2Int(rec.vanishedRow, rec.vanishedCol));
            }

            return rec;
        }

        public bool CheckWinFromCell(int r, int c, CellPiece piece, out List<Vector2Int> winLine)
        {
            return CheckWinFromCell(r, c, piece, out winLine, out _);
        }

        public bool CheckWinFromCell(int r, int c, CellPiece piece, out List<Vector2Int> winLine, out bool blockedStrictFive)
        {
            winLine = null;
            blockedStrictFive = false;
            if (piece == CellPiece.Empty) return false;

            CellPiece opponent = (piece == CellPiece.X) ? CellPiece.O : CellPiece.X;

            for (int d = 0; d < 4; d++)
            {
                int dr = DirRow[d];
                int dc = DirCol[d];

                List<Vector2Int> line = new List<Vector2Int> { new Vector2Int(r, c) };

                int nr = r + dr;
                int nc = c + dc;
                while (IsValidCell(nr, nc) && Board[nr, nc] == piece)
                {
                    line.Add(new Vector2Int(nr, nc));
                    nr += dr;
                    nc += dc;
                }
                Vector2Int endAfter = new Vector2Int(nr, nc);

                nr = r - dr;
                nc = c - dc;
                while (IsValidCell(nr, nc) && Board[nr, nc] == piece)
                {
                    line.Insert(0, new Vector2Int(nr, nc));
                    nr -= dr;
                    nc -= dc;
                }
                Vector2Int endBefore = new Vector2Int(nr, nc);

                // Balatro-style Relic: Diagonal Prism (Open-ended 4-in-a-row diagonal counts as an instant WIN!)
                if (piece == CellPiece.X && line.Count == 4 && (d == 2 || d == 3) && XORoguelikeRunManager.HasRelic(CaroRelicType.DiagonalPrism))
                {
                    bool openAfter = IsValidCell(endAfter.x, endAfter.y) && Board[endAfter.x, endAfter.y] == CellPiece.Empty;
                    bool openBefore = IsValidCell(endBefore.x, endBefore.y) && Board[endBefore.x, endBefore.y] == CellPiece.Empty;
                    if (openAfter && openBefore)
                    {
                        winLine = line;
                        return true;
                    }
                }

                if (line.Count >= WinLength)
                {
                    // Strict Rule Caro: If exactly 5-in-a-row is blocked by opponent on BOTH ends, it does NOT win!
                    if (VariantRule == XOVariantRule.StrictCaro && line.Count == WinLength)
                    {
                        bool blockedAfter = IsValidCell(endAfter.x, endAfter.y) && Board[endAfter.x, endAfter.y] == opponent;
                        bool blockedBefore = IsValidCell(endBefore.x, endBefore.y) && Board[endBefore.x, endBefore.y] == opponent;
                        if (blockedAfter && blockedBefore)
                        {
                            blockedStrictFive = true;
                            continue;
                        }
                    }

                    winLine = line;
                    return true;
                }
            }

            return false;
        }

        public bool IsDistinctWinLine(List<Vector2Int> newLine, List<List<Vector2Int>> existingLines)
        {
            if (newLine == null || newLine.Count == 0) return false;
            HashSet<Vector2Int> newSet = new HashSet<Vector2Int>(newLine);
            foreach (var existing in existingLines)
            {
                int overlap = 0;
                foreach (var pt in existing)
                {
                    if (newSet.Contains(pt)) overlap++;
                }
                if (overlap >= 4) return false;
            }
            return true;
        }

        public bool RegisterWinLineIfValid(List<Vector2Int> winLine, CellPiece piece, out bool reachedTargetWins)
        {
            reachedTargetWins = false;
            if (winLine == null || winLine.Count == 0) return false;

            var list = (piece == CellPiece.X) ? CompletedWinLinesX : CompletedWinLinesO;
            if (IsDistinctWinLine(winLine, list))
            {
                list.Add(new List<Vector2Int>(winLine));
                foreach (var pt in winLine)
                {
                    if (!WinningCells.Contains(pt)) WinningCells.Add(pt);
                }

                if (list.Count >= RequiredWinLines)
                {
                    reachedTargetWins = true;
                }
                return true;
            }
            return false;
        }

        /// <summary>
        /// Collapsing Arena: Every 8-10 turns, the outer boundary crumbles into rubble!
        /// Any piece on the crumbling perimeter is destroyed, forcing action toward the center.
        /// </summary>
        public bool CollapseNextPerimeterRing(out List<Vector2Int> newlyCollapsed)
        {
            newlyCollapsed = new List<Vector2Int>();
            int ring = CollapsedRingsCount;
            int maxRings = (BoardSize / 2) - 2;
            if (ring >= maxRings) return false;

            int minR = ring;
            int maxR = BoardSize - 1 - ring;
            int minC = ring;
            int maxC = BoardSize - 1 - ring;

            for (int r = minR; r <= maxR; r++)
            {
                for (int c = minC; c <= maxC; c++)
                {
                    if (r == minR || r == maxR || c == minC || c == maxC)
                    {
                        if (SpecialTiles[r, c] != SpecialTileType.RockObstacle)
                        {
                            Board[r, c] = CellPiece.Empty;
                            SpecialTiles[r, c] = SpecialTileType.RockObstacle;
                            PieceSizes[r, c] = 0;
                            FrozenTurns[r, c] = 0;
                            newlyCollapsed.Add(new Vector2Int(r, c));
                        }
                    }
                }
            }

            CollapsedRingsCount++;
            return newlyCollapsed.Count > 0;
        }

        /// <summary>
        /// Cosmic Shift: Thunder strikes 2 random empty cells, mutating them into Extra-Turn Lightning tiles!
        /// </summary>
        public bool ExecuteCosmicShift(out List<Vector2Int> affectedCells)
        {
            affectedCells = new List<Vector2Int>();
            List<Vector2Int> empties = new List<Vector2Int>();
            for (int r = 1; r < BoardSize - 1; r++)
            {
                for (int c = 1; c < BoardSize - 1; c++)
                {
                    if (Board[r, c] == CellPiece.Empty && SpecialTiles[r, c] == SpecialTileType.None)
                    {
                        empties.Add(new Vector2Int(r, c));
                    }
                }
            }

            if (empties.Count < 2) return false;

            int idx1 = XORandom.Range(0, empties.Count);
            int idx2 = (idx1 + XORandom.Range(1, empties.Count)) % empties.Count;

            SpecialTiles[empties[idx1].x, empties[idx1].y] = SpecialTileType.ExtraTurnLightning;
            SpecialTiles[empties[idx2].x, empties[idx2].y] = SpecialTileType.ExtraTurnLightning;

            affectedCells.Add(empties[idx1]);
            affectedCells.Add(empties[idx2]);
            return true;
        }

        public Vector2Int CalculateAIMove(CellPiece aiPiece, AIDifficulty difficulty, XOBotProfile botProfile = null)
        {
            CellPiece opponent = (aiPiece == CellPiece.X) ? CellPiece.O : CellPiece.X;

            if (MoveHistory.Count == 0 && VariantRule != XOVariantRule.GravityDrop)
            {
                int mid = BoardSize / 2;
                if (CanPlace(mid, mid)) return new Vector2Int(mid, mid);
            }

            List<Vector2Int> candidates = GetCandidateCells(2);
            if (candidates.Count == 0)
            {
                for (int r = 0; r < BoardSize; r++)
                    for (int c = 0; c < BoardSize; c++)
                        if (CanPlace(r, c)) return new Vector2Int(r, c);
                return new Vector2Int(BoardSize / 2, BoardSize / 2);
            }

            // 1. Always take immediate win
            foreach (var cell in candidates)
            {
                if (WouldWinImmediately(cell.x, cell.y, aiPiece))
                    return cell;
            }

            // 2. Block opponent's immediate win (unless Rookie Bot makes a beginner mistake)
            float mistakeProb = (botProfile != null) ? botProfile.mistakeChance : (difficulty == AIDifficulty.Easy ? 0.25f : 0f);
            if (XORandom.Value >= mistakeProb * 0.7f)
            {
                foreach (var cell in candidates)
                {
                    if (WouldWinImmediately(cell.x, cell.y, opponent))
                        return cell;
                }
            }

            Vector2Int bestCell = candidates[0];
            long bestScore = long.MinValue;

            float defenseWeight = difficulty switch
            {
                AIDifficulty.Easy => 0.75f,
                AIDifficulty.Normal => 1.08f,
                AIDifficulty.Hard => 1.24f,
                _ => 1.0f
            };

            float randomJitter = difficulty switch
            {
                AIDifficulty.Easy => 420f,
                AIDifficulty.Normal => 55f,
                AIDifficulty.Hard => 4f,
                _ => 10f
            };

            if (botProfile != null)
            {
                if (botProfile.eloRating >= 1800)
                {
                    defenseWeight = 1.32f;
                    randomJitter = 1f;
                }
                else if (botProfile.eloRating <= 650)
                {
                    defenseWeight = 0.65f;
                    randomJitter = 650f;
                }
            }

            // Crown of Omni relic: divine aura lowers enemy AI precision
            if (XORoguelikeRunManager.HasRelic(CaroRelicType.CrownOfOmni))
            {
                defenseWeight = Mathf.Max(0.55f, defenseWeight - 0.20f);
                randomJitter += 85f;
            }

            int center = BoardSize / 2;

            foreach (var cell in candidates)
            {
                long attackScore = EvaluatePoint(cell.x, cell.y, aiPiece);
                long defenseScore = EvaluatePoint(cell.x, cell.y, opponent);

                int distFromCenter = Mathf.Abs(cell.x - center) + Mathf.Abs(cell.y - center);
                int centerBonus = Mathf.Max(0, (BoardSize - distFromCenter) * 3);

                long specialTileBonus = 0;
                if (SpecialTiles[cell.x, cell.y] == SpecialTileType.ExtraTurnLightning || SpecialTiles[cell.x, cell.y] == SpecialTileType.WarpPortal)
                {
                    specialTileBonus = (difficulty == AIDifficulty.Easy) ? 900 : 3400;
                }
                else if (SpecialTiles[cell.x, cell.y] == SpecialTileType.GoldBonus)
                {
                    specialTileBonus = 750;
                }

                long totalScore = attackScore + (long)(defenseScore * defenseWeight) + centerBonus + specialTileBonus;
                totalScore += (long)XORandom.Range(0f, randomJitter);

                if (totalScore > bestScore)
                {
                    bestScore = totalScore;
                    bestCell = cell;
                }
            }

            return bestCell;
        }

        public Vector2Int CalculateBestHint(CellPiece playerPiece, out string tacticalReason)
        {
            CellPiece opponent = (playerPiece == CellPiece.X) ? CellPiece.O : CellPiece.X;

            if (MoveHistory.Count == 0 && VariantRule != XOVariantRule.GravityDrop)
            {
                tacticalReason = "Control the center of the board!";
                return new Vector2Int(BoardSize / 2, BoardSize / 2);
            }

            List<Vector2Int> candidates = GetCandidateCells(2);
            if (candidates.Count == 0)
            {
                tacticalReason = "Place on an open square!";
                return new Vector2Int(BoardSize / 2, BoardSize / 2);
            }

            foreach (var cell in candidates)
            {
                if (WouldWinImmediately(cell.x, cell.y, playerPiece))
                {
                    tacticalReason = $"WINNING MOVE! Place here to connect {WinLength} now!";
                    return cell;
                }
            }

            foreach (var cell in candidates)
            {
                if (WouldWinImmediately(cell.x, cell.y, opponent))
                {
                    tacticalReason = $"DANGER! Block here now before Opponent connects {WinLength}!";
                    return cell;
                }
            }

            Vector2Int bestCell = candidates[0];
            long bestScore = long.MinValue;
            long bestAttack = 0;
            long bestDefense = 0;
            int center = BoardSize / 2;

            foreach (var cell in candidates)
            {
                long atk = EvaluatePoint(cell.x, cell.y, playerPiece);
                long def = EvaluatePoint(cell.x, cell.y, opponent);
                int centerBonus = Mathf.Max(0, (BoardSize - (Mathf.Abs(cell.x - center) + Mathf.Abs(cell.y - center))) * 2);
                long specialBonus = (SpecialTiles[cell.x, cell.y] == SpecialTileType.ExtraTurnLightning) ? 2500 : 0;

                long score = atk + (long)(def * 1.15f) + centerBonus + specialBonus;
                if (score > bestScore)
                {
                    bestScore = score;
                    bestCell = cell;
                    bestAttack = atk;
                    bestDefense = def;
                }
            }

            if (SpecialTiles[bestCell.x, bestCell.y] == SpecialTileType.ExtraTurnLightning)
                tacticalReason = "LIGHTNING TILE! Place here to get an EXTRA TURN!";
            else if (bestDefense >= 14000 && bestDefense > bestAttack)
                tacticalReason = "CRITICAL BLOCK! Stop Opponent's attacking line!";
            else if (bestAttack >= 14000)
                tacticalReason = "DOUBLE ATTACK! Build an unstoppable fork!";
            else
                tacticalReason = "Best strategic square on the board!";

            return bestCell;
        }

        private bool WouldWinImmediately(int r, int c, CellPiece piece)
        {
            if (!CanPlaceForPiece(r, c, piece)) return false;

            CellPiece prevOccupant = Board[r, c];

            // In Infinite3x3 mode, temporarily simulate vanishing the oldest piece if player has 3 pieces
            Vector2Int tempVanished = new Vector2Int(-1, -1);
            List<Vector2Int> queue = (piece == CellPiece.X) ? _activeQueueX : _activeQueueO;
            if (VariantRule == XOVariantRule.Infinite3x3 && queue.Count >= 3)
            {
                tempVanished = queue[0];
                if (Board[tempVanished.x, tempVanished.y] == piece)
                    Board[tempVanished.x, tempVanished.y] = CellPiece.Empty;
            }

            Board[r, c] = piece;
            bool createsLine = CheckWinFromCell(r, c, piece, out List<Vector2Int> winLine, out _);
            Board[r, c] = prevOccupant;

            if (tempVanished.x >= 0)
            {
                Board[tempVanished.x, tempVanished.y] = piece;
            }

            if (!createsLine) return false;

            var list = (piece == CellPiece.X) ? CompletedWinLinesX : CompletedWinLinesO;
            if (list.Count >= RequiredWinLines - 1 && IsDistinctWinLine(winLine, list))
            {
                return true;
            }

            return false;
        }

        public List<Vector2Int> GetCandidateCells(int radius)
        {
            List<Vector2Int> result = new List<Vector2Int>();

            // GravityDrop mode: only valid column landing rows are legal moves!
            if (VariantRule == XOVariantRule.GravityDrop)
            {
                for (int c = 0; c < BoardSize; c++)
                {
                    int landR = GetGravityLandingRow(c);
                    if (landR >= 0)
                        result.Add(new Vector2Int(landR, c));
                }
                return result;
            }

            // Infinite3x3 or GobbletStack (compact <= 4x4): evaluate all legal cells on the compact board
            if (VariantRule == XOVariantRule.Infinite3x3 || (VariantRule == XOVariantRule.GobbletStack && BoardSize < 10))
            {
                for (int r = 0; r < BoardSize; r++)
                    for (int c = 0; c < BoardSize; c++)
                        if (CanPlaceForPiece(r, c, CurrentTurn))
                            result.Add(new Vector2Int(r, c));
                return result;
            }

            bool[,] visited = new bool[BoardSize, BoardSize];

            for (int r = 0; r < BoardSize; r++)
            {
                for (int c = 0; c < BoardSize; c++)
                {
                    if (Board[r, c] != CellPiece.Empty
                        || SpecialTiles[r, c] == SpecialTileType.ExtraTurnLightning
                        || SpecialTiles[r, c] == SpecialTileType.WarpPortal)
                    {
                        for (int dr = -radius; dr <= radius; dr++)
                        {
                            for (int dc = -radius; dc <= radius; dc++)
                            {
                                int nr = r + dr;
                                int nc = c + dc;
                                if (CanPlaceForPiece(nr, nc, CurrentTurn) && !visited[nr, nc])
                                {
                                    visited[nr, nc] = true;
                                    result.Add(new Vector2Int(nr, nc));
                                }
                            }
                        }
                    }
                }
            }

            return result;
        }

        public long EvaluatePoint(int r, int c, CellPiece piece)
        {
            long total = 0;
            int openThreesOrBetter = 0;
            CellPiece opponent = (piece == CellPiece.X) ? CellPiece.O : CellPiece.X;

            for (int d = 0; d < 4; d++)
            {
                int dr = DirRow[d];
                int dc = DirCol[d];

                int count = 1;
                int openEnds = 0;
                int enemyBlockedEnds = 0;

                int nr = r + dr;
                int nc = c + dc;
                while (IsValidCell(nr, nc) && Board[nr, nc] == piece)
                {
                    count++;
                    nr += dr;
                    nc += dc;
                }
                if (CanPlace(nr, nc)) openEnds++;
                else if (IsValidCell(nr, nc) && Board[nr, nc] == opponent) enemyBlockedEnds++;

                nr = r - dr;
                nc = c - dc;
                while (IsValidCell(nr, nc) && Board[nr, nc] == piece)
                {
                    count++;
                    nr -= dr;
                    nc -= dc;
                }
                if (CanPlace(nr, nc)) openEnds++;
                else if (IsValidCell(nr, nc) && Board[nr, nc] == opponent) enemyBlockedEnds++;

                if (VariantRule == XOVariantRule.StrictCaro && count == WinLength && enemyBlockedEnds == 2)
                {
                    continue;
                }

                if (openEnds == 0 && count < WinLength)
                    continue;

                long dirScore = ScorePattern(count, openEnds);
                if (dirScore >= 4000)
                {
                    openThreesOrBetter++;
                }
                total += dirScore;
            }

            if (openThreesOrBetter >= 2)
            {
                total += 35000;
            }

            return total;
        }

        private long ScorePattern(int count, int openEnds)
        {
            if (count >= WinLength) return 1000000;

            int needed = WinLength - count;
            if (needed == 1)
            {
                if (openEnds == 2) return 150000;
                if (openEnds == 1) return 22000;
            }
            else if (needed == 2)
            {
                if (openEnds == 2) return 14000;
                if (openEnds == 1) return 1200;
            }
            else if (needed == 3)
            {
                if (openEnds == 2) return 900;
                if (openEnds == 1) return 150;
            }
            else if (needed >= 4)
            {
                if (openEnds == 2) return 40;
                if (openEnds == 1) return 10;
            }
            return 0;
        }
    }
}
