using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using XOPlayable.Ads;
using XOPlayable.Core;
using XOPlayable.Visuals;

namespace XOPlayable.UI
{
    /// <summary>
    /// 3-Tab Cosmetics &amp; Skills Shop modal: Board Themes, X/O Piece Skins, and Tactical Skill Consumables.
    /// </summary>
    public sealed class XOThemeShopModal
    {
        private sealed class ShopCardRefs
        {
            public int index;
            public Image cardBg;
            public Text statusText;
            public Button selectOrCoinBtn;
            public Text selectOrCoinBtnText;
            public Button watchAdUnlockBtn;
        }

        private readonly Font _font;
        private readonly XOGameStateStore _store;
        private readonly Action<string> _playSfx;
        private readonly Action<string, string, Color> _spawnBanner;
        private readonly Action _onCosmeticsChanged;
        private readonly Action _onEconomyChanged;

        public GameObject RootObj { get; private set; }
        public RectTransform CardRT { get; private set; }

        private int _activeShopTab;
        private Button _tabThemesBtn;
        private Button _tabPiecesBtn;
        private Button _tabSkillsBtn;
        private GameObject _themesScrollObj;
        private GameObject _piecesScrollObj;
        private GameObject _skillsScrollObj;

        private readonly List<ShopCardRefs> _themeCards = new List<ShopCardRefs>();
        private readonly List<ShopCardRefs> _pieceSkinCards = new List<ShopCardRefs>();

        public XOThemeShopModal(
            Font font,
            XOGameStateStore store,
            Action<string> playSfx,
            Action<string, string, Color> spawnBanner,
            Action onCosmeticsChanged,
            Action onEconomyChanged)
        {
            _font = font;
            _store = store;
            _playSfx = playSfx;
            _spawnBanner = spawnBanner;
            _onCosmeticsChanged = onCosmeticsChanged;
            _onEconomyChanged = onEconomyChanged;
        }

