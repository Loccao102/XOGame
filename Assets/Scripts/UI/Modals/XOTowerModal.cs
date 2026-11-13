using System;
using UnityEngine;
using UnityEngine.UI;
using XOPlayable.Core;
using XOPlayable.Visuals;

namespace XOPlayable.UI
{
    /// <summary>
    /// Tower of Champions (Roguelike 10-Floor Campaign) modal with run summary, equipped relics, and per-floor Boss Blind list.
    /// </summary>
    public sealed class XOTowerModal
    {
        private readonly Font _font;
        private readonly Action<string> _playSfx;
        private readonly Action<int> _onStartFloorMatch;
        private readonly Action _onOpenCaravan;

        public GameObject RootObj { get; private set; }
        public RectTransform CardRT { get; private set; }

        private Text _runSummaryText;
        private Transform _floorListContainer;
        private ScrollRect _scroll;
        private Button _startRunBtn;
        private Button _caravanBtn;

        public XOTowerModal(
            Font font,
            Action<string> playSfx,
            Action<int> onStartFloorMatch,
            Action onOpenCaravan)
        {
            _font = font;
            _playSfx = playSfx;
            _onStartFloorMatch = onStartFloorMatch;
            _onOpenCaravan = onOpenCaravan;
        }

        /// <summary>Builds the Tower of Champions modal UI.</summary>
        public void Build(Transform canvasTransform)
        {
            RootObj = XOUIBuilder.CreateModalBackdrop("TowerModal", canvasTransform, out RectTransform rt);
            CardRT = rt;
            rt.sizeDelta = new Vector2(920f, 920f);

            XOUIBuilder.CreateModalHeader(rt, _font, XOProceduralAssets.IconCrown, "Tower of Champions", 28, () => { _playSfx?.Invoke("click"); RootObj.SetActive(false); });

            // Run Summary Strip
            GameObject summaryBar = XOUIBuilder.CreateUIObject("TowerSummaryBar", rt);
            RectTransform sbRT = summaryBar.GetComponent<RectTransform>();
            sbRT.SetAnchor(new Vector2(0.5f, 1f), new Vector2(0.5f, 1f));
            sbRT.anchoredPosition = new Vector2(0f, -94f);
            sbRT.sizeDelta = new Vector2(856f, 52f);
            Image sbBg = summaryBar.AddComponent<Image>();
            sbBg.sprite = XOProceduralAssets.PillSprite;
            sbBg.type = Image.Type.Sliced;
            sbBg.color = XOUIColors.Violet100;

            _runSummaryText = XOUIBuilder.CreateText(sbRT, _font, "", 16, FontStyle.Bold, TextAnchor.MiddleCenter);
            _runSummaryText.color = XOUIColors.Violet800;

            XOUIBuilder.CreateModalScrollView("TowerScroll", rt, out RectTransform listRT, out GameObject scrollObj);
            scrollObj.GetComponent<RectTransform>().offsetMin = new Vector2(24f, 102f);
            scrollObj.GetComponent<RectTransform>().offsetMax = new Vector2(-24f, -156f);
            _floorListContainer = listRT;
            _scroll = scrollObj.GetComponent<ScrollRect>();

            _startRunBtn = XOUIBuilder.CreateIcon3DButton(rt, _font, XOProceduralAssets.IconNewGame, "New run", new Vector2(360f, 58f), XOProceduralAssets.ColLilac, Color.white, 17);
            RectTransform srRT = _startRunBtn.GetComponent<RectTransform>();
            srRT.SetAnchor(new Vector2(0f, 0f), new Vector2(0f, 0f));
            srRT.anchoredPosition = new Vector2(32f, 24f);
            _startRunBtn.onClick.AddListener(() =>
            {
                _playSfx?.Invoke("powerup");
                XORoguelikeRunManager.StartNewRun();
                Refresh();
            });

            _caravanBtn = XOUIBuilder.CreateIcon3DButton(rt, _font, XOProceduralAssets.IconRelic, "Relic shop", new Vector2(340f, 58f), XOProceduralAssets.ColSun, XOProceduralAssets.ColInk, 18);
            RectTransform cvRT = _caravanBtn.GetComponent<RectTransform>();
            cvRT.SetAnchor(new Vector2(1f, 0f), new Vector2(1f, 0f));
            cvRT.anchoredPosition = new Vector2(-32f, 24f);
            _caravanBtn.onClick.AddListener(() =>
            {
                RootObj.SetActive(false);
                _onOpenCaravan?.Invoke();
            });

            RootObj.SetActive(false);
        }

        public void Open()
        {
            _playSfx?.Invoke("click");
            if (!XORoguelikeRunManager.IsRunActive)
            {
                XORoguelikeRunManager.StartNewRun();
            }
            Refresh();
            RootObj.transform.SetAsLastSibling();
            RootObj.SetActive(true);
        }

