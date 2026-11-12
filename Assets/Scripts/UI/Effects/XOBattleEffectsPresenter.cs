using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using XOPlayable.Core;
using XOPlayable.Visuals;

namespace XOPlayable.UI
{
    /// <summary>
    /// Handles in-battle UI effects: Bot Chat Bubble, Tactical Callout Banners, Cell VFX Bursts,
    /// Victory Confetti, Board Shake, and Win Strike Line overlay.
    /// </summary>
    public sealed class XOBattleEffectsPresenter
    {
        private readonly MonoBehaviour _host;
        private readonly Font _font;
        private readonly RectTransform _fxOverlayRT;
        private readonly Func<int, int, XOCellView> _getCellView;
        private readonly Action<string> _playSfx;

        private RectTransform _boardOuterFrameRT;
        private RectTransform _boardGridRT;
        private Image _winStrikeLineImg;
        private Vector2 _shakeOrigin;
        private bool _isShaking;
        private readonly Vector3[] _corners = new Vector3[4];

        public GameObject BotChatBubbleObj { get; private set; }
        private Text _botChatText;
        private Coroutine _botChatCoroutine;

        private readonly List<GameObject> _activeTransientFx = new List<GameObject>();

        public XOBattleEffectsPresenter(
            MonoBehaviour host,
            Font font,
            RectTransform fxOverlayRT,
            Func<int, int, XOCellView> getCellView,
            Action<string> playSfx)
        {
            _host = host;
            _font = font;
            _fxOverlayRT = fxOverlayRT;
            _getCellView = getCellView;
            _playSfx = playSfx;
        }

