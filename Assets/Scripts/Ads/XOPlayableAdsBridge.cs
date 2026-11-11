using System;
using System.Collections;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.UI;
using XOPlayable.Core;
using XOPlayable.Visuals;

namespace XOPlayable.Ads
{
    /// <summary>
    /// Universal Playable & Rewarded Ads Bridge for:
    /// - Google Ads App Campaign Playables (ExitApi.exit)
    /// - YouTube Playables SDK (ytgame.game.firstFrameReady / gameReady / sendScore / onAudioEnabledChange)
    /// - MRAID 2.0 / DAPI / Meta Playable Ads
    /// - In-game simulated Rewarded Video Ad flow for instant playable testing
    /// </summary>
    public class XOPlayableAdsBridge : MonoBehaviour
    {
        public static XOPlayableAdsBridge Instance { get; private set; }

#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")]
        private static extern void XOPlayable_InitSDK();

        [DllImport("__Internal")]
        private static extern void XOPlayable_TriggerCTA(string url);

        [DllImport("__Internal")]
        private static extern void XOPlayable_SendScore(int score);

        [DllImport("__Internal")]
        private static extern void XOPlayable_TriggerHaptic(int durationMs);
#endif

        [Header("Google Playable CTA Settings")]
        public string googlePlayStoreUrl = "https://play.google.com/store/apps";
        public float simulatedAdDuration = 2.0f;

        [Header("Playable Auto End-Card Settings")]
        [Tooltip("If > 0, automatically highlights the Install CTA after N completed matches.")]
        public int matchesBeforeAutoInstallPrompt = 2;

        public event Action<string, Action, Action> CustomRewardedAdProvider;

        private GameObject _adModalRoot;
        private Text _adRewardDescText;
        private Text _adTimerText;
        private Image _adProgressBarFill;
        private Button _adCloseClaimButton;
        private Text _adCloseClaimButtonText;

        private bool _isShowingAd;
        private Action _currentOnRewarded;
        private Action _currentOnSkipped;
        private Coroutine _adCoroutine;

        public int TotalRewardedAdsWatched
        {
            get => PlayerPrefs.GetInt("XO_TotalAdsWatched", 0);
            private set => PlayerPrefs.SetInt("XO_TotalAdsWatched", value);
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private const string PlaceholderStoreUrl = "https://play.google.com/store/apps";

        private void WarnIfStoreUrlIsPlaceholder()
        {
            if (string.IsNullOrWhiteSpace(googlePlayStoreUrl) || googlePlayStoreUrl.TrimEnd('/') == PlaceholderStoreUrl)
                Debug.LogWarning("[XO Playable] googlePlayStoreUrl is still the placeholder. Set your real store listing before shipping, otherwise the Install CTA opens the generic Play Store home page.");
        }

        private void OnValidate()
        {
            WarnIfStoreUrlIsPlaceholder();
        }

        private void Start()
        {
            WarnIfStoreUrlIsPlaceholder();
#if UNITY_WEBGL && !UNITY_EDITOR
            XOPlayable_InitSDK();
#endif
        }

        /// <summary>
        /// Mandatory Google Playables & YouTube Playables requirement:
        /// Mute all audio when the browser tab or webview loses focus or is paused.
        /// </summary>
        private void OnApplicationFocus(bool hasFocus)
        {
            AudioListener.pause = !hasFocus;
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            AudioListener.pause = pauseStatus;
        }

        // Called from XOPlayableWebGLBridge.jslib (YouTube Playables ytgame.system.onAudioEnabledChange)
        public void OnExternalAudioStateChanged(int isEnabled)
        {
            AudioListener.pause = (isEnabled == 0);
            AudioListener.volume = (isEnabled == 0) ? 0f : 1f;
        }

        // Called from XOPlayableWebGLBridge.jslib (YouTube Playables ytgame.system.onPause / onResume)
        public void OnExternalPauseStateChanged(int isPaused)
        {
            Time.timeScale = (isPaused == 1) ? 0f : 1f;
            AudioListener.pause = (isPaused == 1);
        }

        public void ReportPlayableScore(int score)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            XOPlayable_SendScore(score);
#else
            Debug.Log($"[Playable SDK] Reported high score to host container: {score}");
#endif
        }