        /// <summary>Rebuilds every floor row with current run state and blind modifiers.</summary>
        public void Refresh()
        {
            if (_floorListContainer == null) return;

            for (int i = _floorListContainer.childCount - 1; i >= 0; i--)
            {
                // Detach first so the layout group stops counting the row this frame (Destroy is deferred).
                Transform old = _floorListContainer.GetChild(i);
                old.SetParent(null, false);
                UnityEngine.Object.Destroy(old.gameObject);
            }

            if (_runSummaryText != null)
            {
                _runSummaryText.text = $"Seed {XORoguelikeRunManager.FormattedSeed} · Floor {XORoguelikeRunManager.CurrentFloor}/{XORoguelikeCatalog.FloorCount} · Hearts {XORoguelikeRunManager.Lives}/{XORoguelikeRunManager.MaxLives} · Relics {XORoguelikeRunManager.EquippedRelics.Count}/{XORoguelikeRunManager.MaxRelicSlots}";
            }

            TowerFloorBlind[] floors = XORoguelikeRunManager.FloorBlinds;
            for (int i = 0; i < floors.Length; i++)
            {
                CreateFloorRow(floors[i]);
            }

            ScrollToCurrentFloor(floors.Length);
        }

        /// <summary>Scrolls so the floor the player is about to fight sits near the top of the list instead of always starting at floor 1.</summary>
        private void ScrollToCurrentFloor(int floorCount)
        {
            if (_scroll == null || floorCount < 2) return;
            Canvas.ForceUpdateCanvases();
            float t = (XORoguelikeRunManager.CurrentFloor - 1f) / (floorCount - 1f);
            _scroll.verticalNormalizedPosition = 1f - Mathf.Clamp01(t);
        }

        private void CreateFloorRow(TowerFloorBlind floor)
        {
            bool runActive = XORoguelikeRunManager.IsRunActive;
            bool isCleared = runActive
                ? (floor.floorNumber < XORoguelikeRunManager.CurrentFloor)
                : (floor.floorNumber <= XOProgressionSystem.HighestTowerFloorCleared);
            bool isCurrentFloor = floor.floorNumber == XORoguelikeRunManager.CurrentFloor;

            GameObject rowObj = XOUIBuilder.CreateUIObject("TowerFloor_" + floor.floorNumber, _floorListContainer);
            LayoutElement le = rowObj.AddComponent<LayoutElement>();
            le.minHeight = 88f;
            le.preferredHeight = 88f;

            Image rowBg = rowObj.AddComponent<Image>();
            rowBg.sprite = XOProceduralAssets.CardPanelSprite;
            rowBg.type = Image.Type.Sliced;
            rowBg.color = isCurrentFloor ? XOUIColors.Emerald100 : (isCleared ? XOUIColors.Slate50 : Color.white);

            RectTransform rowRT = rowObj.GetComponent<RectTransform>();
            Color badgeColor = floor.mapAccentColor.ToUnityColor();

            GameObject numBox = XOUIBuilder.CreateUIObject("FloorBadge", rowRT);
            RectTransform nbRT = numBox.GetComponent<RectTransform>();
            nbRT.SetAnchor(new Vector2(0f, 0.5f), new Vector2(0f, 0.5f));
            nbRT.anchoredPosition = new Vector2(14f, 0f);
            nbRT.sizeDelta = new Vector2(62f, 62f);
            Image nbImg = numBox.AddComponent<Image>();
            nbImg.sprite = XOProceduralAssets.PillSprite;
            nbImg.type = Image.Type.Sliced;
            nbImg.color = Color.Lerp(badgeColor, Color.white, 0.35f);

            Text fNumTxt = XOUIBuilder.CreateText(nbRT, _font, $"F{floor.floorNumber}", 22, FontStyle.Bold, TextAnchor.MiddleCenter);
            fNumTxt.color = XOProceduralAssets.ColInk;

            Text titleTxt = XOUIBuilder.CreateText(rowRT, _font, $"{floor.bossName} · {floor.blindName}", 19, FontStyle.Bold, TextAnchor.UpperLeft);
            titleTxt.rectTransform.offsetMin = new Vector2(92f, 0f);
            titleTxt.rectTransform.offsetMax = new Vector2(-178f, -12f);

            Text subTxt = XOUIBuilder.CreateText(rowRT, _font, $"+{floor.coinReward} coins · {floor.mapName} {floor.boardSize}×{floor.boardSize} · {floor.blindDescription}", 13, FontStyle.Normal, TextAnchor.LowerLeft);
            subTxt.color = XOProceduralAssets.ColMuted;
            subTxt.rectTransform.offsetMin = new Vector2(92f, 12f);
            subTxt.rectTransform.offsetMax = new Vector2(-178f, 0f);

            if (isCleared)
            {
                XOUIBuilder.CreatePillBadge(rowRT, _font, "Cleared", XOUIColors.Emerald100, XOUIColors.Emerald800);
            }
            else if (isCurrentFloor)
            {
                Button fightBtn = XOUIBuilder.CreateIcon3DButton(rowRT, _font, XOProceduralAssets.IconNewGame, "Fight!", new Vector2(144f, 50f), XOProceduralAssets.ColMint, XOProceduralAssets.ColInk, 17);
                RectTransform fRT = fightBtn.GetComponent<RectTransform>();
                fRT.SetAnchor(new Vector2(1f, 0.5f), new Vector2(1f, 0.5f));
                fRT.anchoredPosition = new Vector2(-14f, 0f);
                int targetFloor = floor.floorNumber;
                fightBtn.onClick.AddListener(() =>
                {
                    RootObj.SetActive(false);
                    _onStartFloorMatch?.Invoke(targetFloor);
                });
            }
            else
            {
                XOUIBuilder.CreatePillBadge(rowRT, _font, "Locked", XOUIColors.Slate200, XOUIColors.Slate700);
            }
        }
    }
}

