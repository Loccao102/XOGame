using System;
using System.Collections.Generic;

namespace XOPlayable.Core
{
    /// <summary>Handles special board tile placement, mines, warp portals, voids, collapsing rings, and cosmic shifts.</summary>
    internal static class XOSpecialTileSystem
    {
        public static void SpawnBalancedSpecialTiles(
            XOCaroEngine engine,
            XORandomStream rng,
            int? seed,
            int floorNumber,
            bool hasMinePlanter)
        {
            int boardSize = engine.BoardSize;
            if (boardSize <= 4)
            {
                engine.SpecialTiles[0, boardSize - 1] = SpecialTileType.GoldBonus;
                return;
            }

            int mid = boardSize / 2;
            int minCoord = XOMath.Max(1, mid - 3);
            int maxCoord = XOMath.Min(boardSize - 2, mid + 3);

            var pool = new List<XOCellPos>();
            for (int r = minCoord; r <= maxCoord; r++)
            {
                for (int c = minCoord; c <= maxCoord; c++)
                {
                    if (XOMath.Abs(r - mid) <= 1 && XOMath.Abs(c - mid) <= 1) continue;
                    pool.Add(new XOCellPos(r, c));
                }
            }

            Random seededRnd = seed.HasValue ? new Random(seed.Value) : null;
            for (int i = 0; i < pool.Count; i++)
            {
                int swapIdx = (seededRnd != null) ? seededRnd.Next(i, pool.Count) : rng.Range(i, pool.Count);
                (pool[i], pool[swapIdx]) = (pool[swapIdx], pool[i]);
            }

            int cursor = 0;
            void PlaceSpecial(SpecialTileType type, int count)
            {
                for (int k = 0; k < count && cursor < pool.Count; k++, cursor++)
                {
                    XOCellPos pos = pool[cursor];
                    engine.SpecialTiles[pos.x, pos.y] = type;
                }
            }

            if (floorNumber == 6)
            {
                PlaceSpecial(SpecialTileType.RockObstacle, 6);
                PlaceSpecial(SpecialTileType.GoldBonus, 2);
            }
            else if (floorNumber == 7)
            {
                PlaceSpecial(SpecialTileType.ExtraTurnLightning, 4);
                PlaceSpecial(SpecialTileType.GoldBonus, 2);
            }
            else if (engine.VariantRule == XOVariantRule.GravityDrop)
            {
                PlaceSpecial(SpecialTileType.GoldBonus, 2);
                PlaceSpecial(SpecialTileType.ExtraTurnLightning, 2);
            }
            else if (engine.VariantRule == XOVariantRule.ChaosMinefield || floorNumber == 2)
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

            if (engine.IsTowerClimb && hasMinePlanter)
            {
                PlaceSpecial(SpecialTileType.HiddenMine, 2);
            }
        }

        public static void ExecuteMineBlastAround(
            XOCaroEngine engine,
            XORandomStream rng,
            int centerR,
            int centerC,
            List<BlastedPiece> blasted)
        {
            var neighbors = new List<XOCellPos>();
            for (int dr = -1; dr <= 1; dr++)
            {
                for (int dc = -1; dc <= 1; dc++)
                {
                    if (dr == 0 && dc == 0) continue;
                    int nr = centerR + dr;
                    int nc = centerC + dc;
                    if (engine.IsValidCell(nr, nc) && engine.Board[nr, nc] != CellPiece.Empty)
                        neighbors.Add(new XOCellPos(nr, nc));
                }
            }

            for (int i = 0; i < neighbors.Count; i++)
            {
                int swapIdx = rng.Range(i, neighbors.Count);
                (neighbors[i], neighbors[swapIdx]) = (neighbors[swapIdx], neighbors[i]);
            }

            int blastCount = XOMath.Min(2, neighbors.Count);
            for (int k = 0; k < blastCount; k++)
            {
                XOCellPos pos = neighbors[k];
                blasted?.Add(new BlastedPiece(pos.x, pos.y, engine.Board[pos.x, pos.y], engine.PieceSizes[pos.x, pos.y]));
                engine.ClearCellInternal(pos.x, pos.y);
            }
        }

