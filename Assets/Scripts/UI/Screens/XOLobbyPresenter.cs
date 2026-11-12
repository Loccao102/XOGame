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
        public Text MissionText { get; private set; }

        private RectTransform _sharedHeaderRT;
        private Text _coinsText;
        private Text _rankTitleText;
        private Image _rankBadgeImg;
        private Text _streakBadgeText;
        private Text _chestBadgeText;
        private Image _chestFillImg;
        private Button _soundBtn;
        private Image _soundIconImg;
        private Button _installBtn;

        // Lobby body. Everything lives in a nominal-size frame (1080x1920 portrait, 1920x1080 landscape)
        // that is positioned with top-left pixel coordinates and scaled to fit the viewport.
        private RectTransform _frameRT;
        private XOLobbyBoardDemo _demo;
        private RectTransform _demoRT;
        private RectTransform _wordmarkRT;
        private Text _wordmarkText;
        private Text _taglineText;
        private RectTransform _taglineRT;
        private RectTransform _rankChipRT;
        private RectTransform _playCtaRT;
        private RectTransform _modePillRT;
        private RectTransform _rivalPillRT;
        private RectTransform _missionRT;
        private readonly RectTransform[] _tileRTs = new RectTransform[4];
        private Image _spinDotImg;
        private Text _missionLabelText;
        private Text _missionRewardText;
        private Text _towerLineText;
        private Text _playCtaBtnText;
        private Text _playCtaSubText;
        private Button _modePillBtn;
        private Button _rivalPillBtn;
        private Vector2 _frameSize = new Vector2(1080f, 1920f);

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

        /// <summary>Creates the shared top header: loose chips (coins, streak, chest, sound, install) with no card behind them.</summary>
        public RectTransform BuildSharedHeader(Transform canvasTransform, bool showStoreInstallButton, Action onHomeClicked)
        {
            GameObject headerObj = XOUIBuilder.CreateUIObject("SharedHeaderBar", canvasTransform);
            _sharedHeaderRT = headerObj.GetComponent<RectTransform>();
            _sharedHeaderRT.SetAnchor(new Vector2(0.5f, 1f), new Vector2(0.5f, 1f));
            _sharedHeaderRT.sizeDelta = new Vector2(1080f, 80f);

            // Kept as an invisible graphic so existing theme code and raycast blocking behave as before.
            HeaderCardImg = headerObj.AddComponent<Image>();
            HeaderCardImg.color = new Color(1f, 1f, 1f, 0f);
            HeaderCardImg.raycastTarget = false;

            const float inset = 30f;

            // Left: coins (tap for a rewarded-ad top-up) and win streak.
            Button coinsBtn = XOUIBuilder.CreateIcon3DButton(_sharedHeaderRT, _font, XOProceduralAssets.IconCoin, "150", new Vector2(200f, 64f), XOUIColors.Yellow100, XOProceduralAssets.ColInk, 28);
            coinsBtn.name = "CoinChip";
            RectTransform cRT = coinsBtn.GetComponent<RectTransform>();
            cRT.SetAnchor(new Vector2(0f, 0.5f), new Vector2(0f, 0.5f));
            cRT.anchoredPosition = new Vector2(inset + 100f, 0f);
            _coinsText = coinsBtn.GetComponentInChildren<Text>();
            _coinsText.rectTransform.offsetMax = new Vector2(-56f, -2f);
            coinsBtn.onClick.AddListener(() => _onFreeCoinsClicked?.Invoke());

            GameObject plus = XOUIBuilder.CreateUIObject("PlusDot", cRT);
            RectTransform plusRT = plus.GetComponent<RectTransform>();
            plusRT.SetAnchor(new Vector2(1f, 0.5f), new Vector2(1f, 0.5f));
            plusRT.anchoredPosition = new Vector2(-30f, 1f);
            plusRT.sizeDelta = new Vector2(38f, 38f);
            Image plusImg = plus.AddComponent<Image>();
            plusImg.sprite = XOProceduralAssets.CircleSprite;
            plusImg.color = XOProceduralAssets.ColMint;
            plusImg.raycastTarget = false;
            Text plusTxt = XOUIBuilder.CreateText(plusRT, _font, "+", 30, FontStyle.Bold, TextAnchor.MiddleCenter);
            plusTxt.color = XOProceduralAssets.ColInk;

            GameObject streakObj = XOUIBuilder.CreateUIObject("StreakChip", _sharedHeaderRT);
            RectTransform stRT = streakObj.GetComponent<RectTransform>();
            stRT.SetAnchor(new Vector2(0f, 0.5f), new Vector2(0f, 0.5f));
            stRT.anchoredPosition = new Vector2(inset + 200f + 14f + 88f, 0f);
            stRT.sizeDelta = new Vector2(176f, 64f);
            Image stBg = streakObj.AddComponent<Image>();
            stBg.sprite = XOProceduralAssets.PillSprite;
            stBg.type = Image.Type.Sliced;
            stBg.color = XOUIColors.Amber100;
            stBg.raycastTarget = false;
            XOUIBuilder.CreateIconImage(stRT, XOProceduralAssets.IconFlame, new Vector2(0f, 0.5f), new Vector2(34f, 0f), new Vector2(36f, 36f));
            _streakBadgeText = XOUIBuilder.CreateText(stRT, _font, "0 streak", 22, FontStyle.Bold, TextAnchor.MiddleCenter);
            _streakBadgeText.rectTransform.offsetMin = new Vector2(54f, 0f);
            _streakBadgeText.rectTransform.offsetMax = new Vector2(-10f, 0f);
            _streakBadgeText.color = XOUIColors.Amber700;

            // Right: install (optional), sound, chest.
            float right = inset;
            if (showStoreInstallButton)
            {
                _installBtn = XOUIBuilder.Create3DButton(_sharedHeaderRT, _font, "GET", new Vector2(92f, 64f), XOProceduralAssets.ColMint, XOProceduralAssets.ColInk, 24);
                RectTransform instRT = _installBtn.GetComponent<RectTransform>();
                instRT.SetAnchor(new Vector2(1f, 0.5f), new Vector2(1f, 0.5f));
                instRT.anchoredPosition = new Vector2(-(right + 46f), 0f);
                _installBtn.onClick.AddListener(() => XOPlayableAdsBridge.Instance?.TriggerInstallCTA());
                right += 92f + 12f;
            }

            _soundBtn = XOUIBuilder.Create3DButton(_sharedHeaderRT, _font, "", new Vector2(64f, 64f), Color.white, XOProceduralAssets.ColInk, 16);
            _soundBtn.name = "SoundButton";
            RectTransform sndRT = _soundBtn.GetComponent<RectTransform>();
            sndRT.SetAnchor(new Vector2(1f, 0.5f), new Vector2(1f, 0.5f));
            sndRT.anchoredPosition = new Vector2(-(right + 32f), 0f);
            _soundIconImg = XOUIBuilder.CreateIconImage(sndRT, XOProceduralAssets.IconSoundOn, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(38f, 38f));
            _soundBtn.onClick.AddListener(() =>
            {
                _audio.ToggleMute();
                RefreshHeaderSoundIcon();
            });
            right += 64f + 12f;

            Button chestBtn = XOUIBuilder.CreateIcon3DButton(_sharedHeaderRT, _font, XOProceduralAssets.IconChest, "0/3", new Vector2(150f, 64f), XOUIColors.Violet200, XOUIColors.Violet800, 26);
            chestBtn.name = "ChestChip";
            RectTransform chRT = chestBtn.GetComponent<RectTransform>();
            chRT.SetAnchor(new Vector2(1f, 0.5f), new Vector2(1f, 0.5f));
            chRT.anchoredPosition = new Vector2(-(right + 75f), 0f);
            _chestBadgeText = chestBtn.GetComponentInChildren<Text>();
            _chestFillImg = chestBtn.GetComponent<Image>();
            chestBtn.onClick.AddListener(() => _onChestClicked?.Invoke());

            return _sharedHeaderRT;
        }

        /// <summary>Builds the full Lobby screen hierarchy.</summary>
        public void Build(Transform canvasTransform)
        {
            RootObj = XOUIBuilder.CreateUIObject("LobbyScreenRoot", canvasTransform);
            RootRT = RootObj.GetComponent<RectTransform>();

            GameObject frame = XOUIBuilder.CreateUIObject("LobbyFrame", RootRT);
            _frameRT = frame.GetComponent<RectTransform>();
            _frameRT.SetAnchor(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
            _frameRT.sizeDelta = _frameSize;

            BuildHero();
            BuildPlayCta();
            BuildPills();
            BuildMissionStrip();
            BuildTiles();

            _towerLineText = XOUIBuilder.CreateText(_frameRT, _font, "", 24, FontStyle.Bold, TextAnchor.MiddleCenter);
            _towerLineText.name = "TowerLine";
            _towerLineText.color = XOUIColors.Sky600;

            ApplyTheme(_store.ActiveTheme);
        }

        private void BuildHero()
        {
            GameObject demoObj = XOUIBuilder.CreateUIObject("HeroBoardDemo", _frameRT);
            _demoRT = demoObj.GetComponent<RectTransform>();
            _demoRT.SetAnchor(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
            _demoRT.sizeDelta = new Vector2(600f, 600f);
            _demo = demoObj.AddComponent<XOLobbyBoardDemo>();
            _demo.Build(_demoRT, 600f);

            GameObject wm = XOUIBuilder.CreateUIObject("Wordmark", _frameRT);
            _wordmarkRT = wm.GetComponent<RectTransform>();
            _wordmarkRT.SetAnchor(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
            _wordmarkRT.sizeDelta = new Vector2(600f, 110f);
            XOUIBuilder.CreateIconImage(_wordmarkRT, XOProceduralAssets.XPieceSprite, new Vector2(0.5f, 0.5f), new Vector2(-190f, 0f), new Vector2(84f, 84f));
            XOUIBuilder.CreateIconImage(_wordmarkRT, XOProceduralAssets.OPieceSprite, new Vector2(0.5f, 0.5f), new Vector2(190f, 0f), new Vector2(84f, 84f));
            _wordmarkText = XOUIBuilder.CreateText(_wordmarkRT, _font, "Caro", 88, FontStyle.Bold, TextAnchor.MiddleCenter);
            _wordmarkText.rectTransform.offsetMin = new Vector2(110f, 0f);
            _wordmarkText.rectTransform.offsetMax = new Vector2(-110f, 0f);

            _taglineText = XOUIBuilder.CreateText(_frameRT, _font, $"Line up five. Climb {XORoguelikeCatalog.FloorCount} floors.", 28, FontStyle.Bold, TextAnchor.MiddleCenter);
            _taglineText.name = "Tagline";
            _taglineRT = _taglineText.rectTransform;
            _taglineRT.SetAnchor(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
            _taglineRT.sizeDelta = new Vector2(760f, 44f);

            GameObject chip = XOUIBuilder.CreateUIObject("RankChip", _frameRT);
            _rankChipRT = chip.GetComponent<RectTransform>();
            _rankChipRT.SetAnchor(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
            _rankChipRT.sizeDelta = new Vector2(380f, 52f);
            _rankBadgeImg = chip.AddComponent<Image>();
            _rankBadgeImg.sprite = XOProceduralAssets.PillSprite;
            _rankBadgeImg.type = Image.Type.Sliced;
            _rankBadgeImg.raycastTarget = false;
            XOUIBuilder.CreateIconImage(_rankChipRT, XOProceduralAssets.IconTrophy, new Vector2(0f, 0.5f), new Vector2(32f, 0f), new Vector2(34f, 34f));
            _rankTitleText = XOUIBuilder.CreateText(_rankChipRT, _font, "Bronze Novice · 660 ELO", 22, FontStyle.Bold, TextAnchor.MiddleCenter);
            _rankTitleText.rectTransform.offsetMin = new Vector2(56f, 0f);
            _rankTitleText.rectTransform.offsetMax = new Vector2(-16f, 0f);
        }

        private void BuildPlayCta()
        {
            Button playBtn = XOUIBuilder.Create3DButton(_frameRT, _font, "Play", new Vector2(900f, 150f), XOProceduralAssets.ColMint, XOProceduralAssets.ColInk, 56);
            playBtn.name = "PlayButton";
            _playCtaRT = playBtn.GetComponent<RectTransform>();
            _playCtaRT.SetAnchor(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
            playBtn.onClick.AddListener(() => _onPlayNow?.Invoke());

            XOUIBuilder.CreateIconImage(_playCtaRT, XOProceduralAssets.IconNewGame, new Vector2(0f, 0.5f), new Vector2(86f, 4f), new Vector2(74f, 74f));

            _playCtaBtnText = playBtn.GetComponentInChildren<Text>();
            _playCtaBtnText.alignment = TextAnchor.MiddleLeft;
            _playCtaBtnText.rectTransform.offsetMin = new Vector2(150f, 66f);
            _playCtaBtnText.rectTransform.offsetMax = new Vector2(-24f, -12f);

            _playCtaSubText = XOUIBuilder.CreateText(_playCtaRT, _font, "", 23, FontStyle.Bold, TextAnchor.MiddleLeft);
            _playCtaSubText.name = "PlaySubtitle";
            _playCtaSubText.color = XOUIColors.Slate700;
            _playCtaSubText.rectTransform.offsetMin = new Vector2(150f, 20f);
            _playCtaSubText.rectTransform.offsetMax = new Vector2(-24f, -92f);
        }

        private void BuildPills()
        {
            _modePillBtn = XOUIBuilder.CreateIcon3DButton(_frameRT, _font, XOProceduralAssets.IconSwap, "Change rules", new Vector2(440f, 70f), XOUIColors.Slate700, Color.white, 28);
            _modePillBtn.name = "ChangeRulesButton";
            _modePillRT = _modePillBtn.GetComponent<RectTransform>();
            _modePillBtn.onClick.AddListener(() => _onOpenModeSelect?.Invoke());

            _rivalPillBtn = XOUIBuilder.CreateIcon3DButton(_frameRT, _font, XOProceduralAssets.IconSwords, "Change rival", new Vector2(440f, 70f), XOUIColors.Slate700, Color.white, 28);
            _rivalPillBtn.name = "ChangeRivalButton";
            _rivalPillRT = _rivalPillBtn.GetComponent<RectTransform>();
            _rivalPillBtn.onClick.AddListener(() => _onOpenLeaderboard?.Invoke());
        }

        private void BuildMissionStrip()
        {
            GameObject obj = XOUIBuilder.CreateUIObject("MissionStrip", _frameRT);
            _missionRT = obj.GetComponent<RectTransform>();
            _missionRT.SetAnchor(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
            _missionRT.sizeDelta = new Vector2(900f, 92f);

            QuestCardImg = obj.AddComponent<Image>();
            QuestCardImg.sprite = XOProceduralAssets.CardPanelSprite;
            QuestCardImg.type = Image.Type.Sliced;
            QuestCardImg.raycastTarget = false;

            XOUIBuilder.CreateIconImage(_missionRT, XOProceduralAssets.IconHint, new Vector2(0f, 0.5f), new Vector2(54f, 0f), new Vector2(46f, 46f));

            _missionLabelText = XOUIBuilder.CreateText(_missionRT, _font, "Daily mission", 19, FontStyle.Bold, TextAnchor.UpperLeft);
            _missionLabelText.color = XOUIColors.Amber600;
            _missionLabelText.rectTransform.offsetMin = new Vector2(96f, 0f);
            _missionLabelText.rectTransform.offsetMax = new Vector2(-130f, -12f);

            MissionText = XOUIBuilder.CreateText(_missionRT, _font, "Win a match within 15 moves", 26, FontStyle.Bold, TextAnchor.LowerLeft);
            MissionText.rectTransform.offsetMin = new Vector2(96f, 10f);
            MissionText.rectTransform.offsetMax = new Vector2(-130f, 0f);

            _missionRewardText = XOUIBuilder.CreateText(_missionRT, _font, "+25", 30, FontStyle.Bold, TextAnchor.MiddleRight);
            _missionRewardText.color = XOUIColors.Amber700;
            _missionRewardText.rectTransform.offsetMin = new Vector2(0f, 0f);
            _missionRewardText.rectTransform.offsetMax = new Vector2(-70f, 0f);
            XOUIBuilder.CreateIconImage(_missionRT, XOProceduralAssets.IconCoin, new Vector2(1f, 0.5f), new Vector2(-40f, 0f), new Vector2(40f, 40f));
        }

        private void BuildTiles()
        {
            _tileRTs[0] = BuildTile("Tower", XOProceduralAssets.IconCrown, XOProceduralAssets.ColLilac, Color.white, _onOpenTower);
            _tileRTs[1] = BuildTile("Shop", XOProceduralAssets.IconSkinShop, XOProceduralAssets.ColSky, Color.white, _onOpenShop);
            _tileRTs[2] = BuildTile("Rivals", XOProceduralAssets.IconLeaderboard, XOProceduralAssets.ColPeach, XOProceduralAssets.ColInk, _onOpenLeaderboard);
            _tileRTs[3] = BuildTile("Spin", XOProceduralAssets.IconLuckyWheel, XOProceduralAssets.ColSun, XOProceduralAssets.ColInk, _onOpenLuckyWheel);

            GameObject dot = XOUIBuilder.CreateUIObject("SpinReadyDot", _tileRTs[3]);
            RectTransform dotRT = dot.GetComponent<RectTransform>();
            dotRT.SetAnchor(new Vector2(1f, 1f), new Vector2(1f, 1f));
            dotRT.anchoredPosition = new Vector2(-18f, -18f);
            dotRT.sizeDelta = new Vector2(30f, 30f);
            _spinDotImg = dot.AddComponent<Image>();
            _spinDotImg.sprite = XOProceduralAssets.CircleSprite;
            _spinDotImg.color = XOProceduralAssets.ColCoral;
            _spinDotImg.raycastTarget = false;
        }

        private RectTransform BuildTile(string label, Sprite icon, Color bg, Color textColor, Action onClick)
        {
            Button btn = XOUIBuilder.Create3DButton(_frameRT, _font, label, new Vector2(210f, 250f), bg, textColor, 28);
            btn.name = "Tile_" + label;
            btn.onClick.AddListener(() => onClick?.Invoke());
            RectTransform rt = btn.GetComponent<RectTransform>();

            Image ico = XOUIBuilder.CreateIconImage(rt, icon, new Vector2(0.5f, 0.62f), Vector2.zero, new Vector2(110f, 110f));
            ico.name = "TileIcon";

            Text txt = btn.GetComponentInChildren<Text>();
            txt.rectTransform.anchorMin = new Vector2(0f, 0f);
            txt.rectTransform.anchorMax = new Vector2(1f, 0.28f);
            txt.rectTransform.offsetMin = new Vector2(6f, 10f);
            txt.rectTransform.offsetMax = new Vector2(-6f, 0f);
            return rt;
        }

        /// <summary>Updates header stats, rank chip, coins, sound icon, mission strip, Play subtitle and tower line.</summary>
        public void Refresh(GameMode mode, XOVariantRule variant, int boardSize, AIDifficulty difficulty)
        {
            RankInfo rank = XOProgressionSystem.GetCurrentRank(out int _, out int _);
            if (_rankTitleText != null) _rankTitleText.text = $"{rank.title} · {XOProgressionSystem.PlayerELO} ELO";
            if (_rankBadgeImg != null) _rankBadgeImg.color = Color.Lerp(rank.badgeColor.ToUnityColor(), Color.white, 0.65f);
            if (_streakBadgeText != null) _streakBadgeText.text = $"{XOProgressionSystem.WinStreak} streak";
            if (_coinsText != null) _coinsText.text = _store.Coins.ToString();

            int chest = XOProgressionSystem.ChestProgress;
            if (_chestBadgeText != null) _chestBadgeText.text = (chest >= 3) ? "Open!" : $"{chest}/3";
            if (_chestFillImg != null) _chestFillImg.color = (chest >= 3) ? XOProceduralAssets.ColSun : XOUIColors.Violet200;

            if (_spinDotImg != null) _spinDotImg.enabled = _store.FreeSpinAvailable;

            RefreshHeaderSoundIcon();

            XOMissionData mission = XOProgressionSystem.GetActiveMission();
            if (MissionText != null && mission != null)
            {
                MissionText.text = mission.description;
                if (_missionRewardText != null) _missionRewardText.text = $"+{mission.rewardCoins}";
            }

            if (_playCtaSubText != null)
            {
                string ruleLabel = FormatVariantName(variant, boardSize);
                switch (mode)
                {
                    case GameMode.VsAI:
                        _playCtaSubText.text = $"{ruleLabel} · vs {XOProgressionSystem.GetActiveBot().botName}";
                        break;
                    case GameMode.TowerClimb:
                        TowerFloorBlind blind = XORoguelikeRunManager.GetActiveBlind();
                        _playCtaSubText.text = $"Floor {blind.floorNumber} · {blind.bossName} ({blind.blindName})";
                        break;
                    case GameMode.PuzzleStage:
                        _playCtaSubText.text = $"Puzzle #{XOProgressionSystem.CurrentPuzzleStage} · Win in a few moves";
                        break;
                    default:
                        _playCtaSubText.text = $"{ruleLabel} · 2 players on this device";
                        break;
                }
            }

            if (_towerLineText != null)
            {
                _towerLineText.text = XORoguelikeRunManager.IsRunActive
                    ? $"Tower run: floor {XORoguelikeRunManager.CurrentFloor}/{XORoguelikeCatalog.FloorCount} · {XORoguelikeRunManager.Lives} hearts · {XORoguelikeRunManager.EquippedRelics.Count} relics"
                    : $"Tower cleared: {XOProgressionSystem.HighestTowerFloorCleared}/{XORoguelikeCatalog.FloorCount} floors";
            }

            if (_playCtaBtnText != null)
            {
                _playCtaBtnText.text = (mode == GameMode.TowerClimb)
                    ? $"Battle floor {XORoguelikeRunManager.CurrentFloor}"
                    : "Play";
            }
        }

        public void RefreshHeaderSoundIcon()
        {
            if (_soundIconImg == null) return;
            _soundIconImg.sprite = _audio.IsMuted ? XOProceduralAssets.IconSoundOff : XOProceduralAssets.IconSoundOn;
        }

        /// <summary>Applies the active theme to the lobby surfaces, text and demo board.</summary>
        public void ApplyTheme(XOThemePalette theme)
        {
            Color primary = theme.primaryTextColor.ToUnityColor();
            Color secondary = theme.secondaryTextColor.ToUnityColor();
            if (QuestCardImg != null) QuestCardImg.color = theme.panelSurfaceColor.ToUnityColor();
            if (MissionText != null) MissionText.color = primary;
            if (_wordmarkText != null) _wordmarkText.color = primary;
            if (_taglineText != null) _taglineText.color = secondary;
            if (_rankTitleText != null) _rankTitleText.color = primary;
            if (_demo != null) _demo.ApplyTheme(theme, _store.ActivePieceSkin != null ? _store.ActivePieceSkin.id : null);
        }

        /// <summary>Scales the nominal lobby frame to the viewport and places every element in portrait or landscape.</summary>
        public void ApplyResponsiveLayout(float refW, float refH, bool isLandscape, float safeLeft = 0f, float safeRight = 0f, float safeTop = 0f, float safeBottom = 0f)
        {
            if (RootObj == null || _frameRT == null) return;

            _frameSize = isLandscape ? new Vector2(1920f, 1080f) : new Vector2(1080f, 1920f);
            _frameRT.sizeDelta = _frameSize;
            // Fit the frame inside the safe area (notches / browser bars) and centre it there.
            float availW = Mathf.Max(320f, refW - safeLeft - safeRight);
            float availH = Mathf.Max(320f, refH - safeTop - safeBottom);
            float k = Mathf.Min(1f, Mathf.Min(availW / _frameSize.x, availH / _frameSize.y));
            _frameRT.localScale = new Vector3(k, k, 1f);
            _frameRT.anchoredPosition = new Vector2((safeLeft - safeRight) * 0.5f, (safeBottom - safeTop) * 0.5f);

            if (!isLandscape)
            {
                _demoRT.localScale = Vector3.one;
                Place(_demoRT, 240f, 170f, 600f, 600f);
                _wordmarkRT.localScale = Vector3.one;
                Place(_wordmarkRT, 240f, 790f, 600f, 110f);
                Place(_taglineRT, 160f, 903f, 760f, 44f);
                Place(_rankChipRT, 350f, 960f, 380f, 52f);
                Place(_playCtaRT, 90f, 1050f, 900f, 150f);
                Place(_modePillRT, 90f, 1228f, 440f, 70f);
                Place(_rivalPillRT, 550f, 1228f, 440f, 70f);
                Place(_missionRT, 90f, 1352f, 900f, 92f);
                for (int i = 0; i < _tileRTs.Length; i++) Place(_tileRTs[i], 90f + 230f * i, 1478f, 210f, 250f);
                Place(_towerLineText.rectTransform, 90f, 1768f, 900f, 40f);
            }
            else
            {
                Place(_demoRT, 150f, 190f, 640f, 640f);
                _demoRT.localScale = Vector3.one * (640f / 600f);
                _demoRT.sizeDelta = new Vector2(600f, 600f);
                _wordmarkRT.localScale = Vector3.one * 0.78f;
                Place(_wordmarkRT, 150f, 850f, 640f, 110f * 0.78f);
                Place(_taglineRT, 150f, 935f, 640f, 44f);

                const float x0 = 1010f, w = 760f;
                Place(_rankChipRT, x0 + (w - 380f) * 0.5f, 190f, 380f, 52f);
                Place(_playCtaRT, x0, 270f, w, 150f);
                Place(_modePillRT, x0, 448f, 372f, 70f);
                Place(_rivalPillRT, x0 + 388f, 448f, 372f, 70f);
                Place(_missionRT, x0, 572f, w, 92f);
                float tileW = (w - 3f * 20f) / 4f;
                for (int i = 0; i < _tileRTs.Length; i++) Place(_tileRTs[i], x0 + (tileW + 20f) * i, 700f, tileW, 210f);
                Place(_towerLineText.rectTransform, x0, 950f, w, 40f);
            }
        }

        /// <summary>Positions a centre-anchored rect inside the frame from top-left pixel coordinates (x, y, width, height).</summary>
        private void Place(RectTransform rt, float x, float y, float w, float h)
        {
            if (rt == null) return;
            rt.SetAnchor(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
            rt.anchoredPosition = new Vector2(x + w * 0.5f - _frameSize.x * 0.5f, _frameSize.y * 0.5f - (y + h * 0.5f));
            if (rt.GetComponent<Button>() != null || rt == _missionRT || rt == _rankChipRT || rt == _taglineRT || rt == _towerLineText.rectTransform)
                rt.sizeDelta = new Vector2(w, h);
        }

        private static string FormatVariantName(XOVariantRule variant, int boardSize)
        {
            return variant switch
            {
                XOVariantRule.Infinite3x3 => "Infinite 3×3",
                XOVariantRule.GravityDrop => "Gravity Drop 8×8",
                XOVariantRule.StrictCaro => $"Strict Caro {boardSize}×{boardSize}",
                XOVariantRule.ChaosMinefield => $"Chaos Minefield {boardSize}×{boardSize}",
                XOVariantRule.GobbletStack => "Gobblet 4×4",
                _ => $"Classic Caro {boardSize}×{boardSize}"
            };
        }
    }
}