        public void TriggerHaptic(int durationMs = 20)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            XOPlayable_TriggerHaptic(durationMs);
#elif (UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR
            if (durationMs >= 35)
                Handheld.Vibrate();
#endif
        }

        public void BuildAdOverlayUI(Transform parentCanvas, Font uiFont)
        {
            if (_adModalRoot != null) return;

            _adModalRoot = new GameObject("RewardedAdOverlay", typeof(RectTransform), typeof(CanvasGroup), typeof(Image));
            _adModalRoot.transform.SetParent(parentCanvas, false);

            RectTransform rootRT = _adModalRoot.GetComponent<RectTransform>();
            rootRT.anchorMin = Vector2.zero;
            rootRT.anchorMax = Vector2.one;
            rootRT.offsetMin = Vector2.zero;
            rootRT.offsetMax = Vector2.zero;

            Image bgImg = _adModalRoot.GetComponent<Image>();
            bgImg.color = new Color(0.02f, 0.04f, 0.08f, 0.92f);

            // Center 3D Beveled Card
            GameObject card = new GameObject("AdCard", typeof(RectTransform), typeof(Image));
            card.transform.SetParent(_adModalRoot.transform, false);
            RectTransform cardRT = card.GetComponent<RectTransform>();
            cardRT.anchorMin = new Vector2(0.5f, 0.5f);
            cardRT.anchorMax = new Vector2(0.5f, 0.5f);
            cardRT.sizeDelta = new Vector2(720f, 520f);

            Image cardImg = card.GetComponent<Image>();
            cardImg.sprite = XOProceduralAssets.CardPanelSprite;
            cardImg.type = Image.Type.Sliced;
            cardImg.color = XOThemeDatabase.Hex("#1E293B");

            // Top Ad Pill Badge with Video Icon
            GameObject badge = new GameObject("AdBadge", typeof(RectTransform), typeof(Image));
            badge.transform.SetParent(card.transform, false);
            RectTransform badgeRT = badge.GetComponent<RectTransform>();
            badgeRT.anchorMin = new Vector2(0.5f, 1f);
            badgeRT.anchorMax = new Vector2(0.5f, 1f);
            badgeRT.anchoredPosition = new Vector2(0f, -44f);
            badgeRT.sizeDelta = new Vector2(340f, 50f);
            Image badgeImg = badge.GetComponent<Image>();
            badgeImg.sprite = XOProceduralAssets.PillSprite;
            badgeImg.type = Image.Type.Sliced;
            badgeImg.color = XOThemeDatabase.Hex("#F59E0B");

            GameObject adIconObj = new GameObject("AdIcon", typeof(RectTransform), typeof(Image));
            adIconObj.transform.SetParent(badge.transform, false);
            RectTransform adIconRT = adIconObj.GetComponent<RectTransform>();
            adIconRT.anchorMin = new Vector2(0f, 0.5f);
            adIconRT.anchorMax = new Vector2(0f, 0.5f);
            adIconRT.anchoredPosition = new Vector2(32f, 0f);
            adIconRT.sizeDelta = new Vector2(34f, 34f);
            adIconObj.GetComponent<Image>().sprite = XOProceduralAssets.IconVideoAd;

            Text badgeTxt = CreateText(badge.transform, "REWARDED VIDEO AD", uiFont, 21, FontStyle.Bold, XOThemeDatabase.Hex("#0F172A"));
            badgeTxt.rectTransform.offsetMin = new Vector2(44f, 0f);

            // Center Chest Icon
            GameObject chestIconObj = new GameObject("RewardChestIcon", typeof(RectTransform), typeof(Image));
            chestIconObj.transform.SetParent(card.transform, false);
            RectTransform chestRT = chestIconObj.GetComponent<RectTransform>();
            chestRT.anchorMin = new Vector2(0.5f, 0.5f);
            chestRT.anchorMax = new Vector2(0.5f, 0.5f);
            chestRT.anchoredPosition = new Vector2(0f, 75f);
            chestRT.sizeDelta = new Vector2(115f, 115f);
            chestIconObj.GetComponent<Image>().sprite = XOProceduralAssets.IconChest;

            // Reward Description
            _adRewardDescText = CreateText(card.transform, "Reward: +2 Undo Moves", uiFont, 27, FontStyle.Bold, Color.white);
            RectTransform descRT = _adRewardDescText.GetComponent<RectTransform>();
            descRT.anchorMin = new Vector2(0.5f, 0.5f);
            descRT.anchorMax = new Vector2(0.5f, 0.5f);
            descRT.anchoredPosition = new Vector2(0f, -20f);
            descRT.sizeDelta = new Vector2(640f, 70f);

            // Progress Bar
            GameObject barBg = new GameObject("ProgressBarBG", typeof(RectTransform), typeof(Image));
            barBg.transform.SetParent(card.transform, false);
            RectTransform barBgRT = barBg.GetComponent<RectTransform>();
            barBgRT.anchorMin = new Vector2(0.5f, 0.5f);
            barBgRT.anchorMax = new Vector2(0.5f, 0.5f);
            barBgRT.anchoredPosition = new Vector2(0f, -88f);
            barBgRT.sizeDelta = new Vector2(560f, 26f);
            Image barBgImg = barBg.GetComponent<Image>();
            barBgImg.sprite = XOProceduralAssets.PillSprite;
            barBgImg.type = Image.Type.Sliced;
            barBgImg.color = XOThemeDatabase.Hex("#0F172A");

            GameObject barFill = new GameObject("ProgressBarFill", typeof(RectTransform), typeof(Image));
            barFill.transform.SetParent(barBg.transform, false);
            RectTransform barFillRT = barFill.GetComponent<RectTransform>();
            barFillRT.anchorMin = new Vector2(0f, 0f);
            barFillRT.anchorMax = new Vector2(1f, 1f);
            barFillRT.offsetMin = new Vector2(4f, 4f);
            barFillRT.offsetMax = new Vector2(-4f, -4f);
            _adProgressBarFill = barFill.GetComponent<Image>();
            _adProgressBarFill.sprite = XOProceduralAssets.PillSprite;
            _adProgressBarFill.type = Image.Type.Filled;
            _adProgressBarFill.fillMethod = Image.FillMethod.Horizontal;
            _adProgressBarFill.fillAmount = 0f;
            _adProgressBarFill.color = XOThemeDatabase.Hex("#22C55E");

            _adTimerText = CreateText(card.transform, "Unlocking reward... 2.0s", uiFont, 21, FontStyle.Normal, XOThemeDatabase.Hex("#94A3B8"));
            RectTransform timerRT = _adTimerText.GetComponent<RectTransform>();
            timerRT.anchorMin = new Vector2(0.5f, 0.5f);
            timerRT.anchorMax = new Vector2(0.5f, 0.5f);
            timerRT.anchoredPosition = new Vector2(0f, -130f);
            timerRT.sizeDelta = new Vector2(560f, 38f);

            // 3D Claim Reward Button
            GameObject claimBtnObj = new GameObject("ClaimRewardBtn", typeof(RectTransform), typeof(Image), typeof(Button));
            claimBtnObj.transform.SetParent(card.transform, false);
            RectTransform claimRT = claimBtnObj.GetComponent<RectTransform>();
            claimRT.anchorMin = new Vector2(0.5f, 0f);
            claimRT.anchorMax = new Vector2(0.5f, 0f);
            claimRT.anchoredPosition = new Vector2(0f, 62f);
            claimRT.sizeDelta = new Vector2(420f, 76f);

            Image claimImg = claimBtnObj.GetComponent<Image>();
            claimImg.sprite = XOProceduralAssets.Button3DSprite;
            claimImg.type = Image.Type.Sliced;
            claimImg.color = XOThemeDatabase.Hex("#22C55E");

            _adCloseClaimButton = claimBtnObj.GetComponent<Button>();
            _adCloseClaimButton.onClick.AddListener(OnClaimRewardClicked);
            _adCloseClaimButtonText = CreateText(claimBtnObj.transform, "CLAIM REWARD NOW", uiFont, 24, FontStyle.Bold, Color.white);

            // Skip Button (Top Right)
            GameObject skipBtnObj = new GameObject("SkipAdBtn", typeof(RectTransform), typeof(Image), typeof(Button));
            skipBtnObj.transform.SetParent(card.transform, false);
            RectTransform skipRT = skipBtnObj.GetComponent<RectTransform>();
            skipRT.anchorMin = new Vector2(1f, 1f);
            skipRT.anchorMax = new Vector2(1f, 1f);
            skipRT.anchoredPosition = new Vector2(-38f, -38f);
            skipRT.sizeDelta = new Vector2(48f, 48f);
            Image skipImg = skipBtnObj.GetComponent<Image>();
            skipImg.sprite = XOProceduralAssets.CircleSprite;
            skipImg.color = XOThemeDatabase.Hex("#334155");

            Button skipBtn = skipBtnObj.GetComponent<Button>();
            skipBtn.onClick.AddListener(OnSkipAdClicked);
            CreateText(skipBtnObj.transform, "X", uiFont, 22, FontStyle.Bold, XOThemeDatabase.Hex("#CBD5E1"));

            _adModalRoot.SetActive(false);
        }