        public static XOCellPos ExecuteWarpPortalClone(
            XOCaroEngine engine,
            XORandomStream rng,
            int srcR,
            int srcC,
            CellPiece piece)
        {
            for (int r = 0; r < engine.BoardSize; r++)
            {
                for (int c = 0; c < engine.BoardSize; c++)
                {
                    if ((r != srcR || c != srcC)
                        && engine.SpecialTiles[r, c] == SpecialTileType.WarpPortal
                        && engine.CanPlace(r, c))
                    {
                        engine.PlaceBonusTwinInternal(r, c, piece);
                        return new XOCellPos(r, c);
                    }
                }
            }

            List<XOCellPos> candidates = engine.GetCandidateCells(1);
            if (candidates.Count > 0)
            {
                XOCellPos pick = candidates[rng.Range(0, candidates.Count)];
                engine.PlaceBonusTwinInternal(pick.x, pick.y, piece);
                return pick;
            }

            return XOCellPos.Invalid;
        }

        public static void ApplyCenterVoid(XOCaroEngine engine, int radius = 1)
        {
            int mid = engine.BoardSize / 2;
            for (int r = mid - radius; r <= mid + radius; r++)
            {
                for (int c = mid - radius; c <= mid + radius; c++)
                {
                    if (engine.IsValidCell(r, c))
                        engine.SpecialTiles[r, c] = SpecialTileType.RockObstacle;
                }
            }
        }

        public static bool CollapseNextPerimeterRing(XOCaroEngine engine, out List<XOCellPos> newlyCollapsed)
        {
            newlyCollapsed = new List<XOCellPos>();
            int ring = engine.CollapsedRingsCount;
            int maxRings = (engine.BoardSize / 2) - 2;
            if (ring >= maxRings) return false;

            int minR = ring;
            int maxR = engine.BoardSize - 1 - ring;
            int minC = ring;
            int maxC = engine.BoardSize - 1 - ring;

            for (int r = minR; r <= maxR; r++)
            {
                for (int c = minC; c <= maxC; c++)
                {
                    if ((r == minR || r == maxR || c == minC || c == maxC)
                        && engine.SpecialTiles[r, c] != SpecialTileType.RockObstacle)
                    {
                        engine.ClearCellInternal(r, c);
                        engine.SpecialTiles[r, c] = SpecialTileType.RockObstacle;
                        engine.FrozenTurns[r, c] = 0;
                        newlyCollapsed.Add(new XOCellPos(r, c));
                    }
                }
            }

            engine.IncrementCollapsedRingsInternal();
            return newlyCollapsed.Count > 0;
        }

        public static bool ExecuteCosmicShift(XOCaroEngine engine, XORandomStream rng, out List<XOCellPos> affectedCells)
        {
            affectedCells = new List<XOCellPos>();
            var empties = new List<XOCellPos>();
            for (int r = 1; r < engine.BoardSize - 1; r++)
            {
                for (int c = 1; c < engine.BoardSize - 1; c++)
                {
                    if (engine.Board[r, c] == CellPiece.Empty && engine.SpecialTiles[r, c] == SpecialTileType.None)
                        empties.Add(new XOCellPos(r, c));
                }
            }

            if (empties.Count < 2) return false;

            int idx1 = rng.Range(0, empties.Count);
            int idx2 = (idx1 + rng.Range(1, empties.Count)) % empties.Count;

            engine.SpecialTiles[empties[idx1].x, empties[idx1].y] = SpecialTileType.ExtraTurnLightning;
            engine.SpecialTiles[empties[idx2].x, empties[idx2].y] = SpecialTileType.ExtraTurnLightning;

            affectedCells.Add(empties[idx1]);
            affectedCells.Add(empties[idx2]);
            return true;
        }
    }
}

