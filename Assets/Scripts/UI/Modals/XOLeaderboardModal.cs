using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using XOPlayable.Core;
using XOPlayable.Visuals;

namespace XOPlayable.UI
{
    /// <summary>
    /// 2-Tab Arena Campaign &amp; Lifetime Achievements modal showing Top 10 rankings,
    /// playable AI Bot rivals, and claimable achievement milestones.
    /// </summary>
    public sealed class XOLeaderboardModal
    {
        private readonly Font _font;
        private readonly XOGameStateStore _store;
        private readonly Action<string> _playSfx;
        private readonly Action<int> _onChallengeBot;
        private readonly Action _onEconomyUpdated;

        public GameObject RootObj { get; private set; }
        public RectTransform CardRT { get; private set; }

        private int _activeTab;
        private Button _tabArenaBtn;
        private Button _tabAchievementsBtn;
        private Text _summaryText;
        private RectTransform _arenaRowsContainer;
        private RectTransform _achievementsRowsContainer;
        private GameObject _arenaScrollObj;
        private GameObject _achievementsScrollObj;

        public XOLeaderboardModal(
            Font font,
            XOGameStateStore store,
            Action<string> playSfx,
            Action<int> onChallengeBot,
            Action onEconomyUpdated)
        {
            _font = font;
            _store = store;
            _playSfx = playSfx;
            _onChallengeBot = onChallengeBot;
            _onEconomyUpdated = onEconomyUpdated;
        }

        /// <summary>Builds the Arena Campaign &amp; Achievements modal UI.</summary>
        public void Build(Transform canvasTransform)
        {
            RootObj = XOUIBuilder.CreateModalBackdrop("LeaderboardModal", canvasTransform, out RectTransform cardRT);
            CardRT = cardRT;
            cardRT.sizeDelta = new Vector2(960f, 940f);

            XOUIBuilder.CreateIconImage(cardRT, XOProceduralAssets.IconLeaderboard, new Vector2(0.5f, 1f), new Vector2(-285f, -42f), new Vector2(44f, 44f));
            Text lbTitle = XOUIBuilder.CreateText(cardRT, _font, "ARENA CAMPAIGN & ACHIEVEMENTS", 28, FontStyle.Bold, TextAnchor.UpperCenter);
            lbTitle.rectTransform.offsetMax = new Vector2(25f, -24f);

            Button closeBtn = XOUIBuilder.Create3DButton(cardRT, _font, "X", new Vector2(48f, 46f), Color.white, XOProceduralAssets.ColInk, 20);
            RectTransform closeRT = closeBtn.GetComponent<RectTransform>();
            closeRT.anchorMin = new Vector2(1f, 1f);
            closeRT.anchorMax = new Vector2(1f, 1f);
            closeRT.anchoredPosition = new Vector2(-36f, -36f);
            closeBtn.onClick.AddListener(() => { _playSfx?.Invoke("click"); RootObj.SetActive(false); });

            GameObject tabRow = XOUIBuilder.CreateUIObject("LeaderboardTabsRow", cardRT);
            RectTransform tabRT = tabRow.GetComponent<RectTransform>();
            tabRT.anchorMin = new Vector2(0.5f, 1f);
            tabRT.anchorMax = new Vector2(0.5f, 1f);
            tabRT.pivot = new Vector2(0.5f, 1f);
            tabRT.anchoredPosition = new Vector2(0f, -80f);
            tabRT.sizeDelta = new Vector2(880f, 56f);

            HorizontalLayoutGroup tabLayout = tabRow.AddComponent<HorizontalLayoutGroup>();
            tabLayout.spacing = 16f;
            tabLayout.childControlWidth = true;
            tabLayout.childControlHeight = true;
            tabLayout.childForceExpandWidth = true;

            _tabArenaBtn = XOUIBuilder.CreateIcon3DButton(tabRT, _font, XOProceduralAssets.IconSwords, "ARENA & 6 BOTS CAMPAIGN", new Vector2(430f, 56f), XOProceduralAssets.ColSky, Color.white, 18);
            _tabArenaBtn.onClick.AddListener(() => SwitchTab(0, currentMode: GameMode.VsAI));

            _tabAchievementsBtn = XOUIBuilder.CreateIcon3DButton(tabRT, _font, XOProceduralAssets.IconTrophy, "ACHIEVEMENTS & REWARDS", new Vector2(430f, 56f), XOProceduralAssets.ColTileEdge, XOProceduralAssets.ColInk, 18);
            _tabAchievementsBtn.onClick.AddListener(() => SwitchTab(1, currentMode: GameMode.VsAI));

            GameObject summaryBar = XOUIBuilder.CreateUIObject("LeaderboardSummaryBar", cardRT);
            RectTransform sbRT = summaryBar.GetComponent<RectTransform>();
            sbRT.anchorMin = new Vector2(0.5f, 1f);
            sbRT.anchorMax = new Vector2(0.5f, 1f);
            sbRT.pivot = new Vector2(0.5f, 1f);
            sbRT.anchoredPosition = new Vector2(0f, -146f);
            sbRT.sizeDelta = new Vector2(880f, 44f);
            Image sbImg = summaryBar.AddComponent<Image>();
            sbImg.sprite = XOProceduralAssets.PillSprite;
            sbImg.type = Image.Type.Sliced;
            sbImg.color = XOProceduralAssets.ColTileEdge;
            sbImg.raycastTarget = false;

            _summaryText = XOUIBuilder.CreateText(sbRT, _font, "", 17, FontStyle.Bold, TextAnchor.MiddleCenter);

            XOUIBuilder.CreateModalScrollView("ArenaScroll", cardRT, out _arenaRowsContainer, out _arenaScrollObj);
            _arenaScrollObj.GetComponent<RectTransform>().offsetMax = new Vector2(-24f, -200f);

            XOUIBuilder.CreateModalScrollView("AchScroll", cardRT, out _achievementsRowsContainer, out _achievementsScrollObj);
            _achievementsScrollObj.GetComponent<RectTransform>().offsetMax = new Vector2(-24f, -200f);

            _achievementsScrollObj.SetActive(false);
            RootObj.SetActive(false);
        }

