using System.Collections;
using UnityEngine;

namespace XOPlayable.UI
{
    /// <summary>Counts how many modals are currently visible, so gameplay code can ask once instead of polling every modal root each frame.</summary>
    public sealed class XOModalState
    {
        public int OpenCount { get; internal set; }
        public bool IsAnyOpen => OpenCount > 0;
    }

    /// <summary>
    /// Added to every modal root by the controller. Counts itself into <see cref="XOModalState"/> whenever the modal
    /// becomes active and out again when it is hidden or destroyed, so call sites keep using plain SetActive.
    /// It also plays the shared open animation (scrim fade plus a springy card pop).
    /// </summary>
    public sealed class XOModalTracker : MonoBehaviour
    {
        private const string CardName = "ModalCard";
        private const float OpenDuration = 0.22f;
        private const float StartScale = 0.88f;

        private XOModalState _state;
        private bool _counted;
        private RectTransform _card;
        private CanvasGroup _group;

        public void Bind(XOModalState state)
        {
            _state = state;
            // AddComponent on an already-active object runs OnEnable before Bind, so catch up here.
            if (isActiveAndEnabled) Count();
        }

        private void OnEnable()
        {
            Count();
            PlayOpenAnimation();
        }

        private void OnDisable()
        {
            Uncount();
            // The animation coroutine dies with the object, so never leave the modal half-scaled or half-faded.
            if (_card != null) _card.localScale = Vector3.one;
            if (_group != null) _group.alpha = 1f;
        }

        private void Count()
        {
            if (_state == null || _counted) return;
            _counted = true;
            _state.OpenCount++;
        }

        private void Uncount()
        {
            if (_state == null || !_counted) return;
            _counted = false;
            _state.OpenCount--;
        }

        private void PlayOpenAnimation()
        {
            if (_card == null)
            {
                // The card is created after this component, so the first activation may not find it yet.
                Transform t = transform.Find(CardName);
                if (t != null) _card = t as RectTransform;
            }
            if (_card == null) return;

            if (_group == null)
            {
                _group = GetComponent<CanvasGroup>();
                if (_group == null) _group = gameObject.AddComponent<CanvasGroup>();
            }

            StartCoroutine(OpenRoutine());
        }

        private IEnumerator OpenRoutine()
        {
            _card.localScale = Vector3.one * StartScale;
            _group.alpha = 0f;

            float elapsed = 0f;
            while (elapsed < OpenDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float k = Mathf.Clamp01(elapsed / OpenDuration);
                _card.localScale = Vector3.one * Mathf.LerpUnclamped(StartScale, 1f, EaseOutBack(k));
                _group.alpha = Mathf.Clamp01(k * 2.2f);
                yield return null;
            }

            _card.localScale = Vector3.one;
            _group.alpha = 1f;
        }

        private static float EaseOutBack(float k)
        {
            const float c1 = 1.70158f;
            const float c3 = c1 + 1f;
            float x = k - 1f;
            return 1f + c3 * x * x * x + c1 * x * x;
        }
    }
}
