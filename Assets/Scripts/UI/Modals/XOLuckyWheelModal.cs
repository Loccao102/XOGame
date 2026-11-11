using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using XOPlayable.Ads;
using XOPlayable.Visuals;

namespace XOPlayable.UI
{
    /// <summary>
    /// Lucky Prize Wheel modal offering 1 Free Daily Spin plus Rewarded-Ad Bonus Spins across 6 prize slots.
    /// </summary>
    public sealed class XOLuckyWheelModal
    {
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

        private readonly MonoBehaviour _coroutineHost;
        private readonly Font _font;
        private readonly XOGameStateStore _store;
        private readonly Action<string> _playSfx;
        private readonly Action _onEconomyUpdated;
        private readonly Func<bool> _isHapticsEnabled;

        public GameObject RootObj { get; private set; }
        public RectTransform CardRT { get; private set; }

        private Text _wheelStatusText;
        private Button _wheelSpinActionBtn;
        private Text _wheelSpinActionBtnText;
        private readonly List<Image> _wheelSlotBgs = new List<Image>();
        private bool _isWheelSpinning;

        public XOLuckyWheelModal(
            MonoBehaviour coroutineHost,
            Font font,
            XOGameStateStore store,
            Action<string> playSfx,
            Action onEconomyUpdated,
            Func<bool> isHapticsEnabled)
        {
            _coroutineHost = coroutineHost;
            _font = font;
            _store = store;
            _playSfx = playSfx;
            _onEconomyUpdated = onEconomyUpdated;
            _isHapticsEnabled = isHapticsEnabled;
        }

        private static WheelPrizeSlot[] GetWheelPrizes()
        {
            return new[]
            {
                new WheelPrizeSlot { title = "+15 COINS",     subtitle = "Coin Pouch",       slotColor = XOProceduralAssets.Hex("#0EA5E9"), icon = XOProceduralAssets.IconCoin,       coins = 15,  itemType = "",        itemCount = 0 },
                new WheelPrizeSlot { title = "+1 AI HINT",    subtitle = "Tactical Radar",   slotColor = XOProceduralAssets.Hex("#D97706"), icon = XOProceduralAssets.IconHint,       coins = 5,   itemType = "Hint",    itemCount = 1 },
                new WheelPrizeSlot { title = "+1 BOMB",       subtitle = "Board Blaster",    slotColor = XOProceduralAssets.Hex("#DC2626"), icon = XOProceduralAssets.IconBomb,       coins = 5,   itemType = "Bomb",    itemCount = 1 },
                new WheelPrizeSlot { title = "+30 COINS",     subtitle = "Rich Chest",       slotColor = XOProceduralAssets.Hex("#16A34A"), icon = XOProceduralAssets.IconChest,      coins = 30,  itemType = "",        itemCount = 0 },
                new WheelPrizeSlot { title = "+1 SWAP & ICE", subtitle = "Combo Pack",       slotColor = XOProceduralAssets.Hex("#7C3AED"), icon = XOProceduralAssets.IconSwap,       coins = 10,  itemType = "Combo",   itemCount = 1 },
                new WheelPrizeSlot { title = "JACKPOT +100!", subtitle = "+100 Coins & All", slotColor = XOProceduralAssets.Hex("#E11D48"), icon = XOProceduralAssets.IconCrown,      coins = 100, itemType = "Jackpot", itemCount = 1 }
            };
        }

