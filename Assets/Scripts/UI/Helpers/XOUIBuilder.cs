using System;
using UnityEngine;
using UnityEngine.UI;
using XOPlayable.Visuals;

namespace XOPlayable.UI
{
    /// <summary>
    /// Shared UGUI factory helpers for creating styled Pastel Clay panels, buttons, icons, scroll views, and modal backdrops.
    /// </summary>
    public static class XOUIBuilder
    {
        /// <summary>Loads the default built-in UI font with an OS fallback.</summary>
        public static Font LoadDefaultFont()
        {
            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (font == null) font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            if (font == null) font = Font.CreateDynamicFontFromOSFont("Arial", 24);
            return font;
        }

        /// <summary>Creates a child UI GameObject with a full-stretch <see cref="RectTransform"/>.</summary>
        public static GameObject CreateUIObject(string name, Transform parent)
        {
            GameObject obj = new GameObject(name, typeof(RectTransform));
            obj.transform.SetParent(parent, false);
            RectTransform rt = obj.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            return obj;
        }

        /// <summary>Creates a non-raycasting <see cref="Text"/> element styled with the Pastel Clay ink color.</summary>
        public static Text CreateText(RectTransform parent, Font font, string content, int fontSize, FontStyle style, TextAnchor align)
        {
            GameObject go = CreateUIObject("Text", parent);
            Text txt = go.AddComponent<Text>();
            txt.font = font;
            txt.text = content;
            txt.fontSize = fontSize;
            txt.fontStyle = style;
            txt.alignment = align;
            txt.color = XOProceduralAssets.ColInk;
            txt.supportRichText = true;
            txt.horizontalOverflow = HorizontalWrapMode.Wrap;
            txt.verticalOverflow = VerticalWrapMode.Truncate;
            txt.raycastTarget = false;
            return txt;
        }

        /// <summary>Creates an anchored icon <see cref="Image"/> with preserved aspect ratio.</summary>
        public static Image CreateIconImage(RectTransform parent, Sprite sprite, Vector2 anchor, Vector2 anchoredPos, Vector2 size)
        {
            GameObject go = CreateUIObject("Icon", parent);
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.anchorMin = anchor;
            rt.anchorMax = anchor;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = size;

            Image img = go.AddComponent<Image>();
            img.sprite = sprite;
            img.preserveAspect = true;
            img.raycastTarget = false;
            return img;
        }

