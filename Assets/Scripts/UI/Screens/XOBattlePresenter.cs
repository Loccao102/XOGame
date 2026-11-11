using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using XOPlayable.Core;
using XOPlayable.Visuals;

namespace XOPlayable.UI
{
    /// <summary>
    /// Builds and updates the active Battle screen: Player/AI scoreboard, turn timer bar,
    /// roguelike Relic strip, responsive NxN board grid of <see cref="XOCellView"/> tiles,
    /// status callout banner, and 6-button Tactical Action Deck.
    /// </summary>
    public sealed class XOBattlePresenter
    {
        private readonly Font _font;
        private readonly XOGameStateStore _store;
        private readonly XOCaroEngine _engine;

        public GameObject RootObj { get; private set; }
        public RectTransform RootRT { get; private set; }

        public RectTransform BoardOuterFrameRT { get; private set; }
        public RectTransform BoardGridContainerRT { get; private set; }
        public Image BoardOuterFrameImg { get; private set; }
        public Image StatusBannerImg { get; private set; }
        public Text StatusBannerText { get; private set; }
        public Image ActionsCardImg { get; private set; }

        private RectTransform _hudCardRT;
        private RectTransform _relicBarRT;
        private RectTransform _statusBannerRT;
        private RectTransform _actionsDeckRT;
        private GridLayoutGroup _boardGridLayout;

        private Text _playerXLabelText;
        private Text _playerXScoreText;
        private Text _playerOLabelText;
        private Text _playerOScoreText;
        private Image _turnTimerFillImg;
        private Text _turnRuleText;
        private Image _playerXCardBg;
        private Image _playerOCardBg;

        private Text _hintBadgeText;
        private Text _undoBadgeText;
        private Text _bombBadgeText;
        private Text _freezeBadgeText;
        private Text _swapBadgeText;
        private Button _modeQuickBtn;
        private Text _modeQuickBtnText;

        private GameObject _relicBarObj;
        private Transform _relicIconsContainer;
        private readonly List<XOCellView> _cellViews = new List<XOCellView>();
        private int _currentGridSize;

        private readonly Action<int, int> _onCellClicked;
        private readonly Action _onHomeClicked;
        private readonly Action _onModeQuickClicked;
        private readonly Action _onHintClicked;
        private readonly Action _onUndoClicked;
        private readonly Action _onBombClicked;
        private readonly Action _onFreezeClicked;
        private readonly Action _onSwapClicked;
        private readonly Action _onResetClicked;

        public XOBattlePresenter(
            Font font,
            XOGameStateStore store,
            XOCaroEngine engine,
            Action<int, int> onCellClicked,
            Action onHomeClicked,
            Action onModeQuickClicked,
            Action onHintClicked,
            Action onUndoClicked,
            Action onBombClicked,
            Action onFreezeClicked,
            Action onSwapClicked,
            Action onResetClicked)
        {
            _font = font;
            _store = store;
            _engine = engine;
            _onCellClicked = onCellClicked;
            _onHomeClicked = onHomeClicked;
            _onModeQuickClicked = onModeQuickClicked;
            _onHintClicked = onHintClicked;
            _onUndoClicked = onUndoClicked;
            _onBombClicked = onBombClicked;
            _onFreezeClicked = onFreezeClicked;
            _onSwapClicked = onSwapClicked;
            _onResetClicked = onResetClicked;
        }

        /// <summary>Builds the entire Battle screen hierarchy under the canvas.</summary>
        public void Build(Transform canvasTransform)
        {
            RootObj = XOUIBuilder.CreateUIObject("BattleScreenRoot", canvasTransform);
            RootRT = RootObj.GetComponent<RectTransform>();

            BuildHudScoreboard();
            BuildRelicBar();
            BuildBoardFrame();
            BuildStatusBanner();
            BuildActionDeck();
        }

