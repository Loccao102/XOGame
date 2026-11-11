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
        // MODAL: TOWER OF ANOMALIES (ROGUELIKE FLOOR SELECT & MATCH START)
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
                    ? "Build TWO 5-in-a-rows (2x5) to Win!"
                    : ((blind.maxPlayerMoves > 0) ? $"[Limit: {blind.maxPlayerMoves} Moves!]" : "Connect 5 in a row to Win!");
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
            svRT.offsetMax = new Vector2(-36f, -136f);

            scrollObj.AddComponent<RectMask2D>();

            ScrollRect scrollRect = scrollObj.AddComponent<ScrollRect>();
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;
            scrollRect.scrollSensitivity = 32f;
            scrollRect.inertia = true;
            scrollRect.decelerationRate = 0.135f;

            // Sleek permanent vertical scrollbar for clear visibility of all 10 floors
            GameObject sbObj = CreateUIObject("TowerScrollBar", cardRT);
            RectTransform sbRT = sbObj.GetComponent<RectTransform>();
            sbRT.anchorMin = new Vector2(1f, 0f);
            sbRT.anchorMax = new Vector2(1f, 1f);
            sbRT.pivot = new Vector2(1f, 0.5f);
            sbRT.offsetMin = new Vector2(-24f, 86f);
            sbRT.offsetMax = new Vector2(-14f, -138f);
            Image sbTrackImg = sbObj.AddComponent<Image>();
            sbTrackImg.sprite = XOProceduralAssets.PillSprite;
            sbTrackImg.type = Image.Type.Sliced;
            sbTrackImg.color = new Color(0.12f, 0.16f, 0.28f, 0.12f);

            GameObject slidingArea = CreateUIObject("SlidingArea", sbRT);
            RectTransform saRT = slidingArea.GetComponent<RectTransform>();
            StretchFull(saRT);

            GameObject handleObj = CreateUIObject("Handle", saRT);
            RectTransform handleRT = handleObj.GetComponent<RectTransform>();
            StretchFull(handleRT);
            Image handleImg = handleObj.AddComponent<Image>();
            handleImg.sprite = XOProceduralAssets.PillSprite;
            handleImg.type = Image.Type.Sliced;
            handleImg.color = XOProceduralAssets.ColSky;

            Scrollbar scrollbar = sbObj.AddComponent<Scrollbar>();
            scrollbar.handleRect = handleRT;
            scrollbar.targetGraphic = handleImg;
            scrollbar.direction = Scrollbar.Direction.BottomToTop;

            scrollRect.verticalScrollbar = scrollbar;
            scrollRect.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.Permanent;
            scrollRect.verticalScrollbarSpacing = 6f;

            // Scrollable Content
            GameObject contentObj = CreateUIObject("TowerScrollContent", svRT);
            RectTransform cRT = contentObj.GetComponent<RectTransform>();
            cRT.anchorMin = new Vector2(0f, 1f);
            cRT.anchorMax = new Vector2(1f, 1f);
            cRT.pivot = new Vector2(0.5f, 1f);
            cRT.anchoredPosition = Vector2.zero;
            cRT.sizeDelta = new Vector2(0f, 0f);

            VerticalLayoutGroup vLayout = contentObj.AddComponent<VerticalLayoutGroup>();
            vLayout.spacing = 8f;
            vLayout.childControlWidth = true;
            vLayout.childControlHeight = false;
            vLayout.childForceExpandWidth = true;
            vLayout.childForceExpandHeight = false;
            vLayout.padding = new RectOffset(4, 4, 4, 16);

            ContentSizeFitter csf = contentObj.AddComponent<ContentSizeFitter>();
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scrollRect.viewport = svRT;
            scrollRect.content = cRT;
            _towerScrollRect = scrollRect;
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

            Button closeBtn = CreateIcon3DButton(brRT, XOProceduralAssets.IconHome, "Back to Hub", new Vector2(210f, 50f), XOProceduralAssets.ColMuted, Color.white, 16);
            closeBtn.onClick.AddListener(() =>
            {
                PlaySfx("click");
                _towerModalRoot.SetActive(false);
            });

            Button caravanBtn = CreateIcon3DButton(brRT, XOProceduralAssets.IconChest, "Relic Caravan", new Vector2(210f, 50f), XOProceduralAssets.ColLilac, Color.white, 16);
            caravanBtn.onClick.AddListener(() =>
            {
                PlaySfx("click");
                OpenCaravanDraftModal();
            });

            _towerRestartRunBtn = CreateIcon3DButton(brRT, XOProceduralAssets.IconUndo, "New Run", new Vector2(190f, 50f), XOProceduralAssets.ColCoral, Color.white, 15);
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

                if (_towerScrollRect != null && XORoguelikeRunManager.FloorBlinds.Length > 1)
                {
                    // Auto-scroll to show active floor in viewport
                    float targetPos = 1f - Mathf.Clamp01((float)(XORoguelikeRunManager.CurrentFloor - 1) / (XORoguelikeRunManager.FloorBlinds.Length - 1));
                    _towerScrollRect.verticalNormalizedPosition = targetPos;
                }
            }
        }

        private void CreateTowerFloorRow(RectTransform parent, TowerFloorBlind floor)
        {
            GameObject rowObj = CreateUIObject($"FloorRow_{floor.floorNumber}", parent);
            RectTransform rowRT = rowObj.GetComponent<RectTransform>();
            rowRT.sizeDelta = new Vector2(0f, 80f);

            LayoutElement le = rowObj.AddComponent<LayoutElement>();
            le.minHeight = 80f;
            le.preferredHeight = 80f;
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
                bg.color = XOThemeDatabase.Hex("#EFF6FF"); // active soft sky
            }
            else
            {
                bg.color = XOThemeDatabase.Hex("#F8FAFC"); // clean light slate
            }

            // Icon
            Sprite icon = isCleared ? XOProceduralAssets.IconTrophy : ((isCurrentFloor && !isRunOver) ? XOProceduralAssets.IconCrown : (isRunOver && isCurrentFloor ? XOProceduralAssets.IconSwords : XOProceduralAssets.IconShield));
            Color iconCol = isCleared ? XOProceduralAssets.ColMint : ((isCurrentFloor && !isRunOver) ? XOProceduralAssets.ColSun : (isRunOver && isCurrentFloor ? XOProceduralAssets.ColCoral : new Color(0.40f, 0.46f, 0.58f)));
            Image icImg = CreateIconImage(rowRT, icon, new Vector2(0f, 0.5f), new Vector2(38f, 0f), new Vector2(46f, 46f));
            icImg.color = iconCol;

            // Floor & Boss Name
            string title = $"F.{floor.floorNumber} - {floor.bossName} • {floor.mapName}";
            Text nameTxt = CreateText(rowRT, title, 19, FontStyle.Bold, TextAnchor.UpperLeft);
            nameTxt.rectTransform.offsetMin = new Vector2(76f, 0f);
            nameTxt.rectTransform.offsetMax = new Vector2(-165f, -10f);
            nameTxt.color = (!isLocked && !isRunOver) ? XOProceduralAssets.ColInk : (isRunOver && isCurrentFloor ? XOProceduralAssets.ColCoral : XOThemeDatabase.Hex("#1E293B"));

            // Blind & Relic Info (High-contrast, crystal clear typography)
            string relicInfo = (floor.botEquipment != BotEquipmentType.None) ? $"Boss Relic: {floor.botEquipment} | " : "";
            string desc = $"{relicInfo}{floor.blindDescription} (+{floor.coinReward}c & Draft)";
            Text subTxt = CreateText(rowRT, desc, 15, FontStyle.Normal, TextAnchor.LowerLeft);
            subTxt.rectTransform.offsetMin = new Vector2(76f, 10f);
            subTxt.rectTransform.offsetMax = new Vector2(-165f, 0f);
            subTxt.color = (!isLocked && !isRunOver) 
                ? ((floor.botEquipment != BotEquipmentType.None) ? XOThemeDatabase.Hex("#B91C1C") : XOThemeDatabase.Hex("#0F172A"))
                : (isCleared ? XOThemeDatabase.Hex("#047857") : XOThemeDatabase.Hex("#334155"));

            // Right-side badge or Fight button
            if (isRunOver)
            {
                if (floor.floorNumber == 1)
                {
                    Button startBtn = CreateIcon3DButton(rowRT, XOProceduralAssets.IconNewGame, "START RUN", new Vector2(144f, 50f), XOProceduralAssets.ColMint, XOProceduralAssets.ColInk, 16);
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
                    CreatePillBadge(rowRT, "LOCKED", XOThemeDatabase.Hex("#E2E8F0"), XOThemeDatabase.Hex("#334155"));
                }
            }
            else if (isCleared)
            {
                CreatePillBadge(rowRT, "CLEARED", XOThemeDatabase.Hex("#D1FAE5"), XOThemeDatabase.Hex("#065F46"));
            }
            else if (isCurrentFloor)
            {
                Button fightBtn = CreateIcon3DButton(rowRT, XOProceduralAssets.IconNewGame, "FIGHT!", new Vector2(144f, 50f), XOProceduralAssets.ColMint, XOProceduralAssets.ColInk, 17);
                RectTransform fRT = fightBtn.GetComponent<RectTransform>();
                fRT.anchorMin = new Vector2(1f, 0.5f);
                fRT.anchorMax = new Vector2(1f, 0.5f);
                fRT.pivot = new Vector2(1f, 0.5f);
                fRT.anchoredPosition = new Vector2(-14f, 0f);
                fightBtn.onClick.AddListener(() => StartTowerMatch(floor.floorNumber));
            }
            else
            {
                CreatePillBadge(rowRT, "LOCKED", XOThemeDatabase.Hex("#E2E8F0"), XOThemeDatabase.Hex("#334155"));
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
            bRT.sizeDelta = new Vector2(120f, 40f);

            Image bBg = badge.AddComponent<Image>();
            bBg.sprite = XOProceduralAssets.PillSprite;
            bBg.type = Image.Type.Sliced;
            bBg.color = bgColor;
            bBg.raycastTarget = false;

            Text bTxt = CreateText(bRT, label, 14, FontStyle.Bold, TextAnchor.MiddleCenter);
            bTxt.color = textColor;
            return badge;
        }
    }
}
