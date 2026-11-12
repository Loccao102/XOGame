using System.Collections;
using UnityEngine;

namespace XOPlayable.UI
{
    /// <summary>Plays a short springy scale-in every time the object becomes active (e.g. a result-screen hero icon).</summary>
    public sealed class XOPopOnEnable : MonoBehaviour
    {
        private const float Duration = 0.45f;
        private const float Delay = 0.12f;

        private void OnEnable()
        {
            transform.localScale = Vector3.zero;
            StartCoroutine(Pop());
        }

        private void OnDisable()
        {
            transform.localScale = Vector3.one;
        }

        private IEnumerator Pop()
        {
            float wait = 0f;
            while (wait < Delay) { wait += Time.unscaledDeltaTime; yield return null; }

            float elapsed = 0f;
            while (elapsed < Duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float x = Mathf.Clamp01(elapsed / Duration) - 1f;
                const float c1 = 2.2f;
                const float c3 = c1 + 1f;
                transform.localScale = Vector3.one * (1f + c3 * x * x * x + c1 * x * x);
                yield return null;
            }
            transform.localScale = Vector3.one;
        }
    }
}