        /// <summary>Builds the 3-tab Skin &amp; Theme Shop modal UI.</summary>
        public void Build(Transform canvasTransform)
        {
            RootObj = XOUIBuilder.CreateModalBackdrop("ThemeShopModal", canvasTransform, out RectTransform cardRT);
            CardRT = cardRT;
            cardRT.sizeDelta = new Vector2(940f, 920f);

            XOUIBuilder.CreateIconImage(cardRT, XOProceduralAssets.IconSkinShop, new Vector2(0.5f, 1f), new Vector2(-215f, -45f), new Vector2(46f, 46f));
            XOUIBuilder.CreateText(cardRT, _font, "SKIN & THEME SHOP", 34, FontStyle.Bold, TextAnchor.UpperCenter)
                .rectTransform.offsetMax = new Vector2(25f, -26f);

            Button closeBtn = XOUIBuilder.Create3DButton(cardRT, _font, "X", new Vector2(50f, 48f), Color.white, XOProceduralAssets.ColInk, 20);
            RectTransform closeRT = closeBtn.GetComponent<RectTransform>();
            closeRT.anchorMin = new Vector2(1f, 1f);
            closeRT.anchorMax = new Vector2(1f, 1f);
            closeRT.anchoredPosition = new Vector2(-36f, -36f);
            closeBtn.onClick.AddListener(() => { _playSfx?.Invoke("click"); RootObj.SetActive(false); });

            GameObject tabRow = XOUIBuilder.CreateUIObject("ShopTabsRow", cardRT);
            RectTransform tabRT = tabRow.GetComponent<RectTransform>();
            tabRT.anchorMin = new Vector2(0.5f, 1f);
            tabRT.anchorMax = new Vector2(0.5f, 1f);
            tabRT.pivot = new Vector2(0.5f, 1f);
            tabRT.anchoredPosition = new Vector2(0f, -88f);
            tabRT.sizeDelta = new Vector2(860f, 60f);

            HorizontalLayoutGroup tabLayout = tabRow.AddComponent<HorizontalLayoutGroup>();
            tabLayout.spacing = 16f;
            tabLayout.childControlWidth = true;
            tabLayout.childControlHeight = true;
            tabLayout.childForceExpandWidth = true;

            _tabThemesBtn = XOUIBuilder.CreateIcon3DButton(tabRT, _font, XOProceduralAssets.IconSkinShop, "THEMES (4)", new Vector2(270f, 60f), XOProceduralAssets.ColSky, Color.white, 18);
            _tabThemesBtn.onClick.AddListener(() => SwitchTab(0));

            _tabPiecesBtn = XOUIBuilder.CreateIcon3DButton(tabRT, _font, XOProceduralAssets.IconCrown, "SKINS (5)", new Vector2(270f, 60f), XOProceduralAssets.ColTileEdge, XOProceduralAssets.ColInk, 18);
            _tabPiecesBtn.onClick.AddListener(() => SwitchTab(1));

            _tabSkillsBtn = XOUIBuilder.CreateIcon3DButton(tabRT, _font, XOProceduralAssets.IconBomb, "SKILLS (BUY)", new Vector2(270f, 60f), XOProceduralAssets.ColTileEdge, XOProceduralAssets.ColInk, 18);
            _tabSkillsBtn.onClick.AddListener(() => SwitchTab(2));

            XOUIBuilder.CreateModalScrollView("ThemesScroll", cardRT, out RectTransform tListRT, out _themesScrollObj);
            _themeCards.Clear();
            var themes = XOThemeDatabase.Themes;
            for (int i = 0; i < themes.Length; i++)
            {
                int idx = i;
                XOThemePalette t = themes[i];
                _themeCards.Add(CreateShopItemRow(
                    tListRT,
                    XOProceduralAssets.XPieceSprite,
                    XOProceduralAssets.OPieceSprite,
                    t.cellNormalColor.ToUnityColor(),
                    t.xColor.ToUnityColor(),
                    t.oColor.ToUnityColor(),
                    t.displayName,
                    t.subtitle,
                    () => OnThemePrimaryClicked(idx),
                    () => OnThemeAdClicked(idx),
                    idx));
            }

            XOUIBuilder.CreateModalScrollView("SkinsScroll", cardRT, out RectTransform pListRT, out _piecesScrollObj);
            _pieceSkinCards.Clear();
            var pieceSkins = XOThemeDatabase.PieceSkins;
            for (int i = 0; i < pieceSkins.Length; i++)
            {
                int idx = i;
                XOPieceSkin ps = pieceSkins[i];
                Sprite sx = XOProceduralAssets.GetPieceSkinSprite(ps.id, true);
                Sprite so = XOProceduralAssets.GetPieceSkinSprite(ps.id, false);
                Color cx = (ps.id == "classic_xo") ? XOProceduralAssets.Hex("#22D3EE") : Color.white;
                Color co = (ps.id == "classic_xo") ? XOProceduralAssets.Hex("#FB7185") : Color.white;

                _pieceSkinCards.Add(CreateShopItemRow(
                    pListRT, sx, so,
                    XOProceduralAssets.Hex("#1E293B"),
                    cx, co,
                    ps.displayName,
                    ps.subtitle,
                    () => OnPieceSkinPrimaryClicked(idx),
                    () => OnPieceSkinAdClicked(idx),
                    idx));
            }

            XOUIBuilder.CreateModalScrollView("SkillsScroll", cardRT, out RectTransform sListRT, out _skillsScrollObj);
            CreateSkillBuyRow(sListRT, XOProceduralAssets.IconHint, "AI Hint (+1)", "Spot the optimal Gomoku winning or blocking square", 20, () => _store.HintStock, () => { _store.HintStock++; });
            CreateSkillBuyRow(sListRT, XOProceduralAssets.IconUndo, "Undo Move (+1)", "Revert your last move immediately", 20, () => _store.UndoStock, () => { _store.UndoStock++; });
            CreateSkillBuyRow(sListRT, XOProceduralAssets.IconFreeze, "Freeze Tile (+1)", "Lock any empty square for 3 turns", 30, () => _store.FreezeStock, () => { _store.FreezeStock++; });
            CreateSkillBuyRow(sListRT, XOProceduralAssets.IconBomb, "Bomb Piece (+1)", "Destroy 1 opponent piece on the board", 35, () => _store.BombStock, () => { _store.BombStock++; });
            CreateSkillBuyRow(sListRT, XOProceduralAssets.IconSwap, "Swap Piece (+1)", "Convert 1 opponent piece into your X", 40, () => _store.SwapStock, () => { _store.SwapStock++; });

            _piecesScrollObj.SetActive(false);
            _skillsScrollObj.SetActive(false);
            RootObj.SetActive(false);
        }

