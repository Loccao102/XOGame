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
        // MODAL: GAME MODE SELECTOR (7 VARIANTS & 2-PLAYER)
        // =====================================================================

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
    }
}