        public void ShowRewardedAd(string rewardTitle, Action onRewarded, Action onSkipped = null)
        {
            if (_isShowingAd) return;

            if (CustomRewardedAdProvider != null)
            {
                CustomRewardedAdProvider.Invoke(rewardTitle, () =>
                {
                    TotalRewardedAdsWatched++;
                    onRewarded?.Invoke();
                }, onSkipped);
                return;
            }

            if (_adModalRoot == null)
            {
                TotalRewardedAdsWatched++;
                onRewarded?.Invoke();
                return;
            }

            _isShowingAd = true;
            _currentOnRewarded = onRewarded;
            _currentOnSkipped = onSkipped;

            _adRewardDescText.text = rewardTitle;
            _adModalRoot.transform.SetAsLastSibling();
            _adModalRoot.SetActive(true);

            if (_adCoroutine != null) StopCoroutine(_adCoroutine);
            _adCoroutine = StartCoroutine(SimulateRewardedVideoRoutine());
        }

        private IEnumerator SimulateRewardedVideoRoutine()
        {
            _adCloseClaimButton.interactable = false;
            _adCloseClaimButton.GetComponent<Image>().color = XOThemeDatabase.Hex("#475569");
            _adCloseClaimButtonText.text = "PLAYING VIDEO...";

            float elapsed = 0f;
            while (elapsed < simulatedAdDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float progress = Mathf.Clamp01(elapsed / simulatedAdDuration);
                _adProgressBarFill.fillAmount = progress;

                float remain = Mathf.Max(0f, simulatedAdDuration - elapsed);
                _adTimerText.text = $"Reward unlocks in {remain:0.0}s...";
                yield return null;
            }

            _adProgressBarFill.fillAmount = 1f;
            _adTimerText.text = "Reward unlocked!";
            _adCloseClaimButton.interactable = true;
            _adCloseClaimButton.GetComponent<Image>().color = XOThemeDatabase.Hex("#22C55E");
            _adCloseClaimButtonText.text = "CLAIM REWARD NOW";

            yield return new WaitForSecondsRealtime(0.30f);
            if (_isShowingAd)
            {
                OnClaimRewardClicked();
            }
        }

