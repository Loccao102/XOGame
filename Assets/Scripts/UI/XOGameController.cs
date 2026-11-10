using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using XOPlayable.Ads;
using XOPlayable.Core;
using XOPlayable.Visuals;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif

namespace XOPlayable.UI
{
    public enum ActiveSkillTargeting
    {
        None = 0,
        BombDestroy = 1,
        FreezeTile = 2,
        SwapPiece = 3
    }

    public class XOGameController : MonoBehaviour
    {
        public static XOGameController Instance { get; private set; }

        /// <summary>Test/QA hook: forces the responsive layout to treat the screen as this size (null = real Screen size).</summary>
        public static Vector2Int? DebugScreenSizeOverride;
        private static int ScreenW => DebugScreenSizeOverride.HasValue ? DebugScreenSizeOverride.Value.x : Screen.width;
        private static int ScreenH => DebugScreenSizeOverride.HasValue ? DebugScreenSizeOverride.Value.y : Screen.height;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoBootstrapOnLoad()
        {
            if (FindFirstObjectByType<XOGameController>() == null)
            {
                GameObject go = new GameObject("XOGameController_Auto");
                go.AddComponent<XOGameController>();
            }
        }

        [Header("Caro Board & Variant Settings")]
        public int currentBoardSize = 15; // 15x15 Pro Caro standard (Zero-Draws) // 3x3, 8x8, 10x10, or 15x15
        public int winLength = 5;         // 3, 4, or 5-in-a-row to win
        public XOVariantRule currentVariant = XOVariantRule.ClassicCaro;
        public GameMode currentGameMode = GameMode.VsAI;
        public AIDifficulty currentDifficulty = AIDifficulty.Normal;

        /// <summary>
        /// True for any single-player mode where Opponent (O) is driven by the AI engine.
        /// </summary>
        public bool IsVsAIMode => currentGameMode == GameMode.VsAI || currentGameMode == GameMode.TowerClimb || currentGameMode == GameMode.PuzzleStage;

        [Header("Turn Timer Tension (Seconds per Player Turn)")]
        public float turnTimeLimit = 18f;

        [Header("Playable & Live Arena Settings")]
        public bool enableAmbientBgm = true;
        public bool enableHaptics = true;
        public bool enableLiveMatchmaking = true;
        [Tooltip("Leave false for full standalone Google/YouTube Playable game. Set true only if running as a 30s Store Ad Demo.")]
        public bool showStoreInstallButton = false;

        // Persistent Player Resources
        public int Coins
        {
            get => PlayerPrefs.GetInt("XO_Coins", 50);
            set
            {
                int prev = PlayerPrefs.GetInt("XO_Coins", 50);
                int clamped = Mathf.Max(0, value);
                PlayerPrefs.SetInt("XO_Coins", clamped);
                PlayerPrefs.Save();
                RefreshHeaderAndProgressionUI();
                if (clamped > prev && _mainCanvas != null)
                {
                    SpawnFloatingCoinToast(clamped - prev);
                }
            }
        }

        public int UndoStock
        {
            get => PlayerPrefs.GetInt("XO_UndoStock", 2);
            set
            {
                PlayerPrefs.SetInt("XO_UndoStock", Mathf.Max(0, value));
                PlayerPrefs.Save();
                RefreshBottomActionBadges();
            }
        }

        public int HintStock
        {
            get => PlayerPrefs.GetInt("XO_HintStock", 2);
            set
            {
                PlayerPrefs.SetInt("XO_HintStock", Mathf.Max(0, value));
                PlayerPrefs.Save();
                RefreshBottomActionBadges();
            }
        }

        public int BombStock
        {
            get => PlayerPrefs.GetInt("XO_BombStock", 1);
            set
            {
                PlayerPrefs.SetInt("XO_BombStock", Mathf.Max(0, value));
                PlayerPrefs.Save();
                RefreshBottomActionBadges();
            }
        }

        public int FreezeStock
        {
            get => PlayerPrefs.GetInt("XO_FreezeStock", 1);
            set
            {
                PlayerPrefs.SetInt("XO_FreezeStock", Mathf.Max(0, value));
                PlayerPrefs.Save();
                RefreshBottomActionBadges();
            }
        }

        public int SwapStock
        {
            get => PlayerPrefs.GetInt("XO_SwapStock", 1);
            set
            {
                PlayerPrefs.SetInt("XO_SwapStock", Mathf.Max(0, value));
                PlayerPrefs.Save();
                RefreshBottomActionBadges();
            }
        }

        public bool IsSoundMuted
        {
            get => PlayerPrefs.GetInt("XO_Muted", 0) == 1;
            set
            {
                PlayerPrefs.SetInt("XO_Muted", value ? 1 : 0);
                PlayerPrefs.Save();
                if (_muteButtonIconImg != null)
                    _muteButtonIconImg.sprite = value ? XOProceduralAssets.IconSoundOff : XOProceduralAssets.IconSoundOn;
                if (_bgmAudioSource != null)
                    _bgmAudioSource.mute = value || !enableAmbientBgm;
            }
        }

        public bool FreeSpinAvailable
        {
            get => PlayerPrefs.GetInt("XO_FreeSpinReady", 1) == 1;
            set
            {
                PlayerPrefs.SetInt("XO_FreeSpinReady", value ? 1 : 0);
                PlayerPrefs.Save();
                RefreshHeaderAndProgressionUI();
            }
        }

        private XOCaroEngine _engine;
        private XOThemePalette _activeTheme;
        private XOPieceSkin _activePieceSkin;
        private AudioSource _audioSource;
        private AudioSource _bgmAudioSource;
        private Font _uiFont;

        // Session State & Puzzle State
        private int _scoreX = 0;
        private int _scoreO = 0;
        private bool _isAIThinking = false;
        private bool _reviveUsedThisMatch = false;
        private bool _usedPowerUpThisMatch = false;
        private bool _botUsedCounterSkillThisMatch = false;
        private bool _announcedFirstFiveX = false;
        private bool _announcedFirstFiveO = false;
        private bool _midMatchShiftTriggered = false;
        private Vector2Int _activeHintCell = new Vector2Int(-1, -1);
        private ActiveSkillTargeting _skillTargeting = ActiveSkillTargeting.None;

        private XOPuzzleStage _currentPuzzle;
        private int _puzzleMovesLeft = 0;
        private float _turnTimerRemaining = 18f;

        // Responsive Layout State
        private int _lastScreenWidth = -1;
        private int _lastScreenHeight = -1;
        public enum ScreenState
        {
            Lobby = 0,
            Battle = 1
        }

        public ScreenState CurrentScreen { get; private set; } = ScreenState.Lobby;

        private bool _isLandscapeLayout = true;

        // UI Root References
        private Canvas _mainCanvas;
        private CanvasScaler _canvasScaler;
        private Image _backgroundTopImg;

        // Screen State Roots
        private GameObject _lobbyRootObj;
        private RectTransform _lobbyRootRT;
        private RectTransform _lobbyHeroCardRT;
        private Image _lobbyHeroCardImg;
        private RectTransform _lobbyHubGridRT;
        private GridLayoutGroup _lobbyHubGridLayout;

        private GameObject _battleRootObj;
        private RectTransform _battleRootRT;
        private RectTransform _battleTopHudRT;
        private RectTransform _battleBottomBarRT;

        // Lobby Header & Cards
        private RectTransform _headerCardRT;
        private Image _headerCardImg;
        private Image _muteButtonIconImg;
        private Button _luckySpinHeaderBtn;
        private Text _luckySpinHeaderBtnText;
        private Text _rankBadgeText;
        private Text _streakText;
        private Text _coinText;

        // Lobby Hero Section
        private Text _lobbyModeBannerTitle;
        private Text _lobbyModeBannerDesc;
        private Text _lobbyRivalBannerText;
        private Button _lobbyPlayBtn;
        private Button _lobbyChangeModeBtn;
        private Button _lobbyChangeRivalBtn;

        // Lobby Hub Buttons (4 Cards)
        private Button _lobbyHubModeBtn;
        private Button _lobbyHubArenaBtn;
        private Button _lobbyHubWheelBtn;
        private Button _lobbyHubShopBtn;
        private Button _lobbyHubTowerBtn;
        private Text _towerBadgeText;
        private GameObject _towerModalRoot;
        private RectTransform _towerCardRT;
        private Transform _towerContentContainer;
        private Text _towerRunPillText;
        private Image _towerRunPillBg;
        private Button _towerRestartRunBtn;

        // Roguelike Tower Caravan & Relic Draft UI
        private GameObject _caravanModalRoot;
        private RectTransform _caravanCardRT;
        private Transform _caravanRelicsContainer;
        private Transform _caravanEquippedContainer;
        private Text _caravanSubtitleText;
        private Text _caravanLivesText;
        private Button _caravanHealBtn;
        private Button _caravanRerollBtn;
        private int _caravanRerollCount = 0;
        private Button _caravanContinueBtn;
        private Button _tabSkillsBtn;
        private GameObject _skillsListContainer;

        // Quest & Chest
        private RectTransform _questCardRT;
        private Image _questCardImg;
        private Text _missionText;
        private Button _chestButton;
        private Text _chestButtonText;
        private Image _chestProgressFillImg;

        // Mode Select Modal Elements
        private GameObject _modeSelectModalRoot;
        private RectTransform _modeCardRT;
        private Image _modeCardImg;
        private Button _size10Btn;
        private Button _size15Btn;
        private Button _varInfinite3Btn;
        private Button _varGobbletBtn;
        private Button _varGravityBtn;
        private Button _varStrictBtn;
        private Button _varChaosBtn;
        private Button _modeAiTierBtn;
        private Text _modeAiTierBtnText;
        private Button _mode2PBtn;
        private Button _modePuzzleBtn;
        private Text _modePuzzleBtnText;

        // Battle Screen HUD
        private Button _battleHomeBtn;
        private Image _playerXCardImg;
        private Image _playerOCardImg;
        private Image _playerXTokenIcon;
        private Image _playerOTokenIcon;
        private Text _playerXTitleText;
        private Text _playerOTitleText;
        private Text _scoreXText;
        private Text _scoreOText;
        private Image _statusBannerImg;
        private Text _statusBannerText;
        private Image _turnTimerFillImg;

        // Battle Relic Bar (Tower Jokers Tray)
        private RectTransform _battleRelicBarRT;
        private List<Button> _battleRelicSlotButtons = new List<Button>();
        private List<Image> _battleRelicSlotIcons = new List<Image>();
        private List<Text> _battleRelicSlotTexts = new List<Text>();

        // Battle Board
        private RectTransform _boardOuterFrameRT;
        private Image _boardOuterFrameImg;
        private RectTransform _boardGridContainerRT;
        private GridLayoutGroup _boardGridLayout;
        private readonly List<XOCellView> _cellViews = new List<XOCellView>();
        private Image _winStrikeLineImg;

        // Live Bot Reaction Speech Bubble
        private GameObject _botChatBubbleObj;
        private Text _botChatBubbleText;
        private Coroutine _botChatCoroutine;

        // Battle Bottom Action Bar (Compact Skills)
        private Image _actionsCardImg;
        private GridLayoutGroup _actionsGridLayout;

        private Button _hintBtn;
        private Text _hintBadgeText;
        private Image _hintBadgeBg;

        private Button _undoBtn;
        private Text _undoBadgeText;
        private Image _undoBadgeBg;

        private Button _bombBtn;
        private Text _bombBadgeText;
        private Image _bombBadgeBg;

        private Button _freezeBtn;
        private Text _freezeBadgeText;
        private Image _freezeBadgeBg;

        private Button _swapBtn;
        private Text _swapBadgeText;
        private Image _swapBadgeBg;

        private Button _skinShopBtn;
        private Button _leaderboardBtn;
        private Image _leaderboardBadgeBg;
        private Text _leaderboardBadgeText;
        private Button _freeBonusAdBtn;
        private Button _restartBtn;

        // Modals
        private GameObject _reviveModalRoot;
        private RectTransform _reviveCardRT;
        private Text _reviveTitleText;
        private Text _reviveDescText;
        private Button _reviveActionBtn;
        private Text _reviveActionBtnText;
        private bool _isOutOfMovesPuzzleRevive = false;

        private GameObject _resultModalRoot;
        private RectTransform _resultCardRT;
        private Image _resultHeroIconImg;
        private Text _resultTitleText;
        private Text _resultSubText;
        private Button _doubleRewardAdBtn;
        private Text _doubleRewardAdBtnText;
        private Button _shieldAdBtn;
        private Text _shieldAdBtnText;
        private Button _resultPlayAgainBtn;
        private Text _resultPlayAgainBtnText;
        private Button _resultThirdActionBtn;
        private Text _resultThirdActionBtnText;
        private int _lastMatchRewardCoins = 30;
        private bool _claimedDoubleReward = false;
        private int _lostStreakToRestore = 0;

        // Skin & Theme Shop Modal (3 Tabs: Board Themes, X/O Piece Skins, Skills)
        private GameObject _themeModalRoot;
        private RectTransform _themeCardRT;
        private GameObject _themesScrollViewObj;
        private GameObject _piecesScrollViewObj;
        private GameObject _skillsScrollViewObj;
        private int _activeShopTab = 0;
        private Button _tabThemesBtn;
        private Button _tabPiecesBtn;
        private GameObject _themesListContainer;
        private GameObject _piecesListContainer;
        private readonly List<ShopCardRefs> _themeCards = new List<ShopCardRefs>();
        private readonly List<ShopCardRefs> _pieceSkinCards = new List<ShopCardRefs>();

        // 2-Tab Arena Campaign & Lifetime Achievements Modal
        private GameObject _leaderboardModalRoot;
        private RectTransform _leaderboardCardRT;
        private GameObject _leaderboardScrollViewObj;
        private GameObject _achievementsScrollViewObj;
        private int _activeLeaderboardTab = 0;
        private Button _tabArenaBtn;
        private Button _tabAchievementsBtn;
        private Text _leaderboardSummaryText;
        private RectTransform _leaderboardRowsContainer;
        private RectTransform _achievementsRowsContainer;

        // Lucky Spin Wheel Modal (Daily Free Spin + Rewarded Spin)
        private GameObject _luckyWheelModalRoot;
        private RectTransform _luckyWheelCardRT;
        private readonly List<Image> _wheelSlotBgs = new List<Image>();
        private Text _wheelStatusText;
        private Button _wheelSpinActionBtn;
        private Text _wheelSpinActionBtnText;
        private bool _isWheelSpinning = false;

        private class ShopCardRefs
        {
            public int index;
            public Image cardBg;
            public Text statusText;
            public Button selectOrCoinBtn;
            public Text selectOrCoinBtnText;
            public Button watchAdUnlockBtn;
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            _uiFont = LoadDefaultFont();
            _audioSource = gameObject.AddComponent<AudioSource>();
            _audioSource.playOnAwake = false;

            _bgmAudioSource = gameObject.AddComponent<AudioSource>();
            _bgmAudioSource.playOnAwake = false;
            _bgmAudioSource.loop = true;
            _bgmAudioSource.volume = 0.42f;
            _bgmAudioSource.clip = XOProceduralAssets.GetAmbientBgmLoop();
            _bgmAudioSource.mute = IsSoundMuted || !enableAmbientBgm;
            if (enableAmbientBgm && !IsSoundMuted)
            {
                _bgmAudioSource.Play();
            }

            if (GetComponent<XOPlayableAdsBridge>() == null)
            {
                gameObject.AddComponent<XOPlayableAdsBridge>();
            }

            _activeTheme = XOThemeDatabase.Themes[XOThemeDatabase.GetSelectedThemeIndex()];
            _activePieceSkin = XOThemeDatabase.PieceSkins[XOThemeDatabase.GetSelectedPieceSkinIndex()];

            if (currentBoardSize < 15 && currentVariant == XOVariantRule.ClassicCaro)
            {
                currentBoardSize = 15;
                winLength = 5;
            }

            _engine = new XOCaroEngine(currentBoardSize, winLength);
        }

        private void Start()
        {
            EnsureCameraAndEventSystem();
            BuildCompleteUI();
            XOPlayableAdsBridge.Instance.BuildAdOverlayUI(_mainCanvas.transform, _uiFont);
            StartNewMatch(currentBoardSize);
            UpdateResponsiveLayout(force: true);
            ApplyThemeToAllUI();
            SetScreenState(ScreenState.Lobby);
        }

        private void Update()
        {
            if (ScreenW != _lastScreenWidth || ScreenH != _lastScreenHeight)
            {
                UpdateResponsiveLayout(force: false);
            }

            // Update Turn Timer Bar during active gameplay
            if (CurrentScreen == ScreenState.Battle && _engine != null && _engine.State == MatchState.Playing && !_isAIThinking)
            {
                bool isModalOpen = (_reviveModalRoot != null && _reviveModalRoot.activeSelf)
                                || (_resultModalRoot != null && _resultModalRoot.activeSelf)
                                || (_themeModalRoot != null && _themeModalRoot.activeSelf)
                                || (_leaderboardModalRoot != null && _leaderboardModalRoot.activeSelf)
                                || (_luckyWheelModalRoot != null && _luckyWheelModalRoot.activeSelf)
                                || (_modeSelectModalRoot != null && _modeSelectModalRoot.activeSelf);

                if (!isModalOpen && turnTimeLimit > 0f)
                {
                    _turnTimerRemaining -= Time.deltaTime;
                    if (_turnTimerFillImg != null)
                    {
                        float ratio = Mathf.Clamp01(_turnTimerRemaining / turnTimeLimit);
                        _turnTimerFillImg.fillAmount = ratio;
                        _turnTimerFillImg.color = (ratio > 0.35f)
                            ? XOThemeDatabase.Hex("#22C55E")
                            : XOThemeDatabase.Hex("#EF4444");
                    }

                    // Auto-hint nudge when timer gets low (< 4s) and no hint is active yet
                    if (_turnTimerRemaining <= 0f)
                    {
                        _turnTimerRemaining = turnTimeLimit;
                        if (_activeHintCell.x < 0)
                        {
                            Vector2Int autoMove = _engine.CalculateBestHint(_engine.CurrentTurn, out _);
                            OnBoardCellClicked(autoMove.x, autoMove.y);
                        }
                    }
                }
            }
        }

        private Font LoadDefaultFont()
        {
            Font f = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (f == null) f = Resources.GetBuiltinResource<Font>("Arial.ttf");
            if (f == null) f = Font.CreateDynamicFontFromOSFont("Arial", 28);
            return f;
        }

        private void EnsureCameraAndEventSystem()
        {
            if (Camera.main == null)
            {
                GameObject camObj = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
                camObj.tag = "MainCamera";
                Camera cam = camObj.GetComponent<Camera>();
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = _activeTheme.backgroundTop;
                cam.orthographic = true;
            }
            else
            {
                Camera.main.clearFlags = CameraClearFlags.SolidColor;
                Camera.main.backgroundColor = _activeTheme.backgroundTop;
            }

            EventSystem es = FindFirstObjectByType<EventSystem>();
            if (es == null)
            {
                GameObject esObj = new GameObject("EventSystem", typeof(EventSystem));
#if ENABLE_INPUT_SYSTEM
                esObj.AddComponent<InputSystemUIInputModule>();
#else
                esObj.AddComponent<StandaloneInputModule>();
#endif
            }
            else
            {
#if ENABLE_INPUT_SYSTEM
                if (es.GetComponent<InputSystemUIInputModule>() == null && es.GetComponent<BaseInputModule>() == null)
                {
                    es.gameObject.AddComponent<InputSystemUIInputModule>();
                }
#endif
            }
        }

        // =====================================================================
        // POLISHED 2-SCREEN CASUAL GAME ARCHITECTURE (LOBBY & FOCUSED BATTLE)
        // =====================================================================

