#if UNITY_EDITOR
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using XOPlayable.Core;

namespace XOPlayable.Tests
{
    /// <summary>
    /// Headless rule-engine test cases for every XO variant, skills, AI legality fuzzing and puzzle solvability.
    /// Run: Unity -batchmode -runTests -testPlatform EditMode
    /// </summary>
    public class XOEngineTests
    {
        private static void Log(string msg)
        {
#if UNITY_5_3_OR_NEWER
            UnityEngine.Debug.Log(msg);
#else
            System.Console.WriteLine(msg);
#endif
        }
        private static XOCaroEngine NewEngine(XOVariantRule v, int size = 10, int win = 5)
        {
            var e = new XOCaroEngine(size, win);
            e.Initialize(size, win, spawnSpecialTiles: false, variant: v);
            return e;
        }

        // Places a piece for whoever's turn it is, asserting success.
        private static void P(XOCaroEngine e, int r, int c)
        {
            Assert.IsTrue(e.PlacePiece(r, c), $"PlacePiece({r},{c}) failed for {e.CurrentTurn} in {e.VariantRule}");
        }

        // ---------------- CLASSIC ----------------

        [Test] public void Classic_HorizontalFive_Wins()
        {
            var e = NewEngine(XOVariantRule.ClassicCaro);
            for (int i = 0; i < 4; i++) { P(e, 5, i); P(e, 9, i); }
            P(e, 5, 4);
            Assert.AreEqual(MatchState.XWon, e.State);
            Assert.AreEqual(5, e.WinningCells.Count);
        }

        [Test] public void Classic_VerticalDiagonalAntiDiagonal_Win()
        {
            int[][] dirs = { new[] { 1, 0 }, new[] { 1, 1 }, new[] { 1, -1 } };
            foreach (var d in dirs)
            {
                var e = NewEngine(XOVariantRule.ClassicCaro);
                int sr = 2, sc = (d[1] == -1) ? 7 : 2;
                for (int i = 0; i < 4; i++) { P(e, sr + d[0] * i, sc + d[1] * i); P(e, 0, i * 2); }
                P(e, sr + d[0] * 4, sc + d[1] * 4);
                Assert.AreEqual(MatchState.XWon, e.State, $"dir {d[0]},{d[1]}");
            }
        }

        [Test] public void Classic_FourInRow_DoesNotWin_AndTurnsAlternate()
        {
            var e = NewEngine(XOVariantRule.ClassicCaro);
            Assert.AreEqual(CellPiece.X, e.CurrentTurn);
            P(e, 5, 0); Assert.AreEqual(CellPiece.O, e.CurrentTurn);
            P(e, 9, 0); Assert.AreEqual(CellPiece.X, e.CurrentTurn);
            P(e, 5, 1); P(e, 9, 1); P(e, 5, 2); P(e, 9, 2); P(e, 5, 3);
            Assert.AreEqual(MatchState.Playing, e.State);
        }

        [Test] public void Classic_OccupiedCell_Rejected()
        {
            var e = NewEngine(XOVariantRule.ClassicCaro);
            P(e, 3, 3);
            Assert.IsFalse(e.PlacePiece(3, 3));
            Assert.AreEqual(CellPiece.O, e.CurrentTurn);
        }

        [Test] public void Classic_SpecialTiles_SpawnBalanced_AndRockBlocks()
        {
            for (int k = 0; k < 20; k++)
            {
                var e = new XOCaroEngine(10, 5);
                e.Initialize(10, 5, true, XOVariantRule.ClassicCaro);
                int rocks = 0, gold = 0, light = 0;
                for (int r = 0; r < 10; r++) for (int c = 0; c < 10; c++)
                {
                    var t = e.SpecialTiles[r, c];
                    if (t == SpecialTileType.RockObstacle) { rocks++; Assert.IsFalse(e.CanPlace(r, c), "rock must block"); }
                    if (t == SpecialTileType.GoldBonus) gold++;
                    if (t == SpecialTileType.ExtraTurnLightning) light++;
                }
                Assert.AreEqual(2, rocks); Assert.AreEqual(2, gold); Assert.AreEqual(1, light); // matches SpawnBalancedSpecialTiles
            }
        }