        /// <summary>Creates a tactile 3D rounded button with label text and press feedback.</summary>
        public static Button Create3DButton(RectTransform parent, Font font, string label, Vector2 size, Color bgColor, Color textColor, int fontSize)
        {
            GameObject btnObj = CreateUIObject("Btn_" + SanitizeName(label), parent);
            RectTransform rt = btnObj.GetComponent<RectTransform>();
            rt.SetAnchor(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
            rt.sizeDelta = size;

            Image img = btnObj.AddComponent<Image>();
            img.sprite = XOProceduralAssets.Button3DSprite;
            img.type = Image.Type.Sliced;
            img.color = bgColor;

            Button btn = btnObj.AddComponent<Button>();
            btn.targetGraphic = img;
            btnObj.AddComponent<XOButtonFeedback>();

            Text txt = CreateText(rt, font, label, fontSize, FontStyle.Bold, TextAnchor.MiddleCenter);
            txt.color = textColor;
            txt.rectTransform.offsetMin = new Vector2(8f, 4f);
            txt.rectTransform.offsetMax = new Vector2(-8f, -2f);
            return btn;
        }

        /// <summary>Creates a tactile 3D button with a leading icon and aligned label.</summary>
        public static Button CreateIcon3DButton(RectTransform parent, Font font, Sprite icon, string label, Vector2 size, Color bgColor, Color textColor, int fontSize)
        {
            Button btn = Create3DButton(parent, font, label, size, bgColor, textColor, fontSize);
            RectTransform rt = btn.GetComponent<RectTransform>();

            float iconSize = Mathf.Clamp(size.y * 0.56f, 22f, 44f);
            float leftMargin = Mathf.Max(14f, size.y * 0.25f);
            Image ico = CreateIconImage(rt, icon, new Vector2(0f, 0.5f), new Vector2(leftMargin + iconSize * 0.5f, 1f), new Vector2(iconSize, iconSize));
            ico.name = "BtnIcon";

            Text txt = btn.GetComponentInChildren<Text>();
            if (txt != null)
            {
                txt.alignment = TextAnchor.MiddleCenter;
                txt.rectTransform.offsetMin = new Vector2(leftMargin + iconSize + 6f, 4f);
                txt.rectTransform.offsetMax = new Vector2(-10f, -2f);
            }
            return btn;
        }

        /// <summary>Creates a compact status pill badge (e.g., CLEARED / LOCKED) anchored on the right side of a row.</summary>
        public static GameObject CreatePillBadge(RectTransform parent, Font font, string label, Color bgColor, Color textColor, Vector2? anchoredPos = null, Vector2? size = null)
        {
            GameObject badge = CreateUIObject("StatusBadge", parent);
            RectTransform rt = badge.GetComponent<RectTransform>();
            rt.SetAnchor(new Vector2(1f, 0.5f), new Vector2(1f, 0.5f));
            rt.anchoredPosition = anchoredPos ?? new Vector2(-16f, 0f);
            rt.sizeDelta = size ?? new Vector2(120f, 40f);

            Image bg = badge.AddComponent<Image>();
            bg.sprite = XOProceduralAssets.PillSprite;
            bg.type = Image.Type.Sliced;
            bg.color = bgColor;
            bg.raycastTarget = false;

            Text txt = CreateText(rt, font, label, 14, FontStyle.Bold, TextAnchor.MiddleCenter);
            txt.color = textColor;
            return badge;
        }

        /// <summary>Creates a dark modal backdrop with a centered Pastel Clay card container.</summary>
        public static GameObject CreateModalBackdrop(string name, Transform canvasTransform, out RectTransform cardRT)
        {
            GameObject root = CreateUIObject(name, canvasTransform);
            Image backdrop = root.AddComponent<Image>();
            backdrop.color = new Color(0.06f, 0.09f, 0.16f, 0.62f);

            GameObject cardObj = CreateUIObject("Card", root.transform);
            cardRT = cardObj.GetComponent<RectTransform>();
            cardRT.SetAnchor(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
            cardRT.sizeDelta = new Vector2(880f, 820f);

            Image cardBg = cardObj.AddComponent<Image>();
            cardBg.sprite = XOProceduralAssets.CardPanelSprite;
            cardBg.type = Image.Type.Sliced;
            cardBg.color = XOProceduralAssets.ColCardBg;

            XOModalTracker tracker = root.GetComponent<XOModalTracker>();
            if (tracker == null) tracker = root.AddComponent<XOModalTracker>();
            tracker.SetCard(cardRT);

            return root;
        }

        /// <summary>
        /// Builds the standard modal header: an icon and title laid out side by side and centred on the card, plus a close
        /// button in the top-right corner. Centring is done by a layout group, so the icon follows the title whatever its length.
        /// </summary>
        /// <returns>The title text, in case the caller wants to change it later.</returns>
        public static Text CreateModalHeader(RectTransform card, Font font, Sprite icon, string title, int fontSize, Action onClose)
        {
            GameObject header = CreateUIObject("ModalHeader", card);
            RectTransform headerRT = header.GetComponent<RectTransform>();
            headerRT.anchorMin = new Vector2(0f, 1f);
            headerRT.anchorMax = new Vector2(1f, 1f);
            headerRT.pivot = new Vector2(0.5f, 1f);
            headerRT.anchoredPosition = new Vector2(0f, -16f);
            // Equal margins on both sides keep the title centred on the card and clear of the close button.
            headerRT.sizeDelta = new Vector2(-180f, 56f);

            HorizontalLayoutGroup row = header.AddComponent<HorizontalLayoutGroup>();
            row.childAlignment = TextAnchor.MiddleCenter;
            row.spacing = 12f;
            row.childControlWidth = true;
            row.childControlHeight = true;
            row.childForceExpandWidth = false;
            row.childForceExpandHeight = false;

            Image iconImg = CreateIconImage(headerRT, icon, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(46f, 46f));
            LayoutElement iconSlot = iconImg.gameObject.AddComponent<LayoutElement>();
            iconSlot.preferredWidth = 46f;
            iconSlot.preferredHeight = 46f;

            Text titleText = CreateText(headerRT, font, title, fontSize, FontStyle.Bold, TextAnchor.MiddleCenter);

            Button closeBtn = Create3DButton(card, font, "X", new Vector2(48f, 46f), Color.white, XOProceduralAssets.ColInk, 20);
            closeBtn.GetComponent<RectTransform>().SetAnchor(new Vector2(1f, 1f)).anchoredPosition = new Vector2(-34f, -34f);
            closeBtn.onClick.AddListener(() => onClose?.Invoke());

            return titleText;
        }

        /// <summary>Creates a vertical ScrollRect inside a modal card and outputs its LayoutGroup container.</summary>
        public static void CreateModalScrollView(string name, RectTransform cardParent, out RectTransform contentRT, out GameObject scrollViewObj)
        {
            scrollViewObj = CreateUIObject(name, cardParent);
            RectTransform scrollRT = scrollViewObj.GetComponent<RectTransform>();
            scrollRT.anchorMin = Vector2.zero;
            scrollRT.anchorMax = Vector2.one;
            scrollRT.offsetMin = new Vector2(24f, 28f);
            scrollRT.offsetMax = new Vector2(-24f, -162f);

            ScrollRect sr = scrollViewObj.AddComponent<ScrollRect>();
            sr.horizontal = false;
            sr.vertical = true;
            sr.scrollSensitivity = 28f;
            sr.movementType = ScrollRect.MovementType.Elastic;

            GameObject viewport = CreateUIObject("Viewport", scrollRT);
            RectTransform vpRT = viewport.GetComponent<RectTransform>();
            Image vpImg = viewport.AddComponent<Image>();
            vpImg.color = new Color(1f, 1f, 1f, 0.01f);
            viewport.AddComponent<RectMask2D>();
            sr.viewport = vpRT;

            GameObject content = CreateUIObject("Content", vpRT);
            contentRT = content.GetComponent<RectTransform>();
            contentRT.anchorMin = new Vector2(0f, 1f);
            contentRT.anchorMax = new Vector2(1f, 1f);
            contentRT.pivot = new Vector2(0.5f, 1f);
            contentRT.offsetMin = Vector2.zero;
            contentRT.offsetMax = Vector2.zero;

            VerticalLayoutGroup vlg = content.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 12f;
            vlg.padding = new RectOffset(4, 4, 4, 12);
            vlg.childControlWidth = true;
            vlg.childControlHeight = false;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;

            ContentSizeFitter csf = content.AddComponent<ContentSizeFitter>();
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            sr.content = contentRT;
        }

        private static string SanitizeName(string raw)
        {
            if (string.IsNullOrEmpty(raw)) return "Action";
            return raw.Length > 18 ? raw.Substring(0, 18) : raw;
        }
    }
}

