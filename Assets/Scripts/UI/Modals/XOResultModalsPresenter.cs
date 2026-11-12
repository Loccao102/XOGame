using System;
using UnityEngine;
using UnityEngine.UI;
using XOPlayable.Ads;
using XOPlayable.Core;
using XOPlayable.Visuals;

namespace XOPlayable.UI
{
    /// <summary>
    /// Owns both post-match modals: Second-Chance Revive modal and Match Result modal
    /// (Victory, Defeat, Draw, Tower Floor Clear, Double Coins Ad, Rank Shield Ad, Caravan Draft, Return to Hub).
    /// </summary>
    public sealed class XOResultModalsPresenter
    {
        private readonly Font _font;
        private readonly XOGameStateStore _store;
        private readonly Action<string> _playSfx;
        private readonly Action _onConfirmReviveWithAd;
        private readonly Action _onDeclineRevive;
        private readonly Action _onPlayAgainOrNext;
        private readonly Action _onReturnToLobby;
        private readonly Action _onOpenCaravanDraft;
        private readonly Action _onEconomyUpdated;

        public GameObject ReviveModalRoot { get; private set; }
        public RectTransform ReviveCardRT { get; private set; }
        public GameObject ResultModalRoot { get; private set; }
        public RectTransform ResultCardRT { get; private set; }

        private Text _reviveTitleText;
        private Text _reviveDescText;
        private Text _reviveActionBtnText;

        private Image _resultHeroIconImg;
        private Text _resultTitleText;
        private Text _resultSubText;
        private Button _resultPlayAgainBtn;
        private Text _resultPlayAgainBtnText;
        private Button _doubleRewardAdBtn;
        private Text _doubleRewardAdBtnText;
        private Button _shieldAdBtn;
        private Text _shieldAdBtnText;
        private Button _resultThirdActionBtn;
        private Text _resultThirdActionBtnText;

        public XOResultModalsPresenter(
            Font font,
            XOGameStateStore store,
            Action<string> playSfx,
            Action onConfirmReviveWithAd,
            Action onDeclineRevive,
            Action onPlayAgainOrNext,
            Action onReturnToLobby,
            Action onOpenCaravanDraft,
            Action onEconomyUpdated)
        {
            _font = font;
            _store = store;
            _playSfx = playSfx;
            _onConfirmReviveWithAd = onConfirmReviveWithAd;
            _onDeclineRevive = onDeclineRevive;
            _onPlayAgainOrNext = onPlayAgainOrNext;
            _onReturnToLobby = onReturnToLobby;
            _onOpenCaravanDraft = onOpenCaravanDraft;
            _onEconomyUpdated = onEconomyUpdated;
        }

        /// <summary>Builds both Revive and Result modal hierarchies.</summary>
        public void Build(Transform canvasTransform)
        {
            BuildReviveModal(canvasTransform);
            BuildResultModal(canvasTransform);
        }

