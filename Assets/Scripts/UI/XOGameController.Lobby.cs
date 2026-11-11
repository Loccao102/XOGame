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
        // SCREEN 1: LOBBY / MAIN MENU UI
        // =====================================================================

        private void BuildLobbyScreen(RectTransform parent)
        {
            // 1. Top Bar (Coin pill, Streak pill, Round sound toggle)
            GameObject headerCard = CreateUIObject("LobbyHeaderCard", parent);
            _headerCardRT = headerCard.GetComponent<RectTransform>();
            BuildLobbyHeaderContent(_headerCardRT);

            // 2. Hero Card (Title "Caro 5", 5x5 mini board, Rank row, Big Mint Play, Mode & Rival chips)
            GameObject heroCard = CreateUIObject("LobbyHeroCard", parent);
            _lobbyHeroCardRT = heroCard.GetComponent<RectTransform>();
            _lobbyHeroCardImg = heroCard.AddComponent<Image>();
            _lobbyHeroCardImg.sprite = XOProceduralAssets.CardPanelSprite;
            _lobbyHeroCardImg.type = Image.Type.Sliced;
            _lobbyHeroCardImg.color = XOProceduralAssets.ColCardBg;
            BuildLobbyHeroContent(_lobbyHeroCardRT);

            // 3. Three Equal Tiles Under Hero (Rivals, Spin with badge "Free", Skins)
            GameObject hubGrid = CreateUIObject("LobbyHubGrid", parent);
            _lobbyHubGridRT = hubGrid.GetComponent<RectTransform>();
            BuildLobbyHubContent(_lobbyHubGridRT);

            // 4. Daily Quest Card (Text, Progress bar, Chest button)
            GameObject questCard = CreateUIObject("LobbyQuestCard", parent);
            _questCardRT = questCard.GetComponent<RectTransform>();
            _questCardImg = questCard.AddComponent<Image>();
            _questCardImg.sprite = XOProceduralAssets.CardPanelSprite;
            _questCardImg.type = Image.Type.Sliced;
            _questCardImg.color = XOProceduralAssets.ColCardBg;
            BuildLobbyQuestAndChestContent(_questCardRT);
        }

        private void BuildLobbyHeaderContent(RectTransform parent)
        {
            // --- Coin Pill (White Translucent Capsule) ---
            GameObject coinPill = CreateUIObject("CoinPill", parent);
            RectTransform coinRT = coinPill.GetComponent<RectTransform>();
            coinRT.anchorMin = new Vector2(0f, 0.5f);
            coinRT.anchorMax = new Vector2(0f, 0.5f);
            coinRT.pivot = new Vector2(0f, 0.5f);
            coinRT.anchoredPosition = new Vector2(0f, 0f);
            coinRT.sizeDelta = new Vector2(190f, 56f);

            Image coinPillBg = coinPill.AddComponent<Image>();
            coinPillBg.sprite = XOProceduralAssets.Pill3DSprite;
            coinPillBg.type = Image.Type.Sliced;
            coinPillBg.color = Color.white;

            CreateIconImage(coinRT, XOProceduralAssets.IconCoin, new Vector2(0f, 0.5f), new Vector2(30f, 0f), new Vector2(38f, 38f));
            _coinText = CreateText(coinRT, $"{Coins:N0}", 22, FontStyle.Bold, TextAnchor.MiddleLeft);
            _coinText.rectTransform.offsetMin = new Vector2(56f, 0f);
            _coinText.rectTransform.offsetMax = new Vector2(-12f, 0f);
            _coinText.color = XOProceduralAssets.ColInk;

            // --- Streak Pill (White Translucent Capsule) ---
            GameObject streakPill = CreateUIObject("StreakPill", parent);
            RectTransform streakRT = streakPill.GetComponent<RectTransform>();
            streakRT.anchorMin = new Vector2(0f, 0.5f);
            streakRT.anchorMax = new Vector2(0f, 0.5f);
            streakRT.pivot = new Vector2(0f, 0.5f);
            streakRT.anchoredPosition = new Vector2(206f, 0f);
            streakRT.sizeDelta = new Vector2(180f, 56f);

            Image streakPillBg = streakPill.AddComponent<Image>();
            streakPillBg.sprite = XOProceduralAssets.Pill3DSprite;
            streakPillBg.type = Image.Type.Sliced;
            streakPillBg.color = Color.white;

            CreateIconImage(streakRT, XOProceduralAssets.IconFlame, new Vector2(0f, 0.5f), new Vector2(28f, 0f), new Vector2(36f, 36f));
            _streakText = CreateText(streakRT, $"{XOProgressionSystem.WinStreak} streak", 20, FontStyle.Bold, TextAnchor.MiddleLeft);
            _streakText.rectTransform.offsetMin = new Vector2(54f, 0f);
            _streakText.rectTransform.offsetMax = new Vector2(-12f, 0f);
            _streakText.color = XOProceduralAssets.ColInk;

            // --- Sound Toggle (Round 56x56 Clay Button) ---
            GameObject muteBtnObj = CreateUIObject("MuteButton", parent);
            RectTransform muteRT = muteBtnObj.GetComponent<RectTransform>();
            muteRT.anchorMin = new Vector2(1f, 0.5f);
            muteRT.anchorMax = new Vector2(1f, 0.5f);
            muteRT.pivot = new Vector2(1f, 0.5f);
            muteRT.anchoredPosition = new Vector2(0f, 0f);
            muteRT.sizeDelta = new Vector2(56f, 56f);

            Image muteBg = muteBtnObj.AddComponent<Image>();
            muteBg.sprite = XOProceduralAssets.Button3DSprite;
            muteBg.type = Image.Type.Sliced;
            muteBg.color = Color.white;

            Button muteBtn = muteBtnObj.AddComponent<Button>();
            _muteButtonIconImg = CreateIconImage(muteRT, IsSoundMuted ? XOProceduralAssets.IconSoundOff : XOProceduralAssets.IconSoundOn, new Vector2(0.5f, 0.5f), new Vector2(0f, 1f), new Vector2(36f, 36f));
            muteBtn.onClick.AddListener(() =>
            {
                IsSoundMuted = !IsSoundMuted;
                PlaySfx("click");
            });
        }

        private void RefreshLobbyHeroUI()
        {
            if (_lobbyModeBannerTitle != null)
                _lobbyModeBannerTitle.text = "Caro 5";
            if (_lobbyModeBannerDesc != null)
                _lobbyModeBannerDesc.text = "Classic five in a row";
        }

        private void BuildLobbyHeroContent(RectTransform parent)
        {
            // Title: "Caro 5" (Sentence case, bold ink #2A3566, tight tracking)
            _lobbyModeBannerTitle = CreateText(parent, "Caro 5", 40, FontStyle.Bold, TextAnchor.MiddleCenter);
            _lobbyModeBannerTitle.rectTransform.anchorMin = new Vector2(0f, 1f);
            _lobbyModeBannerTitle.rectTransform.anchorMax = new Vector2(1f, 1f);
            _lobbyModeBannerTitle.rectTransform.pivot = new Vector2(0.5f, 1f);
            _lobbyModeBannerTitle.rectTransform.anchoredPosition = new Vector2(0f, -14f);
            _lobbyModeBannerTitle.rectTransform.sizeDelta = new Vector2(-40f, 48f);
            _lobbyModeBannerTitle.color = XOProceduralAssets.ColInk;

            // Subtitle: "Classic five in a row"
            _lobbyModeBannerDesc = CreateText(parent, "Classic five in a row", 17, FontStyle.Normal, TextAnchor.MiddleCenter);
            _lobbyModeBannerDesc.rectTransform.anchorMin = new Vector2(0f, 1f);
            _lobbyModeBannerDesc.rectTransform.anchorMax = new Vector2(1f, 1f);
            _lobbyModeBannerDesc.rectTransform.pivot = new Vector2(0.5f, 1f);
            _lobbyModeBannerDesc.rectTransform.anchoredPosition = new Vector2(0f, -60f);
            _lobbyModeBannerDesc.rectTransform.sizeDelta = new Vector2(-60f, 28f);
            _lobbyModeBannerDesc.color = XOProceduralAssets.ColMuted;

            // Hero Emblem / Badge Centerpiece (Replaces confusing fake 5x5 pre-placed board!)
            GameObject emblemObj = CreateUIObject("HeroEmblemBox", parent);
            RectTransform ebRT = emblemObj.GetComponent<RectTransform>();
            ebRT.anchorMin = new Vector2(0.5f, 0.5f);
            ebRT.anchorMax = new Vector2(0.5f, 0.5f);
            ebRT.pivot = new Vector2(0.5f, 0.5f);
            ebRT.anchoredPosition = new Vector2(0f, 48f);
            ebRT.sizeDelta = new Vector2(200f, 200f);

            Image ebBg = emblemObj.AddComponent<Image>();
            ebBg.sprite = XOProceduralAssets.SoftCellSprite;
            ebBg.type = Image.Type.Sliced;
            ebBg.color = XOThemeDatabase.Hex("#F0F9FF"); // Soft Sky porcelain

            // Large 3D Crossed Swords & Golden Crown Hero Icon
            CreateIconImage(ebRT, XOProceduralAssets.IconSwords, new Vector2(0.5f, 0.5f), new Vector2(0f, -6f), new Vector2(108f, 108f));
            CreateIconImage(ebRT, XOProceduralAssets.IconCrown, new Vector2(0.5f, 1f), new Vector2(0f, -22f), new Vector2(50f, 50f));

            // Rank Row: 4 Dots + "Silver master"
            GameObject rankRow = CreateUIObject("RankRow", parent);
            RectTransform rankRT = rankRow.GetComponent<RectTransform>();
            rankRT.anchorMin = new Vector2(0.5f, 0.5f);
            rankRT.anchorMax = new Vector2(0.5f, 0.5f);
            rankRT.pivot = new Vector2(0.5f, 0.5f);
            rankRT.anchoredPosition = new Vector2(0f, -68f);
            rankRT.sizeDelta = new Vector2(320f, 34f);

            // 4 Dots
            for (int i = 0; i < 4; i++)
            {
                GameObject dotObj = CreateUIObject($"Dot_{i}", rankRT);
                RectTransform dotRT = dotObj.GetComponent<RectTransform>();
                dotRT.anchorMin = new Vector2(0f, 0.5f);
                dotRT.anchorMax = new Vector2(0f, 0.5f);
                dotRT.pivot = new Vector2(0f, 0.5f);
                dotRT.anchoredPosition = new Vector2(18f + i * 18f, 0f);
                dotRT.sizeDelta = new Vector2(13f, 13f);

                Image dotImg = dotObj.AddComponent<Image>();
                dotImg.sprite = XOProceduralAssets.CircleSprite;
                dotImg.color = (i < 3) ? XOProceduralAssets.ColSun : new Color(0.16f, 0.21f, 0.40f, 0.16f);
            }

            _rankBadgeText = CreateText(rankRT, "Bronze Novice (1/3)", 18, FontStyle.Bold, TextAnchor.MiddleLeft);
            _rankBadgeText.rectTransform.offsetMin = new Vector2(96f, 0f);
            _rankBadgeText.rectTransform.offsetMax = new Vector2(0f, 0f);
            _rankBadgeText.color = XOProceduralAssets.ColInk;

            // Big Mint Play Button (82px tall, #4ED6A4 with #2EA375 bottom edge)
            _lobbyPlayBtn = CreateIcon3DButton(parent, XOProceduralAssets.IconNewGame, "PLAY MATCH (15x15)", new Vector2(460f, 82f), XOProceduralAssets.ColMint, XOProceduralAssets.ColInk, 26);
            RectTransform playRT = _lobbyPlayBtn.GetComponent<RectTransform>();
            playRT.anchorMin = new Vector2(0.5f, 0f);
            playRT.anchorMax = new Vector2(0.5f, 0f);
            playRT.pivot = new Vector2(0.5f, 0f);
            playRT.anchoredPosition = new Vector2(0f, 86f);
            _lobbyPlayBtn.onClick.AddListener(() =>
            {
                PlaySfx("click");
                StartNewMatch(currentBoardSize);
                SetScreenState(ScreenState.Battle);
            });

            // Two Half-Width Chips Below Play: Mode (Sky) and Rival (Lilac)
            GameObject chipsContainer = CreateUIObject("ChipsRow", parent);
            RectTransform chipsRT = chipsContainer.GetComponent<RectTransform>();
            chipsRT.anchorMin = new Vector2(0.5f, 0f);
            chipsRT.anchorMax = new Vector2(0.5f, 0f);
            chipsRT.pivot = new Vector2(0.5f, 0f);
            chipsRT.anchoredPosition = new Vector2(0f, 14f);
            chipsRT.sizeDelta = new Vector2(460f, 62f);

            // Left Chip: Mode (Sky #4FB0FF)
            _lobbyChangeModeBtn = CreateTwoLineChipButton(chipsRT, "Mode", "Pro 15x15", XOProceduralAssets.ColSky, new Vector2(222f, 62f), () =>
            {
                PlaySfx("click");
                _modeSelectModalRoot.SetActive(true);
            }, out Text modeValueTxt);
            RectTransform modeChipRT = _lobbyChangeModeBtn.GetComponent<RectTransform>();
            modeChipRT.anchorMin = new Vector2(0f, 0.5f);
            modeChipRT.anchorMax = new Vector2(0f, 0.5f);
            modeChipRT.pivot = new Vector2(0f, 0.5f);
            modeChipRT.anchoredPosition = new Vector2(0f, 0f);

            // Right Chip: Rival (Lilac #A998FF)
            _lobbyChangeRivalBtn = CreateTwoLineChipButton(chipsRT, "Rival", "Master Leo", XOProceduralAssets.ColLilac, new Vector2(222f, 62f), () =>
            {
                OpenLeaderboardModal();
            }, out _lobbyRivalBannerText);
            RectTransform rivalChipRT = _lobbyChangeRivalBtn.GetComponent<RectTransform>();
            rivalChipRT.anchorMin = new Vector2(1f, 0.5f);
            rivalChipRT.anchorMax = new Vector2(1f, 0.5f);
            rivalChipRT.pivot = new Vector2(1f, 0.5f);
            rivalChipRT.anchoredPosition = new Vector2(0f, 0f);
        }

        private void BuildLobbyHubContent(RectTransform parent)
        {
            GridLayoutGroup grid = parent.gameObject.GetComponent<GridLayoutGroup>();
            if (grid == null) grid = parent.gameObject.AddComponent<GridLayoutGroup>();
            _lobbyHubGridLayout = grid;
            grid.childAlignment = TextAnchor.MiddleCenter;
            grid.spacing = new Vector2(16f, 14f);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 2;
            grid.startCorner = GridLayoutGroup.Corner.UpperLeft;
            grid.startAxis = GridLayoutGroup.Axis.Horizontal;

            Vector2 tileDimen = new Vector2(246f, 82f);
            grid.cellSize = tileDimen;

            // 1. Rivals Tile
            _lobbyHubArenaBtn = CreateIcon3DButton(parent, XOProceduralAssets.IconLeaderboard, "Rivals", tileDimen, Color.white, XOProceduralAssets.ColInk, 18);
            _lobbyHubArenaBtn.onClick.AddListener(OpenLeaderboardModal);
            _leaderboardBtn = _lobbyHubArenaBtn;

            // Rivals Badge (ELO / Rank)
            RectTransform arenaRT = _lobbyHubArenaBtn.GetComponent<RectTransform>();
            GameObject rivalsBadge = CreateUIObject("BadgeRivals", arenaRT);
            RectTransform rbRT = rivalsBadge.GetComponent<RectTransform>();
            rbRT.anchorMin = new Vector2(1f, 1f);
            rbRT.anchorMax = new Vector2(1f, 1f);
            rbRT.pivot = new Vector2(1f, 1f);
            rbRT.anchoredPosition = new Vector2(-6f, -6f);
            rbRT.sizeDelta = new Vector2(56f, 26f);
            Image rbBg = rivalsBadge.AddComponent<Image>();
            rbBg.sprite = XOProceduralAssets.PillSprite;
            rbBg.type = Image.Type.Sliced;
            rbBg.color = XOProceduralAssets.ColSky;
            rbBg.raycastTarget = false;
            _leaderboardBadgeBg = rbBg;
            _leaderboardBadgeText = CreateText(rbRT, "RANK", 13, FontStyle.Bold, TextAnchor.MiddleCenter);
            _leaderboardBadgeText.color = Color.white;

            // 2. Tower Climb Tile (10-Floor Roguelike & Boss Equipment)
            _lobbyHubTowerBtn = CreateIcon3DButton(parent, XOProceduralAssets.IconCrown, "Tower", tileDimen, Color.white, XOProceduralAssets.ColInk, 18);
            _lobbyHubTowerBtn.onClick.AddListener(OpenTowerModal);

            // Floor Badge on Tower
            RectTransform towerRT = _lobbyHubTowerBtn.GetComponent<RectTransform>();
            GameObject towerBadge = CreateUIObject("BadgeTower", towerRT);
            RectTransform tbRT = towerBadge.GetComponent<RectTransform>();
            tbRT.anchorMin = new Vector2(1f, 1f);
            tbRT.anchorMax = new Vector2(1f, 1f);
            tbRT.pivot = new Vector2(1f, 1f);
            tbRT.anchoredPosition = new Vector2(-6f, -6f);
            tbRT.sizeDelta = new Vector2(52f, 26f);
            Image tbBg = towerBadge.AddComponent<Image>();
            tbBg.sprite = XOProceduralAssets.PillSprite;
            tbBg.type = Image.Type.Sliced;
            tbBg.color = XOProceduralAssets.ColLilac;
            tbBg.raycastTarget = false;

            Text tbText = CreateText(tbRT, $"F.{XORoguelikeRunManager.CurrentFloor}", 13, FontStyle.Bold, TextAnchor.MiddleCenter);
            tbText.color = Color.white;
            _towerBadgeText = tbText;

            // 3. Spin Tile with Badge "Free"
            _lobbyHubWheelBtn = CreateIcon3DButton(parent, XOProceduralAssets.IconLuckyWheel, "Spin", tileDimen, Color.white, XOProceduralAssets.ColInk, 18);
            _lobbyHubWheelBtn.onClick.AddListener(OpenLuckyWheelModal);

            // Badge "Free" in coral #FF6F85
            RectTransform spinRT = _lobbyHubWheelBtn.GetComponent<RectTransform>();
            GameObject freeBadge = CreateUIObject("BadgeFree", spinRT);
            RectTransform fbRT = freeBadge.GetComponent<RectTransform>();
            fbRT.anchorMin = new Vector2(1f, 1f);
            fbRT.anchorMax = new Vector2(1f, 1f);
            fbRT.pivot = new Vector2(1f, 1f);
            fbRT.anchoredPosition = new Vector2(-6f, -6f);
            fbRT.sizeDelta = new Vector2(52f, 26f);
            Image fbBg = freeBadge.AddComponent<Image>();
            fbBg.sprite = XOProceduralAssets.PillSprite;
            fbBg.type = Image.Type.Sliced;
            fbBg.color = XOProceduralAssets.ColCoral;
            fbBg.raycastTarget = false;

            Text fbText = CreateText(fbRT, "Free", 13, FontStyle.Bold, TextAnchor.MiddleCenter);
            fbText.color = Color.white;

            // 4. Skins Tile
            _lobbyHubShopBtn = CreateIcon3DButton(parent, XOProceduralAssets.IconSkinShop, "Skins", tileDimen, Color.white, XOProceduralAssets.ColInk, 18);
            _lobbyHubShopBtn.onClick.AddListener(OpenThemeShopModal);
            _skinShopBtn = _lobbyHubShopBtn;
            _lobbyHubModeBtn = _lobbyChangeModeBtn;

            // Badge "Shop" on Skins
            RectTransform skinsRT = _lobbyHubShopBtn.GetComponent<RectTransform>();
            GameObject shopBadge = CreateUIObject("BadgeSkins", skinsRT);
            RectTransform sbRT = shopBadge.GetComponent<RectTransform>();
            sbRT.anchorMin = new Vector2(1f, 1f);
            sbRT.anchorMax = new Vector2(1f, 1f);
            sbRT.pivot = new Vector2(1f, 1f);
            sbRT.anchoredPosition = new Vector2(-6f, -6f);
            sbRT.sizeDelta = new Vector2(52f, 26f);
            Image sbBg = shopBadge.AddComponent<Image>();
            sbBg.sprite = XOProceduralAssets.PillSprite;
            sbBg.type = Image.Type.Sliced;
            sbBg.color = XOProceduralAssets.ColMint;
            sbBg.raycastTarget = false;

            Text sbText = CreateText(sbRT, "Shop", 13, FontStyle.Bold, TextAnchor.MiddleCenter);
            sbText.color = XOProceduralAssets.ColInk;
        }

        private void OnClaimWinChestClicked()
        {
            PlaySfx("chest");
            Coins += 5;
            RefreshHeaderAndProgressionUI();
        }

        private void BuildLobbyQuestAndChestContent(RectTransform parent)
        {
            // Left Column: "Daily quest", "Win 2 matches (1/2)", and progress bar
            _missionText = CreateText(parent, "Daily quest • Win 2 matches", 18, FontStyle.Bold, TextAnchor.MiddleLeft);
            _missionText.rectTransform.anchorMin = new Vector2(0f, 0.5f);
            _missionText.rectTransform.anchorMax = new Vector2(1f, 0.5f);
            _missionText.rectTransform.pivot = new Vector2(0f, 0.5f);
            _missionText.rectTransform.anchoredPosition = new Vector2(24f, 16f);
            _missionText.rectTransform.sizeDelta = new Vector2(-120f, 26f);
            _missionText.color = XOProceduralAssets.ColInk;

            // Progress Track Bar
            GameObject trackObj = CreateUIObject("ProgressTrack", parent);
            RectTransform trackRT = trackObj.GetComponent<RectTransform>();
            trackRT.anchorMin = new Vector2(0f, 0.5f);
            trackRT.anchorMax = new Vector2(1f, 0.5f);
            trackRT.pivot = new Vector2(0f, 0.5f);
            trackRT.anchoredPosition = new Vector2(24f, -16f);
            trackRT.sizeDelta = new Vector2(-120f, 12f);

            Image trackBg = trackObj.AddComponent<Image>();
            trackBg.sprite = XOProceduralAssets.PillSprite;
            trackBg.type = Image.Type.Sliced;
            trackBg.color = new Color(0.16f, 0.21f, 0.40f, 0.08f);

            GameObject fillObj = CreateUIObject("ProgressFill", trackRT);
            RectTransform fillRT = fillObj.GetComponent<RectTransform>();
            fillRT.anchorMin = new Vector2(0f, 0f);
            fillRT.anchorMax = new Vector2(0.5f, 1f); // 50% progress (1/2)
            fillRT.offsetMin = Vector2.zero;
            fillRT.offsetMax = Vector2.zero;

            Image fillImg = fillObj.AddComponent<Image>();
            fillImg.sprite = XOProceduralAssets.PillSprite;
            fillImg.type = Image.Type.Sliced;
            fillImg.color = XOProceduralAssets.ColMint;

            // Right Button: Chest Button with IconChest
            _chestButton = CreateIcon3DButton(parent, XOProceduralAssets.IconChest, "", new Vector2(70f, 70f), XOProceduralAssets.ColSun, Color.white, 1);
            RectTransform chestRT = _chestButton.GetComponent<RectTransform>();
            chestRT.anchorMin = new Vector2(1f, 0.5f);
            chestRT.anchorMax = new Vector2(1f, 0.5f);
            chestRT.pivot = new Vector2(1f, 0.5f);
            chestRT.anchoredPosition = new Vector2(-18f, 0f);
            _chestButton.onClick.AddListener(OnClaimWinChestClicked);
        }

        private Button CreateTwoLineChipButton(RectTransform parent, string label, string value, Color bgCol, Vector2 size, UnityEngine.Events.UnityAction onClick, out Text valueTxt)
        {
            GameObject btnObj = CreateUIObject($"Chip_{label}", parent);
            RectTransform rt = btnObj.GetComponent<RectTransform>();
            rt.sizeDelta = size;

            Image bg = btnObj.AddComponent<Image>();
            bg.sprite = XOProceduralAssets.Button3DSprite;
            bg.type = Image.Type.Sliced;
            bg.color = bgCol;

            Button btn = btnObj.AddComponent<Button>();
            if (onClick != null) btn.onClick.AddListener(onClick);

            Text lbl = CreateText(rt, label, 11, FontStyle.Normal, TextAnchor.MiddleCenter);
            lbl.rectTransform.anchorMin = new Vector2(0f, 0.5f);
            lbl.rectTransform.anchorMax = new Vector2(1f, 0.5f);
            lbl.rectTransform.anchoredPosition = new Vector2(0f, 10f);
            lbl.rectTransform.sizeDelta = new Vector2(0f, 18f);
            lbl.color = new Color(0.16f, 0.21f, 0.40f, 0.75f);

            valueTxt = CreateText(rt, value, 14, FontStyle.Bold, TextAnchor.MiddleCenter);
            valueTxt.rectTransform.anchorMin = new Vector2(0f, 0.5f);
            valueTxt.rectTransform.anchorMax = new Vector2(1f, 0.5f);
            valueTxt.rectTransform.anchoredPosition = new Vector2(0f, -8f);
            valueTxt.rectTransform.sizeDelta = new Vector2(0f, 20f);
            valueTxt.color = XOProceduralAssets.ColInk;

            return btn;
        }
    }
}
