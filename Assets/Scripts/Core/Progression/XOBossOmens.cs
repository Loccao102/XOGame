using System.Collections.Generic;

namespace XOPlayable.Core
{
    /// <summary>
    /// Risk/reward mutators rolled per run and floor (deterministic from the run seed), in the spirit of Balatro boss blinds.
    /// Every omen is pure data on a cloned <see cref="TowerFloorBlind"/>, so the UI picks it up through GetActiveBlind() with no extra code.
    /// </summary>
    public enum BossOmen
    {
        None = 0,
        Elite,       // boss plays +200 Elo, +50% coins
        Relentless,  // boss skill: one more use, 2 boss moves faster cooldown
        Hasty,       // double moves come 2 plies sooner
        ShortHands,  // 6 fewer hands
        Wealthy      // +100% coins, boss +100 Elo
    }

    public static class XOBossOmens
    {
        public static string Name(BossOmen o)
        {
            switch (o)
            {
                case BossOmen.Elite: return "Elite";
                case BossOmen.Relentless: return "Relentless";
                case BossOmen.Hasty: return "Hasty";
                case BossOmen.ShortHands: return "Short hands";
                case BossOmen.Wealthy: return "Wealthy";
                default: return "";
            }
        }

        public static string Describe(BossOmen o)
        {
            switch (o)
            {
                case BossOmen.Elite: return "Boss plays +200 Elo. +50% coins.";
                case BossOmen.Relentless: return "Boss skill gets one more use and a shorter cooldown.";
                case BossOmen.Hasty: return "Boss double moves come sooner.";
                case BossOmen.ShortHands: return "You have 6 fewer hands.";
                case BossOmen.Wealthy: return "Boss plays +100 Elo. Double coins.";
                default: return "";
            }
        }

        /// <summary>Chance that a floor carries an omen: none on floors 1-2, then rising with depth.</summary>
        public static float Chance(int floor)
        {
            if (floor <= 2) return 0f;
            if (floor <= 5) return 0.35f;
            if (floor <= 10) return 0.55f;
            return 0.8f;
        }

        private static bool Applies(BossOmen o, TowerFloorBlind b)
        {
            switch (o)
            {
                case BossOmen.Relentless: return b.botEquipment != BotEquipmentType.None && b.botEquipment != BotEquipmentType.MirrorAegis;
                case BossOmen.Hasty: return b.botEquipment == BotEquipmentType.ThunderHammer || b.botEquipment == BotEquipmentType.OmniCrown;
                default: return true;
            }
        }

        public static BossOmen Roll(int runSeed, int floor, TowerFloorBlind blind)
        {
            if (blind == null) return BossOmen.None;
            var rng = new XORandomStream(unchecked(runSeed * 2749 + floor * 911 + 5));
            if (rng.Range(0f, 1f) >= Chance(floor)) return BossOmen.None;

            var pool = new List<BossOmen>();
            foreach (BossOmen o in new[] { BossOmen.Elite, BossOmen.Relentless, BossOmen.Hasty, BossOmen.ShortHands, BossOmen.Wealthy })
                if (Applies(o, blind)) pool.Add(o);
            return pool[rng.Range(0, pool.Count)];
        }

        /// <summary>Returns a modified copy; the catalog entry is never touched.</summary>
        public static TowerFloorBlind Apply(TowerFloorBlind src, BossOmen omen)
        {
            if (src == null || omen == BossOmen.None) return src;
            TowerFloorBlind b = src.Clone();

            switch (omen)
            {
                case BossOmen.Elite:
                    b.bossElo += 200;
                    b.coinReward = b.coinReward * 3 / 2;
                    break;
                case BossOmen.Relentless:
                    b.bossSkillMaxUses += 1;
                    b.bossSkillCooldown = XOMath.Max(0, b.bossSkillCooldown - 2);
                    break;
                case BossOmen.Hasty:
                    b.doubleMoveEvery = XOMath.Max(4, b.doubleMoveEvery - 2);
                    break;
                case BossOmen.ShortHands:
                    int baseLimit = b.maxPlayerMoves > 0 ? b.maxPlayerMoves : XORoguelikeCatalog.DefaultMoveLimit;
                    b.maxPlayerMoves = XOMath.Max(16, baseLimit - 6);
                    break;
                case BossOmen.Wealthy:
                    b.bossElo += 100;
                    b.coinReward *= 2;
                    break;
            }

            b.blindName = $"{src.blindName} | Omen: {Name(omen)}";
            b.blindDescription = $"{src.blindDescription}\nOMEN - {Name(omen)}: {Describe(omen)}";
            return b;
        }
    }
}
