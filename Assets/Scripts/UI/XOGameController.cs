using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using XOPlayable.Ads;
using XOPlayable.Core;
using XOPlayable.Visuals;

namespace XOPlayable.UI
{
    /// <summary>
    /// Thin scene coordinator binding <see cref="XOCaroEngine"/>, state persistence, audio,
    /// UI screen presenters, modals, responsive layout, and match flow.
    /// </summary>
    public class XOGameController : MonoBehaviour
    {
        public static XOGameController Instance { get; private set; }

        [Header("Default Match Settings")]
        public int currentBoardSize = 15;
        public int winLength = 5;
        public XOVariantRule currentVariant = XOVariantRule.ClassicCaro;
        public GameMode currentGameMode = GameMode.VsAI;
        public AIDifficulty currentDifficulty = AIDifficulty.Normal;
        public float turnTimeLimit = 30f;

        [Header("Audio, Haptics & Playable Flags")]
        public bool enableAmbientBgm = true;
        public bool enableHaptics = true;
        public bool enableLiveMatchmaking = true;
        public bool showStoreInstallButton = true;

        /// <summary>Optional resolution override used by automated multi-resolution QA tools.</summary>
        public Vector2Int? DebugScreenSizeOverride { get; set; }

        public ScreenState CurrentScreen { get; private set; } = ScreenState.Lobby;

        private readonly XOCaroEngine _engine = new XOCaroEngine();
        private readonly XOModalState _modalState = new XOModalState();
        private XOGameStateStore _store;
        private XOAudioManager _audio;
        private Font _uiFont;

        private CanvasScaler _canvasScaler;
        private Image _backgroundTopImg;
        private Image _backgroundBottomImg;
        private RectTransform _fxOverlayRT;

        private XOLobbyPresenter _lobby;
        private XOBattlePresenter _battle;
        private XOBattleEffectsPresenter _effects;
        private XOResponsiveLayoutService _layout;

        private XOModeSelectModal _modeModal;
        private XOTowerModal _towerModal;
        private XOCaravanModal _caravanModal;
        private XOThemeShopModal _themeShopModal;
        private XOLeaderboardModal _leaderboardModal;
        private XOLuckyWheelModal _luckyWheelModal;
        private XOResultModalsPresenter _resultModals;
        private XOTacticalSkillsController _skills;
        private XOMatchOutcomeService _outcomes;
        private XOMatchSessionController _matchSession;

        public int Coins
        {
            get => _store != null ? _store.Coins : 0;
            set { if (_store != null) _store.Coins = value; }
        }

        public int HintStock
        {
            get => _store != null ? _store.HintStock : 0;
            set { if (_store != null) _store.HintStock = value; }
        }

        public int UndoStock
        {
            get => _store != null ? _store.UndoStock : 0;
            set { if (_store != null) _store.UndoStock = value; }
        }

        public int BombStock
        {
            get => _store != null ? _store.BombStock : 0;
            set { if (_store != null) _store.BombStock = value; }
        }

        public int FreezeStock
        {
            get => _store != null ? _store.FreezeStock : 0;
            set { if (_store != null) _store.FreezeStock = value; }
        }

        public int SwapStock
        {
            get => _store != null ? _store.SwapStock : 0;
            set { if (_store != null) _store.SwapStock = value; }
        }

        public bool FreeSpinAvailable
        {
            get => _store != null && _store.FreeSpinAvailable;
            set { if (_store != null) _store.FreeSpinAvailable = value; }
        }

        public bool IsSoundMuted
        {
            get => _audio != null && _audio.IsMuted;
            set { if (_audio != null) { _audio.IsMuted = value; _lobby?.RefreshHeaderSoundIcon(); } }
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            XOStorageUnityBootstrap.Configure();

            _uiFont = XOUIBuilder.LoadDefaultFont();
            _store = new XOGameStateStore();
            _store.OnEconomyChanged += RefreshHeaderAndProgressionUI;

            _audio = new XOAudioManager();
            _audio.Initialize(gameObject, enableAmbientBgm);

            EnsureEventSystemAndCamera();
            Transform canvasTransform = BuildRootCanvasAndBackgrounds();
            BuildPresentersAndModals(canvasTransform);

            ApplyThemeToAllUI();
            SetScreenState(ScreenState.Lobby);
        }

        private void Update()
        {
            _layout?.UpdateLayout(CurrentScreen, DebugScreenSizeOverride ?? Vector2Int.zero, currentBoardSize);
            bool isAdShowing = XOPlayableAdsBridge.Instance != null && XOPlayableAdsBridge.Instance.IsShowingAd;
            if (CurrentScreen == ScreenState.Battle && !_modalState.IsAnyOpen && !isAdShowing)
            {
                _matchSession?.TickTurnTimer(Time.deltaTime);
            }
        }