        private void BuildHudScoreboard()
        {
            GameObject hudObj = XOUIBuilder.CreateUIObject("BattleHudCard", RootRT);
            _hudCardRT = hudObj.GetComponent<RectTransform>();
            _hudCardRT.anchorMin = new Vector2(0.5f, 0.5f);
            _hudCardRT.anchorMax = new Vector2(0.5f, 0.5f);
            _hudCardRT.pivot = new Vector2(0.5f, 0.5f);

            Image hudBg = hudObj.AddComponent<Image>();
            hudBg.sprite = XOProceduralAssets.CardPanelSprite;
            hudBg.type = Image.Type.Sliced;
            hudBg.color = _store.ActiveTheme.panelSurfaceColor.ToUnityColor();

            // Home button
            Button homeBtn = XOUIBuilder.Create3DButton(_hudCardRT, _font, "HUB", new Vector2(76f, 48f), XOProceduralAssets.ColTileEdge, XOProceduralAssets.ColInk, 15);
            RectTransform hRT = homeBtn.GetComponent<RectTransform>();
            hRT.anchorMin = new Vector2(0f, 1f);
            hRT.anchorMax = new Vector2(0f, 1f);
            hRT.pivot = new Vector2(0f, 1f);
            hRT.anchoredPosition = new Vector2(14f, -12f);
            homeBtn.onClick.AddListener(() => _onHomeClicked?.Invoke());

            // Player X Card
            _playerXCardBg = BuildPlayerPill(_hudCardRT, "PlayerXPill", XOProceduralAssets.XPieceSprite, new Vector2(102f, -12f), XOProceduralAssets.Hex("#E0F2FE"), out _playerXLabelText, out _playerXScoreText);

            // Player O / AI Card
            _playerOCardBg = BuildPlayerPill(_hudCardRT, "PlayerOPill", XOProceduralAssets.OPieceSprite, new Vector2(360f, -12f), XOProceduralAssets.Hex("#FFE4E6"), out _playerOLabelText, out _playerOScoreText);

            // Rule / Mode Quick Switch Button
            _modeQuickBtn = XOUIBuilder.Create3DButton(_hudCardRT, _font, "Rules", new Vector2(128f, 44f), XOProceduralAssets.ColLilac, Color.white, 15);
            RectTransform mqRT = _modeQuickBtn.GetComponent<RectTransform>();
            mqRT.anchorMin = new Vector2(1f, 1f);
            mqRT.anchorMax = new Vector2(1f, 1f);
            mqRT.pivot = new Vector2(1f, 1f);
            mqRT.anchoredPosition = new Vector2(-14f, -14f);
            _modeQuickBtnText = _modeQuickBtn.GetComponentInChildren<Text>();
            _modeQuickBtn.onClick.AddListener(() => _onModeQuickClicked?.Invoke());

            // Timer Progress Track
            GameObject timerTrack = XOUIBuilder.CreateUIObject("TimerTrack", _hudCardRT);
            RectTransform ttRT = timerTrack.GetComponent<RectTransform>();
            ttRT.anchorMin = new Vector2(0f, 0f);
            ttRT.anchorMax = new Vector2(1f, 0f);
            ttRT.pivot = new Vector2(0.5f, 0f);
            ttRT.offsetMin = new Vector2(18f, 34f);
            ttRT.offsetMax = new Vector2(-18f, 42f);
            Image trackImg = timerTrack.AddComponent<Image>();
            trackImg.sprite = XOProceduralAssets.PillSprite;
            trackImg.color = XOProceduralAssets.ColTileEdge;

            GameObject timerFill = XOUIBuilder.CreateUIObject("TimerFill", ttRT);
            _turnTimerFillImg = timerFill.AddComponent<Image>();
            _turnTimerFillImg.sprite = XOProceduralAssets.PillSprite;
            _turnTimerFillImg.type = Image.Type.Filled;
            _turnTimerFillImg.fillMethod = Image.FillMethod.Horizontal;
            _turnTimerFillImg.color = XOProceduralAssets.ColMint;

            _turnRuleText = XOUIBuilder.CreateText(_hudCardRT, _font, "Your Turn (X) • Connect 5 to Win", 15, FontStyle.Bold, TextAnchor.LowerCenter);
            _turnRuleText.rectTransform.offsetMin = new Vector2(16f, 6f);
            _turnRuleText.rectTransform.offsetMax = new Vector2(-16f, 0f);
        }

