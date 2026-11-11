using UnityEngine;

namespace XOPlayable.UI
{
    /// <summary>Small layout helpers that replace the repeated anchorMin / anchorMax / pivot assignment blocks.</summary>
    public static class XORectTransformExtensions
    {
        /// <summary>
        /// Anchors the rect to a single point (min and max the same, so size comes from sizeDelta). The pivot is only changed when one is given.
        /// </summary>
        public static RectTransform SetAnchor(this RectTransform rt, Vector2 anchor, Vector2? pivot = null)
        {
            rt.anchorMin = anchor;
            rt.anchorMax = anchor;
            if (pivot.HasValue) rt.pivot = pivot.Value;
            return rt;
        }
    }
}
