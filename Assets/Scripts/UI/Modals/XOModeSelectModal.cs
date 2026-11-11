using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using XOPlayable.Core;
using XOPlayable.Visuals;

namespace XOPlayable.UI
{
    /// <summary>
    /// Mode, Rule Variant, Board Size, and Difficulty selection modal preserving all 7 playable rule variants.
    /// </summary>
    public sealed class XOModeSelectModal
    {
        private readonly Font _font;
        private readonly Action<string> _playSfx;
        private readonly Action<GameMode, XOVariantRule, int, AIDifficulty> _onApplySelection;

        public GameObject RootObj { get; private set; }
        public RectTransform CardRT { get; private set; }

        private readonly List<(Button btn, Image img, Text txt, GameMode mode)> _modeButtons = new List<(Button, Image, Text, GameMode)>();
        private readonly List<(Button btn, Image img, Text txt, AIDifficulty diff)> _diffButtons = new List<(Button, Image, Text, AIDifficulty)>();
        private readonly List<(Button btn, Image img, Text txt, XOVariantRule variant, int size)> _variantButtons = new List<(Button, Image, Text, XOVariantRule, int)>();

        private GameMode _pendingMode;
        private XOVariantRule _pendingVariant;
        private int _pendingBoardSize;
        private AIDifficulty _pendingDifficulty;

        public XOModeSelectModal(
            Font font,
            Action<string> playSfx,
            Action<GameMode, XOVariantRule, int, AIDifficulty> onApplySelection)
        {
            _font = font;
            _playSfx = playSfx;
            _onApplySelection = onApplySelection;
        }

        /// <summary>Builds the Mode &amp; Variant Selection modal UI.</summary>
        public void Build(Transform canvasTransform)
        {
            RootObj = XOUIBuilder.CreateModalBackdrop("ModeSelectModal", canvasTransform, out RectTransform rt);
            CardRT = rt;
            rt.sizeDelta = new Vector2(880f, 860f);

            XOUIBuilder.CreateModalHeader(rt, _font, XOProceduralAssets.IconNewGame, "SELECT MODE & VARIANT", 30, () => { _playSfx?.Invoke("click"); RootObj.SetActive(false); });

            BuildModeRow(rt);
            BuildVariantGrid(rt);
            BuildDifficultyRow(rt);

            Button startBtn = XOUIBuilder.CreateIcon3DButton(rt, _font, XOProceduralAssets.IconNewGame, "START MATCH WITH THESE RULES", new Vector2(780f, 66f), XOProceduralAssets.ColMint, XOProceduralAssets.ColInk, 22);
            RectTransform sRT = startBtn.GetComponent<RectTransform>();
            sRT.SetAnchor(new Vector2(0.5f, 0f), new Vector2(0.5f, 0f));
            sRT.anchoredPosition = new Vector2(0f, 24f);
            startBtn.onClick.AddListener(() =>
            {
                _playSfx?.Invoke("click");
                RootObj.SetActive(false);
                _onApplySelection?.Invoke(_pendingMode, _pendingVariant, _pendingBoardSize, _pendingDifficulty);
            });

            RootObj.SetActive(false);
        }

        private void BuildModeRow(RectTransform cardRT)
        {
            Text lbl = XOUIBuilder.CreateText(cardRT, _font, "1. GAME MODE", 16, FontStyle.Bold, TextAnchor.UpperLeft);
            lbl.color = XOProceduralAssets.ColMuted;
            lbl.rectTransform.offsetMin = new Vector2(36f, 0f);
            lbl.rectTransform.offsetMax = new Vector2(-36f, -86f);

            GameObject row = XOUIBuilder.CreateUIObject("ModeRow", cardRT);
            RectTransform rowRT = row.GetComponent<RectTransform>();
            rowRT.SetAnchor(new Vector2(0.5f, 1f), new Vector2(0.5f, 1f));
            rowRT.anchoredPosition = new Vector2(0f, -112f);
            rowRT.sizeDelta = new Vector2(808f, 58f);

            HorizontalLayoutGroup hlg = row.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 12f;
            hlg.childControlWidth = true;
            hlg.childControlHeight = true;
            hlg.childForceExpandWidth = true;

            AddModeButton(rowRT, "VS AI", GameMode.VsAI);
            AddModeButton(rowRT, "2P LOCAL", GameMode.TwoPlayerLocal);
            AddModeButton(rowRT, "PUZZLES", GameMode.PuzzleStage);
            AddModeButton(rowRT, "TOWER RUN", GameMode.TowerClimb);
        }