        /// <summary>Builds the Lucky Wheel modal hierarchy.</summary>
        public void Build(Transform canvasTransform)
        {
            RootObj = XOUIBuilder.CreateModalBackdrop("LuckyWheelModal", canvasTransform, out RectTransform rt);
            CardRT = rt;
            rt.sizeDelta = new Vector2(720f, 680f);

            XOUIBuilder.CreateIconImage(rt, XOProceduralAssets.IconLuckyWheel, new Vector2(0.5f, 1f), new Vector2(-215f, -46f), new Vector2(54f, 54f));

            Text title = XOUIBuilder.CreateText(rt, _font, "LUCKY PRIZE SPIN!", 30, FontStyle.Bold, TextAnchor.MiddleCenter);
            title.rectTransform.anchorMin = new Vector2(0f, 1f);
            title.rectTransform.anchorMax = new Vector2(1f, 1f);
            title.rectTransform.pivot = new Vector2(0.5f, 1f);
            title.rectTransform.anchoredPosition = new Vector2(18f, -18f);
            title.rectTransform.sizeDelta = new Vector2(-40f, 54f);

            _wheelStatusText = XOUIBuilder.CreateText(rt, _font, "Spin the Lucky Wheel for Instant Coins, Bombs, Swaps & Jackpot!", 17, FontStyle.Bold, TextAnchor.MiddleCenter);
            _wheelStatusText.color = XOProceduralAssets.ColMuted;
            _wheelStatusText.rectTransform.anchorMin = new Vector2(0f, 1f);
            _wheelStatusText.rectTransform.anchorMax = new Vector2(1f, 1f);
            _wheelStatusText.rectTransform.pivot = new Vector2(0.5f, 1f);
            _wheelStatusText.rectTransform.anchoredPosition = new Vector2(0f, -74f);
            _wheelStatusText.rectTransform.sizeDelta = new Vector2(-48f, 36f);

            GameObject gridObj = XOUIBuilder.CreateUIObject("WheelSlotsGrid", rt);
            RectTransform gridRT = gridObj.GetComponent<RectTransform>();
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
                GameObject slotObj = XOUIBuilder.CreateUIObject("PrizeSlot_" + i, gridRT);
                Image slotBg = slotObj.AddComponent<Image>();
                slotBg.sprite = XOProceduralAssets.Button3DSprite;
                slotBg.type = Image.Type.Sliced;
                slotBg.color = p.slotColor;
                _wheelSlotBgs.Add(slotBg);

                RectTransform sRT = slotObj.GetComponent<RectTransform>();
                XOUIBuilder.CreateIconImage(sRT, p.icon, new Vector2(0.5f, 1f), new Vector2(0f, -48f), new Vector2(64f, 64f));

                Text pTitle = XOUIBuilder.CreateText(sRT, _font, p.title, 21, FontStyle.Bold, TextAnchor.MiddleCenter);
                pTitle.color = Color.white;
                pTitle.rectTransform.anchorMin = new Vector2(0f, 0f);
                pTitle.rectTransform.anchorMax = new Vector2(1f, 0f);
                pTitle.rectTransform.pivot = new Vector2(0.5f, 0f);
                pTitle.rectTransform.anchoredPosition = new Vector2(0f, 44f);
                pTitle.rectTransform.sizeDelta = new Vector2(-12f, 34f);

                Text pSub = XOUIBuilder.CreateText(sRT, _font, p.subtitle, 15, FontStyle.Bold, TextAnchor.MiddleCenter);
                pSub.color = XOProceduralAssets.Hex("#FEF08A");
                pSub.rectTransform.anchorMin = new Vector2(0f, 0f);
                pSub.rectTransform.anchorMax = new Vector2(1f, 0f);
                pSub.rectTransform.pivot = new Vector2(0.5f, 0f);
                pSub.rectTransform.anchoredPosition = new Vector2(0f, 16f);
                pSub.rectTransform.sizeDelta = new Vector2(-12f, 26f);
            }

            _wheelSpinActionBtn = XOUIBuilder.CreateIcon3DButton(rt, _font, XOProceduralAssets.IconLuckyWheel, "SPIN NOW (1 FREE SPIN!)", new Vector2(640f, 68f), XOProceduralAssets.Hex("#22C55E"), Color.white, 22);
            _wheelSpinActionBtnText = _wheelSpinActionBtn.GetComponentInChildren<Text>();
            RectTransform spinRT = _wheelSpinActionBtn.GetComponent<RectTransform>();
            spinRT.anchorMin = new Vector2(0.5f, 0f);
            spinRT.anchorMax = new Vector2(0.5f, 0f);
            spinRT.pivot = new Vector2(0.5f, 0f);
            spinRT.anchoredPosition = new Vector2(0f, 88f);
            _wheelSpinActionBtn.onClick.AddListener(OnSpinClicked);