        private void BuildReviveModal(Transform canvasTransform)
        {
            ReviveModalRoot = XOUIBuilder.CreateModalBackdrop("ReviveModal", canvasTransform, out RectTransform rt);
            ReviveCardRT = rt;
            rt.sizeDelta = new Vector2(680f, 520f);

            XOUIBuilder.CreateIconImage(rt, XOProceduralAssets.IconShield, new Vector2(0.5f, 1f), new Vector2(0f, -64f), new Vector2(72f, 72f)).gameObject.AddComponent<XOPopOnEnable>();

            _reviveTitleText = XOUIBuilder.CreateText(rt, _font, "Second chance!", 32, FontStyle.Bold, TextAnchor.UpperCenter);
            _reviveTitleText.rectTransform.offsetMin = new Vector2(24f, 0f);
            _reviveTitleText.rectTransform.offsetMax = new Vector2(-24f, -116f);

            _reviveDescText = XOUIBuilder.CreateText(rt, _font, "", 19, FontStyle.Normal, TextAnchor.MiddleCenter);
            _reviveDescText.color = XOProceduralAssets.ColMuted;
            _reviveDescText.rectTransform.offsetMin = new Vector2(34f, 180f);
            _reviveDescText.rectTransform.offsetMax = new Vector2(-34f, -168f);

            Button reviveBtn = XOUIBuilder.CreateIcon3DButton(rt, _font, XOProceduralAssets.IconVideoAd, "Watch ad: revive", new Vector2(580f, 68f), XOProceduralAssets.ColSun, XOProceduralAssets.ColInk, 19);
            _reviveActionBtnText = reviveBtn.GetComponentInChildren<Text>();
            RectTransform rRT = reviveBtn.GetComponent<RectTransform>();
            rRT.SetAnchor(new Vector2(0.5f, 0f), new Vector2(0.5f, 0f));
            rRT.anchoredPosition = new Vector2(0f, 96f);
            reviveBtn.onClick.AddListener(() => _onConfirmReviveWithAd?.Invoke());

            Button declineBtn = XOUIBuilder.Create3DButton(rt, _font, "No thanks", new Vector2(580f, 52f), XOProceduralAssets.ColTileEdge, XOProceduralAssets.ColMuted, 17);
            RectTransform dRT = declineBtn.GetComponent<RectTransform>();
            dRT.SetAnchor(new Vector2(0.5f, 0f), new Vector2(0.5f, 0f));
            dRT.anchoredPosition = new Vector2(0f, 28f);
            declineBtn.onClick.AddListener(() =>
            {
                ReviveModalRoot.SetActive(false);
                _onDeclineRevive?.Invoke();
            });

            ReviveModalRoot.SetActive(false);
        }

