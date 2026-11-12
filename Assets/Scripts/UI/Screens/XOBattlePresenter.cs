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

        // Everything lives in a nominal-size frame (1080x1920 portrait, 1920x1080 landscape) placed with top-left
        // pixel coordinates and scaled to fit the viewport, like the lobby.
        private RectTransform _frameRT;
        private Vector2 _frameSize = new Vector2(1080f, 1920f);
        private Image _turnBannerImg;
        private RectTransform _backBtnRT;
        private RectTransform _playerXRT;
        private RectTransform _playerORT;
        private RectTransform _rulesChipRT;
        private RectTransform _turnBannerRT;
        private RectTransform _relicBarRT;
        private RectTransform _statusBannerRT;
        private readonly RectTransform[] _skillRTs = new RectTransform[6];
        private int _skillCount;
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

            GameObject frame = XOUIBuilder.CreateUIObject("BattleFrame", RootRT);
            _frameRT = frame.GetComponent<RectTransform>();
            _frameRT.SetAnchor(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
            _frameRT.sizeDelta = _frameSize;

            BuildTopBar();
            BuildTurnBanner();
            BuildRelicBar();
            BuildBoardFrame();
            BuildStatusBanner();
            BuildSkillRow();
        }

        private void BuildTopBar()
        {
            Button homeBtn = XOUIBuilder.Create3DButton(_frameRT, _font, "", new Vector2(76f, 76f), Color.white, XOProceduralAssets.ColInk, 16);
            homeBtn.name = "BackButton";
            _backBtnRT = homeBtn.GetComponent<RectTransform>();
            XOUIBuilder.CreateIconImage(_backBtnRT, XOProceduralAssets.IconHome, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(44f, 44f));
            homeBtn.onClick.AddListener(() => _onHomeClicked?.Invoke());

            _playerXCardBg = BuildPlayerCard("PlayerXCard", XOProceduralAssets.XPieceSprite, out _playerXRT, out _playerXLabelText, out _playerXScoreText);
            _playerOCardBg = BuildPlayerCard("PlayerOCard", XOProceduralAssets.OPieceSprite, out _playerORT, out _playerOLabelText, out _playerOScoreText);

            _modeQuickBtn = XOUIBuilder.Create3DButton(_frameRT, _font, "Rules", new Vector2(100f, 76f), XOUIColors.Violet200, XOUIColors.Violet800, 22);
            _modeQuickBtn.name = "RulesChip";
            _rulesChipRT = _modeQuickBtn.GetComponent<RectTransform>();
            _modeQuickBtnText = _modeQuickBtn.GetComponentInChildren<Text>();
            _modeQuickBtn.onClick.AddListener(() => _onModeQuickClicked?.Invoke());
        }

        private Image BuildPlayerCard(string name, Sprite icon, out RectTransform rt, out Text labelTxt, out Text scoreTxt)
        {
            GameObject obj = XOUIBuilder.CreateUIObject(name, _frameRT);
            rt = obj.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(400f, 104f);

            Image bg = obj.AddComponent<Image>();
            bg.sprite = XOProceduralAssets.CardPanelSprite;
            bg.type = Image.Type.Sliced;
            bg.color = Color.white;
            bg.raycastTarget = false;

            XOUIBuilder.CreateIconImage(rt, icon, new Vector2(0f, 0.5f), new Vector2(56f, 0f), new Vector2(62f, 62f));

            labelTxt = XOUIBuilder.CreateText(rt, _font, "You (X)", 20, FontStyle.Bold, TextAnchor.MiddleLeft);
            labelTxt.resizeTextForBestFit = true;
            labelTxt.resizeTextMinSize = 12;
            labelTxt.resizeTextMaxSize = 20;
            labelTxt.rectTransform.offsetMin = new Vector2(100f, 6f);
            labelTxt.rectTransform.offsetMax = new Vector2(-84f, -6f);

            scoreTxt = XOUIBuilder.CreateText(rt, _font, "0", 48, FontStyle.Bold, TextAnchor.MiddleRight);
            scoreTxt.rectTransform.offsetMin = new Vector2(0f, 4f);
            scoreTxt.rectTransform.offsetMax = new Vector2(-24f, -4f);
            return bg;
        }

        private void BuildTurnBanner()
        {
            GameObject obj = XOUIBuilder.CreateUIObject("TurnBanner", _frameRT);
            _turnBannerRT = obj.GetComponent<RectTransform>();
            _turnBannerRT.sizeDelta = new Vector2(960f, 64f);

            _turnBannerImg = obj.AddComponent<Image>();
            _turnBannerImg.sprite = XOProceduralAssets.PillSprite;
            _turnBannerImg.type = Image.Type.Sliced;
            _turnBannerImg.color = _store.ActiveTheme.panelSurfaceColor.ToUnityColor();
            _turnBannerImg.raycastTarget = false;

            // Timer bar sits inside the bottom edge of the banner.
            GameObject timerTrack = XOUIBuilder.CreateUIObject("TimerTrack", _turnBannerRT);
            RectTransform ttRT = timerTrack.GetComponent<RectTransform>();
            ttRT.anchorMin = new Vector2(0f, 0f);
            ttRT.anchorMax = new Vector2(1f, 0f);
            ttRT.pivot = new Vector2(0.5f, 0f);
            ttRT.offsetMin = new Vector2(32f, 8f);
            ttRT.offsetMax = new Vector2(-32f, 18f);
            Image trackImg = timerTrack.AddComponent<Image>();
            trackImg.sprite = XOProceduralAssets.PillSprite;
            trackImg.color = XOProceduralAssets.ColTileEdge;
            trackImg.raycastTarget = false;

            GameObject timerFill = XOUIBuilder.CreateUIObject("TimerFill", ttRT);
            _turnTimerFillImg = timerFill.AddComponent<Image>();
            _turnTimerFillImg.sprite = XOProceduralAssets.PillSprite;
            _turnTimerFillImg.type = Image.Type.Filled;
            _turnTimerFillImg.fillMethod = Image.FillMethod.Horizontal;
            _turnTimerFillImg.color = XOProceduralAssets.ColMint;
            _turnTimerFillImg.raycastTarget = false;

            _turnRuleText = XOUIBuilder.CreateText(_turnBannerRT, _font, "Your turn · Connect 5 to win", 26, FontStyle.Bold, TextAnchor.MiddleCenter);
            _turnRuleText.rectTransform.offsetMin = new Vector2(24f, 16f);
            _turnRuleText.rectTransform.offsetMax = new Vector2(-24f, -2f);
        }

        private void BuildRelicBar()
        {
            _relicBarObj = XOUIBuilder.CreateUIObject("BattleRelicBar", _frameRT);
            _relicBarRT = _relicBarObj.GetComponent<RectTransform>();
            _relicBarRT.SetAnchor(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));

            Image bg = _relicBarObj.AddComponent<Image>();
            bg.sprite = XOProceduralAssets.CardPanelSprite;
            bg.type = Image.Type.Sliced;
            bg.color = XOUIColors.Amber50;
            bg.raycastTarget = false;

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
            GameObject frameObj = XOUIBuilder.CreateUIObject("BoardOuterFrame", _frameRT);
            BoardOuterFrameRT = frameObj.GetComponent<RectTransform>();
            BoardOuterFrameRT.SetAnchor(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
            BoardOuterFrameRT.sizeDelta = new Vector2(920f, 920f);

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
            GameObject bannerObj = XOUIBuilder.CreateUIObject("StatusBanner", _frameRT);
            _statusBannerRT = bannerObj.GetComponent<RectTransform>();
            _statusBannerRT.SetAnchor(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));

            StatusBannerImg = bannerObj.AddComponent<Image>();
            StatusBannerImg.sprite = XOProceduralAssets.PillSprite;
            StatusBannerImg.type = Image.Type.Sliced;
            StatusBannerImg.color = _store.ActiveTheme.panelSurfaceColor.ToUnityColor();
            StatusBannerImg.raycastTarget = false;

            StatusBannerText = XOUIBuilder.CreateText(_statusBannerRT, _font, "Your move! Tap any square to place X.", 24, FontStyle.Bold, TextAnchor.MiddleCenter);
            StatusBannerText.color = _store.ActiveTheme.primaryTextColor.ToUnityColor();
            StatusBannerText.rectTransform.offsetMin = new Vector2(20f, 4f);
            StatusBannerText.rectTransform.offsetMax = new Vector2(-20f, -4f);
        }

        private void BuildSkillRow()
        {
            _skillCount = 0;
            _hintBadgeText = BuildSkillTile(XOProceduralAssets.IconHint, "Hint", XOProceduralAssets.ColSun, XOProceduralAssets.ColInk, _onHintClicked, hasBadge: true);
            _undoBadgeText = BuildSkillTile(XOProceduralAssets.IconUndo, "Undo", XOProceduralAssets.ColSky, Color.white, _onUndoClicked, hasBadge: true);
            _bombBadgeText = BuildSkillTile(XOProceduralAssets.IconBomb, "Bomb", XOProceduralAssets.ColCoral, Color.white, _onBombClicked, hasBadge: true);
            _freezeBadgeText = BuildSkillTile(XOProceduralAssets.IconFreeze, "Freeze", XOUIColors.Sky400, Color.white, _onFreezeClicked, hasBadge: true);
            _swapBadgeText = BuildSkillTile(XOProceduralAssets.IconSwap, "Swap", XOProceduralAssets.ColLilac, Color.white, _onSwapClicked, hasBadge: true);
            BuildSkillTile(XOProceduralAssets.IconNewGame, "Reset", XOProceduralAssets.ColMint, XOProceduralAssets.ColInk, _onResetClicked, hasBadge: false);
        }

        private Text BuildSkillTile(Sprite icon, string label, Color bgCol, Color txtCol, Action onClick, bool hasBadge)
        {
            Button btn = XOUIBuilder.Create3DButton(_frameRT, _font, label, new Vector2(144f, 144f), bgCol, txtCol, 24);
            btn.name = "Skill_" + label;
            RectTransform rt = btn.GetComponent<RectTransform>();
            btn.onClick.AddListener(() => onClick?.Invoke());
            _skillRTs[_skillCount++] = rt;

            XOUIBuilder.CreateIconImage(rt, icon, new Vector2(0.5f, 0.62f), Vector2.zero, new Vector2(66f, 66f)).name = "SkillIcon";

            Text lbl = btn.GetComponentInChildren<Text>();
            lbl.rectTransform.anchorMin = new Vector2(0f, 0f);
            lbl.rectTransform.anchorMax = new Vector2(1f, 0.30f);
            lbl.rectTransform.offsetMin = new Vector2(4f, 8f);
            lbl.rectTransform.offsetMax = new Vector2(-4f, 0f);

            if (!hasBadge) return null;

            GameObject badgeObj = XOUIBuilder.CreateUIObject("StockBadge", rt);
            RectTransform bRT = badgeObj.GetComponent<RectTransform>();
            bRT.SetAnchor(new Vector2(1f, 1f), new Vector2(1f, 1f));
            bRT.anchoredPosition = new Vector2(-14f, -14f);
            bRT.sizeDelta = new Vector2(44f, 34f);

            Image bBg = badgeObj.AddComponent<Image>();
            bBg.sprite = XOProceduralAssets.PillSprite;
            bBg.type = Image.Type.Sliced;
            bBg.color = XOProceduralAssets.ColInk;
            bBg.raycastTarget = false;

            Text bTxt = XOUIBuilder.CreateText(bRT, _font, "1", 20, FontStyle.Bold, TextAnchor.MiddleCenter);
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

            CellPiece targetOpponent = (_engine.CurrentTurn == CellPiece.O) ? CellPiece.X : CellPiece.O;

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
                    bool isBombTarget = targeting == ActiveSkillTargeting.BombDestroy && piece == targetOpponent;
                    bool isSwapTarget = targeting == ActiveSkillTargeting.SwapPiece && piece == targetOpponent;

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
            if (_playerXLabelText != null) _playerXLabelText.text = (mode == GameMode.TwoPlayerLocal) ? "Player 1 (X)" : "You (X)";

            if (_playerOLabelText != null)
            {
                if (mode == GameMode.VsAI)
                {
                    XOBotProfile bot = XOProgressionSystem.GetActiveBot();
                    _playerOLabelText.text = $"{bot.botName} · {bot.eloRating}";
                }
                else if (mode == GameMode.TowerClimb)
                {
                    TowerFloorBlind blind = XORoguelikeRunManager.GetActiveBlind();
                    BossOmen omen = XORoguelikeRunManager.GetActiveOmen();
                    _playerOLabelText.text = omen == BossOmen.None
                        ? $"{blind.bossName} · {blind.bossElo}"
                        : $"{blind.bossName} · {blind.bossElo} · {XOBossOmens.Name(omen)}";
                }
                else if (mode == GameMode.PuzzleStage)
                {
                    _playerOLabelText.text = $"Puzzle #{XOProgressionSystem.CurrentPuzzleStage}";
                }
                else
                {
                    _playerOLabelText.text = "Player 2 (O)";
                }
            }

            RefreshScoresOnly();

            bool isXTurn = _engine.CurrentTurn == CellPiece.X;
            if (_playerXCardBg != null) _playerXCardBg.color = isXTurn ? XOUIColors.Sky200 : Color.white;
            if (_playerOCardBg != null) _playerOCardBg.color = !isXTurn ? XOUIColors.Rose200 : Color.white;

            UpdateTimerVisual(turnTimeLimit);

            if (_modeQuickBtnText != null)
            {
                _modeQuickBtnText.text = $"{_engine.BoardSize}×{_engine.BoardSize}\nWin {_engine.WinLength}";
            }

            if (_turnRuleText != null)
            {
                string turnOwner = (_store.IsAIThinking)
                    ? "Opponent is thinking…"
                    : (isXTurn ? "Your turn (X)" : "Opponent turn (O)");

                if (mode == GameMode.PuzzleStage && _store.CurrentPuzzle != null)
                {
                    _turnRuleText.text = $"{_store.CurrentPuzzle.title} · {_store.PuzzleMovesLeft} moves left";
                }
                else if (mode == GameMode.TowerClimb)
                {
                    TowerFloorBlind blind = XORoguelikeRunManager.GetActiveBlind();
                    int handsLeft = XORoguelikeRunManager.GetMoveLimit(blind) - XORoguelikeRunManager.PlayerMovesThisMatch;
                    _turnRuleText.text = $"Floor {blind.floorNumber} · {Mathf.Max(0, handsLeft)} hands left · {turnOwner}";
                }
                else
                {
                    _turnRuleText.text = $"{turnOwner} · Connect {_engine.WinLength} to win";
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
            if (_turnBannerImg != null) _turnBannerImg.color = theme.panelSurfaceColor.ToUnityColor();
            if (_turnRuleText != null) _turnRuleText.color = theme.primaryTextColor.ToUnityColor();
            if (_playerXLabelText != null) _playerXLabelText.color = theme.primaryTextColor.ToUnityColor();
            if (_playerOLabelText != null) _playerOLabelText.color = theme.primaryTextColor.ToUnityColor();
            if (_playerXScoreText != null) _playerXScoreText.color = theme.xColor.ToUnityColor();
            if (_playerOScoreText != null) _playerOScoreText.color = theme.oColor.ToUnityColor();
        }

        /// <summary>Scales the nominal battle frame to the viewport and places the HUD, board, banners and skill buttons.</summary>
        public void ApplyResponsiveLayout(float refW, float refH, bool isLandscape, int boardSize, float safeLeft = 0f, float safeRight = 0f, float safeTop = 0f, float safeBottom = 0f)
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
                Place(_backBtnRT, 30f, 148f, 76f, 76f);
                Place(_playerXRT, 122f, 138f, 400f, 104f);
                Place(_playerORT, 536f, 138f, 400f, 104f);
                Place(_rulesChipRT, 950f, 148f, 100f, 76f);
                Place(_turnBannerRT, 60f, 262f, 960f, 64f);
                Place(_relicBarRT, 60f, 338f, 960f, 52f);
                Place(BoardOuterFrameRT, 80f, 420f, 920f, 920f);
                Place(_statusBannerRT, 60f, 1364f, 960f, 56f);
                const float tile = 144f, gap = 18f;
                float startX = (1080f - (6f * tile + 5f * gap)) * 0.5f;
                for (int i = 0; i < _skillCount; i++) Place(_skillRTs[i], startX + (tile + gap) * i, 1560f, tile, tile);
            }
            else
            {
                const float x0 = 1100f, w = 700f;
                Place(BoardOuterFrameRT, 120f, 130f, 880f, 880f);
                Place(_backBtnRT, x0, 138f, 76f, 76f);
                Place(_rulesChipRT, x0 + w - 100f, 138f, 100f, 76f);
                Place(_playerXRT, x0, 238f, w, 100f);
                Place(_playerORT, x0, 356f, w, 100f);
                Place(_turnBannerRT, x0, 478f, w, 64f);
                Place(_statusBannerRT, x0, 554f, w, 52f);
                Place(_relicBarRT, x0, 614f, w, 44f);
                float cellW = (w - 48f) / 3f;
                for (int i = 0; i < _skillCount; i++)
                    Place(_skillRTs[i], x0 + (cellW + 24f) * (i % 3), 672f + 180f * (i / 3), cellW, 160f);
            }

            ResizeBoardGridCells(boardSize);
        }

        /// <summary>Positions a centre-anchored rect inside the frame from top-left pixel coordinates (x, y, width, height).</summary>
        private void Place(RectTransform rt, float x, float y, float w, float h)
        {
            if (rt == null) return;
            rt.SetAnchor(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
            rt.sizeDelta = new Vector2(w, h);
            rt.anchoredPosition = new Vector2(x + w * 0.5f - _frameSize.x * 0.5f, _frameSize.y * 0.5f - (y + h * 0.5f));
        }
    }
}