        private Image BuildPlayerPill(RectTransform parent, string name, Sprite icon, Vector2 anchoredTopLeft, Color bgCol, out Text labelTxt, out Text scoreTxt)
        {
            GameObject obj = XOUIBuilder.CreateUIObject(name, parent);
            RectTransform rt = obj.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = anchoredTopLeft;
            rt.sizeDelta = new Vector2(246f, 48f);

            Image bg = obj.AddComponent<Image>();
            bg.sprite = XOProceduralAssets.PillSprite;
            bg.type = Image.Type.Sliced;
            bg.color = bgCol;

            XOUIBuilder.CreateIconImage(rt, icon, new Vector2(0f, 0.5f), new Vector2(22f, 0f), new Vector2(28f, 28f));

            labelTxt = XOUIBuilder.CreateText(rt, _font, "YOU (X)", 14, FontStyle.Bold, TextAnchor.MiddleLeft);
            labelTxt.rectTransform.offsetMin = new Vector2(44f, 2f);
            labelTxt.rectTransform.offsetMax = new Vector2(-46f, -2f);

            scoreTxt = XOUIBuilder.CreateText(rt, _font, "0", 20, FontStyle.Bold, TextAnchor.MiddleRight);
            scoreTxt.rectTransform.offsetMin = new Vector2(0f, 2f);
            scoreTxt.rectTransform.offsetMax = new Vector2(-14f, -2f);
            return bg;
        }

        private void BuildRelicBar()
        {
            _relicBarObj = XOUIBuilder.CreateUIObject("BattleRelicBar", RootRT);
            _relicBarRT = _relicBarObj.GetComponent<RectTransform>();
            _relicBarRT.anchorMin = new Vector2(0.5f, 0.5f);
            _relicBarRT.anchorMax = new Vector2(0.5f, 0.5f);
            _relicBarRT.pivot = new Vector2(0.5f, 0.5f);

            Image bg = _relicBarObj.AddComponent<Image>();
            bg.sprite = XOProceduralAssets.CardPanelSprite;
            bg.type = Image.Type.Sliced;
            bg.color = XOProceduralAssets.Hex("#FFFBEB");

            HorizontalLayoutGroup hlg = _relicBarObj.AddComponent<HorizontalLayoutGroup>();
            hlg.padding = new RectOffset(12, 12, 6, 6);
            hlg.spacing = 10f;
            hlg.childControlWidth = false;
            hlg.childControlHeight = false;
            hlg.childAlignment = TextAnchor.MiddleCenter;

            _relicIconsContainer = _relicBarObj.transform;
            _relicBarObj.SetActive(false);
        }

        private void BuildBoardFrame()
        {
            GameObject frameObj = XOUIBuilder.CreateUIObject("BoardOuterFrame", RootRT);
            BoardOuterFrameRT = frameObj.GetComponent<RectTransform>();
            BoardOuterFrameRT.anchorMin = new Vector2(0.5f, 0.5f);
            BoardOuterFrameRT.anchorMax = new Vector2(0.5f, 0.5f);
            BoardOuterFrameRT.pivot = new Vector2(0.5f, 0.5f);
            BoardOuterFrameRT.sizeDelta = new Vector2(680f, 680f);

            BoardOuterFrameImg = frameObj.AddComponent<Image>();
            BoardOuterFrameImg.sprite = XOProceduralAssets.CardPanelSprite;
            BoardOuterFrameImg.type = Image.Type.Sliced;
            BoardOuterFrameImg.color = _store.ActiveTheme.boardFrameColor.ToUnityColor();

            GameObject gridObj = XOUIBuilder.CreateUIObject("BoardGridContainer", BoardOuterFrameRT);
            BoardGridContainerRT = gridObj.GetComponent<RectTransform>();
            BoardGridContainerRT.offsetMin = new Vector2(14f, 14f);
            BoardGridContainerRT.offsetMax = new Vector2(-14f, -14f);

            _boardGridLayout = gridObj.AddComponent<GridLayoutGroup>();
            _boardGridLayout.childAlignment = TextAnchor.MiddleCenter;
        }

