using System;
using UnityEngine;
using UnityEngine.UI;
using XOPlayable.Ads;
using XOPlayable.Core;
using XOPlayable.Visuals;

namespace XOPlayable.UI
{
    /// <summary>
    /// Builds and refreshes the Lobby screen (Top Header Bar, Rank &amp; Quest Hero Card,
    /// Tower of Champions banner, Quick-Access Hub buttons, and Play Now CTA).
    /// </summary>
    public sealed class XOLobbyPresenter
    {
        private readonly Font _font;
        private readonly XOGameStateStore _store;
        private readonly XOAudioManager _audio;

        public GameObject RootObj { get; private set; }
        public RectTransform RootRT { get; private set; }

        public Image HeaderCardImg { get; private set; }
        public Image QuestCardImg { get; private set; }
        public Image HeroCardImg { get; private set; }
        public Image ModeCardImg { get; private set; }
        public Text MissionText { get; private set; }

        private RectTransform _sharedHeaderRT;
        private Text _coinsText;
        private Text _rankTitleText;
        private Text _rankStarsText;
        private Image _rankBadgeImg;
        private Text _streakBadgeText;
        private Text _chestBadgeText;
        private Image _chestFillImg;
        private Button _soundBtn;
        private Image _soundIconImg;
        private Button _installBtn;

        private RectTransform _heroCardRT;
        private RectTransform _questCardRT;
        private RectTransform _hubGridRT;
        private RectTransform _playCtaRT;

        private Text _lobbyModeSummaryText;
        private Text _lobbyRivalSummaryText;
        private Text _lobbyTowerStatusText;
        private Text _playCtaBtnText;
        private Button _modePillBtn;
        private Button _rivalPillBtn;

        private readonly Action _onPlayNow;
        private readonly Action _onOpenModeSelect;
        private readonly Action _onOpenTower;
        private readonly Action _onOpenShop;
        private readonly Action _onOpenLeaderboard;
        private readonly Action _onOpenLuckyWheel;
        private readonly Action _onChestClicked;
        private readonly Action _onFreeCoinsClicked;

        public XOLobbyPresenter(
            Font font,
            XOGameStateStore store,
            XOAudioManager audio,
            Action onPlayNow,
            Action onOpenModeSelect,
            Action onOpenTower,
            Action onOpenShop,
            Action onOpenLeaderboard,
            Action onOpenLuckyWheel,
            Action onChestClicked,
            Action onFreeCoinsClicked)
        {
            _font = font;
            _store = store;
            _audio = audio;
            _onPlayNow = onPlayNow;
            _onOpenModeSelect = onOpenModeSelect;
            _onOpenTower = onOpenTower;
            _onOpenShop = onOpenShop;
            _onOpenLeaderboard = onOpenLeaderboard;
            _onOpenLuckyWheel = onOpenLuckyWheel;
            _onChestClicked = onChestClicked;
            _onFreeCoinsClicked = onFreeCoinsClicked;
        }