        private void BuildCompleteUI()
        {
            Transform existingCanvas = transform.Find("XOPlayableCanvas");
            if (existingCanvas != null)
            {
                Destroy(existingCanvas.gameObject);
            }

            GameObject canvasObj = new GameObject("XOPlayableCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObj.transform.SetParent(transform, false);

            _mainCanvas = canvasObj.GetComponent<Canvas>();
            _mainCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _mainCanvas.sortingOrder = 10;

            _canvasScaler = canvasObj.GetComponent<CanvasScaler>();
            _canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            _canvasScaler.referenceResolution = new Vector2(1920f, 1080f);
            _canvasScaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            _canvasScaler.matchWidthOrHeight = 0.5f;

            // Background Image
            GameObject bgObj = CreateUIObject("Background", canvasObj.transform);
            StretchFull(bgObj.GetComponent<RectTransform>());
            _backgroundTopImg = bgObj.AddComponent<Image>();
            _backgroundTopImg.color = _activeTheme.backgroundTop;

            // 1. Lobby Screen Root (Main Menu)
            _lobbyRootObj = CreateUIObject("LobbyScreenRoot", canvasObj.transform);
            _lobbyRootRT = _lobbyRootObj.GetComponent<RectTransform>();
            StretchFull(_lobbyRootRT);
            BuildLobbyScreen(_lobbyRootRT);

            // 2. Battle Screen Root (Gameplay Arena)
            _battleRootObj = CreateUIObject("BattleScreenRoot", canvasObj.transform);
            _battleRootRT = _battleRootObj.GetComponent<RectTransform>();
            StretchFull(_battleRootRT);
            BuildBattleScreen(_battleRootRT);

            // Modals & Overlays
            BuildReviveModal(canvasObj.transform);
            BuildResultModal(canvasObj.transform);
            BuildThemeShopModal(canvasObj.transform);
            BuildLeaderboardModal(canvasObj.transform);
            BuildLuckyWheelModal(canvasObj.transform);
            BuildModeSelectModal(canvasObj.transform);
            BuildTowerModal(canvasObj.transform);
            BuildCaravanDraftModal(canvasObj.transform);
        }

        public void SetScreenState(ScreenState state)
        {
            CurrentScreen = state;
            if (_lobbyRootObj != null)
                _lobbyRootObj.SetActive(state == ScreenState.Lobby);
            if (_battleRootObj != null)
                _battleRootObj.SetActive(state == ScreenState.Battle);

            if (state == ScreenState.Lobby)
            {
                RefreshLobbyHeroUI();
                RefreshHeaderAndProgressionUI();
            }
            else if (state == ScreenState.Battle)
            {
                RefreshTurnAndScoreUI();
                RefreshBottomActionBadges();
            }

            UpdateResponsiveLayout(force: true);
        }

        public void ReturnToLobby()
        {
            PlaySfx("click");
            SetScreenState(ScreenState.Lobby);
        }

        // =====================================================================
        // SCREEN 1: LOBBY / MAIN MENU UI
        // =====================================================================

        private void BuildLobbyScreen(RectTransform parent)
        {
            // 1. Top Bar (Coin pill, Streak pill, Round sound toggle)
            GameObject headerCard = CreateUIObject("LobbyHeaderCard", parent);
            _headerCardRT = headerCard.GetComponent<RectTransform>();
            BuildLobbyHeaderContent(_headerCardRT);

            // 2. Hero Card (Title "Caro 5", 5x5 mini board, Rank row, Big Mint Play, Mode & Rival chips)
            GameObject heroCard = CreateUIObject("LobbyHeroCard", parent);
            _lobbyHeroCardRT = heroCard.GetComponent<RectTransform>();
            _lobbyHeroCardImg = heroCard.AddComponent<Image>();
            _lobbyHeroCardImg.sprite = XOProceduralAssets.CardPanelSprite;
            _lobbyHeroCardImg.type = Image.Type.Sliced;
            _lobbyHeroCardImg.color = XOProceduralAssets.ColCardBg;
            BuildLobbyHeroContent(_lobbyHeroCardRT);

            // 3. Three Equal Tiles Under Hero (Rivals, Spin with badge "Free", Skins)
            GameObject hubGrid = CreateUIObject("LobbyHubGrid", parent);
            _lobbyHubGridRT = hubGrid.GetComponent<RectTransform>();
            BuildLobbyHubContent(_lobbyHubGridRT);

            // 4. Daily Quest Card (Text, Progress bar, Chest button)
            GameObject questCard = CreateUIObject("LobbyQuestCard", parent);
            _questCardRT = questCard.GetComponent<RectTransform>();
            _questCardImg = questCard.AddComponent<Image>();
            _questCardImg.sprite = XOProceduralAssets.CardPanelSprite;
            _questCardImg.type = Image.Type.Sliced;
            _questCardImg.color = XOProceduralAssets.ColCardBg;
            BuildLobbyQuestAndChestContent(_questCardRT);
        }

        private void BuildLobbyHeaderContent(RectTransform parent)
        {
            // --- Coin Pill (White Translucent Capsule) ---
            GameObject coinPill = CreateUIObject("CoinPill", parent);
            RectTransform coinRT = coinPill.GetComponent<RectTransform>();
            coinRT.anchorMin = new Vector2(0f, 0.5f);
            coinRT.anchorMax = new Vector2(0f, 0.5f);
            coinRT.pivot = new Vector2(0f, 0.5f);
            coinRT.anchoredPosition = new Vector2(0f, 0f);
            coinRT.sizeDelta = new Vector2(190f, 56f);

            Image coinPillBg = coinPill.AddComponent<Image>();
            coinPillBg.sprite = XOProceduralAssets.Pill3DSprite;
            coinPillBg.type = Image.Type.Sliced;
            coinPillBg.color = Color.white;

            CreateIconImage(coinRT, XOProceduralAssets.IconCoin, new Vector2(0f, 0.5f), new Vector2(30f, 0f), new Vector2(38f, 38f));
            _coinText = CreateText(coinRT, $"{Coins:N0}", 22, FontStyle.Bold, TextAnchor.MiddleLeft);
            _coinText.rectTransform.offsetMin = new Vector2(56f, 0f);
            _coinText.rectTransform.offsetMax = new Vector2(-12f, 0f);
            _coinText.color = XOProceduralAssets.ColInk;

            // --- Streak Pill (White Translucent Capsule) ---
            GameObject streakPill = CreateUIObject("StreakPill", parent);
            RectTransform streakRT = streakPill.GetComponent<RectTransform>();
            streakRT.anchorMin = new Vector2(0f, 0.5f);
            streakRT.anchorMax = new Vector2(0f, 0.5f);
            streakRT.pivot = new Vector2(0f, 0.5f);
            streakRT.anchoredPosition = new Vector2(206f, 0f);
            streakRT.sizeDelta = new Vector2(180f, 56f);

            Image streakPillBg = streakPill.AddComponent<Image>();
            streakPillBg.sprite = XOProceduralAssets.Pill3DSprite;
            streakPillBg.type = Image.Type.Sliced;
            streakPillBg.color = Color.white;

            CreateIconImage(streakRT, XOProceduralAssets.IconFlame, new Vector2(0f, 0.5f), new Vector2(28f, 0f), new Vector2(36f, 36f));
            _streakText = CreateText(streakRT, $"{XOProgressionSystem.WinStreak} streak", 20, FontStyle.Bold, TextAnchor.MiddleLeft);
            _streakText.rectTransform.offsetMin = new Vector2(54f, 0f);
            _streakText.rectTransform.offsetMax = new Vector2(-12f, 0f);
            _streakText.color = XOProceduralAssets.ColInk;

            // --- Sound Toggle (Round 56x56 Clay Button) ---
            GameObject muteBtnObj = CreateUIObject("MuteButton", parent);
            RectTransform muteRT = muteBtnObj.GetComponent<RectTransform>();
            muteRT.anchorMin = new Vector2(1f, 0.5f);
            muteRT.anchorMax = new Vector2(1f, 0.5f);
            muteRT.pivot = new Vector2(1f, 0.5f);
            muteRT.anchoredPosition = new Vector2(0f, 0f);
            muteRT.sizeDelta = new Vector2(56f, 56f);

            Image muteBg = muteBtnObj.AddComponent<Image>();
            muteBg.sprite = XOProceduralAssets.Button3DSprite;
            muteBg.type = Image.Type.Sliced;
            muteBg.color = Color.white;

            Button muteBtn = muteBtnObj.AddComponent<Button>();
            _muteButtonIconImg = CreateIconImage(muteRT, IsSoundMuted ? XOProceduralAssets.IconSoundOff : XOProceduralAssets.IconSoundOn, new Vector2(0.5f, 0.5f), new Vector2(0f, 1f), new Vector2(36f, 36f));
            muteBtn.onClick.AddListener(() =>
            {
                IsSoundMuted = !IsSoundMuted;
                PlaySfx("click");
            });
        }

        private void RefreshLobbyHeroUI()
        {
            if (_lobbyModeBannerTitle != null)
                _lobbyModeBannerTitle.text = "Caro 5";
            if (_lobbyModeBannerDesc != null)
                _lobbyModeBannerDesc.text = "Classic five in a row";
        }

        private void BuildLobbyHeroContent(RectTransform parent)
        {
            // Title: "Caro 5" (Sentence case, bold ink #2A3566, tight tracking)
            _lobbyModeBannerTitle = CreateText(parent, "Caro 5", 40, FontStyle.Bold, TextAnchor.MiddleCenter);
            _lobbyModeBannerTitle.rectTransform.anchorMin = new Vector2(0f, 1f);
            _lobbyModeBannerTitle.rectTransform.anchorMax = new Vector2(1f, 1f);
            _lobbyModeBannerTitle.rectTransform.pivot = new Vector2(0.5f, 1f);
            _lobbyModeBannerTitle.rectTransform.anchoredPosition = new Vector2(0f, -14f);
            _lobbyModeBannerTitle.rectTransform.sizeDelta = new Vector2(-40f, 48f);
            _lobbyModeBannerTitle.color = XOProceduralAssets.ColInk;

            // Subtitle: "Classic five in a row"
            _lobbyModeBannerDesc = CreateText(parent, "Classic five in a row", 17, FontStyle.Normal, TextAnchor.MiddleCenter);
            _lobbyModeBannerDesc.rectTransform.anchorMin = new Vector2(0f, 1f);
            _lobbyModeBannerDesc.rectTransform.anchorMax = new Vector2(1f, 1f);
            _lobbyModeBannerDesc.rectTransform.pivot = new Vector2(0.5f, 1f);
            _lobbyModeBannerDesc.rectTransform.anchoredPosition = new Vector2(0f, -60f);
            _lobbyModeBannerDesc.rectTransform.sizeDelta = new Vector2(-60f, 28f);
            _lobbyModeBannerDesc.color = XOProceduralAssets.ColMuted;

            // Hero Emblem / Badge Centerpiece (Replaces confusing fake 5x5 pre-placed board!)
            GameObject emblemObj = CreateUIObject("HeroEmblemBox", parent);
            RectTransform ebRT = emblemObj.GetComponent<RectTransform>();
            ebRT.anchorMin = new Vector2(0.5f, 0.5f);
            ebRT.anchorMax = new Vector2(0.5f, 0.5f);
            ebRT.pivot = new Vector2(0.5f, 0.5f);
            ebRT.anchoredPosition = new Vector2(0f, 48f);
            ebRT.sizeDelta = new Vector2(200f, 200f);

            Image ebBg = emblemObj.AddComponent<Image>();
            ebBg.sprite = XOProceduralAssets.SoftCellSprite;
            ebBg.type = Image.Type.Sliced;
            ebBg.color = XOThemeDatabase.Hex("#F0F9FF"); // Soft Sky porcelain

            // Large 3D Crossed Swords & Golden Crown Hero Icon
            CreateIconImage(ebRT, XOProceduralAssets.IconSwords, new Vector2(0.5f, 0.5f), new Vector2(0f, -6f), new Vector2(108f, 108f));
            CreateIconImage(ebRT, XOProceduralAssets.IconCrown, new Vector2(0.5f, 1f), new Vector2(0f, -22f), new Vector2(50f, 50f));

            // Rank Row: 4 Dots + "Silver master"
            GameObject rankRow = CreateUIObject("RankRow", parent);
            RectTransform rankRT = rankRow.GetComponent<RectTransform>();
            rankRT.anchorMin = new Vector2(0.5f, 0.5f);
            rankRT.anchorMax = new Vector2(0.5f, 0.5f);
            rankRT.pivot = new Vector2(0.5f, 0.5f);
            rankRT.anchoredPosition = new Vector2(0f, -68f);
            rankRT.sizeDelta = new Vector2(320f, 34f);

            // 4 Dots
            for (int i = 0; i < 4; i++)
            {
                GameObject dotObj = CreateUIObject($"Dot_{i}", rankRT);
                RectTransform dotRT = dotObj.GetComponent<RectTransform>();
                dotRT.anchorMin = new Vector2(0f, 0.5f);
                dotRT.anchorMax = new Vector2(0f, 0.5f);
                dotRT.pivot = new Vector2(0f, 0.5f);
                dotRT.anchoredPosition = new Vector2(18f + i * 18f, 0f);
                dotRT.sizeDelta = new Vector2(13f, 13f);

                Image dotImg = dotObj.AddComponent<Image>();
                dotImg.sprite = XOProceduralAssets.CircleSprite;
                dotImg.color = (i < 3) ? XOProceduralAssets.ColSun : new Color(0.16f, 0.21f, 0.40f, 0.16f);
            }

            _rankBadgeText = CreateText(rankRT, "Bronze Novice (1/3)", 18, FontStyle.Bold, TextAnchor.MiddleLeft);
            _rankBadgeText.rectTransform.offsetMin = new Vector2(96f, 0f);
            _rankBadgeText.rectTransform.offsetMax = new Vector2(0f, 0f);
            _rankBadgeText.color = XOProceduralAssets.ColInk;

            // Big Mint Play Button (82px tall, #4ED6A4 with #2EA375 bottom edge)
            _lobbyPlayBtn = CreateIcon3DButton(parent, XOProceduralAssets.IconNewGame, "PLAY MATCH (15x15)", new Vector2(460f, 82f), XOProceduralAssets.ColMint, XOProceduralAssets.ColInk, 26);
            RectTransform playRT = _lobbyPlayBtn.GetComponent<RectTransform>();
            playRT.anchorMin = new Vector2(0.5f, 0f);
            playRT.anchorMax = new Vector2(0.5f, 0f);
            playRT.pivot = new Vector2(0.5f, 0f);
            playRT.anchoredPosition = new Vector2(0f, 86f);
            _lobbyPlayBtn.onClick.AddListener(() =>
            {
                PlaySfx("click");
                StartNewMatch(currentBoardSize);
                SetScreenState(ScreenState.Battle);
            });

            // Two Half-Width Chips Below Play: Mode (Sky) and Rival (Lilac)
            GameObject chipsContainer = CreateUIObject("ChipsRow", parent);
            RectTransform chipsRT = chipsContainer.GetComponent<RectTransform>();
            chipsRT.anchorMin = new Vector2(0.5f, 0f);
            chipsRT.anchorMax = new Vector2(0.5f, 0f);
            chipsRT.pivot = new Vector2(0.5f, 0f);
            chipsRT.anchoredPosition = new Vector2(0f, 14f);
            chipsRT.sizeDelta = new Vector2(460f, 62f);

            // Left Chip: Mode (Sky #4FB0FF)
            _lobbyChangeModeBtn = CreateTwoLineChipButton(chipsRT, "Mode", "Pro 15x15", XOProceduralAssets.ColSky, new Vector2(222f, 62f), () =>
            {
                PlaySfx("click");
                _modeSelectModalRoot.SetActive(true);
            }, out Text modeValueTxt);
            RectTransform modeChipRT = _lobbyChangeModeBtn.GetComponent<RectTransform>();
            modeChipRT.anchorMin = new Vector2(0f, 0.5f);
            modeChipRT.anchorMax = new Vector2(0f, 0.5f);
            modeChipRT.pivot = new Vector2(0f, 0.5f);
            modeChipRT.anchoredPosition = new Vector2(0f, 0f);

            // Right Chip: Rival (Lilac #A998FF)
            _lobbyChangeRivalBtn = CreateTwoLineChipButton(chipsRT, "Rival", "Master Leo", XOProceduralAssets.ColLilac, new Vector2(222f, 62f), () =>
            {
                OpenLeaderboardModal();
            }, out _lobbyRivalBannerText);
            RectTransform rivalChipRT = _lobbyChangeRivalBtn.GetComponent<RectTransform>();
            rivalChipRT.anchorMin = new Vector2(1f, 0.5f);
            rivalChipRT.anchorMax = new Vector2(1f, 0.5f);
            rivalChipRT.pivot = new Vector2(1f, 0.5f);
            rivalChipRT.anchoredPosition = new Vector2(0f, 0f);
        }

        private void BuildLobbyHubContent(RectTransform parent)
        {
            GridLayoutGroup grid = parent.gameObject.GetComponent<GridLayoutGroup>();
            if (grid == null) grid = parent.gameObject.AddComponent<GridLayoutGroup>();
            _lobbyHubGridLayout = grid;
            grid.childAlignment = TextAnchor.MiddleCenter;
            grid.spacing = new Vector2(16f, 14f);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 2;
            grid.startCorner = GridLayoutGroup.Corner.UpperLeft;
            grid.startAxis = GridLayoutGroup.Axis.Horizontal;

            Vector2 tileDimen = new Vector2(246f, 82f);
            grid.cellSize = tileDimen;

            // 1. Rivals Tile
            _lobbyHubArenaBtn = CreateIcon3DButton(parent, XOProceduralAssets.IconLeaderboard, "Rivals", tileDimen, Color.white, XOProceduralAssets.ColInk, 18);
            _lobbyHubArenaBtn.onClick.AddListener(OpenLeaderboardModal);
            _leaderboardBtn = _lobbyHubArenaBtn;

            // Rivals Badge (ELO / Rank)
            RectTransform arenaRT = _lobbyHubArenaBtn.GetComponent<RectTransform>();
            GameObject rivalsBadge = CreateUIObject("BadgeRivals", arenaRT);
            RectTransform rbRT = rivalsBadge.GetComponent<RectTransform>();
            rbRT.anchorMin = new Vector2(1f, 1f);
            rbRT.anchorMax = new Vector2(1f, 1f);
            rbRT.pivot = new Vector2(1f, 1f);
            rbRT.anchoredPosition = new Vector2(-6f, -6f);
            rbRT.sizeDelta = new Vector2(56f, 26f);
            Image rbBg = rivalsBadge.AddComponent<Image>();
            rbBg.sprite = XOProceduralAssets.PillSprite;
            rbBg.type = Image.Type.Sliced;
            rbBg.color = XOProceduralAssets.ColSky;
            rbBg.raycastTarget = false;
            _leaderboardBadgeBg = rbBg;
            _leaderboardBadgeText = CreateText(rbRT, "RANK", 13, FontStyle.Bold, TextAnchor.MiddleCenter);
            _leaderboardBadgeText.color = Color.white;

            // 2. Tower Climb Tile (Vượt Tháp 10 Tầng & Trang Bị Boss)
            _lobbyHubTowerBtn = CreateIcon3DButton(parent, XOProceduralAssets.IconCrown, "Tower", tileDimen, Color.white, XOProceduralAssets.ColInk, 18);
            _lobbyHubTowerBtn.onClick.AddListener(OpenTowerModal);

            // Floor Badge on Tower
            RectTransform towerRT = _lobbyHubTowerBtn.GetComponent<RectTransform>();
            GameObject towerBadge = CreateUIObject("BadgeTower", towerRT);
            RectTransform tbRT = towerBadge.GetComponent<RectTransform>();
            tbRT.anchorMin = new Vector2(1f, 1f);
            tbRT.anchorMax = new Vector2(1f, 1f);
            tbRT.pivot = new Vector2(1f, 1f);
            tbRT.anchoredPosition = new Vector2(-6f, -6f);
            tbRT.sizeDelta = new Vector2(52f, 26f);
            Image tbBg = towerBadge.AddComponent<Image>();
            tbBg.sprite = XOProceduralAssets.PillSprite;
            tbBg.type = Image.Type.Sliced;
            tbBg.color = XOProceduralAssets.ColLilac;
            tbBg.raycastTarget = false;

            Text tbText = CreateText(tbRT, $"F.{XORoguelikeRunManager.CurrentFloor}", 13, FontStyle.Bold, TextAnchor.MiddleCenter);
            tbText.color = Color.white;
            _towerBadgeText = tbText;

            // 3. Spin Tile with Badge "Free"
            _lobbyHubWheelBtn = CreateIcon3DButton(parent, XOProceduralAssets.IconLuckyWheel, "Spin", tileDimen, Color.white, XOProceduralAssets.ColInk, 18);
            _lobbyHubWheelBtn.onClick.AddListener(OpenLuckyWheelModal);

            // Badge "Free" in coral #FF6F85
            RectTransform spinRT = _lobbyHubWheelBtn.GetComponent<RectTransform>();
            GameObject freeBadge = CreateUIObject("BadgeFree", spinRT);
            RectTransform fbRT = freeBadge.GetComponent<RectTransform>();
            fbRT.anchorMin = new Vector2(1f, 1f);
            fbRT.anchorMax = new Vector2(1f, 1f);
            fbRT.pivot = new Vector2(1f, 1f);
            fbRT.anchoredPosition = new Vector2(-6f, -6f);
            fbRT.sizeDelta = new Vector2(52f, 26f);
            Image fbBg = freeBadge.AddComponent<Image>();
            fbBg.sprite = XOProceduralAssets.PillSprite;
            fbBg.type = Image.Type.Sliced;
            fbBg.color = XOProceduralAssets.ColCoral;
            fbBg.raycastTarget = false;

            Text fbText = CreateText(fbRT, "Free", 13, FontStyle.Bold, TextAnchor.MiddleCenter);
            fbText.color = Color.white;

            // 4. Skins Tile
            _lobbyHubShopBtn = CreateIcon3DButton(parent, XOProceduralAssets.IconSkinShop, "Skins", tileDimen, Color.white, XOProceduralAssets.ColInk, 18);
            _lobbyHubShopBtn.onClick.AddListener(OpenThemeShopModal);
            _skinShopBtn = _lobbyHubShopBtn;
            _lobbyHubModeBtn = _lobbyChangeModeBtn;

            // Badge "Shop" on Skins
            RectTransform skinsRT = _lobbyHubShopBtn.GetComponent<RectTransform>();
            GameObject shopBadge = CreateUIObject("BadgeSkins", skinsRT);
            RectTransform sbRT = shopBadge.GetComponent<RectTransform>();
            sbRT.anchorMin = new Vector2(1f, 1f);
            sbRT.anchorMax = new Vector2(1f, 1f);
            sbRT.pivot = new Vector2(1f, 1f);
            sbRT.anchoredPosition = new Vector2(-6f, -6f);
            sbRT.sizeDelta = new Vector2(52f, 26f);
            Image sbBg = shopBadge.AddComponent<Image>();
            sbBg.sprite = XOProceduralAssets.PillSprite;
            sbBg.type = Image.Type.Sliced;
            sbBg.color = XOProceduralAssets.ColMint;
            sbBg.raycastTarget = false;

            Text sbText = CreateText(sbRT, "Shop", 13, FontStyle.Bold, TextAnchor.MiddleCenter);
            sbText.color = XOProceduralAssets.ColInk;
        }

        private void OnClaimWinChestClicked()
        {
            PlaySfx("chest");
            Coins += 5;
            RefreshHeaderAndProgressionUI();
        }

        private void BuildLobbyQuestAndChestContent(RectTransform parent)
        {
            // Left Column: "Daily quest", "Win 2 matches (1/2)", and progress bar
            _missionText = CreateText(parent, "Daily quest • Win 2 matches", 18, FontStyle.Bold, TextAnchor.MiddleLeft);
            _missionText.rectTransform.anchorMin = new Vector2(0f, 0.5f);
            _missionText.rectTransform.anchorMax = new Vector2(1f, 0.5f);
            _missionText.rectTransform.pivot = new Vector2(0f, 0.5f);
            _missionText.rectTransform.anchoredPosition = new Vector2(24f, 16f);
            _missionText.rectTransform.sizeDelta = new Vector2(-120f, 26f);
            _missionText.color = XOProceduralAssets.ColInk;

            // Progress Track Bar
            GameObject trackObj = CreateUIObject("ProgressTrack", parent);
            RectTransform trackRT = trackObj.GetComponent<RectTransform>();
            trackRT.anchorMin = new Vector2(0f, 0.5f);
            trackRT.anchorMax = new Vector2(1f, 0.5f);
            trackRT.pivot = new Vector2(0f, 0.5f);
            trackRT.anchoredPosition = new Vector2(24f, -16f);
            trackRT.sizeDelta = new Vector2(-120f, 12f);

            Image trackBg = trackObj.AddComponent<Image>();
            trackBg.sprite = XOProceduralAssets.PillSprite;
            trackBg.type = Image.Type.Sliced;
            trackBg.color = new Color(0.16f, 0.21f, 0.40f, 0.08f);

            GameObject fillObj = CreateUIObject("ProgressFill", trackRT);
            RectTransform fillRT = fillObj.GetComponent<RectTransform>();
            fillRT.anchorMin = new Vector2(0f, 0f);
            fillRT.anchorMax = new Vector2(0.5f, 1f); // 50% progress (1/2)
            fillRT.offsetMin = Vector2.zero;
            fillRT.offsetMax = Vector2.zero;

            Image fillImg = fillObj.AddComponent<Image>();
            fillImg.sprite = XOProceduralAssets.PillSprite;
            fillImg.type = Image.Type.Sliced;
            fillImg.color = XOProceduralAssets.ColMint;

            // Right Button: Chest Button with IconChest
            _chestButton = CreateIcon3DButton(parent, XOProceduralAssets.IconChest, "", new Vector2(70f, 70f), XOProceduralAssets.ColSun, Color.white, 1);
            RectTransform chestRT = _chestButton.GetComponent<RectTransform>();
            chestRT.anchorMin = new Vector2(1f, 0.5f);
            chestRT.anchorMax = new Vector2(1f, 0.5f);
            chestRT.pivot = new Vector2(1f, 0.5f);
            chestRT.anchoredPosition = new Vector2(-18f, 0f);
            _chestButton.onClick.AddListener(OnClaimWinChestClicked);
        }

        private Button CreateTwoLineChipButton(RectTransform parent, string label, string value, Color bgCol, Vector2 size, UnityEngine.Events.UnityAction onClick, out Text valueTxt)
        {
            GameObject btnObj = CreateUIObject($"Chip_{label}", parent);
            RectTransform rt = btnObj.GetComponent<RectTransform>();
            rt.sizeDelta = size;

            Image bg = btnObj.AddComponent<Image>();
            bg.sprite = XOProceduralAssets.Button3DSprite;
            bg.type = Image.Type.Sliced;
            bg.color = bgCol;

            Button btn = btnObj.AddComponent<Button>();
            if (onClick != null) btn.onClick.AddListener(onClick);

            Text lbl = CreateText(rt, label, 11, FontStyle.Normal, TextAnchor.MiddleCenter);
            lbl.rectTransform.anchorMin = new Vector2(0f, 0.5f);
            lbl.rectTransform.anchorMax = new Vector2(1f, 0.5f);
            lbl.rectTransform.anchoredPosition = new Vector2(0f, 10f);
            lbl.rectTransform.sizeDelta = new Vector2(0f, 18f);
            lbl.color = new Color(0.16f, 0.21f, 0.40f, 0.75f);

            valueTxt = CreateText(rt, value, 14, FontStyle.Bold, TextAnchor.MiddleCenter);
            valueTxt.rectTransform.anchorMin = new Vector2(0f, 0.5f);
            valueTxt.rectTransform.anchorMax = new Vector2(1f, 0.5f);
            valueTxt.rectTransform.anchoredPosition = new Vector2(0f, -8f);
            valueTxt.rectTransform.sizeDelta = new Vector2(0f, 20f);
            valueTxt.color = XOProceduralAssets.ColInk;

            return btn;
        }

        // =====================================================================
        // SCREEN 2: BATTLE SCREEN (FOCUSED GAMEPLAY ARENA)
        // =====================================================================

        private void BuildBattleScreen(RectTransform parent)
        {
            // 1. Top HUD ([Home], Turn, Timer, Player cards, Status banner)
            GameObject topHud = CreateUIObject("BattleTopHUD", parent);
            _battleTopHudRT = topHud.GetComponent<RectTransform>();
            BuildBattleTopHud(_battleTopHudRT);

            // 1B. Active Relic Tray (Tower Jokers Bar)
            BuildBattleRelicBar(parent);

            // 2. Center Spacious Caro Board
            GameObject centerBoard = CreateUIObject("BoardOuterFrame", parent);
            _boardOuterFrameRT = centerBoard.GetComponent<RectTransform>();
            _boardOuterFrameImg = centerBoard.AddComponent<Image>();
            _boardOuterFrameImg.sprite = XOProceduralAssets.CardPanelSprite;
            _boardOuterFrameImg.type = Image.Type.Sliced;
            _boardOuterFrameImg.color = XOProceduralAssets.ColCardBg;
            BuildBattleCenterBoard(_boardOuterFrameRT);

            // 3. Bottom Tactile Action Bar (6 Compact Skills)
            GameObject bottomBar = CreateUIObject("BattleBottomBar", parent);
            _battleBottomBarRT = bottomBar.GetComponent<RectTransform>();
            BuildBattleBottomBar(_battleBottomBarRT);
        }

        private void BuildBattleTopHud(RectTransform parent)
        {
            // [ ⌂ Home ] Button
            _battleHomeBtn = CreateIcon3DButton(parent, XOProceduralAssets.IconHome, "Home", new Vector2(136f, 56f), Color.white, XOProceduralAssets.ColInk, 19);
            RectTransform homeRT = _battleHomeBtn.GetComponent<RectTransform>();
            homeRT.anchorMin = new Vector2(0f, 1f);
            homeRT.anchorMax = new Vector2(0f, 1f);
            homeRT.pivot = new Vector2(0f, 1f);
            homeRT.anchoredPosition = new Vector2(0f, 0f);
            _battleHomeBtn.onClick.AddListener(ReturnToLobby);

            // Score Section (Player X vs Bot O)
            GameObject scoreSec = CreateUIObject("ScoreSection", parent);
            RectTransform scoreSecRT = scoreSec.GetComponent<RectTransform>();
            scoreSecRT.anchorMin = new Vector2(0f, 1f);
            scoreSecRT.anchorMax = new Vector2(1f, 1f);
            scoreSecRT.pivot = new Vector2(0.5f, 1f);
            scoreSecRT.anchoredPosition = new Vector2(76f, 0f);
            scoreSecRT.sizeDelta = new Vector2(-160f, 56f);

            // Player X Card (Sky #4FB0FF)
            GameObject cardX = CreateUIObject("PlayerXCard", scoreSecRT);
            RectTransform cardXRT = cardX.GetComponent<RectTransform>();
            cardXRT.anchorMin = new Vector2(0f, 0f);
            cardXRT.anchorMax = new Vector2(0.48f, 1f);
            cardXRT.offsetMin = Vector2.zero;
            cardXRT.offsetMax = Vector2.zero;

            _playerXCardImg = cardX.AddComponent<Image>();
            _playerXCardImg.sprite = XOProceduralAssets.Pill3DSprite;
            _playerXCardImg.type = Image.Type.Sliced;
            _playerXCardImg.color = XOProceduralAssets.ColSky;

            _playerXTokenIcon = CreateIconImage(cardXRT, XOProceduralAssets.XPieceSprite, new Vector2(0f, 0.5f), new Vector2(28f, 0f), new Vector2(38f, 38f));
            _playerXTitleText = CreateText(cardXRT, "YOU (X)", 18, FontStyle.Bold, TextAnchor.MiddleLeft);
            _playerXTitleText.rectTransform.offsetMin = new Vector2(54f, 0f);
            _playerXTitleText.rectTransform.offsetMax = new Vector2(-54f, 0f);
            _playerXTitleText.color = XOProceduralAssets.ColInk;

            _scoreXText = CreateText(cardXRT, "0", 26, FontStyle.Bold, TextAnchor.MiddleRight);
            _scoreXText.rectTransform.offsetMin = new Vector2(-54f, 0f);
            _scoreXText.rectTransform.offsetMax = new Vector2(-14f, 0f);
            _scoreXText.color = XOProceduralAssets.ColInk;

            // Player O Card (Coral #FF6F85)
            GameObject cardO = CreateUIObject("PlayerOCard", scoreSecRT);
            RectTransform cardORT = cardO.GetComponent<RectTransform>();
            cardORT.anchorMin = new Vector2(0.52f, 0f);
            cardORT.anchorMax = new Vector2(1f, 1f);
            cardORT.offsetMin = Vector2.zero;
            cardORT.offsetMax = Vector2.zero;

            _playerOCardImg = cardO.AddComponent<Image>();
            _playerOCardImg.sprite = XOProceduralAssets.Pill3DSprite;
            _playerOCardImg.type = Image.Type.Sliced;
            _playerOCardImg.color = XOProceduralAssets.ColCoral;

            _playerOTokenIcon = CreateIconImage(cardORT, XOProceduralAssets.OPieceSprite, new Vector2(0f, 0.5f), new Vector2(28f, 0f), new Vector2(38f, 38f));
            _playerOTitleText = CreateText(cardORT, "BOT (O)", 18, FontStyle.Bold, TextAnchor.MiddleLeft);
            _playerOTitleText.rectTransform.offsetMin = new Vector2(54f, 0f);
            _playerOTitleText.rectTransform.offsetMax = new Vector2(-54f, 0f);
            _playerOTitleText.color = Color.white;

            _scoreOText = CreateText(cardORT, "0", 26, FontStyle.Bold, TextAnchor.MiddleRight);
            _scoreOText.rectTransform.offsetMin = new Vector2(-54f, 0f);
            _scoreOText.rectTransform.offsetMax = new Vector2(-14f, 0f);
            _scoreOText.color = Color.white;

            // Turn Timer Bar
            GameObject timerTrack = CreateUIObject("TurnTimerTrack", parent);
            RectTransform ttRT = timerTrack.GetComponent<RectTransform>();
            ttRT.anchorMin = new Vector2(0f, 1f);
            ttRT.anchorMax = new Vector2(1f, 1f);
            ttRT.pivot = new Vector2(0.5f, 1f);
            ttRT.anchoredPosition = new Vector2(0f, -66f);
            ttRT.sizeDelta = new Vector2(0f, 8f);

            Image ttBg = timerTrack.AddComponent<Image>();
            ttBg.sprite = XOProceduralAssets.PillSprite;
            ttBg.type = Image.Type.Sliced;
            ttBg.color = new Color(0.16f, 0.21f, 0.40f, 0.12f);

            GameObject timerFill = CreateUIObject("TurnTimerFill", ttRT);
            RectTransform tfRT = timerFill.GetComponent<RectTransform>();
            StretchFull(tfRT);
            _turnTimerFillImg = timerFill.AddComponent<Image>();
            _turnTimerFillImg.sprite = XOProceduralAssets.PillSprite;
            _turnTimerFillImg.type = Image.Type.Filled;
            _turnTimerFillImg.fillMethod = Image.FillMethod.Horizontal;
            _turnTimerFillImg.fillAmount = 1f;
            _turnTimerFillImg.color = XOProceduralAssets.ColMint;

            // Status Banner
            GameObject statusObj = CreateUIObject("StatusBanner", parent);
            RectTransform sbRT = statusObj.GetComponent<RectTransform>();
            sbRT.anchorMin = new Vector2(0f, 1f);
            sbRT.anchorMax = new Vector2(1f, 1f);
            sbRT.pivot = new Vector2(0.5f, 1f);
            sbRT.anchoredPosition = new Vector2(0f, -80f);
            sbRT.sizeDelta = new Vector2(0f, 40f);

            _statusBannerImg = statusObj.AddComponent<Image>();
            _statusBannerImg.sprite = XOProceduralAssets.PillSprite;
            _statusBannerImg.type = Image.Type.Sliced;
            _statusBannerImg.color = Color.white;

            _statusBannerText = CreateText(sbRT, "Your Turn (X) • Connect 5 to win!", 18, FontStyle.Bold, TextAnchor.MiddleCenter);
            _statusBannerText.color = XOProceduralAssets.ColInk;
        }

        private void BuildBattleRelicBar(RectTransform parent)
        {
            GameObject barObj = CreateUIObject("BattleRelicBar", parent);
            _battleRelicBarRT = barObj.GetComponent<RectTransform>();
            _battleRelicBarRT.anchorMin = new Vector2(0.5f, 1f);
            _battleRelicBarRT.anchorMax = new Vector2(0.5f, 1f);
            _battleRelicBarRT.pivot = new Vector2(0.5f, 1f);
            _battleRelicBarRT.anchoredPosition = new Vector2(0f, -142f);
            _battleRelicBarRT.sizeDelta = new Vector2(800f, 44f);

            HorizontalLayoutGroup hLayout = barObj.AddComponent<HorizontalLayoutGroup>();
            hLayout.childAlignment = TextAnchor.MiddleCenter;
            hLayout.spacing = 10f;
            hLayout.childControlWidth = true;
            hLayout.childControlHeight = true;
            hLayout.childForceExpandWidth = true;
            hLayout.childForceExpandHeight = true;

            _battleRelicSlotButtons.Clear();
            _battleRelicSlotIcons.Clear();
            _battleRelicSlotTexts.Clear();

            for (int i = 0; i < XORoguelikeRunManager.MaxRelicSlots; i++)
            {
                int slotIdx = i;
                GameObject slotObj = CreateUIObject($"RelicSlot_{i}", _battleRelicBarRT);
                Image slotBg = slotObj.AddComponent<Image>();
                slotBg.sprite = XOProceduralAssets.PillSprite;
                slotBg.type = Image.Type.Sliced;
                slotBg.color = XOProceduralAssets.ColTileEdge;

                Button slotBtn = slotObj.AddComponent<Button>();
                _battleRelicSlotButtons.Add(slotBtn);

                RectTransform sRT = slotObj.GetComponent<RectTransform>();
                Image ic = CreateIconImage(sRT, XOProceduralAssets.IconRelic, new Vector2(0f, 0.5f), new Vector2(20f, 0f), new Vector2(28f, 28f));
                _battleRelicSlotIcons.Add(ic);

                Text txt = CreateText(sRT, $"Slot {i + 1}", 13, FontStyle.Bold, TextAnchor.MiddleLeft);
                txt.rectTransform.offsetMin = new Vector2(38f, 0f);
                txt.rectTransform.offsetMax = new Vector2(-6f, 0f);
                txt.color = XOProceduralAssets.ColInk;
                _battleRelicSlotTexts.Add(txt);

                slotBtn.onClick.AddListener(() =>
                {
                    if (slotIdx < XORoguelikeRunManager.EquippedRelics.Count)
                    {
                        CaroRelicType type = XORoguelikeRunManager.EquippedRelics[slotIdx];
                        CaroRelic r = XORoguelikeRunManager.GetRelicData(type);
                        if (r != null)
                        {
                            PlaySfx("click");
                            SpawnTacticalCalloutBanner($"[RELIC: {r.name.ToUpper()}]", r.description, r.GetRarityColor());
                        }
                    }
                });
            }

            barObj.SetActive(false);
        }

        public void RefreshBattleRelicBarUI()
        {
            if (_battleRelicBarRT == null) return;

            bool isTower = (currentGameMode == GameMode.TowerClimb);
            _battleRelicBarRT.gameObject.SetActive(isTower);
            if (!isTower) return;

            for (int i = 0; i < XORoguelikeRunManager.MaxRelicSlots; i++)
            {
                if (i >= _battleRelicSlotButtons.Count) break;

                Button btn = _battleRelicSlotButtons[i];
                Image ic = _battleRelicSlotIcons[i];
                Text txt = _battleRelicSlotTexts[i];
                Image bg = btn.GetComponent<Image>();

                if (i < XORoguelikeRunManager.EquippedRelics.Count)
                {
                    CaroRelicType type = XORoguelikeRunManager.EquippedRelics[i];
                    CaroRelic r = XORoguelikeRunManager.GetRelicData(type);
                    bool isDisabled = (XORoguelikeRunManager.DisabledRelicThisMatch.HasValue && XORoguelikeRunManager.DisabledRelicThisMatch.Value == type);

                    if (r != null)
                    {
                        ic.gameObject.SetActive(true);
                        ic.sprite = r.GetIcon();
                        ic.color = isDisabled ? Color.gray : r.GetRarityColor();
                        txt.text = isDisabled ? $"{r.name} (LOCKED)" : r.name;
                        txt.color = isDisabled ? XOProceduralAssets.ColCoral : XOProceduralAssets.ColInk;
                        bg.color = isDisabled ? XOThemeDatabase.Hex("#FEE2E2") : Color.white;
                    }
                }
                else
                {
                    ic.gameObject.SetActive(false);
                    txt.text = $"[Empty {i + 1}]";
                    txt.color = new Color(0.6f, 0.65f, 0.75f);
                    bg.color = XOThemeDatabase.Hex("#F8FAFC");
                }
            }
        }

        private void BuildBattleCenterBoard(RectTransform parent)
        {
            GameObject gridObj = CreateUIObject("BoardGrid", parent);
            _boardGridContainerRT = gridObj.GetComponent<RectTransform>();
            StretchFull(_boardGridContainerRT);

            _boardGridLayout = gridObj.AddComponent<GridLayoutGroup>();
            _boardGridLayout.childAlignment = TextAnchor.MiddleCenter;
            _boardGridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            _boardGridLayout.constraintCount = currentBoardSize;
        }

        private void BuildBattleBottomBar(RectTransform parent)
        {
            float btnW = 154f;
            float btnH = 118f;

            // 1. Hint (Sun #FFCF5C)
            _hintBtn = CreateCompactBattleSkillButton(parent, XOProceduralAssets.IconHint, "Hint", XOProceduralAssets.ColSun, new Vector2(btnW, btnH), OnHintButtonClicked, out _hintBadgeBg, out _hintBadgeText, showBadge: true);

            // 2. Undo (Sky #4FB0FF)
            _undoBtn = CreateCompactBattleSkillButton(parent, XOProceduralAssets.IconUndo, "Undo", XOProceduralAssets.ColSky, new Vector2(btnW, btnH), OnUndoButtonClicked, out _undoBadgeBg, out _undoBadgeText, showBadge: true);

            // 3. Bomb (Coral #FF6F85)
            _bombBtn = CreateCompactBattleSkillButton(parent, XOProceduralAssets.IconBomb, "Bomb", XOProceduralAssets.ColCoral, new Vector2(btnW, btnH), OnBombSkillClicked, out _bombBadgeBg, out _bombBadgeText, showBadge: true);

            // 4. Freeze (Sky #4FB0FF)
            _freezeBtn = CreateCompactBattleSkillButton(parent, XOProceduralAssets.IconFreeze, "Freeze", XOProceduralAssets.ColSky, new Vector2(btnW, btnH), OnFreezeSkillClicked, out _freezeBadgeBg, out _freezeBadgeText, showBadge: true);

            // 5. Swap (Lilac #A998FF)
            _swapBtn = CreateCompactBattleSkillButton(parent, XOProceduralAssets.IconSwap, "Swap", XOProceduralAssets.ColLilac, new Vector2(btnW, btnH), OnSwapSkillClicked, out _swapBadgeBg, out _swapBadgeText, showBadge: true);

            // 6. Reset (Mint #4ED6A4) — clean, no badge
            _restartBtn = CreateCompactBattleSkillButton(parent, XOProceduralAssets.IconNewGame, "Reset", XOProceduralAssets.ColMint, new Vector2(btnW, btnH), () =>
            {
                PlaySfx("click");
                StartNewMatch(currentBoardSize);
            }, out _, out _, showBadge: false);
        }

        private Button CreateCompactBattleSkillButton(RectTransform parent, Sprite icon, string label, Color bgCol, Vector2 size, UnityEngine.Events.UnityAction onClick, out Image badgeBg, out Text badgeTxt, bool showBadge = true)
        {
            GameObject btnObj = CreateUIObject($"Skill_{label}", parent);
            RectTransform rt = btnObj.GetComponent<RectTransform>();
            rt.sizeDelta = size;

            Image bg = btnObj.AddComponent<Image>();
            bg.sprite = XOProceduralAssets.Button3DSprite;
            bg.type = Image.Type.Sliced;
            bg.color = bgCol;

            Button btn = btnObj.AddComponent<Button>();
            if (onClick != null) btn.onClick.AddListener(onClick);

            // Prominent centered vector icon with tactile balance
            CreateIconImage(rt, icon, new Vector2(0.5f, 0.5f), new Vector2(0f, 16f), new Vector2(58f, 58f));

            // High-contrast clean label
            Text tLabel = CreateText(rt, label, 18, FontStyle.Bold, TextAnchor.MiddleCenter);
            tLabel.rectTransform.anchorMin = new Vector2(0f, 0f);
            tLabel.rectTransform.anchorMax = new Vector2(1f, 0f);
            tLabel.rectTransform.pivot = new Vector2(0.5f, 0f);
            tLabel.rectTransform.anchoredPosition = new Vector2(0f, 8f);
            tLabel.rectTransform.sizeDelta = new Vector2(0f, 26f);
            tLabel.color = XOProceduralAssets.ColInk;

            if (showBadge)
            {
                // Crisp stock count badge tucked inside top-right corner
                GameObject badgeObj = CreateUIObject("Badge", rt);
                RectTransform bRT = badgeObj.GetComponent<RectTransform>();
                bRT.anchorMin = new Vector2(1f, 1f);
                bRT.anchorMax = new Vector2(1f, 1f);
                bRT.pivot = new Vector2(1f, 1f);
                bRT.anchoredPosition = new Vector2(-6f, -6f);
                bRT.sizeDelta = new Vector2(36f, 26f);

                badgeBg = badgeObj.AddComponent<Image>();
                badgeBg.sprite = XOProceduralAssets.PillSprite;
                badgeBg.type = Image.Type.Sliced;
                badgeBg.color = new Color(0.12f, 0.16f, 0.28f, 0.95f);
                badgeBg.raycastTarget = false;

                badgeTxt = CreateText(bRT, "1", 16, FontStyle.Bold, TextAnchor.MiddleCenter);
                badgeTxt.color = Color.white;
            }
            else
            {
                badgeBg = null;
                badgeTxt = null;
            }

            return btn;
        }

        // =====================================================================
        // MODAL: GAME MODE SELECTOR (7 VARIANTS & 2-PLAYER)
        // =====================================================================

        public void OpenTowerModal()
        {
            PlaySfx("click");
            if (XORoguelikeRunManager.Lives <= 0)
            {
                XORoguelikeRunManager.StartNewRun();
            }
            if (_towerModalRoot != null)
            {
                RefreshTowerModalUI();
                _towerModalRoot.SetActive(true);
            }
        }

        public void StartTowerMatch(int floor)
        {
            if (XORoguelikeRunManager.Lives <= 0)
            {
                XORoguelikeRunManager.StartNewRun();
                floor = 1;
            }

            XORoguelikeRunManager.CurrentFloor = floor;
            XOProgressionSystem.CurrentTowerFloor = floor;
            TowerFloorBlind blind = XORoguelikeRunManager.GetActiveBlind();
            currentGameMode = GameMode.TowerClimb;
            currentVariant = blind.variantRule;
            currentBoardSize = 15;
            winLength = 5;
            currentDifficulty = blind.difficulty;

            if (_towerModalRoot != null) _towerModalRoot.SetActive(false);
            if (_modeSelectModalRoot != null) _modeSelectModalRoot.SetActive(false);
            if (_caravanModalRoot != null) _caravanModalRoot.SetActive(false);
            if (_resultModalRoot != null) _resultModalRoot.SetActive(false);
            if (_reviveModalRoot != null) _reviveModalRoot.SetActive(false);

            XORoguelikeRunManager.ResetMatchState();
            StartNewMatch(currentBoardSize);

            // Blind & Relic setups on match start
            _engine.RequiredWinLines = blind.requiredWinLines;

            if (blind.blindType == BossBlindType.TheVoid)
            {
                _engine.ApplyCenterVoid(1);
            }
            else if (blind.blindType == BossBlindType.ChaosMinefield)
            {
                System.Random floorRnd = new System.Random(XORoguelikeRunManager.GetFloorSeed(floor));
                int placedMines = 0;
                while (placedMines < 3)
                {
                    int r = floorRnd.Next(1, currentBoardSize - 1);
                    int c = floorRnd.Next(1, currentBoardSize - 1);
                    if (_engine.SpecialTiles[r, c] == SpecialTileType.None)
                    {
                        _engine.SpecialTiles[r, c] = SpecialTileType.HiddenMine;
                        placedMines++;
                    }
                }
                int placedPortals = 0;
                while (placedPortals < 2)
                {
                    int r = floorRnd.Next(1, currentBoardSize - 1);
                    int c = floorRnd.Next(1, currentBoardSize - 1);
                    if (_engine.SpecialTiles[r, c] == SpecialTileType.None)
                    {
                        _engine.SpecialTiles[r, c] = SpecialTileType.WarpPortal;
                        placedPortals++;
                    }
                }
            }

            if (XORoguelikeRunManager.HasRelic(CaroRelicType.ChronoPocket))
            {
                UndoStock += 2;
                RefreshBottomActionBadges();
            }

            SetScreenState(ScreenState.Battle);
            RefreshBattleRelicBarUI();

            if (_boardOuterFrameImg != null)
            {
                _boardOuterFrameImg.color = Color.Lerp(XOProceduralAssets.ColCardBg, blind.mapAccentColor, 0.16f);
            }

            if (_statusBannerText != null)
            {
                string goalText = (blind.requiredWinLines > 1)
                    ? "Tạo 2 hàng 5 (2x5) để Thắng!"
                    : ((blind.maxPlayerMoves > 0) ? $"[Giới hạn {blind.maxPlayerMoves} nước!]" : "Nối 5 quân liên tiếp để Thắng!");
                _statusBannerText.text = $"[F.{blind.floorNumber} - {blind.mapName.ToUpper()}]: {blind.bossName} — {goalText}";
            }
            SpawnTacticalCalloutBanner($"[F.{blind.floorNumber} - {blind.mapName.ToUpper()}]", $"{blind.blindDescription}", blind.mapAccentColor);
        }

        private void BuildTowerModal(Transform canvasTransform)
        {
            _towerModalRoot = CreateModalBackdrop("TowerModal", canvasTransform, out RectTransform cardRT);
            _towerCardRT = cardRT;
            cardRT.sizeDelta = new Vector2(920f, 780f);

            // Title
            Text titleTxt = CreateText(cardRT, "TOWER OF ANOMALIES (ROGUELIKE)", 28, FontStyle.Bold, TextAnchor.UpperCenter);
            titleTxt.rectTransform.offsetMin = new Vector2(24f, 0f);
            titleTxt.rectTransform.offsetMax = new Vector2(-24f, -20f);
            titleTxt.color = XOProceduralAssets.ColInk;

            Text subTxt = CreateText(cardRT, "10 Mutating Boss Blinds • Draft Relics • 1 Life Sudden Death", 15, FontStyle.Normal, TextAnchor.UpperCenter);
            subTxt.rectTransform.offsetMin = new Vector2(24f, 0f);
            subTxt.rectTransform.offsetMax = new Vector2(-24f, -54f);
            subTxt.color = XOProceduralAssets.ColMuted;

            // Run Status Pill (Click to copy Seed!)
            GameObject runPill = CreateUIObject("TowerRunPill", cardRT);
            RectTransform rpRT = runPill.GetComponent<RectTransform>();
            rpRT.anchorMin = new Vector2(0.5f, 1f);
            rpRT.anchorMax = new Vector2(0.5f, 1f);
            rpRT.pivot = new Vector2(0.5f, 1f);
            rpRT.anchoredPosition = new Vector2(0f, -86f);
            rpRT.sizeDelta = new Vector2(660f, 40f);

            Image rpBg = runPill.AddComponent<Image>();
            rpBg.sprite = XOProceduralAssets.PillSprite;
            rpBg.type = Image.Type.Sliced;
            rpBg.color = XOProceduralAssets.ColTileEdge;
            _towerRunPillBg = rpBg;

            Button rpBtn = runPill.AddComponent<Button>();
            rpBtn.onClick.AddListener(() =>
            {
                GUIUtility.systemCopyBuffer = XORoguelikeRunManager.CurrentRunSeed.ToString();
                PlaySfx("click");
                SpawnTacticalCalloutBanner("SEED COPIED!", $"Seed {XORoguelikeRunManager.FormattedSeed} copied to clipboard!", XOProceduralAssets.ColMint);
            });

            _towerRunPillText = CreateText(rpRT, $"Heart: {XORoguelikeRunManager.Lives}/1  |  Seed: {XORoguelikeRunManager.FormattedSeed} (Tap to Copy)  |  Relics: {XORoguelikeRunManager.EquippedRelics.Count}/{XORoguelikeRunManager.MaxRelicSlots}  |  Run Floor: F.{XORoguelikeRunManager.CurrentFloor}", 14, FontStyle.Bold, TextAnchor.MiddleCenter);
            _towerRunPillText.color = XOProceduralAssets.ColInk;
            _towerRunPillText.raycastTarget = false;

            // Viewport & ScrollRect for 10 Blinds (Clamped & Masked!)
            GameObject scrollObj = CreateUIObject("TowerScrollView", cardRT);
            RectTransform svRT = scrollObj.GetComponent<RectTransform>();
            svRT.anchorMin = new Vector2(0f, 0f);
            svRT.anchorMax = new Vector2(1f, 1f);
            svRT.offsetMin = new Vector2(24f, 84f);
            svRT.offsetMax = new Vector2(-24f, -136f);

            scrollObj.AddComponent<RectMask2D>();

            ScrollRect scrollRect = scrollObj.AddComponent<ScrollRect>();
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;
            scrollRect.scrollSensitivity = 28f;
            scrollRect.inertia = true;
            scrollRect.decelerationRate = 0.135f;

            // Scrollable Content
            GameObject contentObj = CreateUIObject("TowerScrollContent", svRT);
            RectTransform cRT = contentObj.GetComponent<RectTransform>();
            cRT.anchorMin = new Vector2(0f, 1f);
            cRT.anchorMax = new Vector2(1f, 1f);
            cRT.pivot = new Vector2(0.5f, 1f);
            cRT.anchoredPosition = Vector2.zero;
            cRT.sizeDelta = new Vector2(0f, 0f);

            VerticalLayoutGroup vLayout = contentObj.AddComponent<VerticalLayoutGroup>();
            vLayout.spacing = 10f;
            vLayout.childControlWidth = true;
            vLayout.childControlHeight = false;
            vLayout.childForceExpandWidth = true;
            vLayout.childForceExpandHeight = false;
            vLayout.padding = new RectOffset(4, 4, 4, 16);

            ContentSizeFitter csf = contentObj.AddComponent<ContentSizeFitter>();
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scrollRect.viewport = svRT;
            scrollRect.content = cRT;
            _towerContentContainer = contentObj.transform;

            // Bottom Action Row
            GameObject botRowObj = CreateUIObject("TowerBottomActions", cardRT);
            RectTransform brRT = botRowObj.GetComponent<RectTransform>();
            brRT.anchorMin = new Vector2(0.5f, 0f);
            brRT.anchorMax = new Vector2(0.5f, 0f);
            brRT.pivot = new Vector2(0.5f, 0f);
            brRT.anchoredPosition = new Vector2(0f, 22f);
            brRT.sizeDelta = new Vector2(720f, 52f);

            HorizontalLayoutGroup hLayout = botRowObj.AddComponent<HorizontalLayoutGroup>();
            hLayout.childAlignment = TextAnchor.MiddleCenter;
            hLayout.spacing = 16f;
            hLayout.childControlWidth = false;
            hLayout.childControlHeight = false;

            Button closeBtn = CreateIcon3DButton(brRT, XOProceduralAssets.IconHome, "Về Sảnh", new Vector2(210f, 50f), XOProceduralAssets.ColMuted, Color.white, 16);
            closeBtn.onClick.AddListener(() =>
            {
                PlaySfx("click");
                _towerModalRoot.SetActive(false);
            });

            Button caravanBtn = CreateIcon3DButton(brRT, XOProceduralAssets.IconChest, "Trại Tiếp Tế", new Vector2(210f, 50f), XOProceduralAssets.ColLilac, Color.white, 16);
            caravanBtn.onClick.AddListener(() =>
            {
                PlaySfx("click");
                OpenCaravanDraftModal();
            });

            _towerRestartRunBtn = CreateIcon3DButton(brRT, XOProceduralAssets.IconUndo, "Lượt Chơi Mới", new Vector2(190f, 50f), XOProceduralAssets.ColCoral, Color.white, 15);
            _towerRestartRunBtn.onClick.AddListener(() =>
            {
                PlaySfx("click");
                XORoguelikeRunManager.StartNewRun();
                RefreshTowerModalUI();
            });

            botRowObj.transform.SetAsLastSibling();

            RefreshTowerModalUI();
            _towerModalRoot.SetActive(false);
        }

        public void RefreshTowerModalUI()
        {
            bool isRunOver = (XORoguelikeRunManager.Lives <= 0);

            if (_towerRunPillText != null)
            {
                if (isRunOver)
                {
                    _towerRunPillText.text = $"Heart: 0/1 (RUN OVER)  |  Seed: {XORoguelikeRunManager.FormattedSeed}  |  Relics: 0/4  |  Floor: F.1";
                    _towerRunPillText.color = XOProceduralAssets.ColCoral;
                    if (_towerRunPillBg != null) _towerRunPillBg.color = XOThemeDatabase.Hex("#FEE2E2");
                }
                else
                {
                    _towerRunPillText.text = $"Heart: 1/1  |  Seed: {XORoguelikeRunManager.FormattedSeed} (Tap to Copy)  |  Relics: {XORoguelikeRunManager.EquippedRelics.Count}/{XORoguelikeRunManager.MaxRelicSlots}  |  Run Floor: F.{XORoguelikeRunManager.CurrentFloor}";
                    _towerRunPillText.color = XOProceduralAssets.ColInk;
                    if (_towerRunPillBg != null) _towerRunPillBg.color = XOProceduralAssets.ColTileEdge;
                }
            }

            if (_towerRestartRunBtn != null)
            {
                _towerRestartRunBtn.gameObject.SetActive(true);
                Text btnTxt = _towerRestartRunBtn.GetComponentInChildren<Text>();
                Image btnImg = _towerRestartRunBtn.GetComponent<Image>();
                RectTransform btnRT = _towerRestartRunBtn.GetComponent<RectTransform>();

                if (isRunOver)
                {
                    if (btnTxt != null) btnTxt.text = "Start New Run";
                    if (btnImg != null) btnImg.color = XOProceduralAssets.ColCoral;
                    if (btnRT != null) btnRT.sizeDelta = new Vector2(210f, 50f);
                }
                else
                {
                    if (btnTxt != null) btnTxt.text = (XORoguelikeRunManager.CurrentFloor > 1) ? "New Run" : "Re-Seed";
                    if (btnImg != null) btnImg.color = (XORoguelikeRunManager.CurrentFloor > 1) ? XOProceduralAssets.ColCoral : XOProceduralAssets.ColLilac;
                    if (btnRT != null) btnRT.sizeDelta = new Vector2(170f, 50f);
                }
            }

            if (_towerContentContainer != null)
            {
                foreach (Transform child in _towerContentContainer)
                {
                    Destroy(child.gameObject);
                }

                for (int i = 0; i < XORoguelikeRunManager.FloorBlinds.Length; i++)
                {
                    CreateTowerFloorRow(_towerContentContainer as RectTransform, XORoguelikeRunManager.FloorBlinds[i]);
                }
            }
        }

        private void CreateTowerFloorRow(RectTransform parent, TowerFloorBlind floor)
        {
            GameObject rowObj = CreateUIObject($"FloorRow_{floor.floorNumber}", parent);
            RectTransform rowRT = rowObj.GetComponent<RectTransform>();
            rowRT.sizeDelta = new Vector2(0f, 78f);

            LayoutElement le = rowObj.AddComponent<LayoutElement>();
            le.minHeight = 78f;
            le.preferredHeight = 78f;
            le.flexibleHeight = 0f;

            Image bg = rowObj.AddComponent<Image>();
            bg.sprite = XOProceduralAssets.RoundedRectSprite;
            bg.type = Image.Type.Sliced;

            bool isRunOver = (XORoguelikeRunManager.Lives <= 0);
            int currentFloor = XORoguelikeRunManager.CurrentFloor;
            bool isCurrentFloor = (floor.floorNumber == currentFloor);
            bool isCleared = (floor.floorNumber < currentFloor);
            bool isLocked = (floor.floorNumber > currentFloor);

            if (isRunOver && isCurrentFloor)
            {
                bg.color = XOThemeDatabase.Hex("#FEF2F2"); // soft red for defeated floor
            }
            else if (isCleared)
            {
                bg.color = XOThemeDatabase.Hex("#ECFDF5"); // soft mint
            }
            else if (isCurrentFloor && !isRunOver)
            {
                bg.color = XOThemeDatabase.Hex("#F0F9FF"); // active soft sky
            }
            else
            {
                bg.color = XOThemeDatabase.Hex("#F8FAFC"); // clean light slate
            }

            // Icon
            Sprite icon = isCleared ? XOProceduralAssets.IconTrophy : ((isCurrentFloor && !isRunOver) ? XOProceduralAssets.IconCrown : (isRunOver && isCurrentFloor ? XOProceduralAssets.IconSwords : XOProceduralAssets.IconShield));
            Color iconCol = isCleared ? XOProceduralAssets.ColMint : ((isCurrentFloor && !isRunOver) ? XOProceduralAssets.ColSun : (isRunOver && isCurrentFloor ? XOProceduralAssets.ColCoral : new Color(0.60f, 0.65f, 0.75f)));
            Image icImg = CreateIconImage(rowRT, icon, new Vector2(0f, 0.5f), new Vector2(36f, 0f), new Vector2(42f, 42f));
            icImg.color = iconCol;

            // Floor & Boss Name
            string title = $"F.{floor.floorNumber} - {floor.mapName} [{floor.bossName}]";
            Text nameTxt = CreateText(rowRT, title, 18, FontStyle.Bold, TextAnchor.UpperLeft);
            nameTxt.rectTransform.offsetMin = new Vector2(74f, 0f);
            nameTxt.rectTransform.offsetMax = new Vector2(-160f, -8f);
            nameTxt.color = (!isLocked && !isRunOver) ? XOProceduralAssets.ColInk : (isRunOver && isCurrentFloor ? XOProceduralAssets.ColCoral : new Color(0.38f, 0.44f, 0.58f));

            // Blind & Relic Info
            string relicInfo = (floor.botEquipment != BotEquipmentType.None) ? $"Boss Relic: {floor.botEquipment} | " : "";
            string desc = $"{relicInfo}{floor.blindDescription} (+{floor.coinReward}c & Draft)";
            Text subTxt = CreateText(rowRT, desc, 14, FontStyle.Normal, TextAnchor.LowerLeft);
            subTxt.rectTransform.offsetMin = new Vector2(74f, 8f);
            subTxt.rectTransform.offsetMax = new Vector2(-160f, 0f);
            subTxt.color = (!isLocked && !isRunOver) 
                ? ((floor.botEquipment != BotEquipmentType.None) ? XOThemeDatabase.Hex("#E11D48") : XOProceduralAssets.ColMuted)
                : new Color(0.55f, 0.60f, 0.72f);

            // Right-side badge or Fight button
            if (isRunOver)
            {
                if (floor.floorNumber == 1)
                {
                    Button startBtn = CreateIcon3DButton(rowRT, XOProceduralAssets.IconNewGame, "Bắt Đầu", new Vector2(140f, 48f), XOProceduralAssets.ColMint, XOProceduralAssets.ColInk, 16);
                    RectTransform sRT = startBtn.GetComponent<RectTransform>();
                    sRT.anchorMin = new Vector2(1f, 0.5f);
                    sRT.anchorMax = new Vector2(1f, 0.5f);
                    sRT.pivot = new Vector2(1f, 0.5f);
                    sRT.anchoredPosition = new Vector2(-14f, 0f);
                    startBtn.onClick.AddListener(() =>
                    {
                        XORoguelikeRunManager.StartNewRun();
                        StartTowerMatch(1);
                    });
                }
                else
                {
                    CreatePillBadge(rowRT, "KHÓA", new Color(0.85f, 0.88f, 0.94f), new Color(0.42f, 0.48f, 0.60f));
                }
            }
            else if (isCleared)
            {
                CreatePillBadge(rowRT, "ĐÃ QUA", XOThemeDatabase.Hex("#D1FAE5"), XOThemeDatabase.Hex("#059669"));
            }
            else if (isCurrentFloor)
            {
                Button fightBtn = CreateIcon3DButton(rowRT, XOProceduralAssets.IconNewGame, "Khiêu Chiến", new Vector2(140f, 48f), XOProceduralAssets.ColMint, XOProceduralAssets.ColInk, 16);
                RectTransform fRT = fightBtn.GetComponent<RectTransform>();
                fRT.anchorMin = new Vector2(1f, 0.5f);
                fRT.anchorMax = new Vector2(1f, 0.5f);
                fRT.pivot = new Vector2(1f, 0.5f);
                fRT.anchoredPosition = new Vector2(-14f, 0f);
                fightBtn.onClick.AddListener(() => StartTowerMatch(floor.floorNumber));
            }
            else
            {
                CreatePillBadge(rowRT, "KHÓA", new Color(0.85f, 0.88f, 0.94f), new Color(0.42f, 0.48f, 0.60f));
            }
        }

        private GameObject CreatePillBadge(RectTransform parent, string label, Color bgColor, Color textColor)
        {
            GameObject badge = CreateUIObject("StatusBadge", parent);
            RectTransform bRT = badge.GetComponent<RectTransform>();
            bRT.anchorMin = new Vector2(1f, 0.5f);
            bRT.anchorMax = new Vector2(1f, 0.5f);
            bRT.pivot = new Vector2(1f, 0.5f);
            bRT.anchoredPosition = new Vector2(-16f, 0f);
            bRT.sizeDelta = new Vector2(110f, 36f);

            Image bBg = badge.AddComponent<Image>();
            bBg.sprite = XOProceduralAssets.PillSprite;
            bBg.type = Image.Type.Sliced;
            bBg.color = bgColor;
            bBg.raycastTarget = false;

            Text bTxt = CreateText(bRT, label, 13, FontStyle.Bold, TextAnchor.MiddleCenter);
            bTxt.color = textColor;
            return badge;
        }

        // =====================================================================
        // TOWER CARAVAN & ROGUELIKE DRAFT MODAL (BALATRO STYLE SHOP)
        // =====================================================================

        public void OpenCaravanDraftModal()
        {
            PlaySfx("reward");
            _caravanRerollCount = 0;
            if (_resultModalRoot != null) _resultModalRoot.SetActive(false);
            if (_caravanModalRoot != null)
            {
                _caravanModalRoot.SetActive(true);
                RefreshCaravanDraftUI();
            }
        }

        private void BuildCaravanDraftModal(Transform canvasTransform)
        {
            _caravanModalRoot = CreateModalBackdrop("CaravanModal", canvasTransform, out RectTransform cardRT);
            _caravanCardRT = cardRT;
            cardRT.sizeDelta = new Vector2(960f, 780f);

            Text titleTxt = CreateText(cardRT, "TOWER CARAVAN — DRAFT A RELIC", 28, FontStyle.Bold, TextAnchor.UpperCenter);
            titleTxt.rectTransform.offsetMin = new Vector2(24f, 0f);
            titleTxt.rectTransform.offsetMax = new Vector2(-24f, -22f);
            titleTxt.color = XOProceduralAssets.ColInk;

            _caravanSubtitleText = CreateText(cardRT, "Choose 1 Relic to power up your run synergies! (Max 4 Slots)", 16, FontStyle.Normal, TextAnchor.UpperCenter);
            _caravanSubtitleText.rectTransform.offsetMin = new Vector2(24f, 0f);
            _caravanSubtitleText.rectTransform.offsetMax = new Vector2(-24f, -58f);
            _caravanSubtitleText.color = XOProceduralAssets.ColMuted;

            // Heart Status & Coins Pill
            GameObject statusPill = CreateUIObject("CaravanStatusPill", cardRT);
            RectTransform spRT = statusPill.GetComponent<RectTransform>();
            spRT.anchorMin = new Vector2(0.5f, 1f);
            spRT.anchorMax = new Vector2(0.5f, 1f);
            spRT.pivot = new Vector2(0.5f, 1f);
            spRT.anchoredPosition = new Vector2(0f, -88f);
            spRT.sizeDelta = new Vector2(580f, 40f);
            Image spBg = statusPill.AddComponent<Image>();
            spBg.sprite = XOProceduralAssets.PillSprite;
            spBg.type = Image.Type.Sliced;
            spBg.color = XOProceduralAssets.ColTileEdge;

            _caravanLivesText = CreateText(spRT, $"Heart: {XORoguelikeRunManager.Lives}/1  |  Seed: {XORoguelikeRunManager.FormattedSeed}  |  Relics: {XORoguelikeRunManager.EquippedRelics.Count}/{XORoguelikeRunManager.MaxRelicSlots}", 15, FontStyle.Bold, TextAnchor.MiddleCenter);
            _caravanLivesText.color = XOProceduralAssets.ColInk;

            // Equipped Relics Strip (with Sell Refund)
            GameObject eqStripObj = CreateUIObject("CaravanEquippedStrip", cardRT);
            RectTransform eqRT = eqStripObj.GetComponent<RectTransform>();
            eqRT.anchorMin = new Vector2(0.5f, 1f);
            eqRT.anchorMax = new Vector2(0.5f, 1f);
            eqRT.pivot = new Vector2(0.5f, 1f);
            eqRT.anchoredPosition = new Vector2(0f, -138f);
            eqRT.sizeDelta = new Vector2(900f, 44f);
            _caravanEquippedContainer = eqStripObj.transform;

            HorizontalLayoutGroup eqHLayout = eqStripObj.AddComponent<HorizontalLayoutGroup>();
            eqHLayout.childAlignment = TextAnchor.MiddleCenter;
            eqHLayout.spacing = 10f;
            eqHLayout.childControlWidth = false;
            eqHLayout.childControlHeight = true;
            eqHLayout.childForceExpandWidth = false;
            eqHLayout.childForceExpandHeight = true;

            // Relic 3-Card Container
            GameObject relicsContainer = CreateUIObject("CaravanRelicCards", cardRT);
            _caravanRelicsContainer = relicsContainer.transform;
            RectTransform rcRT = relicsContainer.GetComponent<RectTransform>();
            rcRT.anchorMin = new Vector2(0f, 0f);
            rcRT.anchorMax = new Vector2(1f, 1f);
            rcRT.offsetMin = new Vector2(30f, 96f);
            rcRT.offsetMax = new Vector2(-30f, -192f);

            HorizontalLayoutGroup hLayout = relicsContainer.AddComponent<HorizontalLayoutGroup>();
            hLayout.spacing = 16f;
            hLayout.childControlWidth = true;
            hLayout.childControlHeight = true;
            hLayout.childForceExpandWidth = true;
            hLayout.childForceExpandHeight = true;

            // Bottom Buttons: Reroll (10c), Skip/Continue
            _caravanRerollBtn = CreateIcon3DButton(cardRT, XOProceduralAssets.IconUndo, "Reroll Relics (15c)", new Vector2(230f, 52f), XOProceduralAssets.ColSun, XOProceduralAssets.ColInk, 15);
            RectTransform rbRT = _caravanRerollBtn.GetComponent<RectTransform>();
            rbRT.anchorMin = new Vector2(0f, 0f);
            rbRT.anchorMax = new Vector2(0f, 0f);
            rbRT.pivot = new Vector2(0f, 0f);
            rbRT.anchoredPosition = new Vector2(32f, 32f);
            _caravanRerollBtn.onClick.AddListener(() =>
            {
                if (Coins >= 15)
                {
                    Coins -= 15;
                    _caravanRerollCount++;
                    PlaySfx("click");
                    RefreshCaravanDraftUI();
                }
                else
                {
                    PlaySfx("lose");
                }
            });

            _caravanContinueBtn = CreateIcon3DButton(cardRT, XOProceduralAssets.IconNewGame, "Next Floor >", new Vector2(220f, 52f), XOProceduralAssets.ColMint, XOProceduralAssets.ColInk, 16);
            RectTransform cbRT = _caravanContinueBtn.GetComponent<RectTransform>();
            cbRT.anchorMin = new Vector2(1f, 0f);
            cbRT.anchorMax = new Vector2(1f, 0f);
            cbRT.pivot = new Vector2(1f, 0f);
            cbRT.anchoredPosition = new Vector2(-32f, 32f);
            _caravanContinueBtn.onClick.AddListener(() =>
            {
                PlaySfx("click");
                _caravanModalRoot.SetActive(false);
                StartNextTowerFloor();
            });

            _caravanModalRoot.SetActive(false);
        }

        private void RefreshCaravanDraftUI()
        {
            if (_caravanRelicsContainer == null) return;
            foreach (Transform child in _caravanRelicsContainer)
            {
                Destroy(child.gameObject);
            }

            if (_caravanLivesText != null)
            {
                _caravanLivesText.text = $"Heart: {XORoguelikeRunManager.Lives}/1  |  Seed: {XORoguelikeRunManager.FormattedSeed}  |  Relics: {XORoguelikeRunManager.EquippedRelics.Count}/{XORoguelikeRunManager.MaxRelicSlots}  |  Coins: {Coins:N0}";
            }

            // Refresh Equipped Relics Strip with Sell buttons
            if (_caravanEquippedContainer != null)
            {
                foreach (Transform child in _caravanEquippedContainer)
                {
                    Destroy(child.gameObject);
                }

                if (XORoguelikeRunManager.EquippedRelics.Count == 0)
                {
                    GameObject emptyObj = CreateUIObject("EmptyPrompt", _caravanEquippedContainer);
                    RectTransform epRT = emptyObj.GetComponent<RectTransform>();
                    epRT.sizeDelta = new Vector2(400f, 36f);
                    Text t = CreateText(epRT, "Equipped Relics: None (0/4 Slots Used)", 14, FontStyle.Italic, TextAnchor.MiddleCenter);
                    t.color = XOProceduralAssets.ColMuted;
                }
                else
                {
                    for (int i = 0; i < XORoguelikeRunManager.EquippedRelics.Count; i++)
                    {
                        CaroRelicType relicType = XORoguelikeRunManager.EquippedRelics[i];
                        CaroRelic relic = XORoguelikeRunManager.GetRelicData(relicType);
                        if (relic == null) continue;

                        int refund = relic.cost / 2;
                        GameObject eqItem = CreateUIObject("Eq_" + relic.id, _caravanEquippedContainer);
                        RectTransform itemRT = eqItem.GetComponent<RectTransform>();
                        itemRT.sizeDelta = new Vector2(210f, 40f);

                        Image itemBg = eqItem.AddComponent<Image>();
                        itemBg.sprite = XOProceduralAssets.PillSprite;
                        itemBg.type = Image.Type.Sliced;
                        itemBg.color = XOProceduralAssets.ColTileEdge;

                        CreateIconImage(itemRT, relic.GetIcon(), new Vector2(0f, 0.5f), new Vector2(18f, 0f), new Vector2(24f, 24f));

                        Text label = CreateText(itemRT, relic.name, 12, FontStyle.Bold, TextAnchor.MiddleLeft);
                        label.rectTransform.offsetMin = new Vector2(34f, 0f);
                        label.rectTransform.offsetMax = new Vector2(-92f, 0f);
                        label.color = XOProceduralAssets.ColInk;

                        Button sellBtn = CreateIcon3DButton(itemRT, XOProceduralAssets.IconCoin, $"Sell (+{refund}c)", new Vector2(90f, 32f), XOProceduralAssets.ColCoral, Color.white, 11);
                        RectTransform sRT = sellBtn.GetComponent<RectTransform>();
                        sRT.anchorMin = new Vector2(1f, 0.5f);
                        sRT.anchorMax = new Vector2(1f, 0.5f);
                        sRT.pivot = new Vector2(1f, 0.5f);
                        sRT.anchoredPosition = new Vector2(-4f, 0f);

                        sellBtn.onClick.AddListener(() =>
                        {
                            if (XORoguelikeRunManager.SellRelic(relicType, out int coinGain))
                            {
                                Coins += coinGain;
                                PlaySfx("reward");
                                SpawnTacticalCalloutBanner("RELIC SOLD!", $"Sold {relic.name} for +{coinGain} Coins!", XOProceduralAssets.ColSun);
                                RefreshCaravanDraftUI();
                            }
                        });
                    }
                }
            }

            List<CaroRelic> draft = XORoguelikeRunManager.GetRandomRelicDraft(3, _caravanRerollCount);
            for (int i = 0; i < draft.Count; i++)
            {
                CaroRelic relic = draft[i];
                CreateCaravanRelicCard(_caravanRelicsContainer, relic);
            }
        }

        private void CreateCaravanRelicCard(Transform parent, CaroRelic relic)
        {
            GameObject cardObj = CreateUIObject("RelicCard_" + relic.id, parent);
            RectTransform cRT = cardObj.GetComponent<RectTransform>();

            Image bg = cardObj.AddComponent<Image>();
            bg.sprite = XOProceduralAssets.CardPanelSprite;
            bg.type = Image.Type.Sliced;
            bg.color = Color.white;

            // Rarity Top Stripe / Badge
            GameObject rarityBadge = CreateUIObject("RarityBadge", cRT);
            RectTransform rbRT = rarityBadge.GetComponent<RectTransform>();
            rbRT.anchorMin = new Vector2(0.5f, 1f);
            rbRT.anchorMax = new Vector2(0.5f, 1f);
            rbRT.pivot = new Vector2(0.5f, 1f);
            rbRT.anchoredPosition = new Vector2(0f, -14f);
            rbRT.sizeDelta = new Vector2(140f, 28f);
            Image rbBg = rarityBadge.AddComponent<Image>();
            rbBg.sprite = XOProceduralAssets.PillSprite;
            rbBg.type = Image.Type.Sliced;
            rbBg.color = relic.GetRarityColor();

            Text rText = CreateText(rbRT, relic.rarity.ToString().ToUpper(), 13, FontStyle.Bold, TextAnchor.MiddleCenter);
            rText.color = (relic.rarity == CaroRelicRarity.Common || relic.rarity == CaroRelicRarity.Rare) ? Color.white : XOProceduralAssets.ColInk;

            // Relic Large Vector Icon
            CreateIconImage(cRT, relic.GetIcon(), new Vector2(0.5f, 1f), new Vector2(0f, -74f), new Vector2(64f, 64f));

            // Name
            Text nameTxt = CreateText(cRT, relic.name, 19, FontStyle.Bold, TextAnchor.UpperCenter);
            nameTxt.rectTransform.offsetMin = new Vector2(10f, 0f);
            nameTxt.rectTransform.offsetMax = new Vector2(-10f, -114f);
            nameTxt.color = XOProceduralAssets.ColInk;

            // Description
            Text descTxt = CreateText(cRT, relic.description, 14, FontStyle.Normal, TextAnchor.MiddleCenter);
            descTxt.rectTransform.offsetMin = new Vector2(12f, 70f);
            descTxt.rectTransform.offsetMax = new Vector2(-12f, -140f);
            descTxt.color = XOProceduralAssets.ColInk;

            // Draft Button
            bool canDraft = XORoguelikeRunManager.EquippedRelics.Count < XORoguelikeRunManager.MaxRelicSlots;
            string btnLabel = canDraft ? "DRAFT (FREE)" : "SLOTS FULL (4/4)";
            Color btnColor = canDraft ? XOProceduralAssets.ColMint : XOProceduralAssets.ColTileEdge;
            Color txtColor = canDraft ? XOProceduralAssets.ColInk : XOProceduralAssets.ColMuted;

            Button draftBtn = CreateIcon3DButton(cRT, XOProceduralAssets.IconRelic, btnLabel, new Vector2(220f, 48f), btnColor, txtColor, 15);
            RectTransform dbRT = draftBtn.GetComponent<RectTransform>();
            dbRT.anchorMin = new Vector2(0.5f, 0f);
            dbRT.anchorMax = new Vector2(0.5f, 0f);
            dbRT.pivot = new Vector2(0.5f, 0f);
            dbRT.anchoredPosition = new Vector2(0f, 16f);

            draftBtn.onClick.AddListener(() =>
            {
                if (!canDraft)
                {
                    PlaySfx("lose");
                    SpawnTacticalCalloutBanner("SLOTS FULL!", "Sell an equipped relic in the bar above first to free a slot!", XOProceduralAssets.ColCoral);
                    return;
                }
                if (XORoguelikeRunManager.EquipRelic(relic.type))
                {
                    PlaySfx("reward");
                    _caravanModalRoot.SetActive(false);
                    StartNextTowerFloor();
                }
                else
                {
                    PlaySfx("lose");
                }
            });
        }

        public void StartNextTowerFloor()
        {
            if (XORoguelikeRunManager.CurrentFloor < 10)
            {
                XORoguelikeRunManager.CurrentFloor++;
                StartTowerMatch(XORoguelikeRunManager.CurrentFloor);
            }
            else
            {
                SetScreenState(ScreenState.Lobby);
                PlaySfx("win");
            }
        }

        private void BuildModeSelectModal(Transform canvasTransform)
        {
            _modeSelectModalRoot = CreateModalBackdrop("ModeSelectModal", canvasTransform, out RectTransform cardRT);
            _modeCardRT = cardRT;
            _modeCardImg = cardRT.GetComponent<Image>();
            cardRT.sizeDelta = new Vector2(880f, 740f);

            // Modal Header
            CreateIconImage(cardRT, XOProceduralAssets.IconSwords, new Vector2(0f, 1f), new Vector2(46f, -42f), new Vector2(40f, 40f));
            Text title = CreateText(cardRT, "Select game mode", 24, FontStyle.Bold, TextAnchor.MiddleLeft);
            title.rectTransform.anchorMin = new Vector2(0f, 1f);
            title.rectTransform.anchorMax = new Vector2(1f, 1f);
            title.rectTransform.pivot = new Vector2(0f, 1f);
            title.rectTransform.anchoredPosition = new Vector2(76f, -24f);
            title.rectTransform.sizeDelta = new Vector2(-150f, 40f);
            title.color = XOProceduralAssets.ColInk;

            // Close [X] Button
            Button closeBtn = Create3DButton(cardRT, "X", new Vector2(46f, 44f), Color.white, XOProceduralAssets.ColInk, 20);
            RectTransform cRT = closeBtn.GetComponent<RectTransform>();
            cRT.anchorMin = new Vector2(1f, 1f);
            cRT.anchorMax = new Vector2(1f, 1f);
            cRT.pivot = new Vector2(1f, 1f);
            cRT.anchoredPosition = new Vector2(-18f, -20f);
            closeBtn.onClick.AddListener(() =>
            {
                PlaySfx("click");
                _modeSelectModalRoot.SetActive(false);
            });

            // 2x4 Grid of Game Modes
            GameObject gridObj = CreateUIObject("ModeGrid", cardRT);
            RectTransform mgRT = gridObj.GetComponent<RectTransform>();
            mgRT.anchorMin = new Vector2(0f, 0f);
            mgRT.anchorMax = new Vector2(1f, 1f);
            mgRT.offsetMin = new Vector2(24f, 96f);
            mgRT.offsetMax = new Vector2(-24f, -76f);

            GridLayoutGroup mgLayout = gridObj.AddComponent<GridLayoutGroup>();
            mgLayout.cellSize = new Vector2(400f, 104f);
            mgLayout.spacing = new Vector2(16f, 12f);
            mgLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            mgLayout.constraintCount = 2;
            mgLayout.childAlignment = TextAnchor.MiddleCenter;

            // 1. 15x15 Standard Caro (Tournament Pro)
            _size15Btn = CreateModeCardButton(mgRT, XOProceduralAssets.IconTrophy, "Standard 15x15", "Tournament Caro board (Connect 5 to win)", () =>
            {
                OnSelectVariantAndSize(XOVariantRule.ClassicCaro, 15, 5);
                _modeSelectModalRoot.SetActive(false);
            });

            // 2. 10x10 Compact Caro
            _size10Btn = CreateModeCardButton(mgRT, XOProceduralAssets.IconNewGame, "Compact 10x10", "Fast-paced compact board (Connect 5)", () =>
            {
                OnSelectVariantAndSize(XOVariantRule.ClassicCaro, 10, 5);
                _modeSelectModalRoot.SetActive(false);
            });

            // 3. 3x3 Infinite Vanishing
            _varInfinite3Btn = CreateModeCardButton(mgRT, XOProceduralAssets.IconSwap, "Infinite 3x3", "Fast-paced! Oldest piece vanishes on 4th move", () =>
            {
                OnSelectVariantAndSize(XOVariantRule.Infinite3x3, 3, 3);
                _modeSelectModalRoot.SetActive(false);
            });

            // 4. 4x4 Gobblet Stack
            _varGobbletBtn = CreateModeCardButton(mgRT, XOProceduralAssets.IconCrown, "Gobblet 4x4", "Stack up to Lv.3 • Eat opponent pieces", () =>
            {
                OnSelectVariantAndSize(XOVariantRule.GobbletStack, 4, 4);
                _modeSelectModalRoot.SetActive(false);
            });

            // 5. 8x8 Gravity Drop
            _varGravityBtn = CreateModeCardButton(mgRT, XOProceduralAssets.IconRock, "Gravity Drop 8x8", "Pieces fall to bottom row like Connect-4", () =>
            {
                OnSelectVariantAndSize(XOVariantRule.GravityDrop, 8, 4);
                _modeSelectModalRoot.SetActive(false);
            });

            // 6. Strict Caro (Blocked Ends) - 15x15 Standard
            _varStrictBtn = CreateModeCardButton(mgRT, XOProceduralAssets.IconShield, "Strict Caro 15x15", "5-in-a-row blocked at both ends does not win", () =>
            {
                OnSelectVariantAndSize(XOVariantRule.StrictCaro, 15, 5);
                _modeSelectModalRoot.SetActive(false);
            });

            // 7. Chaos Minefield - 15x15
            _varChaosBtn = CreateModeCardButton(mgRT, XOProceduralAssets.IconBomb, "Chaos Mines 15x15", "Hidden blast mines and warp portals", () =>
            {
                OnSelectVariantAndSize(XOVariantRule.ChaosMinefield, 15, 5);
                _modeSelectModalRoot.SetActive(false);
            });

            // 8. 2 Players Pass & Play (15x15 Standard)
            _mode2PBtn = CreateModeCardButton(mgRT, XOProceduralAssets.IconSwords, "Pass & Play 15x15", "Local 2 players on one device", () =>
            {
                currentBoardSize = 15;
                winLength = 5;
                currentVariant = XOVariantRule.ClassicCaro;
                OnSelectMode(GameMode.TwoPlayerLocal, currentDifficulty);
                _modeSelectModalRoot.SetActive(false);
            });

            // Bottom Row: Tower of Champions & Boss Stage
            Button towerBtnInModal = CreateIcon3DButton(cardRT, XOProceduralAssets.IconCrown, $"Tower of Champions (F.{XOProgressionSystem.CurrentTowerFloor})", new Vector2(360f, 54f), XOProceduralAssets.ColMint, XOProceduralAssets.ColInk, 17);
            RectTransform twRT = towerBtnInModal.GetComponent<RectTransform>();
            twRT.anchorMin = new Vector2(0.5f, 0f);
            twRT.anchorMax = new Vector2(0.5f, 0f);
            twRT.anchoredPosition = new Vector2(-190f, 38f);
            towerBtnInModal.onClick.AddListener(() =>
            {
                PlaySfx("click");
                _modeSelectModalRoot.SetActive(false);
                OpenTowerModal();
            });

            _modePuzzleBtn = CreateIcon3DButton(cardRT, XOProceduralAssets.IconSwords, $"Boss Stage #{XOProgressionSystem.CurrentPuzzleStage}", new Vector2(360f, 54f), XOProceduralAssets.ColCoral, Color.white, 17);
            RectTransform pzRT = _modePuzzleBtn.GetComponent<RectTransform>();
            pzRT.anchorMin = new Vector2(0.5f, 0f);
            pzRT.anchorMax = new Vector2(0.5f, 0f);
            pzRT.anchoredPosition = new Vector2(190f, 38f);
            _modePuzzleBtnText = _modePuzzleBtn.GetComponentInChildren<Text>();
            _modePuzzleBtn.onClick.AddListener(() =>
            {
                OnSelectMode(GameMode.PuzzleStage, AIDifficulty.Hard);
                _modeSelectModalRoot.SetActive(false);
            });

            _modeAiTierBtn = _lobbyChangeRivalBtn;
            _modeAiTierBtnText = _lobbyRivalBannerText;

            _modeSelectModalRoot.SetActive(false);
        }

        private Button CreateModeCardButton(RectTransform parent, Sprite icon, string title, string subtitle, UnityEngine.Events.UnityAction onClick)
        {
            GameObject btnObj = CreateUIObject($"Mode_{title}", parent);
            RectTransform rt = btnObj.GetComponent<RectTransform>();

            Image bg = btnObj.AddComponent<Image>();
            bg.sprite = XOProceduralAssets.Button3DSprite;
            bg.type = Image.Type.Sliced;
            bg.color = Color.white;

            Button btn = btnObj.AddComponent<Button>();
            if (onClick != null) btn.onClick.AddListener(onClick);

            CreateIconImage(rt, icon, new Vector2(0f, 0.5f), new Vector2(28f, 0f), new Vector2(36f, 36f));

            Text tTitle = CreateText(rt, title, 17, FontStyle.Bold, TextAnchor.MiddleLeft);
            tTitle.rectTransform.anchorMin = new Vector2(0f, 0.5f);
            tTitle.rectTransform.anchorMax = new Vector2(1f, 0.5f);
            tTitle.rectTransform.pivot = new Vector2(0f, 0.5f);
            tTitle.rectTransform.anchoredPosition = new Vector2(56f, 12f);
            tTitle.rectTransform.sizeDelta = new Vector2(-70f, 24f);
            tTitle.color = XOProceduralAssets.ColInk;

            Text tSub = CreateText(rt, subtitle, 12, FontStyle.Normal, TextAnchor.MiddleLeft);
            tSub.rectTransform.anchorMin = new Vector2(0f, 0.5f);
            tSub.rectTransform.anchorMax = new Vector2(1f, 0.5f);
            tSub.rectTransform.pivot = new Vector2(0f, 0.5f);
            tSub.rectTransform.anchoredPosition = new Vector2(56f, -12f);
            tSub.rectTransform.sizeDelta = new Vector2(-70f, 22f);
            tSub.color = XOProceduralAssets.ColMuted;

            return btn;
        }

        // =====================================================================
        // RESPONSIVE LAYOUT ENGINE (LANDSCAPE & PORTRAIT)
        // =====================================================================

        private void UpdateResponsiveLayout(bool force)
        {
            _lastScreenWidth = ScreenW;
            _lastScreenHeight = ScreenH;

            bool landscape = (ScreenW > ScreenH);
            if (!force && landscape == _isLandscapeLayout)
            {
                ResizeBoardGridCells();
                return;
            }

            _isLandscapeLayout = landscape;

            if (_isLandscapeLayout)
            {
                // LANDSCAPE 1920 x 1080 (Match Height = 1.0f)
                _canvasScaler.referenceResolution = new Vector2(1920f, 1080f);
                _canvasScaler.matchWidthOrHeight = 1.0f;

                float aspect = (float)ScreenW / Mathf.Max(1, ScreenH);
                float canvasW = 1080f * aspect;
                float canvasH = 1080f;

                // --- LOBBY SCREEN (LANDSCAPE 16:9) ---
                if (_headerCardRT != null)
                {
                    _headerCardRT.anchorMin = new Vector2(0.5f, 1f);
                    _headerCardRT.anchorMax = new Vector2(0.5f, 1f);
                    _headerCardRT.pivot = new Vector2(0.5f, 1f);
                    _headerCardRT.anchoredPosition = new Vector2(0f, -20f);
                    _headerCardRT.sizeDelta = new Vector2(Mathf.Min(1720f, canvasW - 80f), 60f);
                }
                if (_lobbyHeroCardRT != null)
                {
                    _lobbyHeroCardRT.anchorMin = new Vector2(0.5f, 0.5f);
                    _lobbyHeroCardRT.anchorMax = new Vector2(0.5f, 0.5f);
                    _lobbyHeroCardRT.pivot = new Vector2(0.5f, 0.5f);
                    _lobbyHeroCardRT.anchoredPosition = new Vector2(-440f, -20f);
                    _lobbyHeroCardRT.sizeDelta = new Vector2(660f, 820f);
                }
                if (_lobbyHubGridRT != null)
                {
                    _lobbyHubGridRT.anchorMin = new Vector2(0.5f, 0.5f);
                    _lobbyHubGridRT.anchorMax = new Vector2(0.5f, 0.5f);
                    _lobbyHubGridRT.pivot = new Vector2(0.5f, 0.5f);
                    _lobbyHubGridRT.anchoredPosition = new Vector2(440f, 110f);
                    _lobbyHubGridRT.sizeDelta = new Vector2(800f, 380f);
                    if (_lobbyHubGridLayout != null)
                    {
                        _lobbyHubGridLayout.cellSize = new Vector2(385f, 170f);
                        _lobbyHubGridLayout.spacing = new Vector2(20f, 20f);
                        _lobbyHubGridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
                        _lobbyHubGridLayout.constraintCount = 2;
                        _lobbyHubGridLayout.startCorner = GridLayoutGroup.Corner.UpperLeft;
                        _lobbyHubGridLayout.startAxis = GridLayoutGroup.Axis.Horizontal;
                    }
                    LayoutRebuilder.ForceRebuildLayoutImmediate(_lobbyHubGridRT);
                }
                if (_questCardRT != null)
                {
                    _questCardRT.anchorMin = new Vector2(0.5f, 0.5f);
                    _questCardRT.anchorMax = new Vector2(0.5f, 0.5f);
                    _questCardRT.pivot = new Vector2(0.5f, 0.5f);
                    _questCardRT.anchoredPosition = new Vector2(440f, -210f);
                    _questCardRT.sizeDelta = new Vector2(800f, 150f);
                }

                // --- BATTLE SCREEN (LANDSCAPE 16:9: Left Board + Right Sidebar) ---
                if (_boardOuterFrameRT != null)
                {
                    float boardSize = 860f;
                    _boardOuterFrameRT.anchorMin = new Vector2(0.5f, 0.5f);
                    _boardOuterFrameRT.anchorMax = new Vector2(0.5f, 0.5f);
                    _boardOuterFrameRT.pivot = new Vector2(0.5f, 0.5f);
                    _boardOuterFrameRT.anchoredPosition = new Vector2(-360f, 0f);
                    _boardOuterFrameRT.sizeDelta = new Vector2(boardSize, boardSize);
                }
                if (_battleTopHudRT != null)
                {
                    _battleTopHudRT.anchorMin = new Vector2(0.5f, 0.5f);
                    _battleTopHudRT.anchorMax = new Vector2(0.5f, 0.5f);
                    _battleTopHudRT.pivot = new Vector2(0.5f, 0.5f);
                    _battleTopHudRT.anchoredPosition = new Vector2(520f, 330f);
                    _battleTopHudRT.sizeDelta = new Vector2(680f, 150f);
                }
                if (_battleRelicBarRT != null)
                {
                    _battleRelicBarRT.anchorMin = new Vector2(0.5f, 0.5f);
                    _battleRelicBarRT.anchorMax = new Vector2(0.5f, 0.5f);
                    _battleRelicBarRT.pivot = new Vector2(0.5f, 0.5f);
                    _battleRelicBarRT.anchoredPosition = new Vector2(520f, 160f);
                    _battleRelicBarRT.sizeDelta = new Vector2(680f, 120f);
                }
                if (_battleBottomBarRT != null)
                {
                    _battleBottomBarRT.anchorMin = new Vector2(0.5f, 0.5f);
                    _battleBottomBarRT.anchorMax = new Vector2(0.5f, 0.5f);
                    _battleBottomBarRT.pivot = new Vector2(0.5f, 0.5f);
                    _battleBottomBarRT.anchoredPosition = new Vector2(520f, -140f);
                    _battleBottomBarRT.sizeDelta = new Vector2(680f, 260f);

                    HorizontalLayoutGroup hLayout = _battleBottomBarRT.GetComponent<HorizontalLayoutGroup>();
                    if (hLayout != null) DestroyImmediate(hLayout);

                    GridLayoutGroup gLayout = _battleBottomBarRT.GetComponent<GridLayoutGroup>();
                    if (gLayout == null) gLayout = _battleBottomBarRT.gameObject.AddComponent<GridLayoutGroup>();
                    gLayout.childAlignment = TextAnchor.MiddleCenter;
                    gLayout.cellSize = new Vector2(212f, 116f);
                    gLayout.spacing = new Vector2(16f, 14f);
                    gLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
                    gLayout.constraintCount = 3;

                    Vector2 skillBtnSize = new Vector2(212f, 116f);
                    if (_hintBtn != null) _hintBtn.GetComponent<RectTransform>().sizeDelta = skillBtnSize;
                    if (_undoBtn != null) _undoBtn.GetComponent<RectTransform>().sizeDelta = skillBtnSize;
                    if (_bombBtn != null) _bombBtn.GetComponent<RectTransform>().sizeDelta = skillBtnSize;
                    if (_freezeBtn != null) _freezeBtn.GetComponent<RectTransform>().sizeDelta = skillBtnSize;
                    if (_swapBtn != null) _swapBtn.GetComponent<RectTransform>().sizeDelta = skillBtnSize;
                    if (_restartBtn != null) _restartBtn.GetComponent<RectTransform>().sizeDelta = skillBtnSize;
                }

                // Modal sizes in landscape (Clamped)
                Vector2 largeModalSize = new Vector2(Mathf.Min(1020f, canvasW - 60f), Mathf.Min(840f, canvasH - 60f));
                if (_towerCardRT != null) _towerCardRT.sizeDelta = largeModalSize;
                if (_caravanCardRT != null) _caravanCardRT.sizeDelta = largeModalSize;
                if (_themeCardRT != null) _themeCardRT.sizeDelta = largeModalSize;
                if (_leaderboardCardRT != null) _leaderboardCardRT.sizeDelta = largeModalSize;
                if (_modeCardRT != null) _modeCardRT.sizeDelta = new Vector2(Mathf.Min(920f, canvasW - 60f), Mathf.Min(800f, canvasH - 60f));
                if (_luckyWheelCardRT != null) _luckyWheelCardRT.sizeDelta = new Vector2(Mathf.Min(740f, canvasW - 60f), Mathf.Min(700f, canvasH - 60f));
                if (_reviveCardRT != null) _reviveCardRT.sizeDelta = new Vector2(Mathf.Min(760f, canvasW - 60f), Mathf.Min(580f, canvasH - 60f));
                if (_resultCardRT != null) _resultCardRT.sizeDelta = new Vector2(Mathf.Min(740f, canvasW - 60f), Mathf.Min(600f, canvasH - 60f));
            }
            else
            {
                // PORTRAIT 1080 x 1920 (Match Width = 0.0f for consistent 1080 canvas width across all phones & tablets)
                _canvasScaler.referenceResolution = new Vector2(1080f, 1920f);
                _canvasScaler.matchWidthOrHeight = 0.0f;

                float aspect = (float)ScreenW / Mathf.Max(1, ScreenH);
                float canvasW = 1080f;
                float canvasH = 1080f / Mathf.Max(0.35f, aspect);

                // --- LOBBY SCREEN (PORTRAIT) ---
                if (_headerCardRT != null)
                {
                    _headerCardRT.anchorMin = new Vector2(0.5f, 1f);
                    _headerCardRT.anchorMax = new Vector2(0.5f, 1f);
                    _headerCardRT.pivot = new Vector2(0.5f, 1f);
                    _headerCardRT.anchoredPosition = new Vector2(0f, -22f);
                    _headerCardRT.sizeDelta = new Vector2(Mathf.Min(1000f, canvasW - 40f), 54f);
                }
                if (_lobbyHeroCardRT != null)
                {
                    _lobbyHeroCardRT.anchorMin = new Vector2(0.5f, 1f);
                    _lobbyHeroCardRT.anchorMax = new Vector2(0.5f, 1f);
                    _lobbyHeroCardRT.pivot = new Vector2(0.5f, 1f);
                    _lobbyHeroCardRT.anchoredPosition = new Vector2(0f, -96f);
                    _lobbyHeroCardRT.sizeDelta = new Vector2(Mathf.Min(1000f, canvasW - 40f), Mathf.Min(780f, canvasH * 0.44f));
                }
                if (_lobbyHubGridRT != null)
                {
                    _lobbyHubGridRT.anchorMin = new Vector2(0.5f, 1f);
                    _lobbyHubGridRT.anchorMax = new Vector2(0.5f, 1f);
                    _lobbyHubGridRT.pivot = new Vector2(0.5f, 1f);
                    float heroBottom = (_lobbyHeroCardRT != null) ? (_lobbyHeroCardRT.anchoredPosition.y - _lobbyHeroCardRT.sizeDelta.y) : -876f;
                    _lobbyHubGridRT.anchoredPosition = new Vector2(0f, heroBottom - 18f);
                    float hubW = Mathf.Min(1000f, canvasW - 40f);
                    _lobbyHubGridRT.sizeDelta = new Vector2(hubW, 186f);
                    if (_lobbyHubGridLayout != null)
                    {
                        float colW = Mathf.Floor((hubW - 16f) * 0.5f);
                        _lobbyHubGridLayout.cellSize = new Vector2(colW, 82f);
                        _lobbyHubGridLayout.spacing = new Vector2(16f, 14f);
                        _lobbyHubGridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
                        _lobbyHubGridLayout.constraintCount = 2;
                        _lobbyHubGridLayout.startCorner = GridLayoutGroup.Corner.UpperLeft;
                        _lobbyHubGridLayout.startAxis = GridLayoutGroup.Axis.Horizontal;
                    }
                    LayoutRebuilder.ForceRebuildLayoutImmediate(_lobbyHubGridRT);
                }
                if (_questCardRT != null)
                {
                    _questCardRT.anchorMin = new Vector2(0.5f, 1f);
                    _questCardRT.anchorMax = new Vector2(0.5f, 1f);
                    _questCardRT.pivot = new Vector2(0.5f, 1f);
                    float gridBottom = (_lobbyHubGridRT != null) ? (_lobbyHubGridRT.anchoredPosition.y - _lobbyHubGridRT.sizeDelta.y) : -1080f;
                    _questCardRT.anchoredPosition = new Vector2(0f, gridBottom - 14f);
                    _questCardRT.sizeDelta = new Vector2(Mathf.Min(1000f, canvasW - 40f), 108f);
                }

                // --- BATTLE SCREEN (PORTRAIT) ---
                float maxHudBottomBarW = Mathf.Min(1020f, canvasW - 40f);
                if (_battleTopHudRT != null)
                {
                    _battleTopHudRT.anchorMin = new Vector2(0.5f, 1f);
                    _battleTopHudRT.anchorMax = new Vector2(0.5f, 1f);
                    _battleTopHudRT.pivot = new Vector2(0.5f, 1f);
                    _battleTopHudRT.anchoredPosition = new Vector2(0f, -20f);
                    _battleTopHudRT.sizeDelta = new Vector2(maxHudBottomBarW, 128f);
                }
                if (_battleRelicBarRT != null)
                {
                    _battleRelicBarRT.anchorMin = new Vector2(0.5f, 1f);
                    _battleRelicBarRT.anchorMax = new Vector2(0.5f, 1f);
                    _battleRelicBarRT.pivot = new Vector2(0.5f, 1f);
                    _battleRelicBarRT.anchoredPosition = new Vector2(0f, -156f);
                    _battleRelicBarRT.sizeDelta = new Vector2(Mathf.Min(1020f, canvasW - 40f), 46f);
                }
                if (_boardOuterFrameRT != null)
                {
                    // Dynamically calculate available height so board never collides with HUD or bottom bar
                    float availBoardH = canvasH - 128f - 116f - 80f;
                    float boardSize = Mathf.Clamp(Mathf.Min(canvasW - 48f, availBoardH), 600f, 1020f);
                    _boardOuterFrameRT.anchorMin = new Vector2(0.5f, 0.5f);
                    _boardOuterFrameRT.anchorMax = new Vector2(0.5f, 0.5f);
                    _boardOuterFrameRT.pivot = new Vector2(0.5f, 0.5f);
                    _boardOuterFrameRT.anchoredPosition = new Vector2(0f, -4f);
                    _boardOuterFrameRT.sizeDelta = new Vector2(boardSize, boardSize);
                }
                if (_battleBottomBarRT != null)
                {
                    _battleBottomBarRT.anchorMin = new Vector2(0.5f, 0f);
                    _battleBottomBarRT.anchorMax = new Vector2(0.5f, 0f);
                    _battleBottomBarRT.pivot = new Vector2(0.5f, 0f);
                    _battleBottomBarRT.anchoredPosition = new Vector2(0f, 26f);
                    _battleBottomBarRT.sizeDelta = new Vector2(maxHudBottomBarW, 122f);

                    GridLayoutGroup gLayout = _battleBottomBarRT.GetComponent<GridLayoutGroup>();
                    if (gLayout != null) DestroyImmediate(gLayout);

                    HorizontalLayoutGroup hLayout = _battleBottomBarRT.GetComponent<HorizontalLayoutGroup>();
                    if (hLayout == null) hLayout = _battleBottomBarRT.gameObject.AddComponent<HorizontalLayoutGroup>();
                    hLayout.childAlignment = TextAnchor.MiddleCenter;
                    hLayout.spacing = 10f;
                    hLayout.childControlWidth = false;
                    hLayout.childControlHeight = false;

                    float singleBtnW = Mathf.Floor((maxHudBottomBarW - 5 * 10f) / 6f);
                    Vector2 skillBtnSize = new Vector2(singleBtnW, 118f);
                    if (_hintBtn != null) _hintBtn.GetComponent<RectTransform>().sizeDelta = skillBtnSize;
                    if (_undoBtn != null) _undoBtn.GetComponent<RectTransform>().sizeDelta = skillBtnSize;
                    if (_bombBtn != null) _bombBtn.GetComponent<RectTransform>().sizeDelta = skillBtnSize;
                    if (_freezeBtn != null) _freezeBtn.GetComponent<RectTransform>().sizeDelta = skillBtnSize;
                    if (_swapBtn != null) _swapBtn.GetComponent<RectTransform>().sizeDelta = skillBtnSize;
                    if (_restartBtn != null) _restartBtn.GetComponent<RectTransform>().sizeDelta = skillBtnSize;
                }

                // Modal sizes in portrait (Clamped to screen height)
                Vector2 largeModalSize = new Vector2(Mathf.Min(980f, canvasW - 40f), Mathf.Min(1180f, canvasH - 80f));
                if (_towerCardRT != null) _towerCardRT.sizeDelta = largeModalSize;
                if (_caravanCardRT != null) _caravanCardRT.sizeDelta = largeModalSize;
                if (_themeCardRT != null) _themeCardRT.sizeDelta = largeModalSize;
                if (_leaderboardCardRT != null) _leaderboardCardRT.sizeDelta = largeModalSize;
                if (_modeCardRT != null) _modeCardRT.sizeDelta = new Vector2(Mathf.Min(920f, canvasW - 40f), Mathf.Min(840f, canvasH - 80f));
                if (_luckyWheelCardRT != null) _luckyWheelCardRT.sizeDelta = new Vector2(Mathf.Min(740f, canvasW - 40f), Mathf.Min(700f, canvasH - 80f));
                if (_reviveCardRT != null) _reviveCardRT.sizeDelta = new Vector2(Mathf.Min(760f, canvasW - 40f), Mathf.Min(580f, canvasH - 80f));
                if (_resultCardRT != null) _resultCardRT.sizeDelta = new Vector2(Mathf.Min(780f, canvasW - 40f), Mathf.Min(760f, canvasH - 80f));
            }

            ResizeBoardGridCells();
        }

        private void BuildReviveModal(Transform canvasTransform)
        {
            _reviveModalRoot = CreateModalBackdrop("ReviveModal", canvasTransform, out RectTransform cardRT);
            _reviveCardRT = cardRT;
            cardRT.sizeDelta = new Vector2(760f, 560f);

            // Hero Shield Icon at Top Center
            CreateIconImage(cardRT, XOProceduralAssets.IconShield, new Vector2(0.5f, 1f), new Vector2(0f, -72f), new Vector2(96f, 96f));

            _reviveTitleText = CreateText(cardRT, "SECOND CHANCE!", 36, FontStyle.Bold, TextAnchor.UpperCenter);
            _reviveTitleText.rectTransform.offsetMax = new Vector2(0f, -128f);
            _reviveTitleText.color = XOProceduralAssets.ColCoral;

            // Recessed Description Box
            GameObject descBox = CreateUIObject("ReviveDescBox", cardRT);
            RectTransform dbRT = descBox.GetComponent<RectTransform>();
            dbRT.anchorMin = new Vector2(0.5f, 0.5f);
            dbRT.anchorMax = new Vector2(0.5f, 0.5f);
            dbRT.anchoredPosition = new Vector2(0f, 18f);
            dbRT.sizeDelta = new Vector2(680f, 140f);
            Image dbImg = descBox.AddComponent<Image>();
            dbImg.sprite = XOProceduralAssets.CardPanelSprite;
            dbImg.type = Image.Type.Sliced;
            dbImg.color = XOProceduralAssets.ColTileEdge;
            dbImg.raycastTarget = false;

            _reviveDescText = CreateText(dbRT,
                "Opponent just connected 5 in a row!\nWatch a short ad to UNDO AI's winning move and highlight the exact square to block!",
                20, FontStyle.Normal, TextAnchor.MiddleCenter);
            _reviveDescText.rectTransform.offsetMin = new Vector2(20f, 10f);
            _reviveDescText.rectTransform.offsetMax = new Vector2(-20f, -10f);
            _reviveDescText.color = XOProceduralAssets.ColInk;

            _reviveActionBtn = CreateIcon3DButton(cardRT, XOProceduralAssets.IconVideoAd, "WATCH AD: REVIVE & BLOCK NOW!", new Vector2(660f, 76f), XOProceduralAssets.ColMint, XOProceduralAssets.ColInk, 22);
            RectTransform revBtnRT = _reviveActionBtn.GetComponent<RectTransform>();
            revBtnRT.anchorMin = new Vector2(0.5f, 0f);
            revBtnRT.anchorMax = new Vector2(0.5f, 0f);
            revBtnRT.anchoredPosition = new Vector2(0f, 136f);
            _reviveActionBtnText = _reviveActionBtn.GetComponentInChildren<Text>();
            _reviveActionBtn.onClick.AddListener(OnConfirmReviveWithAd);

            Button giveUpBtn = Create3DButton(cardRT, "No thanks, accept defeat", new Vector2(360f, 52f), XOProceduralAssets.ColTileEdge, XOProceduralAssets.ColMuted, 18);
            RectTransform giveUpRT = giveUpBtn.GetComponent<RectTransform>();
            giveUpRT.anchorMin = new Vector2(0.5f, 0f);
            giveUpRT.anchorMax = new Vector2(0.5f, 0f);
            giveUpRT.anchoredPosition = new Vector2(0f, 55f);
            giveUpBtn.onClick.AddListener(() =>
            {
                PlaySfx("click");
                _reviveModalRoot.SetActive(false);
                ProcessPlayerDefeatConsequences();
            });

            _reviveModalRoot.SetActive(false);
        }

        private void BuildResultModal(Transform canvasTransform)
        {
            _resultModalRoot = CreateModalBackdrop("ResultModal", canvasTransform, out RectTransform cardRT);
            _resultCardRT = cardRT;
            cardRT.sizeDelta = new Vector2(700f, 540f);

            // Large Golden Trophy / Swords Hero Icon at Top
            _resultHeroIconImg = CreateIconImage(cardRT, XOProceduralAssets.IconTrophy, new Vector2(0.5f, 1f), new Vector2(0f, -68f), new Vector2(96f, 96f));

            _resultTitleText = CreateText(cardRT, "CHIẾN THẮNG!", 36, FontStyle.Bold, TextAnchor.UpperCenter);
            _resultTitleText.rectTransform.offsetMax = new Vector2(0f, -122f);
            _resultTitleText.color = XOProceduralAssets.ColInk;

            // Recessed Reward Summary Card
            GameObject rewardBox = CreateUIObject("RewardSummaryBox", cardRT);
            RectTransform rbRT = rewardBox.GetComponent<RectTransform>();
            rbRT.anchorMin = new Vector2(0.5f, 1f);
            rbRT.anchorMax = new Vector2(0.5f, 1f);
            rbRT.pivot = new Vector2(0.5f, 1f);
            rbRT.anchoredPosition = new Vector2(0f, -172f);
            rbRT.sizeDelta = new Vector2(620f, 110f);
            Image rbImg = rewardBox.AddComponent<Image>();
            rbImg.sprite = XOProceduralAssets.CardPanelSprite;
            rbImg.type = Image.Type.Sliced;
            rbImg.color = XOProceduralAssets.ColTileEdge;
            rbImg.raycastTarget = false;

            _resultSubText = CreateText(rbRT, "Phần thưởng trận đấu: +70 Coins", 21, FontStyle.Bold, TextAnchor.MiddleCenter);
            _resultSubText.rectTransform.offsetMin = new Vector2(16f, 8f);
            _resultSubText.rectTransform.offsetMax = new Vector2(-16f, -8f);
            _resultSubText.color = XOProceduralAssets.ColInk;

            // Rank & Win Streak Shield Button (Shown on Defeat)
            _shieldAdBtn = CreateIcon3DButton(cardRT, XOProceduralAssets.IconShield, "XEM QC: BẢO VỆ CHUỖI THẮNG & SAO", new Vector2(620f, 62f), XOProceduralAssets.ColLilac, Color.white, 19);
            RectTransform shieldRT = _shieldAdBtn.GetComponent<RectTransform>();
            shieldRT.anchorMin = new Vector2(0.5f, 0f);
            shieldRT.anchorMax = new Vector2(0.5f, 0f);
            shieldRT.pivot = new Vector2(0.5f, 0f);
            shieldRT.anchoredPosition = new Vector2(0f, 154f);
            _shieldAdBtnText = _shieldAdBtn.GetComponentInChildren<Text>();
            _shieldAdBtn.onClick.AddListener(OnWatchAdActivateRankShield);

            // Double Coins (2x Reward) 3D Button with Video Ad Icon
            _doubleRewardAdBtn = CreateIcon3DButton(cardRT, XOProceduralAssets.IconVideoAd, "XEM QC: NHẬN 2X COINS (+140)", new Vector2(620f, 62f), XOProceduralAssets.ColSun, XOProceduralAssets.ColInk, 20);
            RectTransform x2RT = _doubleRewardAdBtn.GetComponent<RectTransform>();
            x2RT.anchorMin = new Vector2(0.5f, 0f);
            x2RT.anchorMax = new Vector2(0.5f, 0f);
            x2RT.pivot = new Vector2(0.5f, 0f);
            x2RT.anchoredPosition = new Vector2(0f, 154f);
            _doubleRewardAdBtnText = _doubleRewardAdBtn.GetComponentInChildren<Text>();
            _doubleRewardAdBtn.onClick.AddListener(OnWatchAdDoubleMatchReward);

            // Play Again / Next Stage 3D Button
            _resultPlayAgainBtn = CreateIcon3DButton(cardRT, XOProceduralAssets.IconNewGame, "TIẾP TỤC", new Vector2(620f, 62f), XOProceduralAssets.ColMint, XOProceduralAssets.ColInk, 21);
            RectTransform againRT = _resultPlayAgainBtn.GetComponent<RectTransform>();
            againRT.anchorMin = new Vector2(0.5f, 0f);
            againRT.anchorMax = new Vector2(0.5f, 0f);
            againRT.pivot = new Vector2(0.5f, 0f);
            againRT.anchoredPosition = new Vector2(0f, 82f);
            _resultPlayAgainBtnText = _resultPlayAgainBtn.GetComponentInChildren<Text>();
            _resultPlayAgainBtn.onClick.AddListener(() =>
            {
                PlaySfx("click");
                _resultModalRoot.SetActive(false);
                if (currentGameMode == GameMode.TowerClimb)
                {
                    if (XORoguelikeRunManager.Lives <= 0)
                    {
                        XORoguelikeRunManager.StartNewRun();
                        ReturnToLobby();
                        OpenTowerModal();
                    }
                    else
                    {
                        StartTowerMatch(XORoguelikeRunManager.CurrentFloor);
                    }
                }
                else
                {
                    StartNewMatch(currentBoardSize);
                }
            });

            // Return to Main Menu / Lobby Button
            Button returnToLobbyBtn = CreateIcon3DButton(cardRT, XOProceduralAssets.IconHome, "VỀ SẢNH CHÍNH", new Vector2(620f, 54f), XOProceduralAssets.ColTileEdge, XOProceduralAssets.ColInk, 19);
            RectTransform lbyRT = returnToLobbyBtn.GetComponent<RectTransform>();
            lbyRT.anchorMin = new Vector2(0.5f, 0f);
            lbyRT.anchorMax = new Vector2(0.5f, 0f);
            lbyRT.pivot = new Vector2(0.5f, 0f);
            lbyRT.anchoredPosition = new Vector2(0f, 14f);
            returnToLobbyBtn.onClick.AddListener(() =>
            {
                PlaySfx("click");
                _resultModalRoot.SetActive(false);
                ReturnToLobby();
            });

            _resultModalRoot.SetActive(false);
        }

        private ScrollRect CreateModalScrollView(string name, RectTransform cardRT, out RectTransform contentRT, out GameObject scrollObj)
        {
            scrollObj = CreateUIObject(name, cardRT);
            RectTransform svRT = scrollObj.GetComponent<RectTransform>();
            svRT.anchorMin = new Vector2(0f, 0f);
            svRT.anchorMax = new Vector2(1f, 1f);
            svRT.offsetMin = new Vector2(24f, 24f);
            svRT.offsetMax = new Vector2(-24f, -164f);

            scrollObj.AddComponent<RectMask2D>();

            ScrollRect sr = scrollObj.AddComponent<ScrollRect>();
            sr.horizontal = false;
            sr.vertical = true;
            sr.movementType = ScrollRect.MovementType.Clamped;
            sr.scrollSensitivity = 28f;
            sr.inertia = true;
            sr.decelerationRate = 0.135f;

            GameObject contentObj = CreateUIObject("Content", svRT);
            contentRT = contentObj.GetComponent<RectTransform>();
            contentRT.anchorMin = new Vector2(0f, 1f);
            contentRT.anchorMax = new Vector2(1f, 1f);
            contentRT.pivot = new Vector2(0.5f, 1f);
            contentRT.anchoredPosition = Vector2.zero;
            contentRT.sizeDelta = Vector2.zero;

            VerticalLayoutGroup vLayout = contentObj.AddComponent<VerticalLayoutGroup>();
            vLayout.spacing = 10f;
            vLayout.childControlWidth = true;
            vLayout.childControlHeight = false;
            vLayout.childForceExpandWidth = true;
            vLayout.childForceExpandHeight = false;
            vLayout.padding = new RectOffset(4, 4, 4, 16);

            ContentSizeFitter csf = contentObj.AddComponent<ContentSizeFitter>();
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            sr.viewport = svRT;
            sr.content = contentRT;
            return sr;
        }

        private void BuildThemeShopModal(Transform canvasTransform)
        {
            _themeModalRoot = CreateModalBackdrop("ThemeShopModal", canvasTransform, out RectTransform cardRT);
            _themeCardRT = cardRT;
            cardRT.sizeDelta = new Vector2(940f, 920f);

            CreateIconImage(cardRT, XOProceduralAssets.IconSkinShop, new Vector2(0.5f, 1f), new Vector2(-215f, -45f), new Vector2(46f, 46f));
            CreateText(cardRT, "SKIN & THEME SHOP", 34, FontStyle.Bold, TextAnchor.UpperCenter)
                .rectTransform.offsetMax = new Vector2(25f, -26f);

            Button closeBtn = Create3DButton(cardRT, "X", new Vector2(50f, 48f), Color.white, XOProceduralAssets.ColInk, 20);
            RectTransform closeRT = closeBtn.GetComponent<RectTransform>();
            closeRT.anchorMin = new Vector2(1f, 1f);
            closeRT.anchorMax = new Vector2(1f, 1f);
            closeRT.anchoredPosition = new Vector2(-36f, -36f);
            closeBtn.onClick.AddListener(() =>
            {
                PlaySfx("click");
                _themeModalRoot.SetActive(false);
            });

            // 3 Shop Tabs: [THEMES (4)] | [SKINS (5)] | [SKILLS (BUY)]
            GameObject tabRow = CreateUIObject("ShopTabsRow", cardRT);
            RectTransform tabRT = tabRow.GetComponent<RectTransform>();
            tabRT.anchorMin = new Vector2(0.5f, 1f);
            tabRT.anchorMax = new Vector2(0.5f, 1f);
            tabRT.pivot = new Vector2(0.5f, 1f);
            tabRT.anchoredPosition = new Vector2(0f, -88f);
            tabRT.sizeDelta = new Vector2(860f, 60f);

            HorizontalLayoutGroup tabLayout = tabRow.AddComponent<HorizontalLayoutGroup>();
            tabLayout.spacing = 16f;
            tabLayout.childControlWidth = true;
            tabLayout.childControlHeight = true;
            tabLayout.childForceExpandWidth = true;

            _tabThemesBtn = CreateIcon3DButton(tabRT, XOProceduralAssets.IconSkinShop, "THEMES (4)", new Vector2(270f, 60f), XOProceduralAssets.ColSky, Color.white, 18);
            _tabThemesBtn.onClick.AddListener(() => SwitchShopTab(0));

            _tabPiecesBtn = CreateIcon3DButton(tabRT, XOProceduralAssets.IconCrown, "SKINS (5)", new Vector2(270f, 60f), XOProceduralAssets.ColTileEdge, XOProceduralAssets.ColInk, 18);
            _tabPiecesBtn.onClick.AddListener(() => SwitchShopTab(1));

            _tabSkillsBtn = CreateIcon3DButton(tabRT, XOProceduralAssets.IconBomb, "SKILLS (BUY)", new Vector2(270f, 60f), XOProceduralAssets.ColTileEdge, XOProceduralAssets.ColInk, 18);
            _tabSkillsBtn.onClick.AddListener(() => SwitchShopTab(2));

            // Scrollable Container 1: Board Themes List
            CreateModalScrollView("ThemesScroll", cardRT, out RectTransform tListRT, out _themesScrollViewObj);
            _themesListContainer = tListRT.gameObject;

            _themeCards.Clear();
            var themes = XOThemeDatabase.Themes;
            for (int i = 0; i < themes.Length; i++)
            {
                int idx = i;
                XOThemePalette t = themes[i];
                _themeCards.Add(CreateShopItemRow(
                    tListRT,
                    XOProceduralAssets.XPieceSprite,
                    XOProceduralAssets.OPieceSprite,
                    t.cellNormalColor,
                    t.xColor,
                    t.oColor,
                    t.displayName,
                    t.subtitle,
                    () => OnThemePrimaryActionClicked(idx),
                    () => OnThemeAdUnlockClicked(idx),
                    idx));
            }

            // Scrollable Container 2: X/O Piece Skins List
            CreateModalScrollView("SkinsScroll", cardRT, out RectTransform pListRT, out _piecesScrollViewObj);
            _piecesListContainer = pListRT.gameObject;

            _pieceSkinCards.Clear();
            var pieceSkins = XOThemeDatabase.PieceSkins;
            for (int i = 0; i < pieceSkins.Length; i++)
            {
                int idx = i;
                XOPieceSkin ps = pieceSkins[i];
                Sprite sx = XOProceduralAssets.GetPieceSkinSprite(ps.id, true);
                Sprite so = XOProceduralAssets.GetPieceSkinSprite(ps.id, false);
                Color cx = (ps.id == "classic_xo") ? XOThemeDatabase.Hex("#22D3EE") : Color.white;
                Color co = (ps.id == "classic_xo") ? XOThemeDatabase.Hex("#FB7185") : Color.white;

                _pieceSkinCards.Add(CreateShopItemRow(
                    pListRT,
                    sx,
                    so,
                    XOThemeDatabase.Hex("#1E293B"),
                    cx,
                    co,
                    ps.displayName,
                    ps.subtitle,
                    () => OnPieceSkinPrimaryActionClicked(idx),
                    () => OnPieceSkinAdUnlockClicked(idx),
                    idx));
            }

            // Scrollable Container 3: Tactical Skill Consumable Shop
            CreateModalScrollView("SkillsScroll", cardRT, out RectTransform sListRT, out _skillsScrollViewObj);
            _skillsListContainer = sListRT.gameObject;

            CreateSkillBuyRow(sListRT, XOProceduralAssets.IconHint, "AI Hint (+1)", "Spot the optimal Gomoku winning or blocking square", 20, () => HintStock, () => { HintStock++; });
            CreateSkillBuyRow(sListRT, XOProceduralAssets.IconUndo, "Undo Move (+1)", "Revert your last move immediately", 20, () => UndoStock, () => { UndoStock++; });
            CreateSkillBuyRow(sListRT, XOProceduralAssets.IconFreeze, "Freeze Tile (+1)", "Lock any empty square for 3 turns", 30, () => FreezeStock, () => { FreezeStock++; });
            CreateSkillBuyRow(sListRT, XOProceduralAssets.IconBomb, "Bomb Piece (+1)", "Destroy 1 opponent piece on the board", 35, () => BombStock, () => { BombStock++; });
            CreateSkillBuyRow(sListRT, XOProceduralAssets.IconSwap, "Swap Piece (+1)", "Convert 1 opponent piece into your X", 40, () => SwapStock, () => { SwapStock++; });

            _piecesScrollViewObj.SetActive(false);
            _skillsScrollViewObj.SetActive(false);
            _themeModalRoot.SetActive(false);
        }

        private ShopCardRefs CreateShopItemRow(
            RectTransform parent,
            Sprite leftIcon,
            Sprite rightIcon,
            Color swatchBg,
            Color leftTint,
            Color rightTint,
            string title,
            string subtitle,
            UnityEngine.Events.UnityAction onPrimary,
            UnityEngine.Events.UnityAction onAdUnlock,
            int index)
        {
            GameObject itemObj = CreateUIObject("ShopItem_" + title, parent);
            LayoutElement le = itemObj.AddComponent<LayoutElement>();
            le.minHeight = 108f;
            le.preferredHeight = 108f;
            le.flexibleHeight = 0f;

            Image itemBg = itemObj.AddComponent<Image>();
            itemBg.sprite = XOProceduralAssets.CardPanelSprite;
            itemBg.type = Image.Type.Sliced;
            itemBg.color = Color.white;

            RectTransform itemRT = itemObj.GetComponent<RectTransform>();

            // Dual-Icon Swatch Preview Box
            GameObject previewBox = CreateUIObject("PreviewSwatch", itemRT);
            RectTransform prevRT = previewBox.GetComponent<RectTransform>();
            prevRT.anchorMin = new Vector2(0f, 0.5f);
            prevRT.anchorMax = new Vector2(0f, 0.5f);
            prevRT.pivot = new Vector2(0f, 0.5f);
            prevRT.anchoredPosition = new Vector2(16f, 0f);
            prevRT.sizeDelta = new Vector2(120f, 82f);
            Image prevImg = previewBox.AddComponent<Image>();
            prevImg.sprite = XOProceduralAssets.CardPanelSprite;
            prevImg.type = Image.Type.Sliced;
            prevImg.color = swatchBg;

            Image ico1 = CreateIconImage(prevRT, leftIcon, new Vector2(0.5f, 0.5f), new Vector2(-26f, 0f), new Vector2(46f, 46f));
            ico1.color = leftTint;
            Image ico2 = CreateIconImage(prevRT, rightIcon, new Vector2(0.5f, 0.5f), new Vector2(26f, 0f), new Vector2(46f, 46f));
            ico2.color = rightTint;

            Text nameTxt = CreateText(itemRT, title, 21, FontStyle.Bold, TextAnchor.UpperLeft);
            nameTxt.rectTransform.offsetMin = new Vector2(154f, 0f);
            nameTxt.rectTransform.offsetMax = new Vector2(-240f, -14f);
            nameTxt.color = XOProceduralAssets.ColInk;

            Text subTxt = CreateText(itemRT, subtitle, 14, FontStyle.Normal, TextAnchor.MiddleLeft);
            subTxt.rectTransform.offsetMin = new Vector2(154f, 0f);
            subTxt.rectTransform.offsetMax = new Vector2(-240f, 0f);
            subTxt.color = XOProceduralAssets.ColMuted;

            Text statusTxt = CreateText(itemRT, "", 14, FontStyle.Bold, TextAnchor.LowerLeft);
            statusTxt.rectTransform.offsetMin = new Vector2(154f, 14f);
            statusTxt.rectTransform.offsetMax = new Vector2(-240f, 0f);

            Button coinOrSelectBtn = Create3DButton(itemRT, "EQUIP", new Vector2(210f, 44f), XOProceduralAssets.ColMint, XOProceduralAssets.ColInk, 17);
            RectTransform btn1RT = coinOrSelectBtn.GetComponent<RectTransform>();
            btn1RT.anchorMin = new Vector2(1f, 0.5f);
            btn1RT.anchorMax = new Vector2(1f, 0.5f);
            btn1RT.pivot = new Vector2(1f, 0.5f);
            btn1RT.anchoredPosition = new Vector2(-16f, 24f);
            coinOrSelectBtn.onClick.AddListener(onPrimary);

            Button adUnlockBtn = CreateIcon3DButton(itemRT, XOProceduralAssets.IconVideoAd, "Free via Ad", new Vector2(210f, 42f), XOProceduralAssets.ColSun, XOProceduralAssets.ColInk, 16);
            RectTransform btn2RT = adUnlockBtn.GetComponent<RectTransform>();
            btn2RT.anchorMin = new Vector2(1f, 0.5f);
            btn2RT.anchorMax = new Vector2(1f, 0.5f);
            btn2RT.pivot = new Vector2(1f, 0.5f);
            btn2RT.anchoredPosition = new Vector2(-16f, -24f);
            adUnlockBtn.onClick.AddListener(onAdUnlock);

            return new ShopCardRefs
            {
                index = index,
                cardBg = itemBg,
                statusText = statusTxt,
                selectOrCoinBtn = coinOrSelectBtn,
                selectOrCoinBtnText = coinOrSelectBtn.GetComponentInChildren<Text>(),
                watchAdUnlockBtn = adUnlockBtn
            };
        }

        private GameObject CreateModalBackdrop(string name, Transform parent, out RectTransform cardRT)
        {
            GameObject root = CreateUIObject(name, parent);
            RectTransform rt = root.GetComponent<RectTransform>();
            StretchFull(rt);

            Image backdrop = root.AddComponent<Image>();
            backdrop.color = new Color(0.08f, 0.12f, 0.22f, 0.72f); // Deep pastel clay modal scrim
            backdrop.raycastTarget = true;

            GameObject card = CreateUIObject("ModalCard", rt);
            cardRT = card.GetComponent<RectTransform>();
            cardRT.anchorMin = new Vector2(0.5f, 0.5f);
            cardRT.anchorMax = new Vector2(0.5f, 0.5f);
            cardRT.pivot = new Vector2(0.5f, 0.5f);

            Image cardImg = card.AddComponent<Image>();
            cardImg.sprite = XOProceduralAssets.RoundedRectSprite;
            cardImg.type = Image.Type.Sliced;
            cardImg.color = Color.white;
            cardImg.raycastTarget = true;

            return root;
        }

        private void BuildLeaderboardModal(Transform canvasTransform)
        {
            _leaderboardModalRoot = CreateModalBackdrop("LeaderboardModal", canvasTransform, out RectTransform cardRT);
            _leaderboardCardRT = cardRT;
            cardRT.sizeDelta = new Vector2(960f, 940f);

            CreateIconImage(cardRT, XOProceduralAssets.IconLeaderboard, new Vector2(0.5f, 1f), new Vector2(-285f, -42f), new Vector2(44f, 44f));
            Text lbTitle = CreateText(cardRT, "ARENA CAMPAIGN & ACHIEVEMENTS", 28, FontStyle.Bold, TextAnchor.UpperCenter);
            lbTitle.rectTransform.offsetMax = new Vector2(25f, -24f);
            lbTitle.color = XOProceduralAssets.ColInk;

            Button closeBtn = Create3DButton(cardRT, "X", new Vector2(48f, 46f), Color.white, XOProceduralAssets.ColInk, 20);
            RectTransform closeRT = closeBtn.GetComponent<RectTransform>();
            closeRT.anchorMin = new Vector2(1f, 1f);
            closeRT.anchorMax = new Vector2(1f, 1f);
            closeRT.anchoredPosition = new Vector2(-36f, -36f);
            closeBtn.onClick.AddListener(() =>
            {
                PlaySfx("click");
                _leaderboardModalRoot.SetActive(false);
            });

            // 2 Tabs: [ARENA & 6 BOTS CAMPAIGN] | [ACHIEVEMENTS & REWARDS]
            GameObject tabRow = CreateUIObject("LeaderboardTabsRow", cardRT);
            RectTransform tabRT = tabRow.GetComponent<RectTransform>();
            tabRT.anchorMin = new Vector2(0.5f, 1f);
            tabRT.anchorMax = new Vector2(0.5f, 1f);
            tabRT.pivot = new Vector2(0.5f, 1f);
            tabRT.anchoredPosition = new Vector2(0f, -80f);
            tabRT.sizeDelta = new Vector2(880f, 56f);

            HorizontalLayoutGroup tabLayout = tabRow.AddComponent<HorizontalLayoutGroup>();
            tabLayout.spacing = 16f;
            tabLayout.childControlWidth = true;
            tabLayout.childControlHeight = true;
            tabLayout.childForceExpandWidth = true;

            _tabArenaBtn = CreateIcon3DButton(tabRT, XOProceduralAssets.IconSwords, "ARENA & 6 BOTS CAMPAIGN", new Vector2(430f, 56f), XOProceduralAssets.ColSky, Color.white, 18);
            _tabArenaBtn.onClick.AddListener(() => SwitchLeaderboardTab(0));

            _tabAchievementsBtn = CreateIcon3DButton(tabRT, XOProceduralAssets.IconTrophy, "ACHIEVEMENTS & REWARDS", new Vector2(430f, 56f), XOProceduralAssets.ColTileEdge, XOProceduralAssets.ColInk, 18);
            _tabAchievementsBtn.onClick.AddListener(() => SwitchLeaderboardTab(1));

            // Summary Pill Banner
            GameObject summaryBar = CreateUIObject("LeaderboardSummaryBar", cardRT);
            RectTransform sbRT = summaryBar.GetComponent<RectTransform>();
            sbRT.anchorMin = new Vector2(0.5f, 1f);
            sbRT.anchorMax = new Vector2(0.5f, 1f);
            sbRT.pivot = new Vector2(0.5f, 1f);
            sbRT.anchoredPosition = new Vector2(0f, -146f);
            sbRT.sizeDelta = new Vector2(880f, 44f);
            Image sbImg = summaryBar.AddComponent<Image>();
            sbImg.sprite = XOProceduralAssets.PillSprite;
            sbImg.type = Image.Type.Sliced;
            sbImg.color = XOProceduralAssets.ColTileEdge;
            sbImg.raycastTarget = false;

            _leaderboardSummaryText = CreateText(sbRT, "YOUR ARENA RANK: #6  |  ELO: 720  |  Rivals Conquered: 0/6", 17, FontStyle.Bold, TextAnchor.MiddleCenter);
            _leaderboardSummaryText.color = XOProceduralAssets.ColInk;

            // Scrollable Container 1: Top 10 Arena & 6 Bots Campaign
            CreateModalScrollView("ArenaScroll", cardRT, out _leaderboardRowsContainer, out _leaderboardScrollViewObj);
            _leaderboardScrollViewObj.GetComponent<RectTransform>().offsetMax = new Vector2(-24f, -200f);

            // Scrollable Container 2: Lifetime Achievements
            CreateModalScrollView("AchScroll", cardRT, out _achievementsRowsContainer, out _achievementsScrollViewObj);
            _achievementsScrollViewObj.GetComponent<RectTransform>().offsetMax = new Vector2(-24f, -200f);

            _achievementsScrollViewObj.SetActive(false);
            _leaderboardModalRoot.SetActive(false);
        }

        private void OpenLeaderboardModal()
        {
            PlaySfx("click");
            // Automatically open to Achievements tab if the player has a reward ready to claim!
            int initialTab = XOProgressionSystem.HasAnyClaimableAchievement() ? 1 : _activeLeaderboardTab;
            SwitchLeaderboardTab(initialTab);
            _leaderboardModalRoot.transform.SetAsLastSibling();
            _leaderboardModalRoot.SetActive(true);
        }

        private void SwitchLeaderboardTab(int tabIdx)
        {
            PlaySfx("click");
            _activeLeaderboardTab = tabIdx;
            if (_leaderboardScrollViewObj != null) _leaderboardScrollViewObj.SetActive(tabIdx == 0);
            if (_achievementsScrollViewObj != null) _achievementsScrollViewObj.SetActive(tabIdx == 1);

            Color activeCol = XOProceduralAssets.ColSky;
            Color inactiveCol = XOProceduralAssets.ColTileEdge;
            Color activeTxt = Color.white;
            Color inactiveTxt = XOProceduralAssets.ColInk;

            if (_tabArenaBtn != null)
            {
                _tabArenaBtn.GetComponent<Image>().color = (tabIdx == 0) ? activeCol : inactiveCol;
                var t = _tabArenaBtn.GetComponentInChildren<Text>(); if (t != null) t.color = (tabIdx == 0) ? activeTxt : inactiveTxt;
            }
            if (_tabAchievementsBtn != null)
            {
                _tabAchievementsBtn.GetComponent<Image>().color = (tabIdx == 1) ? activeCol : inactiveCol;
                var t = _tabAchievementsBtn.GetComponentInChildren<Text>(); if (t != null) t.color = (tabIdx == 1) ? activeTxt : inactiveTxt;
            }

            if (tabIdx == 0)
                RefreshLeaderboardRowsUI();
            else
                RefreshAchievementsRowsUI();
        }

        private void RefreshLeaderboardRowsUI()
        {
            if (_leaderboardRowsContainer == null) return;

            for (int i = _leaderboardRowsContainer.childCount - 1; i >= 0; i--)
            {
                Destroy(_leaderboardRowsContainer.GetChild(i).gameObject);
            }

            List<XOLeaderboardEntry> entries = XOProgressionSystem.GetTop10Leaderboard(out int myRankPos);
            int conqueredCount = XOProgressionSystem.GetConqueredBotsCount();
            if (_leaderboardSummaryText != null)
            {
                _leaderboardSummaryText.text = $"YOUR ARENA RANK: #{myRankPos}   |   ELO: {XOProgressionSystem.PlayerELO}   |   CAMPAIGN RIVALS CONQUERED: {conqueredCount}/6";
            }

            int activeBotIdx = XOProgressionSystem.SelectedBotIndex;

            foreach (var entry in entries)
            {
                GameObject rowObj = CreateUIObject("RankRow_" + entry.rankPosition, _leaderboardRowsContainer);
                LayoutElement le = rowObj.AddComponent<LayoutElement>();
                le.minHeight = 64f;
                le.preferredHeight = 64f;
                le.flexibleHeight = 0f;

                Image rowBg = rowObj.AddComponent<Image>();
                rowBg.sprite = XOProceduralAssets.CardPanelSprite;
                rowBg.type = Image.Type.Sliced;

                bool isHuman = entry.isHumanPlayer;
                bool isCurrentRival = (entry.botIndexIfRival == activeBotIdx && currentGameMode == GameMode.VsAI);

                if (isHuman)
                    rowBg.color = XOThemeDatabase.Hex("#ECFDF5"); // soft mint
                else if (isCurrentRival)
                    rowBg.color = XOThemeDatabase.Hex("#FEFCE8"); // soft warm sun
                else
                    rowBg.color = Color.white; // clean porcelain

                RectTransform rowRT = rowObj.GetComponent<RectTransform>();

                // Rank # Badge
                Text rankTxt = CreateText(rowRT, $"#{entry.rankPosition}", 20, FontStyle.Bold, TextAnchor.MiddleLeft);
                rankTxt.rectTransform.offsetMin = new Vector2(18f, 0f);
                rankTxt.rectTransform.offsetMax = new Vector2(-780f, 0f);
                rankTxt.color = (entry.rankPosition <= 3) ? XOProceduralAssets.ColSun : XOProceduralAssets.ColMuted;

                // Player / Bot Name, Style Tag & Campaign Conquest Status
                string conquestBadge = "";
                if (!entry.isHumanPlayer && entry.botIndexIfRival >= 0)
                {
                    bool conquered = XOProgressionSystem.IsBotConquered(entry.botIndexIfRival);
                    conquestBadge = conquered ? "  [CONQUERED]" : "  [FIRST WIN: +80c]";
                }

                Text nameTxt = CreateText(rowRT, $"{entry.playerName} — {entry.titleTag}{conquestBadge}", 17, FontStyle.Bold, TextAnchor.MiddleLeft);
                nameTxt.rectTransform.offsetMin = new Vector2(80f, 0f);
                nameTxt.rectTransform.offsetMax = new Vector2(-330f, 0f);
                nameTxt.color = isHuman ? XOThemeDatabase.Hex("#059669") : XOProceduralAssets.ColInk;

                // ELO Rating
                Text eloTxt = CreateText(rowRT, $"ELO {entry.eloRating}", 18, FontStyle.Bold, TextAnchor.MiddleRight);
                eloTxt.rectTransform.offsetMin = new Vector2(450f, 0f);
                eloTxt.rectTransform.offsetMax = new Vector2(-205f, 0f);
                eloTxt.color = XOThemeDatabase.Hex("#D97706");

                // Challenge Bot Button (for the 6 playable AI Rivals)
                if (!entry.isHumanPlayer && entry.botIndexIfRival >= 0)
                {
                    int targetBotIdx = entry.botIndexIfRival;
                    string btnLabel = isCurrentRival ? "ACTIVE" : "CHALLENGE";
                    Color btnCol = isCurrentRival ? XOProceduralAssets.ColMint : XOProceduralAssets.ColSky;
                    Color txtCol = isCurrentRival ? XOProceduralAssets.ColInk : Color.white;

                    Button challengeBtn = Create3DButton(rowRT, btnLabel, new Vector2(170f, 44f), btnCol, txtCol, 15);
                    RectTransform cbRT = challengeBtn.GetComponent<RectTransform>();
                    cbRT.anchorMin = new Vector2(1f, 0.5f);
                    cbRT.anchorMax = new Vector2(1f, 0.5f);
                    cbRT.pivot = new Vector2(1f, 0.5f);
                    cbRT.anchoredPosition = new Vector2(-14f, 0f);
                    challengeBtn.onClick.AddListener(() =>
                    {
                        PlaySfx("click");
                        XOProgressionSystem.SelectedBotIndex = targetBotIdx;
                        XOBotProfile chosenBot = XOProgressionSystem.GetActiveBot();
                        currentGameMode = GameMode.VsAI;
                        currentDifficulty = chosenBot.baseDifficulty;
                        _scoreX = 0;
                        _scoreO = 0;
                        _leaderboardModalRoot.SetActive(false);
                        StartNewMatch(currentBoardSize);
                    });
                }
            }
        }

        private void RefreshAchievementsRowsUI()
        {
            if (_achievementsRowsContainer == null) return;

            for (int i = _achievementsRowsContainer.childCount - 1; i >= 0; i--)
            {
                Destroy(_achievementsRowsContainer.GetChild(i).gameObject);
            }

            List<XOAchievementState> list = XOProgressionSystem.GetAchievementsList();
            int claimedCount = 0;
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i].isClaimed) claimedCount++;
            }

