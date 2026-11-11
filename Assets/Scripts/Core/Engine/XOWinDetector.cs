using System.Collections.Generic;

namespace XOPlayable.Core
{
    /// <summary>Stateless win-line detection and multi-line registration rules.</summary>
    public static class XOWinDetector
    {
        public static readonly int[] DirRow = { 0, 1, 1, 1 };
        public static readonly int[] DirCol = { 1, 0, 1, -1 };

        public static bool CheckWinFromCell(
            CellPiece[,] board,
            int boardSize,
            int winLength,
            XOVariantRule variantRule,
            bool isTowerClimb,
            bool hasDiagonalPrism,
            int r,
            int c,
            CellPiece piece,
            out List<XOCellPos> winLine,
            out bool blockedStrictFive)
        {
            winLine = null;
            blockedStrictFive = false;
            if (piece == CellPiece.Empty) return false;

            CellPiece opponent = (piece == CellPiece.X) ? CellPiece.O : CellPiece.X;

            for (int d = 0; d < 4; d++)
            {
                int dr = DirRow[d];
                int dc = DirCol[d];

                var line = new List<XOCellPos> { new XOCellPos(r, c) };

                int nr = r + dr;
                int nc = c + dc;
                while (IsInBounds(boardSize, nr, nc) && board[nr, nc] == piece)
                {
                    line.Add(new XOCellPos(nr, nc));
                    nr += dr;
                    nc += dc;
                }
                var endAfter = new XOCellPos(nr, nc);

                nr = r - dr;
                nc = c - dc;
                while (IsInBounds(boardSize, nr, nc) && board[nr, nc] == piece)
                {
                    line.Insert(0, new XOCellPos(nr, nc));
                    nr -= dr;
                    nc -= dc;
                }
                var endBefore = new XOCellPos(nr, nc);

                // Roguelike Relic: Diagonal Prism (open-ended diagonal 4 wins in Tower Climb only)
                if (isTowerClimb && piece == CellPiece.X && line.Count == 4 && (d == 2 || d == 3) && hasDiagonalPrism)
                {
                    if (IsEmptyCell(board, boardSize, endAfter) && IsEmptyCell(board, boardSize, endBefore))
                    {
                        winLine = line;
                        return true;
                    }
                }

                if (line.Count >= winLength)
                {
                    if (variantRule == XOVariantRule.StrictCaro && line.Count == winLength)
                    {
                        bool blockedAfter = IsInBounds(boardSize, endAfter.x, endAfter.y) && board[endAfter.x, endAfter.y] == opponent;
                        bool blockedBefore = IsInBounds(boardSize, endBefore.x, endBefore.y) && board[endBefore.x, endBefore.y] == opponent;
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

        public static bool IsDistinctWinLine(List<XOCellPos> newLine, List<List<XOCellPos>> existingLines)
        {
            if (newLine == null || newLine.Count == 0) return false;
            var newSet = new HashSet<XOCellPos>(newLine);
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

        public static bool RegisterWinLineIfValid(
            List<XOCellPos> winLine,
            CellPiece piece,
            int requiredWinLines,
            List<List<XOCellPos>> completedX,
            List<List<XOCellPos>> completedO,
            List<XOCellPos> winningCells,
            out bool reachedTargetWins)
        {
            reachedTargetWins = false;
            if (winLine == null || winLine.Count == 0) return false;

            List<List<XOCellPos>> list = (piece == CellPiece.X) ? completedX : completedO;
            if (!IsDistinctWinLine(winLine, list)) return false;

            list.Add(new List<XOCellPos>(winLine));
            foreach (var pt in winLine)
            {
                if (!winningCells.Contains(pt)) winningCells.Add(pt);
            }

            if (list.Count >= requiredWinLines)
                reachedTargetWins = true;

            return true;
        }

        public static bool IsOpenThreeAtCell(CellPiece[,] board, int boardSize, int r, int c, CellPiece piece)
        {
            for (int d = 0; d < 4; d++)
            {
                int dr = DirRow[d];
                int dc = DirCol[d];
                int count = 1;

                int nr = r + dr, nc = c + dc;
                while (IsInBounds(boardSize, nr, nc) && board[nr, nc] == piece) { count++; nr += dr; nc += dc; }
                var endAfter = new XOCellPos(nr, nc);

                nr = r - dr; nc = c - dc;
                while (IsInBounds(boardSize, nr, nc) && board[nr, nc] == piece) { count++; nr -= dr; nc -= dc; }
                var endBefore = new XOCellPos(nr, nc);

                if (count == 3 && IsEmptyCell(board, boardSize, endAfter) && IsEmptyCell(board, boardSize, endBefore))
                    return true;
            }
            return false;
        }

        public static bool IsWinningLineDiagonal(IReadOnlyList<XOCellPos> winningCells)
        {
            if (winningCells == null || winningCells.Count < 2) return false;
            XOCellPos a = winningCells[0];
            XOCellPos b = winningCells[1];
            return XOMath.Abs(a.x - b.x) == 1 && XOMath.Abs(a.y - b.y) == 1;
        }

        private static bool IsInBounds(int boardSize, int r, int c)
            => r >= 0 && r < boardSize && c >= 0 && c < boardSize;

        private static bool IsEmptyCell(CellPiece[,] board, int boardSize, XOCellPos p)
            => IsInBounds(boardSize, p.x, p.y) && board[p.x, p.y] == CellPiece.Empty;
    }
}

