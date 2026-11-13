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
    /// Roguelike Relic Caravan Draft modal: buy/sell passive Caro Relics between Tower floors,
    /// heal hearts, or watch a Rewarded Ad to reroll offered relics.
    /// </summary>
    public sealed class XOCaravanModal
    {
        private readonly Font _font;
        private readonly XOGameStateStore _store;
        private readonly Action<string> _playSfx;
        private readonly Action<string, string, Color> _spawnBanner;
        private readonly Action _onEconomyUpdated;
        private readonly Action _onContinueToNextFloor;

        public GameObject RootObj { get; private set; }
        public RectTransform CardRT { get; private set; }

        private Text _caravanStatusText;
        private Text _equippedTitle;
        private Text _titleText;
        private bool _afterFloor;
        private Transform _offersContainer;
        private RectTransform _equippedContainer;
        private int _rerollCount;
        private List<CaroRelic> _currentOffers = new List<CaroRelic>();

        public XOCaravanModal(
            Font font,
            XOGameStateStore store,
            Action<string> playSfx,
            Action<string, string, Color> spawnBanner,
            Action onEconomyUpdated,
            Action onContinueToNextFloor)
        {
            _font = font;
            _store = store;
            _playSfx = playSfx;
            _spawnBanner = spawnBanner;
            _onEconomyUpdated = onEconomyUpdated;
            _onContinueToNextFloor = onContinueToNextFloor;
        }

        /// <summary>Builds the Caravan Draft &amp; Merchant modal UI.</summary>
        public void Build(Transform canvasTransform)
        {
            RootObj = XOUIBuilder.CreateModalBackdrop("CaravanModal", canvasTransform, out RectTransform rt);
            CardRT = rt;
            rt.sizeDelta = new Vector2(920f, 880f);

            _titleText = XOUIBuilder.CreateModalHeader(rt, _font, XOProceduralAssets.IconRelic, "Relic shop", 29, () =>
            {
                _playSfx?.Invoke("click");
                RootObj.SetActive(false);
                // Opened right after a floor: closing the shop skips it and moves on, like leaving a Balatro shop.
                if (_afterFloor) { _afterFloor = false; _onContinueToNextFloor?.Invoke(); }
            });

            _caravanStatusText = XOUIBuilder.CreateText(rt, _font, "", 20, FontStyle.Bold, TextAnchor.UpperCenter);
            _caravanStatusText.color = XOUIColors.Slate700;
            _caravanStatusText.rectTransform.offsetMin = new Vector2(24f, 0f);
            _caravanStatusText.rectTransform.offsetMax = new Vector2(-24f, -94f);

            // Offers list
            XOUIBuilder.CreateModalScrollView("CaravanOffers", rt, out RectTransform offerRT, out GameObject offersObj);
            offersObj.GetComponent<RectTransform>().offsetMin = new Vector2(24f, 284f);
            offersObj.GetComponent<RectTransform>().offsetMax = new Vector2(-24f, -132f);
            _offersContainer = offerRT;
            VerticalLayoutGroup offersLayout = offerRT.GetComponent<VerticalLayoutGroup>();
            if (offersLayout != null) offersLayout.spacing = 12f;

            _equippedTitle = XOUIBuilder.CreateText(rt, _font, "Equipped relics", 20, FontStyle.Bold, TextAnchor.LowerLeft);
            _equippedTitle.color = XOUIColors.Slate700;
            RectTransform etRT = _equippedTitle.rectTransform;
            etRT.anchorMin = new Vector2(0f, 0f);
            etRT.anchorMax = new Vector2(1f, 0f);
            etRT.pivot = new Vector2(0.5f, 0f);
            etRT.offsetMin = new Vector2(34f, 240f);
            etRT.offsetMax = new Vector2(-34f, 272f);

            GameObject eqRow = XOUIBuilder.CreateUIObject("EquippedRelicsRow", rt);
            RectTransform eqRT = eqRow.GetComponent<RectTransform>();
            eqRT.SetAnchor(new Vector2(0.5f, 0f), new Vector2(0.5f, 0f));
            eqRT.pivot = new Vector2(0.5f, 0f);
            eqRT.anchoredPosition = new Vector2(0f, 104f);
            eqRT.sizeDelta = new Vector2(856f, 128f);

            HorizontalLayoutGroup eqHlg = eqRow.AddComponent<HorizontalLayoutGroup>();
            eqHlg.padding = new RectOffset(0, 0, 0, 0);
            eqHlg.spacing = 14f;
            eqHlg.childControlWidth = true;
            eqHlg.childControlHeight = true;
            eqHlg.childForceExpandWidth = true;
            eqHlg.childForceExpandHeight = true;
            _equippedContainer = eqRT;

            // Bottom actions: reroll (rewarded ad) + next floor
            Button rerollBtn = XOUIBuilder.CreateIcon3DButton(rt, _font, XOProceduralAssets.IconVideoAd, "Watch ad: reroll", new Vector2(300f, 64f), XOProceduralAssets.ColSun, XOProceduralAssets.ColInk, 20);
            RectTransform rrRT = rerollBtn.GetComponent<RectTransform>();
            rrRT.SetAnchor(new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(0f, 0f));
            rrRT.anchoredPosition = new Vector2(32f, 24f);
            rerollBtn.onClick.AddListener(OnRerollClicked);

            Button nextFloorBtn = XOUIBuilder.CreateIcon3DButton(rt, _font, XOProceduralAssets.IconNewGame, "Next floor", new Vector2(500f, 64f), XOProceduralAssets.ColMint, XOProceduralAssets.ColInk, 24);
            RectTransform nfRT = nextFloorBtn.GetComponent<RectTransform>();
            nfRT.SetAnchor(new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(1f, 0f));
            nfRT.anchoredPosition = new Vector2(-32f, 24f);
            nextFloorBtn.onClick.AddListener(() =>
            {
                _playSfx?.Invoke("click");
                RootObj.SetActive(false);
                _afterFloor = false;
                _onContinueToNextFloor?.Invoke();
            });

            RootObj.SetActive(false);
        }

        /// <summary>Opens the shop as the step between two Tower floors. Buying is optional; leaving by either button starts the next floor.</summary>
        public void OpenAfterFloor()
        {
            Open();
            _afterFloor = true;
        }

        public void Open()
        {
            _afterFloor = false;
            _playSfx?.Invoke("click");
            _rerollCount = 0;
            _currentOffers = XORoguelikeRunManager.GetRandomRelicDraft(3, _rerollCount);
            Refresh();
            RootObj.transform.SetAsLastSibling();
            RootObj.SetActive(true);
        }

        public void Refresh()
        {
            if (_caravanStatusText != null)
            {
                _caravanStatusText.text = $"Coins {_store.Coins} · Hearts {XORoguelikeRunManager.Lives}/{XORoguelikeRunManager.MaxLives}";
            }

            ClearChildren(_offersContainer);
            foreach (CaroRelic relic in _currentOffers)
            {
                CreateOfferRow(relic);
            }

            ClearChildren(_equippedContainer);
            int slots = XORoguelikeRunManager.MaxRelicSlots;
            int filled = 0;
            foreach (CaroRelicType rType in XORoguelikeRunManager.EquippedRelics)
            {
                CaroRelic relic = XORoguelikeRunManager.GetRelicData(rType);
                if (relic == null) continue;
                CreateEquippedChip(relic);
                filled++;
            }
            for (int i = filled; i < slots; i++) CreateEmptySlot();

            if (_equippedTitle != null)
                _equippedTitle.text = $"Equipped relics {XORoguelikeRunManager.EquippedRelics.Count}/{slots}  <size=15><color=#64748B>tap a relic to sell it</color></size>";
        }

        private void CreateOfferRow(CaroRelic relic)
        {
            GameObject row = XOUIBuilder.CreateUIObject("RelicOffer_" + relic.id, _offersContainer);
            LayoutElement le = row.AddComponent<LayoutElement>();
            le.minHeight = 124f;
            le.preferredHeight = 124f;

            Image bg = row.AddComponent<Image>();
            bg.sprite = XOProceduralAssets.CardPanelSprite;
            bg.type = Image.Type.Sliced;
            bg.color = Color.white;

            RectTransform rowRT = row.GetComponent<RectTransform>();
            Color rarity = relic.GetRarityColor();

            // Rarity-tinted tile behind the icon.
            GameObject tile = XOUIBuilder.CreateUIObject("IconTile", rowRT);
            RectTransform tileRT = tile.GetComponent<RectTransform>();
            tileRT.SetAnchor(new Vector2(0f, 0.5f), new Vector2(0f, 0.5f));
            tileRT.anchoredPosition = new Vector2(62f, 0f);
            tileRT.sizeDelta = new Vector2(88f, 88f);
            Image tileImg = tile.AddComponent<Image>();
            tileImg.sprite = XOProceduralAssets.CardPanelSprite;
            tileImg.type = Image.Type.Sliced;
            tileImg.color = Color.Lerp(rarity, Color.white, 0.72f);
            tileImg.raycastTarget = false;
            XOUIBuilder.CreateIconImage(tileRT, relic.GetIcon(), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(56f, 56f));

            Text title = XOUIBuilder.CreateText(rowRT, _font, relic.name, 24, FontStyle.Bold, TextAnchor.UpperLeft);
            title.rectTransform.offsetMin = new Vector2(124f, 0f);
            title.rectTransform.offsetMax = new Vector2(-206f, -12f);

            string rarityHex = ColorUtility.ToHtmlStringRGB(Color.Lerp(rarity, Color.black, 0.35f));
            Text desc = XOUIBuilder.CreateText(rowRT, _font, $"<color=#{rarityHex}><b>{relic.rarity}</b></color>\n{relic.description}", 16, FontStyle.Normal, TextAnchor.UpperLeft);
            desc.color = XOUIColors.Slate600;
            desc.rectTransform.offsetMin = new Vector2(124f, 10f);
            desc.rectTransform.offsetMax = new Vector2(-206f, -44f);

            bool owned = XORoguelikeRunManager.HasRelic(relic.type);
            bool full = XORoguelikeRunManager.EquippedRelics.Count >= XORoguelikeRunManager.MaxRelicSlots;
            bool affordable = _store.Coins >= relic.cost;
            bool canBuy = !owned && !full && affordable;

            Button buyBtn;
            if (owned || full)
            {
                buyBtn = XOUIBuilder.Create3DButton(rowRT, _font, owned ? "Equipped" : "Slots full", new Vector2(176f, 60f), XOProceduralAssets.ColTileEdge, XOProceduralAssets.ColMuted, 18);
            }
            else
            {
                buyBtn = XOUIBuilder.CreateIcon3DButton(rowRT, _font, XOProceduralAssets.IconCoin, relic.cost.ToString(), new Vector2(176f, 60f),
                    canBuy ? XOProceduralAssets.ColMint : XOProceduralAssets.ColTileEdge,
                    canBuy ? XOProceduralAssets.ColInk : XOProceduralAssets.ColCoral, 24);
            }
            RectTransform bRT = buyBtn.GetComponent<RectTransform>();
            bRT.SetAnchor(new Vector2(1f, 0.5f), new Vector2(1f, 0.5f));
            bRT.anchoredPosition = new Vector2(-98f, 0f);
            buyBtn.interactable = !owned;

            buyBtn.onClick.AddListener(() => OnBuyRelicClicked(relic));
        }

        private void CreateEmptySlot()
        {
            GameObject slot = XOUIBuilder.CreateUIObject("EmptySlot", _equippedContainer);
            Image img = slot.AddComponent<Image>();
            img.sprite = XOProceduralAssets.RecessedSlotSprite;
            img.type = Image.Type.Sliced;
            img.color = XOUIColors.Slate100;
            img.raycastTarget = false;

            Text label = XOUIBuilder.CreateText(slot.GetComponent<RectTransform>(), _font, "Empty slot", 16, FontStyle.Bold, TextAnchor.MiddleCenter);
            label.color = XOProceduralAssets.ColMuted;
        }

        private void CreateEquippedChip(CaroRelic relic)
        {
            Button chipBtn = XOUIBuilder.Create3DButton(_equippedContainer, _font, "", new Vector2(154f, 120f), Color.Lerp(relic.GetRarityColor(), Color.white, 0.6f), XOProceduralAssets.ColInk, 15);
            RectTransform cRT = chipBtn.GetComponent<RectTransform>();
            XOUIBuilder.CreateIconImage(cRT, relic.GetIcon(), new Vector2(0.5f, 0.64f), Vector2.zero, new Vector2(48f, 48f));

            Text nTxt = XOUIBuilder.CreateText(cRT, _font, relic.name, 16, FontStyle.Bold, TextAnchor.LowerCenter);
            nTxt.resizeTextForBestFit = true;
            nTxt.resizeTextMinSize = 11;
            nTxt.resizeTextMaxSize = 16;
            nTxt.rectTransform.offsetMin = new Vector2(8f, 10f);
            nTxt.rectTransform.offsetMax = new Vector2(-8f, 0f);
            nTxt.rectTransform.anchorMax = new Vector2(1f, 0.38f);

            chipBtn.onClick.AddListener(() =>
            {
                if (XORoguelikeRunManager.SellRelic(relic.type, out int refund))
                {
                    _store.Coins += refund;
                    _playSfx?.Invoke("chest");
                    _onEconomyUpdated?.Invoke();
                    Refresh();
                }
            });
        }

        private void OnBuyRelicClicked(CaroRelic relic)
        {
            if (XORoguelikeRunManager.HasRelic(relic.type)) return;

            if (XORoguelikeRunManager.EquippedRelics.Count >= XORoguelikeRunManager.MaxRelicSlots)
            {
                _playSfx?.Invoke("lose");
                _spawnBanner?.Invoke("Relic slots full!", "Sell an existing relic first to free up a slot.", XOProceduralAssets.ColCoral);
                return;
            }

            if (_store.Coins < relic.cost)
            {
                _playSfx?.Invoke("lose");
                _spawnBanner?.Invoke("Not enough coins!", $"You need {relic.cost} coins for {relic.name}.", XOProceduralAssets.ColCoral);
                return;
            }

            if (XORoguelikeRunManager.EquipRelic(relic.type))
            {
                _store.Coins -= relic.cost;
                _playSfx?.Invoke("reward");
                _onEconomyUpdated?.Invoke();
                Refresh();
            }
        }

        private void OnRerollClicked()
        {
            _playSfx?.Invoke("click");
            XOPlayableAdsBridge.Instance.ShowRewardedAd(
                "Reroll Caravan Relic Draft Offerings",
                onRewarded: () =>
                {
                    _rerollCount++;
                    _currentOffers = XORoguelikeRunManager.GetRandomRelicDraft(3, _rerollCount);
                    _playSfx?.Invoke("reward");
                    Refresh();
                });
        }

        private static void ClearChildren(Transform container)
        {
            if (container == null) return;
            for (int i = container.childCount - 1; i >= 0; i--)
            {
                UnityEngine.Object.Destroy(container.GetChild(i).gameObject);
            }
        }
    }
}