        private static void EnsureEventSystemAndCamera()
        {
            if (Camera.main == null)
            {
                GameObject camObj = new GameObject("MainCamera");
                camObj.tag = "MainCamera";
                Camera cam = camObj.AddComponent<Camera>();
                cam.orthographic = true;
                cam.clearFlags = CameraClearFlags.SolidColor;
            }

            if (FindFirstObjectByType<EventSystem>() == null)
            {
                GameObject esObj = new GameObject("EventSystem");
                esObj.AddComponent<EventSystem>();
                // Resolve Unity.InputSystem dynamically so XOPlayable.UI compiles even after InputSystem package trimming.
                Type newInputModule = Type.GetType("UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem");
                if (newInputModule != null)
                    esObj.AddComponent(newInputModule);
                else
                    esObj.AddComponent<StandaloneInputModule>();
            }
        }

        private Transform BuildRootCanvasAndBackgrounds()
        {
            GameObject canvasObj = new GameObject("XOCaroCanvas");
            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;

            _canvasScaler = canvasObj.AddComponent<CanvasScaler>();
            _canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            _canvasScaler.referenceResolution = new Vector2(1080f, 1920f);
            _canvasScaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            _canvasScaler.matchWidthOrHeight = 0.5f;
            canvasObj.AddComponent<GraphicRaycaster>();

            GameObject bgBottomObj = XOUIBuilder.CreateUIObject("BackgroundBottom", canvas.transform);
            _backgroundBottomImg = bgBottomObj.AddComponent<Image>();
            _backgroundBottomImg.raycastTarget = false;

            GameObject bgTopObj = XOUIBuilder.CreateUIObject("BackgroundTop", canvas.transform);
            _backgroundTopImg = bgTopObj.AddComponent<Image>();
            _backgroundTopImg.sprite = XOProceduralAssets.VerticalFadeSprite;
            _backgroundTopImg.raycastTarget = false;

            return canvas.transform;
        }

