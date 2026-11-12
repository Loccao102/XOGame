using System;
using System.Collections.Generic;

namespace XOPlayable.Core
{
    /// <summary>
    /// Search-based AI used by Tower bosses (<see cref="AIDifficulty.Boss"/>).
    /// Compared with the one-ply heuristic it adds: split-three / four-three / three-three recognition,
    /// a VCF search (victory by continuous fours) for attack and defence, and a 2-ply reply check.
    /// Strength scales with the boss Elo through <see cref="Tuning"/>.
    /// Lookahead only runs on Classic/Strict boards, where "set Board[r,c]" is an exact simulation of a move;
    /// Gravity / Gobblet / Chaos boards use the same pattern scorer one ply deep.
    /// </summary>
    internal static class XOBossAI
    {
        internal static Action<string> DebugLog;      // tuning aid, null in the game
        internal static float AttackWeight = 1.5f;      // bosses keep the initiative; defence is guaranteed by the hard blocks + VCF defence below
        internal static float DefenseWeight = 0.95f;

        public struct Tuning
        {
            public int Breadth;       // root candidates searched
            public int ReplyBreadth;  // opponent replies examined per root candidate
            public int VcfDepth;      // max consecutive fours searched
            public int VcfBudget;     // node cap per VCF search
            public int MoveNodes;     // node cap for the whole move (deterministic, keeps WebGL frame time bounded)
        }

        public static Tuning TuneForElo(int elo)
        {
            int level = XOMath.Clamp((elo - 1200) / 200, 0, 6);
            return new Tuning
            {
                Breadth = 6 + level,
                ReplyBreadth = 5 + level / 2,
                VcfDepth = 3 + level,
                VcfBudget = 150 + 120 * level,
                MoveNodes = 2500 + 1000 * level
            };
        }

        public static bool IsSearchable(XOCaroEngine e)
            => e.VariantRule == XOVariantRule.ClassicCaro || e.VariantRule == XOVariantRule.StrictCaro;

        // ------------------------------------------------------------------ pattern analysis

        // Scratch buffers: the AI is single-threaded, so static reuse avoids GC churn on WebGL.
        private static readonly int[] Line = new int[40];

        private static readonly int[] DirR = { 0, 1, 1, 1 };
        private static readonly int[] DirC = { 1, 0, 1, -1 };

        /// <summary>0 = empty, 1 = own stone, 2 = blocked (enemy, wall or rock).</summary>
        private static int Code(XOCaroEngine e, int r, int c, CellPiece me)
        {
            if (!e.IsValidCell(r, c)) return 2;
            CellPiece p = e.Board[r, c];
            if (p == me) return 1;
            if (p != CellPiece.Empty) return 2;
            if (e.SpecialTiles[r, c] == SpecialTileType.RockObstacle) return 2;
            return 0;
        }

        private static void FillLine(XOCaroEngine e, int r, int c, int d, CellPiece me, int w)
        {
            int len = 2 * w - 1;
            for (int i = 0; i < len; i++)
            {
                int k = i - (w - 1);
                Line[i] = (k == 0) ? 1 : Code(e, r + DirR[d] * k, c + DirC[d] * k, me);
            }
        }

        /// <summary>Distinct cells that would complete w-in-a-row, counting only windows through the centre stone.</summary>
        private static int CountWinSlots(int w, out bool five)
        {
            int len = 2 * w - 1;
            long mask = 0;
            five = false;
            for (int s = 0; s < w; s++)
            {
                int own = 0, blocked = 0, emptyIdx = -1;
                for (int j = 0; j < w; j++)
                {
                    int v = Line[s + j];
                    if (v == 1) own++;
                    else if (v == 2) blocked++;
                    else emptyIdx = s + j;
                }
                if (blocked != 0) continue;
                if (own == w) five = true;
                else if (own == w - 1 && emptyIdx >= 0) mask |= 1L << emptyIdx;
            }
            int n = 0;
            for (int i = 0; i < len; i++) if ((mask & (1L << i)) != 0) n++;
            return n;
        }

        private static void AnalyzeLine(int w, out bool five, out int fours, out bool liveThree, out int soft)
        {
            int len = 2 * w - 1;
            fours = CountWinSlots(w, out five);
            liveThree = false;
            soft = 0;

            if (!five && fours == 0)
            {
                for (int i = 0; i < len; i++)
                {
                    if (Line[i] != 0) continue;
                    Line[i] = 1;
                    int n = CountWinSlots(w, out bool f);
                    Line[i] = 0;
                    if (n >= 2 && !f) { liveThree = true; break; }
                }
            }

            for (int s = 0; s < w; s++)
            {
                int own = 0, blocked = 0;
                for (int j = 0; j < w; j++)
                {
                    int v = Line[s + j];
                    if (v == 1) own++;
                    else if (v == 2) blocked++;
                }
                if (blocked != 0) continue;
                if (own == 3) soft += 300;
                else if (own == 2) soft += 60;
                else if (own == 1) soft += 8;
            }
        }

