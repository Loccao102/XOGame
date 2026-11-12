using System.Diagnostics;
using XOPlayable.Core;

if (args.Length > 0 && args[0] == "relics") { RelicSim.Run(args); return; }

// Bot-vs-bot balance batches for the Tower.
//   dotnet run --project Tools/BalanceSim -c Release -- [games] [elo,elo,...]
// "Player proxy" = the existing Hard heuristic AI (a competent but non-searching opponent).
// Reports, per boss Elo, how often the boss beats that proxy and how long each boss move takes.

if (Environment.GetEnvironmentVariable("XO_LOG") == "1") XOBossAI.DebugLog = Console.WriteLine;
if (float.TryParse(Environment.GetEnvironmentVariable("XO_ATK"), out float atkW)) XOBossAI.AttackWeight = atkW;
if (float.TryParse(Environment.GetEnvironmentVariable("XO_DEF"), out float defW)) XOBossAI.DefenseWeight = defW;
int games = args.Length > 0 ? int.Parse(args[0]) : 20;
int[] elos = args.Length > 1
    ? args[1].Split(',').Select(int.Parse).ToArray()
    : new[] { 1250, 1550, 1950, 2200, 2500 };

Console.WriteLine($"games per cell: {games} (boss plays O = second mover, then X = first mover)");
Console.WriteLine("boss_elo  boss_as  boss_win%  proxy_win%  draw%  avg_plies  boss_ms_avg  boss_ms_max");

foreach (int elo in elos)
{
    foreach (CellPiece bossSide in new[] { CellPiece.O, CellPiece.X })
    {
        int bossWins = 0, proxyWins = 0, draws = 0, plies = 0, bossMoves = 0;
        double ms = 0, msMax = 0;

        for (int g = 0; g < games; g++)
        {
            var e = new XOCaroEngine(15, 5);
            e.Initialize(15, 5, spawnSpecialTiles: false, variant: XOVariantRule.ClassicCaro);
            e.SetRandomSeed(1000 + g);
            var profile = new XOBotProfile { eloRating = elo, baseDifficulty = AIDifficulty.Boss, mistakeChance = 0f };

            int guard = 0;
            while (e.State == MatchState.Playing && guard++ < 225)
            {
                CellPiece turn = e.CurrentTurn;
                bool isBoss = turn == bossSide;
                var sw = Stopwatch.StartNew();
                XOCellPos m = isBoss
                    ? e.CalculateAIMove(turn, AIDifficulty.Boss, profile)
                    : e.CalculateAIMove(turn, AIDifficulty.Hard);
                sw.Stop();
                if (isBoss) { ms += sw.Elapsed.TotalMilliseconds; msMax = Math.Max(msMax, sw.Elapsed.TotalMilliseconds); bossMoves++; }
                if (!m.IsValid || !e.TryPlace(m.x, m.y).Placed) break;
            }

            if (Environment.GetEnvironmentVariable("XO_DUMP") == "1" && g == 0)
            {
                Console.WriteLine($"--- elo {elo} boss={bossSide} result={e.State} plies={e.MoveHistory.Count}");
                for (int r = 0; r < e.BoardSize; r++)
                {
                    var sb = new System.Text.StringBuilder();
                    for (int c = 0; c < e.BoardSize; c++) sb.Append(e.Board[r, c] == CellPiece.X ? 'X' : e.Board[r, c] == CellPiece.O ? 'O' : '.');
                    Console.WriteLine(sb.ToString());
                }
            }
            plies += e.MoveHistory.Count;
            if (e.State == MatchState.Draw || e.State == MatchState.Playing) draws++;
            else if ((e.State == MatchState.XWon && bossSide == CellPiece.X) || (e.State == MatchState.OWon && bossSide == CellPiece.O)) bossWins++;
            else proxyWins++;
        }

        double n = games;
        Console.WriteLine($"{elo,8}  {bossSide,7}  {100 * bossWins / n,9:F0}  {100 * proxyWins / n,10:F0}  {100 * draws / n,5:F0}  {plies / n,9:F1}  {ms / Math.Max(1, bossMoves),11:F1}  {msMax,11:F1}");
    }
}

