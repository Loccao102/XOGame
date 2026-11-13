using XOPlayable.Core;

/// <summary>
/// Per-relic win-rate batches for the Tower. A strong "player proxy" (Boss AI at --proxy Elo, playing X) climbs real floor
/// configs against the real boss kit. Every relic is run alone against a no-relic baseline on the SAME game seeds,
/// so the delta column is a paired comparison.
/// Orchestration mirrors XOMatchSessionController: boss skill -> boss move -> double move, hands limit, Guardian Aegis,
/// Shatter Hammer, Phoenix Sigil, collapse/cosmic/mid-match shifts. Omens are not applied (catalog blinds only).
/// Relics that only touch coins or UI (Knight Step, Bounty Hunter, Alchemist Stone, Chrono Pocket, Telescope) are listed as not simulated.
/// </summary>
internal static class RelicSim
{
    private static readonly CaroRelicType[] Simulated =
    {
        CaroRelicType.DiagonalPrism, CaroRelicType.TwinThreat, CaroRelicType.GuardianAegis, CaroRelicType.ShatterHammer,
        CaroRelicType.VampireFang, CaroRelicType.CrownOfOmni, CaroRelicType.ThawCharm, CaroRelicType.Counterstrike,
        CaroRelicType.Hourglass, CaroRelicType.MinePlanter
    };

    private static readonly CaroRelicType[] NotSimulated =
    {
        CaroRelicType.KnightStep, CaroRelicType.BountyHunter, CaroRelicType.AlchemistStone, CaroRelicType.ChronoPocket, CaroRelicType.Telescope
    };

    private static float ProxySlip = 0.08f;

    public static void Run(string[] args)
    {
        // relics <games> <proxyElo> <floors csv>
        int games = args.Length > 1 ? int.Parse(args[1]) : 20;
        int proxyElo = args.Length > 2 ? int.Parse(args[2]) : 1800;
        int[] floors = args.Length > 3 ? args[3].Split(',').Select(int.Parse).ToArray() : new[] { 3, 6, 8, 12, 15 };
        if (args.Length > 4) ProxySlip = float.Parse(args[4], System.Globalization.CultureInfo.InvariantCulture);
        bool baselineOnly = args.Length > 5 && args[5] == "baseline";
        HashSet<string> only = (args.Length > 5 && args[5] != "baseline" && args[5] != "all") ? new HashSet<string>(args[5].Split(',')) : null;

        Console.WriteLine($"games/cell={games}  proxy Elo={proxyElo} slip={ProxySlip}  floors={string.Join(",", floors)}  (win% of the player proxy; draws and hand-outs count as losses)");
        Console.Write("relic".PadRight(16));
        foreach (int f in floors) Console.Write($"F{f}".PadLeft(7));
        Console.WriteLine("    avg   delta  +-95%");

        var sets = new List<(string name, HashSet<CaroRelicType> relics)> { ("(baseline)", new HashSet<CaroRelicType>()) };
        if (!baselineOnly) foreach (var r in Simulated.Where(x => only == null || only.Contains(x.ToString()))) sets.Add((r.ToString(), new HashSet<CaroRelicType> { r }));

        double baseAvg = 0;
        foreach (var (name, relics) in sets)
        {
            var rates = new double[floors.Length];
            for (int i = 0; i < floors.Length; i++)
            {
                int wins = 0;
                for (int g = 0; g < games; g++) if (PlayFloor(XORoguelikeCatalog.GetFloorBlind(floors[i]), relics, proxyElo, 7000 + g)) wins++;
                rates[i] = 100.0 * wins / games;
            }
            double avg = rates.Average();
            if (name == "(baseline)") baseAvg = avg;
            double n = games * floors.Length;
            double ci = 1.96 * Math.Sqrt(Math.Max(avg, 1) / 100.0 * (1 - Math.Min(avg, 99) / 100.0) / n) * 100.0;

            Console.Write(name.PadRight(16));
            foreach (double r in rates) Console.Write($"{r,7:F0}");
            Console.WriteLine($"  {avg,5:F1}  {avg - baseAvg,+6:F1}  {ci,5:F1}");
        }
        Console.WriteLine("not simulated (coins/UI only): " + string.Join(", ", NotSimulated));
    }