        private void BuildResultModal(Transform canvasTransform)
        {
            ResultModalRoot = XOUIBuilder.CreateModalBackdrop("ResultModal", canvasTransform, out RectTransform rt);
            ResultCardRT = rt;
            rt.sizeDelta = new Vector2(720f, 700f);

            _resultHeroIconImg = XOUIBuilder.CreateIconImage(rt, XOProceduralAssets.IconTrophy, new Vector2(0.5f, 1f), new Vector2(0f, -66f), new Vector2(82f, 82f));
            _resultHeroIconImg.gameObject.AddComponent<XOPopOnEnable>();

            _resultTitleText = XOUIBuilder.CreateText(rt, _font, "Victory!", 38, FontStyle.Bold, TextAnchor.UpperCenter);
            _resultTitleText.rectTransform.offsetMin = new Vector2(24f, 0f);
            _resultTitleText.rectTransform.offsetMax = new Vector2(-24f, -124f);

            _resultSubText = XOUIBuilder.CreateText(rt, _font, "", 19, FontStyle.Bold, TextAnchor.MiddleCenter);
            _resultSubText.color = XOProceduralAssets.ColInk;
            _resultSubText.rectTransform.offsetMin = new Vector2(36f, 320f);
            _resultSubText.rectTransform.offsetMax = new Vector2(-36f, -182f);

            _resultPlayAgainBtn = XOUIBuilder.CreateIcon3DButton(rt, _font, XOProceduralAssets.IconNewGame, "Play again", new Vector2(600f, 64f), XOProceduralAssets.ColMint, XOProceduralAssets.ColInk, 21);
            _resultPlayAgainBtnText = _resultPlayAgainBtn.GetComponentInChildren<Text>();
            RectTransform paRT = _resultPlayAgainBtn.GetComponent<RectTransform>();
            paRT.SetAnchor(new Vector2(0.5f, 0f), new Vector2(0.5f, 0f));
            paRT.anchoredPosition = new Vector2(0f, 240f);
            _resultPlayAgainBtn.onClick.AddListener(() =>
            {
                _playSfx?.Invoke("click");
                ResultModalRoot.SetActive(false);
                _onPlayAgainOrNext?.Invoke();
            });

            _doubleRewardAdBtn = XOUIBuilder.CreateIcon3DButton(rt, _font, XOProceduralAssets.IconVideoAd, "Watch ad: 2× coins", new Vector2(600f, 58f), XOUIColors.Amber500, Color.white, 18);
            _doubleRewardAdBtnText = _doubleRewardAdBtn.GetComponentInChildren<Text>();
            RectTransform dblRT = _doubleRewardAdBtn.GetComponent<RectTransform>();
            dblRT.SetAnchor(new Vector2(0.5f, 0f), new Vector2(0.5f, 0f));
            dblRT.anchoredPosition = new Vector2(0f, 170f);
            _doubleRewardAdBtn.onClick.AddListener(OnWatchAdDoubleReward);

            _shieldAdBtn = XOUIBuilder.CreateIcon3DButton(rt, _font, XOProceduralAssets.IconShield, "Watch ad: rank shield (+1 star)", new Vector2(600f, 56f), XOUIColors.Violet500, Color.white, 17);
            _shieldAdBtnText = _shieldAdBtn.GetComponentInChildren<Text>();
            RectTransform shRT = _shieldAdBtn.GetComponent<RectTransform>();
            shRT.SetAnchor(new Vector2(0.5f, 0f), new Vector2(0.5f, 0f));
            shRT.anchoredPosition = new Vector2(0f, 102f);
            _shieldAdBtn.onClick.AddListener(OnWatchAdShield);

            _resultThirdActionBtn = XOUIBuilder.CreateIcon3DButton(rt, _font, XOProceduralAssets.IconRelic, "Visit relic caravan", new Vector2(600f, 54f), XOProceduralAssets.ColSky, Color.white, 17);
            _resultThirdActionBtnText = _resultThirdActionBtn.GetComponentInChildren<Text>();
            RectTransform taRT = _resultThirdActionBtn.GetComponent<RectTransform>();
            taRT.SetAnchor(new Vector2(0.5f, 0f), new Vector2(0.5f, 0f));
            taRT.anchoredPosition = new Vector2(0f, 102f);
            _resultThirdActionBtn.onClick.AddListener(() =>
            {
                _playSfx?.Invoke("click");
                ResultModalRoot.SetActive(false);
                _onOpenCaravanDraft?.Invoke();
            });
            _resultThirdActionBtn.gameObject.SetActive(false);

            Button lobbyBtn = XOUIBuilder.Create3DButton(rt, _font, "Back to lobby", new Vector2(600f, 52f), XOProceduralAssets.ColTileEdge, XOProceduralAssets.ColInk, 18);
            RectTransform lbRT = lobbyBtn.GetComponent<RectTransform>();
            lbRT.SetAnchor(new Vector2(0.5f, 0f), new Vector2(0.5f, 0f));
            lbRT.anchoredPosition = new Vector2(0f, 34f);
            lobbyBtn.onClick.AddListener(() =>
            {
                _playSfx?.Invoke("click");
                ResultModalRoot.SetActive(false);
                _onReturnToLobby?.Invoke();
            });

            ResultModalRoot.SetActive(false);
        }

        public void ShowRevivePrompt(string title, string desc, string btnLabel)
        {
            if (_reviveTitleText != null) _reviveTitleText.text = title;
            if (_reviveDescText != null) _reviveDescText.text = desc;
            if (_reviveActionBtnText != null) _reviveActionBtnText.text = btnLabel;
            ReviveModalRoot.transform.SetAsLastSibling();
            ReviveModalRoot.SetActive(true);
        }

        /// <summary>Configures and opens the end-of-match Result Modal based on outcome and GameMode.</summary>
        public void ShowMatchResult(MatchState state, GameMode mode, int boardSize, XOPuzzleStage currentPuzzle, string extraNotes)
        {
            _store.ClaimedDoubleReward = false;

            if (state == MatchState.XWon) ConfigureVictoryView(mode, boardSize, currentPuzzle, extraNotes);
            else if (state == MatchState.OWon) ConfigureDefeatView(mode, extraNotes);
            else ConfigureDrawView(mode);

            _doubleRewardAdBtn.interactable = true;
            _doubleRewardAdBtn.GetComponent<Image>().color = XOUIColors.Amber500;
            _doubleRewardAdBtnText.text = $"Watch ad: 2× coins (+{_store.LastMatchRewardCoins})";

            ResultModalRoot.transform.SetAsLastSibling();
            ResultModalRoot.SetActive(true);
        }

