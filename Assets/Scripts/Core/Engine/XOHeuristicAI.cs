using System.Collections.Generic;

namespace XOPlayable.Core
{
    /// <summary>Stateless tactical heuristic evaluation, AI move selection, and hint calculation.</summary>
    internal static class XOHeuristicAI
    {
        public static XOCellPos CalculateAIMove(
            XOCaroEngine engine,
            XORandomStream rng,
            CellPiece aiPiece,
            AIDifficulty difficulty,
            XOBotProfile botProfile,
            bool hasCrownOfOmni)
        {
            CellPiece opponent = (aiPiece == CellPiece.X) ? CellPiece.O : CellPiece.X;

            if (engine.MoveHistory.Count == 0 && engine.VariantRule != XOVariantRule.GravityDrop)
            {
                int mid = engine.BoardSize / 2;
                if (engine.CanPlace(mid, mid)) return new XOCellPos(mid, mid);
            }

            List<XOCellPos> candidates = GetCandidateCells(engine, 2);
            if (candidates.Count == 0)
            {
                for (int r = 0; r < engine.BoardSize; r++)
                    for (int c = 0; c < engine.BoardSize; c++)
                        if (engine.CanPlace(r, c)) return new XOCellPos(r, c);
                return new XOCellPos(engine.BoardSize / 2, engine.BoardSize / 2);
            }

            foreach (XOCellPos cell in candidates)
            {
                if (engine.WouldWinImmediatelyInternal(cell.x, cell.y, aiPiece))
                    return cell;
            }

            float mistakeProb = (botProfile != null) ? botProfile.mistakeChance : (difficulty == AIDifficulty.Easy ? 0.25f : 0f);
            if (rng.Value >= mistakeProb * 0.7f)
            {
                foreach (XOCellPos cell in candidates)
                {
                    if (engine.WouldWinImmediatelyInternal(cell.x, cell.y, opponent))
                        return cell;
                }
            }

            XOCellPos bestCell = candidates[0];
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

            if (engine.IsTowerClimb && hasCrownOfOmni)
            {
                defenseWeight = XOMath.Max(0.55f, defenseWeight - 0.20f);
                randomJitter += 85f;
            }

            int center = engine.BoardSize / 2;
            foreach (XOCellPos cell in candidates)
            {
                long attackScore = EvaluatePoint(engine, cell.x, cell.y, aiPiece);
                long defenseScore = EvaluatePoint(engine, cell.x, cell.y, opponent);

                int distFromCenter = XOMath.Abs(cell.x - center) + XOMath.Abs(cell.y - center);
                int centerBonus = XOMath.Max(0, (engine.BoardSize - distFromCenter) * 3);

                long specialTileBonus = 0;
                SpecialTileType tile = engine.SpecialTiles[cell.x, cell.y];
                if (tile == SpecialTileType.ExtraTurnLightning || tile == SpecialTileType.WarpPortal)
                    specialTileBonus = (difficulty == AIDifficulty.Easy) ? 900 : 3400;
                else if (tile == SpecialTileType.GoldBonus)
                    specialTileBonus = 750;

                long totalScore = attackScore + (long)(defenseScore * defenseWeight) + centerBonus + specialTileBonus;
                totalScore += (long)rng.Range(0f, randomJitter);

                if (totalScore > bestScore)
                {
                    bestScore = totalScore;
                    bestCell = cell;
                }
            }

            return bestCell;
        }

        public static XOCellPos CalculateBestHint(XOCaroEngine engine, CellPiece playerPiece, out string tacticalReason)
        {
            CellPiece opponent = (playerPiece == CellPiece.X) ? CellPiece.O : CellPiece.X;

            if (engine.MoveHistory.Count == 0 && engine.VariantRule != XOVariantRule.GravityDrop)
            {
                tacticalReason = "Control the center of the board!";
                return new XOCellPos(engine.BoardSize / 2, engine.BoardSize / 2);
            }

            List<XOCellPos> candidates = GetCandidateCells(engine, 2);
            if (candidates.Count == 0)
            {
                tacticalReason = "Place on an open square!";
                return new XOCellPos(engine.BoardSize / 2, engine.BoardSize / 2);
            }

            foreach (XOCellPos cell in candidates)
            {
                if (engine.WouldWinImmediatelyInternal(cell.x, cell.y, playerPiece))
                {
                    tacticalReason = $"WINNING MOVE! Place here to connect {engine.WinLength} now!";
                    return cell;
                }
            }

            foreach (XOCellPos cell in candidates)
            {
                if (engine.WouldWinImmediatelyInternal(cell.x, cell.y, opponent))
                {
                    tacticalReason = $"DANGER! Block here now before Opponent connects {engine.WinLength}!";
                    return cell;
                }
            }

            XOCellPos bestCell = candidates[0];
            long bestScore = long.MinValue;
            long bestAttack = 0;
            long bestDefense = 0;
            int center = engine.BoardSize / 2;

            foreach (XOCellPos cell in candidates)
            {
                long atk = EvaluatePoint(engine, cell.x, cell.y, playerPiece);
                long def = EvaluatePoint(engine, cell.x, cell.y, opponent);
                int centerBonus = XOMath.Max(0, (engine.BoardSize - (XOMath.Abs(cell.x - center) + XOMath.Abs(cell.y - center))) * 2);
                long specialBonus = (engine.SpecialTiles[cell.x, cell.y] == SpecialTileType.ExtraTurnLightning) ? 2500 : 0;

                long score = atk + (long)(def * 1.15f) + centerBonus + specialBonus;
                if (score > bestScore)
                {
                    bestScore = score;
                    bestCell = cell;
                    bestAttack = atk;
                    bestDefense = def;
                }
            }

            if (engine.SpecialTiles[bestCell.x, bestCell.y] == SpecialTileType.ExtraTurnLightning)
                tacticalReason = "LIGHTNING TILE! Place here to get an EXTRA TURN!";
            else if (bestDefense >= 14000 && bestDefense > bestAttack)
                tacticalReason = "CRITICAL BLOCK! Stop Opponent's attacking line!";
            else if (bestAttack >= 14000)
                tacticalReason = "DOUBLE ATTACK! Build an unstoppable fork!";
            else
                tacticalReason = "Best strategic square on the board!";

            return bestCell;
        }