        public void Open()
        {
            _playSfx?.Invoke("click");
            SwitchTab(_activeShopTab, playClickSfx: false);
            RootObj.transform.SetAsLastSibling();
            RootObj.SetActive(true);
        }

        private void SwitchTab(int tabIdx, bool playClickSfx = true)
        {
            if (playClickSfx) _playSfx?.Invoke("click");
            _activeShopTab = tabIdx;
            if (_themesScrollObj != null) _themesScrollObj.SetActive(tabIdx == 0);
            if (_piecesScrollObj != null) _piecesScrollObj.SetActive(tabIdx == 1);
            if (_skillsScrollObj != null) _skillsScrollObj.SetActive(tabIdx == 2);

            ApplyTabStyle(_tabThemesBtn, tabIdx == 0);
            ApplyTabStyle(_tabPiecesBtn, tabIdx == 1);
            ApplyTabStyle(_tabSkillsBtn, tabIdx == 2);
            Refresh();
        }

        private static void ApplyTabStyle(Button btn, bool active)
        {
            if (btn == null) return;
            btn.GetComponent<Image>().color = active ? XOProceduralAssets.ColSky : XOProceduralAssets.ColTileEdge;
            Text t = btn.GetComponentInChildren<Text>();
            if (t != null) t.color = active ? Color.white : XOProceduralAssets.ColInk;
        }

        public void Refresh()
        {
            int selectedThemeIdx = XOThemeDatabase.GetSelectedThemeIndex();
            var themes = XOThemeDatabase.Themes;
            foreach (ShopCardRefs card in _themeCards)
            {
                XOThemePalette t = themes[card.index];
                UpdateShopCardVisual(card, XOThemeDatabase.IsThemeUnlocked(t.id), card.index == selectedThemeIdx, t.coinCost);
            }

            int selectedPieceIdx = XOThemeDatabase.GetSelectedPieceSkinIndex();
            var skins = XOThemeDatabase.PieceSkins;
            foreach (ShopCardRefs card in _pieceSkinCards)
            {
                XOPieceSkin ps = skins[card.index];
                UpdateShopCardVisual(card, XOThemeDatabase.IsPieceSkinUnlocked(ps.id), card.index == selectedPieceIdx, ps.coinCost);
            }
        }

