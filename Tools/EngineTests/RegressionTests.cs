using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using XOPlayable.Core;

namespace XOPlayable.Tests
{
    /// <summary>
    /// Regression tests for the rule-engine bugs found in the November 2026 code review.
    /// Each test fails on the pre-fix engine and passes after the fix.
    /// </summary>
    public class XORegressionTests
    {
        private static XOCaroEngine New(XOVariantRule v = XOVariantRule.ClassicCaro, int size = 10, int win = 5)
        {
            var e = new XOCaroEngine(size, win);
            e.Initialize(size, win, spawnSpecialTiles: false, variant: v);
            return e;
        }

        private static void P(XOCaroEngine e, int r, int c)
        {
            Assert.IsTrue(e.PlacePiece(r, c), $"PlacePiece({r},{c}) failed for {e.CurrentTurn} in {e.VariantRule}");
        }

        // 1. Win lines must be rolled back together with the move that created them.
        [Test] public void Incinerate_ThenBotCompletesSameLine_StillWins()
        {
            var e = New();
            P(e, 0, 0); P(e, 9, 0); P(e, 0, 2); P(e, 9, 1); P(e, 0, 4); P(e, 9, 2); P(e, 0, 6); P(e, 9, 3); P(e, 2, 0);
            P(e, 9, 4);
            Assert.AreEqual(MatchState.OWon, e.State);

            Assert.IsTrue(e.IncinerateLastWinningPiece(out Vector2Int tile, 2));
            Assert.AreEqual(MatchState.Playing, e.State);
            Assert.AreEqual(0, e.CompletedWinLinesO.Count, "stale locked line left behind");

            P(e, 2, 2);               // X
            P(e, 8, 8);               // O elsewhere while (9,4) is frozen
            P(e, 2, 4);               // X
            P(e, tile.x, tile.y);     // O completes the very same five again
            Assert.AreEqual(MatchState.OWon, e.State, "bot's identical line must count again");
        }

        [Test] public void Undo_RemovesLockedWinLine_ForMultiLineBosses()
        {
            var e = New();
            e.RequiredWinLines = 2;
            for (int i = 0; i < 4; i++) { P(e, 5, i); P(e, 9, i); }
            P(e, 5, 4);
            Assert.AreEqual(MatchState.Playing, e.State);
            Assert.AreEqual(1, e.CompletedWinLinesX.Count);

            Assert.IsTrue(e.UndoMove(GameMode.TwoPlayerLocal));
            Assert.AreEqual(0, e.CompletedWinLinesX.Count, "undo must unlock the line");
            Assert.AreEqual(0, e.WinningCells.Count);

            P(e, 5, 4);
            Assert.AreEqual(1, e.CompletedWinLinesX.Count, "re-placing must lock it exactly once");
        }

        // 2. A portal move and its twin are one logical move for undo.
        [Test] public void Undo_AfterWarpPortal_RemovesPieceAndTwinTogether()
        {
            var e = New(XOVariantRule.ChaosMinefield);
            e.SpecialTiles[2, 2] = SpecialTileType.WarpPortal;
            e.SpecialTiles[7, 7] = SpecialTileType.WarpPortal;
            P(e, 2, 2);
            Assert.AreEqual(CellPiece.X, e.Board[7, 7], "twin should have spawned");

            Assert.IsTrue(e.UndoMove(GameMode.TwoPlayerLocal));
            Assert.AreEqual(CellPiece.Empty, e.Board[2, 2], "original piece must be undone too");
            Assert.AreEqual(CellPiece.Empty, e.Board[7, 7]);
            Assert.AreEqual(SpecialTileType.WarpPortal, e.SpecialTiles[2, 2]);
            Assert.AreEqual(SpecialTileType.WarpPortal, e.SpecialTiles[7, 7]);
            Assert.AreEqual(CellPiece.X, e.CurrentTurn);
            Assert.AreEqual(0, e.MoveHistory.Count);
        }

        // 3. Undo must restore everything the move changed.
        [Test] public void Undo_AfterMine_RestoresBlastedPiecesAndMine()
        {
            var e = New(XOVariantRule.ChaosMinefield);
            e.SpecialTiles[5, 5] = SpecialTileType.HiddenMine;
            P(e, 0, 0); P(e, 5, 4); P(e, 0, 2); P(e, 5, 6);
            P(e, 5, 5);
            Assert.AreEqual(CellPiece.Empty, e.Board[5, 4]);
            Assert.AreEqual(CellPiece.Empty, e.Board[5, 6]);

            Assert.IsTrue(e.UndoMove(GameMode.TwoPlayerLocal));
            Assert.AreEqual(CellPiece.O, e.Board[5, 4]);
            Assert.AreEqual(CellPiece.O, e.Board[5, 6]);
            Assert.AreEqual(CellPiece.Empty, e.Board[5, 5]);
            Assert.AreEqual(SpecialTileType.HiddenMine, e.SpecialTiles[5, 5]);
        }