    /// <summary>Plays one floor. Returns true when the proxy (X) wins.</summary>
    private static bool PlayFloor(TowerFloorBlind blind, HashSet<CaroRelicType> relics, int proxyElo, int seed)
    {
        var e = new XOCaroEngine(blind.boardSize, 5);
        e.IsTowerClimb = true;
        e.RelicQuery = t => relics.Contains(t);
        e.Initialize(blind.boardSize, 5, spawnSpecialTiles: true, variant: blind.variantRule, specialTilesSeed: seed, floorNumber: blind.floorNumber, isTowerClimb: true);
        e.RequiredWinLines = blind.requiredWinLines;
        e.SetRandomSeed(seed);

        XOBotProfile bossProfile = XOBossKit.MakeProfile(blind);
        var proxyProfile = new XOBotProfile { eloRating = proxyElo, baseDifficulty = AIDifficulty.Boss };
        int limit = XORoguelikeRunState.ComputeMoveLimit(blind, relics.Contains(CaroRelicType.Hourglass));
        if (int.TryParse(Environment.GetEnvironmentVariable("XO_HANDS"), out int handsOverride)) limit = handsOverride;

        var st = new SimState();
        var shiftRng = new XORandomStream(seed + 99);
        var proxyRng = new XORandomStream(seed + 7);   // human-like noise: random opening + occasional slip, same per seed across relic sets
        int guard = 0;

        while (e.State == MatchState.Playing && guard++ < 400)
        {
            if (e.CurrentTurn == CellPiece.X)
            {
                var q = e.RelicQuery;
                e.RelicQuery = _ => false;                       // Crown of Omni must not weaken the player proxy
                XOCellPos m = e.CalculateAIMove(CellPiece.X, AIDifficulty.Boss, proxyProfile);
                e.RelicQuery = q;
                if (!m.IsValid) { Dbg(blind, seed, relics, "early-exit#1 state=" + e.State + " plies=" + e.MoveHistory.Count + " xMoves=" + st.xMoves); return false; }

                bool gravity = e.VariantRule == XOVariantRule.GravityDrop;
                if (st.xMoves == 0 && !gravity)
                {
                    int mid = e.BoardSize / 2;
                    var open = new XOCellPos(mid + proxyRng.Range(-2, 3), mid + proxyRng.Range(-2, 3));
                    if (e.CanPlace(open.x, open.y)) m = open;
                }
                else if (proxyRng.Range(0f, 1f) < ProxySlip)
                {
                    var cands = e.GetCandidateCells(2);
                    if (cands.Count > 0) m = cands[proxyRng.Range(0, cands.Count)];
                }

                PlaceResult res = e.TryPlace(m.x, m.y);
                if (!res.Placed) { Dbg(blind, seed, relics, "early-exit#2 state=" + e.State + " plies=" + e.MoveHistory.Count + " xMoves=" + st.xMoves); return false; }
                st.xMoves++;
                AfterPlacement(e, blind, relics, st, shiftRng, CellPiece.X, res.LandingCell);
                if (e.State == MatchState.Playing && e.CurrentTurn == CellPiece.O && st.xMoves >= limit) { Dbg(blind, seed, relics, "early-exit#3 state=" + e.State + " plies=" + e.MoveHistory.Count + " xMoves=" + st.xMoves); return false; } // out of hands
            }
            else
            {
                if (relics.Contains(CaroRelicType.GuardianAegis)) e.TryExecuteGuardianAegis(out _);
                bool freezeAfter = Environment.GetEnvironmentVariable("XO_FREEZE_AFTER") == "1";
                if (!freezeAfter) BossSkill(e, blind, st);

                XOCellPos m = e.CalculateAIMove(CellPiece.O, blind.difficulty, bossProfile);
                if (!m.IsValid) { Dbg(blind, seed, relics, "early-exit#4 state=" + e.State + " plies=" + e.MoveHistory.Count + " xMoves=" + st.xMoves); return false; }
                PlaceResult res = e.TryPlace(m.x, m.y);
                if (!res.Placed) { Dbg(blind, seed, relics, "early-exit#5 state=" + e.State + " plies=" + e.MoveHistory.Count + " xMoves=" + st.xMoves); return false; }
                AfterPlacement(e, blind, relics, st, shiftRng, CellPiece.O, res.LandingCell);

                if (freezeAfter && e.State == MatchState.Playing) BossSkill(e, blind, st);
                if (e.State == MatchState.Playing && XOBossKit.IsDoubleMoveDue(blind, e.MoveHistory.Count))
                {
                    XOCellPos extra = e.CalculateAIMove(CellPiece.O, blind.difficulty, bossProfile);
                    if (extra.IsValid)
                    {
                        e.ForceCurrentTurn(CellPiece.O);
                        PlaceResult r2 = e.TryPlace(extra.x, extra.y);
                        if (r2.Placed) AfterPlacement(e, blind, relics, st, shiftRng, CellPiece.O, r2.LandingCell);
                    }
                }
            }
        }
        if (Environment.GetEnvironmentVariable("XO_DBG") == "1") Console.Error.WriteLine($"  floor {blind.floorNumber} seed {seed} relics [{string.Join(",", relics)}] -> {e.State} plies={e.MoveHistory.Count} xMoves={st.xMoves}/{limit} guard={guard}");
        return e.State == MatchState.XWon;
    }

