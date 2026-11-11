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
        private void ShowBotReactionBubble(string message)
        {
            if (_botChatBubbleObj == null || _botChatBubbleText == null) return;
            if (_botChatCoroutine != null) StopCoroutine(_botChatCoroutine);
            _botChatCoroutine = StartCoroutine(BotChatBubbleRoutine(message));
        }

        private IEnumerator BotChatBubbleRoutine(string message)
        {
            _botChatBubbleText.text = message;
            _botChatBubbleObj.transform.SetAsLastSibling();
            _botChatBubbleObj.SetActive(true);
            PlaySfx("bot_chat");

            CanvasGroup cg = _botChatBubbleObj.GetComponent<CanvasGroup>();
            RectTransform rt = _botChatBubbleObj.GetComponent<RectTransform>();

            float elapsed = 0f;
            float duration = 3.2f;
            while (elapsed < duration)
            {
                if (rt == null) yield break;
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);

                float scale = (t < 0.12f)
                    ? Mathf.Lerp(0.5f, 1.08f, t / 0.12f)
                    : ((t < 0.22f) ? Mathf.Lerp(1.08f, 1.0f, (t - 0.12f) / 0.10f) : 1.0f);
                rt.localScale = new Vector3(scale, scale, 1f);
                cg.alpha = (t < 0.8f) ? 1f : (1f - (t - 0.8f) / 0.2f);
                yield return null;
            }

            if (_botChatBubbleObj != null)
            {
                _botChatBubbleObj.SetActive(false);
            }
        }

        private GameObject _activeTacticalCalloutObj;

        private void SpawnTacticalCalloutBanner(string title, string subtitle, Color bannerColor)
        {
            if (_boardOuterFrameRT == null) return;
            if (_activeTacticalCalloutObj != null)
            {
                Destroy(_activeTacticalCalloutObj);
                _activeTacticalCalloutObj = null;
            }
            StartCoroutine(TacticalCalloutRoutine(title, subtitle, bannerColor));
        }

        private IEnumerator TacticalCalloutRoutine(string title, string subtitle, Color bannerColor)
        {
            GameObject bannerObj = new GameObject("TacticalCallout", typeof(RectTransform), typeof(CanvasGroup), typeof(Image));
            _activeTacticalCalloutObj = bannerObj;
            bannerObj.transform.SetParent(_boardOuterFrameRT, false);
            bannerObj.transform.SetAsLastSibling();

            RectTransform rt = bannerObj.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            Vector2 startPos = new Vector2(0f, 160f);
            rt.anchoredPosition = startPos;
            rt.sizeDelta = new Vector2(540f, 84f);

            Image bg = bannerObj.GetComponent<Image>();
            bg.sprite = XOProceduralAssets.Button3DSprite;
            bg.type = Image.Type.Sliced;
            bg.color = bannerColor;
            bg.raycastTarget = false;

            CreateIconImage(rt, XOProceduralAssets.IconLightning, new Vector2(0f, 0.5f), new Vector2(44f, 4f), new Vector2(48f, 48f));

            Text titleTxt = CreateText(rt, title, 25, FontStyle.Bold, TextAnchor.LowerLeft);
            titleTxt.rectTransform.offsetMin = new Vector2(82f, 40f);
            titleTxt.rectTransform.offsetMax = new Vector2(-18f, -6f);
            titleTxt.color = Color.white;

            Text subTxt = CreateText(rt, subtitle, 18, FontStyle.Bold, TextAnchor.UpperLeft);
            subTxt.rectTransform.offsetMin = new Vector2(82f, 10f);
            subTxt.rectTransform.offsetMax = new Vector2(-18f, -42f);
            subTxt.color = XOThemeDatabase.Hex("#FEF08A");

            CanvasGroup cg = bannerObj.GetComponent<CanvasGroup>();
            float duration = 1.45f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                if (rt == null) yield break;
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);

                rt.anchoredPosition = startPos + new Vector2(0f, t * 55f);
                float scale = (t < 0.14f) ? Mathf.Lerp(0.4f, 1.12f, t / 0.14f) : Mathf.Lerp(1.12f, 1.0f, (t - 0.14f) / 0.86f);
                rt.localScale = new Vector3(scale, scale, 1f);
                cg.alpha = (t < 0.72f) ? 1f : (1f - (t - 0.72f) / 0.28f);
                yield return null;
            }

            if (bannerObj != null)
            {
                if (_activeTacticalCalloutObj == bannerObj) _activeTacticalCalloutObj = null;
                Destroy(bannerObj);
            }
        }

        private void DrawWinStrikeLineIfApplicable()
        {
            if (_engine.WinningCells == null || _engine.WinningCells.Count < 2 || _winStrikeLineImg == null)
            {
                if (_winStrikeLineImg != null) _winStrikeLineImg.gameObject.SetActive(false);
                return;
            }

            Canvas.ForceUpdateCanvases();

            Vector2Int first = _engine.WinningCells[0];
            Vector2Int last = _engine.WinningCells[_engine.WinningCells.Count - 1];

            int idxA = first.x * _engine.BoardSize + first.y;
            int idxB = last.x * _engine.BoardSize + last.y;
            if (idxA >= _cellViews.Count || idxB >= _cellViews.Count) return;

            RectTransform rtA = _cellViews[idxA].GetComponent<RectTransform>();
            RectTransform rtB = _cellViews[idxB].GetComponent<RectTransform>();

            Vector2 posA = _boardOuterFrameRT.InverseTransformPoint(rtA.position);
            Vector2 posB = _boardOuterFrameRT.InverseTransformPoint(rtB.position);

            Vector2 mid = (posA + posB) * 0.5f;
            Vector2 dir = posB - posA;
            float distance = dir.magnitude + _boardGridLayout.cellSize.x * 0.55f;
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

            RectTransform lineRT = _winStrikeLineImg.rectTransform;
            lineRT.anchorMin = new Vector2(0.5f, 0.5f);
            lineRT.anchorMax = new Vector2(0.5f, 0.5f);
            lineRT.pivot = new Vector2(0.5f, 0.5f);
            lineRT.anchoredPosition = mid;
            lineRT.sizeDelta = new Vector2(distance, 14f);
            lineRT.localRotation = Quaternion.Euler(0f, 0f, angle);

            _winStrikeLineImg.color = _activeTheme.goldAdColor;
            _winStrikeLineImg.transform.SetAsLastSibling();
            _winStrikeLineImg.gameObject.SetActive(true);
        }

        private void PlaySfx(string key)
        {
            if (IsSoundMuted || _audioSource == null) return;
            AudioClip clip = XOProceduralAssets.GetSound(key);
            if (clip != null)
            {
                _audioSource.PlayOneShot(clip);
            }
        }

        // =====================================================================
        // LIGHTWEIGHT UGUI PARTICLE BURSTS (BOMB, FREEZE, VICTORY CONFETTI)
        // =====================================================================

        private void SpawnCellVFXBurst(int row, int col, Color c1, Color c2, int count)
        {
            int idx = row * _engine.BoardSize + col;
            if (idx < 0 || idx >= _cellViews.Count || _boardOuterFrameRT == null) return;

            StartCoroutine(ShakeBoardRoutine(0.18f, 11f));

            RectTransform cellRT = _cellViews[idx].GetComponent<RectTransform>();
            Vector2 localCenter = _boardOuterFrameRT.InverseTransformPoint(cellRT.position);

            for (int i = 0; i < count; i++)
            {
                float angle = (i / (float)count) * Mathf.PI * 2f + UnityEngine.Random.Range(-0.25f, 0.25f);
                float speed = UnityEngine.Random.Range(110f, 260f);
                Vector2 vel = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * speed;
                Color colChoice = (i % 2 == 0) ? c1 : c2;
                float size = UnityEngine.Random.Range(10f, 20f);
                StartCoroutine(AnimateUIParticleRoutine(_boardOuterFrameRT, localCenter, vel, colChoice, size, 0.48f));
            }
        }

        private void SpawnVictoryConfettiBurst()
        {
            if (_boardOuterFrameRT == null) return;
            StartCoroutine(ShakeBoardRoutine(0.22f, 9f));

            Color[] palette =
            {
                XOThemeDatabase.Hex("#FBBF24"),
                XOThemeDatabase.Hex("#22D3EE"),
                XOThemeDatabase.Hex("#4ADE80"),
                XOThemeDatabase.Hex("#FB7185"),
                XOThemeDatabase.Hex("#A855F7")
            };

            for (int i = 0; i < 28; i++)
            {
                Vector2 startPos = new Vector2(UnityEngine.Random.Range(-220f, 220f), UnityEngine.Random.Range(-120f, 160f));
                float angle = UnityEngine.Random.Range(0f, Mathf.PI * 2f);
                float speed = UnityEngine.Random.Range(140f, 340f);
                Vector2 vel = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * speed;
                Color colChoice = palette[i % palette.Length];
                float size = UnityEngine.Random.Range(12f, 24f);
                StartCoroutine(AnimateUIParticleRoutine(_boardOuterFrameRT, startPos, vel, colChoice, size, 0.75f));
            }
        }

        private GameObject _activeCoinToastObj;
        private Coroutine _activeCoinToastCoroutine;
        private int _accumulatedCoinToast = 0;

        private IEnumerator ShakeBoardRoutine(float duration, float magnitude)
        {
            if (_boardOuterFrameRT == null) yield break;
            Vector2 basePos = _boardOuterFrameRT.anchoredPosition;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float damper = 1f - Mathf.Clamp01(elapsed / duration);
                float ox = UnityEngine.Random.Range(-1f, 1f) * magnitude * damper;
                float oy = UnityEngine.Random.Range(-1f, 1f) * magnitude * damper;
                if (_boardOuterFrameRT != null)
                {
                    _boardOuterFrameRT.anchoredPosition = basePos + new Vector2(ox, oy);
                }
                yield return null;
            }
            if (_boardOuterFrameRT != null)
            {
                _boardOuterFrameRT.anchoredPosition = basePos;
            }
        }

        private void SpawnFloatingCoinToast(int amount)
        {
            if (_mainCanvas == null || amount <= 0) return;
            _accumulatedCoinToast += amount;
            if (_activeCoinToastCoroutine != null)
            {
                StopCoroutine(_activeCoinToastCoroutine);
            }
            _activeCoinToastCoroutine = StartCoroutine(FloatingCoinToastRoutine());
        }

        private IEnumerator FloatingCoinToastRoutine()
        {
            int displayAmount = _accumulatedCoinToast;
            if (_activeCoinToastObj != null)
            {
                Destroy(_activeCoinToastObj);
            }

            GameObject toastObj = new GameObject("FloatingCoinToast", typeof(RectTransform), typeof(CanvasGroup), typeof(Image));
            _activeCoinToastObj = toastObj;
            toastObj.transform.SetParent(_mainCanvas.transform, false);
            toastObj.transform.SetAsLastSibling();

            RectTransform rt = toastObj.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);

            // In Landscape: spawn above the right sidebar HUD! In Portrait: top center
            Vector2 startPos = _isLandscapeLayout ? new Vector2(510f, 440f) : new Vector2(0f, 440f);
            rt.anchoredPosition = startPos;
            rt.sizeDelta = new Vector2(280f, 56f);

            Image bg = toastObj.GetComponent<Image>();
            bg.sprite = XOProceduralAssets.Pill3DSprite;
            bg.type = Image.Type.Sliced;
            bg.color = XOThemeDatabase.Hex("#F59E0B");
            bg.raycastTarget = false;

            CreateIconImage(rt, XOProceduralAssets.IconCoin, new Vector2(0f, 0.5f), new Vector2(36f, 2f), new Vector2(38f, 38f));
            Text txt = CreateText(rt, $"+{displayAmount} COINS!", 24, FontStyle.Bold, TextAnchor.MiddleCenter);
            txt.rectTransform.offsetMin = new Vector2(46f, 4f);
            txt.color = XOProceduralAssets.ColInk;

            CanvasGroup cg = toastObj.GetComponent<CanvasGroup>();
            float duration = 1.25f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                if (rt == null) yield break;
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);

                rt.anchoredPosition = startPos + new Vector2(0f, t * 75f);
                float popScale = (t < 0.18f) ? Mathf.Lerp(0.6f, 1.12f, t / 0.18f) : Mathf.Lerp(1.12f, 1.0f, (t - 0.18f) / 0.82f);
                rt.localScale = new Vector3(popScale, popScale, 1f);
                cg.alpha = (t < 0.7f) ? 1f : (1f - (t - 0.7f) / 0.3f);
                yield return null;
            }

            if (toastObj != null)
            {
                Destroy(toastObj);
            }
            _accumulatedCoinToast = 0;
            _activeCoinToastObj = null;
            _activeCoinToastCoroutine = null;
        }

        private IEnumerator AnimateUIParticleRoutine(RectTransform parent, Vector2 startPos, Vector2 velocity, Color color, float size, float duration)
        {
            GameObject pObj = new GameObject("VFXParticle", typeof(RectTransform), typeof(Image));
            pObj.transform.SetParent(parent, false);
            RectTransform rt = pObj.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = startPos;
            rt.sizeDelta = new Vector2(size, size);

            Image img = pObj.GetComponent<Image>();
            img.sprite = XOProceduralAssets.CircleSprite;
            img.color = color;
            img.raycastTarget = false;

            float elapsed = 0f;
            Vector2 pos = startPos;
            while (elapsed < duration)
            {
                if (rt == null) yield break;
                float dt = Time.deltaTime;
                elapsed += dt;
                float t = Mathf.Clamp01(elapsed / duration);

                pos += velocity * dt;
                velocity *= (1f - 3.2f * dt);
                velocity.y -= 180f * dt; // subtle gravity

                rt.anchoredPosition = pos;
                float scale = 1f - t * 0.85f;
                rt.localScale = new Vector3(scale, scale, 1f);
                img.color = new Color(color.r, color.g, color.b, 1f - t);
                yield return null;
            }

            if (pObj != null)
            {
                Destroy(pObj);
            }
        }
    }
}
