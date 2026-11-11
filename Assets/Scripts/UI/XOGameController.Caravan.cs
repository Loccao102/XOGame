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
        // TOWER CARAVAN & ROGUELIKE DRAFT MODAL (BALATRO STYLE SHOP)
        // =====================================================================

        public void OpenCaravanDraftModal()
        {
            PlaySfx("reward");
            _caravanRerollCount = 0;
            if (_resultModalRoot != null) _resultModalRoot.SetActive(false);
            if (_caravanModalRoot != null)
            {
                _caravanModalRoot.SetActive(true);
                RefreshCaravanDraftUI();
            }
        }

        private void BuildCaravanDraftModal(Transform canvasTransform)
        {
            _caravanModalRoot = CreateModalBackdrop("CaravanModal", canvasTransform, out RectTransform cardRT);
            _caravanCardRT = cardRT;
            cardRT.sizeDelta = new Vector2(960f, 780f);

            Text titleTxt = CreateText(cardRT, "TOWER CARAVAN — DRAFT A RELIC", 28, FontStyle.Bold, TextAnchor.UpperCenter);
            titleTxt.rectTransform.offsetMin = new Vector2(24f, 0f);
            titleTxt.rectTransform.offsetMax = new Vector2(-24f, -22f);
            titleTxt.color = XOProceduralAssets.ColInk;

            _caravanSubtitleText = CreateText(cardRT, "Choose 1 Relic to power up your run synergies! (Max 4 Slots)", 16, FontStyle.Normal, TextAnchor.UpperCenter);
            _caravanSubtitleText.rectTransform.offsetMin = new Vector2(24f, 0f);
            _caravanSubtitleText.rectTransform.offsetMax = new Vector2(-24f, -58f);
            _caravanSubtitleText.color = XOProceduralAssets.ColMuted;

            // Heart Status & Coins Pill
            GameObject statusPill = CreateUIObject("CaravanStatusPill", cardRT);
            RectTransform spRT = statusPill.GetComponent<RectTransform>();
            spRT.anchorMin = new Vector2(0.5f, 1f);
            spRT.anchorMax = new Vector2(0.5f, 1f);
            spRT.pivot = new Vector2(0.5f, 1f);
            spRT.anchoredPosition = new Vector2(0f, -88f);
            spRT.sizeDelta = new Vector2(580f, 40f);
            Image spBg = statusPill.AddComponent<Image>();
            spBg.sprite = XOProceduralAssets.PillSprite;
            spBg.type = Image.Type.Sliced;
            spBg.color = XOProceduralAssets.ColTileEdge;

            _caravanLivesText = CreateText(spRT, $"Heart: {XORoguelikeRunManager.Lives}/1  |  Seed: {XORoguelikeRunManager.FormattedSeed}  |  Relics: {XORoguelikeRunManager.EquippedRelics.Count}/{XORoguelikeRunManager.MaxRelicSlots}", 15, FontStyle.Bold, TextAnchor.MiddleCenter);
            _caravanLivesText.color = XOProceduralAssets.ColInk;

            // Equipped Relics Strip (with Sell Refund)
            GameObject eqStripObj = CreateUIObject("CaravanEquippedStrip", cardRT);
            RectTransform eqRT = eqStripObj.GetComponent<RectTransform>();
            eqRT.anchorMin = new Vector2(0.5f, 1f);
            eqRT.anchorMax = new Vector2(0.5f, 1f);
            eqRT.pivot = new Vector2(0.5f, 1f);
            eqRT.anchoredPosition = new Vector2(0f, -138f);
            eqRT.sizeDelta = new Vector2(900f, 44f);
            _caravanEquippedContainer = eqStripObj.transform;

            HorizontalLayoutGroup eqHLayout = eqStripObj.AddComponent<HorizontalLayoutGroup>();
            eqHLayout.childAlignment = TextAnchor.MiddleCenter;
            eqHLayout.spacing = 10f;
            eqHLayout.childControlWidth = false;
            eqHLayout.childControlHeight = true;
            eqHLayout.childForceExpandWidth = false;
            eqHLayout.childForceExpandHeight = true;

            // Relic 3-Card Container
            GameObject relicsContainer = CreateUIObject("CaravanRelicCards", cardRT);
            _caravanRelicsContainer = relicsContainer.transform;
            RectTransform rcRT = relicsContainer.GetComponent<RectTransform>();
            rcRT.anchorMin = new Vector2(0f, 0f);
            rcRT.anchorMax = new Vector2(1f, 1f);
            rcRT.offsetMin = new Vector2(30f, 96f);
            rcRT.offsetMax = new Vector2(-30f, -192f);

            HorizontalLayoutGroup hLayout = relicsContainer.AddComponent<HorizontalLayoutGroup>();
            hLayout.spacing = 16f;
            hLayout.childControlWidth = true;
            hLayout.childControlHeight = true;
            hLayout.childForceExpandWidth = true;
            hLayout.childForceExpandHeight = true;

            // Bottom Buttons: Reroll (10c), Skip/Continue
            _caravanRerollBtn = CreateIcon3DButton(cardRT, XOProceduralAssets.IconUndo, "Reroll Relics (15c)", new Vector2(230f, 52f), XOProceduralAssets.ColSun, XOProceduralAssets.ColInk, 15);
            RectTransform rbRT = _caravanRerollBtn.GetComponent<RectTransform>();
            rbRT.anchorMin = new Vector2(0f, 0f);
            rbRT.anchorMax = new Vector2(0f, 0f);
            rbRT.pivot = new Vector2(0f, 0f);
            rbRT.anchoredPosition = new Vector2(32f, 32f);
            _caravanRerollBtn.onClick.AddListener(() =>
            {
                if (Coins >= 15)
                {
                    Coins -= 15;
                    _caravanRerollCount++;
                    PlaySfx("click");
                    RefreshCaravanDraftUI();
                }
                else
                {
                    PlaySfx("lose");
                }
            });

            _caravanContinueBtn = CreateIcon3DButton(cardRT, XOProceduralAssets.IconNewGame, "Next Floor >", new Vector2(220f, 52f), XOProceduralAssets.ColMint, XOProceduralAssets.ColInk, 16);
            RectTransform cbRT = _caravanContinueBtn.GetComponent<RectTransform>();
            cbRT.anchorMin = new Vector2(1f, 0f);
            cbRT.anchorMax = new Vector2(1f, 0f);
            cbRT.pivot = new Vector2(1f, 0f);
            cbRT.anchoredPosition = new Vector2(-32f, 32f);
            _caravanContinueBtn.onClick.AddListener(() =>
            {
                PlaySfx("click");
                _caravanModalRoot.SetActive(false);
                StartNextTowerFloor();
            });

            _caravanModalRoot.SetActive(false);
        }

        private void RefreshCaravanDraftUI()
        {
            if (_caravanRelicsContainer == null) return;
            foreach (Transform child in _caravanRelicsContainer)
            {
                Destroy(child.gameObject);
            }

            if (_caravanLivesText != null)
            {
                _caravanLivesText.text = $"Heart: {XORoguelikeRunManager.Lives}/1  |  Seed: {XORoguelikeRunManager.FormattedSeed}  |  Relics: {XORoguelikeRunManager.EquippedRelics.Count}/{XORoguelikeRunManager.MaxRelicSlots}  |  Coins: {Coins:N0}";
            }

            // Refresh Equipped Relics Strip with Sell buttons
            if (_caravanEquippedContainer != null)
            {
                foreach (Transform child in _caravanEquippedContainer)
                {
                    Destroy(child.gameObject);
                }

                if (XORoguelikeRunManager.EquippedRelics.Count == 0)
                {
                    GameObject emptyObj = CreateUIObject("EmptyPrompt", _caravanEquippedContainer);
                    RectTransform epRT = emptyObj.GetComponent<RectTransform>();
                    epRT.sizeDelta = new Vector2(400f, 36f);
                    Text t = CreateText(epRT, "Equipped Relics: None (0/4 Slots Used)", 14, FontStyle.Italic, TextAnchor.MiddleCenter);
                    t.color = XOProceduralAssets.ColMuted;
                }
                else
                {
                    for (int i = 0; i < XORoguelikeRunManager.EquippedRelics.Count; i++)
                    {
                        CaroRelicType relicType = XORoguelikeRunManager.EquippedRelics[i];
                        CaroRelic relic = XORoguelikeRunManager.GetRelicData(relicType);
                        if (relic == null) continue;

                        int refund = relic.cost / 2;
                        GameObject eqItem = CreateUIObject("Eq_" + relic.id, _caravanEquippedContainer);
                        RectTransform itemRT = eqItem.GetComponent<RectTransform>();
                        itemRT.sizeDelta = new Vector2(210f, 40f);

                        Image itemBg = eqItem.AddComponent<Image>();
                        itemBg.sprite = XOProceduralAssets.PillSprite;
                        itemBg.type = Image.Type.Sliced;
                        itemBg.color = XOProceduralAssets.ColTileEdge;

                        CreateIconImage(itemRT, relic.GetIcon(), new Vector2(0f, 0.5f), new Vector2(18f, 0f), new Vector2(24f, 24f));

                        Text label = CreateText(itemRT, relic.name, 12, FontStyle.Bold, TextAnchor.MiddleLeft);
                        label.rectTransform.offsetMin = new Vector2(34f, 0f);
                        label.rectTransform.offsetMax = new Vector2(-92f, 0f);
                        label.color = XOProceduralAssets.ColInk;

                        Button sellBtn = CreateIcon3DButton(itemRT, XOProceduralAssets.IconCoin, $"Sell (+{refund}c)", new Vector2(90f, 32f), XOProceduralAssets.ColCoral, Color.white, 11);
                        RectTransform sRT = sellBtn.GetComponent<RectTransform>();
                        sRT.anchorMin = new Vector2(1f, 0.5f);
                        sRT.anchorMax = new Vector2(1f, 0.5f);
                        sRT.pivot = new Vector2(1f, 0.5f);
                        sRT.anchoredPosition = new Vector2(-4f, 0f);

                        sellBtn.onClick.AddListener(() =>
                        {
                            if (XORoguelikeRunManager.SellRelic(relicType, out int coinGain))
                            {
                                Coins += coinGain;
                                PlaySfx("reward");
                                SpawnTacticalCalloutBanner("RELIC SOLD!", $"Sold {relic.name} for +{coinGain} Coins!", XOProceduralAssets.ColSun);
                                RefreshCaravanDraftUI();
                            }
                        });
                    }
                }
            }

            List<CaroRelic> draft = XORoguelikeRunManager.GetRandomRelicDraft(3, _caravanRerollCount);
            for (int i = 0; i < draft.Count; i++)
            {
                CaroRelic relic = draft[i];
                CreateCaravanRelicCard(_caravanRelicsContainer, relic);
            }
        }

        private void CreateCaravanRelicCard(Transform parent, CaroRelic relic)
        {
            GameObject cardObj = CreateUIObject("RelicCard_" + relic.id, parent);
            RectTransform cRT = cardObj.GetComponent<RectTransform>();

            Image bg = cardObj.AddComponent<Image>();
            bg.sprite = XOProceduralAssets.CardPanelSprite;
            bg.type = Image.Type.Sliced;
            bg.color = Color.white;

            // Rarity Top Stripe / Badge
            GameObject rarityBadge = CreateUIObject("RarityBadge", cRT);
            RectTransform rbRT = rarityBadge.GetComponent<RectTransform>();
            rbRT.anchorMin = new Vector2(0.5f, 1f);
            rbRT.anchorMax = new Vector2(0.5f, 1f);
            rbRT.pivot = new Vector2(0.5f, 1f);
            rbRT.anchoredPosition = new Vector2(0f, -14f);
            rbRT.sizeDelta = new Vector2(140f, 28f);
            Image rbBg = rarityBadge.AddComponent<Image>();
            rbBg.sprite = XOProceduralAssets.PillSprite;
            rbBg.type = Image.Type.Sliced;
            rbBg.color = relic.GetRarityColor();

            Text rText = CreateText(rbRT, relic.rarity.ToString().ToUpper(), 13, FontStyle.Bold, TextAnchor.MiddleCenter);
            rText.color = (relic.rarity == CaroRelicRarity.Common || relic.rarity == CaroRelicRarity.Rare) ? Color.white : XOProceduralAssets.ColInk;

            // Relic Large Vector Icon
            CreateIconImage(cRT, relic.GetIcon(), new Vector2(0.5f, 1f), new Vector2(0f, -74f), new Vector2(64f, 64f));

            // Name
            Text nameTxt = CreateText(cRT, relic.name, 19, FontStyle.Bold, TextAnchor.UpperCenter);
            nameTxt.rectTransform.offsetMin = new Vector2(10f, 0f);
            nameTxt.rectTransform.offsetMax = new Vector2(-10f, -114f);
            nameTxt.color = XOProceduralAssets.ColInk;

            // Description
            Text descTxt = CreateText(cRT, relic.description, 14, FontStyle.Normal, TextAnchor.MiddleCenter);
            descTxt.rectTransform.offsetMin = new Vector2(12f, 70f);
            descTxt.rectTransform.offsetMax = new Vector2(-12f, -140f);
            descTxt.color = XOProceduralAssets.ColInk;

            // Draft Button
            bool canDraft = XORoguelikeRunManager.EquippedRelics.Count < XORoguelikeRunManager.MaxRelicSlots;
            string btnLabel = canDraft ? "DRAFT (FREE)" : "SLOTS FULL (4/4)";
            Color btnColor = canDraft ? XOProceduralAssets.ColMint : XOProceduralAssets.ColTileEdge;
            Color txtColor = canDraft ? XOProceduralAssets.ColInk : XOProceduralAssets.ColMuted;

            Button draftBtn = CreateIcon3DButton(cRT, XOProceduralAssets.IconRelic, btnLabel, new Vector2(220f, 48f), btnColor, txtColor, 15);
            RectTransform dbRT = draftBtn.GetComponent<RectTransform>();
            dbRT.anchorMin = new Vector2(0.5f, 0f);
            dbRT.anchorMax = new Vector2(0.5f, 0f);
            dbRT.pivot = new Vector2(0.5f, 0f);
            dbRT.anchoredPosition = new Vector2(0f, 16f);

            draftBtn.onClick.AddListener(() =>
            {
                if (!canDraft)
                {
                    PlaySfx("lose");
                    SpawnTacticalCalloutBanner("SLOTS FULL!", "Sell an equipped relic in the bar above first to free a slot!", XOProceduralAssets.ColCoral);
                    return;
                }
                if (XORoguelikeRunManager.EquipRelic(relic.type))
                {
                    PlaySfx("reward");
                    _caravanModalRoot.SetActive(false);
                    StartNextTowerFloor();
                }
                else
                {
                    PlaySfx("lose");
                }
            });
        }

        public void StartNextTowerFloor()
        {
            if (XORoguelikeRunManager.CurrentFloor < 10)
            {
                XORoguelikeRunManager.CurrentFloor++;
                StartTowerMatch(XORoguelikeRunManager.CurrentFloor);
            }
            else
            {
                SetScreenState(ScreenState.Lobby);
                PlaySfx("win");
            }
        }
    }
}