        private void BuildStatusBanner()
        {
            GameObject bannerObj = XOUIBuilder.CreateUIObject("StatusBanner", RootRT);
            _statusBannerRT = bannerObj.GetComponent<RectTransform>();
            _statusBannerRT.anchorMin = new Vector2(0.5f, 0.5f);
            _statusBannerRT.anchorMax = new Vector2(0.5f, 0.5f);
            _statusBannerRT.pivot = new Vector2(0.5f, 0.5f);

            StatusBannerImg = bannerObj.AddComponent<Image>();
            StatusBannerImg.sprite = XOProceduralAssets.PillSprite;
            StatusBannerImg.type = Image.Type.Sliced;
            StatusBannerImg.color = _store.ActiveTheme.panelSurfaceColor.ToUnityColor();

            StatusBannerText = XOUIBuilder.CreateText(_statusBannerRT, _font, "Your move! Tap any square to place X.", 17, FontStyle.Bold, TextAnchor.MiddleCenter);
            StatusBannerText.color = _store.ActiveTheme.primaryTextColor.ToUnityColor();
            StatusBannerText.rectTransform.offsetMin = new Vector2(16f, 4f);
            StatusBannerText.rectTransform.offsetMax = new Vector2(-16f, -4f);
        }

        private void BuildActionDeck()
        {
            GameObject deckObj = XOUIBuilder.CreateUIObject("TacticalActionDeck", RootRT);
            _actionsDeckRT = deckObj.GetComponent<RectTransform>();
            _actionsDeckRT.anchorMin = new Vector2(0.5f, 0.5f);
            _actionsDeckRT.anchorMax = new Vector2(0.5f, 0.5f);
            _actionsDeckRT.pivot = new Vector2(0.5f, 0.5f);

            ActionsCardImg = deckObj.AddComponent<Image>();
            ActionsCardImg.sprite = XOProceduralAssets.CardPanelSprite;
            ActionsCardImg.type = Image.Type.Sliced;
            ActionsCardImg.color = _store.ActiveTheme.panelSurfaceColor.ToUnityColor();

            GridLayoutGroup glg = deckObj.AddComponent<GridLayoutGroup>();
            glg.padding = new RectOffset(14, 14, 14, 14);
            glg.spacing = new Vector2(12f, 12f);
            glg.cellSize = new Vector2(156f, 74f);
            glg.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            glg.constraintCount = 3;
            glg.childAlignment = TextAnchor.MiddleCenter;

            _hintBadgeText = BuildSkillTile(XOProceduralAssets.IconHint, "Hint", XOProceduralAssets.Hex("#FCD34D"), XOProceduralAssets.ColInk, _onHintClicked, hasBadge: true);
            _undoBadgeText = BuildSkillTile(XOProceduralAssets.IconUndo, "Undo", XOProceduralAssets.ColSky, Color.white, _onUndoClicked, hasBadge: true);
            _bombBadgeText = BuildSkillTile(XOProceduralAssets.IconBomb, "Bomb", XOProceduralAssets.ColCoral, Color.white, _onBombClicked, hasBadge: true);
            _freezeBadgeText = BuildSkillTile(XOProceduralAssets.IconFreeze, "Freeze", XOProceduralAssets.Hex("#38BDF8"), Color.white, _onFreezeClicked, hasBadge: true);
            _swapBadgeText = BuildSkillTile(XOProceduralAssets.IconSwap, "Swap", XOProceduralAssets.ColLilac, Color.white, _onSwapClicked, hasBadge: true);
            BuildSkillTile(XOProceduralAssets.IconNewGame, "Reset", XOProceduralAssets.ColMint, XOProceduralAssets.ColInk, _onResetClicked, hasBadge: false);
        }