        /// <summary>Creates the shared top header bar anchored at the top of the canvas.</summary>
        public RectTransform BuildSharedHeader(Transform canvasTransform, bool showStoreInstallButton, Action onHomeClicked)
        {
            GameObject headerObj = XOUIBuilder.CreateUIObject("SharedHeaderBar", canvasTransform);
            _sharedHeaderRT = headerObj.GetComponent<RectTransform>();
            _sharedHeaderRT.SetAnchor(new Vector2(0.5f, 1f), new Vector2(0.5f, 1f));
            _sharedHeaderRT.sizeDelta = new Vector2(1020f, 92f);

            HeaderCardImg = headerObj.AddComponent<Image>();
            HeaderCardImg.sprite = XOProceduralAssets.CardPanelSprite;
            HeaderCardImg.type = Image.Type.Sliced;
            HeaderCardImg.color = _store.ActiveTheme.headerCardColor.ToUnityColor();

            // Left: Rank Badge & Stars
            GameObject rankPill = XOUIBuilder.CreateUIObject("RankPill", _sharedHeaderRT);
            RectTransform rankRT = rankPill.GetComponent<RectTransform>();
            rankRT.SetAnchor(new Vector2(0f, 0.5f), new Vector2(0f, 0.5f));
            rankRT.anchoredPosition = new Vector2(18f, 0f);
            rankRT.sizeDelta = new Vector2(270f, 58f);

            _rankBadgeImg = rankPill.AddComponent<Image>();
            _rankBadgeImg.sprite = XOProceduralAssets.PillSprite;
            _rankBadgeImg.type = Image.Type.Sliced;
            _rankBadgeImg.color = XOProceduralAssets.ColTileEdge;

            XOUIBuilder.CreateIconImage(rankRT, XOProceduralAssets.IconTrophy, new Vector2(0f, 0.5f), new Vector2(26f, 0f), new Vector2(34f, 34f));
            _rankTitleText = XOUIBuilder.CreateText(rankRT, _font, "Bronze Novice", 17, FontStyle.Bold, TextAnchor.UpperLeft);
            _rankTitleText.rectTransform.offsetMin = new Vector2(52f, 6f);
            _rankTitleText.rectTransform.offsetMax = new Vector2(-10f, -6f);

            _rankStarsText = XOUIBuilder.CreateText(rankRT, _font, "1 Star  •  ELO 660", 13, FontStyle.Bold, TextAnchor.LowerLeft);
            _rankStarsText.color = XOProceduralAssets.ColMuted;
            _rankStarsText.rectTransform.offsetMin = new Vector2(52f, 6f);
            _rankStarsText.rectTransform.offsetMax = new Vector2(-10f, -6f);

            // Streak Badge Pill
            GameObject streakObj = XOUIBuilder.CreateUIObject("StreakPill", _sharedHeaderRT);
            RectTransform stRT = streakObj.GetComponent<RectTransform>();
            stRT.SetAnchor(new Vector2(0f, 0.5f), new Vector2(0f, 0.5f));
            stRT.anchoredPosition = new Vector2(300f, 0f);
            stRT.sizeDelta = new Vector2(138f, 52f);
            Image stBg = streakObj.AddComponent<Image>();
            stBg.sprite = XOProceduralAssets.PillSprite;
            stBg.type = Image.Type.Sliced;
            stBg.color = XOUIColors.Amber100;
            XOUIBuilder.CreateIconImage(stRT, XOProceduralAssets.IconFlame, new Vector2(0f, 0.5f), new Vector2(24f, 0f), new Vector2(28f, 28f));
            _streakBadgeText = XOUIBuilder.CreateText(stRT, _font, "0 Streak", 15, FontStyle.Bold, TextAnchor.MiddleCenter);
            _streakBadgeText.rectTransform.offsetMin = new Vector2(36f, 0f);
            _streakBadgeText.color = XOUIColors.Amber700;

            // Coins Button Pill (Tap to watch rewarded ad for +100 Coins!)
            Button coinsBtn = XOUIBuilder.CreateIcon3DButton(_sharedHeaderRT, _font, XOProceduralAssets.IconCoin, "150", new Vector2(168f, 54f), XOUIColors.Yellow100, XOProceduralAssets.ColInk, 20);
            RectTransform cRT = coinsBtn.GetComponent<RectTransform>();
            cRT.SetAnchor(new Vector2(1f, 0.5f), new Vector2(1f, 0.5f));
            cRT.anchoredPosition = new Vector2(-290f, 0f);
            _coinsText = coinsBtn.GetComponentInChildren<Text>();
            coinsBtn.onClick.AddListener(() => _onFreeCoinsClicked?.Invoke());

            // Chest Progress Button (x/3)
            Button chestBtn = XOUIBuilder.CreateIcon3DButton(_sharedHeaderRT, _font, XOProceduralAssets.IconChest, "0/3", new Vector2(118f, 54f), XOProceduralAssets.ColLilac, Color.white, 17);
            RectTransform chRT = chestBtn.GetComponent<RectTransform>();
            chRT.SetAnchor(new Vector2(1f, 0.5f), new Vector2(1f, 0.5f));
            chRT.anchoredPosition = new Vector2(-162f, 0f);
            _chestBadgeText = chestBtn.GetComponentInChildren<Text>();
            _chestFillImg = chestBtn.GetComponent<Image>();
            chestBtn.onClick.AddListener(() => _onChestClicked?.Invoke());

            // Sound Toggle Button
            _soundBtn = XOUIBuilder.Create3DButton(_sharedHeaderRT, _font, "", new Vector2(56f, 54f), XOProceduralAssets.ColTileEdge, XOProceduralAssets.ColInk, 16);
            RectTransform sndRT = _soundBtn.GetComponent<RectTransform>();
            sndRT.SetAnchor(new Vector2(1f, 0.5f), new Vector2(1f, 0.5f));
            sndRT.anchoredPosition = new Vector2(-94f, 0f);
            _soundIconImg = XOUIBuilder.CreateIconImage(sndRT, XOProceduralAssets.IconSoundOn, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(32f, 32f));
            _soundBtn.onClick.AddListener(() =>
            {
                _audio.ToggleMute();
                RefreshHeaderSoundIcon();
            });

            // Optional Store Install / Home Button
            if (showStoreInstallButton)
            {
                _installBtn = XOUIBuilder.Create3DButton(_sharedHeaderRT, _font, "GET", new Vector2(72f, 54f), XOProceduralAssets.ColMint, XOProceduralAssets.ColInk, 16);
                RectTransform instRT = _installBtn.GetComponent<RectTransform>();
                instRT.SetAnchor(new Vector2(1f, 0.5f), new Vector2(1f, 0.5f));
                instRT.anchoredPosition = new Vector2(-14f, 0f);
                _installBtn.onClick.AddListener(() => XOPlayableAdsBridge.Instance?.TriggerInstallCTA());
            }

            return _sharedHeaderRT;
        }