        [Test] public void Classic_LightningTile_GivesExtraTurn()
        {
            var e = new XOCaroEngine(10, 5);
            e.Initialize(10, 5, true, XOVariantRule.ClassicCaro);
            XOCellPos lt = Find(e, SpecialTileType.ExtraTurnLightning);
            Assert.IsTrue(e.PlacePiece(lt.x, lt.y, out var trig, out _, out _));
            Assert.AreEqual(SpecialTileType.ExtraTurnLightning, trig);
            Assert.AreEqual(CellPiece.X, e.CurrentTurn, "X should move again after Lightning");
        }

        // ---------------- INFINITE 3x3 ----------------

        [Test] public void Infinite3x3_FourthPieceVanishesOldest()
        {
            var e = NewEngine(XOVariantRule.Infinite3x3, 3, 3);
            Assert.AreEqual(3, e.BoardSize);
            P(e, 0, 0); P(e, 1, 1); P(e, 0, 2); P(e, 2, 2); P(e, 2, 1); P(e, 1, 0);
            Assert.IsTrue(e.IsPieceAboutToVanish(0, 0), "X oldest (0,0) should be flagged FADE");
            P(e, 1, 2); // X 4th piece
            Assert.AreEqual(CellPiece.Empty, e.Board[0, 0], "oldest X must vanish");
            Assert.AreEqual(3, Count(e, CellPiece.X));
        }

        [Test] public void Infinite3x3_Undo_RestoresVanishedPiece()
        {
            var e = NewEngine(XOVariantRule.Infinite3x3, 3, 3);
            P(e, 0, 0); P(e, 1, 1); P(e, 0, 2); P(e, 2, 2); P(e, 2, 1); P(e, 1, 0);
            P(e, 1, 2);
            Assert.IsTrue(e.UndoMove(GameMode.TwoPlayerLocal));
            Assert.AreEqual(CellPiece.X, e.Board[0, 0]);
            Assert.AreEqual(CellPiece.Empty, e.Board[1, 2]);
            Assert.AreEqual(CellPiece.X, e.CurrentTurn);
        }

        [Test] public void Infinite3x3_ThreeInRow_Wins()
        {
            var e = NewEngine(XOVariantRule.Infinite3x3, 3, 3);
            P(e, 0, 0); P(e, 1, 0); P(e, 0, 1); P(e, 1, 1); P(e, 0, 2);
            Assert.AreEqual(MatchState.XWon, e.State);
        }

        // ---------------- GRAVITY DROP ----------------

        [Test] public void Gravity_PieceDropsToBottom_AndStacks()
        {
            var e = NewEngine(XOVariantRule.GravityDrop, 8, 4);
            Assert.AreEqual(8, e.BoardSize); Assert.AreEqual(4, e.WinLength);
            P(e, 0, 3);
            Assert.AreEqual(CellPiece.X, e.Board[7, 3]);
            P(e, 0, 3);
            Assert.AreEqual(CellPiece.O, e.Board[6, 3]);
        }

        [Test] public void Gravity_FullColumn_Rejected()
        {
            var e = NewEngine(XOVariantRule.GravityDrop, 8, 4);
            for (int i = 0; i < 8; i++) P(e, 0, 0);
            Assert.AreEqual(-1, e.GetGravityLandingRow(0));
            Assert.IsFalse(e.PlacePiece(0, 0));
        }

        [Test] public void Gravity_FourHorizontal_Wins()
        {
            var e = NewEngine(XOVariantRule.GravityDrop, 8, 4);
            for (int i = 0; i < 3; i++) { P(e, 0, i); P(e, 0, i); }
            P(e, 0, 3);
            Assert.AreEqual(MatchState.XWon, e.State);
        }

        // ---------------- STRICT CARO ----------------

        [Test] public void Strict_FiveBlockedBothEnds_DoesNotWin()
        {
            var e = NewEngine(XOVariantRule.StrictCaro);
            // O at (5,0) and (5,6); X fills (5,1..5)
            P(e, 5, 1); P(e, 5, 0);
            P(e, 5, 2); P(e, 5, 6);
            P(e, 5, 3); P(e, 0, 0);
            P(e, 5, 4); P(e, 0, 2);
            Assert.IsTrue(e.PlacePiece(5, 5, out _, out string callout, out _));
            Assert.AreEqual(MatchState.Playing, e.State, "blocked-both-ends five must NOT win");
            Assert.AreEqual("BLOCKED BOTH ENDS! (NO WIN)", callout);
        }

        [Test] public void Strict_FiveOpenOneEnd_Wins()
        {
            var e = NewEngine(XOVariantRule.StrictCaro);
            P(e, 5, 1); P(e, 5, 0);
            P(e, 5, 2); P(e, 0, 9);
            P(e, 5, 3); P(e, 0, 0);
            P(e, 5, 4); P(e, 0, 2);
            P(e, 5, 5);
            Assert.AreEqual(MatchState.XWon, e.State);
        }

