namespace XOPlayable.Core
{
    public static class XOProgressionCatalog
    {
        public static readonly RankInfo[] Ranks = new RankInfo[]
        {
            new RankInfo { title = "Bronze Novice",  minStars = 0,  maxStars = 3,   badgeColor = new XOColor(0.80f, 0.50f, 0.20f) },
            new RankInfo { title = "Silver Knight",  minStars = 3,  maxStars = 7,   badgeColor = new XOColor(0.75f, 0.80f, 0.88f) },
            new RankInfo { title = "Gold Master",    minStars = 7,  maxStars = 12,  badgeColor = new XOColor(0.98f, 0.75f, 0.15f) },
            new RankInfo { title = "Diamond Elite",  minStars = 12, maxStars = 20,  badgeColor = new XOColor(0.22f, 0.83f, 0.98f) },
            new RankInfo { title = "Grandmaster",    minStars = 20, maxStars = 999, badgeColor = new XOColor(0.96f, 0.25f, 0.55f) }
        };

        public static readonly XOMissionData[] Missions = new XOMissionData[]
        {
            new XOMissionData { id = 0, description = "Win a match within 15 moves",          rewardCoins = 25 },
            new XOMissionData { id = 1, description = "Win with a 5-in-a-row diagonal line",  rewardCoins = 30 },
            new XOMissionData { id = 2, description = "Use Bomb, Freeze or Swap and win",     rewardCoins = 25 },
            new XOMissionData { id = 3, description = "Reach a 2+ Win Streak",                rewardCoins = 35 },
            new XOMissionData { id = 4, description = "Clear a Tower Floor or 1300+ ELO Bot", rewardCoins = 40 }
        };