        /// <summary>Builds the full Lobby screen hierarchy.</summary>
        public void Build(Transform canvasTransform)
        {
            RootObj = XOUIBuilder.CreateUIObject("LobbyScreenRoot", canvasTransform);
            RootRT = RootObj.GetComponent<RectTransform>();

            BuildHeroCard();
            BuildQuestCard();
            BuildHubActionsGrid();
            BuildPlayCtaSection();
        }

        private void BuildHeroCard()
        {
            GameObject cardObj = XOUIBuilder.CreateUIObject("LobbyHeroCard", RootRT);
            _heroCardRT = cardObj.GetComponent<RectTransform>();
            _heroCardRT.SetAnchor(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));

            HeroCardImg = cardObj.AddComponent<Image>();
            HeroCardImg.sprite = XOProceduralAssets.CardPanelSprite;
            HeroCardImg.type = Image.Type.Sliced;
            HeroCardImg.color = _store.ActiveTheme.panelSurfaceColor.ToUnityColor();

            XOUIBuilder.CreateIconImage(_heroCardRT, XOProceduralAssets.XPieceSprite, new Vector2(0.5f, 1f), new Vector2(-210f, -54f), new Vector2(54f, 54f));
            XOUIBuilder.CreateIconImage(_heroCardRT, XOProceduralAssets.OPieceSprite, new Vector2(0.5f, 1f), new Vector2(210f, -54f), new Vector2(54f, 54f));

            Text title = XOUIBuilder.CreateText(_heroCardRT, _font, "XO CARO ARENA", 38, FontStyle.Bold, TextAnchor.UpperCenter);
            title.rectTransform.offsetMin = new Vector2(24f, 0f);
            title.rectTransform.offsetMax = new Vector2(-24f, -24f);

            Text sub = XOUIBuilder.CreateText(_heroCardRT, _font, "Tactical 5-in-a-Row • 7 Rules • 10 Tower Bosses", 17, FontStyle.Bold, TextAnchor.UpperCenter);
            sub.color = XOProceduralAssets.ColMuted;
            sub.rectTransform.offsetMin = new Vector2(24f, 0f);
            sub.rectTransform.offsetMax = new Vector2(-24f, -72f);

            // Active Variant & Mode Selector Pill
            _modePillBtn = XOUIBuilder.CreateIcon3DButton(_heroCardRT, _font, XOProceduralAssets.IconNewGame, "Classic Caro 15x15  •  Vs AI", new Vector2(460f, 58f), XOProceduralAssets.ColTileEdge, XOProceduralAssets.ColInk, 18);
            RectTransform mpRT = _modePillBtn.GetComponent<RectTransform>();
            mpRT.SetAnchor(new Vector2(0.5f, 0.5f));
            mpRT.anchoredPosition = new Vector2(0f, -12f);
            _lobbyModeSummaryText = _modePillBtn.GetComponentInChildren<Text>();
            _modePillBtn.onClick.AddListener(() => _onOpenModeSelect?.Invoke());
            ModeCardImg = _modePillBtn.GetComponent<Image>();