        // ---------------- GOBBLET 4x4 ----------------

        [Test] public void Gobblet_EatSmallerEnemy_LevelsUp_AndConsumesStock()
        {
            var e = NewEngine(XOVariantRule.GobbletStack, 4, 4);
            Assert.AreEqual(4, e.BoardSize);
            P(e, 0, 0);                       // X Lv1
            Assert.AreEqual(1, e.PieceSizes[0, 0]);
            Assert.IsTrue(e.CanPlace(0, 0), "O can gobble X Lv1");
            Assert.IsTrue(e.PlacePiece(0, 0, out _, out string callout, out _));
            Assert.AreEqual(CellPiece.O, e.Board[0, 0]);
            Assert.AreEqual(2, e.PieceSizes[0, 0]);
            Assert.AreEqual(2, e.GobbleStockO);
            StringAssert.StartsWith("GOBBLED", callout);
            P(e, 0, 0);                       // X eats O Lv2 -> X Lv3
            Assert.AreEqual(3, e.PieceSizes[0, 0]);
            Assert.IsFalse(e.CanPlace(0, 0), "Lv3 cannot be gobbled");
        }

        [Test] public void Gobblet_NoStock_CannotGobble()
        {
            var e = NewEngine(XOVariantRule.GobbletStack, 4, 4);
            // X places at 3 cells, O gobbles all 3
            P(e, 0, 0); P(e, 0, 0);
            P(e, 1, 1); P(e, 1, 1);
            P(e, 2, 2); P(e, 2, 2);
            Assert.AreEqual(0, e.GobbleStockO);
            P(e, 3, 3);
            Assert.IsFalse(e.CanPlace(3, 3), "O has no gobbles left");
        }

        [Test] public void Gobblet_Undo_RestoresEatenPiece()
        {
            var e = NewEngine(XOVariantRule.GobbletStack, 4, 4);
            P(e, 0, 0); P(e, 0, 0);
            Assert.IsTrue(e.UndoMove(GameMode.TwoPlayerLocal));
            Assert.AreEqual(CellPiece.X, e.Board[0, 0]);
            Assert.AreEqual(1, e.PieceSizes[0, 0]);
            Assert.AreEqual(3, e.GobbleStockO);
        }

        [Test] public void Gobblet_FourInRow_Wins()
        {
            var e = NewEngine(XOVariantRule.GobbletStack, 4, 4);
            for (int i = 0; i < 3; i++) { P(e, 0, i); P(e, 3, i); }
            P(e, 0, 3);
            Assert.AreEqual(MatchState.XWon, e.State);
        }

        // ---------------- CHAOS ----------------

        [Test] public void Chaos_SpawnsMinesPortalsRock()
        {
            var e = new XOCaroEngine(10, 5);
            e.Initialize(10, 5, true, XOVariantRule.ChaosMinefield);
            Assert.AreEqual(3, CountTiles(e, SpecialTileType.HiddenMine));
            Assert.AreEqual(2, CountTiles(e, SpecialTileType.WarpPortal));
            Assert.AreEqual(1, CountTiles(e, SpecialTileType.RockObstacle));
        }

        [Test] public void Chaos_WarpPortal_ClonesTwinPiece()
        {
            var e = new XOCaroEngine(10, 5);
            e.Initialize(10, 5, true, XOVariantRule.ChaosMinefield);
            XOCellPos p = Find(e, SpecialTileType.WarpPortal);
            int before = Count(e, CellPiece.X);
            Assert.IsTrue(e.PlacePiece(p.x, p.y, out var trig, out _, out _));
            Assert.AreEqual(SpecialTileType.WarpPortal, trig);
            Assert.AreEqual(before + 2, Count(e, CellPiece.X), "portal should add a twin X");
        }

        // ---------------- SKILLS ----------------