        public void Open(GameMode currentMode)
        {
            _playSfx?.Invoke("click");
            int initialTab = XOProgressionSystem.HasAnyClaimableAchievement() ? 1 : _activeTab;
            SwitchTab(initialTab, currentMode, playClickSfx: false);
            RootObj.transform.SetAsLastSibling();
            RootObj.SetActive(true);
        }

        private void SwitchTab(int tabIdx, GameMode currentMode, bool playClickSfx = true)
        {
            if (playClickSfx) _playSfx?.Invoke("click");
            _activeTab = tabIdx;
            if (_arenaScrollObj != null) _arenaScrollObj.SetActive(tabIdx == 0);
            if (_achievementsScrollObj != null) _achievementsScrollObj.SetActive(tabIdx == 1);

            ApplyTabVisual(_tabArenaBtn, tabIdx == 0);
            ApplyTabVisual(_tabAchievementsBtn, tabIdx == 1);

            if (tabIdx == 0) RefreshArenaRows(currentMode);
            else RefreshAchievementRows();
        }

        private static void ApplyTabVisual(Button btn, bool active)
        {
            if (btn == null) return;
            btn.GetComponent<Image>().color = active ? XOProceduralAssets.ColSky : XOProceduralAssets.ColTileEdge;
            Text t = btn.GetComponentInChildren<Text>();
            if (t != null) t.color = active ? Color.white : XOProceduralAssets.ColInk;
        }