        /// <summary>6 distinct AI Rivals from Novice (ELO 550) to Grandmaster (ELO 1900).</summary>
        public static readonly XOBotProfile[] Bots = new XOBotProfile[]
        {
            new XOBotProfile
            {
                botIndex = 0,
                botName = "Rookie Leo",
                eloRating = 550,
                baseDifficulty = AIDifficulty.Easy,
                styleTitle = "Casual Beginner",
                counterSkill = BotCounterSkill.None,
                mistakeChance = 0.34f,
                greetingLines = new[] { "Hey! Go easy on me, I'm still learning!", "Let's have a fun 5-in-a-row match!" },
                onPlayerGoodMoveLines = new[] { "Whoa, I didn't see that line coming!", "Wait, you blocked me?!" },
                onBotThreatLines = new[] { "Aha! Look at my row!", "Wait, am I actually winning?!" },
                onPlayerSkillUsedLines = new[] { "Hey! Using power-ups is cheating haha!", "Whoa, nice gadget!" },
                onBotCounterSkillLines = new[] { "Watch my move!" }
            },
            new XOBotProfile
            {
                botIndex = 1,
                botName = "Scout Hannah",
                eloRating = 820,
                baseDifficulty = AIDifficulty.Easy,
                styleTitle = "Treasure Hunter",
                counterSkill = BotCounterSkill.TileHunter,
                mistakeChance = 0.16f,
                greetingLines = new[] { "Those Gold & Lightning tiles on the board are MINE!", "Race you to the special tiles!" },
                onPlayerGoodMoveLines = new[] { "Hey, I wanted that square!", "Sharp defense! You're pretty good." },
                onBotThreatLines = new[] { "Four in a row! Can you stop it?", "Too fast for you!" },
                onPlayerSkillUsedLines = new[] { "Ooh, flashy skill! My turn to strike back!", "Not fair, I liked that piece!" },
                onBotCounterSkillLines = new[] { "Snagged the bonus tile before you!" }
            },
            new XOBotProfile
            {
                botIndex = 2,
                botName = "Guardian Viktor",
                eloRating = 1120,
                baseDifficulty = AIDifficulty.Normal,
                styleTitle = "Iron Defender",
                counterSkill = BotCounterSkill.TileHunter,
                mistakeChance = 0.05f,
                greetingLines = new[] { "My defense is unbreakable. Show me your strategy.", "Every open-3 you build, I will shut down." },
                onPlayerGoodMoveLines = new[] { "Impressive tactical vision.", "A solid block. Let's see how you handle my next line." },
                onBotThreatLines = new[] { "Your defense has a gap! Check my line!", "Check! Block this or lose." },
                onPlayerSkillUsedLines = new[] { "Tactical weaponry? I will adapt.", "A clever disruption, but my formation holds." },
                onBotCounterSkillLines = new[] { "Securing key board territory!" }
            },
            new XOBotProfile
            {
                botIndex = 3,
                botName = "Frost Witch Lyra",
                eloRating = 1380,
                baseDifficulty = AIDifficulty.Normal,
                styleTitle = "Cryo Tactician (Can Freeze!)",
                counterSkill = BotCounterSkill.CryoFreeze,
                mistakeChance = 0.0f,
                greetingLines = new[] { "Careful—I can FREEZE squares on the board too!", "Let's see how you play when the board turns to ice!" },
                onPlayerGoodMoveLines = new[] { "Cold and calculated! Nice move!", "Hmph, you spotted my trap." },
                onBotThreatLines = new[] { "Winter closes in! Try blocking both ends!", "Four connected! Feel the chill?" },
                onPlayerSkillUsedLines = new[] { "Two can play the skill game!", "Bold move! Don't forget I have ice magic too." },
                onBotCounterSkillLines = new[] { "CRYO COUNTER! I just FROZE a key square!", "ICE LOCK! That square is frozen for 3 turns!" }
            },
            new XOBotProfile
            {
                botIndex = 4,
                botName = "Warlord Kael",
                eloRating = 1620,
                baseDifficulty = AIDifficulty.Hard,
                styleTitle = "Fork Master (Counter-Strike!)",
                counterSkill = BotCounterSkill.WarlordStrike,
                mistakeChance = 0.0f,
                greetingLines = new[] { "Only true tacticians survive my Double-Three forks!", "I hunt Lightning tiles and punish every mistake!" },
                onPlayerGoodMoveLines = new[] { "Worthy of a Warlord! Great read!", "You blocked my primary fork?!" },
                onBotThreatLines = new[] { "UNSTOPPABLE ATTACK! Block this if you can!", "Double threat! Which end will you save?" },
                onPlayerSkillUsedLines = new[] { "Smart use of power-ups! You'll need them!", "That only delays the inevitable!" },
                onBotCounterSkillLines = new[] { "WARLORD COUNTER! Locking down your attack lane!", "TACTICAL LOCKDOWN! Try getting past that!" }
            },
            new XOBotProfile
            {
                botIndex = 5,
                botName = "Grandmaster Zero",
                eloRating = 1900,
                baseDifficulty = AIDifficulty.Hard,
                styleTitle = "Supreme AI (Full Power)",
                counterSkill = BotCounterSkill.WarlordStrike,
                mistakeChance = 0.0f,
                greetingLines = new[] { "Calculating 10,000 board permutations. Make your move.", "Few humans have defeated me on a 15x15 grid." },
                onPlayerGoodMoveLines = new[] { "Optimal move detected. Recalculating...", "Fascinating. A 99th-percentile human move." },
                onBotThreatLines = new[] { "Win probability rising to 94%. Defend immediately.", "Lethal sequence initiated." },
                onPlayerSkillUsedLines = new[] { "External variable detected. Re-evaluating matrix.", "Clever anomaly. Adjusting strategy." },
                onBotCounterSkillLines = new[] { "PROTOCOL ZERO: Locking critical intersection!", "COUNTER-MEASURE DEPLOYED: Tile locked!" }
            }
        };