        private void AddModeButton(RectTransform parent, string label, GameMode mode)
        {
            Button btn = XOUIBuilder.Create3DButton(parent, _font, label, new Vector2(190f, 56f), XOProceduralAssets.ColTileEdge, XOProceduralAssets.ColInk, 16);
            btn.onClick.AddListener(() =>
            {
                _playSfx?.Invoke("click");
                _pendingMode = mode;
                Refresh(_pendingMode, _pendingVariant, _pendingBoardSize, _pendingDifficulty);
            });
            _modeButtons.Add((btn, btn.GetComponent<Image>(), btn.GetComponentInChildren<Text>(), mode));
        }

        private void BuildVariantGrid(RectTransform cardRT)
        {
            Text lbl = XOUIBuilder.CreateText(cardRT, _font, "2. BOARD & RULE VARIANT (ALL 7 RULESETS)", 16, FontStyle.Bold, TextAnchor.UpperLeft);
            lbl.color = XOProceduralAssets.ColMuted;
            lbl.rectTransform.offsetMin = new Vector2(36f, 0f);
            lbl.rectTransform.offsetMax = new Vector2(-36f, -190f);

            XOUIBuilder.CreateModalScrollView("VariantsScroll", cardRT, out RectTransform listRT, out GameObject scrollObj);
            RectTransform sRT = scrollObj.GetComponent<RectTransform>();
            sRT.offsetMin = new Vector2(32f, 210f);
            sRT.offsetMax = new Vector2(-32f, -218f);

            AddVariantCard(listRT, "Classic Caro 15x15", "Pure competitive 5-in-a-row Gomoku on a 15x15 board", XOVariantRule.ClassicCaro, 15);
            AddVariantCard(listRT, "Quick Caro 10x10", "Fast-paced 5-in-a-row battles on a compact 10x10 board", XOVariantRule.ClassicCaro, 10);
            AddVariantCard(listRT, "Infinite 3x3 Bolt", "Connect 3 — max 3 pieces per player, oldest piece vanishes!", XOVariantRule.Infinite3x3, 3);
            AddVariantCard(listRT, "Gravity Drop 8x8", "Pieces drop to the bottom-most empty row — Connect 4 wins!", XOVariantRule.GravityDrop, 8);
            AddVariantCard(listRT, "Strict Blocked Caro 10x10", "5-in-a-row blocked on BOTH ends by opponent does NOT win", XOVariantRule.StrictCaro, 10);
            AddVariantCard(listRT, "Chaos Minefield & Portals", "Hidden Blast Mines, Warp Twin Portals, Gold & Lightning tiles", XOVariantRule.ChaosMinefield, 10);
            AddVariantCard(listRT, "Gobblet Stack 4x4", "Consume smaller enemy pieces cleanly in tactical 4-in-a-row", XOVariantRule.GobbletStack, 4);
        }

        private void AddVariantCard(RectTransform parent, string title, string desc, XOVariantRule variant, int size)
        {
            Button btn = XOUIBuilder.Create3DButton(parent, _font, "", new Vector2(780f, 68f), Color.white, XOProceduralAssets.ColInk, 16);
            LayoutElement le = btn.gameObject.AddComponent<LayoutElement>();
            le.minHeight = 68f;
            le.preferredHeight = 68f;

            RectTransform rt = btn.GetComponent<RectTransform>();
            Text tTitle = XOUIBuilder.CreateText(rt, _font, title, 18, FontStyle.Bold, TextAnchor.UpperLeft);
            tTitle.rectTransform.offsetMin = new Vector2(18f, 0f);
            tTitle.rectTransform.offsetMax = new Vector2(-18f, -10f);

            Text tDesc = XOUIBuilder.CreateText(rt, _font, desc, 13, FontStyle.Normal, TextAnchor.LowerLeft);
            tDesc.color = XOProceduralAssets.ColMuted;
            tDesc.rectTransform.offsetMin = new Vector2(18f, 10f);
            tDesc.rectTransform.offsetMax = new Vector2(-18f, 0f);

            btn.onClick.AddListener(() =>
            {
                _playSfx?.Invoke("click");
                _pendingVariant = variant;
                _pendingBoardSize = size;
                Refresh(_pendingMode, _pendingVariant, _pendingBoardSize, _pendingDifficulty);
            });

            _variantButtons.Add((btn, btn.GetComponent<Image>(), tTitle, variant, size));
        }