        private void RefreshArenaRows(GameMode currentMode)
        {
            if (_arenaRowsContainer == null) return;
            ClearChildren(_arenaRowsContainer);

            List<XOLeaderboardEntry> entries = XOProgressionSystem.GetTop10Leaderboard(out int myRankPos);
            int conqueredCount = XOProgressionSystem.GetConqueredBotsCount();
            if (_summaryText != null)
            {
                _summaryText.text = $"YOUR ARENA RANK: #{myRankPos}   |   ELO: {XOProgressionSystem.PlayerELO}   |   CAMPAIGN RIVALS CONQUERED: {conqueredCount}/6";
            }

            int activeBotIdx = XOProgressionSystem.SelectedBotIndex;
            foreach (XOLeaderboardEntry entry in entries)
            {
                GameObject rowObj = XOUIBuilder.CreateUIObject("RankRow_" + entry.rankPosition, _arenaRowsContainer);
                LayoutElement le = rowObj.AddComponent<LayoutElement>();
                le.minHeight = 64f;
                le.preferredHeight = 64f;

                Image rowBg = rowObj.AddComponent<Image>();
                rowBg.sprite = XOProceduralAssets.CardPanelSprite;
                rowBg.type = Image.Type.Sliced;

                bool isHuman = entry.isHumanPlayer;
                bool isCurrentRival = entry.botIndexIfRival == activeBotIdx && currentMode == GameMode.VsAI;
                rowBg.color = isHuman ? XOProceduralAssets.Hex("#ECFDF5") : (isCurrentRival ? XOProceduralAssets.Hex("#FEFCE8") : Color.white);

                RectTransform rowRT = rowObj.GetComponent<RectTransform>();

                Text rankTxt = XOUIBuilder.CreateText(rowRT, _font, $"#{entry.rankPosition}", 20, FontStyle.Bold, TextAnchor.MiddleLeft);
                rankTxt.rectTransform.offsetMin = new Vector2(18f, 0f);
                rankTxt.rectTransform.offsetMax = new Vector2(-780f, 0f);
                rankTxt.color = (entry.rankPosition <= 3) ? XOProceduralAssets.ColSun : XOProceduralAssets.ColMuted;

                string conquestBadge = string.Empty;
                if (!isHuman && entry.botIndexIfRival >= 0)
                {
                    bool conquered = XOProgressionSystem.IsBotConquered(entry.botIndexIfRival);
                    conquestBadge = conquered ? "  [CONQUERED]" : "  [FIRST WIN: +80c]";
                }

                Text nameTxt = XOUIBuilder.CreateText(rowRT, _font, $"{entry.playerName} — {entry.titleTag}{conquestBadge}", 17, FontStyle.Bold, TextAnchor.MiddleLeft);
                nameTxt.rectTransform.offsetMin = new Vector2(80f, 0f);
                nameTxt.rectTransform.offsetMax = new Vector2(-330f, 0f);
                nameTxt.color = isHuman ? XOProceduralAssets.Hex("#059669") : XOProceduralAssets.ColInk;

                Text eloTxt = XOUIBuilder.CreateText(rowRT, _font, $"ELO {entry.eloRating}", 18, FontStyle.Bold, TextAnchor.MiddleRight);
                eloTxt.rectTransform.offsetMin = new Vector2(450f, 0f);
                eloTxt.rectTransform.offsetMax = new Vector2(-205f, 0f);
                eloTxt.color = XOProceduralAssets.Hex("#D97706");

                if (!isHuman && entry.botIndexIfRival >= 0)
                {
                    int targetBotIdx = entry.botIndexIfRival;
                    string btnLabel = isCurrentRival ? "ACTIVE" : "CHALLENGE";
                    Color btnCol = isCurrentRival ? XOProceduralAssets.ColMint : XOProceduralAssets.ColSky;
                    Color txtCol = isCurrentRival ? XOProceduralAssets.ColInk : Color.white;

                    Button challengeBtn = XOUIBuilder.Create3DButton(rowRT, _font, btnLabel, new Vector2(170f, 44f), btnCol, txtCol, 15);
                    RectTransform cbRT = challengeBtn.GetComponent<RectTransform>();
                    cbRT.anchorMin = new Vector2(1f, 0.5f);
                    cbRT.anchorMax = new Vector2(1f, 0.5f);
                    cbRT.pivot = new Vector2(1f, 0.5f);
                    cbRT.anchoredPosition = new Vector2(-14f, 0f);
                    challengeBtn.onClick.AddListener(() =>
                    {
                        _playSfx?.Invoke("click");
                        RootObj.SetActive(false);
                        _onChallengeBot?.Invoke(targetBotIdx);
                    });
                }
            }
        }