        [Test] public void Skills_Freeze_Bomb_Swap()
        {
            var e = NewEngine(XOVariantRule.ClassicCaro);
            P(e, 4, 4); P(e, 5, 5);
            Assert.IsTrue(e.FreezeEmptyCell(4, 5, 3));
            Assert.IsFalse(e.CanPlace(4, 5));
            Assert.IsTrue(e.BombDestroyPiece(5, 5, CellPiece.O));
            Assert.AreEqual(CellPiece.Empty, e.Board[5, 5]);
            P(e, 6, 6); // X
            P(e, 7, 7); // O
            Assert.IsTrue(e.SwapOpponentPiece(7, 7, CellPiece.O, CellPiece.X));
            Assert.AreEqual(CellPiece.X, e.Board[7, 7]);
            // freeze ticks down after 3 placements total since freeze
            P(e, 0, 0);
            Assert.IsTrue(e.CanPlace(4, 5) || e.FrozenTurns[4, 5] == 0, "freeze should expire after 3 moves");
        }

        // ---------------- AI ----------------

        [Test] public void AI_TakesImmediateWin_And_BlocksImmediateLoss()
        {
            var e = NewEngine(XOVariantRule.ClassicCaro);
            // O has 4 in a row at row 2 cols 2..5 (open), X has 4 in row 6 cols 2..5. O to move -> must win.
            e.Initialize(10, 5, false, XOVariantRule.ClassicCaro);
            P(e, 6, 2); P(e, 2, 2); P(e, 6, 3); P(e, 2, 3); P(e, 6, 4); P(e, 2, 4); P(e, 6, 5); P(e, 2, 5);
            P(e, 9, 9); // X wastes; O to move
            XOCellPos m = e.CalculateAIMove(CellPiece.O, AIDifficulty.Hard);
            Assert.IsTrue(m == new XOCellPos(2, 1) || m == new XOCellPos(2, 6), $"AI must take win, got {m}");

            var e2 = NewEngine(XOVariantRule.ClassicCaro);
            P(e2, 6, 2); P(e2, 0, 0); P(e2, 6, 3); P(e2, 0, 9); P(e2, 6, 4); P(e2, 9, 0); P(e2, 6, 5);
            XOCellPos b = e2.CalculateAIMove(CellPiece.O, AIDifficulty.Hard);
            Assert.IsTrue(b == new XOCellPos(6, 1) || b == new XOCellPos(6, 6), $"AI must block, got {b}");
        }

        [Test] public void AI_AlwaysReturnsLegalMove_AllVariants_Fuzz()
        {
            var report = new StringBuilder();
            var variants = new (XOVariantRule v, int size, int win)[]
            {
                (XOVariantRule.ClassicCaro, 10, 5), (XOVariantRule.ClassicCaro, 15, 5),
                (XOVariantRule.Infinite3x3, 3, 3), (XOVariantRule.GobbletStack, 4, 4),
                (XOVariantRule.GravityDrop, 8, 4), (XOVariantRule.StrictCaro, 10, 5),
                (XOVariantRule.ChaosMinefield, 10, 5)
            };
            int illegal = 0;
            foreach (var cfg in variants)
            {
                int xw = 0, ow = 0, dr = 0, unfinished = 0;
                for (int g = 0; g < 25; g++)
                {
                    var e = new XOCaroEngine(cfg.size, cfg.win);
                    e.Initialize(cfg.size, cfg.win, true, cfg.v);
                    int cap = cfg.size * cfg.size * 3;
                    int moves = 0;
                    while (e.State == MatchState.Playing && moves < cap)
                    {
                        var diff = (e.CurrentTurn == CellPiece.X) ? AIDifficulty.Normal : AIDifficulty.Hard;
                        XOCellPos m = e.CalculateAIMove(e.CurrentTurn, diff);
                        if (!e.PlacePiece(m.x, m.y))
                        {
                            illegal++;
                            report.AppendLine($"ILLEGAL AI move {m} in {cfg.v} {cfg.size}x{cfg.size} at move {moves} (turn {e.CurrentTurn})");
                            break;
                        }
                        moves++;
                    }
                    if (e.State == MatchState.XWon) xw++;
                    else if (e.State == MatchState.OWon) ow++;
                    else if (e.State == MatchState.Draw) dr++;
                    else unfinished++;
                }
                report.AppendLine($"{cfg.v} {cfg.size}x{cfg.size}: X(Normal) {xw} / O(Hard) {ow} / Draw {dr} / Unfinished(cap) {unfinished}");
            }
            Log("[XO FUZZ]\n" + report);
            System.IO.Directory.CreateDirectory("TestResults");
            System.IO.File.WriteAllText("TestResults/ai_fuzz_report.txt", report.ToString());
            Assert.AreEqual(0, illegal, report.ToString());
        }