        private ShopCardRefs CreateShopItemRow(
            RectTransform parent,
            Sprite leftIcon,
            Sprite rightIcon,
            Color swatchBg,
            Color leftTint,
            Color rightTint,
            string title,
            string subtitle,
            UnityEngine.Events.UnityAction onPrimary,
            UnityEngine.Events.UnityAction onAdUnlock,
            int index)
        {
            GameObject itemObj = XOUIBuilder.CreateUIObject("ShopItem_" + title, parent);
            LayoutElement le = itemObj.AddComponent<LayoutElement>();
            le.minHeight = 108f;
            le.preferredHeight = 108f;

            Image itemBg = itemObj.AddComponent<Image>();
            itemBg.sprite = XOProceduralAssets.CardPanelSprite;
            itemBg.type = Image.Type.Sliced;
            itemBg.color = Color.white;

            RectTransform itemRT = itemObj.GetComponent<RectTransform>();
            GameObject previewBox = XOUIBuilder.CreateUIObject("PreviewSwatch", itemRT);
            RectTransform prevRT = previewBox.GetComponent<RectTransform>();
            prevRT.anchorMin = new Vector2(0f, 0.5f);
            prevRT.anchorMax = new Vector2(0f, 0.5f);
            prevRT.pivot = new Vector2(0f, 0.5f);
            prevRT.anchoredPosition = new Vector2(16f, 0f);
            prevRT.sizeDelta = new Vector2(120f, 82f);
            Image prevImg = previewBox.AddComponent<Image>();
            prevImg.sprite = XOProceduralAssets.CardPanelSprite;
            prevImg.type = Image.Type.Sliced;
            prevImg.color = swatchBg;

            Image ico1 = XOUIBuilder.CreateIconImage(prevRT, leftIcon, new Vector2(0.5f, 0.5f), new Vector2(-26f, 0f), new Vector2(46f, 46f));
            ico1.color = leftTint;
            Image ico2 = XOUIBuilder.CreateIconImage(prevRT, rightIcon, new Vector2(0.5f, 0.5f), new Vector2(26f, 0f), new Vector2(46f, 46f));
            ico2.color = rightTint;

            Text nameTxt = XOUIBuilder.CreateText(itemRT, _font, title, 21, FontStyle.Bold, TextAnchor.UpperLeft);
            nameTxt.rectTransform.offsetMin = new Vector2(154f, 0f);
            nameTxt.rectTransform.offsetMax = new Vector2(-240f, -14f);

            Text subTxt = XOUIBuilder.CreateText(itemRT, _font, subtitle, 14, FontStyle.Normal, TextAnchor.MiddleLeft);
            subTxt.color = XOProceduralAssets.ColMuted;
            subTxt.rectTransform.offsetMin = new Vector2(154f, 0f);
            subTxt.rectTransform.offsetMax = new Vector2(-240f, 0f);

            Text statusTxt = XOUIBuilder.CreateText(itemRT, _font, "", 14, FontStyle.Bold, TextAnchor.LowerLeft);
            statusTxt.rectTransform.offsetMin = new Vector2(154f, 14f);
            statusTxt.rectTransform.offsetMax = new Vector2(-240f, 0f);

            Button primaryBtn = XOUIBuilder.Create3DButton(itemRT, _font, "EQUIP", new Vector2(210f, 44f), XOProceduralAssets.ColMint, XOProceduralAssets.ColInk, 17);
            RectTransform btn1RT = primaryBtn.GetComponent<RectTransform>();
            btn1RT.anchorMin = new Vector2(1f, 0.5f);
            btn1RT.anchorMax = new Vector2(1f, 0.5f);
            btn1RT.pivot = new Vector2(1f, 0.5f);
            btn1RT.anchoredPosition = new Vector2(-16f, 24f);
            primaryBtn.onClick.AddListener(onPrimary);

            Button adUnlockBtn = XOUIBuilder.CreateIcon3DButton(itemRT, _font, XOProceduralAssets.IconVideoAd, "Free via Ad", new Vector2(210f, 42f), XOProceduralAssets.ColSun, XOProceduralAssets.ColInk, 16);
            RectTransform btn2RT = adUnlockBtn.GetComponent<RectTransform>();
            btn2RT.anchorMin = new Vector2(1f, 0.5f);
            btn2RT.anchorMax = new Vector2(1f, 0.5f);
            btn2RT.pivot = new Vector2(1f, 0.5f);
            btn2RT.anchoredPosition = new Vector2(-16f, -24f);
            adUnlockBtn.onClick.AddListener(onAdUnlock);

            return new ShopCardRefs
            {
                index = index,
                cardBg = itemBg,
                statusText = statusTxt,
                selectOrCoinBtn = primaryBtn,
                selectOrCoinBtnText = primaryBtn.GetComponentInChildren<Text>(),
                watchAdUnlockBtn = adUnlockBtn
            };
        }