        private void BuildPresentersAndModals(Transform canvasTransform)
        {
            GameObject fxObj = XOUIBuilder.CreateUIObject("BattleFXOverlay", canvasTransform);
            _fxOverlayRT = fxObj.GetComponent<RectTransform>();

            _effects = new XOBattleEffectsPresenter(
                this,
                _uiFont,
                _fxOverlayRT,
                (r, c) => _battle?.GetCellView(r, c),
                PlaySfx);

            _lobby = new XOLobbyPresenter(
                _uiFont,
                _store,
                _audio,
                onPlayNow: () =>
                {
                    PlaySfx("click");
                    SetScreenState(ScreenState.Battle);
                    StartNewMatch(currentBoardSize);
                },
                onOpenModeSelect: () => _modeModal.Open(currentGameMode, currentVariant, currentBoardSize, currentDifficulty),
                onOpenTower: OpenTowerModal,
                onOpenShop: () => _themeShopModal.Open(),
                onOpenLeaderboard: () => _leaderboardModal.Open(currentGameMode),
                onOpenLuckyWheel: () => _luckyWheelModal.Open(),
                onChestClicked: () => _skills.OnChestClicked(),
                onFreeCoinsClicked: () => _skills.OnFreeCoinsClicked());

            RectTransform headerRT = _lobby.BuildSharedHeader(canvasTransform, showStoreInstallButton, ReturnToLobby);
            _lobby.Build(canvasTransform);

            _battle = new XOBattlePresenter(
                _uiFont,
                _store,
                _engine,
                onCellClicked: (r, c) => _matchSession.OnBoardCellClicked(r, c),
                onHomeClicked: ReturnToLobby,
                onModeQuickClicked: () => _modeModal.Open(currentGameMode, currentVariant, currentBoardSize, currentDifficulty),
                onHintClicked: () => _skills.OnHintClicked(),
                onUndoClicked: () => _skills.OnUndoClicked(),
                onBombClicked: () => _skills.OnBombClicked(),
                onFreezeClicked: () => _skills.OnFreezeClicked(),
                onSwapClicked: () => _skills.OnSwapClicked(),
                onResetClicked: () => { PlaySfx("click"); StartNewMatch(currentBoardSize); });

            _battle.Build(canvasTransform);
            _effects.BindBoardVisuals(_battle.BoardOuterFrameRT, _battle.BoardGridContainerRT);
            _fxOverlayRT.SetAsLastSibling();

            _modeModal = new XOModeSelectModal(_uiFont, PlaySfx, (mode, variant, size, diff) =>
            {
                currentGameMode = mode;
                currentVariant = variant;
                currentBoardSize = size;
                currentDifficulty = diff;
                SetScreenState(ScreenState.Battle);
                StartNewMatch(currentBoardSize);
            });
            _modeModal.Build(canvasTransform);

            _towerModal = new XOTowerModal(_uiFont, PlaySfx, StartTowerMatch, OpenCaravanDraftModal);
            _towerModal.Build(canvasTransform);

            _caravanModal = new XOCaravanModal(
                _uiFont,
                _store,
                PlaySfx,
                (title, sub, col) => _effects.SpawnTacticalCalloutBanner(title, sub, col),
                RefreshHeaderAndProgressionUI,
                StartNextTowerFloor);
            _caravanModal.Build(canvasTransform);

            _themeShopModal = new XOThemeShopModal(
                _uiFont,
                _store,
                PlaySfx,
                (title, sub, col) => _effects.SpawnTacticalCalloutBanner(title, sub, col),
                ApplyThemeToAllUI,
                () => { RefreshHeaderAndProgressionUI(); RefreshBottomActionBadges(); });
            _themeShopModal.Build(canvasTransform);

            _leaderboardModal = new XOLeaderboardModal(
                _uiFont,
                _store,
                PlaySfx,
                onChallengeBot: botIdx =>
                {
                    XOProgressionSystem.SelectedBotIndex = botIdx;
                    XOBotProfile chosen = XOProgressionSystem.GetActiveBot();
                    currentGameMode = GameMode.VsAI;
                    currentDifficulty = chosen.baseDifficulty;
                    _store.ScoreX = 0;
                    _store.ScoreO = 0;
                    SetScreenState(ScreenState.Battle);
                    StartNewMatch(currentBoardSize);
                },
                () => { RefreshHeaderAndProgressionUI(); RefreshBottomActionBadges(); });
            _leaderboardModal.Build(canvasTransform);

            _luckyWheelModal = new XOLuckyWheelModal(
                this,
                _uiFont,
                _store,
                PlaySfx,
                () => { RefreshHeaderAndProgressionUI(); RefreshBottomActionBadges(); },
                () => enableHaptics);
            _luckyWheelModal.Build(canvasTransform);

            _resultModals = new XOResultModalsPresenter(
                _uiFont,
                _store,
                PlaySfx,
                onConfirmReviveWithAd: () => _skills.OnConfirmReviveWithAd(),
                onDeclineRevive: () => { _store.ScoreO++; _outcomes.ProcessPlayerDefeatConsequences(); },
                onPlayAgainOrNext: () =>
                {
                    if (currentGameMode == GameMode.TowerClimb && _engine.State == MatchState.XWon)
                        StartNextTowerFloor();
                    else if (currentGameMode == GameMode.TowerClimb && !XORoguelikeRunManager.IsRunActive)
                    {
                        XORoguelikeRunManager.StartNewRun();
                        StartTowerMatch(1);
                    }
                    else
                        StartNewMatch(currentBoardSize);
                },
                onReturnToLobby: ReturnToLobby,
                onOpenCaravanDraft: OpenCaravanDraftModal,
                onEconomyUpdated: RefreshHeaderAndProgressionUI);
            _resultModals.Build(canvasTransform);

            Func<XOMatchConfig> configReader = () => new XOMatchConfig(currentGameMode, currentVariant, currentBoardSize, winLength, currentDifficulty, turnTimeLimit, enableHaptics);

            _outcomes = new XOMatchOutcomeService(
                this,
                _engine,
                _store,
                _audio,
                _battle,
                _effects,
                _resultModals,
                configReader,
                RefreshHeaderAndProgressionUI);

            _skills = new XOTacticalSkillsController(
                _engine,
                _store,
                _audio,
                _battle,
                _effects,
                _resultModals,
                configReader,
                RefreshHeaderAndProgressionUI,
                _outcomes.HandleMatchEnded);

            _matchSession = new XOMatchSessionController(
                this,
                _engine,
                _store,
                _audio,
                _battle,
                _effects,
                _skills,
                _outcomes,
                configReader,
                (size, winLen, rule) => { currentBoardSize = size; winLength = winLen; currentVariant = rule; },
                RefreshHeaderAndProgressionUI,
                () => _layout?.UpdateLayout(CurrentScreen, DebugScreenSizeOverride ?? Vector2Int.zero, currentBoardSize, force: true));

            GameObject[] modalRoots =
            {
                _modeModal.RootObj,
                _towerModal.RootObj,
                _caravanModal.RootObj,
                _themeShopModal.RootObj,
                _leaderboardModal.RootObj,
                _luckyWheelModal.RootObj,
                _resultModals.ReviveModalRoot,
                _resultModals.ResultModalRoot
            };
            foreach (GameObject modalRoot in modalRoots)
            {
                modalRoot?.GetComponent<XOModalTracker>()?.Bind(_modalState);
            }

            XOPlayableAdsBridge.Instance?.BuildAdOverlayUI(canvasTransform, _uiFont);

            _layout = new XOResponsiveLayoutService(_canvasScaler, headerRT, _lobby, _battle);
            _layout.RegisterModalCards(
                (_modeModal.CardRT, new Vector2(880f, 860f)),
                (_towerModal.CardRT, new Vector2(920f, 920f)),
                (_caravanModal.CardRT, new Vector2(920f, 880f)),
                (_themeShopModal.CardRT, new Vector2(940f, 920f)),
                (_leaderboardModal.CardRT, new Vector2(960f, 940f)),
                (_luckyWheelModal.CardRT, new Vector2(720f, 680f)),
                (_resultModals.ReviveCardRT, new Vector2(680f, 520f)),
                (_resultModals.ResultCardRT, new Vector2(720f, 700f)),
                (XOPlayableAdsBridge.Instance?.AdCardRT, new Vector2(720f, 520f)));
        }