        private Text BuildSkillTile(Sprite icon, string label, Color bgCol, Color txtCol, Action onClick, bool hasBadge)
        {
            Button btn = XOUIBuilder.Create3DButton(_actionsDeckRT, _font, "", new Vector2(156f, 74f), bgCol, txtCol, 15);
            RectTransform rt = btn.GetComponent<RectTransform>();
            btn.onClick.AddListener(() => onClick?.Invoke());

            XOUIBuilder.CreateIconImage(rt, icon, new Vector2(0.5f, 0.5f), new Vector2(0f, 10f), new Vector2(36f, 36f));

            Text lbl = XOUIBuilder.CreateText(rt, _font, label, 15, FontStyle.Bold, TextAnchor.LowerCenter);
            lbl.color = txtCol;
            lbl.rectTransform.offsetMin = new Vector2(4f, 6f);
            lbl.rectTransform.offsetMax = new Vector2(-4f, 0f);

            if (!hasBadge) return null;

            GameObject badgeObj = XOUIBuilder.CreateUIObject("StockBadge", rt);
            RectTransform bRT = badgeObj.GetComponent<RectTransform>();
            bRT.anchorMin = new Vector2(1f, 1f);
            bRT.anchorMax = new Vector2(1f, 1f);
            bRT.pivot = new Vector2(1f, 1f);
            bRT.anchoredPosition = new Vector2(-6f, -6f);
            bRT.sizeDelta = new Vector2(28f, 24f);

            Image bBg = badgeObj.AddComponent<Image>();
            bBg.sprite = XOProceduralAssets.PillSprite;
            bBg.type = Image.Type.Sliced;
            bBg.color = XOProceduralAssets.ColInk;
            bBg.raycastTarget = false;

            Text bTxt = XOUIBuilder.CreateText(bRT, _font, "1", 13, FontStyle.Bold, TextAnchor.MiddleCenter);
            bTxt.color = Color.white;
            return bTxt;
        }

        /// <summary>Rebuilds board cell GameObjects only when grid dimensions change.</summary>
        public void RebuildBoardCellsIfNeeded(int size)
        {
            _currentGridSize = size;
            int targetCount = size * size;
            if (_cellViews.Count != targetCount)
            {
                foreach (var cv in _cellViews)
                {
                    if (cv != null) UnityEngine.Object.Destroy(cv.gameObject);
                }
                _cellViews.Clear();

                for (int r = 0; r < size; r++)
                {
                    for (int c = 0; c < size; c++)
                    {
                        GameObject cellObj = new GameObject($"Cell_{r}_{c}", typeof(RectTransform), typeof(Image), typeof(Button), typeof(XOCellView));
                        cellObj.transform.SetParent(BoardGridContainerRT, false);

                        Image cellImg = cellObj.GetComponent<Image>();
                        cellImg.sprite = XOProceduralAssets.SoftCellSprite;
                        cellImg.type = Image.Type.Sliced;

                        XOCellView view = cellObj.GetComponent<XOCellView>();
                        view.Initialize(r, c, _font, (row, col) => _onCellClicked?.Invoke(row, col));
                        _cellViews.Add(view);
                    }
                }
            }

            ResizeBoardGridCells(size);
        }

        /// <summary>Computes cell square sizes dynamically from <see cref="BoardOuterFrameRT"/>.</summary>
        public void ResizeBoardGridCells(int size)
        {
            if (_boardGridLayout == null || BoardOuterFrameRT == null) return;
            int s = Mathf.Max(1, size);
            float boardPixelWidth = BoardOuterFrameRT.sizeDelta.x - 28f;
            float spacing = (s <= 10) ? 5f : 3.5f;
            float totalSpacing = spacing * (s - 1);
            float cellSize = Mathf.Floor((boardPixelWidth - totalSpacing) / s);

            _boardGridLayout.spacing = new Vector2(spacing, spacing);
            _boardGridLayout.cellSize = new Vector2(cellSize, cellSize);
            _boardGridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            _boardGridLayout.constraintCount = s;
        }

