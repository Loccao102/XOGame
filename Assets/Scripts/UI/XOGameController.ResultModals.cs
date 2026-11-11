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
        private void BuildReviveModal(Transform canvasTransform)
        {
            _reviveModalRoot = CreateModalBackdrop("ReviveModal", canvasTransform, out RectTransform cardRT);
            _reviveCardRT = cardRT;
            cardRT.sizeDelta = new Vector2(760f, 560f);

            // Hero Shield Icon at Top Center
            CreateIconImage(cardRT, XOProceduralAssets.IconShield, new Vector2(0.5f, 1f), new Vector2(0f, -72f), new Vector2(96f, 96f));

            _reviveTitleText = CreateText(cardRT, "SECOND CHANCE!", 36, FontStyle.Bold, TextAnchor.UpperCenter);
            _reviveTitleText.rectTransform.offsetMax = new Vector2(0f, -128f);
            _reviveTitleText.color = XOProceduralAssets.ColCoral;

            // Recessed Description Box
            GameObject descBox = CreateUIObject("ReviveDescBox", cardRT);
            RectTransform dbRT = descBox.GetComponent<RectTransform>();
            dbRT.anchorMin = new Vector2(0.5f, 0.5f);
            dbRT.anchorMax = new Vector2(0.5f, 0.5f);
            dbRT.anchoredPosition = new Vector2(0f, 18f);
            dbRT.sizeDelta = new Vector2(680f, 140f);
            Image dbImg = descBox.AddComponent<Image>();
            dbImg.sprite = XOProceduralAssets.CardPanelSprite;
            dbImg.type = Image.Type.Sliced;
            dbImg.color = XOProceduralAssets.ColTileEdge;
            dbImg.raycastTarget = false;

            _reviveDescText = CreateText(dbRT,
                "Opponent just connected 5 in a row!\nWatch a short ad to UNDO AI's winning move and highlight the exact square to block!",
                20, FontStyle.Normal, TextAnchor.MiddleCenter);
            _reviveDescText.rectTransform.offsetMin = new Vector2(20f, 10f);
            _reviveDescText.rectTransform.offsetMax = new Vector2(-20f, -10f);
            _reviveDescText.color = XOProceduralAssets.ColInk;

            _reviveActionBtn = CreateIcon3DButton(cardRT, XOProceduralAssets.IconVideoAd, "WATCH AD: REVIVE & BLOCK NOW!", new Vector2(660f, 76f), XOProceduralAssets.ColMint, XOProceduralAssets.ColInk, 22);
            RectTransform revBtnRT = _reviveActionBtn.GetComponent<RectTransform>();
            revBtnRT.anchorMin = new Vector2(0.5f, 0f);
            revBtnRT.anchorMax = new Vector2(0.5f, 0f);
            revBtnRT.anchoredPosition = new Vector2(0f, 136f);
            _reviveActionBtnText = _reviveActionBtn.GetComponentInChildren<Text>();
            _reviveActionBtn.onClick.AddListener(OnConfirmReviveWithAd);

            Button giveUpBtn = Create3DButton(cardRT, "No thanks, accept defeat", new Vector2(360f, 52f), XOProceduralAssets.ColTileEdge, XOProceduralAssets.ColMuted, 18);
            RectTransform giveUpRT = giveUpBtn.GetComponent<RectTransform>();
            giveUpRT.anchorMin = new Vector2(0.5f, 0f);
            giveUpRT.anchorMax = new Vector2(0.5f, 0f);
            giveUpRT.anchoredPosition = new Vector2(0f, 55f);
            giveUpBtn.onClick.AddListener(() =>
            {
                PlaySfx("click");
                _reviveModalRoot.SetActive(false);
                ProcessPlayerDefeatConsequences();
            });

            _reviveModalRoot.SetActive(false);
        }

        private void BuildResultModal(Transform canvasTransform)
        {
            _resultModalRoot = CreateModalBackdrop("ResultModal", canvasTransform, out RectTransform cardRT);
            _resultCardRT = cardRT;
            cardRT.sizeDelta = new Vector2(700f, 540f);

            // Large Golden Trophy / Swords Hero Icon at Top
            _resultHeroIconImg = CreateIconImage(cardRT, XOProceduralAssets.IconTrophy, new Vector2(0.5f, 1f), new Vector2(0f, -68f), new Vector2(96f, 96f));

            _resultTitleText = CreateText(cardRT, "VICTORY!", 36, FontStyle.Bold, TextAnchor.UpperCenter);
            _resultTitleText.rectTransform.offsetMax = new Vector2(0f, -122f);
            _resultTitleText.color = XOProceduralAssets.ColInk;

            // Recessed Reward Summary Card
            GameObject rewardBox = CreateUIObject("RewardSummaryBox", cardRT);
            RectTransform rbRT = rewardBox.GetComponent<RectTransform>();
            rbRT.anchorMin = new Vector2(0.5f, 1f);
            rbRT.anchorMax = new Vector2(0.5f, 1f);
            rbRT.pivot = new Vector2(0.5f, 1f);
            rbRT.anchoredPosition = new Vector2(0f, -172f);
            rbRT.sizeDelta = new Vector2(620f, 110f);
            Image rbImg = rewardBox.AddComponent<Image>();
            rbImg.sprite = XOProceduralAssets.CardPanelSprite;
            rbImg.type = Image.Type.Sliced;
            rbImg.color = XOProceduralAssets.ColTileEdge;
            rbImg.raycastTarget = false;

            _resultSubText = CreateText(rbRT, "Match Reward: +70 Coins", 21, FontStyle.Bold, TextAnchor.MiddleCenter);
            _resultSubText.rectTransform.offsetMin = new Vector2(16f, 8f);
            _resultSubText.rectTransform.offsetMax = new Vector2(-16f, -8f);
            _resultSubText.color = XOProceduralAssets.ColInk;

            // Rank & Win Streak Shield Button (Shown on Defeat)
            _shieldAdBtn = CreateIcon3DButton(cardRT, XOProceduralAssets.IconShield, "WATCH AD: SHIELD WIN STREAK & RANK", new Vector2(620f, 62f), XOProceduralAssets.ColLilac, Color.white, 19);
            RectTransform shieldRT = _shieldAdBtn.GetComponent<RectTransform>();
            shieldRT.anchorMin = new Vector2(0.5f, 0f);
            shieldRT.anchorMax = new Vector2(0.5f, 0f);
            shieldRT.pivot = new Vector2(0.5f, 0f);
            shieldRT.anchoredPosition = new Vector2(0f, 154f);
            _shieldAdBtnText = _shieldAdBtn.GetComponentInChildren<Text>();
            _shieldAdBtn.onClick.AddListener(OnWatchAdActivateRankShield);

            // Double Coins (2x Reward) 3D Button with Video Ad Icon
            _doubleRewardAdBtn = CreateIcon3DButton(cardRT, XOProceduralAssets.IconVideoAd, "WATCH AD: 2X COINS (+140)", new Vector2(620f, 62f), XOProceduralAssets.ColSun, XOProceduralAssets.ColInk, 20);
            RectTransform x2RT = _doubleRewardAdBtn.GetComponent<RectTransform>();
            x2RT.anchorMin = new Vector2(0.5f, 0f);
            x2RT.anchorMax = new Vector2(0.5f, 0f);
            x2RT.pivot = new Vector2(0.5f, 0f);
            x2RT.anchoredPosition = new Vector2(0f, 154f);
            _doubleRewardAdBtnText = _doubleRewardAdBtn.GetComponentInChildren<Text>();
            _doubleRewardAdBtn.onClick.AddListener(OnWatchAdDoubleMatchReward);

            // Play Again / Next Stage 3D Button
            _resultPlayAgainBtn = CreateIcon3DButton(cardRT, XOProceduralAssets.IconNewGame, "CONTINUE", new Vector2(620f, 62f), XOProceduralAssets.ColMint, XOProceduralAssets.ColInk, 21);
            RectTransform againRT = _resultPlayAgainBtn.GetComponent<RectTransform>();
            againRT.anchorMin = new Vector2(0.5f, 0f);
            againRT.anchorMax = new Vector2(0.5f, 0f);
            againRT.pivot = new Vector2(0.5f, 0f);
            againRT.anchoredPosition = new Vector2(0f, 82f);
            _resultPlayAgainBtnText = _resultPlayAgainBtn.GetComponentInChildren<Text>();
            _resultPlayAgainBtn.onClick.AddListener(() =>
            {
                PlaySfx("click");
                _resultModalRoot.SetActive(false);
                if (currentGameMode == GameMode.TowerClimb)
                {
                    if (XORoguelikeRunManager.Lives <= 0)
                    {
                        XORoguelikeRunManager.StartNewRun();
                        ReturnToLobby();
                        OpenTowerModal();
                    }
                    else
                    {
                        StartTowerMatch(XORoguelikeRunManager.CurrentFloor);
                    }
                }
                else
                {
                    StartNewMatch(currentBoardSize);
                }
            });

            // Return to Main Menu / Lobby Button
            Button returnToLobbyBtn = CreateIcon3DButton(cardRT, XOProceduralAssets.IconHome, "BACK TO HUB", new Vector2(620f, 54f), XOProceduralAssets.ColTileEdge, XOProceduralAssets.ColInk, 19);
            RectTransform lbyRT = returnToLobbyBtn.GetComponent<RectTransform>();
            lbyRT.anchorMin = new Vector2(0.5f, 0f);
            lbyRT.anchorMax = new Vector2(0.5f, 0f);
            lbyRT.pivot = new Vector2(0.5f, 0f);
            lbyRT.anchoredPosition = new Vector2(0f, 14f);
            returnToLobbyBtn.onClick.AddListener(() =>
            {
                PlaySfx("click");
                _resultModalRoot.SetActive(false);
                ReturnToLobby();
            });

            _resultModalRoot.SetActive(false);
        }

        private ScrollRect CreateModalScrollView(string name, RectTransform cardRT, out RectTransform contentRT, out GameObject scrollObj)
        {
            scrollObj = CreateUIObject(name, cardRT);
            RectTransform svRT = scrollObj.GetComponent<RectTransform>();
            svRT.anchorMin = new Vector2(0f, 0f);
            svRT.anchorMax = new Vector2(1f, 1f);
            svRT.offsetMin = new Vector2(24f, 24f);
            svRT.offsetMax = new Vector2(-24f, -164f);

            scrollObj.AddComponent<RectMask2D>();

            ScrollRect sr = scrollObj.AddComponent<ScrollRect>();
            sr.horizontal = false;
            sr.vertical = true;
            sr.movementType = ScrollRect.MovementType.Clamped;
            sr.scrollSensitivity = 28f;
            sr.inertia = true;
            sr.decelerationRate = 0.135f;

            GameObject contentObj = CreateUIObject("Content", svRT);
            contentRT = contentObj.GetComponent<RectTransform>();
            contentRT.anchorMin = new Vector2(0f, 1f);
            contentRT.anchorMax = new Vector2(1f, 1f);
            contentRT.pivot = new Vector2(0.5f, 1f);
            contentRT.anchoredPosition = Vector2.zero;
            contentRT.sizeDelta = Vector2.zero;

            VerticalLayoutGroup vLayout = contentObj.AddComponent<VerticalLayoutGroup>();
            vLayout.spacing = 10f;
            vLayout.childControlWidth = true;
            vLayout.childControlHeight = false;
            vLayout.childForceExpandWidth = true;
            vLayout.childForceExpandHeight = false;
            vLayout.padding = new RectOffset(4, 4, 4, 16);

            ContentSizeFitter csf = contentObj.AddComponent<ContentSizeFitter>();
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            sr.viewport = svRT;
            sr.content = contentRT;
            return sr;
        }
    }
}
