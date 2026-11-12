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

            XOUIBuilder.CreateModalHeader(rt, _font, XOProceduralAssets.IconRelic, "Relic caravan", 29, () => { _playSfx?.Invoke("click"); RootObj.SetActive(false); });

            _caravanStatusText = XOUIBuilder.CreateText(rt, _font, "", 16, FontStyle.Bold, TextAnchor.UpperCenter);
            _caravanStatusText.color = XOProceduralAssets.ColMuted;
            _caravanStatusText.rectTransform.offsetMin = new Vector2(24f, 0f);
            _caravanStatusText.rectTransform.offsetMax = new Vector2(-24f, -92f);

            // Offers Grid/List
            XOUIBuilder.CreateModalScrollView("CaravanOffers", rt, out RectTransform offerRT, out GameObject offersObj);
            offersObj.GetComponent<RectTransform>().offsetMin = new Vector2(24f, 290f);
            offersObj.GetComponent<RectTransform>().offsetMax = new Vector2(-24f, -128f);
            _offersContainer = offerRT;

            Text eqHeader = XOUIBuilder.CreateText(rt, _font, "Equipped relics (tap to sell)", 14, FontStyle.Bold, TextAnchor.LowerLeft);
            eqHeader.color = XOProceduralAssets.ColMuted;
            eqHeader.rectTransform.offsetMin = new Vector2(32f, 250f);
            eqHeader.rectTransform.offsetMax = new Vector2(-32f, 0f);

            GameObject eqRow = XOUIBuilder.CreateUIObject("EquippedRelicsRow", rt);
            RectTransform eqRT = eqRow.GetComponent<RectTransform>();
            eqRT.SetAnchor(new Vector2(0.5f, 0f), new Vector2(0.5f, 0f));
            eqRT.anchoredPosition = new Vector2(0f, 114f);
            eqRT.sizeDelta = new Vector2(856f, 126f);
            Image eqBg = eqRow.AddComponent<Image>();
            eqBg.sprite = XOProceduralAssets.RecessedSlotSprite;
            eqBg.type = Image.Type.Sliced;
            eqBg.color = XOProceduralAssets.ColTileEdge;

            HorizontalLayoutGroup eqHlg = eqRow.AddComponent<HorizontalLayoutGroup>();
            eqHlg.padding = new RectOffset(14, 14, 10, 10);
            eqHlg.spacing = 12f;
            eqHlg.childControlWidth = false;
            eqHlg.childControlHeight = false;
            eqHlg.childAlignment = TextAnchor.MiddleLeft;
            _equippedContainer = eqRT;

            // Bottom Actions: Reroll + Heal Heart + Next Floor
            Button rerollBtn = XOUIBuilder.CreateIcon3DButton(rt, _font, XOProceduralAssets.IconVideoAd, "Watch ad: reroll relics", new Vector2(270f, 60f), XOProceduralAssets.ColSun, XOProceduralAssets.ColInk, 16);
            RectTransform rrRT = rerollBtn.GetComponent<RectTransform>();
            rrRT.SetAnchor(new Vector2(0f, 0f), new Vector2(0f, 0f));
            rrRT.anchoredPosition = new Vector2(30f, 26f);
            rerollBtn.onClick.AddListener(OnRerollClicked);

            Button nextFloorBtn = XOUIBuilder.CreateIcon3DButton(rt, _font, XOProceduralAssets.IconNewGame, "Next floor", new Vector2(460f, 60f), XOProceduralAssets.ColMint, XOProceduralAssets.ColInk, 19);
            RectTransform nfRT = nextFloorBtn.GetComponent<RectTransform>();
            nfRT.SetAnchor(new Vector2(1f, 0f), new Vector2(1f, 0f));
            nfRT.anchoredPosition = new Vector2(-30f, 26f);
            nextFloorBtn.onClick.AddListener(() =>
            {
                _playSfx?.Invoke("click");
                RootObj.SetActive(false);
                _onContinueToNextFloor?.Invoke();
            });

            RootObj.SetActive(false);
        }

        public void Open()
        {
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
                _caravanStatusText.text = $"Coins: {_store.Coins}  •  Hearts: {XORoguelikeRunManager.Lives}/{XORoguelikeRunManager.MaxLives}  •  Relics: {XORoguelikeRunManager.EquippedRelics.Count}/{XORoguelikeRunManager.MaxRelicSlots}";
            }

            ClearChildren(_offersContainer);
            foreach (CaroRelic relic in _currentOffers)
            {
                CreateOfferRow(relic);
            }

            ClearChildren(_equippedContainer);
            foreach (CaroRelicType rType in XORoguelikeRunManager.EquippedRelics)
            {
                CaroRelic relic = XORoguelikeRunManager.GetRelicData(rType);
                if (relic != null) CreateEquippedChip(relic);
            }
        }

        private void CreateOfferRow(CaroRelic relic)
        {
            GameObject row = XOUIBuilder.CreateUIObject("RelicOffer_" + relic.id, _offersContainer);
            LayoutElement le = row.AddComponent<LayoutElement>();
            le.minHeight = 98f;
            le.preferredHeight = 98f;

            Image bg = row.AddComponent<Image>();
            bg.sprite = XOProceduralAssets.CardPanelSprite;
            bg.type = Image.Type.Sliced;
            bg.color = Color.white;

            RectTransform rowRT = row.GetComponent<RectTransform>();
            XOUIBuilder.CreateIconImage(rowRT, relic.GetIcon(), new Vector2(0f, 0.5f), new Vector2(44f, 0f), new Vector2(54f, 54f));

            Text title = XOUIBuilder.CreateText(rowRT, _font, $"{relic.name} [{relic.rarity}]", 19, FontStyle.Bold, TextAnchor.UpperLeft);
            title.rectTransform.offsetMin = new Vector2(88f, 0f);
            title.rectTransform.offsetMax = new Vector2(-190f, -12f);

            Text desc = XOUIBuilder.CreateText(rowRT, _font, relic.description, 14, FontStyle.Normal, TextAnchor.LowerLeft);
            desc.color = XOProceduralAssets.ColMuted;
            desc.rectTransform.offsetMin = new Vector2(88f, 12f);
            desc.rectTransform.offsetMax = new Vector2(-190f, 0f);

            bool owned = XORoguelikeRunManager.HasRelic(relic.type);
            bool full = XORoguelikeRunManager.EquippedRelics.Count >= XORoguelikeRunManager.MaxRelicSlots;
            bool canBuy = !owned && !full && _store.Coins >= relic.cost;

            string btnLabel = owned ? "Equipped" : (full ? "Slots full" : $"BUY ({relic.cost}c)");
            Color btnCol = owned ? XOProceduralAssets.ColTileEdge : (canBuy ? XOProceduralAssets.ColMint : XOProceduralAssets.ColTileEdge);

            Button buyBtn = XOUIBuilder.Create3DButton(rowRT, _font, btnLabel, new Vector2(158f, 52f), btnCol, canBuy ? XOProceduralAssets.ColInk : XOProceduralAssets.ColMuted, 16);
            RectTransform bRT = buyBtn.GetComponent<RectTransform>();
            bRT.SetAnchor(new Vector2(1f, 0.5f), new Vector2(1f, 0.5f));
            bRT.anchoredPosition = new Vector2(-14f, 0f);
            buyBtn.interactable = !owned;

            buyBtn.onClick.AddListener(() => OnBuyRelicClicked(relic));
        }

        private void CreateEquippedChip(CaroRelic relic)
        {
            Button chipBtn = XOUIBuilder.Create3DButton(_equippedContainer, _font, "", new Vector2(154f, 96f), Color.Lerp(relic.GetRarityColor(), Color.white, 0.65f), XOProceduralAssets.ColInk, 13);
            RectTransform cRT = chipBtn.GetComponent<RectTransform>();
            XOUIBuilder.CreateIconImage(cRT, relic.GetIcon(), new Vector2(0.5f, 1f), new Vector2(0f, -24f), new Vector2(36f, 36f));

            Text nTxt = XOUIBuilder.CreateText(cRT, _font, $"{relic.name}\n(Tap to Sell)", 12, FontStyle.Bold, TextAnchor.LowerCenter);
            nTxt.rectTransform.offsetMin = new Vector2(6f, 8f);
            nTxt.rectTransform.offsetMax = new Vector2(-6f, 0f);

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