        /// <summary>Returns the cell view at (row, col) if in range.</summary>
        public XOCellView GetCellView(int row, int col)
        {
            int idx = row * _currentGridSize + col;
            if (idx < 0 || idx >= _cellViews.Count) return null;
            return _cellViews[idx];
        }

        /// <summary>Updates every board tile visual from the engine state.</summary>
        public void RefreshAllCells(bool animateLastMove)
        {
            int size = _engine.BoardSize;
            MoveRecord? lastMove = _engine.GetLastMove();
            XOCellPos hint = _store.ActiveHintCell;
            ActiveSkillTargeting targeting = _store.SkillTargeting;
            XOThemePalette theme = _store.ActiveTheme;
            XOPieceSkin skin = _store.ActivePieceSkin;

            bool isBombTarget = targeting == ActiveSkillTargeting.BombDestroy;
            bool isSwapTarget = targeting == ActiveSkillTargeting.SwapPiece;

            for (int r = 0; r < size; r++)
            {
                for (int c = 0; c < size; c++)
                {
                    XOCellView view = GetCellView(r, c);
                    if (view == null) continue;

                    CellPiece piece = _engine.Board[r, c];
                    bool isLast = lastMove.HasValue && lastMove.Value.row == r && lastMove.Value.col == c;
                    bool isWin = _engine.WinningCells.Contains(new XOCellPos(r, c));
                    bool isHint = hint.IsValid && hint.x == r && hint.y == c;
                    bool isAboutToVanish = _engine.IsPieceAboutToVanish(r, c);

                    view.SetState(
                        piece,
                        _engine.SpecialTiles[r, c],
                        _engine.FrozenTurns[r, c],
                        theme,
                        skin,
                        isLast,
                        isWin,
                        isHint,
                        isBombTarget,
                        isSwapTarget,
                        isAboutToVanish,
                        _engine.PieceSizes[r, c],
                        animateLastMove && isLast);
                }
            }
        }

        /// <summary>Refreshes player/AI turn labels, scores, mode pill, and timer fill bar.</summary>
        public void RefreshTurnAndScore(GameMode mode, float turnTimeLimit)
        {
            if (_playerXLabelText != null) _playerXLabelText.text = (mode == GameMode.TwoPlayerLocal) ? "PLAYER 1 (X)" : "YOU (X)";

            if (_playerOLabelText != null)
            {
                if (mode == GameMode.VsAI)
                {
                    XOBotProfile bot = XOProgressionSystem.GetActiveBot();
                    _playerOLabelText.text = $"{bot.botName.ToUpper()} [{bot.eloRating}]";
                }
                else if (mode == GameMode.TowerClimb)
                {
                    TowerFloorBlind blind = XORoguelikeRunManager.GetActiveBlind();
                    _playerOLabelText.text = $"{blind.bossName.ToUpper()} [{blind.bossElo}]";
                }
                else if (mode == GameMode.PuzzleStage)
                {
                    _playerOLabelText.text = $"PUZZLE #{XOProgressionSystem.CurrentPuzzleStage}";
                }
                else
                {
                    _playerOLabelText.text = "PLAYER 2 (O)";
                }
            }

            RefreshScoresOnly();

            bool isXTurn = _engine.CurrentTurn == CellPiece.X;
            if (_playerXCardBg != null) _playerXCardBg.color = isXTurn ? XOProceduralAssets.Hex("#BAE6FD") : XOProceduralAssets.Hex("#F1F5F9");
            if (_playerOCardBg != null) _playerOCardBg.color = !isXTurn ? XOProceduralAssets.Hex("#FECDD3") : XOProceduralAssets.Hex("#F1F5F9");

            UpdateTimerVisual(turnTimeLimit);

            if (_modeQuickBtnText != null)
            {
                _modeQuickBtnText.text = $"{_engine.BoardSize}x{_engine.BoardSize} • Win {_engine.WinLength}";
            }

            if (_turnRuleText != null)
            {
                string turnOwner = (_store.IsAIThinking)
                    ? "Opponent is thinking..."
                    : (isXTurn ? "YOUR TURN (X)" : "OPPONENT TURN (O)");

                if (mode == GameMode.PuzzleStage && _store.CurrentPuzzle != null)
                {
                    _turnRuleText.text = $"{_store.CurrentPuzzle.title}  •  Moves Left: {_store.PuzzleMovesLeft}";
                }
                else if (mode == GameMode.TowerClimb)
                {
                    TowerFloorBlind blind = XORoguelikeRunManager.GetActiveBlind();
                    _turnRuleText.text = $"Floor {blind.floorNumber}: {blind.blindName}  •  {turnOwner}";
                }
                else
                {
                    _turnRuleText.text = $"{turnOwner}  •  Connect {_engine.WinLength} to win!";
                }
            }
        }