        private void RefreshAchievementRows()
        {
            if (_achievementsRowsContainer == null) return;
            ClearChildren(_achievementsRowsContainer);

            List<XOAchievementState> list = XOProgressionSystem.GetAchievementsList();
            int claimedCount = 0;
            foreach (var item in list) if (item.isClaimed) claimedCount++;

            if (_summaryText != null)
            {
                _summaryText.text = $"LIFETIME ACHIEVEMENTS COMPLETED: {claimedCount}/{list.Count}   |   Complete milestones to claim Coins & Free Skills!";
            }

            foreach (XOAchievementState ach in list)
            {
                GameObject rowObj = XOUIBuilder.CreateUIObject("AchRow_" + ach.id, _achievementsRowsContainer);
                LayoutElement le = rowObj.AddComponent<LayoutElement>();
                le.minHeight = 74f;
                le.preferredHeight = 74f;

                Image rowBg = rowObj.AddComponent<Image>();
                rowBg.sprite = XOProceduralAssets.CardPanelSprite;
                rowBg.type = Image.Type.Sliced;
                rowBg.color = ach.IsReadyToClaim
                    ? XOProceduralAssets.Hex("#ECFDF5")
                    : (ach.isClaimed ? XOProceduralAssets.Hex("#F1F5F9") : Color.white);

                RectTransform rowRT = rowObj.GetComponent<RectTransform>();
                XOUIBuilder.CreateIconImage(rowRT, XOProceduralAssets.IconTrophy, new Vector2(0f, 0.5f), new Vector2(38f, 0f), new Vector2(44f, 44f));

                Text titleTxt = XOUIBuilder.CreateText(rowRT, _font, $"{ach.title}  ({ach.currentValue}/{ach.targetValue})", 19, FontStyle.Bold, TextAnchor.UpperLeft);
                titleTxt.rectTransform.offsetMin = new Vector2(76f, 0f);
                titleTxt.rectTransform.offsetMax = new Vector2(-280f, -10f);
                titleTxt.color = ach.IsReadyToClaim ? XOProceduralAssets.Hex("#059669") : (ach.isClaimed ? XOProceduralAssets.ColMuted : XOProceduralAssets.ColInk);

                Text descTxt = XOUIBuilder.CreateText(rowRT, _font, ach.description, 14, FontStyle.Normal, TextAnchor.LowerLeft);
                descTxt.color = XOProceduralAssets.ColMuted;
                descTxt.rectTransform.offsetMin = new Vector2(76f, 10f);
                descTxt.rectTransform.offsetMax = new Vector2(-280f, 0f);

                Text rewTxt = XOUIBuilder.CreateText(rowRT, _font, $"+{ach.rewardCoins} Coins\n{ach.bonusItemLabel}", 15, FontStyle.Bold, TextAnchor.MiddleRight);
                rewTxt.color = XOProceduralAssets.Hex("#D97706");
                rewTxt.rectTransform.offsetMin = new Vector2(480f, 0f);
                rewTxt.rectTransform.offsetMax = new Vector2(-205f, 0f);

                string btnLabel = ach.isClaimed ? "CLAIMED" : (ach.IsReadyToClaim ? "CLAIM REWARD!" : $"{ach.currentValue}/{ach.targetValue}");
                Color btnCol = ach.IsReadyToClaim ? XOProceduralAssets.ColMint : XOProceduralAssets.ColTileEdge;
                Color txtCol = ach.IsReadyToClaim ? XOProceduralAssets.ColInk : XOProceduralAssets.ColMuted;

                Button claimBtn = XOUIBuilder.Create3DButton(rowRT, _font, btnLabel, new Vector2(175f, 48f), btnCol, txtCol, 15);
                RectTransform cbRT = claimBtn.GetComponent<RectTransform>();
                cbRT.anchorMin = new Vector2(1f, 0.5f);
                cbRT.anchorMax = new Vector2(1f, 0.5f);
                cbRT.pivot = new Vector2(1f, 0.5f);
                cbRT.anchoredPosition = new Vector2(-14f, 0f);
                claimBtn.interactable = ach.IsReadyToClaim;

                string achId = ach.id;
                int rewardCoins = ach.rewardCoins;
                string bonusItem = ach.bonusItemLabel;
                claimBtn.onClick.AddListener(() =>
                {
                    XOProgressionSystem.MarkAchievementClaimed(achId);
                    _playSfx?.Invoke("chest");
                    _store.Coins += rewardCoins;
                    GrantBonusSkillLabel(bonusItem);
                    _onEconomyUpdated?.Invoke();
                    RefreshAchievementRows();
                });
            }
        }

        private void GrantBonusSkillLabel(string bonusItem)
        {
            if (bonusItem.Contains("Hint")) _store.HintStock += 1;
            else if (bonusItem.Contains("Swap")) _store.SwapStock += 1;
            else if (bonusItem.Contains("Bomb")) _store.BombStock += 1;
            else if (bonusItem.Contains("Freeze")) _store.FreezeStock += 1;
            else if (bonusItem.Contains("Undo")) _store.UndoStock += 1;
            else if (bonusItem.Contains("All"))
            {
                _store.HintStock += 2;
                _store.UndoStock += 2;
                _store.BombStock += 2;
                _store.FreezeStock += 2;
                _store.SwapStock += 2;
            }
        }

        private static void ClearChildren(Transform container)
        {
            for (int i = container.childCount - 1; i >= 0; i--)
            {
                UnityEngine.Object.Destroy(container.GetChild(i).gameObject);
            }
        }
    }
}