        private void OnClaimRewardClicked()
        {
            if (!_isShowingAd) return;
            _isShowingAd = false;

            if (_adCoroutine != null) StopCoroutine(_adCoroutine);
            _adModalRoot.SetActive(false);

            TotalRewardedAdsWatched++;
            Action cb = _currentOnRewarded;
            _currentOnRewarded = null;
            _currentOnSkipped = null;
            cb?.Invoke();
        }

        private void OnSkipAdClicked()
        {
            if (!_isShowingAd) return;
            _isShowingAd = false;

            if (_adCoroutine != null) StopCoroutine(_adCoroutine);
            _adModalRoot.SetActive(false);

            Action skipCb = _currentOnSkipped;
            _currentOnRewarded = null;
            _currentOnSkipped = null;
            skipCb?.Invoke();
        }

        public void TriggerInstallCTA()
        {
            Debug.Log("[Google Playable CTA] Triggering Store Exit / CTA: " + googlePlayStoreUrl);
#if UNITY_WEBGL && !UNITY_EDITOR
            XOPlayable_TriggerCTA(googlePlayStoreUrl);
#else
            Application.OpenURL(googlePlayStoreUrl);
#endif
        }

        private static Text CreateText(Transform parent, string content, Font font, int size, FontStyle style, Color color)
        {
            GameObject go = new GameObject("Text", typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            Text t = go.GetComponent<Text>();
            t.font = font;
            t.text = content;
            t.fontSize = size;
            t.fontStyle = style;
            t.color = color;
            t.alignment = TextAnchor.MiddleCenter;
            t.raycastTarget = false;
            return t;
        }
    }
}
