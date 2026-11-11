using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace XOPlayable.UI
{
    /// <summary>
    /// Gives any button tactile feedback: a slight grow on hover and a squash while pressed.
    /// Runs on unscaled time and only animates while the scale is moving, so idle buttons cost nothing.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class XOButtonFeedback : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
    {
        private const float HoverScale = 1.03f;
        private const float PressedScale = 0.94f;
        private const float Speed = 18f;

        private Selectable _selectable;
        private Vector3 _baseScale = Vector3.one;
        private float _current = 1f;
        private float _target = 1f;
        private bool _hover;
        private bool _pressed;
        private Coroutine _routine;

        private void Awake()
        {
            _selectable = GetComponent<Selectable>();
            _baseScale = transform.localScale;
        }

        private void OnDisable()
        {
            // Coroutines stop with the object, so put the button back to rest.
            _routine = null;
            _hover = false;
            _pressed = false;
            _current = 1f;
            _target = 1f;
            transform.localScale = _baseScale;
        }

        public void OnPointerEnter(PointerEventData eventData) { _hover = true; Refresh(); }

        public void OnPointerExit(PointerEventData eventData) { _hover = false; _pressed = false; Refresh(); }

        public void OnPointerDown(PointerEventData eventData) { _pressed = true; Refresh(); }

        public void OnPointerUp(PointerEventData eventData) { _pressed = false; Refresh(); }

        private void Refresh()
        {
            bool usable = _selectable == null || _selectable.IsInteractable();
            _target = !usable ? 1f : (_pressed ? PressedScale : (_hover ? HoverScale : 1f));

            if (_routine == null && isActiveAndEnabled)
            {
                // Re-read the resting scale in case layout code changed it while the button was idle.
                if (Mathf.Approximately(_current, 1f)) _baseScale = transform.localScale;
                _routine = StartCoroutine(Animate());
            }
        }

        private IEnumerator Animate()
        {
            while (!Mathf.Approximately(_current, _target))
            {
                _current = Mathf.Lerp(_current, _target, 1f - Mathf.Exp(-Speed * Time.unscaledDeltaTime));
                if (Mathf.Abs(_current - _target) < 0.001f) _current = _target;
                transform.localScale = _baseScale * _current;
                yield return null;
            }

            transform.localScale = _baseScale * _current;
            _routine = null;
        }
    }
}