        [Test] public void Undo_RestoresConsumedSpecialTile()
        {
            var e = New();
            e.SpecialTiles[3, 3] = SpecialTileType.GoldBonus;
            P(e, 3, 3);
            Assert.AreEqual(SpecialTileType.None, e.SpecialTiles[3, 3]);
            Assert.IsTrue(e.UndoMove(GameMode.TwoPlayerLocal));
            Assert.AreEqual(SpecialTileType.GoldBonus, e.SpecialTiles[3, 3]);
        }

        [Test] public void Undo_RestoresFrozenCountdown()
        {
            var e = New();
            Assert.IsTrue(e.FreezeEmptyCell(4, 5, 3));
            P(e, 0, 0);
            Assert.AreEqual(2, e.FrozenTurns[4, 5]);
            Assert.IsTrue(e.UndoMove(GameMode.TwoPlayerLocal));
            Assert.AreEqual(3, e.FrozenTurns[4, 5]);
        }

        // 4. A full Gobblet board is not a draw while someone can still gobble.
        [Test] public void Gobblet_FullBoard_NotDrawWhileGobbleAvailable()
        {
            var e = New(XOVariantRule.GobbletStack, 4, 4);
            string[] layout = { "XXOO", "OOXX", "XXOO", "OOXX" };   // no four-in-a-row for either side
            var xs = new List<Vector2Int>(); var os = new List<Vector2Int>();
            for (int r = 0; r < 4; r++)
                for (int c = 0; c < 4; c++)
                    (layout[r][c] == 'X' ? xs : os).Add(new Vector2Int(r, c));
            for (int i = 0; i < 8; i++) { P(e, xs[i].x, xs[i].y); P(e, os[i].x, os[i].y); }

            Assert.AreEqual(MatchState.Playing, e.State, "X can still gobble an O piece");
            Assert.IsTrue(e.HasAnyLegalMove());
            Assert.IsTrue(e.PlacePiece(0, 2), "X gobbles O at (0,2)");
        }

        // 5. GravityDrop: a frozen cell blocks its column instead of letting pieces float above it.
        [Test] public void Gravity_FrozenCellBlocksColumn_NoFloatingPieces()
        {
            var e = New(XOVariantRule.GravityDrop, 8, 4);
            int bottom = e.BoardSize - 1;
            Assert.IsTrue(e.FreezeEmptyCell(bottom, 3, 3));
            Assert.AreEqual(-1, e.GetGravityLandingRow(3));
            Assert.IsFalse(e.PlacePiece(0, 3));
            P(e, 0, 2);
            Assert.AreEqual(CellPiece.X, e.Board[bottom, 2]);
        }

        [Test] public void Gravity_AllColumnsBlockedByFrozen_HasNoLegalMove()
        {
            var e = New(XOVariantRule.GravityDrop, 8, 4);
            for (int c = 0; c < e.BoardSize; c++) Assert.IsTrue(e.FreezeEmptyCell(e.BoardSize - 1, c, 3));
            Assert.IsFalse(e.HasAnyLegalMove());
        }

        // 6. Randomness and relics are controllable, so runs and tests are reproducible.
        private static string MineBlastBoard(int seed)
        {
            XORandom.SetSeed(seed);
            var e = New(XOVariantRule.ChaosMinefield);
            e.SpecialTiles[5, 5] = SpecialTileType.HiddenMine;
            P(e, 0, 0); P(e, 5, 4); P(e, 0, 2); P(e, 5, 6); P(e, 0, 4); P(e, 4, 5); P(e, 0, 6); P(e, 6, 5);
            P(e, 5, 5);
            var sb = new System.Text.StringBuilder();
            foreach (var cell in new[] { (5, 4), (5, 6), (4, 5), (6, 5) }) sb.Append(e.Board[cell.Item1, cell.Item2] == CellPiece.Empty ? '.' : 'O');
            return sb.ToString();
        }

        [Test] public void SeededRandom_MineBlast_IsReproducible()
        {
            Assert.AreEqual(MineBlastBoard(1234), MineBlastBoard(1234));
            var outcomes = new HashSet<string>();
            for (int seed = 1; seed <= 30; seed++) outcomes.Add(MineBlastBoard(seed));
            Assert.IsTrue(outcomes.Count > 1, "different seeds should be able to blast different pieces");
        }

        [Test] public void RelicQuery_CanBeInjected_DiagonalPrismWinsOnOpenFour()
        {
            string Play(bool prism)
            {
                var e = New();
                e.IsTowerClimb = true; // relics only apply inside the Tower
                e.RelicQuery = t => prism && t == CaroRelicType.DiagonalPrism;
                P(e, 2, 2); P(e, 9, 0); P(e, 3, 3); P(e, 9, 2); P(e, 4, 4); P(e, 9, 4); P(e, 5, 5);
                return e.State.ToString();
            }
            Assert.AreEqual("XWon", Play(true));
            Assert.AreEqual("Playing", Play(false));
        }
    }
}
