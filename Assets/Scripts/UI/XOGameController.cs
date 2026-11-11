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

    public partial class XOGameController : MonoBehaviour
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
        private Image _backgroundBottomImg;

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
        private ScrollRect _towerScrollRect;
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

        /// <summary>Tracks which modals are visible; filled by XOModalTracker on every modal root.</summary>
        private readonly XOModalState _modalState = new XOModalState();

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
                bool isModalOpen = _modalState.IsAnyOpen;

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

            // Background: a solid bottom colour with the top colour faded over it gives a soft vertical gradient.
            GameObject bgBottomObj = CreateUIObject("BackgroundBottom", canvasObj.transform);
            StretchFull(bgBottomObj.GetComponent<RectTransform>());
            _backgroundBottomImg = bgBottomObj.AddComponent<Image>();
            _backgroundBottomImg.color = _activeTheme.backgroundBottom;
            _backgroundBottomImg.raycastTarget = false;

            GameObject bgObj = CreateUIObject("Background", canvasObj.transform);
            StretchFull(bgObj.GetComponent<RectTransform>());
            _backgroundTopImg = bgObj.AddComponent<Image>();
            _backgroundTopImg.sprite = XOProceduralAssets.VerticalFadeSprite;
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

            AttachButtonFeedback(canvasObj.transform);
        }

        public void SetScreenState(ScreenState state)
        {
            CurrentScreen = state;
            if (_lobbyRootObj != null)
                _lobbyRootObj.SetActive(state == ScreenState.Lobby);
            if (_battleRootObj != null)
                _battleRootObj.SetActive(state == ScreenState.Battle);

            if (_shownScreen != state)
            {
                _shownScreen = state;
                FadeInScreen(state == ScreenState.Lobby ? _lobbyRootObj : (state == ScreenState.Battle ? _battleRootObj : null));
            }

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

        // Short fade so switching between lobby and battle never reads as a hard cut.
        private const float ScreenFadeDuration = 0.25f;
        private ScreenState? _shownScreen;
        private Coroutine _screenFadeRoutine;
        private CanvasGroup _fadingGroup;

        private void FadeInScreen(GameObject root)
        {
            if (root == null) return;

            if (_screenFadeRoutine != null)
            {
                StopCoroutine(_screenFadeRoutine);
                _screenFadeRoutine = null;
                if (_fadingGroup != null) _fadingGroup.alpha = 1f;
            }

            CanvasGroup group = root.GetComponent<CanvasGroup>();
            if (group == null) group = root.AddComponent<CanvasGroup>();
            _fadingGroup = group;
            _screenFadeRoutine = StartCoroutine(FadeInRoutine(group));
        }

        private IEnumerator FadeInRoutine(CanvasGroup group)
        {
            group.alpha = 0f;
            float elapsed = 0f;
            while (elapsed < ScreenFadeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                group.alpha = Mathf.SmoothStep(0f, 1f, elapsed / ScreenFadeDuration);
                yield return null;
            }

            group.alpha = 1f;
            _screenFadeRoutine = null;
        }

        public void ReturnToLobby()
        {
            PlaySfx("click");
            XORoguelikeRunManager.IsTowerMatchActive = false;
            SetScreenState(ScreenState.Lobby);
        }
    }
}
