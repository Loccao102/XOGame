using System.Collections.Generic;

namespace XOPlayable.Core
{
    /// <summary>Player tactical skills, Bot counter-skills, and active Roguelike Relic board abilities.</summary>
    internal static class XOCaroSkillsAndRelics
    {
        public static bool BombDestroyPiece(XOCaroEngine engine, int r, int c, CellPiece targetPiece = CellPiece.O)
        {
            if (engine.State != MatchState.Playing || !engine.IsValidCell(r, c)) return false;
            if (engine.Board[r, c] != targetPiece) return false;

            engine.Board[r, c] = CellPiece.Empty;
            engine.PieceSizes[r, c] = 0;
            engine.FrozenTurns[r, c] = 0;
            engine.RemoveFromQueuesInternal(new XOCellPos(r, c));
            engine.RemoveLastHistoryAtInternal(r, c);
            return true;
        }

        public static bool SwapOpponentPiece(XOCaroEngine engine, int r, int c, CellPiece targetOpponent = CellPiece.O, CellPiece newOwner = CellPiece.X)
        {
            if (engine.State != MatchState.Playing || !engine.IsValidCell(r, c)) return false;
            if (engine.Board[r, c] != targetOpponent) return false;

            engine.Board[r, c] = newOwner;
            engine.FrozenTurns[r, c] = 0;
            engine.TransferCellQueueOwnerInternal(new XOCellPos(r, c), newOwner);
            engine.ReplaceLastHistoryOwnerInternal(r, c, newOwner);

            if (engine.CheckWinFromCell(r, c, newOwner, out List<XOCellPos> winLine, out _))
            {
                if (engine.RegisterWinLineIfValid(winLine, newOwner, out bool won) && won)
                {
                    engine.SetMatchStateInternal((newOwner == CellPiece.X) ? MatchState.XWon : MatchState.OWon);
                }
            }

            return true;
        }

        public static bool FreezeEmptyCell(XOCaroEngine engine, int r, int c, int turns = 3)
        {
            if (engine.State != MatchState.Playing || !engine.IsValidCell(r, c)) return false;
            if (!engine.CanPlace(r, c)) return false;

            engine.FrozenTurns[r, c] = turns;
            return true;
        }

        public static bool TryExecuteBotCounterFreeze(XOCaroEngine engine, XOBotProfile bot, out XOCellPos frozenCell)
        {
            frozenCell = XOCellPos.Invalid;
            if (engine.VariantRule == XOVariantRule.Infinite3x3) return false;
            if (bot == null || (bot.counterSkill != BotCounterSkill.CryoFreeze && bot.counterSkill != BotCounterSkill.WarlordStrike))
                return false;

            List<XOCellPos> candidates = engine.GetCandidateCells(2);
            XOCellPos bestThreatCell = XOCellPos.Invalid;
            long highestPlayerThreat = 0;

            foreach (XOCellPos cell in candidates)
            {
                if (engine.WouldWinImmediatelyInternal(cell.x, cell.y, CellPiece.O))
                    return false;

                long playerThreat = engine.EvaluatePoint(cell.x, cell.y, CellPiece.X);
                if (playerThreat > highestPlayerThreat)
                {
                    highestPlayerThreat = playerThreat;
                    bestThreatCell = cell;
                }
            }

            if (highestPlayerThreat >= 12000 && bestThreatCell.IsValid)
            {
                engine.FrozenTurns[bestThreatCell.x, bestThreatCell.y] = engine.HasRelicInternal(CaroRelicType.ThawCharm) ? 1 : 3;
                frozenCell = bestThreatCell;
                return true;
            }

            return false;
        }

