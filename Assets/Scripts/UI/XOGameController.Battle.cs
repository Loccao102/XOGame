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
        // SCREEN 2: BATTLE SCREEN (FOCUSED GAMEPLAY ARENA)
        // =====================================================================

        private void BuildBattleScreen(RectTransform parent)
        {
            // 1. Center Spacious Caro Board (Created first so it is rendered behind HUD overlays)
            GameObject centerBoard = CreateUIObject("BoardOuterFrame", parent);
            _boardOuterFrameRT = centerBoard.GetComponent<RectTransform>();
            _boardOuterFrameImg = centerBoard.AddComponent<Image>();
            _boardOuterFrameImg.sprite = XOProceduralAssets.CardPanelSprite;
            _boardOuterFrameImg.type = Image.Type.Sliced;
            _boardOuterFrameImg.color = XOProceduralAssets.ColCardBg;
            BuildBattleCenterBoard(_boardOuterFrameRT);

            // 2. Top HUD ([Home], Turn, Timer, Player cards, Status banner)
            GameObject topHud = CreateUIObject("BattleTopHUD", parent);
            _battleTopHudRT = topHud.GetComponent<RectTransform>();
            BuildBattleTopHud(_battleTopHudRT);

            // 3. Active Relic Tray (Tower Jokers Bar)
            BuildBattleRelicBar(parent);

            // 4. Bottom Tactile Action Bar (6 Compact Skills)
            GameObject bottomBar = CreateUIObject("BattleBottomBar", parent);
            _battleBottomBarRT = bottomBar.GetComponent<RectTransform>();
            BuildBattleBottomBar(_battleBottomBarRT);
        }

        private void BuildBattleTopHud(RectTransform parent)
        {
            // [ ⌂ Home ] Button
            _battleHomeBtn = CreateIcon3DButton(parent, XOProceduralAssets.IconHome, "Home", new Vector2(120f, 52f), Color.white, XOProceduralAssets.ColInk, 17);
            RectTransform homeRT = _battleHomeBtn.GetComponent<RectTransform>();
            homeRT.anchorMin = new Vector2(0f, 1f);
            homeRT.anchorMax = new Vector2(0f, 1f);
            homeRT.pivot = new Vector2(0f, 1f);
            homeRT.anchoredPosition = new Vector2(0f, 0f);
            _battleHomeBtn.onClick.AddListener(ReturnToLobby);

            // Score Section (Player X vs Bot O) - Starts neatly after Home button
            GameObject scoreSec = CreateUIObject("ScoreSection", parent);
            RectTransform scoreSecRT = scoreSec.GetComponent<RectTransform>();
            scoreSecRT.anchorMin = new Vector2(0f, 1f);
            scoreSecRT.anchorMax = new Vector2(1f, 1f);
            scoreSecRT.pivot = new Vector2(0f, 1f);
            scoreSecRT.anchoredPosition = new Vector2(130f, 0f);
            scoreSecRT.sizeDelta = new Vector2(-130f, 52f);

            // Player X Card (Sky #4FB0FF)
            GameObject cardX = CreateUIObject("PlayerXCard", scoreSecRT);
            RectTransform cardXRT = cardX.GetComponent<RectTransform>();
            cardXRT.anchorMin = new Vector2(0f, 0f);
            cardXRT.anchorMax = new Vector2(0.485f, 1f);
            cardXRT.offsetMin = Vector2.zero;
            cardXRT.offsetMax = Vector2.zero;

            _playerXCardImg = cardX.AddComponent<Image>();
            _playerXCardImg.sprite = XOProceduralAssets.Pill3DSprite;
            _playerXCardImg.type = Image.Type.Sliced;
            _playerXCardImg.color = XOProceduralAssets.ColSky;

            _playerXTokenIcon = CreateIconImage(cardXRT, XOProceduralAssets.XPieceSprite, new Vector2(0f, 0.5f), new Vector2(24f, 0f), new Vector2(32f, 32f));
            _playerXTitleText = CreateText(cardXRT, "YOU (X)", 16, FontStyle.Bold, TextAnchor.MiddleLeft);
            _playerXTitleText.rectTransform.offsetMin = new Vector2(44f, 0f);
            _playerXTitleText.rectTransform.offsetMax = new Vector2(-42f, 0f);
            _playerXTitleText.color = XOProceduralAssets.ColInk;
            _playerXTitleText.resizeTextForBestFit = true;
            _playerXTitleText.resizeTextMinSize = 10;
            _playerXTitleText.resizeTextMaxSize = 16;
            _playerXTitleText.horizontalOverflow = HorizontalWrapMode.Overflow;

            _scoreXText = CreateText(cardXRT, "0", 22, FontStyle.Bold, TextAnchor.MiddleRight);
            _scoreXText.rectTransform.offsetMin = new Vector2(-42f, 0f);
            _scoreXText.rectTransform.offsetMax = new Vector2(-10f, 0f);
            _scoreXText.color = XOProceduralAssets.ColInk;

            // Player O Card (Coral #FF6F85)
            GameObject cardO = CreateUIObject("PlayerOCard", scoreSecRT);
            RectTransform cardORT = cardO.GetComponent<RectTransform>();
            cardORT.anchorMin = new Vector2(0.515f, 0f);
            cardORT.anchorMax = new Vector2(1f, 1f);
            cardORT.offsetMin = Vector2.zero;
            cardORT.offsetMax = Vector2.zero;

            _playerOCardImg = cardO.AddComponent<Image>();
            _playerOCardImg.sprite = XOProceduralAssets.Pill3DSprite;
            _playerOCardImg.type = Image.Type.Sliced;
            _playerOCardImg.color = XOProceduralAssets.ColCoral;

            _playerOTokenIcon = CreateIconImage(cardORT, XOProceduralAssets.OPieceSprite, new Vector2(0f, 0.5f), new Vector2(24f, 0f), new Vector2(32f, 32f));
            _playerOTitleText = CreateText(cardORT, "BOT (O)", 16, FontStyle.Bold, TextAnchor.MiddleLeft);
            _playerOTitleText.rectTransform.offsetMin = new Vector2(44f, 0f);
            _playerOTitleText.rectTransform.offsetMax = new Vector2(-42f, 0f);
            _playerOTitleText.color = Color.white;
            _playerOTitleText.resizeTextForBestFit = true;
            _playerOTitleText.resizeTextMinSize = 10;
            _playerOTitleText.resizeTextMaxSize = 16;
            _playerOTitleText.horizontalOverflow = HorizontalWrapMode.Overflow;

            _scoreOText = CreateText(cardORT, "0", 22, FontStyle.Bold, TextAnchor.MiddleRight);
            _scoreOText.rectTransform.offsetMin = new Vector2(-42f, 0f);
            _scoreOText.rectTransform.offsetMax = new Vector2(-10f, 0f);
            _scoreOText.color = Color.white;

            // Turn Timer Bar
            GameObject timerTrack = CreateUIObject("TurnTimerTrack", parent);
            RectTransform ttRT = timerTrack.GetComponent<RectTransform>();
            ttRT.anchorMin = new Vector2(0f, 1f);
            ttRT.anchorMax = new Vector2(1f, 1f);
            ttRT.pivot = new Vector2(0.5f, 1f);
            ttRT.anchoredPosition = new Vector2(0f, -66f);
            ttRT.sizeDelta = new Vector2(0f, 10f);

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
            sbRT.anchoredPosition = new Vector2(0f, -84f);
            sbRT.sizeDelta = new Vector2(0f, 44f);

            _statusBannerImg = statusObj.AddComponent<Image>();
            _statusBannerImg.sprite = XOProceduralAssets.PillSprite;
            _statusBannerImg.type = Image.Type.Sliced;
            _statusBannerImg.color = Color.white;

            _statusBannerText = CreateText(sbRT, "Your Turn (X) • Connect 5 to win!", 17, FontStyle.Bold, TextAnchor.MiddleCenter);
            _statusBannerText.color = XOProceduralAssets.ColInk;
            _statusBannerText.resizeTextForBestFit = true;
            _statusBannerText.resizeTextMinSize = 12;
            _statusBannerText.resizeTextMaxSize = 17;
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
                    bool isDisabled = (XORoguelikeRunManager.CurrentMatch.DisabledRelic.HasValue && XORoguelikeRunManager.CurrentMatch.DisabledRelic.Value == type);

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
    }
}
