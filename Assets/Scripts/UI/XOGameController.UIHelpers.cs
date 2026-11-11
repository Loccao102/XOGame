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
        private GameObject CreateModalBackdrop(string name, Transform parent, out RectTransform cardRT)
        {
            GameObject root = CreateUIObject(name, parent);
            RectTransform rt = root.GetComponent<RectTransform>();
            StretchFull(rt);
            root.AddComponent<XOModalTracker>().Bind(_modalState);

            Image backdrop = root.AddComponent<Image>();
            backdrop.color = new Color(0.08f, 0.12f, 0.22f, 0.72f); // Deep pastel clay modal scrim
            backdrop.raycastTarget = true;

            GameObject card = CreateUIObject("ModalCard", rt);
            cardRT = card.GetComponent<RectTransform>();
            cardRT.anchorMin = new Vector2(0.5f, 0.5f);
            cardRT.anchorMax = new Vector2(0.5f, 0.5f);
            cardRT.pivot = new Vector2(0.5f, 0.5f);

            Image cardImg = card.AddComponent<Image>();
            cardImg.sprite = XOProceduralAssets.RoundedRectSprite;
            cardImg.type = Image.Type.Sliced;
            cardImg.color = Color.white;
            cardImg.raycastTarget = true;

            return root;
        }

        // =====================================================================
        // UI HELPER BUILDERS (3D BUTTONS & ILLUSTRATED ICON IMAGES)
        // =====================================================================

        private GameObject CreateUIObject(string name, Transform parent)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go;
        }

        private void StretchFull(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        private Image CreateIconImage(Transform parent, Sprite sprite, Vector2 anchor, Vector2 anchoredPos, Vector2 size)
        {
            GameObject go = new GameObject("Icon_" + (sprite != null ? sprite.name : "Img"), typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.anchorMin = anchor;
            rt.anchorMax = anchor;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = size;

            Image img = go.GetComponent<Image>();
            img.sprite = sprite;
            img.preserveAspect = true;
            img.raycastTarget = false;
            return img;
        }

        private Text CreateText(Transform parent, string content, int fontSize, FontStyle style, TextAnchor anchor)
        {
            GameObject go = new GameObject("Text", typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            RectTransform rt = go.GetComponent<RectTransform>();
            StretchFull(rt);

            Text txt = go.GetComponent<Text>();
            txt.font = _uiFont;
            txt.text = content;
            txt.fontSize = fontSize;
            txt.fontStyle = style;
            txt.alignment = anchor;
            txt.color = Color.white;
            txt.raycastTarget = false;
            return txt;
        }

        private Button Create3DButton(Transform parent, string label, Vector2 size, Color bgColor, Color textColor, int fontSize)
        {
            GameObject go = new GameObject("Btn3D_" + label, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);

            RectTransform rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = size;

            Image img = go.GetComponent<Image>();
            img.sprite = XOProceduralAssets.Button3DSprite;
            img.type = Image.Type.Sliced;
            img.color = bgColor;

            Button btn = go.GetComponent<Button>();
            go.AddComponent<XOButtonFeedback>();
            Text txt = CreateText(rt, label, fontSize, FontStyle.Bold, TextAnchor.MiddleCenter);
            txt.rectTransform.offsetMin = new Vector2(8f, 6f);
            txt.rectTransform.offsetMax = new Vector2(-8f, -2f);
            txt.color = textColor;

            return btn;
        }

        private Button CreateIcon3DButton(Transform parent, Sprite iconSprite, string label, Vector2 size, Color bgColor, Color textColor, int fontSize)
        {
            GameObject go = new GameObject("BtnIcon3D_" + label, typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
            go.transform.SetParent(parent, false);

            LayoutElement le = go.GetComponent<LayoutElement>();
            le.preferredWidth = size.x;
            le.preferredHeight = size.y;
            le.minWidth = size.x * 0.75f;
            le.minHeight = size.y * 0.75f;

            RectTransform rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = size;

            Image img = go.GetComponent<Image>();
            img.sprite = XOProceduralAssets.Button3DSprite;
            img.type = Image.Type.Sliced;
            img.color = bgColor;

            bool hasLabel = !string.IsNullOrEmpty(label);
            float iconSize = Mathf.Clamp(size.y * 0.58f, 28f, 56f);

            if (hasLabel)
            {
                CreateIconImage(rt, iconSprite, new Vector2(0f, 0.5f), new Vector2(iconSize * 0.68f + 8f, 2f), new Vector2(iconSize, iconSize));
                Text txt = CreateText(rt, label, fontSize, FontStyle.Bold, TextAnchor.MiddleCenter);
                txt.rectTransform.offsetMin = new Vector2(iconSize + 14f, 6f);
                txt.rectTransform.offsetMax = new Vector2(-12f, -2f);
                txt.color = textColor;
            }
            else
            {
                // Centered icon when button has no text label (e.g. Chest button)
                CreateIconImage(rt, iconSprite, new Vector2(0.5f, 0.5f), new Vector2(0f, 2f), new Vector2(iconSize, iconSize));
            }

            Button btn = go.GetComponent<Button>();
            go.AddComponent<XOButtonFeedback>();
            return btn;
        }

        /// <summary>
        /// Adds hover/press feedback to every button under <paramref name="root"/> that does not have it yet.
        /// Board cells are skipped because they run their own pop animation on the same transform.
        /// </summary>
        private void AttachButtonFeedback(Transform root)
        {
            foreach (Button button in root.GetComponentsInChildren<Button>(true))
            {
                if (button.GetComponent<XOCellView>() != null) continue;
                if (button.GetComponent<XOButtonFeedback>() != null) continue;
                button.gameObject.AddComponent<XOButtonFeedback>();
            }
        }
    }
}