            if (_leaderboardSummaryText != null)
            {
                _leaderboardSummaryText.text = $"LIFETIME ACHIEVEMENTS COMPLETED: {claimedCount}/{list.Count}   |   Complete milestones to claim Coins & Free Skills!";
            }

            foreach (var ach in list)
            {
                GameObject rowObj = CreateUIObject("AchRow_" + ach.id, _achievementsRowsContainer);
                LayoutElement le = rowObj.AddComponent<LayoutElement>();
                le.minHeight = 74f;
                le.preferredHeight = 74f;
                le.flexibleHeight = 0f;

                Image rowBg = rowObj.AddComponent<Image>();
                rowBg.sprite = XOProceduralAssets.CardPanelSprite;
                rowBg.type = Image.Type.Sliced;
                rowBg.color = ach.IsReadyToClaim
                    ? XOThemeDatabase.Hex("#ECFDF5")
                    : (ach.isClaimed ? XOThemeDatabase.Hex("#F1F5F9") : Color.white);

                RectTransform rowRT = rowObj.GetComponent<RectTransform>();

                CreateIconImage(rowRT, XOProceduralAssets.IconTrophy, new Vector2(0f, 0.5f), new Vector2(38f, 0f), new Vector2(44f, 44f));

                // Title & Description
                Text titleTxt = CreateText(rowRT, $"{ach.title}  ({ach.currentValue}/{ach.targetValue})", 19, FontStyle.Bold, TextAnchor.UpperLeft);
                titleTxt.rectTransform.offsetMin = new Vector2(76f, 0f);
                titleTxt.rectTransform.offsetMax = new Vector2(-280f, -10f);
                titleTxt.color = ach.IsReadyToClaim ? XOThemeDatabase.Hex("#059669") : (ach.isClaimed ? XOProceduralAssets.ColMuted : XOProceduralAssets.ColInk);

                Text descTxt = CreateText(rowRT, ach.description, 14, FontStyle.Normal, TextAnchor.LowerLeft);
                descTxt.rectTransform.offsetMin = new Vector2(76f, 10f);
                descTxt.rectTransform.offsetMax = new Vector2(-280f, 0f);
                descTxt.color = XOProceduralAssets.ColMuted;

                // Reward Label
                Text rewTxt = CreateText(rowRT, $"+{ach.rewardCoins} Coins\n{ach.bonusItemLabel}", 15, FontStyle.Bold, TextAnchor.MiddleRight);
                rewTxt.rectTransform.offsetMin = new Vector2(480f, 0f);
                rewTxt.rectTransform.offsetMax = new Vector2(-205f, 0f);
                rewTxt.color = XOThemeDatabase.Hex("#D97706");

                // Claim / Progress Button
                string btnLabel = ach.isClaimed
                    ? "CLAIMED"
                    : (ach.IsReadyToClaim ? "CLAIM REWARD!" : $"{ach.currentValue}/{ach.targetValue}");
                Color btnCol = ach.isClaimed
                    ? XOProceduralAssets.ColTileEdge
                    : (ach.IsReadyToClaim ? XOProceduralAssets.ColMint : XOProceduralAssets.ColTileEdge);
                Color txtCol = ach.isClaimed
                    ? XOProceduralAssets.ColMuted
                    : (ach.IsReadyToClaim ? XOProceduralAssets.ColInk : XOProceduralAssets.ColMuted);

                Button claimBtn = Create3DButton(rowRT, btnLabel, new Vector2(175f, 48f), btnCol, txtCol, 15);
                RectTransform cbRT = claimBtn.GetComponent<RectTransform>();
                cbRT.anchorMin = new Vector2(1f, 0.5f);
                cbRT.anchorMax = new Vector2(1f, 0.5f);
                cbRT.pivot = new Vector2(1f, 0.5f);
                cbRT.anchoredPosition = new Vector2(-14f, 0f);
                claimBtn.interactable = ach.IsReadyToClaim;

                string achId = ach.id;
                int rewardCoins = ach.rewardCoins;
                string bonusItem = ach.bonusItemLabel;
                claimBtn.onClick.AddListener(() =>
                {
                    XOProgressionSystem.MarkAchievementClaimed(achId);
                    PlaySfx("chest");
                    Coins += rewardCoins;
                    if (bonusItem.Contains("Hint")) HintStock += 1;
                    else if (bonusItem.Contains("Swap")) SwapStock += 1;
                    else if (bonusItem.Contains("Bomb")) BombStock += 1;
                    else if (bonusItem.Contains("Freeze")) FreezeStock += 1;
                    else if (bonusItem.Contains("Undo")) UndoStock += 1;
                    else if (bonusItem.Contains("All"))
                    {
                        HintStock += 2; UndoStock += 2; BombStock += 2; FreezeStock += 2; SwapStock += 2;
                    }
                    RefreshBottomActionBadges();
                    RefreshAchievementsRowsUI();
                });
            }
        }

        // =====================================================================
        // LUCKY SPIN WHEEL MODAL (1 FREE SPIN + REWARDED AD SPINS)
        // =====================================================================

        private struct WheelPrizeSlot
        {
            public string title;
            public string subtitle;
            public Color slotColor;
            public Sprite icon;
            public int coins;
            public string itemType;
            public int itemCount;
        }

        private WheelPrizeSlot[] GetWheelPrizes()
        {
            return new WheelPrizeSlot[]
            {
                new WheelPrizeSlot { title = "+15 COINS",      subtitle = "Coin Pouch",       slotColor = XOThemeDatabase.Hex("#0EA5E9"), icon = XOProceduralAssets.IconCoin,  coins = 15,  itemType = "",       itemCount = 0 },
                new WheelPrizeSlot { title = "+1 AI HINT",     subtitle = "Tactical Radar",   slotColor = XOThemeDatabase.Hex("#D97706"), icon = XOProceduralAssets.IconHint,  coins = 5,   itemType = "Hint",   itemCount = 1 },
                new WheelPrizeSlot { title = "+1 BOMB",        subtitle = "Board Blaster",    slotColor = XOThemeDatabase.Hex("#DC2626"), icon = XOProceduralAssets.IconBomb,  coins = 5,   itemType = "Bomb",   itemCount = 1 },
                new WheelPrizeSlot { title = "+30 COINS",      subtitle = "Rich Chest",       slotColor = XOThemeDatabase.Hex("#16A34A"), icon = XOProceduralAssets.IconChest, coins = 30,  itemType = "",       itemCount = 0 },
                new WheelPrizeSlot { title = "+1 SWAP & ICE",  subtitle = "Combo Pack",       slotColor = XOThemeDatabase.Hex("#7C3AED"), icon = XOProceduralAssets.IconSwap,  coins = 10,  itemType = "Combo",  itemCount = 1 },
                new WheelPrizeSlot { title = "JACKPOT +100!",  subtitle = "+100 Coins & All", slotColor = XOThemeDatabase.Hex("#E11D48"), icon = XOProceduralAssets.IconCrown, coins = 100, itemType = "Jackpot",itemCount = 1 }
            };
        }

        private void BuildLuckyWheelModal(Transform canvasTransform)
        {
            _luckyWheelModalRoot = CreateModalBackdrop("LuckyWheelModal", canvasTransform, out RectTransform rt);
            _luckyWheelCardRT = rt;
            rt.sizeDelta = new Vector2(720f, 680f);

            CreateIconImage(rt, XOProceduralAssets.IconLuckyWheel, new Vector2(0.5f, 1f), new Vector2(-215f, -46f), new Vector2(54f, 54f));

            Text title = CreateText(rt, "LUCKY PRIZE SPIN!", 30, FontStyle.Bold, TextAnchor.MiddleCenter);
            title.rectTransform.anchorMin = new Vector2(0f, 1f);
            title.rectTransform.anchorMax = new Vector2(1f, 1f);
            title.rectTransform.pivot = new Vector2(0.5f, 1f);
            title.rectTransform.anchoredPosition = new Vector2(18f, -18f);
            title.rectTransform.sizeDelta = new Vector2(-40f, 54f);
            title.color = XOProceduralAssets.ColInk;

            _wheelStatusText = CreateText(rt, "Spin the Lucky Wheel for Instant Coins, Bombs, Swaps & Jackpot!", 17, FontStyle.Bold, TextAnchor.MiddleCenter);
            _wheelStatusText.rectTransform.anchorMin = new Vector2(0f, 1f);
            _wheelStatusText.rectTransform.anchorMax = new Vector2(1f, 1f);
            _wheelStatusText.rectTransform.pivot = new Vector2(0.5f, 1f);
            _wheelStatusText.rectTransform.anchoredPosition = new Vector2(0f, -74f);
            _wheelStatusText.rectTransform.sizeDelta = new Vector2(-48f, 36f);
            _wheelStatusText.color = XOProceduralAssets.ColMuted;

            // 6 Prize Slots arranged in a 3x2 Grid
            GameObject gridObj = CreateUIObject("WheelSlotsGrid", rt);
            RectTransform gridRT = gridObj.GetComponent<RectTransform>();
            gridRT.anchorMin = new Vector2(0f, 0f);
            gridRT.anchorMax = new Vector2(1f, 1f);
            gridRT.offsetMin = new Vector2(28f, 168f);
            gridRT.offsetMax = new Vector2(-28f, -122f);

            GridLayoutGroup glg = gridObj.AddComponent<GridLayoutGroup>();
            glg.cellSize = new Vector2(210f, 185f);
            glg.spacing = new Vector2(16f, 16f);
            glg.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            glg.constraintCount = 3;
            glg.childAlignment = TextAnchor.MiddleCenter;

            _wheelSlotBgs.Clear();
            WheelPrizeSlot[] prizes = GetWheelPrizes();
            for (int i = 0; i < prizes.Length; i++)
            {
                WheelPrizeSlot p = prizes[i];
                GameObject slotObj = CreateUIObject("PrizeSlot_" + i, gridRT);
                Image slotBg = slotObj.AddComponent<Image>();
                slotBg.sprite = XOProceduralAssets.Button3DSprite;
                slotBg.type = Image.Type.Sliced;
                slotBg.color = p.slotColor;
                _wheelSlotBgs.Add(slotBg);

                RectTransform sRT = slotObj.GetComponent<RectTransform>();
                CreateIconImage(sRT, p.icon, new Vector2(0.5f, 1f), new Vector2(0f, -48f), new Vector2(64f, 64f));

                Text pTitle = CreateText(sRT, p.title, 21, FontStyle.Bold, TextAnchor.MiddleCenter);
                pTitle.rectTransform.anchorMin = new Vector2(0f, 0f);
                pTitle.rectTransform.anchorMax = new Vector2(1f, 0f);
                pTitle.rectTransform.pivot = new Vector2(0.5f, 0f);
                pTitle.rectTransform.anchoredPosition = new Vector2(0f, 44f);
                pTitle.rectTransform.sizeDelta = new Vector2(-12f, 34f);
                pTitle.color = Color.white;

                Text pSub = CreateText(sRT, p.subtitle, 15, FontStyle.Bold, TextAnchor.MiddleCenter);
                pSub.rectTransform.anchorMin = new Vector2(0f, 0f);
                pSub.rectTransform.anchorMax = new Vector2(1f, 0f);
                pSub.rectTransform.pivot = new Vector2(0.5f, 0f);
                pSub.rectTransform.anchoredPosition = new Vector2(0f, 16f);
                pSub.rectTransform.sizeDelta = new Vector2(-12f, 26f);
                pSub.color = XOThemeDatabase.Hex("#FEF08A");
            }

            // Primary Spin Button (Free Spin first, then Bonus Ad Spin)
            _wheelSpinActionBtn = CreateIcon3DButton(
                rt,
                XOProceduralAssets.IconLuckyWheel,
                "SPIN NOW (1 FREE SPIN!)",
                new Vector2(640f, 68f),
                XOThemeDatabase.Hex("#22C55E"),
                Color.white,
                22
            );
            _wheelSpinActionBtnText = _wheelSpinActionBtn.GetComponentInChildren<Text>();
            RectTransform spinRT = _wheelSpinActionBtn.GetComponent<RectTransform>();
            spinRT.anchorMin = new Vector2(0.5f, 0f);
            spinRT.anchorMax = new Vector2(0.5f, 0f);
            spinRT.pivot = new Vector2(0.5f, 0f);
            spinRT.anchoredPosition = new Vector2(0f, 88f);
            _wheelSpinActionBtn.onClick.AddListener(OnSpinWheelClicked);

            // Close Button
            Button closeBtn = Create3DButton(rt, "CLOSE", new Vector2(640f, 54f), XOThemeDatabase.Hex("#334155"), Color.white, 20);
            RectTransform closeRT = closeBtn.GetComponent<RectTransform>();
            closeRT.anchorMin = new Vector2(0.5f, 0f);
            closeRT.anchorMax = new Vector2(0.5f, 0f);
            closeRT.pivot = new Vector2(0.5f, 0f);
            closeRT.anchoredPosition = new Vector2(0f, 22f);
            closeBtn.onClick.AddListener(() =>
            {
                if (_isWheelSpinning) return;
                PlaySfx("click");
                _luckyWheelModalRoot.SetActive(false);
            });

            _luckyWheelModalRoot.SetActive(false);
        }

        private void OpenLuckyWheelModal()
        {
            PlaySfx("click");
            RefreshLuckyWheelUI();
            _luckyWheelModalRoot.transform.SetAsLastSibling();
            _luckyWheelModalRoot.SetActive(true);
        }

        private void RefreshLuckyWheelUI()
        {
            if (_wheelSpinActionBtn == null || _wheelSpinActionBtnText == null) return;
            WheelPrizeSlot[] prizes = GetWheelPrizes();
            for (int i = 0; i < _wheelSlotBgs.Count && i < prizes.Length; i++)
            {
                _wheelSlotBgs[i].color = prizes[i].slotColor;
                _wheelSlotBgs[i].transform.localScale = Vector3.one;
            }

            if (FreeSpinAvailable)
            {
                _wheelSpinActionBtnText.text = "SPIN NOW (1 FREE DAILY SPIN!)";
                _wheelSpinActionBtn.GetComponent<Image>().color = XOThemeDatabase.Hex("#22C55E");
            }
            else
            {
                _wheelSpinActionBtnText.text = "WATCH AD: BONUS LUCKY SPIN!";
                _wheelSpinActionBtn.GetComponent<Image>().color = XOThemeDatabase.Hex("#F59E0B");
            }
        }

        private void OnSpinWheelClicked()
        {
            if (_isWheelSpinning) return;

            if (FreeSpinAvailable)
            {
                FreeSpinAvailable = false;
                StartCoroutine(SpinWheelRoutine());
            }
            else
            {
                PlaySfx("click");
                XOPlayableAdsBridge.Instance.ShowRewardedAd(
                    "Bonus Lucky Wheel Spin (Win up to +250 Coins & Tactical Skills!)",
                    onRewarded: () =>
                    {
                        StartCoroutine(SpinWheelRoutine());
                    });
            }
        }

        private IEnumerator SpinWheelRoutine()
        {
            _isWheelSpinning = true;
            _wheelSpinActionBtn.interactable = false;
            _wheelStatusText.text = "Spinning the Lucky Prize Wheel...";

            WheelPrizeSlot[] prizes = GetWheelPrizes();
            int prizeCount = prizes.Length;
            int winningIdx = UnityEngine.Random.Range(0, prizeCount);
            int totalSteps = prizeCount * 3 + winningIdx;

            for (int step = 0; step <= totalSteps; step++)
            {
                int activeIdx = step % prizeCount;
                for (int i = 0; i < _wheelSlotBgs.Count && i < prizeCount; i++)
                {
                    bool highlight = (i == activeIdx);
                    _wheelSlotBgs[i].color = highlight ? XOThemeDatabase.Hex("#FBBF24") : prizes[i].slotColor;
                    _wheelSlotBgs[i].transform.localScale = highlight ? new Vector3(1.07f, 1.07f, 1f) : Vector3.one;
                }

                PlaySfx("spin_tick");
                if (enableHaptics) XOPlayableAdsBridge.Instance?.TriggerHaptic(10);

                float progress = step / (float)Mathf.Max(1, totalSteps);
                float wait = Mathf.Lerp(0.055f, 0.24f, progress * progress);
                yield return new WaitForSeconds(wait);
            }

            // Grant Prize
            WheelPrizeSlot won = prizes[winningIdx];
            PlaySfx(won.itemType == "Jackpot" ? "jackpot" : "chest");
            if (enableHaptics) XOPlayableAdsBridge.Instance?.TriggerHaptic(45);

            Coins += won.coins;
            if (won.itemType == "Hint") HintStock += won.itemCount;
            else if (won.itemType == "Bomb") BombStock += won.itemCount;
            else if (won.itemType == "Combo") { SwapStock += 1; FreezeStock += 1; }
            else if (won.itemType == "Jackpot")
            {
                HintStock += 1; UndoStock += 1; BombStock += 1; FreezeStock += 1; SwapStock += 1;
            }

            _wheelStatusText.text = $"YOU WON: {won.title} ({won.subtitle})!";
            _wheelSpinActionBtn.interactable = true;
            _isWheelSpinning = false;
            RefreshLuckyWheelUI();
            RefreshHeaderAndProgressionUI();
            RefreshBottomActionBadges();
        }

        // =====================================================================
        // BOARD LOGIC, AI RIVALS, SPECIAL TILES & TACTICAL SKILLS
        // =====================================================================

        public void StartNewMatch(int boardSize)
        {
            StopAllCoroutines();
            _isAIThinking = false;
            _reviveUsedThisMatch = false;
            _usedPowerUpThisMatch = false;
            _botUsedCounterSkillThisMatch = false;
            _announcedFirstFiveX = false;
            _announcedFirstFiveO = false;
            _midMatchShiftTriggered = false;
            _isOutOfMovesPuzzleRevive = false;
            _skillTargeting = ActiveSkillTargeting.None;
            _activeHintCell = new Vector2Int(-1, -1);
            _turnTimerRemaining = turnTimeLimit;

            if (currentGameMode == GameMode.PuzzleStage)
            {
                currentBoardSize = 15;
                winLength = 5;
                _engine.Initialize(currentBoardSize, winLength, spawnSpecialTiles: false, variant: XOVariantRule.ClassicCaro);
                _currentPuzzle = XOProgressionSystem.GeneratePuzzleStage(XOProgressionSystem.CurrentPuzzleStage, currentBoardSize);
                _engine.LoadPuzzleStage(_currentPuzzle);
                _puzzleMovesLeft = _currentPuzzle.maxPlayerMoves;
                _activeHintCell = _engine.CalculateBestHint(CellPiece.X, out _);
            }
            else
            {
                currentBoardSize = boardSize;
                // Only spawn Special Tiles (Gold, Lightning, Mines, Portals, Rocks) in Tower of Anomalies mode!
                // Normal matches are pure, competitive Gomoku (No coin tiles, no buggy extra-turns)
                bool spawnSpecials = (currentGameMode == GameMode.TowerClimb);
                int? specialSeed = (currentGameMode == GameMode.TowerClimb) ? XORoguelikeRunManager.GetFloorSeed(XORoguelikeRunManager.CurrentFloor) : (int?)null;
                int floorNum = (currentGameMode == GameMode.TowerClimb) ? XORoguelikeRunManager.CurrentFloor : 0;
                _engine.Initialize(currentBoardSize, winLength, spawnSpecialTiles: spawnSpecials, variant: currentVariant, specialTilesSeed: specialSeed, floorNumber: floorNum);
                currentBoardSize = _engine.BoardSize;
                winLength = _engine.WinLength;
            }

            if (_winStrikeLineImg != null)
                _winStrikeLineImg.gameObject.SetActive(false);

            RebuildBoardCellsIfNeeded(currentBoardSize);
            RefreshAllCells(animateLastMove: false);
            RefreshSelectorButtonsUI();
            RefreshHeaderAndProgressionUI();
            RefreshTurnAndScoreUI();
            RefreshBottomActionBadges();
            RefreshBattleRelicBarUI();

            if (currentGameMode == GameMode.VsAI)
            {
                XOBotProfile bot = XOProgressionSystem.GetActiveBot();
                ShowBotReactionBubble($"{bot.botName}: \"{bot.PickRandom(bot.greetingLines)}\"");
            }
            else if (currentGameMode == GameMode.TowerClimb)
            {
                TowerFloorBlind blind = XORoguelikeRunManager.GetActiveBlind();
                ShowBotReactionBubble($"{blind.bossName}: \"Floor {blind.floorNumber} challenge: {blind.blindName}! Let's battle!\"");
            }
            else if (_botChatBubbleObj != null)
            {
                _botChatBubbleObj.SetActive(false);
            }
        }

        private void RebuildBoardCellsIfNeeded(int size)
        {
            int targetCount = size * size;

            if (_cellViews.Count != targetCount)
            {
                foreach (var cv in _cellViews)
                {
                    if (cv != null) Destroy(cv.gameObject);
                }
                _cellViews.Clear();

                for (int r = 0; r < size; r++)
                {
                    for (int c = 0; c < size; c++)
                    {
                        GameObject cellObj = new GameObject($"Cell_{r}_{c}", typeof(RectTransform), typeof(Image), typeof(Button), typeof(XOCellView));
                        cellObj.transform.SetParent(_boardGridContainerRT, false);

                        Image cellImg = cellObj.GetComponent<Image>();
                        cellImg.sprite = XOProceduralAssets.SoftCellSprite;
                        cellImg.type = Image.Type.Sliced;

                        XOCellView view = cellObj.GetComponent<XOCellView>();
                        view.Initialize(r, c, _uiFont, OnBoardCellClicked);
                        _cellViews.Add(view);
                    }
                }
            }

            ResizeBoardGridCells();
        }

        private void ResizeBoardGridCells()
        {
            if (_boardGridLayout == null || _boardOuterFrameRT == null) return;
            int size = Mathf.Max(1, currentBoardSize);
            float boardPixelWidth = _boardOuterFrameRT.sizeDelta.x - 28f;
            float spacing = (size <= 10) ? 5f : 3.5f;
            float totalSpacing = spacing * (size - 1);
            float cellSize = Mathf.Floor((boardPixelWidth - totalSpacing) / size);

            _boardGridLayout.spacing = new Vector2(spacing, spacing);
            _boardGridLayout.cellSize = new Vector2(cellSize, cellSize);
            _boardGridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            _boardGridLayout.constraintCount = size;
        }

        private void OnBoardCellClicked(int row, int col)
        {
            if (_isAIThinking || _engine.State != MatchState.Playing)
                return;

            // Check Boss Mirror Aegis / Omni Crown Immunity in Tower Mode
            if (currentGameMode == GameMode.TowerClimb)
            {
                TowerFloorBlind blind = XORoguelikeRunManager.GetActiveBlind();
                if (_engine.IsBotImmuneToSkill(blind.botEquipment, "Bomb") && _skillTargeting == ActiveSkillTargeting.BombDestroy)
                {
                    PlaySfx("lose");
                    SpawnTacticalCalloutBanner("MIRROR AEGIS!", $"{blind.bossName} is immune to Bomb!", XOProceduralAssets.ColCoral);
                    _skillTargeting = ActiveSkillTargeting.None;
                    RefreshAllCells(animateLastMove: false);
                    RefreshTurnAndScoreUI();
                    return;
                }
                if (_engine.IsBotImmuneToSkill(blind.botEquipment, "Swap") && _skillTargeting == ActiveSkillTargeting.SwapPiece)
                {
                    PlaySfx("lose");
                    SpawnTacticalCalloutBanner("MIRROR AEGIS!", $"{blind.bossName} is immune to Swap!", XOProceduralAssets.ColLilac);
                    _skillTargeting = ActiveSkillTargeting.None;
                    RefreshAllCells(animateLastMove: false);
                    RefreshTurnAndScoreUI();
                    return;
                }
            }

            // 1. Bomb Targeting Mode
            if (_skillTargeting == ActiveSkillTargeting.BombDestroy)
            {
                if (_engine.Board[row, col] == CellPiece.O)
                {
                    BombStock--;
                    _usedPowerUpThisMatch = true;
                    _skillTargeting = ActiveSkillTargeting.None;
                    _engine.BombDestroyPiece(row, col, CellPiece.O);
                    PlaySfx("bomb");
                    SpawnCellVFXBurst(row, col, XOThemeDatabase.Hex("#EF4444"), XOThemeDatabase.Hex("#FBBF24"), 16);
                    RefreshAllCells(animateLastMove: false);
                    RefreshBottomActionBadges();
                    _statusBannerText.text = "BOOM! Destroyed Opponent's O piece! Still your turn.";

                    if (currentGameMode == GameMode.VsAI)
                    {
                        XOBotProfile bot = XOProgressionSystem.GetActiveBot();
                        ShowBotReactionBubble($"{bot.botName}: \"{bot.PickRandom(bot.onPlayerSkillUsedLines)}\"");
                    }
                }
                else
                {
                    _statusBannerText.text = "Tap on any pulsing red Opponent (O) piece on the board to destroy it!";
                    PlaySfx("click");
                }
                return;
            }

            // 2. Freeze Targeting Mode
            if (_skillTargeting == ActiveSkillTargeting.FreezeTile)
            {
                if (_engine.CanPlace(row, col))
                {
                    FreezeStock--;
                    _usedPowerUpThisMatch = true;
                    _skillTargeting = ActiveSkillTargeting.None;
                    _engine.FreezeEmptyCell(row, col, 3);
                    PlaySfx("freeze");
                    SpawnCellVFXBurst(row, col, XOThemeDatabase.Hex("#38BDF8"), Color.white, 14);
                    RefreshAllCells(animateLastMove: false);
                    RefreshBottomActionBadges();
                    _statusBannerText.text = "Tile frozen for 3 turns! Nobody can place on it.";

                    if (currentGameMode == GameMode.VsAI)
                    {
                        XOBotProfile bot = XOProgressionSystem.GetActiveBot();
                        ShowBotReactionBubble($"{bot.botName}: \"{bot.PickRandom(bot.onPlayerSkillUsedLines)}\"");
                    }
                }
                else
                {
                    _statusBannerText.text = "Tap on an open EMPTY tile on the board to freeze it!";
                    PlaySfx("click");
                }
                return;
            }

            // 3. Swap Piece Targeting Mode (Convert Opponent O into Player X!)
            if (_skillTargeting == ActiveSkillTargeting.SwapPiece)
            {
                if (_engine.Board[row, col] == CellPiece.O)
                {
                    SwapStock--;
                    _usedPowerUpThisMatch = true;
                    _skillTargeting = ActiveSkillTargeting.None;
                    _engine.SwapOpponentPiece(row, col, CellPiece.O, CellPiece.X);
                    PlaySfx("swap");
                    SpawnCellVFXBurst(row, col, XOThemeDatabase.Hex("#A855F7"), XOThemeDatabase.Hex("#22D3EE"), 18);
                    SpawnTacticalCalloutBanner("PIECE SWAPPED! (O -> X)", "Converted enemy piece into your X!", XOThemeDatabase.Hex("#9333EA"));
                    RefreshAllCells(animateLastMove: true);
                    RefreshBottomActionBadges();

                    if (_engine.State != MatchState.Playing)
                    {
                        HandleMatchEnded(_engine.State);
                        return;
                    }

                    _statusBannerText.text = "SWAPPED! Converted Opponent's O into your X! Still your turn.";
                    if (currentGameMode == GameMode.VsAI)
                    {
                        XOBotProfile bot = XOProgressionSystem.GetActiveBot();
                        ShowBotReactionBubble($"{bot.botName}: \"{bot.PickRandom(bot.onPlayerSkillUsedLines)}\"");
                    }
                }
                else
                {
                    _statusBannerText.text = "Tap on any pulsing purple Opponent (O) piece to convert it into your X!";
                    PlaySfx("click");
                }
                return;
            }

            // 4. Normal Move Placement (Supports Gravity Drop column landing!)
            if (_engine.VariantRule == XOVariantRule.GravityDrop)
            {
                int landingRow = _engine.GetGravityLandingRow(col);
                if (landingRow < 0)
                {
                    _statusBannerText.text = "This column is full! Tap another column to drop your piece.";
                    PlaySfx("click");
                    return;
                }
                row = landingRow;
            }

            if (_engine.IsRockObstacle(row, col))
            {
                _statusBannerText.text = "Rock Obstacle! Nobody can place on a rock square.";
                PlaySfx("click");
                return;
            }

            if (_engine.IsCellFrozen(row, col))
            {
                _statusBannerText.text = "This square is frozen! Choose another square.";
                PlaySfx("click");
                return;
            }

            if (!_engine.CanPlace(row, col))
                return;

            if (IsVsAIMode && _engine.CurrentTurn != CellPiece.X)
                return;

            ExecuteMove(row, col);

            if (currentGameMode == GameMode.PuzzleStage && _engine.State == MatchState.Playing)
            {
                _puzzleMovesLeft--;
                if (_puzzleMovesLeft <= 0)
                {
                    PromptOutOfPuzzleMovesRevive();
                    return;
                }
            }
            else if (currentGameMode == GameMode.TowerClimb && _engine.State == MatchState.Playing)
            {
                XORoguelikeRunManager.PlayerMovesThisMatch++;
                TowerFloorBlind blind = XORoguelikeRunManager.GetActiveBlind();
                if (blind.maxPlayerMoves > 0 && XORoguelikeRunManager.PlayerMovesThisMatch >= blind.maxPlayerMoves)
                {
                    PlaySfx("lose");
                    _statusBannerText.text = $"OUT OF MOVES! Limit of {blind.maxPlayerMoves} moves reached!";
                    StartCoroutine(ShowModalAfterDelay(0.6f, () => ProcessPlayerDefeatConsequences()));
                    return;
                }
            }

            if (_engine.State == MatchState.Playing
                && IsVsAIMode
                && _engine.CurrentTurn == CellPiece.O)
            {
                StartCoroutine(AITurnRoutine());
            }
        }

        private void ExecuteMove(int row, int col)
        {
            if (_engine.VariantRule == XOVariantRule.GravityDrop)
            {
                int landR = _engine.GetGravityLandingRow(col);
                if (landR >= 0) row = landR;
            }

            CellPiece mover = _engine.CurrentTurn;
            _activeHintCell = new Vector2Int(-1, -1);
            _turnTimerRemaining = turnTimeLimit;

            if (!_engine.PlacePiece(row, col, out SpecialTileType triggeredSpecial, out string tacticalCallout, out int calloutBonusCoins))
                return;

            bool isGobbleMove = !string.IsNullOrEmpty(tacticalCallout) && tacticalCallout.StartsWith("GOBBLED");
            PlaySfx(isGobbleMove ? "gobble" : (mover == CellPiece.X ? "place_x" : "place_o"));
            if (enableHaptics) XOPlayableAdsBridge.Instance?.TriggerHaptic(isGobbleMove ? 35 : 15);

            // Handle Special Board Tile effects (Gold, Lightning, Hidden Mine & Warp Portal!)
            if (triggeredSpecial == SpecialTileType.GoldBonus)
            {
                SpawnCellVFXBurst(row, col, XOThemeDatabase.Hex("#FBBF24"), Color.white, 14);
                if (mover == CellPiece.X)
                {
                    XOProgressionSystem.TotalSpecialTilesClaimed++;
                    int goldGain = XORoguelikeRunManager.HasRelic(CaroRelicType.BountyHunter) ? 40 : 20;
                    Coins += goldGain;
                    string bountyText = XORoguelikeRunManager.HasRelic(CaroRelicType.BountyHunter) ? "+40 Coins (Bounty Hunter 2x!)" : "+20 Bonus Coins!";
                    SpawnTacticalCalloutBanner("GOLD TILE CLAIMED!", bountyText, XOThemeDatabase.Hex("#D97706"));
                }
                else if (currentGameMode == GameMode.VsAI)
                {
                    XOBotProfile bot = XOProgressionSystem.GetActiveBot();
                    ShowBotReactionBubble($"{bot.botName}: \"Haha! Snagged the Gold Tile before you!\"");
                }
            }
            else if (triggeredSpecial == SpecialTileType.ExtraTurnLightning)
            {
                PlaySfx("lightning");
                SpawnCellVFXBurst(row, col, XOThemeDatabase.Hex("#FDE047"), XOThemeDatabase.Hex("#22D3EE"), 18);
                if (mover == CellPiece.X)
                {
                    XOProgressionSystem.TotalSpecialTilesClaimed++;
                    SpawnTacticalCalloutBanner("LIGHTNING BONUS TURN!", "You get to place 1 MORE piece right now!", XOThemeDatabase.Hex("#0EA5E9"));
                }
                else
                {
                    SpawnTacticalCalloutBanner("BOT BONUS TURN!", "Bot claimed a Lightning Tile & goes again!", XOThemeDatabase.Hex("#EF4444"));
                }
            }
            else if (triggeredSpecial == SpecialTileType.HiddenMine)
            {
                PlaySfx("bomb");
                SpawnCellVFXBurst(row, col, XOThemeDatabase.Hex("#EF4444"), XOThemeDatabase.Hex("#F97316"), 20);
                if (mover == CellPiece.X) XOProgressionSystem.TotalSpecialTilesClaimed++;
                SpawnTacticalCalloutBanner("MINE BLAST TRIGGERED!", "Blasted adjacent pieces around the mine!", XOThemeDatabase.Hex("#DC2626"));
            }
            else if (triggeredSpecial == SpecialTileType.WarpPortal)
            {
                PlaySfx("swap");
                SpawnCellVFXBurst(row, col, XOThemeDatabase.Hex("#A855F7"), XOThemeDatabase.Hex("#22D3EE"), 20);
                if (mover == CellPiece.X) XOProgressionSystem.TotalSpecialTilesClaimed++;
                SpawnTacticalCalloutBanner("WARP PORTAL CLONE!", "Spawned a BONUS Twin Piece on the linked Portal!", XOThemeDatabase.Hex("#9333EA"));
            }
            else if (!string.IsNullOrEmpty(tacticalCallout) && _engine.State == MatchState.Playing)
            {
                if (mover == CellPiece.X)
                {
                    XOProgressionSystem.TotalTacticalCallouts++;
                    PlaySfx("callout");
                    Coins += calloutBonusCoins;
                    SpawnTacticalCalloutBanner(tacticalCallout, $"+{calloutBonusCoins} Tactical Bonus Coins!", XOThemeDatabase.Hex("#16A34A"));
                    if (currentGameMode == GameMode.VsAI)
                    {
                        XOBotProfile bot = XOProgressionSystem.GetActiveBot();
                        ShowBotReactionBubble($"{bot.botName}: \"{bot.PickRandom(bot.onPlayerGoodMoveLines)}\"");
                    }
                    else if (currentGameMode == GameMode.TowerClimb)
                    {
                        TowerFloorBlind blind = XORoguelikeRunManager.GetActiveBlind();
                        ShowBotReactionBubble($"{blind.bossName}: \"Impressive move! But can you conquer the floor?\"");
                    }
                }
                else if (currentGameMode == GameMode.VsAI)
                {
                    XOBotProfile bot = XOProgressionSystem.GetActiveBot();
                    ShowBotReactionBubble($"{bot.botName}: \"{bot.PickRandom(bot.onBotThreatLines)}\"");
                }
                else if (currentGameMode == GameMode.TowerClimb)
                {
                    TowerFloorBlind blind = XORoguelikeRunManager.GetActiveBlind();
                    ShowBotReactionBubble($"{blind.bossName}: \"Feel the power of Floor {blind.floorNumber}!\"");
                }
            }

            // ROGUELIKE SHATTER HAMMER RELIC: Once per match, shatter adjacent Bot piece
            if (mover == CellPiece.X && currentGameMode == GameMode.TowerClimb
                && XORoguelikeRunManager.HasRelic(CaroRelicType.ShatterHammer)
                && !XORoguelikeRunManager.ShatterHammerTriggeredThisMatch)
            {
                if (_engine.TryExecuteShatterHammer(row, col, out Vector2Int shatteredCell))
                {
                    PlaySfx("bomb");
                    SpawnCellVFXBurst(shatteredCell.x, shatteredCell.y, XOProceduralAssets.ColCoral, XOProceduralAssets.ColSun, 20);
                    SpawnTacticalCalloutBanner("SHATTER HAMMER!", "Shattered and destroyed adjacent Bot piece!", XOProceduralAssets.ColCoral);
                }
            }

            // Tower Hazards: Collapsing Arena perimeter, Cosmic Shift lightning & Mid-Match Surprise Shifts
            if (currentGameMode == GameMode.TowerClimb && _engine.State == MatchState.Playing)
            {
                TowerFloorBlind activeBlind = XORoguelikeRunManager.GetActiveBlind();
                if (activeBlind != null)
                {
                    // Dynamic mid-match surprise shift
                    if (activeBlind.hasMidMatchShift && !_midMatchShiftTriggered && _engine.MoveHistory.Count >= activeBlind.midMatchShiftTurn)
                    {
                        _midMatchShiftTriggered = true;
                        PlaySfx("powerup");
                        StartCoroutine(ShakeBoardRoutine(0.35f, 12f));
                        SpawnTacticalCalloutBanner(activeBlind.midMatchShiftTitle, activeBlind.midMatchShiftDesc, XOProceduralAssets.ColCoral);

                        if (activeBlind.blindType == BossBlindType.TheArm)
                        {
                            _engine.VariantRule = XOVariantRule.StrictCaro;
                        }
                        else if (activeBlind.blindType == BossBlindType.TwinDragon)
                        {
                            _engine.RequiredWinLines = 2;
                        }
                        else if (activeBlind.blindType == BossBlindType.FogOfWar)
                        {
                            FreezeRandomStrategicTiles(3, 2);
                        }
                        else if (activeBlind.blindType == BossBlindType.CollapsingArena)
                        {
                            if (_engine.CollapseNextPerimeterRing(out var collapsed))
                            {
                                foreach (var cell in collapsed)
                                    SpawnCellVFXBurst(cell.x, cell.y, XOProceduralAssets.ColCoral, XOProceduralAssets.ColSun, 10);
                            }
                        }
                        else if (activeBlind.blindType == BossBlindType.StormColosseum)
                        {
                            if (_engine.ExecuteCosmicShift(out var struck))
                            {
                                foreach (var cell in struck)
                                    SpawnCellVFXBurst(cell.x, cell.y, XOProceduralAssets.ColSky, Color.white, 14);
                            }
                        }
                    }

                    // Secondary arena collapse (e.g. Move 18 or Floor 10 periodic)
                    if (activeBlind.hasCollapsingPerimeter && _engine.MoveHistory.Count > 0 && (_engine.MoveHistory.Count == 18 || (activeBlind.blindType == BossBlindType.OverlordApex && _engine.MoveHistory.Count % 10 == 0)))
                    {
                        if (_engine.CollapseNextPerimeterRing(out var collapsed))
                        {
                            PlaySfx("bomb");
                            foreach (var cell in collapsed)
                            {
                                SpawnCellVFXBurst(cell.x, cell.y, XOProceduralAssets.ColCoral, XOProceduralAssets.ColSun, 10);
                            }
                            SpawnTacticalCalloutBanner("ARENA COLLAPSED!", "Outer perimeter crumbled into rubble! Ring destroyed!", XOProceduralAssets.ColCoral);
                        }
                    }

                    // Secondary cosmic shift at Move 16
                    if (activeBlind.hasCosmicShift && _engine.MoveHistory.Count == 16)
                    {
                        if (_engine.ExecuteCosmicShift(out var struck))
                        {
                            PlaySfx("lightning");
                            foreach (var cell in struck)
                            {
                                SpawnCellVFXBurst(cell.x, cell.y, XOProceduralAssets.ColSky, Color.white, 14);
                            }
                            SpawnTacticalCalloutBanner("STORM TEMPEST!", "Second wave of lightning struck! Extra-Turn tiles charged!", XOProceduralAssets.ColSky);
                        }
                    }
                }
            }

            // Twin 5-in-a-Row Callouts (Floor 8 & 10)
            if (_engine.RequiredWinLines > 1 && _engine.State == MatchState.Playing)
            {
                if (mover == CellPiece.X && _engine.CompletedWinLinesX.Count == 1 && !_announcedFirstFiveX)
                {
                    _announcedFirstFiveX = true;
                    PlaySfx("reward");
                    SpawnTacticalCalloutBanner("1ST FIVE LOCKED! (1/2)", "Now connect a 2ND distinct 5-in-a-row to claim victory!", XOProceduralAssets.ColSun);
                }
                else if (mover == CellPiece.O && _engine.CompletedWinLinesO.Count == 1 && !_announcedFirstFiveO)
                {
                    _announcedFirstFiveO = true;
                    PlaySfx("lose");
                    SpawnTacticalCalloutBanner("BOSS LOCKED 1ST FIVE! (1/2)", "Boss needs only 1 more line to win! Block aggressively!", XOProceduralAssets.ColCoral);
                }
            }

            RefreshAllCells(animateLastMove: true);
            RefreshBottomActionBadges();

            if (_engine.State != MatchState.Playing)
            {
                HandleMatchEnded(_engine.State);
            }
            else
            {
                RefreshTurnAndScoreUI();
                if (triggeredSpecial == SpecialTileType.ExtraTurnLightning && mover == CellPiece.X)
                {
                    _statusBannerText.text = "LIGHTNING BONUS TURN! Place one more X immediately!";
                }
            }
        }

        private void FreezeRandomStrategicTiles(int count, int duration)
        {
            List<Vector2Int> candidates = _engine.GetCandidateCells(2);
            System.Random rnd = new System.Random();
            int frozen = 0;
            while (candidates.Count > 0 && frozen < count)
            {
                int idx = rnd.Next(candidates.Count);
                Vector2Int pos = candidates[idx];
                candidates.RemoveAt(idx);
                if (_engine.FreezeEmptyCell(pos.x, pos.y, duration))
                {
                    SpawnCellVFXBurst(pos.x, pos.y, XOThemeDatabase.Hex("#38BDF8"), Color.white, 12);
                    frozen++;
                }
            }
        }

        private IEnumerator AITurnRoutine()
        {
            _isAIThinking = true;
            RefreshTurnAndScoreUI();

            XOBotProfile activeBot = (currentGameMode == GameMode.VsAI) ? XOProgressionSystem.GetActiveBot() : null;
            TowerFloorBlind blind = (currentGameMode == GameMode.TowerClimb) ? XORoguelikeRunManager.GetActiveBlind() : null;
            AIDifficulty aiDiff = (currentGameMode == GameMode.PuzzleStage)
                ? AIDifficulty.Hard
                : (currentGameMode == GameMode.TowerClimb && blind != null)
                ? blind.difficulty
                : (activeBot != null ? activeBot.baseDifficulty : currentDifficulty);

            // Loop while it is O's turn (supports Bot Extra-Turn if Bot claims a Lightning Tile!)
            while (_engine.State == MatchState.Playing && _engine.CurrentTurn == CellPiece.O)
            {
                yield return new WaitForSeconds(0.32f);

                // Two-Way Bot Counter-Freeze (High-tier bots can freeze 1 dangerous player threat square once per match!)
                if (!_botUsedCounterSkillThisMatch && currentGameMode == GameMode.VsAI && activeBot != null)
                {
                    if (_engine.TryExecuteBotCounterFreeze(activeBot, out Vector2Int frozenCell))
                    {
                        _botUsedCounterSkillThisMatch = true;
                        PlaySfx("freeze");
                        SpawnCellVFXBurst(frozenCell.x, frozenCell.y, XOThemeDatabase.Hex("#38BDF8"), Color.white, 15);
                        RefreshAllCells(animateLastMove: false);
                        SpawnTacticalCalloutBanner($"{activeBot.botName.ToUpper()} COUNTER!", "Bot FROZE your open square for 3 turns!", XOThemeDatabase.Hex("#0284C7"));
                        ShowBotReactionBubble($"{activeBot.botName}: \"{activeBot.PickRandom(activeBot.onBotCounterSkillLines)}\"");
                        yield return new WaitForSeconds(0.45f);
                    }
                }

                // Relic: Guardian Aegis (Auto-freeze AI winning square)
                if (_engine.TryExecuteGuardianAegis(out Vector2Int aegisFrozenCell))
                {
                    PlaySfx("shield");
                    RefreshAllCells(animateLastMove: false);
                    SpawnTacticalCalloutBanner("GUARDIAN AEGIS!", "Relic auto-froze AI's winning threat for 2 turns!", XOProceduralAssets.ColLilac);
                    yield return new WaitForSeconds(0.45f);
                }

                // Tower Mode Boss Equipment Logic!
                if (currentGameMode == GameMode.TowerClimb && blind != null)
                {
                    BotEquipmentType bossEquip = blind.botEquipment;

                    // 1. Frost Staff / Omni Crown (Freeze Threat)
                    if (!_botUsedCounterSkillThisMatch && (bossEquip == BotEquipmentType.FrostStaff || bossEquip == BotEquipmentType.OmniCrown))
                    {
                        if (_engine.TryExecuteBotCounterFreeze(new XOBotProfile { counterSkill = BotCounterSkill.CryoFreeze }, out Vector2Int frozenCell))
                        {
                            _botUsedCounterSkillThisMatch = true;
                            PlaySfx("freeze");
                            SpawnCellVFXBurst(frozenCell.x, frozenCell.y, XOProceduralAssets.ColSky, Color.white, 15);
                            RefreshAllCells(animateLastMove: false);
                            SpawnTacticalCalloutBanner($"{blind.bossName.ToUpper()} RELIC!", "Frost Staff FROZE your open threat for 3 turns!", XOProceduralAssets.ColSky);
                            yield return new WaitForSeconds(0.45f);
                        }
                    }

                    // 2. Flame Blade (Bomb Player Piece)
                    if (!_botUsedCounterSkillThisMatch && bossEquip == BotEquipmentType.FlameBlade)
                    {
                        if (_engine.TryExecuteBotFlameBlade(out Vector2Int bombedCell))
                        {
                            _botUsedCounterSkillThisMatch = true;
                            PlaySfx("bomb");
                            SpawnCellVFXBurst(bombedCell.x, bombedCell.y, XOProceduralAssets.ColCoral, XOProceduralAssets.ColSun, 18);
                            RefreshAllCells(animateLastMove: false);
                            SpawnTacticalCalloutBanner($"{blind.bossName.ToUpper()} RELIC!", "Flame Blade BOMBED your critical piece!", XOProceduralAssets.ColCoral);
                            yield return new WaitForSeconds(0.45f);
                        }
                    }

                    // 3. Thorn Armor (Spawns a thorn rock obstacle)
                    if (!_botUsedCounterSkillThisMatch && bossEquip == BotEquipmentType.ThornArmor)
                    {
                        if (_engine.TryExecuteBotThornArmor(out Vector2Int thornCell))
                        {
                            _botUsedCounterSkillThisMatch = true;
                            PlaySfx("shield");
                            RefreshAllCells(animateLastMove: false);
                            SpawnTacticalCalloutBanner($"{blind.bossName.ToUpper()} RELIC!", "Thorn Armor blocked an attack corridor!", XOProceduralAssets.ColMuted);
                            yield return new WaitForSeconds(0.45f);
                        }
                    }
                }

                Vector2Int aiMove = _engine.CalculateAIMove(CellPiece.O, aiDiff, activeBot);
                ExecuteMove(aiMove.x, aiMove.y);

                // Boss Relic: Thunder Hammer / Omni Crown Double Move
                if (currentGameMode == GameMode.TowerClimb && blind != null && _engine.State == MatchState.Playing)
                {
                    if ((blind.botEquipment == BotEquipmentType.ThunderHammer || blind.botEquipment == BotEquipmentType.OmniCrown) && _engine.MoveHistory.Count % 8 == 0)
                    {
                        PlaySfx("lightning");
                        SpawnTacticalCalloutBanner("THUNDER HAMMER!", $"{blind.bossName} triggers a lightning DOUBLE MOVE!", XOProceduralAssets.ColSun);
                        yield return new WaitForSeconds(0.35f);
                        Vector2Int extraMove = _engine.CalculateAIMove(CellPiece.O, aiDiff, activeBot);
                        ExecuteMove(extraMove.x, extraMove.y);
                    }
                }
            }

            _isAIThinking = false;
            if (_engine.State == MatchState.Playing)
            {
                RefreshTurnAndScoreUI();
            }
        }

        private void PromptOutOfPuzzleMovesRevive()
        {
            PlaySfx("lose");
            _isOutOfMovesPuzzleRevive = true;
            _reviveTitleText.text = "OUT OF MOVES!";
            _reviveDescText.text = $"You ran out of moves on {_currentPuzzle.title}!\nWatch a short ad to get +3 EXTRA MOVES and a winning Hint!";
            _reviveActionBtnText.text = "WATCH AD: +3 MOVES & HINT!";

            _reviveModalRoot.transform.SetAsLastSibling();
            _reviveModalRoot.SetActive(true);
        }

        private void HandleMatchEnded(MatchState endState)
        {
            DrawWinStrikeLineIfApplicable();

            if (endState == MatchState.XWon)
            {
                _scoreX++;
                PlaySfx("win");
                if (enableHaptics) XOPlayableAdsBridge.Instance?.TriggerHaptic(50);
                SpawnVictoryConfettiBurst();
                _statusBannerText.text = $"AWESOME! Player X connected {_engine.WinLength} in a row!";
                StartCoroutine(ShowModalAfterDelay(0.65f, () => ProcessPlayerVictoryConsequences()));
            }
            else if (endState == MatchState.OWon)
            {
                if (enableHaptics) XOPlayableAdsBridge.Instance?.TriggerHaptic(40);

                // ROGUELIKE PHOENIX SIGIL INTERCEPT: Divine fire incinerates winning piece once per match!
                if (currentGameMode == GameMode.TowerClimb
                    && XORoguelikeRunManager.HasRelic(CaroRelicType.VampireFang)
                    && !XORoguelikeRunManager.PhoenixSigilTriggeredThisMatch)
                {
                    XORoguelikeRunManager.PhoenixSigilTriggeredThisMatch = true;
                    if (_engine.IncinerateLastWinningPiece(out Vector2Int burntTile, freezeTurns: 2))
                    {
                        PlaySfx("powerup");
                        if (enableHaptics) XOPlayableAdsBridge.Instance?.TriggerHaptic(75);
                        SpawnCellVFXBurst(burntTile.x, burntTile.y, XOProceduralAssets.ColCoral, XOProceduralAssets.ColSun, 24);
                        SpawnTacticalCalloutBanner("PHOENIX SIGIL!", "Holy flames incinerated the Bot's winning piece and froze the tile for 2 turns! Run saved!", XOProceduralAssets.ColSun);
                        RefreshAllCells(animateLastMove: false);
                        RefreshTurnAndScoreUI();
                        RefreshBattleRelicBarUI();
                        return;
                    }
                }

                if (currentGameMode != GameMode.TowerClimb && IsVsAIMode && !_reviveUsedThisMatch)
                {
                    PlaySfx("lose");
                    _isOutOfMovesPuzzleRevive = false;
                    _reviveTitleText.text = "SECOND CHANCE!";
                    string opponentName = (currentGameMode == GameMode.TowerClimb) ? "Boss" : "AI";
                    _reviveDescText.text = $"{opponentName} just connected {_engine.WinLength} in a row!\nWatch a short ad to UNDO its winning move\nand highlight the exact square to block!";
                    _reviveActionBtnText.text = "WATCH AD: REVIVE & BLOCK NOW!";

                    _statusBannerText.text = $"{opponentName} connected {_engine.WinLength}! Use Second Chance to block it now!";
                    StartCoroutine(ShowModalAfterDelay(0.6f, () =>
                    {
                        _reviveModalRoot.transform.SetAsLastSibling();
                        _reviveModalRoot.SetActive(true);
                    }));
                }
                else
                {
                    _scoreO++;
                    PlaySfx(currentGameMode == GameMode.TwoPlayerLocal ? "win" : "lose");
                    StartCoroutine(ShowModalAfterDelay(0.65f, () => ProcessPlayerDefeatConsequences()));
                }
            }
            else if (endState == MatchState.Draw)
            {
                PlaySfx("undo");
                _statusBannerText.text = "DRAW GAME! Well played by both sides!";
                StartCoroutine(ShowModalAfterDelay(0.55f, () => ShowMatchResultModal(MatchState.Draw, "")));
            }

            RefreshScoreTextsOnly();
        }

        private void ProcessPlayerVictoryConsequences()
        {
            XOProgressionSystem.TotalMatchesWon++;
            XOProgressionSystem.WinStreak++;
            int streak = XOProgressionSystem.WinStreak;
            int starsEarned = (streak >= 3) ? 2 : 1;
            XOProgressionSystem.TotalStars += starsEarned;

            XOProgressionSystem.ChestProgress = Mathf.Min(3, XOProgressionSystem.ChestProgress + 1);

            string extraNotes = $"+{starsEarned} Rank Star(s)   |   Win Streak: {streak}";

            if (currentGameMode == GameMode.VsAI)
            {
                XOBotProfile bot = XOProgressionSystem.GetActiveBot();
                if (XOProgressionSystem.MarkBotConquered(XOProgressionSystem.SelectedBotIndex))
                {
                    Coins += 80;
                    extraNotes += $"\nFirst-Time Victory vs {bot.botName}: +80 Bounty Coins! ({XOProgressionSystem.GetConqueredBotsCount()}/6 Rivals Conquered)";
                }
            }
            else if (currentGameMode == GameMode.TowerClimb)
            {
                TowerFloorBlind blind = XORoguelikeRunManager.GetActiveBlind();
                XOProgressionSystem.MarkTowerFloorCleared(blind.floorNumber);
                extraNotes += $"\nFloor #{blind.floorNumber} ({blind.blindName}) Conquered!";
                if (XORoguelikeRunManager.HasRelic(CaroRelicType.CrownOfOmni))
                {
                    Coins += 25;
                    extraNotes += "\nCrown of Omni: +25 Divine Coins!";
                }
            }
            else if (currentGameMode == GameMode.PuzzleStage)
            {
                int clearedStage = XOProgressionSystem.CurrentPuzzleStage;
                XOProgressionSystem.CurrentPuzzleStage++;
                extraNotes += $"\nStage #{clearedStage} Cleared! Unlocked Stage #{XOProgressionSystem.CurrentPuzzleStage}!";
            }

            bool isDiag = _engine.IsWinningLineDiagonal();
            bool isHardOrPuzzle = (currentGameMode == GameMode.PuzzleStage || currentDifficulty == AIDifficulty.Hard);
            if (XOProgressionSystem.CheckAndAdvanceMission(
                _engine.GetPlayerMadeMovesCount(),
                isDiag,
                _usedPowerUpThisMatch,
                streak,
                isHardOrPuzzle,
                out XOMissionData doneMission))
            {
                Coins += doneMission.rewardCoins;
                extraNotes += $"\nQuest Complete '{doneMission.description}': +{doneMission.rewardCoins} Coins!";
            }

            RefreshHeaderAndProgressionUI();
            RefreshSelectorButtonsUI();
            XOPlayableAdsBridge.Instance?.ReportPlayableScore(XOProgressionSystem.TotalStars * 100 + Coins);
            ShowMatchResultModal(MatchState.XWon, extraNotes);
        }

        private void ProcessPlayerDefeatConsequences()
        {
            if (currentGameMode == GameMode.TowerClimb)
            {
                TowerFloorBlind blind = XORoguelikeRunManager.GetActiveBlind();
                bool runOver = XORoguelikeRunManager.LoseHeart();
                string towerNote = runOver
                    ? $"Run Ended (0 Hearts)! Defeated on Floor {blind.floorNumber} by {blind.bossName}.\nSeed: {XORoguelikeRunManager.FormattedSeed} • Relics Lost: {XORoguelikeRunManager.EquippedRelics.Count} • Run Coins: +{XORoguelikeRunManager.RunTotalCoinsEarned}\nRelics & Floor progression reset for next run."
                    : $"Lost 1 Heart! ({XORoguelikeRunManager.Lives}/{XORoguelikeRunManager.MaxLives} remaining)\nTap Rematch to fight {blind.bossName} again!";
                ShowMatchResultModal(MatchState.OWon, towerNote);
                return;
            }

            _lostStreakToRestore = XOProgressionSystem.WinStreak;
            if (XOProgressionSystem.TotalStars > 0)
            {
                XOProgressionSystem.TotalStars--;
            }
            XOProgressionSystem.WinStreak = 0;

            RefreshHeaderAndProgressionUI();
            string note = (_lostStreakToRestore > 0)
                ? $"Lost your {_lostStreakToRestore}-Win Streak & -1 Rank Star!\n(Use Rank Shield below to restore them!)"
                : "-1 Rank Star. Use Rank Shield below to protect your star!";

            ShowMatchResultModal(MatchState.OWon, note);
        }

        private IEnumerator ShowModalAfterDelay(float delay, System.Action action)
        {
            yield return new WaitForSeconds(delay);
            action?.Invoke();
        }

        // =====================================================================
        // SKILLS & REWARDED AD ACTIONS: UNDO, HINT, BOMB, FREEZE, CHEST
        // =====================================================================

        private void OnUndoButtonClicked()
        {
            if (_isAIThinking) return;
            _skillTargeting = ActiveSkillTargeting.None;

            if (!_engine.HasUndoablePlayerMove())
            {
                _statusBannerText.text = "No moves to Undo yet!";
                PlaySfx("click");
                return;
            }

            if (UndoStock > 0)
            {
                UndoStock--;
                PerformUndoAction();
            }
            else
            {
                PlaySfx("click");
                XOPlayableAdsBridge.Instance.ShowRewardedAd(
                    "Get +2 Undo Moves & Undo Immediately (+25 Coins)",
                    onRewarded: () =>
                    {
                        PlaySfx("reward");
                        Coins += 25;
                        UndoStock += 1;
                        PerformUndoAction();
                    });
            }
        }

        private void PerformUndoAction()
        {
            if (_engine.UndoMove(currentGameMode, CellPiece.X))
            {
                if (currentGameMode == GameMode.PuzzleStage && _currentPuzzle != null)
                {
                    _puzzleMovesLeft = Mathf.Min(_currentPuzzle.maxPlayerMoves, _puzzleMovesLeft + 1);
                }

                PlaySfx("undo");
                _activeHintCell = new Vector2Int(-1, -1);
                _turnTimerRemaining = turnTimeLimit;
                if (_winStrikeLineImg != null)
                    _winStrikeLineImg.gameObject.SetActive(false);

                RefreshAllCells(animateLastMove: false);
                RefreshTurnAndScoreUI();
                _statusBannerText.text = "Move Undone! Your turn to play.";
            }
        }

        private void OnHintButtonClicked()
        {
            if (_isAIThinking || _engine.State != MatchState.Playing)
                return;

            _skillTargeting = ActiveSkillTargeting.None;

            if (HintStock > 0)
            {
                HintStock--;
                ActivateTacticalHint();
            }
            else
            {
                PlaySfx("click");
                XOPlayableAdsBridge.Instance.ShowRewardedAd(
                    "Get +2 AI Hints & Reveal Best Move (+25 Coins)",
                    onRewarded: () =>
                    {
                        PlaySfx("reward");
                        Coins += 25;
                        HintStock += 1;
                        ActivateTacticalHint();
                    });
            }
        }

        private void ActivateTacticalHint()
        {
            CellPiece targetPiece = _engine.CurrentTurn;
            Vector2Int bestCell = _engine.CalculateBestHint(targetPiece, out string reason);
            _activeHintCell = bestCell;

            PlaySfx("hint");
            RefreshAllCells(animateLastMove: false);
            _statusBannerText.text = $"AI HINT: {reason} (Flashing Gold Target)";
        }

        private void OnBombSkillClicked()
        {
            if (_isAIThinking || _engine.State != MatchState.Playing)
                return;

            if (_skillTargeting == ActiveSkillTargeting.BombDestroy)
            {
                _skillTargeting = ActiveSkillTargeting.None;
                RefreshAllCells(animateLastMove: false);
                RefreshTurnAndScoreUI();
                return;
            }

            if (BombStock > 0)
            {
                EnterBombTargetingMode();
            }
            else
            {
                PlaySfx("click");
                XOPlayableAdsBridge.Instance.ShowRewardedAd(
                    "Get +2 Bombs (Destroy any Opponent O piece on the board)",
                    onRewarded: () =>
                    {
                        PlaySfx("reward");
                        BombStock += 2;
                        EnterBombTargetingMode();
                    });
            }
        }

        private void EnterBombTargetingMode()
        {
            bool hasOpponentPiece = false;
            for (int r = 0; r < _engine.BoardSize; r++)
                for (int c = 0; c < _engine.BoardSize; c++)
                    if (_engine.Board[r, c] == CellPiece.O) hasOpponentPiece = true;

            if (!hasOpponentPiece)
            {
                _statusBannerText.text = "No Opponent (O) pieces on the board to destroy yet!";
                PlaySfx("click");
                return;
            }

            _skillTargeting = ActiveSkillTargeting.BombDestroy;
            PlaySfx("click");
            RefreshAllCells(animateLastMove: false);
            _statusBannerText.text = "BOMB MODE: Tap any pulsing red Opponent (O) piece to DESTROY it!";
        }

        private void OnFreezeSkillClicked()
        {
            if (_isAIThinking || _engine.State != MatchState.Playing)
                return;

            if (_skillTargeting == ActiveSkillTargeting.FreezeTile)
            {
                _skillTargeting = ActiveSkillTargeting.None;
                RefreshAllCells(animateLastMove: false);
                RefreshTurnAndScoreUI();
                return;
            }

            if (FreezeStock > 0)
            {
                _skillTargeting = ActiveSkillTargeting.FreezeTile;
                PlaySfx("click");
                _statusBannerText.text = "FREEZE MODE: Tap any empty square to LOCK it for 3 turns!";
            }
            else
            {
                PlaySfx("click");
                XOPlayableAdsBridge.Instance.ShowRewardedAd(
                    "Get +2 Freeze Skills (Lock empty squares to block AI)",
                    onRewarded: () =>
                    {
                        PlaySfx("reward");
                        FreezeStock += 2;
                        _skillTargeting = ActiveSkillTargeting.FreezeTile;
                        _statusBannerText.text = "FREEZE MODE: Tap any empty square to LOCK it for 3 turns!";
                    });
            }
        }

        private void OnSwapSkillClicked()
        {
            if (_isAIThinking || _engine.State != MatchState.Playing)
                return;

            if (_skillTargeting == ActiveSkillTargeting.SwapPiece)
            {
                _skillTargeting = ActiveSkillTargeting.None;
                RefreshAllCells(animateLastMove: false);
                RefreshTurnAndScoreUI();
                return;
            }

            if (SwapStock > 0)
            {
                EnterSwapTargetingMode();
            }
            else
            {
                PlaySfx("click");
                XOPlayableAdsBridge.Instance.ShowRewardedAd(
                    "Get +1 Swap Piece Skill (Convert any Opponent O into your X!)",
                    onRewarded: () =>
                    {
                        PlaySfx("reward");
                        SwapStock += 1;
                        EnterSwapTargetingMode();
                    });
            }
        }

        private void EnterSwapTargetingMode()
        {
            bool hasOpponentPiece = false;
            for (int r = 0; r < _engine.BoardSize; r++)
                for (int c = 0; c < _engine.BoardSize; c++)
                    if (_engine.Board[r, c] == CellPiece.O) hasOpponentPiece = true;

            if (!hasOpponentPiece)
            {
                _statusBannerText.text = "No Opponent (O) pieces on the board to swap yet!";
                PlaySfx("click");
                return;
            }

            _skillTargeting = ActiveSkillTargeting.SwapPiece;
            PlaySfx("click");
            RefreshAllCells(animateLastMove: false);
            _statusBannerText.text = "SWAP MODE: Tap any pulsing purple Opponent (O) piece to convert it into YOUR X!";
        }

        private void OnChestButtonClicked()
        {
            if (XOProgressionSystem.ChestProgress >= 3)
            {
                XOProgressionSystem.ChestProgress = 0;
                Coins += 100;
                BombStock += 1;
                FreezeStock += 1;
                SwapStock += 1;
                UndoStock += 1;
                HintStock += 1;
                PlaySfx("chest");
                RefreshHeaderAndProgressionUI();
                RefreshBottomActionBadges();
                _statusBannerText.text = "GOLD CHEST OPENED: +100 Coins, +1 Swap, +1 Bomb, +1 Freeze, +1 Undo & +1 Hint!";
            }
            else
            {
                PlaySfx("click");
                XOPlayableAdsBridge.Instance.ShowRewardedAd(
                    "Unlock Gold Chest Early (+100 Coins, +1 Swap, +1 Bomb, +1 Freeze)",
                    onRewarded: () =>
                    {
                        XOProgressionSystem.ChestProgress = 0;
                        Coins += 100;
                        BombStock += 1;
                        FreezeStock += 1;
                        SwapStock += 1;
                        UndoStock += 1;
                        PlaySfx("chest");
                        RefreshHeaderAndProgressionUI();
                        RefreshBottomActionBadges();
                        _statusBannerText.text = "CHEST UNLOCKED: +100 Coins, +1 Swap, +1 Bomb, +1 Freeze, +1 Undo!";
                    });
            }
        }

        private void OnConfirmReviveWithAd()
        {
            PlaySfx("click");

            if (_isOutOfMovesPuzzleRevive)
            {
                XOPlayableAdsBridge.Instance.ShowRewardedAd(
                    "Get +3 Extra Moves & Reveal Optimal Hint!",
                    onRewarded: () =>
                    {
                        _reviveModalRoot.SetActive(false);
                        _isOutOfMovesPuzzleRevive = false;
                        _puzzleMovesLeft += 3;
                        PlaySfx("reward");
                        ActivateTacticalHint();
                        RefreshTurnAndScoreUI();
                    });
                return;
            }

            XOPlayableAdsBridge.Instance.ShowRewardedAd(
                "Second Chance: Undo AI's Winning Move & Show Block!",
                onRewarded: () =>
                {
                    _reviveModalRoot.SetActive(false);
                    _reviveUsedThisMatch = true;
                    Coins += 25;

                    if (_engine.ReviveFromAILoss(out Vector2Int criticalCell))
                    {
                        PlaySfx("reward");
                        _activeHintCell = criticalCell;
                        if (_winStrikeLineImg != null)
                            _winStrikeLineImg.gameObject.SetActive(false);

                        RefreshAllCells(animateLastMove: false);
                        RefreshTurnAndScoreUI();
                        _statusBannerText.text = "REVIVED! Place on the flashing gold square now to block AI!";
                    }
                });
        }

        private void ShowMatchResultModal(MatchState state, string extraNotes)
        {
            _claimedDoubleReward = false;

            if (state == MatchState.XWon)
            {
                _lastMatchRewardCoins = (currentGameMode == GameMode.PuzzleStage)
                    ? (_currentPuzzle != null ? _currentPuzzle.rewardCoins : 40)
                    : (currentGameMode == GameMode.TowerClimb)
                    ? XORoguelikeRunManager.GetActiveBlind().coinReward
                    : ((currentBoardSize == 15) ? 20 : 15);

                Coins += _lastMatchRewardCoins;
                if (_resultHeroIconImg != null) _resultHeroIconImg.sprite = XOProceduralAssets.IconTrophy;

                if (currentGameMode == GameMode.TowerClimb)
                {
                    TowerFloorBlind blind = XORoguelikeRunManager.GetActiveBlind();
                    bool firstClear = XOProgressionSystem.MarkTowerFloorCleared(blind.floorNumber);
                    XORoguelikeRunManager.RunTotalCoinsEarned += _lastMatchRewardCoins;

                    if (blind.floorNumber == 10)
                    {
                        XOThemeDatabase.UnlockPieceSkin("king_dragon");
                    }

                    _resultTitleText.text = $"VƯỢT TẦNG {blind.floorNumber} THÀNH CÔNG!";
                    _resultSubText.text = $"Đã hạ gục: {blind.bossName}\nPhần thưởng: +{_lastMatchRewardCoins} Coins!\n(Trang bị thêm Cổ Vật tại Trại Tiếp Tế)";

                    if (_resultPlayAgainBtnText != null)
                    {
                        _resultPlayAgainBtnText.text = (blind.floorNumber < 10) ? $"TIẾP TỤC TẦNG {blind.floorNumber + 1}" : "VỀ SẢNH CHÍNH";
                    }
                    if (_resultPlayAgainBtn != null)
                    {
                        _resultPlayAgainBtn.GetComponent<Image>().color = XOProceduralAssets.ColMint;
                    }
                }
                else
                {
                    _resultTitleText.text = (currentGameMode == GameMode.PuzzleStage) ? "VƯỢT ẢI THÀNH CÔNG!" : "CHIẾN THẮNG!";
                    _resultTitleText.color = XOThemeDatabase.Hex("#FBBF24");
                    _resultSubText.text = $"Phần thưởng trận đấu: +{_lastMatchRewardCoins} Coins\n{extraNotes}";

                    if (_resultPlayAgainBtnText != null)
                    {
                        _resultPlayAgainBtnText.text = "CHƠI TIẾP TRẬN MỚI";
                    }
                    if (_resultPlayAgainBtn != null)
                    {
                        _resultPlayAgainBtn.GetComponent<Image>().color = XOProceduralAssets.ColMint;
                    }
                }
                _resultTitleText.color = XOThemeDatabase.Hex("#FBBF24");
                _shieldAdBtn.gameObject.SetActive(false);
            }
            else if (state == MatchState.OWon)
            {
                _lastMatchRewardCoins = 15;
                Coins += _lastMatchRewardCoins;
                if (_resultHeroIconImg != null) _resultHeroIconImg.sprite = XOProceduralAssets.IconSwords;

                if (currentGameMode == GameMode.TowerClimb)
                {
                    TowerFloorBlind blind = XORoguelikeRunManager.GetActiveBlind();
                    _resultTitleText.text = "KẾT THÚC LƯỢT CHƠI (0 TIM)";
                    _resultTitleText.color = XOProceduralAssets.ColCoral;
                    _resultSubText.text = $"Thất bại trước: {blind.bossName}\nLượt chơi đã kết thúc, vui lòng bắt đầu lại từ Tầng 1.";

                    _shieldAdBtn.gameObject.SetActive(false); // 1-life sudden death: no revives
                    if (_resultPlayAgainBtnText != null)
                    {
                        _resultPlayAgainBtnText.text = "BẮT ĐẦU LẠI TỪ TẦNG 1";
                    }
                    if (_resultPlayAgainBtn != null)
                    {
                        _resultPlayAgainBtn.GetComponent<Image>().color = XOProceduralAssets.ColCoral;
                    }
                }
                else
                {
                    _resultTitleText.text = (currentGameMode == GameMode.TwoPlayerLocal) ? "NGƯỜI CHƠI O THẮNG!" : "CHƯA MAY MẮN! CỐ GẮNG LẦN SAU";
                    _resultTitleText.color = XOThemeDatabase.Hex("#38BDF8");
                    _resultSubText.text = $"Thưởng an ủi: +{_lastMatchRewardCoins} Coins\n{extraNotes}";

                    if (_resultPlayAgainBtnText != null)
                    {
                        _resultPlayAgainBtnText.text = "CHƠI LẠI TRẬN NÀY";
                    }
                    if (_resultPlayAgainBtn != null)
                    {
                        _resultPlayAgainBtn.GetComponent<Image>().color = XOProceduralAssets.ColMint;
                    }

                    bool showShield = (currentGameMode != GameMode.TwoPlayerLocal);
                    _shieldAdBtn.gameObject.SetActive(showShield);
                    if (showShield)
                    {
                        _shieldAdBtn.interactable = true;
                        _shieldAdBtn.GetComponent<Image>().color = XOThemeDatabase.Hex("#8B5CF6");
                        _shieldAdBtnText.text = (_lostStreakToRestore > 0)
                            ? $"XEM QC: BẢO VỆ CHUỖI {_lostStreakToRestore} THẮNG & +1 SAO"
                            : "XEM QC: KHIÊN XẾP HẠNG (+1 SAO)";
                    }
                }
            }
            else
            {
                _lastMatchRewardCoins = 25;
                Coins += _lastMatchRewardCoins;
                if (_resultHeroIconImg != null) _resultHeroIconImg.sprite = XOProceduralAssets.IconShield;
                _resultTitleText.text = "HÒA TRẬN!";
                _resultTitleText.color = XOThemeDatabase.Hex("#A855F7");
                _resultSubText.text = (currentGameMode == GameMode.TowerClimb)
                    ? $"Hòa cờ với {XORoguelikeRunManager.GetActiveBlind().bossName}!\nKhông mất tim. Chơi lại tầng này để vượt tháp!\nThưởng: +{_lastMatchRewardCoins} Coins"
                    : $"Trận đấu cân tài cân sức!\nThưởng: +{_lastMatchRewardCoins} Coins";
                _shieldAdBtn.gameObject.SetActive(false);

                if (_resultPlayAgainBtnText != null)
                {
                    _resultPlayAgainBtnText.text = (currentGameMode == GameMode.TowerClimb)
                        ? $"TÁI ĐẤU VỚI {XORoguelikeRunManager.GetActiveBlind().bossName.ToUpper()}"
                        : "CHƠI LẠI";
                }
                if (_resultPlayAgainBtn != null)
                {
                    _resultPlayAgainBtn.GetComponent<Image>().color = XOProceduralAssets.ColMint;
                }
            }

            _doubleRewardAdBtn.interactable = true;
            _doubleRewardAdBtn.GetComponent<Image>().color = XOThemeDatabase.Hex("#F59E0B");
            _doubleRewardAdBtnText.text = $"XEM QC: NHẬN THƯỞNG X2 (+{_lastMatchRewardCoins} COINS)";

            if (_resultThirdActionBtn != null)
            {
                _resultThirdActionBtn.gameObject.SetActive(false);
            }

            _resultModalRoot.transform.SetAsLastSibling();
            _resultModalRoot.SetActive(true);
        }

        private void OnWatchAdActivateRankShield()
        {
            PlaySfx("click");

            if (currentGameMode == GameMode.TowerClimb)
            {
                XOPlayableAdsBridge.Instance.ShowRewardedAd(
                    "Restore +1 Heart (Keep your Roguelike Run alive!)",
                    onRewarded: () =>
                    {
                        PlaySfx("reward");
                        XORoguelikeRunManager.RestoreHeart();
                        _shieldAdBtn.interactable = false;
                        _shieldAdBtn.GetComponent<Image>().color = XOProceduralAssets.ColMuted;
                        _shieldAdBtnText.text = $"HEART RESTORED! ({XORoguelikeRunManager.Lives}/{XORoguelikeRunManager.MaxLives} HEARTS)";
                    });
                return;
            }

            XOPlayableAdsBridge.Instance.ShowRewardedAd(
                "Activate Rank Shield: Restore Win Streak & Rank Star!",
                onRewarded: () =>
                {
                    PlaySfx("reward");
                    XOProgressionSystem.TotalStars += 1;
                    if (_lostStreakToRestore > 0)
                    {
                        XOProgressionSystem.WinStreak = _lostStreakToRestore;
                    }

                    RefreshHeaderAndProgressionUI();
                    _shieldAdBtn.interactable = false;
                    _shieldAdBtn.GetComponent<Image>().color = XOThemeDatabase.Hex("#475569");
                    _shieldAdBtnText.text = "STREAK & RANK STAR RESTORED!";
                });
        }

        private void OnWatchAdDoubleMatchReward()
        {
            if (_claimedDoubleReward) return;
            PlaySfx("click");

            XOPlayableAdsBridge.Instance.ShowRewardedAd(
                $"Double Match Reward (+{_lastMatchRewardCoins} Coins)",
                onRewarded: () =>
                {
                    _claimedDoubleReward = true;
                    Coins += _lastMatchRewardCoins;
                    PlaySfx("reward");

                    _doubleRewardAdBtn.interactable = false;
                    _doubleRewardAdBtn.GetComponent<Image>().color = XOThemeDatabase.Hex("#475569");
                    _doubleRewardAdBtnText.text = "2X REWARD CLAIMED!";
                });
        }

        private void OnWatchAdForFreeCoins()
        {
            PlaySfx("click");
            XOPlayableAdsBridge.Instance.ShowRewardedAd(
                "Claim +100 Free Coins & +1 Bomb!",
                onRewarded: () =>
                {
                    PlaySfx("reward");
                    Coins += 100;
                    BombStock += 1;
                    _statusBannerText.text = "Claimed +100 Free Coins and +1 Bomb!";
                });
        }

        // =====================================================================
        // 2-TAB SKIN & THEME SHOP (BOARD THEMES + X/O PIECE SKINS)
        // =====================================================================

        private void OpenThemeShopModal()
        {
            PlaySfx("click");
            RefreshThemeShopUI();
            _themeModalRoot.transform.SetAsLastSibling();
            _themeModalRoot.SetActive(true);
        }

        private void SwitchShopTab(int tabIdx)
        {
            PlaySfx("click");
            _activeShopTab = tabIdx;
            if (_themesScrollViewObj != null) _themesScrollViewObj.SetActive(tabIdx == 0);
            if (_piecesScrollViewObj != null) _piecesScrollViewObj.SetActive(tabIdx == 1);
            if (_skillsScrollViewObj != null) _skillsScrollViewObj.SetActive(tabIdx == 2);

            Color activeCol = XOProceduralAssets.ColSky;
            Color inactiveCol = XOProceduralAssets.ColTileEdge;
            Color activeTxt = Color.white;
            Color inactiveTxt = XOProceduralAssets.ColInk;

            if (_tabThemesBtn != null)
            {
                _tabThemesBtn.GetComponent<Image>().color = (tabIdx == 0) ? activeCol : inactiveCol;
                var t = _tabThemesBtn.GetComponentInChildren<Text>(); if (t != null) t.color = (tabIdx == 0) ? activeTxt : inactiveTxt;
            }
            if (_tabPiecesBtn != null)
            {
                _tabPiecesBtn.GetComponent<Image>().color = (tabIdx == 1) ? activeCol : inactiveCol;
                var t = _tabPiecesBtn.GetComponentInChildren<Text>(); if (t != null) t.color = (tabIdx == 1) ? activeTxt : inactiveTxt;
            }
            if (_tabSkillsBtn != null)
            {
                _tabSkillsBtn.GetComponent<Image>().color = (tabIdx == 2) ? activeCol : inactiveCol;
                var t = _tabSkillsBtn.GetComponentInChildren<Text>(); if (t != null) t.color = (tabIdx == 2) ? activeTxt : inactiveTxt;
            }
            RefreshThemeShopUI();
        }

        private void CreateSkillBuyRow(RectTransform parent, Sprite icon, string title, string subtitle, int cost, System.Func<int> getStock, System.Action onBuy)
        {
            GameObject itemObj = CreateUIObject("SkillBuy_" + title, parent);
            LayoutElement le = itemObj.AddComponent<LayoutElement>();
            le.minHeight = 100f;
            le.preferredHeight = 100f;
            le.flexibleHeight = 0f;

            Image bg = itemObj.AddComponent<Image>();
            bg.sprite = XOProceduralAssets.CardPanelSprite;
            bg.type = Image.Type.Sliced;
            bg.color = Color.white;

            RectTransform itemRT = itemObj.GetComponent<RectTransform>();

            // Icon inside pastel recessed box
            GameObject iconBox = CreateUIObject("IconBox", itemRT);
            RectTransform ibRT = iconBox.GetComponent<RectTransform>();
            ibRT.anchorMin = new Vector2(0f, 0.5f);
            ibRT.anchorMax = new Vector2(0f, 0.5f);
            ibRT.pivot = new Vector2(0f, 0.5f);
            ibRT.anchoredPosition = new Vector2(16f, 0f);
            ibRT.sizeDelta = new Vector2(68f, 68f);

            Image ibBg = iconBox.AddComponent<Image>();
            ibBg.sprite = XOProceduralAssets.PillSprite;
            ibBg.type = Image.Type.Sliced;
            ibBg.color = XOProceduralAssets.ColTileEdge;

            CreateIconImage(ibRT, icon, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(46f, 46f));

            Text nameTxt = CreateText(itemRT, title, 20, FontStyle.Bold, TextAnchor.UpperLeft);
            nameTxt.rectTransform.offsetMin = new Vector2(98f, 0f);
            nameTxt.rectTransform.offsetMax = new Vector2(-210f, -14f);
            nameTxt.color = XOProceduralAssets.ColInk;

            Text subTxt = CreateText(itemRT, subtitle, 14, FontStyle.Normal, TextAnchor.MiddleLeft);
            subTxt.rectTransform.offsetMin = new Vector2(98f, 0f);
            subTxt.rectTransform.offsetMax = new Vector2(-210f, 0f);
            subTxt.color = XOProceduralAssets.ColMuted;

            Text stockTxt = CreateText(itemRT, $"Owned: {getStock()}", 14, FontStyle.Bold, TextAnchor.LowerLeft);
            stockTxt.rectTransform.offsetMin = new Vector2(98f, 14f);
            stockTxt.rectTransform.offsetMax = new Vector2(-210f, 0f);
            stockTxt.color = XOThemeDatabase.Hex("#0284C7");

            Button buyBtn = CreateIcon3DButton(itemRT, XOProceduralAssets.IconCoin, $"Buy ({cost}c)", new Vector2(180f, 48f), XOProceduralAssets.ColMint, XOProceduralAssets.ColInk, 16);
            RectTransform bRT = buyBtn.GetComponent<RectTransform>();
            bRT.anchorMin = new Vector2(1f, 0.5f);
            bRT.anchorMax = new Vector2(1f, 0.5f);
            bRT.pivot = new Vector2(1f, 0.5f);
            bRT.anchoredPosition = new Vector2(-16f, 0f);
            buyBtn.onClick.AddListener(() =>
            {
                if (Coins >= cost)
                {
                    Coins -= cost;
                    onBuy();
                    stockTxt.text = $"Owned: {getStock()}";
                    RefreshHeaderAndProgressionUI();
                    RefreshBottomActionBadges();
                    PlaySfx("reward");
                }
                else
                {
                    PlaySfx("lose");
                    SpawnTacticalCalloutBanner("NOT ENOUGH COINS!", $"You need {cost} coins to buy {title}.", XOProceduralAssets.ColCoral);
                }
            });
        }

        private void RefreshThemeShopUI()
        {
            int selectedThemeIdx = XOThemeDatabase.GetSelectedThemeIndex();
            var themes = XOThemeDatabase.Themes;

            foreach (var card in _themeCards)
            {
                XOThemePalette t = themes[card.index];
                bool unlocked = XOThemeDatabase.IsThemeUnlocked(t.id);
                bool isSelected = (card.index == selectedThemeIdx);
                UpdateShopCardVisual(card, unlocked, isSelected, t.coinCost);
            }

            int selectedPieceIdx = XOThemeDatabase.GetSelectedPieceSkinIndex();
            var pieceSkins = XOThemeDatabase.PieceSkins;

            foreach (var card in _pieceSkinCards)
            {
                XOPieceSkin ps = pieceSkins[card.index];
                bool unlocked = XOThemeDatabase.IsPieceSkinUnlocked(ps.id);
                bool isSelected = (card.index == selectedPieceIdx);
                UpdateShopCardVisual(card, unlocked, isSelected, ps.coinCost);
            }
        }

        private void UpdateShopCardVisual(ShopCardRefs card, bool unlocked, bool isSelected, int cost)
        {
            card.cardBg.color = isSelected ? XOThemeDatabase.Hex("#ECFDF5") : Color.white;

            if (unlocked)
            {
                card.statusText.text = isSelected ? "STATUS: EQUIPPED" : "STATUS: UNLOCKED";
                card.statusText.color = isSelected ? XOThemeDatabase.Hex("#059669") : XOThemeDatabase.Hex("#0284C7");

                card.selectOrCoinBtnText.text = isSelected ? "EQUIPPED" : "EQUIP";
                card.selectOrCoinBtn.GetComponent<Image>().color = isSelected ? XOProceduralAssets.ColMint : XOProceduralAssets.ColSky;
                var txt = card.selectOrCoinBtn.GetComponentInChildren<Text>();
                if (txt != null) txt.color = isSelected ? XOProceduralAssets.ColInk : Color.white;
                card.watchAdUnlockBtn.gameObject.SetActive(false);
                card.selectOrCoinBtn.GetComponent<RectTransform>().anchoredPosition = new Vector2(-16f, 0f);
            }
            else
            {
                card.statusText.text = $"LOCKED — {cost} Coins";
                card.statusText.color = XOThemeDatabase.Hex("#D97706");

                card.selectOrCoinBtnText.text = $"BUY ({cost}c)";
                bool canAfford = (Coins >= cost);
                card.selectOrCoinBtn.GetComponent<Image>().color = canAfford ? XOProceduralAssets.ColMint : XOProceduralAssets.ColTileEdge;
                var txt = card.selectOrCoinBtn.GetComponentInChildren<Text>();
                if (txt != null) txt.color = canAfford ? XOProceduralAssets.ColInk : XOProceduralAssets.ColMuted;
                card.watchAdUnlockBtn.gameObject.SetActive(true);
                card.selectOrCoinBtn.GetComponent<RectTransform>().anchoredPosition = new Vector2(-16f, 24f);
            }
        }

        private void OnThemePrimaryActionClicked(int themeIdx)
        {
            XOThemePalette t = XOThemeDatabase.Themes[themeIdx];
            if (XOThemeDatabase.IsThemeUnlocked(t.id))
            {
                PlaySfx("click");
                SelectAndApplyTheme(themeIdx);
            }
            else if (Coins >= t.coinCost)
            {
                Coins -= t.coinCost;
                XOThemeDatabase.UnlockTheme(t.id);
                PlaySfx("reward");
                SelectAndApplyTheme(themeIdx);
            }
            else
            {
                OnThemeAdUnlockClicked(themeIdx);
            }
        }

        private void OnThemeAdUnlockClicked(int themeIdx)
        {
            XOThemePalette t = XOThemeDatabase.Themes[themeIdx];
            PlaySfx("click");
            XOPlayableAdsBridge.Instance.ShowRewardedAd(
                $"Unlock '{t.displayName}' Board Theme Permanently",
                onRewarded: () =>
                {
                    XOThemeDatabase.UnlockTheme(t.id);
                    PlaySfx("reward");
                    SelectAndApplyTheme(themeIdx);
                });
        }

        private void SelectAndApplyTheme(int themeIdx)
        {
            XOThemeDatabase.SetSelectedThemeIndex(themeIdx);
            _activeTheme = XOThemeDatabase.Themes[themeIdx];
            ApplyThemeToAllUI();
            RefreshThemeShopUI();
        }

        private void OnPieceSkinPrimaryActionClicked(int skinIdx)
        {
            XOPieceSkin ps = XOThemeDatabase.PieceSkins[skinIdx];
            if (XOThemeDatabase.IsPieceSkinUnlocked(ps.id))
            {
                PlaySfx("click");
                SelectAndApplyPieceSkin(skinIdx);
            }
            else if (Coins >= ps.coinCost)
            {
                Coins -= ps.coinCost;
                XOThemeDatabase.UnlockPieceSkin(ps.id);
                PlaySfx("reward");
                SelectAndApplyPieceSkin(skinIdx);
            }
            else
            {
                OnPieceSkinAdUnlockClicked(skinIdx);
            }
        }

        private void OnPieceSkinAdUnlockClicked(int skinIdx)
        {
            XOPieceSkin ps = XOThemeDatabase.PieceSkins[skinIdx];
            PlaySfx("click");
            XOPlayableAdsBridge.Instance.ShowRewardedAd(
                $"Unlock '{ps.displayName}' Piece Skin Permanently",
                onRewarded: () =>
                {
                    XOThemeDatabase.UnlockPieceSkin(ps.id);
                    PlaySfx("reward");
                    SelectAndApplyPieceSkin(skinIdx);
                });
        }

        private void SelectAndApplyPieceSkin(int skinIdx)
        {
            XOThemeDatabase.SetSelectedPieceSkinIndex(skinIdx);
            _activePieceSkin = XOThemeDatabase.PieceSkins[skinIdx];
            RefreshTurnAndScoreUI();
            RefreshAllCells(animateLastMove: false);
            RefreshThemeShopUI();
        }

        private void ApplyThemeToAllUI()
        {
            if (Camera.main != null)
                Camera.main.backgroundColor = _activeTheme.backgroundTop;

            if (_backgroundTopImg != null)
                _backgroundTopImg.color = _activeTheme.backgroundTop;

            if (_headerCardImg != null)
                _headerCardImg.color = _activeTheme.headerCardColor;

            if (_questCardImg != null)
                _questCardImg.color = _activeTheme.panelSurfaceColor;

            if (_modeCardImg != null)
                _modeCardImg.color = _activeTheme.panelSurfaceColor;

            if (_lobbyHeroCardImg != null)
                _lobbyHeroCardImg.color = _activeTheme.panelSurfaceColor;

            if (_actionsCardImg != null)
                _actionsCardImg.color = _activeTheme.panelSurfaceColor;

            if (_missionText != null)
                _missionText.color = _activeTheme.primaryTextColor;

            if (_boardOuterFrameImg != null)
                _boardOuterFrameImg.color = _activeTheme.boardFrameColor;

            if (_statusBannerImg != null)
                _statusBannerImg.color = _activeTheme.panelSurfaceColor;

            if (_statusBannerText != null)
                _statusBannerText.color = _activeTheme.primaryTextColor;

            RefreshHeaderAndProgressionUI();
            RefreshTurnAndScoreUI();
            RefreshAllCells(animateLastMove: false);
        }

        // =====================================================================
        // UI STATE REFRESHERS & WINNING STRIKE LINE
        // =====================================================================

        private void OnSelectVariantAndSize(XOVariantRule variant, int newSize, int newWinLen)
        {
            PlaySfx("click");
            if (currentGameMode == GameMode.PuzzleStage)
            {
                currentGameMode = GameMode.VsAI;
            }
            currentVariant = variant;
            currentBoardSize = newSize;
            winLength = newWinLen;
            StartNewMatch(newSize);
        }

        private void OnCycleAIRivalButtonClicked()
        {
            PlaySfx("click");
            if (currentGameMode != GameMode.VsAI)
            {
                currentGameMode = GameMode.VsAI;
            }
            else
            {
                int nextBot = (XOProgressionSystem.SelectedBotIndex + 1) % XOProgressionSystem.Bots.Length;
                XOProgressionSystem.SelectedBotIndex = nextBot;
            }

            XOBotProfile bot = XOProgressionSystem.GetActiveBot();
            currentDifficulty = bot.baseDifficulty;
            _scoreX = 0;
            _scoreO = 0;
            StartNewMatch(currentBoardSize);
            if (enableLiveMatchmaking)
            {
                SpawnTacticalCalloutBanner("MATCHMAKING FOUND!", $"Entering Arena vs {bot.botName} [ELO {bot.eloRating}]", XOThemeDatabase.Hex("#0EA5E9"));
            }
        }

        private void OnSelectMode(GameMode mode, AIDifficulty diff)
        {
            PlaySfx("click");
            currentGameMode = mode;
            currentDifficulty = diff;
            _scoreX = 0;
            _scoreO = 0;
            if (mode == GameMode.VsAI || mode == GameMode.TwoPlayerLocal)
            {
                currentBoardSize = 15;
                winLength = 5;
                currentVariant = XOVariantRule.ClassicCaro;
            }
            StartNewMatch(currentBoardSize);
        }

        private void RefreshSelectorButtonsUI()
        {
            Color activeCol = XOThemeDatabase.Hex("#E0F2FE"); // Soft pastel sky highlight for selected mode
            Color inactiveCol = Color.white; // Clean porcelain card for inactive
            bool isPuzzle = (currentGameMode == GameMode.PuzzleStage);

            if (_size10Btn != null) _size10Btn.GetComponent<Image>().color = (!isPuzzle && currentVariant == XOVariantRule.ClassicCaro && currentBoardSize == 10) ? activeCol : inactiveCol;
            if (_size15Btn != null) _size15Btn.GetComponent<Image>().color = (!isPuzzle && currentVariant == XOVariantRule.ClassicCaro && currentBoardSize == 15) ? activeCol : inactiveCol;
            if (_varInfinite3Btn != null) _varInfinite3Btn.GetComponent<Image>().color = (!isPuzzle && currentVariant == XOVariantRule.Infinite3x3) ? activeCol : inactiveCol;
            if (_varGobbletBtn != null) _varGobbletBtn.GetComponent<Image>().color = (!isPuzzle && currentVariant == XOVariantRule.GobbletStack) ? activeCol : inactiveCol;
            if (_varGravityBtn != null) _varGravityBtn.GetComponent<Image>().color = (!isPuzzle && currentVariant == XOVariantRule.GravityDrop) ? activeCol : inactiveCol;
            if (_varStrictBtn != null) _varStrictBtn.GetComponent<Image>().color = (!isPuzzle && currentVariant == XOVariantRule.StrictCaro) ? activeCol : inactiveCol;
            if (_varChaosBtn != null) _varChaosBtn.GetComponent<Image>().color = (!isPuzzle && currentVariant == XOVariantRule.ChaosMinefield) ? activeCol : inactiveCol;

            if (_modeAiTierBtn != null) _modeAiTierBtn.GetComponent<Image>().color = (currentGameMode == GameMode.VsAI) ? activeCol : inactiveCol;
            if (_mode2PBtn != null) _mode2PBtn.GetComponent<Image>().color = (currentGameMode == GameMode.TwoPlayerLocal) ? activeCol : inactiveCol;
            if (_modePuzzleBtn != null) _modePuzzleBtn.GetComponent<Image>().color = isPuzzle ? XOThemeDatabase.Hex("#FEE2E2") : inactiveCol;

            if (_modeAiTierBtnText != null && currentGameMode == GameMode.VsAI)
            {
                XOBotProfile bot = XOProgressionSystem.GetActiveBot();
                string tierTag = bot.baseDifficulty switch
                {
                    AIDifficulty.Easy => "EASY",
                    AIDifficulty.Normal => "NORMAL",
                    _ => "HARD"
                };
                _modeAiTierBtnText.text = $"AI: {tierTag}\n[{bot.botName}]";
            }

            if (_modePuzzleBtnText != null)
            {
                _modePuzzleBtnText.text = $"BOSS STAGE #{XOProgressionSystem.CurrentPuzzleStage}";
            }

            RefreshLobbyHeroUI();
        }

        public void RefreshHeaderAndProgressionUI()
        {
            if (_coinText != null)
                _coinText.text = $"{Coins} Coins";

            if (_luckySpinHeaderBtn != null && _luckySpinHeaderBtnText != null)
            {
                if (FreeSpinAvailable)
                {
                    _luckySpinHeaderBtnText.text = "FREE SPIN!";
                    _luckySpinHeaderBtn.GetComponent<Image>().color = XOThemeDatabase.Hex("#22C55E");
                }
                else
                {
                    _luckySpinHeaderBtnText.text = "LUCKY SPIN";
                    _luckySpinHeaderBtn.GetComponent<Image>().color = XOThemeDatabase.Hex("#F59E0B");
                }
            }

            if (_rankBadgeText != null)
            {
                RankInfo r = XOProgressionSystem.GetCurrentRank(out int starsInTier, out int tierTarget);
                _rankBadgeText.text = $"{r.title} ({starsInTier}/{tierTarget})";
            }

            if (_streakText != null)
            {
                int st = XOProgressionSystem.WinStreak;
                _streakText.text = $"Streak: {st}";
            }

            if (_missionText != null)
            {
                XOMissionData m = XOProgressionSystem.GetActiveMission();
                _missionText.text = $"QUEST: {m.description} (+{m.rewardCoins})";
            }

            if (_towerBadgeText != null)
            {
                _towerBadgeText.text = $"F.{XORoguelikeRunManager.CurrentFloor}";
            }

            int cp = XOProgressionSystem.ChestProgress;
            if (_chestProgressFillImg != null)
            {
                _chestProgressFillImg.fillAmount = Mathf.Clamp01(cp / 3f);
            }

            if (_chestButtonText != null && _chestButton != null)
            {
                if (cp >= 3)
                {
                    _chestButtonText.text = "OPEN GOLD CHEST NOW!";
                    _chestButton.GetComponent<Image>().color = XOThemeDatabase.Hex("#22C55E");
                }
                else
                {
                    _chestButtonText.text = $"WIN CHEST ({cp}/3) — UNLOCK";
                    _chestButton.GetComponent<Image>().color = XOThemeDatabase.Hex("#D97706");
                }
            }
        }

        private void RefreshTurnAndScoreUI()
        {
            RefreshScoreTextsOnly();

            bool isXTurn = (_engine.CurrentTurn == CellPiece.X);
            _playerXCardImg.color = isXTurn ? Color.Lerp(_activeTheme.panelSurfaceColor, _activeTheme.xColor, 0.28f) : _activeTheme.panelSurfaceColor;
            _playerOCardImg.color = !isXTurn ? Color.Lerp(_activeTheme.panelSurfaceColor, _activeTheme.oColor, 0.28f) : _activeTheme.panelSurfaceColor;

            _playerXTitleText.color = _activeTheme.xColor;
            _playerOTitleText.color = _activeTheme.oColor;
            _scoreXText.color = _activeTheme.primaryTextColor;
            _scoreOText.color = _activeTheme.primaryTextColor;

            // Update Scoreboard Token Icons to match equipped Piece Skin
            string skinId = (_activePieceSkin != null) ? _activePieceSkin.id : "classic_xo";
            if (_playerXTokenIcon != null)
            {
                _playerXTokenIcon.sprite = XOProceduralAssets.GetPieceSkinSprite(skinId, true);
                _playerXTokenIcon.color = (skinId == "classic_xo") ? _activeTheme.xColor : Color.white;
            }
            if (_playerOTokenIcon != null)
            {
                _playerOTokenIcon.sprite = XOProceduralAssets.GetPieceSkinSprite(skinId, false);
                _playerOTokenIcon.color = (skinId == "classic_xo") ? _activeTheme.oColor : Color.white;
            }

            if (currentGameMode == GameMode.PuzzleStage && _currentPuzzle != null)
            {
                _playerXTitleText.text = $"YOU ({_puzzleMovesLeft} Left)";
                _playerOTitleText.text = $"BOSS #{_currentPuzzle.stageNumber}";

                if (_engine.State == MatchState.Playing && _skillTargeting == ActiveSkillTargeting.None)
                {
                    _statusBannerText.text = _isAIThinking
                        ? "Stage Boss is countering..."
                        : $"{_currentPuzzle.title}: {_currentPuzzle.objectiveHint}";
                }
            }
            else if (currentGameMode == GameMode.TowerClimb)
            {
                TowerFloorBlind blind = XORoguelikeRunManager.GetActiveBlind();
                string movesSub = (blind.maxPlayerMoves > 0)
                    ? $" ({Mathf.Max(0, blind.maxPlayerMoves - XORoguelikeRunManager.PlayerMovesThisMatch)} Moves)"
                    : $" (HP: {XORoguelikeRunManager.Lives}/{XORoguelikeRunManager.MaxLives})";

                string twinX = (_engine.RequiredWinLines > 1) ? $" [{_engine.CompletedWinLinesX.Count}/{_engine.RequiredWinLines} 5s]" : "";
                string twinO = (_engine.RequiredWinLines > 1) ? $" [{_engine.CompletedWinLinesO.Count}/{_engine.RequiredWinLines} 5s]" : "";

                _playerXTitleText.text = $"YOU{movesSub}{twinX}";
                _playerOTitleText.text = $"F.{blind.floorNumber} {blind.bossName.ToUpper()}{twinO}";

                if (_engine.State == MatchState.Playing && _skillTargeting == ActiveSkillTargeting.None)
                {
                    if (_isAIThinking)
                    {
                        _statusBannerText.text = $"{blind.bossName} ({blind.blindName}) is thinking...";
                    }
                    else
                    {
                        if (_engine.RequiredWinLines > 1)
                        {
                            _statusBannerText.text = $"Your Turn (X) vs {blind.bossName} — TWIN DRAGON: Connect TWO 5-in-a-rows ({_engine.CompletedWinLinesX.Count}/2) to Win!";
                        }
                        else
                        {
                            string limitNotice = (blind.maxPlayerMoves > 0)
                                ? $"[{Mathf.Max(0, blind.maxPlayerMoves - XORoguelikeRunManager.PlayerMovesThisMatch)} moves left]"
                                : "Connect 5 to advance";
                            _statusBannerText.text = $"Your Turn (X) vs {blind.bossName} — {blind.blindName}: {limitNotice}";
                        }
                    }
                }
            }
            else if (currentGameMode == GameMode.VsAI)
            {
                XOBotProfile bot = XOProgressionSystem.GetActiveBot();
                _playerXTitleText.text = $"YOU [{XOProgressionSystem.PlayerELO}]";
                _playerOTitleText.text = $"{bot.botName.ToUpper()} [{bot.eloRating}]";

                if (_engine.State == MatchState.Playing && _skillTargeting == ActiveSkillTargeting.None)
                {
                    if (_isAIThinking)
                    {
                        _statusBannerText.text = $"{bot.botName} is calculating next move...";
                    }
                    else
                    {
                        _statusBannerText.text = _engine.VariantRule switch
                        {
                            XOVariantRule.Infinite3x3    => "Infinite 3x3: Max 3 pieces per side! 4th move vanishes oldest [FADE] piece!",
                            XOVariantRule.GobbletStack   => $"4x4 Gobblet: Tap smaller enemy piece (Lv.1/Lv.2) to EAT it! ({_engine.GobbleStockX} Gobbles left)",
                            XOVariantRule.GravityDrop    => "Gravity Drop (Connect-4): Tap any column to drop your piece to the bottom!",
                            XOVariantRule.StrictCaro     => "Strict Caro (Blocked-Ends): 5-in-a-row blocked on BOTH ends does NOT win!",
                            XOVariantRule.ChaosMinefield => "Chaos Minefield: Watch out for Blast Mines & Twin Clone Portals!",
                            _                            => $"Your Turn (X) vs {bot.botName} — Connect 5 to win!"
                        };
                    }
                }
            }
            else
            {
                _playerXTitleText.text = "PLAYER 1";
                _playerOTitleText.text = "PLAYER 2";

                if (_engine.State == MatchState.Playing && _skillTargeting == ActiveSkillTargeting.None)
                {
                    _statusBannerText.text = isXTurn
                        ? "Player 1's Turn (X)"
                        : "Player 2's Turn (O)";
                }
            }

            RefreshBattleRelicBarUI();
        }

        private void RefreshScoreTextsOnly()
        {
            if (_scoreXText != null) _scoreXText.text = _scoreX.ToString();
            if (_scoreOText != null) _scoreOText.text = _scoreO.ToString();
        }

        public void RefreshBottomActionBadges()
        {
            UpdateSingleBadge(_undoBadgeBg, _undoBadgeText, UndoStock);
            UpdateSingleBadge(_hintBadgeBg, _hintBadgeText, HintStock);
            UpdateSingleBadge(_bombBadgeBg, _bombBadgeText, BombStock);
            UpdateSingleBadge(_freezeBadgeBg, _freezeBadgeText, FreezeStock);
            UpdateSingleBadge(_swapBadgeBg, _swapBadgeText, SwapStock);

            if (_leaderboardBadgeBg != null && _leaderboardBadgeText != null)
            {
                if (XOProgressionSystem.HasAnyClaimableAchievement())
                {
                    _leaderboardBadgeText.text = "CLAIM!";
                    _leaderboardBadgeText.color = Color.white;
                    _leaderboardBadgeBg.color = XOThemeDatabase.Hex("#22C55E");
                }
                else
                {
                    _leaderboardBadgeText.text = "RANK";
                    _leaderboardBadgeText.color = XOThemeDatabase.Hex("#0F172A");
                    _leaderboardBadgeBg.color = XOThemeDatabase.Hex("#FEF08A");
                }
            }
        }

        private void UpdateSingleBadge(Image bg, Text txt, int count)
        {
            if (bg == null || txt == null) return;
            if (count > 0)
            {
                txt.text = $"{count}";
                txt.color = Color.white;
                bg.color = new Color(0.16f, 0.21f, 0.40f, 0.95f);
            }
            else
            {
                txt.text = "+AD";
                txt.color = XOProceduralAssets.ColInk;
                bg.color = XOProceduralAssets.ColSun;
            }
        }

        private void RefreshAllCells(bool animateLastMove)
        {
            MoveRecord? lastMove = _engine.GetLastMove();
            HashSet<Vector2Int> winSet = new HashSet<Vector2Int>(_engine.WinningCells);
            bool isBombTargetMode = (_skillTargeting == ActiveSkillTargeting.BombDestroy);
            bool isSwapTargetMode = (_skillTargeting == ActiveSkillTargeting.SwapPiece);

            int size = _engine.BoardSize;
            for (int r = 0; r < size; r++)
            {
                for (int c = 0; c < size; c++)
                {
                    int idx = r * size + c;
                    if (idx >= _cellViews.Count) continue;

                    bool isLast = lastMove.HasValue && lastMove.Value.row == r && lastMove.Value.col == c;
                    bool isWin = winSet.Contains(new Vector2Int(r, c));
                    bool isHint = (_activeHintCell.x == r && _activeHintCell.y == c);
                    bool isVanishingNext = _engine.IsPieceAboutToVanish(r, c);
                    int gobbleSize = (_engine.VariantRule == XOVariantRule.GobbletStack) ? _engine.PieceSizes[r, c] : 0;

                    _cellViews[idx].SetState(
                        _engine.Board[r, c],
                        _engine.SpecialTiles[r, c],
                        _engine.FrozenTurns[r, c],
                        _activeTheme,
                        _activePieceSkin,
                        isLast,
                        isWin,
                        isHint,
                        isBombTargetMode,
                        isSwapTargetMode,
                        isVanishingNext: isVanishingNext,
                        gobbleSize: gobbleSize,
                        animatePop: animateLastMove && isLast
                    );
                }
            }
        }

        private void ShowBotReactionBubble(string message)
        {
            if (_botChatBubbleObj == null || _botChatBubbleText == null) return;
            if (_botChatCoroutine != null) StopCoroutine(_botChatCoroutine);
            _botChatCoroutine = StartCoroutine(BotChatBubbleRoutine(message));
        }

        private IEnumerator BotChatBubbleRoutine(string message)
        {
            _botChatBubbleText.text = message;
            _botChatBubbleObj.transform.SetAsLastSibling();
            _botChatBubbleObj.SetActive(true);
            PlaySfx("bot_chat");

            CanvasGroup cg = _botChatBubbleObj.GetComponent<CanvasGroup>();
            RectTransform rt = _botChatBubbleObj.GetComponent<RectTransform>();

            float elapsed = 0f;
            float duration = 3.2f;
            while (elapsed < duration)
            {
                if (rt == null) yield break;
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);

                float scale = (t < 0.12f)
                    ? Mathf.Lerp(0.5f, 1.08f, t / 0.12f)
                    : ((t < 0.22f) ? Mathf.Lerp(1.08f, 1.0f, (t - 0.12f) / 0.10f) : 1.0f);
                rt.localScale = new Vector3(scale, scale, 1f);
                cg.alpha = (t < 0.8f) ? 1f : (1f - (t - 0.8f) / 0.2f);
                yield return null;
            }

            if (_botChatBubbleObj != null)
            {
                _botChatBubbleObj.SetActive(false);
            }
        }

        private void SpawnTacticalCalloutBanner(string title, string subtitle, Color bannerColor)
        {
            if (_boardOuterFrameRT == null) return;
            StartCoroutine(TacticalCalloutRoutine(title, subtitle, bannerColor));
        }

        private IEnumerator TacticalCalloutRoutine(string title, string subtitle, Color bannerColor)
        {
            GameObject bannerObj = new GameObject("TacticalCallout", typeof(RectTransform), typeof(CanvasGroup), typeof(Image));
            bannerObj.transform.SetParent(_boardOuterFrameRT, false);
            bannerObj.transform.SetAsLastSibling();

            RectTransform rt = bannerObj.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            Vector2 startPos = new Vector2(0f, 160f);
            rt.anchoredPosition = startPos;
            rt.sizeDelta = new Vector2(540f, 84f);

            Image bg = bannerObj.GetComponent<Image>();
            bg.sprite = XOProceduralAssets.Button3DSprite;
            bg.type = Image.Type.Sliced;
            bg.color = bannerColor;
            bg.raycastTarget = false;

            CreateIconImage(rt, XOProceduralAssets.IconLightning, new Vector2(0f, 0.5f), new Vector2(44f, 4f), new Vector2(48f, 48f));

            Text titleTxt = CreateText(rt, title, 25, FontStyle.Bold, TextAnchor.LowerLeft);
            titleTxt.rectTransform.offsetMin = new Vector2(82f, 40f);
            titleTxt.rectTransform.offsetMax = new Vector2(-18f, -6f);
            titleTxt.color = Color.white;

            Text subTxt = CreateText(rt, subtitle, 18, FontStyle.Bold, TextAnchor.UpperLeft);
            subTxt.rectTransform.offsetMin = new Vector2(82f, 10f);
            subTxt.rectTransform.offsetMax = new Vector2(-18f, -42f);
            subTxt.color = XOThemeDatabase.Hex("#FEF08A");

            CanvasGroup cg = bannerObj.GetComponent<CanvasGroup>();
            float duration = 1.45f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                if (rt == null) yield break;
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);

                rt.anchoredPosition = startPos + new Vector2(0f, t * 55f);
                float scale = (t < 0.14f) ? Mathf.Lerp(0.4f, 1.12f, t / 0.14f) : Mathf.Lerp(1.12f, 1.0f, (t - 0.14f) / 0.86f);
                rt.localScale = new Vector3(scale, scale, 1f);
                cg.alpha = (t < 0.72f) ? 1f : (1f - (t - 0.72f) / 0.28f);
                yield return null;
            }

            if (bannerObj != null)
            {
                Destroy(bannerObj);
            }
        }

        private void DrawWinStrikeLineIfApplicable()
        {
            if (_engine.WinningCells == null || _engine.WinningCells.Count < 2 || _winStrikeLineImg == null)
            {
                if (_winStrikeLineImg != null) _winStrikeLineImg.gameObject.SetActive(false);
                return;
            }

            Canvas.ForceUpdateCanvases();

            Vector2Int first = _engine.WinningCells[0];
            Vector2Int last = _engine.WinningCells[_engine.WinningCells.Count - 1];

            int idxA = first.x * _engine.BoardSize + first.y;
            int idxB = last.x * _engine.BoardSize + last.y;
            if (idxA >= _cellViews.Count || idxB >= _cellViews.Count) return;

            RectTransform rtA = _cellViews[idxA].GetComponent<RectTransform>();
            RectTransform rtB = _cellViews[idxB].GetComponent<RectTransform>();

            Vector2 posA = _boardOuterFrameRT.InverseTransformPoint(rtA.position);
            Vector2 posB = _boardOuterFrameRT.InverseTransformPoint(rtB.position);

            Vector2 mid = (posA + posB) * 0.5f;
            Vector2 dir = posB - posA;
            float distance = dir.magnitude + _boardGridLayout.cellSize.x * 0.55f;
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

            RectTransform lineRT = _winStrikeLineImg.rectTransform;
            lineRT.anchorMin = new Vector2(0.5f, 0.5f);
            lineRT.anchorMax = new Vector2(0.5f, 0.5f);
            lineRT.pivot = new Vector2(0.5f, 0.5f);
            lineRT.anchoredPosition = mid;
            lineRT.sizeDelta = new Vector2(distance, 14f);
            lineRT.localRotation = Quaternion.Euler(0f, 0f, angle);

            _winStrikeLineImg.color = _activeTheme.goldAdColor;
            _winStrikeLineImg.transform.SetAsLastSibling();
            _winStrikeLineImg.gameObject.SetActive(true);
        }

        private void PlaySfx(string key)
        {
            if (IsSoundMuted || _audioSource == null) return;
            AudioClip clip = XOProceduralAssets.GetSound(key);
            if (clip != null)
            {
                _audioSource.PlayOneShot(clip);
            }
        }

        // =====================================================================
        // LIGHTWEIGHT UGUI PARTICLE BURSTS (BOMB, FREEZE, VICTORY CONFETTI)
        // =====================================================================

        private void SpawnCellVFXBurst(int row, int col, Color c1, Color c2, int count)
        {
            int idx = row * _engine.BoardSize + col;
            if (idx < 0 || idx >= _cellViews.Count || _boardOuterFrameRT == null) return;

            StartCoroutine(ShakeBoardRoutine(0.18f, 11f));

            RectTransform cellRT = _cellViews[idx].GetComponent<RectTransform>();
            Vector2 localCenter = _boardOuterFrameRT.InverseTransformPoint(cellRT.position);

            for (int i = 0; i < count; i++)
            {
                float angle = (i / (float)count) * Mathf.PI * 2f + UnityEngine.Random.Range(-0.25f, 0.25f);
                float speed = UnityEngine.Random.Range(110f, 260f);
                Vector2 vel = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * speed;
                Color colChoice = (i % 2 == 0) ? c1 : c2;
                float size = UnityEngine.Random.Range(10f, 20f);
                StartCoroutine(AnimateUIParticleRoutine(_boardOuterFrameRT, localCenter, vel, colChoice, size, 0.48f));
            }
        }

        private void SpawnVictoryConfettiBurst()
        {
            if (_boardOuterFrameRT == null) return;
            StartCoroutine(ShakeBoardRoutine(0.22f, 9f));

            Color[] palette =
            {
                XOThemeDatabase.Hex("#FBBF24"),
                XOThemeDatabase.Hex("#22D3EE"),
                XOThemeDatabase.Hex("#4ADE80"),
                XOThemeDatabase.Hex("#FB7185"),
                XOThemeDatabase.Hex("#A855F7")
            };

            for (int i = 0; i < 28; i++)
            {
                Vector2 startPos = new Vector2(UnityEngine.Random.Range(-220f, 220f), UnityEngine.Random.Range(-120f, 160f));
                float angle = UnityEngine.Random.Range(0f, Mathf.PI * 2f);
                float speed = UnityEngine.Random.Range(140f, 340f);
                Vector2 vel = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * speed;
                Color colChoice = palette[i % palette.Length];
                float size = UnityEngine.Random.Range(12f, 24f);
                StartCoroutine(AnimateUIParticleRoutine(_boardOuterFrameRT, startPos, vel, colChoice, size, 0.75f));
            }
        }

        private IEnumerator ShakeBoardRoutine(float duration, float magnitude)
        {
            if (_boardOuterFrameRT == null) yield break;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float damper = 1f - Mathf.Clamp01(elapsed / duration);
                float ox = UnityEngine.Random.Range(-1f, 1f) * magnitude * damper;
                float oy = UnityEngine.Random.Range(-1f, 1f) * magnitude * damper;
                _boardOuterFrameRT.localPosition = new Vector3(ox, oy, 0f);
                yield return null;
            }
            if (_boardOuterFrameRT != null)
            {
                _boardOuterFrameRT.localPosition = Vector3.zero;
            }
        }

        private void SpawnFloatingCoinToast(int amount)
        {
            if (_mainCanvas == null || amount <= 0) return;
            StartCoroutine(FloatingCoinToastRoutine(amount));
        }

        private IEnumerator FloatingCoinToastRoutine(int amount)
        {
            GameObject toastObj = new GameObject("FloatingCoinToast", typeof(RectTransform), typeof(CanvasGroup), typeof(Image));
            toastObj.transform.SetParent(_mainCanvas.transform, false);
            toastObj.transform.SetAsLastSibling();

            RectTransform rt = toastObj.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            Vector2 startPos = new Vector2(0f, 60f);
            rt.anchoredPosition = startPos;
            rt.sizeDelta = new Vector2(280f, 56f);

            Image bg = toastObj.GetComponent<Image>();
            bg.sprite = XOProceduralAssets.Pill3DSprite;
            bg.type = Image.Type.Sliced;
            bg.color = XOThemeDatabase.Hex("#F59E0B");
            bg.raycastTarget = false;

            CreateIconImage(rt, XOProceduralAssets.IconCoin, new Vector2(0f, 0.5f), new Vector2(36f, 2f), new Vector2(38f, 38f));
            Text txt = CreateText(rt, $"+{amount} COINS!", 24, FontStyle.Bold, TextAnchor.MiddleCenter);
            txt.rectTransform.offsetMin = new Vector2(46f, 4f);
            txt.color = XOThemeDatabase.Hex("#0F172A");

            CanvasGroup cg = toastObj.GetComponent<CanvasGroup>();
            float duration = 1.05f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                if (rt == null) yield break;
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);

                rt.anchoredPosition = startPos + new Vector2(0f, t * 115f);
                float popScale = (t < 0.18f) ? Mathf.Lerp(0.6f, 1.12f, t / 0.18f) : Mathf.Lerp(1.12f, 1.0f, (t - 0.18f) / 0.82f);
                rt.localScale = new Vector3(popScale, popScale, 1f);
                cg.alpha = (t < 0.7f) ? 1f : (1f - (t - 0.7f) / 0.3f);
                yield return null;
            }

            if (toastObj != null)
            {
                Destroy(toastObj);
            }
        }

        private IEnumerator AnimateUIParticleRoutine(RectTransform parent, Vector2 startPos, Vector2 velocity, Color color, float size, float duration)
        {
            GameObject pObj = new GameObject("VFXParticle", typeof(RectTransform), typeof(Image));
            pObj.transform.SetParent(parent, false);
            RectTransform rt = pObj.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = startPos;
            rt.sizeDelta = new Vector2(size, size);

            Image img = pObj.GetComponent<Image>();
            img.sprite = XOProceduralAssets.CircleSprite;
            img.color = color;
            img.raycastTarget = false;

            float elapsed = 0f;
            Vector2 pos = startPos;
            while (elapsed < duration)
            {
                if (rt == null) yield break;
                float dt = Time.deltaTime;
                elapsed += dt;
                float t = Mathf.Clamp01(elapsed / duration);

                pos += velocity * dt;
                velocity *= (1f - 3.2f * dt);
                velocity.y -= 180f * dt; // subtle gravity

                rt.anchoredPosition = pos;
                float scale = 1f - t * 0.85f;
                rt.localScale = new Vector3(scale, scale, 1f);
                img.color = new Color(color.r, color.g, color.b, 1f - t);
                yield return null;
            }

            if (pObj != null)
            {
                Destroy(pObj);
            }
        }

        // =====================================================================
        // UI HELPER BUILDERS (3D BUTTONS & ILLUSTRATED ICON IMAGES)
        // =====================================================================

        private GameObject CreateUIObject(string name, Transform parent)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go;
        }

        private void StretchFull(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        private Image CreateIconImage(Transform parent, Sprite sprite, Vector2 anchor, Vector2 anchoredPos, Vector2 size)
        {
            GameObject go = new GameObject("Icon_" + (sprite != null ? sprite.name : "Img"), typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.anchorMin = anchor;
            rt.anchorMax = anchor;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = size;

            Image img = go.GetComponent<Image>();
            img.sprite = sprite;
            img.preserveAspect = true;
            img.raycastTarget = false;
            return img;
        }

        private Text CreateText(Transform parent, string content, int fontSize, FontStyle style, TextAnchor anchor)
        {
            GameObject go = new GameObject("Text", typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            RectTransform rt = go.GetComponent<RectTransform>();
            StretchFull(rt);

            Text txt = go.GetComponent<Text>();
            txt.font = _uiFont;
            txt.text = content;
            txt.fontSize = fontSize;
            txt.fontStyle = style;
            txt.alignment = anchor;
            txt.color = Color.white;
            txt.raycastTarget = false;
            return txt;
        }

        private Button Create3DButton(Transform parent, string label, Vector2 size, Color bgColor, Color textColor, int fontSize)
        {
            GameObject go = new GameObject("Btn3D_" + label, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);

            RectTransform rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = size;

            Image img = go.GetComponent<Image>();
            img.sprite = XOProceduralAssets.Button3DSprite;
            img.type = Image.Type.Sliced;
            img.color = bgColor;

            Button btn = go.GetComponent<Button>();
            Text txt = CreateText(rt, label, fontSize, FontStyle.Bold, TextAnchor.MiddleCenter);
            txt.rectTransform.offsetMin = new Vector2(8f, 6f);
            txt.rectTransform.offsetMax = new Vector2(-8f, -2f);
            txt.color = textColor;

            return btn;
        }

        private Button CreateIcon3DButton(Transform parent, Sprite iconSprite, string label, Vector2 size, Color bgColor, Color textColor, int fontSize)
        {
            GameObject go = new GameObject("BtnIcon3D_" + label, typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
            go.transform.SetParent(parent, false);

            LayoutElement le = go.GetComponent<LayoutElement>();
            le.preferredWidth = size.x;
            le.preferredHeight = size.y;
            le.minWidth = size.x * 0.75f;
            le.minHeight = size.y * 0.75f;

            RectTransform rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = size;

            Image img = go.GetComponent<Image>();
            img.sprite = XOProceduralAssets.Button3DSprite;
            img.type = Image.Type.Sliced;
            img.color = bgColor;

            bool hasLabel = !string.IsNullOrEmpty(label);
            float iconSize = Mathf.Clamp(size.y * 0.58f, 28f, 56f);

            if (hasLabel)
            {
                CreateIconImage(rt, iconSprite, new Vector2(0f, 0.5f), new Vector2(iconSize * 0.68f + 8f, 2f), new Vector2(iconSize, iconSize));
                Text txt = CreateText(rt, label, fontSize, FontStyle.Bold, TextAnchor.MiddleCenter);
                txt.rectTransform.offsetMin = new Vector2(iconSize + 14f, 6f);
                txt.rectTransform.offsetMax = new Vector2(-12f, -2f);
                txt.color = textColor;
            }
            else
            {
                // Centered icon when button has no text label (e.g. Chest button)
                CreateIconImage(rt, iconSprite, new Vector2(0.5f, 0.5f), new Vector2(0f, 2f), new Vector2(iconSize, iconSize));
            }

            Button btn = go.GetComponent<Button>();
            return btn;
        }
    }
}
