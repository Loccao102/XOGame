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
        private void BuildLeaderboardModal(Transform canvasTransform)
        {
            _leaderboardModalRoot = CreateModalBackdrop("LeaderboardModal", canvasTransform, out RectTransform cardRT);
            _leaderboardCardRT = cardRT;
            cardRT.sizeDelta = new Vector2(960f, 940f);

            CreateIconImage(cardRT, XOProceduralAssets.IconLeaderboard, new Vector2(0.5f, 1f), new Vector2(-285f, -42f), new Vector2(44f, 44f));
            Text lbTitle = CreateText(cardRT, "ARENA CAMPAIGN & ACHIEVEMENTS", 28, FontStyle.Bold, TextAnchor.UpperCenter);
            lbTitle.rectTransform.offsetMax = new Vector2(25f, -24f);
            lbTitle.color = XOProceduralAssets.ColInk;

            Button closeBtn = Create3DButton(cardRT, "X", new Vector2(48f, 46f), Color.white, XOProceduralAssets.ColInk, 20);
            RectTransform closeRT = closeBtn.GetComponent<RectTransform>();
            closeRT.anchorMin = new Vector2(1f, 1f);
            closeRT.anchorMax = new Vector2(1f, 1f);
            closeRT.anchoredPosition = new Vector2(-36f, -36f);
            closeBtn.onClick.AddListener(() =>
            {
                PlaySfx("click");
                _leaderboardModalRoot.SetActive(false);
            });

            // 2 Tabs: [ARENA & 6 BOTS CAMPAIGN] | [ACHIEVEMENTS & REWARDS]
            GameObject tabRow = CreateUIObject("LeaderboardTabsRow", cardRT);
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

            _tabArenaBtn = CreateIcon3DButton(tabRT, XOProceduralAssets.IconSwords, "ARENA & 6 BOTS CAMPAIGN", new Vector2(430f, 56f), XOProceduralAssets.ColSky, Color.white, 18);
            _tabArenaBtn.onClick.AddListener(() => SwitchLeaderboardTab(0));

            _tabAchievementsBtn = CreateIcon3DButton(tabRT, XOProceduralAssets.IconTrophy, "ACHIEVEMENTS & REWARDS", new Vector2(430f, 56f), XOProceduralAssets.ColTileEdge, XOProceduralAssets.ColInk, 18);
            _tabAchievementsBtn.onClick.AddListener(() => SwitchLeaderboardTab(1));

            // Summary Pill Banner
            GameObject summaryBar = CreateUIObject("LeaderboardSummaryBar", cardRT);
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

            _leaderboardSummaryText = CreateText(sbRT, "YOUR ARENA RANK: #6  |  ELO: 720  |  Rivals Conquered: 0/6", 17, FontStyle.Bold, TextAnchor.MiddleCenter);
            _leaderboardSummaryText.color = XOProceduralAssets.ColInk;

            // Scrollable Container 1: Top 10 Arena & 6 Bots Campaign
            CreateModalScrollView("ArenaScroll", cardRT, out _leaderboardRowsContainer, out _leaderboardScrollViewObj);
            _leaderboardScrollViewObj.GetComponent<RectTransform>().offsetMax = new Vector2(-24f, -200f);

            // Scrollable Container 2: Lifetime Achievements
            CreateModalScrollView("AchScroll", cardRT, out _achievementsRowsContainer, out _achievementsScrollViewObj);
            _achievementsScrollViewObj.GetComponent<RectTransform>().offsetMax = new Vector2(-24f, -200f);

            _achievementsScrollViewObj.SetActive(false);
            _leaderboardModalRoot.SetActive(false);
        }

        private void OpenLeaderboardModal()
        {
            PlaySfx("click");
            // Automatically open to Achievements tab if the player has a reward ready to claim!
            int initialTab = XOProgressionSystem.HasAnyClaimableAchievement() ? 1 : _activeLeaderboardTab;
            SwitchLeaderboardTab(initialTab);
            _leaderboardModalRoot.transform.SetAsLastSibling();
            _leaderboardModalRoot.SetActive(true);
        }

        private void SwitchLeaderboardTab(int tabIdx)
        {
            PlaySfx("click");
            _activeLeaderboardTab = tabIdx;
            if (_leaderboardScrollViewObj != null) _leaderboardScrollViewObj.SetActive(tabIdx == 0);
            if (_achievementsScrollViewObj != null) _achievementsScrollViewObj.SetActive(tabIdx == 1);

            Color activeCol = XOProceduralAssets.ColSky;
            Color inactiveCol = XOProceduralAssets.ColTileEdge;
            Color activeTxt = Color.white;
            Color inactiveTxt = XOProceduralAssets.ColInk;

            if (_tabArenaBtn != null)
            {
                _tabArenaBtn.GetComponent<Image>().color = (tabIdx == 0) ? activeCol : inactiveCol;
                var t = _tabArenaBtn.GetComponentInChildren<Text>(); if (t != null) t.color = (tabIdx == 0) ? activeTxt : inactiveTxt;
            }
            if (_tabAchievementsBtn != null)
            {
                _tabAchievementsBtn.GetComponent<Image>().color = (tabIdx == 1) ? activeCol : inactiveCol;
                var t = _tabAchievementsBtn.GetComponentInChildren<Text>(); if (t != null) t.color = (tabIdx == 1) ? activeTxt : inactiveTxt;
            }

            if (tabIdx == 0)
                RefreshLeaderboardRowsUI();
            else
                RefreshAchievementsRowsUI();
        }

        private void RefreshLeaderboardRowsUI()
        {
            if (_leaderboardRowsContainer == null) return;

            for (int i = _leaderboardRowsContainer.childCount - 1; i >= 0; i--)
            {
                Destroy(_leaderboardRowsContainer.GetChild(i).gameObject);
            }

            List<XOLeaderboardEntry> entries = XOProgressionSystem.GetTop10Leaderboard(out int myRankPos);
            int conqueredCount = XOProgressionSystem.GetConqueredBotsCount();
            if (_leaderboardSummaryText != null)
            {
                _leaderboardSummaryText.text = $"YOUR ARENA RANK: #{myRankPos}   |   ELO: {XOProgressionSystem.PlayerELO}   |   CAMPAIGN RIVALS CONQUERED: {conqueredCount}/6";
            }

            int activeBotIdx = XOProgressionSystem.SelectedBotIndex;

            foreach (var entry in entries)
            {
                GameObject rowObj = CreateUIObject("RankRow_" + entry.rankPosition, _leaderboardRowsContainer);
                LayoutElement le = rowObj.AddComponent<LayoutElement>();
                le.minHeight = 64f;
                le.preferredHeight = 64f;
                le.flexibleHeight = 0f;

                Image rowBg = rowObj.AddComponent<Image>();
                rowBg.sprite = XOProceduralAssets.CardPanelSprite;
                rowBg.type = Image.Type.Sliced;

                bool isHuman = entry.isHumanPlayer;
                bool isCurrentRival = (entry.botIndexIfRival == activeBotIdx && currentGameMode == GameMode.VsAI);

                if (isHuman)
                    rowBg.color = XOThemeDatabase.Hex("#ECFDF5"); // soft mint
                else if (isCurrentRival)
                    rowBg.color = XOThemeDatabase.Hex("#FEFCE8"); // soft warm sun
                else
                    rowBg.color = Color.white; // clean porcelain

                RectTransform rowRT = rowObj.GetComponent<RectTransform>();

                // Rank # Badge
                Text rankTxt = CreateText(rowRT, $"#{entry.rankPosition}", 20, FontStyle.Bold, TextAnchor.MiddleLeft);
                rankTxt.rectTransform.offsetMin = new Vector2(18f, 0f);
                rankTxt.rectTransform.offsetMax = new Vector2(-780f, 0f);
                rankTxt.color = (entry.rankPosition <= 3) ? XOProceduralAssets.ColSun : XOProceduralAssets.ColMuted;

                // Player / Bot Name, Style Tag & Campaign Conquest Status
                string conquestBadge = "";
                if (!entry.isHumanPlayer && entry.botIndexIfRival >= 0)
                {
                    bool conquered = XOProgressionSystem.IsBotConquered(entry.botIndexIfRival);
                    conquestBadge = conquered ? "  [CONQUERED]" : "  [FIRST WIN: +80c]";
                }

                Text nameTxt = CreateText(rowRT, $"{entry.playerName} — {entry.titleTag}{conquestBadge}", 17, FontStyle.Bold, TextAnchor.MiddleLeft);
                nameTxt.rectTransform.offsetMin = new Vector2(80f, 0f);
                nameTxt.rectTransform.offsetMax = new Vector2(-330f, 0f);
                nameTxt.color = isHuman ? XOThemeDatabase.Hex("#059669") : XOProceduralAssets.ColInk;

                // ELO Rating
                Text eloTxt = CreateText(rowRT, $"ELO {entry.eloRating}", 18, FontStyle.Bold, TextAnchor.MiddleRight);
                eloTxt.rectTransform.offsetMin = new Vector2(450f, 0f);
                eloTxt.rectTransform.offsetMax = new Vector2(-205f, 0f);
                eloTxt.color = XOThemeDatabase.Hex("#D97706");

                // Challenge Bot Button (for the 6 playable AI Rivals)
                if (!entry.isHumanPlayer && entry.botIndexIfRival >= 0)
                {
                    int targetBotIdx = entry.botIndexIfRival;
                    string btnLabel = isCurrentRival ? "ACTIVE" : "CHALLENGE";
                    Color btnCol = isCurrentRival ? XOProceduralAssets.ColMint : XOProceduralAssets.ColSky;
                    Color txtCol = isCurrentRival ? XOProceduralAssets.ColInk : Color.white;

                    Button challengeBtn = Create3DButton(rowRT, btnLabel, new Vector2(170f, 44f), btnCol, txtCol, 15);
                    RectTransform cbRT = challengeBtn.GetComponent<RectTransform>();
                    cbRT.anchorMin = new Vector2(1f, 0.5f);
                    cbRT.anchorMax = new Vector2(1f, 0.5f);
                    cbRT.pivot = new Vector2(1f, 0.5f);
                    cbRT.anchoredPosition = new Vector2(-14f, 0f);
                    challengeBtn.onClick.AddListener(() =>
                    {
                        PlaySfx("click");
                        XOProgressionSystem.SelectedBotIndex = targetBotIdx;
                        XOBotProfile chosenBot = XOProgressionSystem.GetActiveBot();
                        currentGameMode = GameMode.VsAI;
                        currentDifficulty = chosenBot.baseDifficulty;
                        _scoreX = 0;
                        _scoreO = 0;
                        _leaderboardModalRoot.SetActive(false);
                        StartNewMatch(currentBoardSize);
                    });
                }
            }
        }

        private void RefreshAchievementsRowsUI()
        {
            if (_achievementsRowsContainer == null) return;

            for (int i = _achievementsRowsContainer.childCount - 1; i >= 0; i--)
            {
                Destroy(_achievementsRowsContainer.GetChild(i).gameObject);
            }

            List<XOAchievementState> list = XOProgressionSystem.GetAchievementsList();
            int claimedCount = 0;
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i].isClaimed) claimedCount++;
            }

            if (_leaderboardSummaryText != null)
            {
                _leaderboardSummaryText.text = $"LIFETIME ACHIEVEMENTS COMPLETED: {claimedCount}/{list.Count}   |   Complete milestones to claim Coins & Free Skills!";
            }

            foreach (var ach in list)
            {
                GameObject rowObj = CreateUIObject("AchRow_" + ach.id, _achievementsRowsContainer);
                LayoutElement le = rowObj.AddComponent<LayoutElement>();
                le.minHeight = 74f;
                le.preferredHeight = 74f;
                le.flexibleHeight = 0f;

                Image rowBg = rowObj.AddComponent<Image>();
                rowBg.sprite = XOProceduralAssets.CardPanelSprite;
                rowBg.type = Image.Type.Sliced;
                rowBg.color = ach.IsReadyToClaim
                    ? XOThemeDatabase.Hex("#ECFDF5")
                    : (ach.isClaimed ? XOThemeDatabase.Hex("#F1F5F9") : Color.white);

                RectTransform rowRT = rowObj.GetComponent<RectTransform>();

                CreateIconImage(rowRT, XOProceduralAssets.IconTrophy, new Vector2(0f, 0.5f), new Vector2(38f, 0f), new Vector2(44f, 44f));

                // Title & Description
                Text titleTxt = CreateText(rowRT, $"{ach.title}  ({ach.currentValue}/{ach.targetValue})", 19, FontStyle.Bold, TextAnchor.UpperLeft);
                titleTxt.rectTransform.offsetMin = new Vector2(76f, 0f);
                titleTxt.rectTransform.offsetMax = new Vector2(-280f, -10f);
                titleTxt.color = ach.IsReadyToClaim ? XOThemeDatabase.Hex("#059669") : (ach.isClaimed ? XOProceduralAssets.ColMuted : XOProceduralAssets.ColInk);

                Text descTxt = CreateText(rowRT, ach.description, 14, FontStyle.Normal, TextAnchor.LowerLeft);
                descTxt.rectTransform.offsetMin = new Vector2(76f, 10f);
                descTxt.rectTransform.offsetMax = new Vector2(-280f, 0f);
                descTxt.color = XOProceduralAssets.ColMuted;

                // Reward Label
                Text rewTxt = CreateText(rowRT, $"+{ach.rewardCoins} Coins\n{ach.bonusItemLabel}", 15, FontStyle.Bold, TextAnchor.MiddleRight);
                rewTxt.rectTransform.offsetMin = new Vector2(480f, 0f);
                rewTxt.rectTransform.offsetMax = new Vector2(-205f, 0f);
                rewTxt.color = XOThemeDatabase.Hex("#D97706");

                // Claim / Progress Button
                string btnLabel = ach.isClaimed
                    ? "CLAIMED"
                    : (ach.IsReadyToClaim ? "CLAIM REWARD!" : $"{ach.currentValue}/{ach.targetValue}");
                Color btnCol = ach.isClaimed
                    ? XOProceduralAssets.ColTileEdge
                    : (ach.IsReadyToClaim ? XOProceduralAssets.ColMint : XOProceduralAssets.ColTileEdge);
                Color txtCol = ach.isClaimed
                    ? XOProceduralAssets.ColMuted
                    : (ach.IsReadyToClaim ? XOProceduralAssets.ColInk : XOProceduralAssets.ColMuted);

                Button claimBtn = Create3DButton(rowRT, btnLabel, new Vector2(175f, 48f), btnCol, txtCol, 15);
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
                    PlaySfx("chest");
                    Coins += rewardCoins;
                    if (bonusItem.Contains("Hint")) HintStock += 1;
                    else if (bonusItem.Contains("Swap")) SwapStock += 1;
                    else if (bonusItem.Contains("Bomb")) BombStock += 1;
                    else if (bonusItem.Contains("Freeze")) FreezeStock += 1;
                    else if (bonusItem.Contains("Undo")) UndoStock += 1;
                    else if (bonusItem.Contains("All"))
                    {
                        HintStock += 2; UndoStock += 2; BombStock += 2; FreezeStock += 2; SwapStock += 2;
                    }
                    RefreshBottomActionBadges();
                    RefreshAchievementsRowsUI();
                });
            }
        }
    }
}