        public void RefreshScoresOnly()
        {
            if (_playerXScoreText != null) _playerXScoreText.text = _store.ScoreX.ToString();
            if (_playerOScoreText != null) _playerOScoreText.text = _store.ScoreO.ToString();
        }

        public void UpdateTimerVisual(float turnTimeLimit)
        {
            if (_turnTimerFillImg == null) return;
            float ratio = (turnTimeLimit > 0.01f) ? Mathf.Clamp01(_store.TurnTimerRemaining / turnTimeLimit) : 1f;
            _turnTimerFillImg.fillAmount = ratio;
            _turnTimerFillImg.color = (ratio < 0.28f) ? XOProceduralAssets.ColCoral : XOProceduralAssets.ColMint;
        }

        /// <summary>Updates tactical stock count badges on Hint/Undo/Bomb/Freeze/Swap buttons.</summary>
        public void RefreshBottomActionBadges()
        {
            SetBadge(_hintBadgeText, _store.HintStock);
            SetBadge(_undoBadgeText, _store.UndoStock);
            SetBadge(_bombBadgeText, _store.BombStock);
            SetBadge(_freezeBadgeText, _store.FreezeStock);
            SetBadge(_swapBadgeText, _store.SwapStock);
        }

        private static void SetBadge(Text badgeText, int stock)
        {
            if (badgeText != null) badgeText.text = (stock > 0) ? stock.ToString() : "AD";
        }

        /// <summary>Rebuilds equipped Roguelike Relic badges shown above the board in Tower mode.</summary>
        public void RefreshBattleRelicBar(GameMode mode)
        {
            if (_relicBarObj == null || _relicIconsContainer == null) return;
            bool show = mode == GameMode.TowerClimb && XORoguelikeRunManager.EquippedRelics.Count > 0;
            _relicBarObj.SetActive(show);
            if (!show) return;

            for (int i = _relicIconsContainer.childCount - 1; i >= 0; i--)
            {
                UnityEngine.Object.Destroy(_relicIconsContainer.GetChild(i).gameObject);
            }

            foreach (CaroRelicType relicType in XORoguelikeRunManager.EquippedRelics)
            {
                CaroRelic relic = XORoguelikeRunManager.GetRelicData(relicType);
                if (relic == null) continue;

                GameObject badge = XOUIBuilder.CreateUIObject("RelicChip_" + relic.id, _relicIconsContainer);
                RectTransform bRT = badge.GetComponent<RectTransform>();
                bRT.sizeDelta = new Vector2(118f, 40f);
                Image bg = badge.AddComponent<Image>();
                bg.sprite = XOProceduralAssets.PillSprite;
                bg.type = Image.Type.Sliced;
                bg.color = Color.Lerp(relic.GetRarityColor(), Color.white, 0.72f);

                XOUIBuilder.CreateIconImage(bRT, relic.GetIcon(), new Vector2(0f, 0.5f), new Vector2(18f, 0f), new Vector2(24f, 24f));
                Text nameTxt = XOUIBuilder.CreateText(bRT, _font, relic.name, 12, FontStyle.Bold, TextAnchor.MiddleLeft);
                nameTxt.rectTransform.offsetMin = new Vector2(34f, 2f);
                nameTxt.rectTransform.offsetMax = new Vector2(-6f, -2f);
            }
        }

