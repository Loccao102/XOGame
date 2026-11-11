using UnityEngine;
using UnityEngine.UI;

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

            if (!force && _lastScreenSize.x == w && _lastScreenSize.y == h && _lastScreenState == currentScreen)
                return;

            _lastScreenSize = new Vector2Int(w, h);
            _lastScreenState = currentScreen;

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

            LayoutHeader(refW, isLandscape);
            _lobby?.ApplyResponsiveLayout(refW, refH, isLandscape);
            _battle?.ApplyResponsiveLayout(refW, refH, isLandscape, boardSize);
            LayoutModals(refW, refH);
        }

        private void LayoutHeader(float refW, bool isLandscape)
        {
            if (_sharedHeaderRT == null) return;
            float maxHeaderW = isLandscape ? Mathf.Min(1660f, refW - 60f) : 1020f;
            _sharedHeaderRT.anchorMin = new Vector2(0.5f, 1f);
            _sharedHeaderRT.anchorMax = new Vector2(0.5f, 1f);
            _sharedHeaderRT.pivot = new Vector2(0.5f, 1f);
            _sharedHeaderRT.anchoredPosition = new Vector2(0f, isLandscape ? -14f : -22f);
            _sharedHeaderRT.sizeDelta = new Vector2(maxHeaderW, isLandscape ? 82f : 96f);
        }

        private void LayoutModals(float refW, float refH)
        {
            float maxW = Mathf.Max(420f, refW - 56f);
            float maxH = Mathf.Max(480f, refH - 72f);

            for (int i = 0; i < _modalCardRTs.Length; i++)
            {
                RectTransform rt = _modalCardRTs[i];
                if (rt == null) continue;
                Vector2 target = _modalBaseSizes[i];
                rt.sizeDelta = new Vector2(Mathf.Min(target.x, maxW), Mathf.Min(target.y, maxH));
            }
        }
    }
}