        /// <summary>Attaches board transform references and builds overlay visuals (win strike line &amp; bot chat bubble).</summary>
        public void BindBoardVisuals(RectTransform boardOuterFrameRT, RectTransform boardGridRT)
        {
            _boardOuterFrameRT = boardOuterFrameRT;
            _boardGridRT = boardGridRT;

            if (_winStrikeLineImg == null && boardGridRT != null)
            {
                GameObject strikeObj = XOUIBuilder.CreateUIObject("WinStrikeLine", boardGridRT);
                RectTransform sRT = strikeObj.GetComponent<RectTransform>();
                sRT.SetAnchor(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
                _winStrikeLineImg = strikeObj.AddComponent<Image>();
                _winStrikeLineImg.sprite = XOProceduralAssets.PillSprite;
                _winStrikeLineImg.type = Image.Type.Sliced;
                _winStrikeLineImg.color = XOUIColors.Amber400;
                _winStrikeLineImg.raycastTarget = false;
                // The grid layout group would otherwise treat the line as one more cell and reposition it.
                strikeObj.AddComponent<LayoutElement>().ignoreLayout = true;
                strikeObj.SetActive(false);
            }

            if (BotChatBubbleObj == null && _fxOverlayRT != null)
            {
                BotChatBubbleObj = XOUIBuilder.CreateUIObject("BotChatBubble", _fxOverlayRT);
                RectTransform bRT = BotChatBubbleObj.GetComponent<RectTransform>();
                bRT.SetAnchor(new Vector2(0.5f, 1f), new Vector2(0.5f, 1f));
                bRT.anchoredPosition = new Vector2(0f, -158f);
                bRT.sizeDelta = new Vector2(680f, 52f);

                Image bg = BotChatBubbleObj.AddComponent<Image>();
                bg.sprite = XOProceduralAssets.Pill3DSprite;
                bg.type = Image.Type.Sliced;
                bg.color = Color.white;
                bg.raycastTarget = false;

                _botChatText = XOUIBuilder.CreateText(bRT, _font, "", 16, FontStyle.Bold, TextAnchor.MiddleCenter);
                _botChatText.color = XOProceduralAssets.ColInk;
                _botChatText.rectTransform.offsetMin = new Vector2(18f, 4f);
                _botChatText.rectTransform.offsetMax = new Vector2(-18f, -4f);
                BotChatBubbleObj.SetActive(false);
            }
        }

        public void HideWinStrikeLine()
        {
            if (_winStrikeLineImg != null) _winStrikeLineImg.gameObject.SetActive(false);
        }

        public void HideBotChatBubble()
        {
            if (BotChatBubbleObj != null) BotChatBubbleObj.SetActive(false);
        }

        /// <summary>Clears any transient confetti/banners/particles and stops active effect coroutines cleanly.</summary>
        public void ClearTransientEffects()
        {
            HideWinStrikeLine();
            for (int i = _activeTransientFx.Count - 1; i >= 0; i--)
            {
                if (_activeTransientFx[i] != null) UnityEngine.Object.Destroy(_activeTransientFx[i]);
            }
            _activeTransientFx.Clear();
            StopShake();
        }

        private void StopShake()
        {
            if (_isShaking && _boardOuterFrameRT != null) _boardOuterFrameRT.anchoredPosition = _shakeOrigin;
            _isShaking = false;
        }

        /// <summary>World position of the board's top-centre, bottom-centre or centre. Effects anchor to the board, so they follow it in any layout.</summary>
        private Vector3 BoardAnchorWorld(int edge)
        {
            if (_boardOuterFrameRT == null) return _fxOverlayRT != null ? _fxOverlayRT.position : Vector3.zero;
            _boardOuterFrameRT.GetWorldCorners(_corners); // 0 BL, 1 TL, 2 TR, 3 BR
            switch (edge)
            {
                case 1: return (_corners[1] + _corners[2]) * 0.5f;
                case -1: return (_corners[0] + _corners[3]) * 0.5f;
                default: return (_corners[0] + _corners[2]) * 0.5f;
            }
        }

        /// <summary>Displays a timed AI/Boss speech bubble near the top of the battle view.</summary>
        public void ShowBotReactionBubble(string message)
        {
            if (BotChatBubbleObj == null || _botChatText == null || string.IsNullOrEmpty(message)) return;
            _botChatText.text = message;
            BotChatBubbleObj.transform.SetAsLastSibling();
            BotChatBubbleObj.SetActive(true);
            // Sits on the bottom edge of the board so it never covers the player cards or the turn banner.
            BotChatBubbleObj.GetComponent<RectTransform>().position = BoardAnchorWorld(-1);
            _playSfx?.Invoke("bot_chat");

            if (_botChatCoroutine != null) _host.StopCoroutine(_botChatCoroutine);
            _botChatCoroutine = _host.StartCoroutine(HideBubbleAfterDelayRoutine(3.6f));
        }

        /// <summary>Spawns an animated floating callout banner in the center-top of the FX overlay.</summary>
        public void SpawnTacticalCalloutBanner(string title, string subtitle, Color accentColor)
        {
            if (_fxOverlayRT == null) return;

            GameObject bannerObj = XOUIBuilder.CreateUIObject("CalloutBanner", _fxOverlayRT);
            _activeTransientFx.Add(bannerObj);
            RectTransform rt = bannerObj.GetComponent<RectTransform>();
            rt.SetAnchor(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
            rt.sizeDelta = new Vector2(620f, 92f);
            rt.position = BoardAnchorWorld(1);
            rt.anchoredPosition += new Vector2(0f, -70f);

            Image bg = bannerObj.AddComponent<Image>();
            bg.sprite = XOProceduralAssets.Button3DSprite;
            bg.type = Image.Type.Sliced;
            bg.color = accentColor;
            bg.raycastTarget = false;

            Text tTitle = XOUIBuilder.CreateText(rt, _font, title, 23, FontStyle.Bold, TextAnchor.UpperCenter);
            tTitle.rectTransform.offsetMin = new Vector2(16f, 0f);
            tTitle.rectTransform.offsetMax = new Vector2(-16f, -12f);
            tTitle.color = Color.white;

            Text tSub = XOUIBuilder.CreateText(rt, _font, subtitle, 16, FontStyle.Bold, TextAnchor.LowerCenter);
            tSub.rectTransform.offsetMin = new Vector2(16f, 14f);
            tSub.rectTransform.offsetMax = new Vector2(-16f, 0f);
            tSub.color = new Color(1f, 1f, 1f, 0.94f);

            _host.StartCoroutine(AnimateBannerRoutine(bannerObj, rt, bg, 1.85f));
        }

        /// <summary>Spawns radial UGUI particle dots centered on a specific grid cell.</summary>
        public void SpawnCellVFXBurst(int row, int col, Color primary, Color secondary, int particleCount = 14)
        {
            if (_fxOverlayRT == null || _getCellView == null) return;
            XOCellView cellView = _getCellView(row, col);
            if (cellView == null) return;

            Vector3 worldPos = cellView.transform.position;
            for (int i = 0; i < particleCount; i++)
            {
                GameObject dot = XOUIBuilder.CreateUIObject("FxDot", _fxOverlayRT);
                _activeTransientFx.Add(dot);
                RectTransform dRT = dot.GetComponent<RectTransform>();
                dRT.SetAnchor(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
                dRT.position = worldPos;
                float size = UnityEngine.Random.Range(10f, 20f);
                dRT.sizeDelta = new Vector2(size, size);

                Image img = dot.AddComponent<Image>();
                img.sprite = XOProceduralAssets.CircleSprite;
                img.color = (i % 2 == 0) ? primary : secondary;
                img.raycastTarget = false;

                float angle = (i / (float)Mathf.Max(1, particleCount)) * Mathf.PI * 2f + UnityEngine.Random.Range(-0.25f, 0.25f);
                float speed = UnityEngine.Random.Range(110f, 260f);
                Vector2 velocity = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * speed;
                _host.StartCoroutine(AnimateParticleRoutine(dot, dRT, img, velocity, 0.55f, hasGravity: false));
            }
        }

        /// <summary>Launches multi-color victory confetti particles across the play area.</summary>
        public void SpawnVictoryConfettiBurst()
        {
            if (_fxOverlayRT == null) return;

            Color[] palette =
            {
                XOProceduralAssets.ColMint,
                XOProceduralAssets.ColSky,
                XOProceduralAssets.ColSun,
                XOProceduralAssets.ColCoral,
                XOProceduralAssets.ColLilac
            };

            for (int i = 0; i < 40; i++)
            {
                GameObject piece = XOUIBuilder.CreateUIObject("Confetti", _fxOverlayRT);
                _activeTransientFx.Add(piece);
                RectTransform pRT = piece.GetComponent<RectTransform>();
                pRT.SetAnchor(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
                pRT.position = BoardAnchorWorld(0);
                pRT.anchoredPosition += new Vector2(UnityEngine.Random.Range(-240f, 240f), UnityEngine.Random.Range(-60f, 160f));
                pRT.sizeDelta = new Vector2(UnityEngine.Random.Range(12f, 20f), UnityEngine.Random.Range(8f, 14f));
                pRT.localRotation = Quaternion.Euler(0f, 0f, UnityEngine.Random.Range(0f, 360f));

                Image img = piece.AddComponent<Image>();
                img.sprite = XOProceduralAssets.RoundedRectSprite;
                img.color = palette[i % palette.Length];
                img.raycastTarget = false;

                Vector2 velocity = new Vector2(UnityEngine.Random.Range(-320f, 320f), UnityEngine.Random.Range(180f, 460f));
                _host.StartCoroutine(AnimateParticleRoutine(piece, pRT, img, velocity, 1.35f, hasGravity: true));
            }
        }

        /// <summary>Starts a damped board-frame shake coroutine.</summary>
        public void ShakeBoard(float duration = 0.32f, float magnitude = 10f)
        {
            if (_boardOuterFrameRT != null)
            {
                _host.StartCoroutine(ShakeBoardRoutine(duration, magnitude));
            }
        }

        /// <summary>Draws the golden strike-through line across the engine's winning sequence if at least 2 cells exist.</summary>
        public void DrawWinStrikeLineIfApplicable(XOCaroEngine engine)
        {
            if (_winStrikeLineImg == null || engine == null || engine.WinningCells == null || engine.WinningCells.Count < 2)
            {
                HideWinStrikeLine();
                return;
            }

            XOCellPos startPos = engine.WinningCells[0];
            XOCellPos endPos = engine.WinningCells[engine.WinningCells.Count - 1];
            XOCellView startView = _getCellView?.Invoke(startPos.x, startPos.y);
            XOCellView endView = _getCellView?.Invoke(endPos.x, endPos.y);
            if (startView == null || endView == null) return;

            Canvas.ForceUpdateCanvases();
            RectTransform startRT = startView.GetComponent<RectTransform>();
            RectTransform endRT = endView.GetComponent<RectTransform>();

            // Cells are placed by a layout group (top-left anchored), so measure in the grid's own local space via world positions.
            Vector2 a = _boardGridRT.InverseTransformPoint(startRT.position);
            Vector2 b = _boardGridRT.InverseTransformPoint(endRT.position);
            Vector2 delta = b - a;
            float thickness = Mathf.Clamp(startRT.rect.width * 0.2f, 8f, 22f);
            float dist = delta.magnitude + startRT.rect.width * 0.6f;
            float angle = Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg;

            RectTransform sRT = _winStrikeLineImg.rectTransform;
            sRT.anchoredPosition = (a + b) * 0.5f;
            sRT.sizeDelta = new Vector2(dist, thickness);
            sRT.localRotation = Quaternion.Euler(0f, 0f, angle);
            _winStrikeLineImg.color = (engine.State == MatchState.XWon)
                ? XOUIColors.Sky400
                : XOUIColors.Rose400;
            _winStrikeLineImg.transform.SetAsLastSibling();
            _winStrikeLineImg.gameObject.SetActive(true);
        }

        private IEnumerator HideBubbleAfterDelayRoutine(float delay)
        {
            yield return new WaitForSeconds(delay);
            HideBotChatBubble();
        }

        private IEnumerator ShakeBoardRoutine(float duration, float magnitude)
        {
            if (!_isShaking) _shakeOrigin = _boardOuterFrameRT.anchoredPosition;
            _isShaking = true;
            float elapsed = 0f;
            while (elapsed < duration && _boardOuterFrameRT != null && _isShaking)
            {
                elapsed += Time.deltaTime;
                float damper = 1f - Mathf.Clamp01(elapsed / duration);
                float ox = UnityEngine.Random.Range(-1f, 1f) * magnitude * damper;
                float oy = UnityEngine.Random.Range(-1f, 1f) * magnitude * damper;
                _boardOuterFrameRT.anchoredPosition = _shakeOrigin + new Vector2(ox, oy);
                yield return null;
            }
            StopShake();
        }

        private IEnumerator AnimateBannerRoutine(GameObject obj, RectTransform rt, Image bg, float duration)
        {
            float elapsed = 0f;
            Vector2 startPos = rt.anchoredPosition;
            while (elapsed < duration && obj != null)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                rt.anchoredPosition = startPos + new Vector2(0f, t * 44f);
                rt.localScale = Vector3.one * Mathf.LerpUnclamped(0.8f, 1f, EaseOutBack(Mathf.Clamp01(t / 0.16f)));
                float alpha = (t > 0.72f) ? Mathf.Lerp(1f, 0f, (t - 0.72f) / 0.28f) : 1f;
                Color c = bg.color;
                c.a = alpha;
                bg.color = c;
                yield return null;
            }
            _activeTransientFx.Remove(obj);
            if (obj != null) UnityEngine.Object.Destroy(obj);
        }

        private static float EaseOutBack(float k)
        {
            const float c1 = 1.70158f;
            const float c3 = c1 + 1f;
            float x = k - 1f;
            return 1f + c3 * x * x * x + c1 * x * x;
        }

        private IEnumerator AnimateParticleRoutine(GameObject obj, RectTransform rt, Image img, Vector2 velocity, float lifetime, bool hasGravity)
        {
            float elapsed = 0f;
            Color baseCol = img.color;
            while (elapsed < lifetime && obj != null)
            {
                float dt = Time.deltaTime;
                elapsed += dt;
                if (hasGravity) velocity.y -= 580f * dt;
                rt.anchoredPosition += velocity * dt;
                float t = Mathf.Clamp01(elapsed / lifetime);
                img.color = XOProceduralAssets.WithAlpha(baseCol, 1f - t);
                yield return null;
            }
            _activeTransientFx.Remove(obj);
            if (obj != null) UnityEngine.Object.Destroy(obj);
        }
    }
}