        /// <summary>Value for <paramref name="me"/> of owning cell (r,c), including four-three and three-three forks.</summary>
        public static long PatternScore(XOCaroEngine e, int r, int c, CellPiece me)
        {
            int w = e.WinLength;
            long total = 0;
            int fourDirs = 0, threeDirs = 0;

            for (int d = 0; d < 4; d++)
            {
                FillLine(e, r, c, d, me, w);
                AnalyzeLine(w, out bool five, out int fours, out bool liveThree, out int soft);
                if (five) return 1000000;

                if (fours >= 2) { total += 150000; fourDirs += 2; }
                else if (fours == 1) { total += 22000; fourDirs += 1; }
                else if (liveThree) { total += 14000; threeDirs += 1; }
                total += soft;
            }

            if (fourDirs >= 2) total += 100000;
            else if (fourDirs >= 1 && threeDirs >= 1) total += 60000;
            else if (threeDirs >= 2) total += 30000;
            return total;
        }

        /// <summary>True when placing at (r,c) would create at least one four-line for <paramref name="me"/> (cheap pre-filter).</summary>
        private static bool CreatesFourPattern(XOCaroEngine e, int r, int c, CellPiece me)
        {
            int w = e.WinLength;
            for (int d = 0; d < 4; d++)
            {
                FillLine(e, r, c, d, me, w);
                int n = CountWinSlots(w, out bool five);
                if (five || n >= 1) return true;
            }
            return false;
        }

        /// <summary>Exact winning replies for <paramref name="me"/> after a stone was just placed at (r,c); stops counting at 2.</summary>
        private static int CountWinCells(XOCaroEngine e, int r, int c, CellPiece me, out XOCellPos first)
        {
            first = XOCellPos.Invalid;
            int count = 0;
            int reach = e.WinLength - 1;
            for (int d = 0; d < 4; d++)
            {
                for (int k = -reach; k <= reach; k++)
                {
                    if (k == 0) continue;
                    int nr = r + DirR[d] * k, nc = c + DirC[d] * k;
                    if (!e.IsValidCell(nr, nc) || e.Board[nr, nc] != CellPiece.Empty) continue;
                    if (!e.WouldWinImmediatelyInternal(nr, nc, me)) continue;
                    if (count == 0) first = new XOCellPos(nr, nc);
                    count++;
                    if (count >= 2) return count;
                }
            }
            return count;
        }

        // ------------------------------------------------------------------ VCF

        // Pooled scratch for the VCF recursion: a stamp grid instead of a fresh bool[,] and one reusable list per recursion level.
        private static int[,] _stamp = new int[0, 0];
        private static int _stampId;
        private static readonly List<XOCellPos>[] _listPool = new List<XOCellPos>[32];
        private static int _poolTop;
        private static int _moveNodes;

