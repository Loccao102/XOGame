namespace XOPlayable.Core
{
    public class CaroRelic
    {
        public CaroRelicType type;
        public string id;
        public string name;
        public string description;
        public CaroRelicRarity rarity;
        public int cost;
    }

    public class TowerFloorBlind
    {
        public TowerFloorBlind Clone() => (TowerFloorBlind)MemberwiseClone();

        public int floorNumber;
        public string floorTitle;
        public string mapName;
        public string mapThemeId;
        public XOColor mapAccentColor = XOColor.White;
        public string bossName;
        public string bossTitle;
        public int bossElo;
        public AIDifficulty difficulty;
        public BotEquipmentType botEquipment;
        public BossBlindType blindType;
        public string blindName;
        public string blindDescription;
        public int maxPlayerMoves; // 0 = unlimited
        public int boardSize;
        public int coinReward;
        public XOVariantRule variantRule;
        public int requiredWinLines = 1; // 1 = standard 5-in-a-row; 2 = Twin 5
        public bool hasCollapsingPerimeter = false;
        public bool hasCosmicShift = false;
        public bool hasMidMatchShift = false;
        public int midMatchShiftTurn = 0;
        public string midMatchShiftTitle = "";
        public string midMatchShiftDesc = "";

        // Boss kit tuning (see XOBossKit)
        public int bossSkillMaxUses = 1;        // how many times per match the boss may fire its equipment skill
        public int bossSkillCooldown = 0;       // boss moves between uses
        public int doubleMoveEvery = 8;         // plies between Thunder Hammer / Omni Crown double moves
        public BossBlindType midShiftEffect = BossBlindType.None; // overrides blindType when the mid-match shift fires
    }
}