        public static readonly XOTowerFloor[] TowerFloors = new XOTowerFloor[]
        {
            new XOTowerFloor
            {
                floorNumber = 1, bossName = "Gate Sentry Vane", bossTitle = "Floor 1 Guardian", bossElo = 700,
                difficulty = AIDifficulty.Easy, equipment = BotEquipmentType.None, equipmentName = "None",
                equipmentDesc = "Standard defense, no active equipment.", rewardCoins = 25, rewardBonus = "+1 Hint", boardSize = 15
            },
            new XOTowerFloor
            {
                floorNumber = 2, bossName = "Shield Knight Boris", bossTitle = "Floor 2 Defender", bossElo = 900,
                difficulty = AIDifficulty.Easy, equipment = BotEquipmentType.MirrorAegis, equipmentName = "Mirror Aegis",
                equipmentDesc = "Immune to player Bomb and Swap skills!", rewardCoins = 30, rewardBonus = "+1 Undo", boardSize = 15
            },
            new XOTowerFloor
            {
                floorNumber = 3, bossName = "Frost Sorceress Lyra", bossTitle = "Floor 3 Cryomancer", bossElo = 1150,
                difficulty = AIDifficulty.Normal, equipment = BotEquipmentType.FrostStaff, equipmentName = "Frost Staff",
                equipmentDesc = "Freezes your dangerous open square for 3 turns when you build a 3-in-a-row!", rewardCoins = 40, rewardBonus = "+1 Freeze", boardSize = 15
            },
            new XOTowerFloor
            {
                floorNumber = 4, bossName = "Spike Gladiator Thorne", bossTitle = "Floor 4 Brawler", bossElo = 1300,
                difficulty = AIDifficulty.Normal, equipment = BotEquipmentType.ThornArmor, equipmentName = "Thorn Armor",
                equipmentDesc = "Locks an adjacent connecting tile with thorny barriers when threatened!", rewardCoins = 50, rewardBonus = "+1 Bomb", boardSize = 15
            },
            new XOTowerFloor
            {
                floorNumber = 5, bossName = "Thunder Warden Raiden", bossTitle = "Floor 5 Stormcaller", bossElo = 1500,
                difficulty = AIDifficulty.Hard, equipment = BotEquipmentType.ThunderHammer, equipmentName = "Thunder Hammer",
                equipmentDesc = "Every 5 turns, unleashes a lightning double-turn (places 2 pieces)!", rewardCoins = 65, rewardBonus = "+1 Swap", boardSize = 15
            },
            new XOTowerFloor
            {
                floorNumber = 6, bossName = "Flame Warmaster Ignis", bossTitle = "Floor 6 Pyromancer", bossElo = 1680,
                difficulty = AIDifficulty.Hard, equipment = BotEquipmentType.FlameBlade, equipmentName = "Flame Blade",
                equipmentDesc = "Detonates a tactical bomb destroying 1 of your key pieces once per match!", rewardCoins = 80, rewardBonus = "+1 Bomb", boardSize = 15
            },
            new XOTowerFloor
            {
                floorNumber = 7, bossName = "Frostbite Dualist Zephyr", bossTitle = "Floor 7 Ice Vanguard", bossElo = 1850,
                difficulty = AIDifficulty.Hard, equipment = BotEquipmentType.FrostStaff, equipmentName = "Glacial Staff",
                equipmentDesc = "Deep freeze locks open squares on your attack lines!", rewardCoins = 95, rewardBonus = "+1 Freeze", boardSize = 15
            },
            new XOTowerFloor
            {
                floorNumber = 8, bossName = "Ironclad Warlord Kael", bossTitle = "Floor 8 Iron Monarch", bossElo = 2000,
                difficulty = AIDifficulty.Hard, equipment = BotEquipmentType.MirrorAegis, equipmentName = "Reflective Bastion",
                equipmentDesc = "Total skill immunity: deflects Bomb and Swap back!", rewardCoins = 115, rewardBonus = "+1 Swap", boardSize = 15
            },
            new XOTowerFloor
            {
                floorNumber = 9, bossName = "Shadow Archon Malakor", bossTitle = "Floor 9 Void Master", bossElo = 2150,
                difficulty = AIDifficulty.Hard, equipment = BotEquipmentType.FlameBlade, equipmentName = "Infernal Blade",
                equipmentDesc = "Tactical bomb detonation + aggressive double-three counter attacks!", rewardCoins = 140, rewardBonus = "+1 All", boardSize = 15
            },
            new XOTowerFloor
            {
                floorNumber = 10, bossName = "Tower Overlord Zero", bossTitle = "Apex Floor 10 Boss", bossElo = 2300,
                difficulty = AIDifficulty.Hard, equipment = BotEquipmentType.OmniCrown, equipmentName = "Omni Crown",
                equipmentDesc = "Supreme Overlord relic: Cryo Freeze + Double Turn on critical turns!", rewardCoins = 250, rewardBonus = "Piece Skin Unlock!", boardSize = 15
            }
        };
    }
}