    private static void Dbg(TowerFloorBlind b, int seed, HashSet<CaroRelicType> r, string msg)
    {
        if (Environment.GetEnvironmentVariable("XO_DBG") == "1") Console.Error.WriteLine($"  floor {b.floorNumber} seed {seed} [{string.Join(",", r)}] {msg}");
    }

    private sealed class SimState
    {
        public int xMoves, skillUses, lastSkillPly;
        public bool shatterUsed, phoenixUsed, shiftDone;
    }

    private static void BossSkill(XOCaroEngine e, TowerFloorBlind b, SimState st)
    {
        if (!XOBossKit.CanUseSkill(b, st.skillUses, st.lastSkillPly, e.MoveHistory.Count)) return;
        bool used = false;
        if ((b.botEquipment == BotEquipmentType.FrostStaff || b.botEquipment == BotEquipmentType.OmniCrown)
            && e.TryExecuteBotCounterFreeze(new XOBotProfile { counterSkill = BotCounterSkill.CryoFreeze }, out _)) used = true;
        else if (b.botEquipment == BotEquipmentType.FlameBlade && e.TryExecuteBotFlameBlade(out _)) used = true;
        else if (b.botEquipment == BotEquipmentType.ThornArmor && e.TryExecuteBotThornArmor(out _)) used = true;
        if (used) { st.skillUses++; st.lastSkillPly = e.MoveHistory.Count; }
    }

    /// <summary>Mirror of HandleTowerPostMoveEvents plus the Phoenix Sigil check from HandleOpponentWin.</summary>
    private static void AfterPlacement(XOCaroEngine e, TowerFloorBlind b, HashSet<CaroRelicType> relics, SimState st, XORandomStream rng, CellPiece mover, XOCellPos cell)
    {
        if (e.State == MatchState.OWon && relics.Contains(CaroRelicType.VampireFang) && !st.phoenixUsed)
        {
            st.phoenixUsed = true;
            if (e.IncinerateLastWinningPiece(out _, freezeTurns: 2)) return; // run saved, match continues
        }

        if (mover == CellPiece.X && relics.Contains(CaroRelicType.ShatterHammer) && !st.shatterUsed)
            if (e.TryExecuteShatterHammer(cell.x, cell.y, out _)) st.shatterUsed = true;

        if (e.State != MatchState.Playing) return;
        int ply = e.MoveHistory.Count;

        if (b.hasMidMatchShift && !st.shiftDone && ply >= b.midMatchShiftTurn)
        {
            st.shiftDone = true;
            BossBlindType fx = b.midShiftEffect != BossBlindType.None ? b.midShiftEffect : b.blindType;
            if (fx == BossBlindType.TheArm) e.VariantRule = XOVariantRule.StrictCaro;
            else if (fx == BossBlindType.TwinDragon) e.RequiredWinLines = 2;
            else if (fx == BossBlindType.FogOfWar)
            {
                var cands = e.GetCandidateCells(2);
                for (int frozen = 0; cands.Count > 0 && frozen < 3;)
                {
                    int i = rng.Range(0, cands.Count);
                    XOCellPos p = cands[i];
                    cands.RemoveAt(i);
                    if (e.FreezeEmptyCell(p.x, p.y, 2)) frozen++;
                }
            }
            else if (fx == BossBlindType.CollapsingArena) e.CollapseNextPerimeterRing(out _);
            else if (fx == BossBlindType.StormColosseum) e.ExecuteCosmicShift(out _);
        }

        if (b.hasCollapsingPerimeter && ply > 0 && (ply == 18 || (b.blindType == BossBlindType.OverlordApex && ply % 10 == 0)))
            e.CollapseNextPerimeterRing(out _);
        if (b.hasCosmicShift && ply == 16) e.ExecuteCosmicShift(out _);
    }
}