        private void ConfigureVictoryView(GameMode mode, int boardSize, XOPuzzleStage currentPuzzle, string extraNotes)
        {
            _store.LastMatchRewardCoins = (mode == GameMode.PuzzleStage)
                ? (currentPuzzle != null ? currentPuzzle.rewardCoins : 40)
                : (mode == GameMode.TowerClimb)
                ? XORoguelikeRunManager.GetActiveBlind().coinReward
                : ((boardSize == 15) ? 20 : 15);

            _store.Coins += _store.LastMatchRewardCoins;
            if (_resultHeroIconImg != null) _resultHeroIconImg.sprite = XOProceduralAssets.IconTrophy;
            _resultTitleText.color = XOUIColors.Amber400;
            _shieldAdBtn.gameObject.SetActive(false);

            if (mode == GameMode.TowerClimb)
            {
                TowerFloorBlind blind = XORoguelikeRunManager.GetActiveBlind();
                XOProgressionSystem.MarkTowerFloorCleared(blind.floorNumber);
                XORoguelikeRunManager.RunTotalCoinsEarned += _store.LastMatchRewardCoins;
                if (blind.floorNumber == 10) XOThemeDatabase.UnlockPieceSkin("king_dragon");

                _resultTitleText.text = $"Floor {blind.floorNumber} cleared!";
                _resultSubText.text = $"Defeated: {blind.bossName}\nReward: +{_store.LastMatchRewardCoins} Coins!\n(Visit Relic Caravan before next floor!)";
                _resultPlayAgainBtnText.text = (blind.floorNumber < XORoguelikeCatalog.FloorCount) ? $"Continue to floor {blind.floorNumber + 1}" : "Back to lobby";
                _resultPlayAgainBtn.GetComponent<Image>().color = XOProceduralAssets.ColMint;
                _resultThirdActionBtn.gameObject.SetActive(blind.floorNumber < XORoguelikeCatalog.FloorCount);
                if (_resultThirdActionBtnText != null) _resultThirdActionBtnText.text = "Visit relic caravan";
            }
            else
            {
                _resultTitleText.text = (mode == GameMode.PuzzleStage) ? "Puzzle cleared!" : "Victory!";
                _resultSubText.text = $"Match Reward: +{_store.LastMatchRewardCoins} Coins\n{extraNotes}";
                _resultPlayAgainBtnText.text = (mode == GameMode.PuzzleStage) ? "Next puzzle" : "Play again";
                _resultPlayAgainBtn.GetComponent<Image>().color = XOProceduralAssets.ColMint;
                _resultThirdActionBtn.gameObject.SetActive(false);
            }
        }