        public static List<XOCellPos> GetCandidateCells(XOCaroEngine engine, int radius)
        {
            var result = new List<XOCellPos>();
            int boardSize = engine.BoardSize;

            if (engine.VariantRule == XOVariantRule.GravityDrop)
            {
                for (int c = 0; c < boardSize; c++)
                {
                    int landR = engine.GetGravityLandingRow(c);
                    if (landR >= 0) result.Add(new XOCellPos(landR, c));
                }
                return result;
            }

            if (engine.VariantRule == XOVariantRule.Infinite3x3 || (engine.VariantRule == XOVariantRule.GobbletStack && boardSize < 10))
            {
                for (int r = 0; r < boardSize; r++)
                    for (int c = 0; c < boardSize; c++)
                        if (engine.CanPlaceForPiece(r, c, engine.CurrentTurn))
                            result.Add(new XOCellPos(r, c));
                return result;
            }

            bool[,] visited = new bool[boardSize, boardSize];
            for (int r = 0; r < boardSize; r++)
            {
                for (int c = 0; c < boardSize; c++)
                {
                    if (engine.Board[r, c] != CellPiece.Empty
                        || engine.SpecialTiles[r, c] == SpecialTileType.ExtraTurnLightning
                        || engine.SpecialTiles[r, c] == SpecialTileType.WarpPortal)
                    {
                        for (int dr = -radius; dr <= radius; dr++)
                        {
                            for (int dc = -radius; dc <= radius; dc++)
                            {
                                int nr = r + dr;
                                int nc = c + dc;
                                if (engine.CanPlaceForPiece(nr, nc, engine.CurrentTurn) && !visited[nr, nc])
                                {
                                    visited[nr, nc] = true;
                                    result.Add(new XOCellPos(nr, nc));
                                }
                            }
                        }
                    }
                }
            }

            return result;
        }

        public static long EvaluatePoint(XOCaroEngine engine, int r, int c, CellPiece piece)
        {
            long total = 0;
            int openThreesOrBetter = 0;
            CellPiece opponent = (piece == CellPiece.X) ? CellPiece.O : CellPiece.X;

            for (int d = 0; d < 4; d++)
            {
                int dr = XOWinDetector.DirRow[d];
                int dc = XOWinDetector.DirCol[d];

                int count = 1;
                int openEnds = 0;
                int enemyBlockedEnds = 0;

                int nr = r + dr;
                int nc = c + dc;
                while (engine.IsValidCell(nr, nc) && engine.Board[nr, nc] == piece)
                {
                    count++;
                    nr += dr;
                    nc += dc;
                }
                if (engine.CanPlace(nr, nc)) openEnds++;
                else if (engine.IsValidCell(nr, nc) && engine.Board[nr, nc] == opponent) enemyBlockedEnds++;

                nr = r - dr;
                nc = c - dc;
                while (engine.IsValidCell(nr, nc) && engine.Board[nr, nc] == piece)
                {
                    count++;
                    nr -= dr;
                    nc -= dc;
                }
                if (engine.CanPlace(nr, nc)) openEnds++;
                else if (engine.IsValidCell(nr, nc) && engine.Board[nr, nc] == opponent) enemyBlockedEnds++;

                if (engine.VariantRule == XOVariantRule.StrictCaro && count == engine.WinLength && enemyBlockedEnds == 2)
                    continue;

                if (openEnds == 0 && count < engine.WinLength)
                    continue;

                long dirScore = ScorePattern(count, openEnds, engine.WinLength);
                if (dirScore >= 4000) openThreesOrBetter++;
                total += dirScore;
            }

            if (openThreesOrBetter >= 2) total += 35000;
            return total;
        }

        private static long ScorePattern(int count, int openEnds, int winLength)
        {
            if (count >= winLength) return 1000000;

            int needed = winLength - count;
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