        public void SetScreenState(ScreenState state)
        {
            CurrentScreen = state;
            if (_lobby?.RootObj != null) _lobby.RootObj.SetActive(state == ScreenState.Lobby);
            if (_battle?.RootObj != null) _battle.RootObj.SetActive(state == ScreenState.Battle);
            RefreshHeaderAndProgressionUI();
            _layout?.UpdateLayout(CurrentScreen, DebugScreenSizeOverride ?? Vector2Int.zero, currentBoardSize, force: true);
        }

        public void ReturnToLobby()
        {
            PlaySfx("click");
            _matchSession?.StopActiveRoutines();
            _effects?.ClearTransientEffects();
            _effects?.HideBotChatBubble();
            SetScreenState(ScreenState.Lobby);
        }

        public void OpenTowerModal() => _towerModal?.Open();
        public void OpenCaravanDraftModal() => _caravanModal?.Open();

        public void StartTowerMatch(int floorNumber)
        {
            PlaySfx("click");
            if (!XORoguelikeRunManager.IsRunActive) XORoguelikeRunManager.StartNewRun();
            XORoguelikeRunManager.CurrentFloor = Mathf.Clamp(floorNumber, 1, XORoguelikeCatalog.FloorCount);
            TowerFloorBlind blind = XORoguelikeRunManager.GetActiveBlind();

            currentGameMode = GameMode.TowerClimb;
            currentBoardSize = blind.boardSize;
            currentVariant = blind.variantRule;
            currentDifficulty = blind.difficulty;

            SetScreenState(ScreenState.Battle);
            StartNewMatch(currentBoardSize);
        }

        public void StartNextTowerFloor()
        {
            int cur = XORoguelikeRunManager.CurrentFloor;
            if (cur >= XORoguelikeCatalog.FloorCount)
            {
                ReturnToLobby();
                return;
            }
            StartTowerMatch(cur + 1);
        }

        public void StartNewMatch(int boardSize) => _matchSession?.StartNewMatch(boardSize);
        public void PlaySfx(string key) => _audio?.PlaySfx(key);

        public void RefreshHeaderAndProgressionUI()
        {
            _lobby?.Refresh(currentGameMode, currentVariant, currentBoardSize, currentDifficulty);
            _battle?.RefreshTurnAndScore(currentGameMode, turnTimeLimit);
        }

        public void RefreshBottomActionBadges() => _battle?.RefreshBottomActionBadges();
        public void RefreshBattleRelicBarUI() => _battle?.RefreshBattleRelicBar(currentGameMode);
        public void RefreshTowerModalUI() => _towerModal?.Refresh();

        public void ApplyThemeToAllUI()
        {
            XOThemePalette theme = _store.ActiveTheme;
            Color bgTop = theme.backgroundTop.ToUnityColor();
            if (Camera.main != null) Camera.main.backgroundColor = bgTop;
            if (_backgroundTopImg != null) _backgroundTopImg.color = bgTop;
            if (_backgroundBottomImg != null) _backgroundBottomImg.color = theme.backgroundBottom.ToUnityColor();

            _lobby?.ApplyTheme(theme);
            _battle?.ApplyTheme(theme);
            RefreshHeaderAndProgressionUI();
            _battle?.RefreshAllCells(animateLastMove: false);
        }
    }
}
