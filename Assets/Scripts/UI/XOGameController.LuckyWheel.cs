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
        // LUCKY SPIN WHEEL MODAL (1 FREE SPIN + REWARDED AD SPINS)
        // =====================================================================

        private struct WheelPrizeSlot
        {
            public string title;
            public string subtitle;
            public Color slotColor;
            public Sprite icon;
            public int coins;
            public string itemType;
            public int itemCount;
        }

        private WheelPrizeSlot[] GetWheelPrizes()
        {
            return new WheelPrizeSlot[]
            {
                new WheelPrizeSlot { title = "+15 COINS",      subtitle = "Coin Pouch",       slotColor = XOThemeDatabase.Hex("#0EA5E9"), icon = XOProceduralAssets.IconCoin,  coins = 15,  itemType = "",       itemCount = 0 },
                new WheelPrizeSlot { title = "+1 AI HINT",     subtitle = "Tactical Radar",   slotColor = XOThemeDatabase.Hex("#D97706"), icon = XOProceduralAssets.IconHint,  coins = 5,   itemType = "Hint",   itemCount = 1 },
                new WheelPrizeSlot { title = "+1 BOMB",        subtitle = "Board Blaster",    slotColor = XOThemeDatabase.Hex("#DC2626"), icon = XOProceduralAssets.IconBomb,  coins = 5,   itemType = "Bomb",   itemCount = 1 },
                new WheelPrizeSlot { title = "+30 COINS",      subtitle = "Rich Chest",       slotColor = XOThemeDatabase.Hex("#16A34A"), icon = XOProceduralAssets.IconChest, coins = 30,  itemType = "",       itemCount = 0 },
                new WheelPrizeSlot { title = "+1 SWAP & ICE",  subtitle = "Combo Pack",       slotColor = XOThemeDatabase.Hex("#7C3AED"), icon = XOProceduralAssets.IconSwap,  coins = 10,  itemType = "Combo",  itemCount = 1 },
                new WheelPrizeSlot { title = "JACKPOT +100!",  subtitle = "+100 Coins & All", slotColor = XOThemeDatabase.Hex("#E11D48"), icon = XOProceduralAssets.IconCrown, coins = 100, itemType = "Jackpot",itemCount = 1 }
            };
        }

        private void BuildLuckyWheelModal(Transform canvasTransform)
        {
            _luckyWheelModalRoot = CreateModalBackdrop("LuckyWheelModal", canvasTransform, out RectTransform rt);
            _luckyWheelCardRT = rt;
            rt.sizeDelta = new Vector2(720f, 680f);

            CreateIconImage(rt, XOProceduralAssets.IconLuckyWheel, new Vector2(0.5f, 1f), new Vector2(-215f, -46f), new Vector2(54f, 54f));

            Text title = CreateText(rt, "LUCKY PRIZE SPIN!", 30, FontStyle.Bold, TextAnchor.MiddleCenter);
            title.rectTransform.anchorMin = new Vector2(0f, 1f);
            title.rectTransform.anchorMax = new Vector2(1f, 1f);
            title.rectTransform.pivot = new Vector2(0.5f, 1f);
            title.rectTransform.anchoredPosition = new Vector2(18f, -18f);
            title.rectTransform.sizeDelta = new Vector2(-40f, 54f);
            title.color = XOProceduralAssets.ColInk;

            _wheelStatusText = CreateText(rt, "Spin the Lucky Wheel for Instant Coins, Bombs, Swaps & Jackpot!", 17, FontStyle.Bold, TextAnchor.MiddleCenter);
            _wheelStatusText.rectTransform.anchorMin = new Vector2(0f, 1f);
            _wheelStatusText.rectTransform.anchorMax = new Vector2(1f, 1f);
            _wheelStatusText.rectTransform.pivot = new Vector2(0.5f, 1f);
            _wheelStatusText.rectTransform.anchoredPosition = new Vector2(0f, -74f);
            _wheelStatusText.rectTransform.sizeDelta = new Vector2(-48f, 36f);
            _wheelStatusText.color = XOProceduralAssets.ColMuted;

            // 6 Prize Slots arranged in a 3x2 Grid
            GameObject gridObj = CreateUIObject("WheelSlotsGrid", rt);
            RectTransform gridRT = gridObj.GetComponent<RectTransform>();
            gridRT.anchorMin = new Vector2(0f, 0f);
            gridRT.anchorMax = new Vector2(1f, 1f);
            gridRT.offsetMin = new Vector2(28f, 168f);
            gridRT.offsetMax = new Vector2(-28f, -122f);

            GridLayoutGroup glg = gridObj.AddComponent<GridLayoutGroup>();
            glg.cellSize = new Vector2(210f, 185f);
            glg.spacing = new Vector2(16f, 16f);
            glg.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            glg.constraintCount = 3;
            glg.childAlignment = TextAnchor.MiddleCenter;

            _wheelSlotBgs.Clear();
            WheelPrizeSlot[] prizes = GetWheelPrizes();
            for (int i = 0; i < prizes.Length; i++)
            {
                WheelPrizeSlot p = prizes[i];
                GameObject slotObj = CreateUIObject("PrizeSlot_" + i, gridRT);
                Image slotBg = slotObj.AddComponent<Image>();
                slotBg.sprite = XOProceduralAssets.Button3DSprite;
                slotBg.type = Image.Type.Sliced;
                slotBg.color = p.slotColor;
                _wheelSlotBgs.Add(slotBg);

                RectTransform sRT = slotObj.GetComponent<RectTransform>();
                CreateIconImage(sRT, p.icon, new Vector2(0.5f, 1f), new Vector2(0f, -48f), new Vector2(64f, 64f));

                Text pTitle = CreateText(sRT, p.title, 21, FontStyle.Bold, TextAnchor.MiddleCenter);
                pTitle.rectTransform.anchorMin = new Vector2(0f, 0f);
                pTitle.rectTransform.anchorMax = new Vector2(1f, 0f);
                pTitle.rectTransform.pivot = new Vector2(0.5f, 0f);
                pTitle.rectTransform.anchoredPosition = new Vector2(0f, 44f);
                pTitle.rectTransform.sizeDelta = new Vector2(-12f, 34f);
                pTitle.color = Color.white;

                Text pSub = CreateText(sRT, p.subtitle, 15, FontStyle.Bold, TextAnchor.MiddleCenter);
                pSub.rectTransform.anchorMin = new Vector2(0f, 0f);
                pSub.rectTransform.anchorMax = new Vector2(1f, 0f);
                pSub.rectTransform.pivot = new Vector2(0.5f, 0f);
                pSub.rectTransform.anchoredPosition = new Vector2(0f, 16f);
                pSub.rectTransform.sizeDelta = new Vector2(-12f, 26f);
                pSub.color = XOThemeDatabase.Hex("#FEF08A");
            }

            // Primary Spin Button (Free Spin first, then Bonus Ad Spin)
            _wheelSpinActionBtn = CreateIcon3DButton(
                rt,
                XOProceduralAssets.IconLuckyWheel,
                "SPIN NOW (1 FREE SPIN!)",
                new Vector2(640f, 68f),
                XOThemeDatabase.Hex("#22C55E"),
                Color.white,
                22
            );
            _wheelSpinActionBtnText = _wheelSpinActionBtn.GetComponentInChildren<Text>();
            RectTransform spinRT = _wheelSpinActionBtn.GetComponent<RectTransform>();
            spinRT.anchorMin = new Vector2(0.5f, 0f);
            spinRT.anchorMax = new Vector2(0.5f, 0f);
            spinRT.pivot = new Vector2(0.5f, 0f);
            spinRT.anchoredPosition = new Vector2(0f, 88f);
            _wheelSpinActionBtn.onClick.AddListener(OnSpinWheelClicked);

            // Close Button
            Button closeBtn = Create3DButton(rt, "CLOSE", new Vector2(640f, 54f), XOThemeDatabase.Hex("#334155"), Color.white, 20);
            RectTransform closeRT = closeBtn.GetComponent<RectTransform>();
            closeRT.anchorMin = new Vector2(0.5f, 0f);
            closeRT.anchorMax = new Vector2(0.5f, 0f);
            closeRT.pivot = new Vector2(0.5f, 0f);
            closeRT.anchoredPosition = new Vector2(0f, 22f);
            closeBtn.onClick.AddListener(() =>
            {
                if (_isWheelSpinning) return;
                PlaySfx("click");
                _luckyWheelModalRoot.SetActive(false);
            });

            _luckyWheelModalRoot.SetActive(false);
        }

        private void OpenLuckyWheelModal()
        {
            PlaySfx("click");
            RefreshLuckyWheelUI();
            _luckyWheelModalRoot.transform.SetAsLastSibling();
            _luckyWheelModalRoot.SetActive(true);
        }

        private void RefreshLuckyWheelUI()
        {
            if (_wheelSpinActionBtn == null || _wheelSpinActionBtnText == null) return;
            WheelPrizeSlot[] prizes = GetWheelPrizes();
            for (int i = 0; i < _wheelSlotBgs.Count && i < prizes.Length; i++)
            {
                _wheelSlotBgs[i].color = prizes[i].slotColor;
                _wheelSlotBgs[i].transform.localScale = Vector3.one;
            }

            if (FreeSpinAvailable)
            {
                _wheelSpinActionBtnText.text = "SPIN NOW (1 FREE DAILY SPIN!)";
                _wheelSpinActionBtn.GetComponent<Image>().color = XOThemeDatabase.Hex("#22C55E");
            }
            else
            {
                _wheelSpinActionBtnText.text = "WATCH AD: BONUS LUCKY SPIN!";
                _wheelSpinActionBtn.GetComponent<Image>().color = XOThemeDatabase.Hex("#F59E0B");
            }
        }

        private void OnSpinWheelClicked()
        {
            if (_isWheelSpinning) return;

            if (FreeSpinAvailable)
            {
                FreeSpinAvailable = false;
                StartCoroutine(SpinWheelRoutine());
            }
            else
            {
                PlaySfx("click");
                XOPlayableAdsBridge.Instance.ShowRewardedAd(
                    "Bonus Lucky Wheel Spin (Win up to +250 Coins & Tactical Skills!)",
                    onRewarded: () =>
                    {
                        StartCoroutine(SpinWheelRoutine());
                    });
            }
        }

        private IEnumerator SpinWheelRoutine()
        {
            _isWheelSpinning = true;
            _wheelSpinActionBtn.interactable = false;
            _wheelStatusText.text = "Spinning the Lucky Prize Wheel...";

            WheelPrizeSlot[] prizes = GetWheelPrizes();
            int prizeCount = prizes.Length;
            int winningIdx = UnityEngine.Random.Range(0, prizeCount);
            int totalSteps = prizeCount * 3 + winningIdx;

            for (int step = 0; step <= totalSteps; step++)
            {
                int activeIdx = step % prizeCount;
                for (int i = 0; i < _wheelSlotBgs.Count && i < prizeCount; i++)
                {
                    bool highlight = (i == activeIdx);
                    _wheelSlotBgs[i].color = highlight ? XOThemeDatabase.Hex("#FBBF24") : prizes[i].slotColor;
                    _wheelSlotBgs[i].transform.localScale = highlight ? new Vector3(1.07f, 1.07f, 1f) : Vector3.one;
                }

                PlaySfx("spin_tick");
                if (enableHaptics) XOPlayableAdsBridge.Instance?.TriggerHaptic(10);

                float progress = step / (float)Mathf.Max(1, totalSteps);
                float wait = Mathf.Lerp(0.055f, 0.24f, progress * progress);
                yield return new WaitForSeconds(wait);
            }

            // Grant Prize
            WheelPrizeSlot won = prizes[winningIdx];
            PlaySfx(won.itemType == "Jackpot" ? "jackpot" : "chest");
            if (enableHaptics) XOPlayableAdsBridge.Instance?.TriggerHaptic(45);

            Coins += won.coins;
            if (won.itemType == "Hint") HintStock += won.itemCount;
            else if (won.itemType == "Bomb") BombStock += won.itemCount;
            else if (won.itemType == "Combo") { SwapStock += 1; FreezeStock += 1; }
            else if (won.itemType == "Jackpot")
            {
                HintStock += 1; UndoStock += 1; BombStock += 1; FreezeStock += 1; SwapStock += 1;
            }

            _wheelStatusText.text = $"YOU WON: {won.title} ({won.subtitle})!";
            _wheelSpinActionBtn.interactable = true;
            _isWheelSpinning = false;
            RefreshLuckyWheelUI();
            RefreshHeaderAndProgressionUI();
            RefreshBottomActionBadges();
        }
    }
}