        private static void FillCellsNearStones(XOCaroEngine e, CellPiece owner, int radius, List<XOCellPos> list)
        {
            int n = e.BoardSize;
            if (_stamp.GetLength(0) != n) { _stamp = new int[n, n]; _stampId = 0; }
            _stampId++;
            list.Clear();
            for (int r = 0; r < n; r++)
            {
                for (int c = 0; c < n; c++)
                {
                    if (e.Board[r, c] != owner) continue;
                    for (int dr = -radius; dr <= radius; dr++)
                    {
                        for (int dc = -radius; dc <= radius; dc++)
                        {
                            int nr = r + dr, nc = c + dc;
                            if (!e.IsValidCell(nr, nc) || _stamp[nr, nc] == _stampId) continue;
                            if (e.Board[nr, nc] != CellPiece.Empty) continue;
                            _stamp[nr, nc] = _stampId;
                            list.Add(new XOCellPos(nr, nc));
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Can <paramref name="a"/> force a win using only fours (each answered by a single forced block)?
        /// Caller guarantees the defender has no immediate winning move on the current board.
        /// </summary>
        public static bool HasVcf(XOCaroEngine e, CellPiece a, CellPiece d, int depth, int budget, out XOCellPos firstMove)
        {
            int b = Math.Min(budget, _moveNodes);
            int before = b;
            bool found = Vcf(e, a, d, depth, ref b, out firstMove);
            _moveNodes -= before - b;
            return found;
        }

        private static bool Vcf(XOCaroEngine e, CellPiece a, CellPiece d, int depth, ref int budget, out XOCellPos first)
        {
            first = XOCellPos.Invalid;
            if (depth <= 0 || budget <= 0 || _poolTop >= _listPool.Length) return false;

            List<XOCellPos> cells = _listPool[_poolTop] ?? (_listPool[_poolTop] = new List<XOCellPos>(64));
            _poolTop++;
            try { return VcfNode(e, a, d, depth, ref budget, out first, cells); }
            finally { _poolTop--; }
        }

        private static bool VcfNode(XOCaroEngine e, CellPiece a, CellPiece d, int depth, ref int budget, out XOCellPos first, List<XOCellPos> cells)
        {
            first = XOCellPos.Invalid;
            FillCellsNearStones(e, a, 2, cells);

            foreach (XOCellPos m in cells)
            {
                budget--;
                if (e.WouldWinImmediatelyInternal(m.x, m.y, a)) { first = m; return true; }
            }

            foreach (XOCellPos m in cells)
            {
                if (budget <= 0) break;
                if (!e.CanPlaceForPiece(m.x, m.y, a)) continue;
                if (!CreatesFourPattern(e, m.x, m.y, a)) continue;

                budget--;
                e.Board[m.x, m.y] = a;
                int wins = CountWinCells(e, m.x, m.y, a, out XOCellPos block);
                bool success = false;

                if (wins >= 2)
                {
                    success = true;
                }
                else if (wins == 1)
                {
                    e.Board[block.x, block.y] = d;
                    // If the forced block gives the defender a four of their own, this line is not a clean forced win.
                    bool counter = CountWinCells(e, block.x, block.y, d, out _) >= 1;
                    if (!counter) success = Vcf(e, a, d, depth - 1, ref budget, out _);
                    e.Board[block.x, block.y] = CellPiece.Empty;
                }

                e.Board[m.x, m.y] = CellPiece.Empty;
                if (success) { first = m; return true; }
            }

            return false;
        }

        // ------------------------------------------------------------------ move selection

        private struct Scored
        {
            public XOCellPos cell;
            public long order;
        }

        private static long OrderScore(XOCaroEngine e, XOCellPos cell, CellPiece me, CellPiece opp, long attack, long defense)
        {
            int center = e.BoardSize / 2;
            int dist = XOMath.Abs(cell.x - center) + XOMath.Abs(cell.y - center);
            long centerBonus = XOMath.Max(0, (e.BoardSize - dist) * 3);

            long tile = 0;
            SpecialTileType t = e.SpecialTiles[cell.x, cell.y];
            if (t == SpecialTileType.ExtraTurnLightning || t == SpecialTileType.WarpPortal) tile = 3400;
            else if (t == SpecialTileType.GoldBonus) tile = 750;

            return (long)(attack * AttackWeight) + (long)(defense * DefenseWeight) + centerBonus + tile;
        }

        /// <summary>Returns <see cref="XOCellPos.Invalid"/> when the search cannot decide; the caller then falls back to the heuristic.</summary>
        public static XOCellPos CalculateMove(XOCaroEngine e, XORandomStream rng, CellPiece me, int elo)
        {
            if (!IsSearchable(e)) return XOCellPos.Invalid;
            CellPiece opp = (me == CellPiece.X) ? CellPiece.O : CellPiece.X;
            Tuning tune = TuneForElo(elo);
            _moveNodes = tune.MoveNodes;
            _poolTop = 0;

            List<XOCellPos> cands = XOHeuristicAI.GetCandidateCells(e, 2);
            if (cands.Count == 0) return XOCellPos.Invalid;

            // 1. Win now.
            foreach (XOCellPos c in cands)
                if (e.WouldWinImmediatelyInternal(c.x, c.y, me)) return c;

            // 2. Block an immediate loss.
            foreach (XOCellPos c in cands)
                if (e.WouldWinImmediatelyInternal(c.x, c.y, opp)) return c;

            // 3. Forced win by continuous fours.
            if (HasVcf(e, me, opp, tune.VcfDepth, tune.VcfBudget, out XOCellPos vcfMove) && vcfMove.IsValid)
                { DebugLog?.Invoke($"ply {e.MoveHistory.Count}: own VCF -> {vcfMove.x},{vcfMove.y}"); return vcfMove; }

            // Score and order every candidate once, on the untouched board.
            var scored = new List<Scored>(cands.Count);
            foreach (XOCellPos c in cands)
            {
                long atk = PatternScore(e, c.x, c.y, me);
                long def = PatternScore(e, c.x, c.y, opp);
                scored.Add(new Scored { cell = c, order = OrderScore(e, c, me, opp, atk, def) });
            }
            scored.Sort((x, y) => y.order.CompareTo(x.order));

            // 4. If the opponent has a VCF now, find a move that breaks it.
            if (HasVcf(e, opp, me, tune.VcfDepth, tune.VcfBudget, out _))
            {
                int tried = 0;
                foreach (Scored s in scored)
                {
                    if (tried++ >= 24) break;
                    e.Board[s.cell.x, s.cell.y] = me;
                    int myWins = CountWinCells(e, s.cell.x, s.cell.y, me, out _);
                    bool safe = myWins >= 1 || !HasVcf(e, opp, me, tune.VcfDepth, tune.VcfBudget, out _);
                    e.Board[s.cell.x, s.cell.y] = CellPiece.Empty;
                    if (safe) { DebugLog?.Invoke($"ply {e.MoveHistory.Count}: DEFEND opp VCF with {s.cell.x},{s.cell.y}"); return s.cell; }
                }
            }

            // 5. Two-ply evaluation of the best candidates.
            int breadth = XOMath.Min(tune.Breadth, scored.Count);
            XOCellPos best = scored[0].cell;
            long bestValue = long.MinValue;

            for (int i = 0; i < breadth; i++)
            {
                XOCellPos m = scored[i].cell;
                long value = scored[i].order;

                e.Board[m.x, m.y] = me;
                int wc = CountWinCells(e, m.x, m.y, me, out XOCellPos forcedBlock);

                if (wc >= 2)
                {
                    value += 900000;
                }
                else
                {
                    long replyBest = 0;
                    if (wc == 1)
                    {
                        replyBest = ReplyValue(e, forcedBlock, me, opp);
                    }
                    else
                    {
                        List<XOCellPos> replies = XOHeuristicAI.GetCandidateCells(e, 2);
                        var rs = new List<long>(replies.Count);
                        foreach (XOCellPos rc in replies) rs.Add(ReplyValue(e, rc, me, opp));
                        rs.Sort((x, y) => y.CompareTo(x));
                        int rb = XOMath.Min(tune.ReplyBreadth, rs.Count);
                        for (int k = 0; k < rb; k++) replyBest = Math.Max(replyBest, rs[k]);

                        if (HasVcf(e, opp, me, tune.VcfDepth, tune.VcfBudget, out _))
                        {
                            value -= 600000;
                        }
                        else if (ForcedWinAfterQuietMove(e, me, opp, replies, tune))
                        {
                            value += 800000; // threat-space win: every reply loses to a VCF
                        }
                    }
                    value -= (long)(replyBest * 0.9f);
                }

                e.Board[m.x, m.y] = CellPiece.Empty;

                value += (long)rng.Range(0f, 25f); // breaks deterministic lock-ups without changing tactical priorities
                if (value > bestValue) { bestValue = value; best = m; }
            }

            DebugLog?.Invoke($"ply {e.MoveHistory.Count}: search best {best.x},{best.y} value={bestValue}");
            return best;
        }

        /// <summary>
        /// Called with my quiet move already on the board and the opponent to move.
        /// True when I would have a VCF if the opponent passed AND no opponent reply (that does not itself make a four) stops it.
        /// </summary>
        private static bool ForcedWinAfterQuietMove(XOCaroEngine e, CellPiece me, CellPiece opp, List<XOCellPos> replies, Tuning tune)
        {
            if (!HasVcf(e, me, opp, tune.VcfDepth, tune.VcfBudget, out _)) return false;

            foreach (XOCellPos rc in replies)
            {
                e.Board[rc.x, rc.y] = opp;
                bool oppFour = CountWinCells(e, rc.x, rc.y, opp, out _) >= 1;
                bool stillWins = !oppFour && HasVcf(e, me, opp, tune.VcfDepth, tune.VcfBudget, out _);
                e.Board[rc.x, rc.y] = CellPiece.Empty;
                if (!stillWins) return false;
            }
            return true;
        }

        /// <summary>How good cell c is for the opponent to answer with (own gain plus denial of mine).</summary>
        private static long ReplyValue(XOCaroEngine e, XOCellPos c, CellPiece me, CellPiece opp)
        {
            long gain = PatternScore(e, c.x, c.y, opp);
            long denial = Math.Min(PatternScore(e, c.x, c.y, me), 80000L);
            return gain + (long)(denial * 0.8f);
        }
    }
}