        public void ApplyTheme(XOThemePalette theme)
        {
            if (BoardOuterFrameImg != null) BoardOuterFrameImg.color = theme.boardFrameColor.ToUnityColor();
            if (StatusBannerImg != null) StatusBannerImg.color = theme.panelSurfaceColor.ToUnityColor();
            if (StatusBannerText != null) StatusBannerText.color = theme.primaryTextColor.ToUnityColor();
            if (ActionsCardImg != null) ActionsCardImg.color = theme.panelSurfaceColor.ToUnityColor();
        }

        /// <summary>Positions Battle HUD, Square Board Frame, Status Banner, and Action Deck for current screen aspect.</summary>
        public void ApplyResponsiveLayout(float refW, float refH, bool isLandscape, int boardSize)
        {
            if (RootObj == null) return;
            if (isLandscape)
            {
                float availH = refH - 130f;
                float boardPx = Mathf.Clamp(Mathf.Min(availH - 36f, refW * 0.46f), 460f, 780f);
                float leftCenter = -Mathf.Min(340f, (refW - boardPx) * 0.25f);
                float rightCenter = boardPx * 0.5f + 275f + leftCenter * 0.35f;
                float boardX = Mathf.Min(-240f, -refW * 0.14f);
                float rightPanelX = boardX + boardPx * 0.5f + 290f;

                if (BoardOuterFrameRT != null)
                {
                    BoardOuterFrameRT.anchoredPosition = new Vector2(boardX, -42f);
                    BoardOuterFrameRT.sizeDelta = new Vector2(boardPx, boardPx);
                }
                if (_hudCardRT != null) { _hudCardRT.anchoredPosition = new Vector2(rightPanelX, 175f); _hudCardRT.sizeDelta = new Vector2(540f, 122f); }
                if (_relicBarRT != null) { _relicBarRT.anchoredPosition = new Vector2(rightPanelX, 82f); _relicBarRT.sizeDelta = new Vector2(540f, 52f); }
                if (_statusBannerRT != null) { _statusBannerRT.anchoredPosition = new Vector2(rightPanelX, 12f); _statusBannerRT.sizeDelta = new Vector2(540f, 60f); }
                if (_actionsDeckRT != null) { _actionsDeckRT.anchoredPosition = new Vector2(rightPanelX, -145f); _actionsDeckRT.sizeDelta = new Vector2(540f, 194f); }
            }
            else
            {
                float maxBoardW = Mathf.Min(940f, refW - 64f);
                float maxBoardH = Mathf.Max(460f, refH - 640f);
                float boardPx = Mathf.Clamp(Mathf.Min(maxBoardW, maxBoardH), 460f, 920f);
                float panelW = Mathf.Min(940f, refW - 64f);

                if (_hudCardRT != null) { _hudCardRT.anchoredPosition = new Vector2(0f, boardPx * 0.5f + 135f); _hudCardRT.sizeDelta = new Vector2(panelW, 118f); }
                if (_relicBarRT != null) { _relicBarRT.anchoredPosition = new Vector2(0f, boardPx * 0.5f + 46f); _relicBarRT.sizeDelta = new Vector2(panelW, 50f); }
                if (BoardOuterFrameRT != null) { BoardOuterFrameRT.anchoredPosition = new Vector2(0f, -20f); BoardOuterFrameRT.sizeDelta = new Vector2(boardPx, boardPx); }
                if (_statusBannerRT != null) { _statusBannerRT.anchoredPosition = new Vector2(0f, -boardPx * 0.5f - 66f); _statusBannerRT.sizeDelta = new Vector2(panelW, 56f); }
                if (_actionsDeckRT != null) { _actionsDeckRT.anchoredPosition = new Vector2(0f, -boardPx * 0.5f - 200f); _actionsDeckRT.sizeDelta = new Vector2(panelW, 190f); }
            }

            ResizeBoardGridCells(boardSize);
        }
    }
}