            Button closeBtn = XOUIBuilder.Create3DButton(rt, _font, "CLOSE", new Vector2(640f, 54f), XOProceduralAssets.Hex("#334155"), Color.white, 20);
            RectTransform closeRT = closeBtn.GetComponent<RectTransform>();
            closeRT.anchorMin = new Vector2(0.5f, 0f);
            closeRT.anchorMax = new Vector2(0.5f, 0f);
            closeRT.pivot = new Vector2(0.5f, 0f);
            closeRT.anchoredPosition = new Vector2(0f, 22f);
            closeBtn.onClick.AddListener(() =>
            {
                if (_isWheelSpinning) return;
                _playSfx?.Invoke("click");
                RootObj.SetActive(false);
            });

            RootObj.SetActive(false);
        }

        public void Open()
        {
            _playSfx?.Invoke("click");
            Refresh();
            RootObj.transform.SetAsLastSibling();
            RootObj.SetActive(true);
        }

        public void Refresh()
        {
            if (_wheelSpinActionBtn == null || _wheelSpinActionBtnText == null) return;
            WheelPrizeSlot[] prizes = GetWheelPrizes();
            for (int i = 0; i < _wheelSlotBgs.Count && i < prizes.Length; i++)
            {
                _wheelSlotBgs[i].color = prizes[i].slotColor;
                _wheelSlotBgs[i].transform.localScale = Vector3.one;
            }

            if (_store.FreeSpinAvailable)
            {
                _wheelSpinActionBtnText.text = "SPIN NOW (1 FREE DAILY SPIN!)";
                _wheelSpinActionBtn.GetComponent<Image>().color = XOProceduralAssets.Hex("#22C55E");
            }
            else
            {
                _wheelSpinActionBtnText.text = "WATCH AD: BONUS LUCKY SPIN!";
                _wheelSpinActionBtn.GetComponent<Image>().color = XOProceduralAssets.Hex("#F59E0B");
            }
        }

        private void OnSpinClicked()
        {
            if (_isWheelSpinning) return;

            if (_store.FreeSpinAvailable)
            {
                _store.FreeSpinAvailable = false;
                _coroutineHost.StartCoroutine(SpinRoutine());
            }
            else
            {
                _playSfx?.Invoke("click");
                XOPlayableAdsBridge.Instance.ShowRewardedAd(
                    "Bonus Lucky Wheel Spin (Win up to +250 Coins & Tactical Skills!)",
                    onRewarded: () => _coroutineHost.StartCoroutine(SpinRoutine()));
            }
        }

        private IEnumerator SpinRoutine()
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
                    _wheelSlotBgs[i].color = highlight ? XOProceduralAssets.Hex("#FBBF24") : prizes[i].slotColor;
                    _wheelSlotBgs[i].transform.localScale = highlight ? new Vector3(1.07f, 1.07f, 1f) : Vector3.one;
                }

                _playSfx?.Invoke("spin_tick");
                if (_isHapticsEnabled?.Invoke() == true) XOPlayableAdsBridge.Instance?.TriggerHaptic(10);

                float progress = step / (float)Mathf.Max(1, totalSteps);
                yield return new WaitForSeconds(Mathf.Lerp(0.055f, 0.24f, progress * progress));
            }

            WheelPrizeSlot won = prizes[winningIdx];
            _playSfx?.Invoke(won.itemType == "Jackpot" ? "jackpot" : "chest");
            if (_isHapticsEnabled?.Invoke() == true) XOPlayableAdsBridge.Instance?.TriggerHaptic(45);

            _store.Coins += won.coins;
            if (won.itemType == "Hint") _store.HintStock += won.itemCount;
            else if (won.itemType == "Bomb") _store.BombStock += won.itemCount;
            else if (won.itemType == "Combo") { _store.SwapStock += 1; _store.FreezeStock += 1; }
            else if (won.itemType == "Jackpot")
            {
                _store.HintStock += 1;
                _store.UndoStock += 1;
                _store.BombStock += 1;
                _store.FreezeStock += 1;
                _store.SwapStock += 1;
            }

            _wheelStatusText.text = $"YOU WON: {won.title} ({won.subtitle})!";
            _wheelSpinActionBtn.interactable = true;
            _isWheelSpinning = false;
            Refresh();
            _onEconomyUpdated?.Invoke();
        }
    }
}

