using UnityEngine;
using XOPlayable.Core;

namespace XOPlayable.Visuals
{
    /// <summary>
    /// Presentation lookups for relics. They live here, not on <see cref="CaroRelic"/>, so the rules in
    /// <c>Core</c> never reference sprites or procedural art.
    /// </summary>
    public static class XORelicVisuals
    {
        public static Color GetRarityColor(this CaroRelic relic)
        {
            return relic.rarity switch
            {
                CaroRelicRarity.Common => XOProceduralAssets.ColMuted,
                CaroRelicRarity.Uncommon => XOProceduralAssets.ColSky,
                CaroRelicRarity.Rare => XOProceduralAssets.ColLilac,
                CaroRelicRarity.Legendary => XOProceduralAssets.ColSun,
                _ => Color.white
            };
        }

        public static Sprite GetIcon(this CaroRelic relic)
        {
            return relic.type switch
            {
                CaroRelicType.DiagonalPrism  => XOProceduralAssets.IconRelic,
                CaroRelicType.TwinThreat     => XOProceduralAssets.IconSwords,
                CaroRelicType.KnightStep     => XOProceduralAssets.IconTrophy,
                CaroRelicType.GuardianAegis  => XOProceduralAssets.IconShield,
                CaroRelicType.AlchemistStone => XOProceduralAssets.IconCoin,
                CaroRelicType.MinePlanter    => XOProceduralAssets.IconBomb,
                CaroRelicType.ChronoPocket   => XOProceduralAssets.IconUndo,
                CaroRelicType.BountyHunter   => XOProceduralAssets.IconChest,
                CaroRelicType.ShatterHammer  => XOProceduralAssets.IconRock,
                CaroRelicType.Telescope      => XOProceduralAssets.IconHint,
                CaroRelicType.VampireFang    => XOProceduralAssets.IconHeart,
                CaroRelicType.CrownOfOmni    => XOProceduralAssets.IconCrown,
                CaroRelicType.ThawCharm => XOProceduralAssets.IconFreeze,
                CaroRelicType.Counterstrike => XOProceduralAssets.IconLightning,
                CaroRelicType.Hourglass => XOProceduralAssets.IconUndo,
                _ => XOProceduralAssets.IconRelic
            };
        }
    }
}
