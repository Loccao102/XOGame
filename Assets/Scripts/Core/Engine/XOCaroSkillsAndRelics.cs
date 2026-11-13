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
            engine.FrozenFor[r, c] = CellPiece.Empty;
            return true;
        }

        /// <summary>
        /// Boss freeze length in ticks (one tick per placement), cast AFTER the boss moves. 2 locks the cell for one player turn and one boss turn,
        /// so the player gets first access when it thaws (odd lengths hand the thawed cell to the boss: 8% vs 40% player win rate on floor 3).
        /// </summary>
        internal static int BossFreezeTurns = 2;
        /// <summary>Minimum player threat (EvaluatePoint scale: 12000 open three, 22000 four) before the boss spends its freeze.</summary>
        internal static long BossFreezeThreatMin = 12000;

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

            if (highestPlayerThreat >= BossFreezeThreatMin && bestThreatCell.IsValid)
            {
                bool hasThaw = engine.HasRelicInternal(CaroRelicType.ThawCharm);
                engine.FrozenTurns[bestThreatCell.x, bestThreatCell.y] = BossFreezeTurns;
                // Classic freeze locks both sides. With Thaw Charm the ice turns on the boss: only the boss is locked out of the cell.
                engine.FrozenFor[bestThreatCell.x, bestThreatCell.y] = hasThaw ? CellPiece.O : CellPiece.Empty;
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

