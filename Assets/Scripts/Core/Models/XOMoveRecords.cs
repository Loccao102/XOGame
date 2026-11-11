using System;
using System.Collections.Generic;

namespace XOPlayable.Core
{
    /// <summary>One piece destroyed as a side effect of a move (e.g. a Hidden Mine blast); kept so Undo can restore it.</summary>
    [Serializable]
    public struct BlastedPiece
    {
        public int row;
        public int col;
        public CellPiece piece;
        public int size;

        public XOCellPos Cell => new XOCellPos(row, col);

        public BlastedPiece(int row, int col, CellPiece piece, int size)
        {
            this.row = row;
            this.col = col;
            this.piece = piece;
            this.size = size;
        }
    }

    /// <summary>Everything a move changed besides the placed piece itself, so Undo is an exact inverse.</summary>
    [Serializable]
    public sealed class MoveUndoInfo
    {
        /// <summary>Special tile that sat on the move's cell before the move consumed (or Alchemist Stone overwrote) it.</summary>
        public SpecialTileType consumedTile;
        /// <summary>Cells whose freeze countdown was decremented by this move.</summary>
        public List<XOCellPos> tickedFrozen = new List<XOCellPos>();
        /// <summary>Pieces removed by a Hidden Mine blast triggered by this move.</summary>
        public List<BlastedPiece> blasted = new List<BlastedPiece>();
        /// <summary>Number of locked win lines per side before the move; anything beyond is rolled back.</summary>
        public int winLinesXBefore;
        public int winLinesOBefore;
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
        /// <summary>True for the Warp Portal twin; it is undone together with the move that spawned it.</summary>
        public bool isBonusTwin;
        public MoveUndoInfo undo;

        public XOCellPos Cell => new XOCellPos(row, col);

        public MoveRecord(
            int row,
            int col,
            CellPiece piece,
            int vanishedRow = -1,
            int vanishedCol = -1,
            CellPiece gobbledPiece = CellPiece.Empty,
            int gobbledPrevSize = 0,
            bool isBonusTwin = false,
            MoveUndoInfo undo = null)
        {
            this.row = row;
            this.col = col;
            this.piece = piece;
            this.vanishedRow = vanishedRow;
            this.vanishedCol = vanishedCol;
            this.gobbledPiece = gobbledPiece;
            this.gobbledPrevSize = gobbledPrevSize;
            this.isBonusTwin = isBonusTwin;
            this.undo = undo;
        }
    }

    /// <summary>Complete value object reporting every board side-effect produced by a single placement.</summary>
    public readonly struct PlaceResult
    {
        public readonly bool Placed;
        public readonly XOCellPos LandingCell;
        public readonly CellPiece PlacedPiece;
        public readonly int NewPieceSize;
        public readonly CellPiece GobbledPrevPiece;
        public readonly int GobbledPrevSize;
        public readonly SpecialTileType TriggeredSpecial;
        public readonly string TacticalCallout;
        public readonly int CalloutBonusCoins;
        public readonly XOCellPos VanishedCell;
        public readonly XOCellPos TwinCell;
        public readonly IReadOnlyList<BlastedPiece> BlastedPieces;
        public readonly IReadOnlyList<XOCellPos> WinningLine;
        public readonly bool CompletedNewWinLine;
        public readonly bool StrictBlockedFive;
        public readonly bool GrantedExtraTurn;
        public readonly MatchState ResultState;

        /// <summary>Row where the piece landed (differs from clicked row in GravityDrop).</summary>
        public int Row => LandingCell.x;
        /// <summary>Column where the piece landed.</summary>
        public int Col => LandingCell.y;
        public bool HasCallout => !string.IsNullOrEmpty(TacticalCallout);
        public bool HasTwin => TwinCell.IsValid;
        public bool HasVanished => VanishedCell.IsValid;
        public bool HasBlastedPieces => BlastedPieces != null && BlastedPieces.Count > 0;
        public bool HasWinningLine => WinningLine != null && WinningLine.Count > 0;

        public PlaceResult(
            bool placed,
            XOCellPos landingCell,
            CellPiece placedPiece,
            int newPieceSize,
            CellPiece gobbledPrevPiece,
            int gobbledPrevSize,
            SpecialTileType triggeredSpecial,
            string tacticalCallout,
            int calloutBonusCoins,
            XOCellPos vanishedCell,
            XOCellPos twinCell,
            IReadOnlyList<BlastedPiece> blastedPieces,
            IReadOnlyList<XOCellPos> winningLine,
            bool completedNewWinLine,
            bool strictBlockedFive,
            bool grantedExtraTurn,
            MatchState resultState)
        {
            Placed = placed;
            LandingCell = landingCell;
            PlacedPiece = placedPiece;
            NewPieceSize = newPieceSize;
            GobbledPrevPiece = gobbledPrevPiece;
            GobbledPrevSize = gobbledPrevSize;
            TriggeredSpecial = triggeredSpecial;
            TacticalCallout = tacticalCallout;
            CalloutBonusCoins = calloutBonusCoins;
            VanishedCell = vanishedCell;
            TwinCell = twinCell;
            BlastedPieces = blastedPieces ?? Array.Empty<BlastedPiece>();
            WinningLine = winningLine ?? Array.Empty<XOCellPos>();
            CompletedNewWinLine = completedNewWinLine;
            StrictBlockedFive = strictBlockedFive;
            GrantedExtraTurn = grantedExtraTurn;
            ResultState = resultState;
        }

        public static PlaceResult Rejected => new PlaceResult(
            placed: false,
            landingCell: XOCellPos.Invalid,
            placedPiece: CellPiece.Empty,
            newPieceSize: 0,
            gobbledPrevPiece: CellPiece.Empty,
            gobbledPrevSize: 0,
            triggeredSpecial: SpecialTileType.None,
            tacticalCallout: null,
            calloutBonusCoins: 0,
            vanishedCell: XOCellPos.Invalid,
            twinCell: XOCellPos.Invalid,
            blastedPieces: Array.Empty<BlastedPiece>(),
            winningLine: Array.Empty<XOCellPos>(),
            completedNewWinLine: false,
            strictBlockedFive: false,
            grantedExtraTurn: false,
            resultState: MatchState.Playing);
    }
}

