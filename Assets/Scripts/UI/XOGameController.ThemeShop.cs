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
        private void BuildThemeShopModal(Transform canvasTransform)
        {
            _themeModalRoot = CreateModalBackdrop("ThemeShopModal", canvasTransform, out RectTransform cardRT);
            _themeCardRT = cardRT;
            cardRT.sizeDelta = new Vector2(940f, 920f);

            CreateIconImage(cardRT, XOProceduralAssets.IconSkinShop, new Vector2(0.5f, 1f), new Vector2(-215f, -45f), new Vector2(46f, 46f));
            CreateText(cardRT, "SKIN & THEME SHOP", 34, FontStyle.Bold, TextAnchor.UpperCenter)
                .rectTransform.offsetMax = new Vector2(25f, -26f);

            Button closeBtn = Create3DButton(cardRT, "X", new Vector2(50f, 48f), Color.white, XOProceduralAssets.ColInk, 20);
            RectTransform closeRT = closeBtn.GetComponent<RectTransform>();
            closeRT.anchorMin = new Vector2(1f, 1f);
            closeRT.anchorMax = new Vector2(1f, 1f);
            closeRT.anchoredPosition = new Vector2(-36f, -36f);
            closeBtn.onClick.AddListener(() =>
            {
                PlaySfx("click");
                _themeModalRoot.SetActive(false);
            });

            // 3 Shop Tabs: [THEMES (4)] | [SKINS (5)] | [SKILLS (BUY)]
            GameObject tabRow = CreateUIObject("ShopTabsRow", cardRT);
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

            _tabThemesBtn = CreateIcon3DButton(tabRT, XOProceduralAssets.IconSkinShop, "THEMES (4)", new Vector2(270f, 60f), XOProceduralAssets.ColSky, Color.white, 18);
            _tabThemesBtn.onClick.AddListener(() => SwitchShopTab(0));

            _tabPiecesBtn = CreateIcon3DButton(tabRT, XOProceduralAssets.IconCrown, "SKINS (5)", new Vector2(270f, 60f), XOProceduralAssets.ColTileEdge, XOProceduralAssets.ColInk, 18);
            _tabPiecesBtn.onClick.AddListener(() => SwitchShopTab(1));

            _tabSkillsBtn = CreateIcon3DButton(tabRT, XOProceduralAssets.IconBomb, "SKILLS (BUY)", new Vector2(270f, 60f), XOProceduralAssets.ColTileEdge, XOProceduralAssets.ColInk, 18);
            _tabSkillsBtn.onClick.AddListener(() => SwitchShopTab(2));

            // Scrollable Container 1: Board Themes List
            CreateModalScrollView("ThemesScroll", cardRT, out RectTransform tListRT, out _themesScrollViewObj);
            _themesListContainer = tListRT.gameObject;

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
                    t.cellNormalColor,
                    t.xColor,
                    t.oColor,
                    t.displayName,
                    t.subtitle,
                    () => OnThemePrimaryActionClicked(idx),
                    () => OnThemeAdUnlockClicked(idx),
                    idx));
            }

            // Scrollable Container 2: X/O Piece Skins List
            CreateModalScrollView("SkinsScroll", cardRT, out RectTransform pListRT, out _piecesScrollViewObj);
            _piecesListContainer = pListRT.gameObject;

            _pieceSkinCards.Clear();
            var pieceSkins = XOThemeDatabase.PieceSkins;
            for (int i = 0; i < pieceSkins.Length; i++)
            {
                int idx = i;
                XOPieceSkin ps = pieceSkins[i];
                Sprite sx = XOProceduralAssets.GetPieceSkinSprite(ps.id, true);
                Sprite so = XOProceduralAssets.GetPieceSkinSprite(ps.id, false);
                Color cx = (ps.id == "classic_xo") ? XOThemeDatabase.Hex("#22D3EE") : Color.white;
                Color co = (ps.id == "classic_xo") ? XOThemeDatabase.Hex("#FB7185") : Color.white;

                _pieceSkinCards.Add(CreateShopItemRow(
                    pListRT,
                    sx,
                    so,
                    XOThemeDatabase.Hex("#1E293B"),
                    cx,
                    co,
                    ps.displayName,
                    ps.subtitle,
                    () => OnPieceSkinPrimaryActionClicked(idx),
                    () => OnPieceSkinAdUnlockClicked(idx),
                    idx));
            }

            // Scrollable Container 3: Tactical Skill Consumable Shop
            CreateModalScrollView("SkillsScroll", cardRT, out RectTransform sListRT, out _skillsScrollViewObj);
            _skillsListContainer = sListRT.gameObject;

            CreateSkillBuyRow(sListRT, XOProceduralAssets.IconHint, "AI Hint (+1)", "Spot the optimal Gomoku winning or blocking square", 20, () => HintStock, () => { HintStock++; });
            CreateSkillBuyRow(sListRT, XOProceduralAssets.IconUndo, "Undo Move (+1)", "Revert your last move immediately", 20, () => UndoStock, () => { UndoStock++; });
            CreateSkillBuyRow(sListRT, XOProceduralAssets.IconFreeze, "Freeze Tile (+1)", "Lock any empty square for 3 turns", 30, () => FreezeStock, () => { FreezeStock++; });
            CreateSkillBuyRow(sListRT, XOProceduralAssets.IconBomb, "Bomb Piece (+1)", "Destroy 1 opponent piece on the board", 35, () => BombStock, () => { BombStock++; });
            CreateSkillBuyRow(sListRT, XOProceduralAssets.IconSwap, "Swap Piece (+1)", "Convert 1 opponent piece into your X", 40, () => SwapStock, () => { SwapStock++; });

            _piecesScrollViewObj.SetActive(false);
            _skillsScrollViewObj.SetActive(false);
            _themeModalRoot.SetActive(false);
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
            GameObject itemObj = CreateUIObject("ShopItem_" + title, parent);
            LayoutElement le = itemObj.AddComponent<LayoutElement>();
            le.minHeight = 108f;
            le.preferredHeight = 108f;
            le.flexibleHeight = 0f;

            Image itemBg = itemObj.AddComponent<Image>();
            itemBg.sprite = XOProceduralAssets.CardPanelSprite;
            itemBg.type = Image.Type.Sliced;
            itemBg.color = Color.white;

            RectTransform itemRT = itemObj.GetComponent<RectTransform>();

            // Dual-Icon Swatch Preview Box
            GameObject previewBox = CreateUIObject("PreviewSwatch", itemRT);
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

            Image ico1 = CreateIconImage(prevRT, leftIcon, new Vector2(0.5f, 0.5f), new Vector2(-26f, 0f), new Vector2(46f, 46f));
            ico1.color = leftTint;
            Image ico2 = CreateIconImage(prevRT, rightIcon, new Vector2(0.5f, 0.5f), new Vector2(26f, 0f), new Vector2(46f, 46f));
            ico2.color = rightTint;

            Text nameTxt = CreateText(itemRT, title, 21, FontStyle.Bold, TextAnchor.UpperLeft);
            nameTxt.rectTransform.offsetMin = new Vector2(154f, 0f);
            nameTxt.rectTransform.offsetMax = new Vector2(-240f, -14f);
            nameTxt.color = XOProceduralAssets.ColInk;

            Text subTxt = CreateText(itemRT, subtitle, 14, FontStyle.Normal, TextAnchor.MiddleLeft);
            subTxt.rectTransform.offsetMin = new Vector2(154f, 0f);
            subTxt.rectTransform.offsetMax = new Vector2(-240f, 0f);
            subTxt.color = XOProceduralAssets.ColMuted;

            Text statusTxt = CreateText(itemRT, "", 14, FontStyle.Bold, TextAnchor.LowerLeft);
            statusTxt.rectTransform.offsetMin = new Vector2(154f, 14f);
            statusTxt.rectTransform.offsetMax = new Vector2(-240f, 0f);

            Button coinOrSelectBtn = Create3DButton(itemRT, "EQUIP", new Vector2(210f, 44f), XOProceduralAssets.ColMint, XOProceduralAssets.ColInk, 17);
            RectTransform btn1RT = coinOrSelectBtn.GetComponent<RectTransform>();
            btn1RT.anchorMin = new Vector2(1f, 0.5f);
            btn1RT.anchorMax = new Vector2(1f, 0.5f);
            btn1RT.pivot = new Vector2(1f, 0.5f);
            btn1RT.anchoredPosition = new Vector2(-16f, 24f);
            coinOrSelectBtn.onClick.AddListener(onPrimary);

            Button adUnlockBtn = CreateIcon3DButton(itemRT, XOProceduralAssets.IconVideoAd, "Free via Ad", new Vector2(210f, 42f), XOProceduralAssets.ColSun, XOProceduralAssets.ColInk, 16);
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
                selectOrCoinBtn = coinOrSelectBtn,
                selectOrCoinBtnText = coinOrSelectBtn.GetComponentInChildren<Text>(),
                watchAdUnlockBtn = adUnlockBtn
            };
        }

        // =====================================================================
        // 2-TAB SKIN & THEME SHOP (BOARD THEMES + X/O PIECE SKINS)
        // =====================================================================

        private void OpenThemeShopModal()
        {
            PlaySfx("click");
            RefreshThemeShopUI();
            _themeModalRoot.transform.SetAsLastSibling();
            _themeModalRoot.SetActive(true);
        }

        private void SwitchShopTab(int tabIdx)
        {
            PlaySfx("click");
            _activeShopTab = tabIdx;
            if (_themesScrollViewObj != null) _themesScrollViewObj.SetActive(tabIdx == 0);
            if (_piecesScrollViewObj != null) _piecesScrollViewObj.SetActive(tabIdx == 1);
            if (_skillsScrollViewObj != null) _skillsScrollViewObj.SetActive(tabIdx == 2);

            Color activeCol = XOProceduralAssets.ColSky;
            Color inactiveCol = XOProceduralAssets.ColTileEdge;
            Color activeTxt = Color.white;
            Color inactiveTxt = XOProceduralAssets.ColInk;

            if (_tabThemesBtn != null)
            {
                _tabThemesBtn.GetComponent<Image>().color = (tabIdx == 0) ? activeCol : inactiveCol;
                var t = _tabThemesBtn.GetComponentInChildren<Text>(); if (t != null) t.color = (tabIdx == 0) ? activeTxt : inactiveTxt;
            }
            if (_tabPiecesBtn != null)
            {
                _tabPiecesBtn.GetComponent<Image>().color = (tabIdx == 1) ? activeCol : inactiveCol;
                var t = _tabPiecesBtn.GetComponentInChildren<Text>(); if (t != null) t.color = (tabIdx == 1) ? activeTxt : inactiveTxt;
            }
            if (_tabSkillsBtn != null)
            {
                _tabSkillsBtn.GetComponent<Image>().color = (tabIdx == 2) ? activeCol : inactiveCol;
                var t = _tabSkillsBtn.GetComponentInChildren<Text>(); if (t != null) t.color = (tabIdx == 2) ? activeTxt : inactiveTxt;
            }
            RefreshThemeShopUI();
        }

        private void CreateSkillBuyRow(RectTransform parent, Sprite icon, string title, string subtitle, int cost, System.Func<int> getStock, System.Action onBuy)
        {
            GameObject itemObj = CreateUIObject("SkillBuy_" + title, parent);
            LayoutElement le = itemObj.AddComponent<LayoutElement>();
            le.minHeight = 100f;
            le.preferredHeight = 100f;
            le.flexibleHeight = 0f;

            Image bg = itemObj.AddComponent<Image>();
            bg.sprite = XOProceduralAssets.CardPanelSprite;
            bg.type = Image.Type.Sliced;
            bg.color = Color.white;

            RectTransform itemRT = itemObj.GetComponent<RectTransform>();

            // Icon inside pastel recessed box
            GameObject iconBox = CreateUIObject("IconBox", itemRT);
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

            CreateIconImage(ibRT, icon, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(46f, 46f));

            Text nameTxt = CreateText(itemRT, title, 20, FontStyle.Bold, TextAnchor.UpperLeft);
            nameTxt.rectTransform.offsetMin = new Vector2(98f, 0f);
            nameTxt.rectTransform.offsetMax = new Vector2(-210f, -14f);
            nameTxt.color = XOProceduralAssets.ColInk;

            Text subTxt = CreateText(itemRT, subtitle, 14, FontStyle.Normal, TextAnchor.MiddleLeft);
            subTxt.rectTransform.offsetMin = new Vector2(98f, 0f);
            subTxt.rectTransform.offsetMax = new Vector2(-210f, 0f);
            subTxt.color = XOProceduralAssets.ColMuted;

            Text stockTxt = CreateText(itemRT, $"Owned: {getStock()}", 14, FontStyle.Bold, TextAnchor.LowerLeft);
            stockTxt.rectTransform.offsetMin = new Vector2(98f, 14f);
            stockTxt.rectTransform.offsetMax = new Vector2(-210f, 0f);
            stockTxt.color = XOThemeDatabase.Hex("#0284C7");

            Button buyBtn = CreateIcon3DButton(itemRT, XOProceduralAssets.IconCoin, $"Buy ({cost}c)", new Vector2(180f, 48f), XOProceduralAssets.ColMint, XOProceduralAssets.ColInk, 16);
            RectTransform bRT = buyBtn.GetComponent<RectTransform>();
            bRT.anchorMin = new Vector2(1f, 0.5f);
            bRT.anchorMax = new Vector2(1f, 0.5f);
            bRT.pivot = new Vector2(1f, 0.5f);
            bRT.anchoredPosition = new Vector2(-16f, 0f);
            buyBtn.onClick.AddListener(() =>
            {
                if (Coins >= cost)
                {
                    Coins -= cost;
                    onBuy();
                    stockTxt.text = $"Owned: {getStock()}";
                    RefreshHeaderAndProgressionUI();
                    RefreshBottomActionBadges();
                    PlaySfx("reward");
                }
                else
                {
                    PlaySfx("lose");
                    SpawnTacticalCalloutBanner("NOT ENOUGH COINS!", $"You need {cost} coins to buy {title}.", XOProceduralAssets.ColCoral);
                }
            });
        }

        private void RefreshThemeShopUI()
        {
            int selectedThemeIdx = XOThemeDatabase.GetSelectedThemeIndex();
            var themes = XOThemeDatabase.Themes;

            foreach (var card in _themeCards)
            {
                XOThemePalette t = themes[card.index];
                bool unlocked = XOThemeDatabase.IsThemeUnlocked(t.id);
                bool isSelected = (card.index == selectedThemeIdx);
                UpdateShopCardVisual(card, unlocked, isSelected, t.coinCost);
            }

            int selectedPieceIdx = XOThemeDatabase.GetSelectedPieceSkinIndex();
            var pieceSkins = XOThemeDatabase.PieceSkins;

            foreach (var card in _pieceSkinCards)
            {
                XOPieceSkin ps = pieceSkins[card.index];
                bool unlocked = XOThemeDatabase.IsPieceSkinUnlocked(ps.id);
                bool isSelected = (card.index == selectedPieceIdx);
                UpdateShopCardVisual(card, unlocked, isSelected, ps.coinCost);
            }
        }

        private void UpdateShopCardVisual(ShopCardRefs card, bool unlocked, bool isSelected, int cost)
        {
            card.cardBg.color = isSelected ? XOThemeDatabase.Hex("#ECFDF5") : Color.white;

            if (unlocked)
            {
                card.statusText.text = isSelected ? "STATUS: EQUIPPED" : "STATUS: UNLOCKED";
                card.statusText.color = isSelected ? XOThemeDatabase.Hex("#059669") : XOThemeDatabase.Hex("#0284C7");

                card.selectOrCoinBtnText.text = isSelected ? "EQUIPPED" : "EQUIP";
                card.selectOrCoinBtn.GetComponent<Image>().color = isSelected ? XOProceduralAssets.ColMint : XOProceduralAssets.ColSky;
                var txt = card.selectOrCoinBtn.GetComponentInChildren<Text>();
                if (txt != null) txt.color = isSelected ? XOProceduralAssets.ColInk : Color.white;
                card.watchAdUnlockBtn.gameObject.SetActive(false);
                card.selectOrCoinBtn.GetComponent<RectTransform>().anchoredPosition = new Vector2(-16f, 0f);
            }
            else
            {
                card.statusText.text = $"LOCKED — {cost} Coins";
                card.statusText.color = XOThemeDatabase.Hex("#D97706");

                card.selectOrCoinBtnText.text = $"BUY ({cost}c)";
                bool canAfford = (Coins >= cost);
                card.selectOrCoinBtn.GetComponent<Image>().color = canAfford ? XOProceduralAssets.ColMint : XOProceduralAssets.ColTileEdge;
                var txt = card.selectOrCoinBtn.GetComponentInChildren<Text>();
                if (txt != null) txt.color = canAfford ? XOProceduralAssets.ColInk : XOProceduralAssets.ColMuted;
                card.watchAdUnlockBtn.gameObject.SetActive(true);
                card.selectOrCoinBtn.GetComponent<RectTransform>().anchoredPosition = new Vector2(-16f, 24f);
            }
        }

        private void OnThemePrimaryActionClicked(int themeIdx)
        {
            XOThemePalette t = XOThemeDatabase.Themes[themeIdx];
            if (XOThemeDatabase.IsThemeUnlocked(t.id))
            {
                PlaySfx("click");
                SelectAndApplyTheme(themeIdx);
            }
            else if (Coins >= t.coinCost)
            {
                Coins -= t.coinCost;
                XOThemeDatabase.UnlockTheme(t.id);
                PlaySfx("reward");
                SelectAndApplyTheme(themeIdx);
            }
            else
            {
                OnThemeAdUnlockClicked(themeIdx);
            }
        }

        private void OnThemeAdUnlockClicked(int themeIdx)
        {
            XOThemePalette t = XOThemeDatabase.Themes[themeIdx];
            PlaySfx("click");
            XOPlayableAdsBridge.Instance.ShowRewardedAd(
                $"Unlock '{t.displayName}' Board Theme Permanently",
                onRewarded: () =>
                {
                    XOThemeDatabase.UnlockTheme(t.id);
                    PlaySfx("reward");
                    SelectAndApplyTheme(themeIdx);
                });
        }

        private void SelectAndApplyTheme(int themeIdx)
        {
            XOThemeDatabase.SetSelectedThemeIndex(themeIdx);
            _activeTheme = XOThemeDatabase.Themes[themeIdx];
            ApplyThemeToAllUI();
            RefreshThemeShopUI();
        }

        private void OnPieceSkinPrimaryActionClicked(int skinIdx)
        {
            XOPieceSkin ps = XOThemeDatabase.PieceSkins[skinIdx];
            if (XOThemeDatabase.IsPieceSkinUnlocked(ps.id))
            {
                PlaySfx("click");
                SelectAndApplyPieceSkin(skinIdx);
            }
            else if (Coins >= ps.coinCost)
            {
                Coins -= ps.coinCost;
                XOThemeDatabase.UnlockPieceSkin(ps.id);
                PlaySfx("reward");
                SelectAndApplyPieceSkin(skinIdx);
            }
            else
            {
                OnPieceSkinAdUnlockClicked(skinIdx);
            }
        }

        private void OnPieceSkinAdUnlockClicked(int skinIdx)
        {
            XOPieceSkin ps = XOThemeDatabase.PieceSkins[skinIdx];
            PlaySfx("click");
            XOPlayableAdsBridge.Instance.ShowRewardedAd(
                $"Unlock '{ps.displayName}' Piece Skin Permanently",
                onRewarded: () =>
                {
                    XOThemeDatabase.UnlockPieceSkin(ps.id);
                    PlaySfx("reward");
                    SelectAndApplyPieceSkin(skinIdx);
                });
        }

        private void SelectAndApplyPieceSkin(int skinIdx)
        {
            XOThemeDatabase.SetSelectedPieceSkinIndex(skinIdx);
            _activePieceSkin = XOThemeDatabase.PieceSkins[skinIdx];
            RefreshTurnAndScoreUI();
            RefreshAllCells(animateLastMove: false);
            RefreshThemeShopUI();
        }

        private void ApplyThemeToAllUI()
        {
            if (Camera.main != null)
                Camera.main.backgroundColor = _activeTheme.backgroundTop;

            if (_backgroundTopImg != null)
                _backgroundTopImg.color = _activeTheme.backgroundTop;

            if (_backgroundBottomImg != null)
                _backgroundBottomImg.color = _activeTheme.backgroundBottom;

            if (_headerCardImg != null)
                _headerCardImg.color = _activeTheme.headerCardColor;

            if (_questCardImg != null)
                _questCardImg.color = _activeTheme.panelSurfaceColor;

            if (_modeCardImg != null)
                _modeCardImg.color = _activeTheme.panelSurfaceColor;

            if (_lobbyHeroCardImg != null)
                _lobbyHeroCardImg.color = _activeTheme.panelSurfaceColor;

            if (_actionsCardImg != null)
                _actionsCardImg.color = _activeTheme.panelSurfaceColor;

            if (_missionText != null)
                _missionText.color = _activeTheme.primaryTextColor;

            if (_boardOuterFrameImg != null)
                _boardOuterFrameImg.color = _activeTheme.boardFrameColor;

            if (_statusBannerImg != null)
                _statusBannerImg.color = _activeTheme.panelSurfaceColor;

            if (_statusBannerText != null)
                _statusBannerText.color = _activeTheme.primaryTextColor;

            RefreshHeaderAndProgressionUI();
            RefreshTurnAndScoreUI();
            RefreshAllCells(animateLastMove: false);
        }
    }
}
