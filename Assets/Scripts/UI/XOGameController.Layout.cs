using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using XOPlayable.Ads;
using XOPlayable.Core;
using XOPlayable.Visuals;

namespace XOPlayable.UI
{
    public partial class XOGameController
    {
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
                float boardPadding = 48f;
                float maxBoardHeight = canvasH - 96f; // Leaves 48px top and bottom padding
                float maxBoardWidth = (canvasW * 0.52f) - (boardPadding * 1.5f);
                float boardSize = Mathf.Clamp(Mathf.Min(maxBoardHeight, maxBoardWidth), 560f, 860f);

                float boardCenterX = -(canvasW * 0.5f) + (boardSize * 0.5f) + boardPadding;

                if (_boardOuterFrameRT != null)
                {
                    _boardOuterFrameRT.anchorMin = new Vector2(0.5f, 0.5f);
                    _boardOuterFrameRT.anchorMax = new Vector2(0.5f, 0.5f);
                    _boardOuterFrameRT.pivot = new Vector2(0.5f, 0.5f);
                    _boardOuterFrameRT.anchoredPosition = new Vector2(boardCenterX, 0f);
                    _boardOuterFrameRT.sizeDelta = new Vector2(boardSize, boardSize);
                }

                // Sidebar is strictly positioned to the right of the board with a guaranteed 36px safe gap!
                float boardRightX = boardCenterX + (boardSize * 0.5f);
                float sidebarLeftX = boardRightX + 36f;
                float sidebarRightX = (canvasW * 0.5f) - 48f;
                float sidebarW = Mathf.Max(520f, sidebarRightX - sidebarLeftX);
                float sidebarCenterX = sidebarLeftX + (sidebarW * 0.5f);

                bool isTower = (currentGameMode == GameMode.TowerClimb);

                if (_battleTopHudRT != null)
                {
                    _battleTopHudRT.anchorMin = new Vector2(0.5f, 0.5f);
                    _battleTopHudRT.anchorMax = new Vector2(0.5f, 0.5f);
                    _battleTopHudRT.pivot = new Vector2(0.5f, 0.5f);
                    float topHudY = isTower ? 340f : 260f;
                    _battleTopHudRT.anchoredPosition = new Vector2(sidebarCenterX, topHudY);
                    _battleTopHudRT.sizeDelta = new Vector2(sidebarW, 160f);
                }
                if (_battleRelicBarRT != null)
                {
                    _battleRelicBarRT.anchorMin = new Vector2(0.5f, 0.5f);
                    _battleRelicBarRT.anchorMax = new Vector2(0.5f, 0.5f);
                    _battleRelicBarRT.pivot = new Vector2(0.5f, 0.5f);
                    _battleRelicBarRT.anchoredPosition = new Vector2(sidebarCenterX, 160f);
                    _battleRelicBarRT.sizeDelta = new Vector2(sidebarW, 90f);
                }
                if (_battleBottomBarRT != null)
                {
                    _battleBottomBarRT.anchorMin = new Vector2(0.5f, 0.5f);
                    _battleBottomBarRT.anchorMax = new Vector2(0.5f, 0.5f);
                    _battleBottomBarRT.pivot = new Vector2(0.5f, 0.5f);
                    float bottomBarY = isTower ? -145f : -70f;
                    _battleBottomBarRT.anchoredPosition = new Vector2(sidebarCenterX, bottomBarY);
                    _battleBottomBarRT.sizeDelta = new Vector2(sidebarW, 260f);

                    HorizontalLayoutGroup hLayout = _battleBottomBarRT.GetComponent<HorizontalLayoutGroup>();
                    if (hLayout != null) DestroyImmediate(hLayout);

                    GridLayoutGroup gLayout = _battleBottomBarRT.GetComponent<GridLayoutGroup>();
                    if (gLayout == null) gLayout = _battleBottomBarRT.gameObject.AddComponent<GridLayoutGroup>();
                    gLayout.childAlignment = TextAnchor.MiddleCenter;
                    float cellW = Mathf.Floor((sidebarW - 32f) / 3f);
                    gLayout.cellSize = new Vector2(cellW, 118f);
                    gLayout.spacing = new Vector2(16f, 14f);
                    gLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
                    gLayout.constraintCount = 3;

                    Vector2 skillBtnSize = new Vector2(cellW, 118f);
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
                    bool isTowerP = (currentGameMode == GameMode.TowerClimb);
                    // Dynamically calculate available height so board never collides with HUD or bottom bar
                    float availBoardH = canvasH - 128f - 116f - (isTowerP ? 130f : 80f);
                    float boardSize = Mathf.Clamp(Mathf.Min(canvasW - 48f, availBoardH), 540f, 1020f);
                    _boardOuterFrameRT.anchorMin = new Vector2(0.5f, 0.5f);
                    _boardOuterFrameRT.anchorMax = new Vector2(0.5f, 0.5f);
                    _boardOuterFrameRT.pivot = new Vector2(0.5f, 0.5f);
                    _boardOuterFrameRT.anchoredPosition = new Vector2(0f, isTowerP ? -28f : -4f);
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
    }
}