            // Active Rival Pill
            _rivalPillBtn = XOUIBuilder.CreateIcon3DButton(_heroCardRT, _font, XOProceduralAssets.IconSwords, "Rival: Guardian Viktor (1120)", new Vector2(460f, 52f), XOUIColors.Indigo50, XOProceduralAssets.ColInk, 16);
            RectTransform rpRT = _rivalPillBtn.GetComponent<RectTransform>();
            rpRT.SetAnchor(new Vector2(0.5f, 0.5f));
            rpRT.anchoredPosition = new Vector2(0f, -78f);
            _lobbyRivalSummaryText = _rivalPillBtn.GetComponentInChildren<Text>();
            _rivalPillBtn.onClick.AddListener(() => _onOpenLeaderboard?.Invoke());

            // Tower Quick Status Pill
            _lobbyTowerStatusText = XOUIBuilder.CreateText(_heroCardRT, _font, "Tower Progress: Floor 1 Ready", 15, FontStyle.Bold, TextAnchor.LowerCenter);
            _lobbyTowerStatusText.color = XOUIColors.Sky600;
            _lobbyTowerStatusText.rectTransform.offsetMin = new Vector2(20f, 16f);
            _lobbyTowerStatusText.rectTransform.offsetMax = new Vector2(-20f, 0f);
        }

        private void BuildQuestCard()
        {
            GameObject questObj = XOUIBuilder.CreateUIObject("LobbyQuestCard", RootRT);
            _questCardRT = questObj.GetComponent<RectTransform>();
            _questCardRT.SetAnchor(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));

            QuestCardImg = questObj.AddComponent<Image>();
            QuestCardImg.sprite = XOProceduralAssets.CardPanelSprite;
            QuestCardImg.type = Image.Type.Sliced;
            QuestCardImg.color = _store.ActiveTheme.panelSurfaceColor.ToUnityColor();

            XOUIBuilder.CreateIconImage(_questCardRT, XOProceduralAssets.IconHint, new Vector2(0f, 0.5f), new Vector2(42f, 0f), new Vector2(40f, 40f));

            Text qHeader = XOUIBuilder.CreateText(_questCardRT, _font, "ACTIVE DAILY MISSION", 14, FontStyle.Bold, TextAnchor.UpperLeft);
            qHeader.color = XOUIColors.Amber600;
            qHeader.rectTransform.offsetMin = new Vector2(76f, 0f);
            qHeader.rectTransform.offsetMax = new Vector2(-18f, -12f);