        private void CreateSkillBuyRow(RectTransform parent, Sprite icon, string title, string subtitle, int cost, Func<int> getStock, Action onBuy)
        {
            GameObject itemObj = XOUIBuilder.CreateUIObject("SkillBuy_" + title, parent);
            LayoutElement le = itemObj.AddComponent<LayoutElement>();
            le.minHeight = 100f;
            le.preferredHeight = 100f;

            Image bg = itemObj.AddComponent<Image>();
            bg.sprite = XOProceduralAssets.CardPanelSprite;
            bg.type = Image.Type.Sliced;
            bg.color = Color.white;

            RectTransform itemRT = itemObj.GetComponent<RectTransform>();
            GameObject iconBox = XOUIBuilder.CreateUIObject("IconBox", itemRT);
            RectTransform ibRT = iconBox.GetComponent<RectTransform>();
            ibRT.anchorMin = new Vector2(0f, 0.5f);
            ibRT.anchorMax = new Vector2(0f, 0.5f);
            ibRT.pivot = new Vector2(0f, 0.5f);
            ibRT.anchoredPosition = new Vector2(16f, 0f);
            ibRT.sizeDelta = new Vector2(68f, 68f);

            Image ibBg = iconBox.AddComponent<Image>();
            ibBg.sprite = XOProceduralAssets.PillSprite;
            ibBg.type = Image.Type.Sliced;
            ibBg.color = XOProceduralAssets.ColTileEdge;
            XOUIBuilder.CreateIconImage(ibRT, icon, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(46f, 46f));

            Text nameTxt = XOUIBuilder.CreateText(itemRT, _font, title, 20, FontStyle.Bold, TextAnchor.UpperLeft);
            nameTxt.rectTransform.offsetMin = new Vector2(98f, 0f);
            nameTxt.rectTransform.offsetMax = new Vector2(-210f, -14f);

            Text subTxt = XOUIBuilder.CreateText(itemRT, _font, subtitle, 14, FontStyle.Normal, TextAnchor.MiddleLeft);
            subTxt.color = XOProceduralAssets.ColMuted;
            subTxt.rectTransform.offsetMin = new Vector2(98f, 0f);
            subTxt.rectTransform.offsetMax = new Vector2(-210f, 0f);

            Text stockTxt = XOUIBuilder.CreateText(itemRT, _font, $"Owned: {getStock()}", 14, FontStyle.Bold, TextAnchor.LowerLeft);
            stockTxt.color = XOProceduralAssets.Hex("#0284C7");
            stockTxt.rectTransform.offsetMin = new Vector2(98f, 14f);
            stockTxt.rectTransform.offsetMax = new Vector2(-210f, 0f);

            Button buyBtn = XOUIBuilder.CreateIcon3DButton(itemRT, _font, XOProceduralAssets.IconCoin, $"Buy ({cost}c)", new Vector2(180f, 48f), XOProceduralAssets.ColMint, XOProceduralAssets.ColInk, 16);
            RectTransform bRT = buyBtn.GetComponent<RectTransform>();
            bRT.anchorMin = new Vector2(1f, 0.5f);
            bRT.anchorMax = new Vector2(1f, 0.5f);
            bRT.pivot = new Vector2(1f, 0.5f);
            bRT.anchoredPosition = new Vector2(-16f, 0f);
            buyBtn.onClick.AddListener(() =>
            {
                if (_store.Coins >= cost)
                {
                    _store.Coins -= cost;
                    onBuy();
                    stockTxt.text = $"Owned: {getStock()}";
                    _onEconomyChanged?.Invoke();
                    _playSfx?.Invoke("reward");
                }
                else
                {
                    _playSfx?.Invoke("lose");
                    _spawnBanner?.Invoke("NOT ENOUGH COINS!", $"You need {cost} coins to buy {title}.", XOProceduralAssets.ColCoral);
                }
            });
        }