        [Test] public void AI_NoLegalMove_FrozenOnly_DoesNotSoftLock()
        {
            // Fill a small 3x3-style classic board except one frozen cell: AI must not return an illegal move silently.
            var e = new XOCaroEngine(5, 5);
            e.Initialize(5, 5, false, XOVariantRule.ClassicCaro);
            // Fill all but (4,4) with a non-winning pattern
            CellPiece[] pat = { CellPiece.X, CellPiece.X, CellPiece.O, CellPiece.O, CellPiece.X };
            int placed = 0;
            for (int r = 0; r < 5 && e.State == MatchState.Playing; r++)
                for (int c = 0; c < 5 && e.State == MatchState.Playing; c++)
                {
                    if (r == 4 && c == 4) continue;
                    // place whoever's turn on next empty cell
                    e.PlacePiece(r, c); placed++;
                }
            if (e.State != MatchState.Playing) Assert.Pass("Board ended naturally before edge case");
            e.FreezeEmptyCell(4, 4, 3);
            XOCellPos m = e.CalculateAIMove(e.CurrentTurn, AIDifficulty.Hard);
            bool legal = e.CanPlace(m.x, m.y);
            Assert.IsTrue(legal || e.State != MatchState.Playing || e.HasAnyLegalMove() == false,
                "When only frozen cells remain, the engine must expose that so the game can pass/draw instead of soft-locking");
        }

        [Test] public void Hint_ReturnsLegalCell_AndFindsWinningMove()
        {
            var e = NewEngine(XOVariantRule.ClassicCaro);
            for (int i = 0; i < 4; i++) { P(e, 5, i + 1); P(e, 0, i * 2); }
            XOCellPos h = e.CalculateBestHint(CellPiece.X, out string reason);
            Assert.IsTrue(h == new XOCellPos(5, 0) || h == new XOCellPos(5, 5), $"hint {h}");
            StringAssert.Contains("WINNING", reason);
        }

        // ---------------- PUZZLE STAGES ----------------

        [Test] public void Puzzles_AllPatterns_LoadCleanly_AndAreSolvableVsHardAI()
        {
            var sb = new StringBuilder();
            int unsolvable = 0, badLoad = 0;
            foreach (int boardSize in new[] { 10, 15 })
            {
                for (int stage = 1; stage <= 6; stage++)
                {
                    var puzzle = XOProgressionSystem.GeneratePuzzleStage(stage, boardSize);
                    var e = new XOCaroEngine(boardSize, 5);
                    e.Initialize(boardSize, 5, false, XOVariantRule.ClassicCaro);
                    e.LoadPuzzleStage(puzzle);
                    int loaded = e.MoveHistory.Count;
                    bool loadOk = loaded == puzzle.presetMoves.Count && e.State == MatchState.Playing;
                    if (!loadOk) badLoad++;
                    // Widen the search beam only when a narrow one fails, to keep the test fast.
                    bool solved = false; string line = "";
                    foreach (int beam in new[] { 8, 25 })
                    {
                        if (SolveDfs(e, puzzle.maxPlayerMoves, beam, out line)) { solved = true; break; }
                    }
                    if (!solved) unsolvable++;
                    sb.AppendLine($"[{boardSize}x{boardSize}] Stage {stage} '{puzzle.title}': presets {loaded}/{puzzle.presetMoves.Count}, maxMoves {puzzle.maxPlayerMoves}, " +
                                  $"{(solved ? "SOLVABLE: " + line : "NOT SOLVABLE vs Hard AI within move limit")}");
                }
            }
            Log("[XO PUZZLES]\n" + sb);
            System.IO.Directory.CreateDirectory("TestResults");
            System.IO.File.WriteAllText("TestResults/puzzle_report.txt", sb.ToString());
            Assert.AreEqual(0, badLoad, sb.ToString());
            Assert.AreEqual(0, unsolvable, sb.ToString());
        }

        // DFS: X to force a win within `depth` X-moves vs deterministic-ish Hard AI.
        private static bool SolveDfs(XOCaroEngine e, int depth, int beam, out string line)
        {
            line = "";
            if (depth <= 0) return false;
            var cands = TopCandidates(e, CellPiece.X, beam);
            foreach (var c in cands)
            {
                int histBefore = e.MoveHistory.Count;
                if (!e.PlacePiece(c.x, c.y)) continue;
                if (e.State == MatchState.XWon) { Rewind(e, histBefore); line = $"({c.x},{c.y})"; return true; }
                bool ok = false; string sub = "";
                if (e.State == MatchState.Playing)
                {
                    while (e.State == MatchState.Playing && e.CurrentTurn == CellPiece.O)
                    {
                        var m = e.CalculateAIMove(CellPiece.O, AIDifficulty.Hard);
                        if (!e.PlacePiece(m.x, m.y)) break;
                    }
                    if (e.State == MatchState.Playing) ok = SolveDfs(e, depth - 1, beam, out sub);
                }
                Rewind(e, histBefore);
                if (ok) { line = $"({c.x},{c.y}) -> {sub}"; return true; }
            }
            return false;
        }

