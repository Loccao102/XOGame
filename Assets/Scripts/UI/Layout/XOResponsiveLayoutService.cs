using UnityEngine;
using UnityEngine.UI;
using XOPlayable.Ads;

namespace XOPlayable.UI
{
    /// <summary>
    /// Computes portrait vs landscape canvas reference resolution and responsively positions
    /// header, lobby, battle board, action deck, and active modal cards across device aspect ratios.
    /// </summary>
    public sealed class XOResponsiveLayoutService
    {
        private readonly CanvasScaler _canvasScaler;
        private readonly XOLobbyPresenter _lobby;
        private readonly XOBattlePresenter _battle;
        private readonly RectTransform _sharedHeaderRT;

        private RectTransform[] _modalCardRTs = new RectTransform[0];
        private Vector2[] _modalBaseSizes = new Vector2[0];

        private Vector2Int _lastScreenSize = new Vector2Int(-1, -1);
        private ScreenState _lastScreenState = (ScreenState)(-1);
        private Vector4 _lastSafeInsets = new Vector4(-1f, -1f, -1f, -1f);

        public XOResponsiveLayoutService(
            CanvasScaler canvasScaler,
            RectTransform sharedHeaderRT,
            XOLobbyPresenter lobby,
            XOBattlePresenter battle)
        {
            _canvasScaler = canvasScaler;
            _sharedHeaderRT = sharedHeaderRT;
            _lobby = lobby;
            _battle = battle;
        }

        /// <summary>Registers modal card RectTransforms so they automatically scale inside small screens.</summary>
        public void RegisterModalCards(params (RectTransform rt, Vector2 baseSize)[] modals)
        {
            _modalCardRTs = new RectTransform[modals.Length];
            _modalBaseSizes = new Vector2[modals.Length];
            for (int i = 0; i < modals.Length; i++)
            {
                _modalCardRTs[i] = modals[i].rt;
                _modalBaseSizes[i] = modals[i].baseSize;
            }
        }

        /// <summary>Applies responsive layout adjustments whenever viewport size or active screen changes.</summary>
        public void UpdateLayout(ScreenState currentScreen, Vector2Int screenOverride, int boardSize, bool force = false)
        {
            int w = (screenOverride.x > 0) ? screenOverride.x : Mathf.Max(320, Screen.width);
            int h = (screenOverride.y > 0) ? screenOverride.y : Mathf.Max(320, Screen.height);
            Vector4 safeNorm = (XOPlayableAdsBridge.Instance != null)
                ? XOPlayableAdsBridge.Instance.GetSafeAreaInsetsNormalized()
                : Vector4.zero;

            bool sameSafe = (safeNorm - _lastSafeInsets).sqrMagnitude < 0.000001f;
            if (!force && _lastScreenSize.x == w && _lastScreenSize.y == h && _lastScreenState == currentScreen && sameSafe)
                return;

            _lastScreenSize = new Vector2Int(w, h);
            _lastScreenState = currentScreen;
            _lastSafeInsets = safeNorm;

            bool isLandscape = w >= h * 1.05f;
            float aspect = w / (float)h;

            if (_canvasScaler != null)
            {
                if (isLandscape)
                {
                    _canvasScaler.referenceResolution = new Vector2(1920f, 1080f);
                    _canvasScaler.matchWidthOrHeight = 1f;
                }
                else
                {
                    _canvasScaler.referenceResolution = new Vector2(1080f, 1920f);
                    _canvasScaler.matchWidthOrHeight = (aspect < 0.55f) ? 0f : 0.45f;
                }
            }

            float refW = isLandscape ? 1080f * Mathf.Clamp(aspect, 1.15f, 1.9f) : 1080f;
            float refH = isLandscape ? 1080f : 1080f / Mathf.Clamp(aspect, 0.46f, 0.95f);

            float safeL = safeNorm.x * refW;
            float safeR = safeNorm.y * refW;
            float safeT = safeNorm.z * refH;
            float safeB = safeNorm.w * refH;

            LayoutHeader(refW, isLandscape, safeL, safeR, safeT);
            _lobby?.ApplyResponsiveLayout(refW, refH, isLandscape, safeL, safeR, safeT, safeB);
            _battle?.ApplyResponsiveLayout(refW, refH, isLandscape, boardSize, safeL, safeR, safeT, safeB);
            LayoutModals(refW, refH, safeL, safeR, safeT, safeB);
        }

        private void LayoutHeader(float refW, bool isLandscape, float safeL, float safeR, float safeT)
        {
            if (_sharedHeaderRT == null) return;
            // Loose chips with no card behind them: full safe-area width, the chips carry their own 30px inset.
            float safeAreaW = Mathf.Max(320f, refW - safeL - safeR);
            float headerW = isLandscape ? Mathf.Min(1920f, safeAreaW) : Mathf.Min(1080f, safeAreaW);
            _sharedHeaderRT.SetAnchor(new Vector2(0.5f, 1f), new Vector2(0.5f, 1f));
            _sharedHeaderRT.anchoredPosition = new Vector2((safeL - safeR) * 0.5f, -safeT - (isLandscape ? 18f : 24f));
            _sharedHeaderRT.sizeDelta = new Vector2(headerW, isLandscape ? 76f : 80f);
        }

        private void LayoutModals(float refW, float refH, float safeL, float safeR, float safeT, float safeB)
        {
            float maxW = Mathf.Max(360f, refW - 56f - safeL - safeR);
            float maxH = Mathf.Max(420f, refH - 72f - safeT - safeB);
            Vector2 centerOffset = new Vector2((safeL - safeR) * 0.5f, (safeB - safeT) * 0.5f);

            for (int i = 0; i < _modalCardRTs.Length; i++)
            {
                RectTransform rt = _modalCardRTs[i];
                if (rt == null) continue;
                Vector2 target = _modalBaseSizes[i];
                rt.anchoredPosition = centerOffset;
                rt.sizeDelta = new Vector2(Mathf.Min(target.x, maxW), Mathf.Min(target.y, maxH));
            }
        }
    }
}