        public static bool TryExecuteBotFlameBlade(XOCaroEngine engine, out XOCellPos bombedCell)
        {
            bombedCell = XOCellPos.Invalid;
            List<XOCellPos> candidates = engine.GetCandidateCells(1);
            long maxThreat = 0;
            XOCellPos target = XOCellPos.Invalid;

            foreach (XOCellPos cell in candidates)
            {
                if (engine.Board[cell.x, cell.y] == CellPiece.X)
                {
                    long threat = engine.EvaluatePoint(cell.x, cell.y, CellPiece.X);
                    if (threat > maxThreat)
                    {
                        maxThreat = threat;
                        target = cell;
                    }
                }
            }

            if (maxThreat >= 6000 && target.IsValid)
            {
                engine.Board[target.x, target.y] = CellPiece.Empty;
                engine.PieceSizes[target.x, target.y] = 0;
                bombedCell = target;
                return true;
            }
            return false;
        }

        public static bool TryExecuteBotThornArmor(XOCaroEngine engine, out XOCellPos thornCell)
        {
            thornCell = XOCellPos.Invalid;
            List<XOCellPos> candidates = engine.GetCandidateCells(1);
            long maxThreat = 0;
            XOCellPos target = XOCellPos.Invalid;

            foreach (XOCellPos cell in candidates)
            {
                if (engine.CanPlace(cell.x, cell.y))
                {
                    long threat = engine.EvaluatePoint(cell.x, cell.y, CellPiece.X);
                    if (threat > maxThreat)
                    {
                        maxThreat = threat;
                        target = cell;
                    }
                }
            }

            if (maxThreat >= 8000 && target.IsValid)
            {
                engine.SpecialTiles[target.x, target.y] = SpecialTileType.RockObstacle;
                thornCell = target;
                return true;
            }
            return false;
        }

        public static bool IsBotImmuneToSkill(BotEquipmentType equipment, string skillName)
        {
            return (equipment == BotEquipmentType.MirrorAegis || equipment == BotEquipmentType.OmniCrown)
                && (skillName == "Bomb" || skillName == "Swap");
        }

        public static bool TryExecuteGuardianAegis(
            XOCaroEngine engine,
            XORoguelikeRunState runState,
            bool hasGuardianAegis,
            out XOCellPos frozenCell)
        {
            frozenCell = XOCellPos.Invalid;
            if (!engine.IsTowerClimb || !hasGuardianAegis || runState.GuardianAegisTriggeredThisMatch)
                return false;

            for (int r = 0; r < engine.BoardSize; r++)
            {
                for (int c = 0; c < engine.BoardSize; c++)
                {
                    if (engine.Board[r, c] != CellPiece.Empty) continue;

                    engine.Board[r, c] = CellPiece.O;
                    bool aiWouldWin = engine.CheckWinFromCell(r, c, CellPiece.O, out _);
                    engine.Board[r, c] = CellPiece.Empty;

                    if (aiWouldWin)
                    {
                        FreezeEmptyCell(engine, r, c, 2);
                        runState.GuardianAegisTriggeredThisMatch = true;
                        frozenCell = new XOCellPos(r, c);
                        return true;
                    }
                }
            }
            return false;
        }

        public static bool TryExecuteShatterHammer(
            XOCaroEngine engine,
            XORoguelikeRunState runState,
            bool hasShatterHammer,
            int r,
            int c,
            out XOCellPos shatteredCell)
        {
            shatteredCell = XOCellPos.Invalid;
            if (!engine.IsTowerClimb || !hasShatterHammer || runState.ShatterHammerTriggeredThisMatch)
                return false;

            for (int dr = -1; dr <= 1; dr++)
            {
                for (int dc = -1; dc <= 1; dc++)
                {
                    if (dr == 0 && dc == 0) continue;
                    int nr = r + dr, nc = c + dc;
                    if (engine.IsValidCell(nr, nc) && engine.Board[nr, nc] == CellPiece.O)
                    {
                        engine.Board[nr, nc] = CellPiece.Empty;
                        runState.ShatterHammerTriggeredThisMatch = true;
                        shatteredCell = new XOCellPos(nr, nc);
                        return true;
                    }
                }
            }
            return false;
        }
    }
}

