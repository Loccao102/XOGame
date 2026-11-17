using System;

namespace XOPlayable.Core
{
    public enum RelicArchetype { None = 0, Attack, Defence, Economy, Terrain }

    /// <summary>
    /// Relic archetypes and their synergies. Everything takes a "has relic" predicate so the game (run state),
    /// the engine (RelicQuery) and the balance simulator all evaluate the same rules.
    /// Owning 3 relics of one archetype switches on that archetype's set bonus.
    /// </summary>
    public static class XORelicArchetypes
    {
        public const int SetSize = 3;
        public const int EmberMaxGrowth = 8;

        public static RelicArchetype Of(CaroRelicType t)
        {
            switch (t)
            {
                case CaroRelicType.TwinThreat:
                case CaroRelicType.Counterstrike:
                case CaroRelicType.OpeningGambit:
                case CaroRelicType.GlassCannon:
                case CaroRelicType.ShatterHammer:
                case CaroRelicType.DiagonalPrism:
                case CaroRelicType.Hourglass:
                case CaroRelicType.SecondWind:
                case CaroRelicType.WarDrum:
                case CaroRelicType.EmberBrand:
                    return RelicArchetype.Attack;

                case CaroRelicType.GuardianAegis:
                case CaroRelicType.VampireFang:
                case CaroRelicType.RedTalisman:
                case CaroRelicType.CrimsonWard:
                case CaroRelicType.ThawCharm:
                case CaroRelicType.CrownOfOmni:
                case CaroRelicType.ChronoPocket:
                case CaroRelicType.BulwarkIdol:
                    return RelicArchetype.Defence;

                case CaroRelicType.KnightStep:
                case CaroRelicType.AlchemistStone:
                case CaroRelicType.BountyHunter:
                case CaroRelicType.FortuneScroll:
                case CaroRelicType.Haggler:
                case CaroRelicType.CursedCoin:
                case CaroRelicType.MerchantCrown:
                    return RelicArchetype.Economy;

                case CaroRelicType.MinePlanter:
                case CaroRelicType.Telescope:
                case CaroRelicType.FateDice:
                case CaroRelicType.SiegeRam:
                case CaroRelicType.CartographerEye:
                    return RelicArchetype.Terrain;

                default:
                    return RelicArchetype.None;
            }
        }

        public static string Name(RelicArchetype a) => a == RelicArchetype.None ? "" : a.ToString();

        public static string SetBonusText(RelicArchetype a)
        {
            switch (a)
            {
                case RelicArchetype.Attack: return "+4 hands";
                case RelicArchetype.Defence: return "bosses play 100 Elo weaker";
                case RelicArchetype.Economy: return "+25% floor rewards";
                case RelicArchetype.Terrain: return "+2 gold tiles each match";
                default: return "";
            }
        }

        public static int Count(Func<CaroRelicType, bool> has, RelicArchetype a)
        {
            int n = 0;
            foreach (CaroRelicType t in Enum.GetValues(typeof(CaroRelicType)))
                if (Of(t) == a && has(t)) n++;
            return n;
        }

        public static bool HasSet(Func<CaroRelicType, bool> has, RelicArchetype a) => Count(has, a) >= SetSize;

        /// <summary>Extra hands: Attack set (+4), War Drum (+2 per Attack relic), Ember Brand (grows per floor).</summary>
        public static int HandsBonus(Func<CaroRelicType, bool> has, int emberGrowth)
        {
            int attack = Count(has, RelicArchetype.Attack);
            int bonus = attack >= SetSize ? 4 : 0;
            if (has(CaroRelicType.WarDrum)) bonus += 2 * attack;
            if (has(CaroRelicType.EmberBrand)) bonus += XOMath.Clamp(emberGrowth, 0, EmberMaxGrowth);
            return bonus;
        }

        /// <summary>Boss Elo change from relics: Defence set (-100), Bulwark Idol (-40 per Defence relic), Cursed Coin (+150).</summary>
        public static int BossEloDelta(Func<CaroRelicType, bool> has)
        {
            int defence = Count(has, RelicArchetype.Defence);
            int delta = defence >= SetSize ? -100 : 0;
            if (has(CaroRelicType.BulwarkIdol)) delta -= 40 * defence;
            if (has(CaroRelicType.CursedCoin)) delta += 150;
            return delta;
        }

        /// <summary>Floor reward change in percent: Economy set (+25), Merchant Crown (+10 per Economy relic), Cursed Coin (+50).</summary>
        public static int RewardPercent(Func<CaroRelicType, bool> has)
        {
            int economy = Count(has, RelicArchetype.Economy);
            int pct = economy >= SetSize ? 25 : 0;
            if (has(CaroRelicType.MerchantCrown)) pct += 10 * economy;
            if (has(CaroRelicType.CursedCoin)) pct += 50;
            return pct;
        }

        /// <summary>Gold tiles placed near the center at match start: Fate Dice (3), Cartographer's Eye (1 per Terrain relic), Terrain set (+2).</summary>
        public static int BonusGoldTiles(Func<CaroRelicType, bool> has)
        {
            int terrain = Count(has, RelicArchetype.Terrain);
            int n = terrain >= SetSize ? 2 : 0;
            if (has(CaroRelicType.FateDice)) n += 3;
            if (has(CaroRelicType.CartographerEye)) n += terrain;
            return n;
        }

        /// <summary>Applies the relic-driven Elo and reward changes to a copy of the blind (the input is never modified).</summary>
        public static TowerFloorBlind ModifyBlind(TowerFloorBlind blind, Func<CaroRelicType, bool> has)
        {
            if (blind == null) return null;
            int elo = BossEloDelta(has);
            int pct = RewardPercent(has);
            if (elo == 0 && pct == 0) return blind;

            TowerFloorBlind b = blind.Clone();
            b.bossElo = XOMath.Max(600, b.bossElo + elo);
            b.coinReward = b.coinReward * (100 + pct) / 100;
            if (has(CaroRelicType.CursedCoin)) b.blindDescription = blind.blindDescription + "\nCURSED COIN: boss +150 Elo, +50% coins.";
            return b;
        }
    }
}