            MissionText = XOUIBuilder.CreateText(_questCardRT, _font, "Win a match within 15 moves (+25 Coins)", 17, FontStyle.Bold, TextAnchor.LowerLeft);
            MissionText.rectTransform.offsetMin = new Vector2(76f, 12f);
            MissionText.rectTransform.offsetMax = new Vector2(-18f, 0f);
        }

        private void BuildHubActionsGrid()
        {
            GameObject gridObj = XOUIBuilder.CreateUIObject("LobbyHubGrid", RootRT);
            _hubGridRT = gridObj.GetComponent<RectTransform>();
            _hubGridRT.SetAnchor(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));

            GridLayoutGroup glg = gridObj.AddComponent<GridLayoutGroup>();
            glg.cellSize = new Vector2(246f, 84f);
            glg.spacing = new Vector2(16f, 16f);
            glg.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            glg.constraintCount = 2;
            glg.childAlignment = TextAnchor.MiddleCenter;

            XOUIBuilder.CreateIcon3DButton(_hubGridRT, _font, XOProceduralAssets.IconCrown, "TOWER CLIMB", new Vector2(246f, 84f), XOProceduralAssets.ColLilac, Color.white, 19)
                .onClick.AddListener(() => _onOpenTower?.Invoke());

            XOUIBuilder.CreateIcon3DButton(_hubGridRT, _font, XOProceduralAssets.IconSkinShop, "THEME SHOP", new Vector2(246f, 84f), XOProceduralAssets.ColSky, Color.white, 19)
                .onClick.AddListener(() => _onOpenShop?.Invoke());

            XOUIBuilder.CreateIcon3DButton(_hubGridRT, _font, XOProceduralAssets.IconLeaderboard, "ARENA RIVALS", new Vector2(246f, 84f), XOProceduralAssets.ColPeach, XOProceduralAssets.ColInk, 19)
                .onClick.AddListener(() => _onOpenLeaderboard?.Invoke());

            XOUIBuilder.CreateIcon3DButton(_hubGridRT, _font, XOProceduralAssets.IconLuckyWheel, "LUCKY WHEEL", new Vector2(246f, 84f), XOProceduralAssets.ColSun, XOProceduralAssets.ColInk, 19)
                .onClick.AddListener(() => _onOpenLuckyWheel?.Invoke());
        }

        private void BuildPlayCtaSection()
        {
            Button playBtn = XOUIBuilder.CreateIcon3DButton(RootRT, _font, XOProceduralAssets.IconNewGame, "PLAY MATCH NOW", new Vector2(510f, 88f), XOProceduralAssets.ColMint, XOProceduralAssets.ColInk, 26);
            _playCtaRT = playBtn.GetComponent<RectTransform>();
            _playCtaRT.SetAnchor(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
            _playCtaBtnText = playBtn.GetComponentInChildren<Text>();
            playBtn.onClick.AddListener(() => _onPlayNow?.Invoke());
        }

        /// <summary>Updates header stats, rank badge, coins, sound icon, quest label, and lobby summary cards.</summary>
        public void Refresh(GameMode mode, XOVariantRule variant, int boardSize, AIDifficulty difficulty)
        {
            RankInfo rank = XOProgressionSystem.GetCurrentRank(out int _, out int _);
            if (_rankTitleText != null) _rankTitleText.text = rank.title;
            if (_rankStarsText != null) _rankStarsText.text = $"{XOProgressionSystem.TotalStars} Stars  •  ELO {XOProgressionSystem.PlayerELO}";
            if (_rankBadgeImg != null) _rankBadgeImg.color = Color.Lerp(rank.badgeColor.ToUnityColor(), Color.white, 0.65f);
            if (_streakBadgeText != null) _streakBadgeText.text = $"{XOProgressionSystem.WinStreak} Streak";
            if (_coinsText != null) _coinsText.text = _store.Coins.ToString();

            int chest = XOProgressionSystem.ChestProgress;
            if (_chestBadgeText != null) _chestBadgeText.text = (chest >= 3) ? "OPEN!" : $"{chest}/3";
            if (_chestFillImg != null) _chestFillImg.color = (chest >= 3) ? XOProceduralAssets.ColSun : XOProceduralAssets.ColLilac;

            RefreshHeaderSoundIcon();

            XOMissionData mission = XOProgressionSystem.GetActiveMission();
            if (MissionText != null && mission != null)
            {
                MissionText.text = $"{mission.description}  (+{mission.rewardCoins}c)";
                MissionText.color = _store.ActiveTheme.primaryTextColor.ToUnityColor();
            }

            if (_lobbyModeSummaryText != null)
            {
                string modeLabel = mode switch
                {
                    GameMode.VsAI => "Vs AI",
                    GameMode.TwoPlayerLocal => "2-Player Local",
                    GameMode.PuzzleStage => $"Puzzle #{XOProgressionSystem.CurrentPuzzleStage}",
                    GameMode.TowerClimb => $"Tower Floor {XORoguelikeRunManager.CurrentFloor}",
                    _ => "Vs AI"
                };
                string ruleLabel = FormatVariantName(variant, boardSize);
                _lobbyModeSummaryText.text = $"{ruleLabel}  •  {modeLabel}";
            }

            if (_lobbyRivalSummaryText != null)
            {
                if (mode == GameMode.VsAI)
                {
                    XOBotProfile bot = XOProgressionSystem.GetActiveBot();
                    _lobbyRivalSummaryText.text = $"Rival: {bot.botName} (ELO {bot.eloRating})";
                }
                else if (mode == GameMode.TowerClimb)
                {
                    TowerFloorBlind blind = XORoguelikeRunManager.GetActiveBlind();
                    _lobbyRivalSummaryText.text = $"Floor {blind.floorNumber}: {blind.bossName} ({blind.blindName})";
                }
                else
                {
                    _lobbyRivalSummaryText.text = (mode == GameMode.PuzzleStage) ? "Solve Tactical Win-in-N" : $"Local Duel • Difficulty: {difficulty}";
                }
            }

            if (_lobbyTowerStatusText != null)
            {
                _lobbyTowerStatusText.text = XORoguelikeRunManager.IsRunActive
                    ? $"Active Tower Run: Floor {XORoguelikeRunManager.CurrentFloor}/{XORoguelikeCatalog.FloorCount}  •  {XORoguelikeRunManager.Lives} Hearts  •  {XORoguelikeRunManager.EquippedRelics.Count} Relics"
                    : $"Tower Cleared: {XOProgressionSystem.HighestTowerFloorCleared}/{XORoguelikeCatalog.FloorCount} Floors  •  Tap TOWER CLIMB to start!";
            }

            if (_playCtaBtnText != null)
            {
                _playCtaBtnText.text = (mode == GameMode.TowerClimb)
                    ? $"BATTLE FLOOR {XORoguelikeRunManager.CurrentFloor}"
                    : "PLAY MATCH NOW";
            }
        }

        public void RefreshHeaderSoundIcon()
        {
            if (_soundIconImg == null) return;
            _soundIconImg.sprite = _audio.IsMuted ? XOProceduralAssets.IconSoundOff : XOProceduralAssets.IconSoundOn;
        }

        /// <summary>Applies Pastel Clay theme colors to Lobby backgrounds and surfaces.</summary>
        public void ApplyTheme(XOThemePalette theme)
        {
            if (HeaderCardImg != null) HeaderCardImg.color = theme.headerCardColor.ToUnityColor();
            if (HeroCardImg != null) HeroCardImg.color = theme.panelSurfaceColor.ToUnityColor();
            if (QuestCardImg != null) QuestCardImg.color = theme.panelSurfaceColor.ToUnityColor();
            if (MissionText != null) MissionText.color = theme.primaryTextColor.ToUnityColor();
        }

        /// <summary>Positions Lobby UI cards responsively across Landscape and Portrait viewports.</summary>
        public void ApplyResponsiveLayout(float refW, float refH, bool isLandscape, float safeLeft = 0f, float safeRight = 0f, float safeTop = 0f, float safeBottom = 0f)
        {
            if (RootObj == null) return;
            float safeAreaW = Mathf.Max(320f, refW - safeLeft - safeRight);
            float safeCenterX = (safeLeft - safeRight) * 0.5f;
            float safeYOffset = Mathf.Clamp((safeBottom - safeTop) * 0.5f, -90f, 90f);

            if (isLandscape)
            {
                float colOffset = Mathf.Min(360f, safeAreaW * 0.22f);
                float colW = Mathf.Min(560f, Mathf.Max(360f, safeAreaW * 0.44f));
                float rightW = Mathf.Min(520f, colW);
                if (_heroCardRT != null) { _heroCardRT.anchoredPosition = new Vector2(safeCenterX - colOffset, 10f + safeYOffset); _heroCardRT.sizeDelta = new Vector2(colW, 360f); }
                if (_questCardRT != null) { _questCardRT.anchoredPosition = new Vector2(safeCenterX - colOffset, -235f + safeYOffset); _questCardRT.sizeDelta = new Vector2(colW, 92f); }
                if (_hubGridRT != null) { _hubGridRT.anchoredPosition = new Vector2(safeCenterX + colOffset, 65f + safeYOffset); _hubGridRT.sizeDelta = new Vector2(rightW, 190f); }
                if (_playCtaRT != null) { _playCtaRT.anchoredPosition = new Vector2(safeCenterX + colOffset, -135f + safeYOffset); _playCtaRT.sizeDelta = new Vector2(rightW - 10f, 92f); }
            }
            else
            {
                float cardW = Mathf.Min(940f, safeAreaW - 72f);
                if (_heroCardRT != null) { _heroCardRT.anchoredPosition = new Vector2(safeCenterX, 250f + safeYOffset); _heroCardRT.sizeDelta = new Vector2(cardW, 330f); }
                if (_questCardRT != null) { _questCardRT.anchoredPosition = new Vector2(safeCenterX, 20f + safeYOffset); _questCardRT.sizeDelta = new Vector2(cardW, 90f); }
                if (_hubGridRT != null) { _hubGridRT.anchoredPosition = new Vector2(safeCenterX, -155f + safeYOffset); _hubGridRT.sizeDelta = new Vector2(Mathf.Min(520f, cardW), 190f); }
                if (_playCtaRT != null) { _playCtaRT.anchoredPosition = new Vector2(safeCenterX, -330f + safeYOffset); _playCtaRT.sizeDelta = new Vector2(cardW, 88f); }
            }
        }

        private static string FormatVariantName(XOVariantRule variant, int boardSize)
        {
            return variant switch
            {
                XOVariantRule.Infinite3x3 => "Infinite 3x3 Bolt",
                XOVariantRule.GravityDrop => "Gravity Drop 8x8",
                XOVariantRule.StrictCaro => $"Strict Caro {boardSize}x{boardSize}",
                XOVariantRule.ChaosMinefield => $"Chaos Minefield {boardSize}x{boardSize}",
                XOVariantRule.GobbletStack => "Gobblet XO 4x4",
                _ => $"Classic Caro {boardSize}x{boardSize}"
            };
        }
    }
}