        private void ConfigureDefeatView(GameMode mode, string extraNotes)
        {
            _store.LastMatchRewardCoins = 15;
            _store.Coins += _store.LastMatchRewardCoins;
            if (_resultHeroIconImg != null) _resultHeroIconImg.sprite = XOProceduralAssets.IconSwords;
            _resultThirdActionBtn.gameObject.SetActive(false);

            if (mode == GameMode.TowerClimb)
            {
                TowerFloorBlind blind = XORoguelikeRunManager.GetActiveBlind();
                _resultTitleText.text = XORoguelikeRunManager.IsRunActive ? "DEFEATED BY BOSS" : "RUN ENDED (0 HEARTS)";
                _resultTitleText.color = XOProceduralAssets.ColCoral;
                _resultSubText.text = extraNotes;
                _resultPlayAgainBtnText.text = XORoguelikeRunManager.IsRunActive ? "Rematch floor" : "Start new run";
                _resultPlayAgainBtn.GetComponent<Image>().color = XOProceduralAssets.ColCoral;
                _shieldAdBtn.gameObject.SetActive(false);
            }
            else
            {
                _resultTitleText.text = (mode == GameMode.TwoPlayerLocal) ? "PLAYER O WINS!" : "Defeat";
                _resultTitleText.color = XOUIColors.Sky400;
                _resultSubText.text = $"Consolation Reward: +{_store.LastMatchRewardCoins} Coins\n{extraNotes}";
                _resultPlayAgainBtnText.text = "Play again";
                _resultPlayAgainBtn.GetComponent<Image>().color = XOProceduralAssets.ColMint;

                bool showShield = mode != GameMode.TwoPlayerLocal;
                _shieldAdBtn.gameObject.SetActive(showShield);
                if (showShield)
                {
                    _shieldAdBtn.interactable = true;
                    _shieldAdBtn.GetComponent<Image>().color = XOUIColors.Violet500;
                    _shieldAdBtnText.text = (_store.LostStreakToRestore > 0)
                        ? $"Watch ad: restore {_store.LostStreakToRestore} win streak"
                        : "Watch ad: rank shield (+1 star)";
                }
            }
        }

        private void ConfigureDrawView(GameMode mode)
        {
            _store.LastMatchRewardCoins = 25;
            _store.Coins += _store.LastMatchRewardCoins;
            if (_resultHeroIconImg != null) _resultHeroIconImg.sprite = XOProceduralAssets.IconShield;
            _resultTitleText.text = "Draw!";
            _resultTitleText.color = XOUIColors.Purple500;
            _resultSubText.text = (mode == GameMode.TowerClimb)
                ? $"Draw against {XORoguelikeRunManager.GetActiveBlind().bossName}!\nNo heart lost. Replay this floor to advance!\nReward: +{_store.LastMatchRewardCoins} Coins"
                : $"Stalemate Draw!\nReward: +{_store.LastMatchRewardCoins} Coins";
            _shieldAdBtn.gameObject.SetActive(false);
            _resultThirdActionBtn.gameObject.SetActive(false);
            _resultPlayAgainBtnText.text = (mode == GameMode.TowerClimb)
                ? $"Rematch vs {XORoguelikeRunManager.GetActiveBlind().bossName}"
                : "REMATCH";
            _resultPlayAgainBtn.GetComponent<Image>().color = XOProceduralAssets.ColMint;
        }

        private void OnWatchAdShield()
        {
            _playSfx?.Invoke("click");
            XOPlayableAdsBridge.Instance.ShowRewardedAd(
                "Activate Rank Shield: Restore Win Streak & Rank Star!",
                onRewarded: () =>
                {
                    _playSfx?.Invoke("reward");
                    XOProgressionSystem.TotalStars += 1;
                    if (_store.LostStreakToRestore > 0)
                    {
                        XOProgressionSystem.WinStreak = _store.LostStreakToRestore;
                    }
                    _onEconomyUpdated?.Invoke();
                    _shieldAdBtn.interactable = false;
                    _shieldAdBtn.GetComponent<Image>().color = XOUIColors.Slate600;
                    _shieldAdBtnText.text = "STREAK & RANK STAR RESTORED!";
                });
        }

        private void OnWatchAdDoubleReward()
        {
            if (_store.ClaimedDoubleReward) return;
            _playSfx?.Invoke("click");

            XOPlayableAdsBridge.Instance.ShowRewardedAd(
                $"Double Match Reward (+{_store.LastMatchRewardCoins} Coins)",
                onRewarded: () =>
                {
                    _store.ClaimedDoubleReward = true;
                    _store.Coins += _store.LastMatchRewardCoins;
                    _playSfx?.Invoke("reward");
                    _onEconomyUpdated?.Invoke();

                    _doubleRewardAdBtn.interactable = false;
                    _doubleRewardAdBtn.GetComponent<Image>().color = XOUIColors.Slate600;
                    _doubleRewardAdBtnText.text = "2× reward claimed!";
                });
        }
    }
}