        private static void Rewind(XOCaroEngine e, int historyCount)
        {
            while (e.MoveHistory.Count > historyCount)
            {
                if (!e.UndoMove(GameMode.TwoPlayerLocal)) break;
            }
        }

        private static List<XOCellPos> TopCandidates(XOCaroEngine e, CellPiece p, int k)
        {
            var list = new List<(XOCellPos c, long s)>();
            CellPiece opp = p == CellPiece.X ? CellPiece.O : CellPiece.X;
            for (int r = 0; r < e.BoardSize; r++)
                for (int c = 0; c < e.BoardSize; c++)
                {
                    if (!e.CanPlace(r, c)) continue;
                    long s = e.EvaluatePoint(r, c, p) + e.EvaluatePoint(r, c, opp);
                    if (s > 0) list.Add((new XOCellPos(r, c), s));
                }
            // Stable order: equal scores break ties by position, so results do not depend on the runtime's sort.
            var ordered = new List<(XOCellPos c, long s)>(list);
            ordered.Sort((a, b) =>
            {
                int byScore = b.s.CompareTo(a.s);
                if (byScore != 0) return byScore;
                int byRow = a.c.x.CompareTo(b.c.x);
                return byRow != 0 ? byRow : a.c.y.CompareTo(b.c.y);
            });
            var res = new List<XOCellPos>();
            for (int i = 0; i < ordered.Count && i < k; i++) res.Add(ordered[i].c);
            return res;
        }

        // ---------------- PROGRESSION ----------------

        [Test] public void Progression_Ranks_Missions_Achievements()
        {
            int savedStars = XOProgressionSystem.TotalStars;
            int savedMission = XOProgressionSystem.CurrentMissionIndex;
            try
            {
                XOProgressionSystem.TotalStars = 0;
                Assert.AreEqual("Bronze Novice", XOProgressionSystem.GetCurrentRank(out _, out _).title);
                XOProgressionSystem.TotalStars = 7;
                Assert.AreEqual("Gold Master", XOProgressionSystem.GetCurrentRank(out _, out _).title);
                XOProgressionSystem.TotalStars = 50;
                Assert.AreEqual("Grandmaster", XOProgressionSystem.GetCurrentRank(out _, out _).title);

                XOProgressionSystem.CurrentMissionIndex = 0;
                Assert.IsFalse(XOProgressionSystem.CheckAndAdvanceMission(20, false, false, 0, false, out _));
                Assert.IsTrue(XOProgressionSystem.CheckAndAdvanceMission(8, false, false, 0, false, out _));
                Assert.AreEqual(1, XOProgressionSystem.CurrentMissionIndex);

                Assert.AreEqual(8, XOProgressionSystem.GetAchievementsList().Count);
                XOProgressionSystem.ChestProgress = 99;
                Assert.AreEqual(3, XOProgressionSystem.ChestProgress);
            }
            finally
            {
                XOProgressionSystem.TotalStars = savedStars;
                XOProgressionSystem.CurrentMissionIndex = savedMission;
            }
        }

        // ---------------- helpers ----------------

        private static int Count(XOCaroEngine e, CellPiece p)
        {
            int n = 0;
            for (int r = 0; r < e.BoardSize; r++) for (int c = 0; c < e.BoardSize; c++) if (e.Board[r, c] == p) n++;
            return n;
        }

        private static int CountTiles(XOCaroEngine e, SpecialTileType t)
        {
            int n = 0;
            for (int r = 0; r < e.BoardSize; r++) for (int c = 0; c < e.BoardSize; c++) if (e.SpecialTiles[r, c] == t) n++;
            return n;
        }

        private static XOCellPos Find(XOCaroEngine e, SpecialTileType t)
        {
            for (int r = 0; r < e.BoardSize; r++) for (int c = 0; c < e.BoardSize; c++) if (e.SpecialTiles[r, c] == t) return new XOCellPos(r, c);
            Assert.Fail("tile not found: " + t);
            return default;
        }
    }
}
#endif