        private void BuildDifficultyRow(RectTransform cardRT)
        {
            Text lbl = XOUIBuilder.CreateText(cardRT, _font, "3. AI DIFFICULTY TIER", 16, FontStyle.Bold, TextAnchor.LowerLeft);
            lbl.color = XOProceduralAssets.ColMuted;
            lbl.rectTransform.offsetMin = new Vector2(36f, 168f);
            lbl.rectTransform.offsetMax = new Vector2(-36f, 0f);

            GameObject row = XOUIBuilder.CreateUIObject("DiffRow", cardRT);
            RectTransform rowRT = row.GetComponent<RectTransform>();
            rowRT.SetAnchor(new Vector2(0.5f, 0f), new Vector2(0.5f, 0f));
            rowRT.anchoredPosition = new Vector2(0f, 104f);
            rowRT.sizeDelta = new Vector2(808f, 52f);

            HorizontalLayoutGroup hlg = row.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 14f;
            hlg.childControlWidth = true;
            hlg.childControlHeight = true;
            hlg.childForceExpandWidth = true;

            AddDiffButton(rowRT, "EASY", AIDifficulty.Easy);
            AddDiffButton(rowRT, "NORMAL", AIDifficulty.Normal);
            AddDiffButton(rowRT, "HARD", AIDifficulty.Hard);
        }

        private void AddDiffButton(RectTransform parent, string label, AIDifficulty diff)
        {
            Button btn = XOUIBuilder.Create3DButton(parent, _font, label, new Vector2(250f, 50f), XOProceduralAssets.ColTileEdge, XOProceduralAssets.ColInk, 16);
            btn.onClick.AddListener(() =>
            {
                _playSfx?.Invoke("click");
                _pendingDifficulty = diff;
                Refresh(_pendingMode, _pendingVariant, _pendingBoardSize, _pendingDifficulty);
            });
            _diffButtons.Add((btn, btn.GetComponent<Image>(), btn.GetComponentInChildren<Text>(), diff));
        }

        public void Open(GameMode mode, XOVariantRule variant, int boardSize, AIDifficulty difficulty)
        {
            _playSfx?.Invoke("click");
            Refresh(mode, variant, boardSize, difficulty);
            RootObj.transform.SetAsLastSibling();
            RootObj.SetActive(true);
        }

        public void Refresh(GameMode mode, XOVariantRule variant, int boardSize, AIDifficulty difficulty)
        {
            _pendingMode = mode;
            _pendingVariant = variant;
            _pendingBoardSize = boardSize;
            _pendingDifficulty = difficulty;

            foreach (var m in _modeButtons)
            {
                bool active = m.mode == mode;
                m.img.color = active ? XOProceduralAssets.ColSky : XOProceduralAssets.ColTileEdge;
                if (m.txt != null) m.txt.color = active ? Color.white : XOProceduralAssets.ColInk;
            }

            foreach (var d in _diffButtons)
            {
                bool active = d.diff == difficulty;
                d.img.color = active ? XOProceduralAssets.ColLilac : XOProceduralAssets.ColTileEdge;
                if (d.txt != null) d.txt.color = active ? Color.white : XOProceduralAssets.ColInk;
            }

            foreach (var v in _variantButtons)
            {
                bool active = v.variant == variant && (variant != XOVariantRule.ClassicCaro || v.size == boardSize);
                v.img.color = active ? XOUIColors.Emerald50 : Color.white;
                if (v.txt != null) v.txt.color = active ? XOUIColors.Emerald600 : XOProceduralAssets.ColInk;
            }
        }
    }
}