        private void UpdateShopCardVisual(ShopCardRefs card, bool unlocked, bool isSelected, int cost)
        {
            card.cardBg.color = isSelected ? XOProceduralAssets.Hex("#ECFDF5") : Color.white;
            Text txt = card.selectOrCoinBtn.GetComponentInChildren<Text>();

            if (unlocked)
            {
                card.statusText.text = isSelected ? "STATUS: EQUIPPED" : "STATUS: UNLOCKED";
                card.statusText.color = isSelected ? XOProceduralAssets.Hex("#059669") : XOProceduralAssets.Hex("#0284C7");
                card.selectOrCoinBtnText.text = isSelected ? "EQUIPPED" : "EQUIP";
                card.selectOrCoinBtn.GetComponent<Image>().color = isSelected ? XOProceduralAssets.ColMint : XOProceduralAssets.ColSky;
                if (txt != null) txt.color = isSelected ? XOProceduralAssets.ColInk : Color.white;
                card.watchAdUnlockBtn.gameObject.SetActive(false);
                card.selectOrCoinBtn.GetComponent<RectTransform>().anchoredPosition = new Vector2(-16f, 0f);
            }
            else
            {
                card.statusText.text = $"LOCKED — {cost} Coins";
                card.statusText.color = XOProceduralAssets.Hex("#D97706");
                card.selectOrCoinBtnText.text = $"BUY ({cost}c)";
                bool canAfford = _store.Coins >= cost;
                card.selectOrCoinBtn.GetComponent<Image>().color = canAfford ? XOProceduralAssets.ColMint : XOProceduralAssets.ColTileEdge;
                if (txt != null) txt.color = canAfford ? XOProceduralAssets.ColInk : XOProceduralAssets.ColMuted;
                card.watchAdUnlockBtn.gameObject.SetActive(true);
                card.selectOrCoinBtn.GetComponent<RectTransform>().anchoredPosition = new Vector2(-16f, 24f);
            }
        }

        private void OnThemePrimaryClicked(int idx)
        {
            XOThemePalette t = XOThemeDatabase.Themes[idx];
            if (XOThemeDatabase.IsThemeUnlocked(t.id))
            {
                _playSfx?.Invoke("click");
                ApplyThemeSelection(idx);
            }
            else if (_store.Coins >= t.coinCost)
            {
                _store.Coins -= t.coinCost;
                XOThemeDatabase.UnlockTheme(t.id);
                _playSfx?.Invoke("reward");
                ApplyThemeSelection(idx);
            }
            else
            {
                OnThemeAdClicked(idx);
            }
        }

        private void OnThemeAdClicked(int idx)
        {
            XOThemePalette t = XOThemeDatabase.Themes[idx];
            _playSfx?.Invoke("click");
            XOPlayableAdsBridge.Instance.ShowRewardedAd(
                $"Unlock '{t.displayName}' Board Theme Permanently",
                onRewarded: () =>
                {
                    XOThemeDatabase.UnlockTheme(t.id);
                    _playSfx?.Invoke("reward");
                    ApplyThemeSelection(idx);
                });
        }

        private void ApplyThemeSelection(int idx)
        {
            XOThemeDatabase.SetSelectedThemeIndex(idx);
            _store.ReloadCosmetics();
            _onCosmeticsChanged?.Invoke();
            Refresh();
        }

        private void OnPieceSkinPrimaryClicked(int idx)
        {
            XOPieceSkin ps = XOThemeDatabase.PieceSkins[idx];
            if (XOThemeDatabase.IsPieceSkinUnlocked(ps.id))
            {
                _playSfx?.Invoke("click");
                ApplyPieceSkinSelection(idx);
            }
            else if (_store.Coins >= ps.coinCost)
            {
                _store.Coins -= ps.coinCost;
                XOThemeDatabase.UnlockPieceSkin(ps.id);
                _playSfx?.Invoke("reward");
                ApplyPieceSkinSelection(idx);
            }
            else
            {
                OnPieceSkinAdClicked(idx);
            }
        }

        private void OnPieceSkinAdClicked(int idx)
        {
            XOPieceSkin ps = XOThemeDatabase.PieceSkins[idx];
            _playSfx?.Invoke("click");
            XOPlayableAdsBridge.Instance.ShowRewardedAd(
                $"Unlock '{ps.displayName}' Piece Skin Permanently",
                onRewarded: () =>
                {
                    XOThemeDatabase.UnlockPieceSkin(ps.id);
                    _playSfx?.Invoke("reward");
                    ApplyPieceSkinSelection(idx);
                });
        }

        private void ApplyPieceSkinSelection(int idx)
        {
            XOThemeDatabase.SetSelectedPieceSkinIndex(idx);
            _store.ReloadCosmetics();
            _onCosmeticsChanged?.Invoke();
            Refresh();
        }
    }
}

