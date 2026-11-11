namespace XOPlayable.Core
{
    /// <summary>Tower boss behaviour that the UI should not have to re-derive: skill cadence, double moves and the AI profile.</summary>
    public static class XOBossKit
    {
        /// <summary>
        /// True when the boss may fire its equipment skill now. The first use is free; later uses need
        /// <see cref="TowerFloorBlind.bossSkillCooldown"/> boss moves (two plies each) since the last one.
        /// </summary>
        public static bool CanUseSkill(TowerFloorBlind blind, int usesSoFar, int lastUsePly, int currentPly)
        {
            if (blind == null) return false;
            if (usesSoFar >= XOMath.Max(1, blind.bossSkillMaxUses)) return false;
            return usesSoFar == 0 || currentPly - lastUsePly >= blind.bossSkillCooldown * 2;
        }

        /// <summary>Thunder Hammer / Omni Crown bosses take a double move every <see cref="TowerFloorBlind.doubleMoveEvery"/> plies.</summary>
        public static bool IsDoubleMoveDue(TowerFloorBlind blind, int plyCount)
        {
            if (blind == null) return false;
            if (blind.botEquipment != BotEquipmentType.ThunderHammer && blind.botEquipment != BotEquipmentType.OmniCrown) return false;
            return plyCount > 0 && plyCount % XOMath.Max(2, blind.doubleMoveEvery) == 0;
        }

        /// <summary>
        /// AI profile for a boss so that <see cref="TowerFloorBlind.bossElo"/> actually drives strength
        /// (before this, floors 5-10 all played the same Hard AI regardless of Elo).
        /// </summary>
        public static XOBotProfile MakeProfile(TowerFloorBlind blind)
        {
            return new XOBotProfile
            {
                botName = blind.bossName,
                styleTitle = blind.bossTitle,
                eloRating = blind.bossElo,
                baseDifficulty = blind.difficulty,
                counterSkill = BotCounterSkill.None,
                mistakeChance = blind.difficulty == AIDifficulty.Easy ? 0.25f : 0f
            };
        }
    }
}
