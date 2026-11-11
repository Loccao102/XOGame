using UnityEngine;

namespace XOPlayable.Visuals
{
    /// <summary>
    /// Pastel Clay palette constants, SDF/texture math helpers, and procedural 9-sliced panel/button/cell sprites.
    /// </summary>
    internal static class XOTexturePrimitives
    {
        public static readonly Color ColInk       = Hex("#0F172A"); // High contrast dark slate (9.5+:1 WCAG AAA)
        public static readonly Color ColMuted     = Hex("#334155"); // Sharp, clear Slate-700 secondary text (7+:1 WCAG AAA)
        public static readonly Color ColMint      = Hex("#4ED6A4");
        public static readonly Color ColMintEdge  = Hex("#2EA375");
        public static readonly Color ColSky       = Hex("#4FB0FF");
        public static readonly Color ColSkyEdge   = Hex("#258FDE");
        public static readonly Color ColLilac     = Hex("#A998FF");
        public static readonly Color ColLilacEdge = Hex("#8570E3");
        public static readonly Color ColPeach     = Hex("#FFA96B");
        public static readonly Color ColPeachEdge = Hex("#DE8240");
        public static readonly Color ColSun       = Hex("#FFCF5C");
        public static readonly Color ColSunEdge   = Hex("#DBA02E");
        public static readonly Color ColCoral     = Hex("#FF6F85");
        public static readonly Color ColCoralEdge = Hex("#E04B62");
        public static readonly Color ColTileEdge  = Hex("#D6DEEF");
        public static readonly Color ColCardBg    = new Color(1f, 1f, 1f, 0.92f);

        /// <summary>
        /// Chunky rounded button with solid darker bottom edge (box-shadow 0 7px 0 darker shade)
        /// and soft inner top highlight. Pure white-tinted face so Image.color tints it cleanly.
        /// </summary>
        internal static Sprite CreateTactile3DButtonSprite(int width, int height, float radius, int slice)
        {
            Texture2D tex = NewTex(width, height);
            Color[] px = new Color[width * height];
            Vector2 half = new Vector2(width * 0.5f, height * 0.5f);
            float lipHeight = 7.5f;

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    Vector2 p = new Vector2(x + 0.5f - half.x, y + 0.5f - half.y);
                    float dOuter = SdfRoundedBox(p, half - new Vector2(2f, 2f), radius);
                    if (dOuter > 0.8f)
                    {
                        px[y * width + x] = Color.clear;
                        continue;
                    }

                    float outerAlpha = Mathf.Clamp01(0.7f - dOuter);

                    Vector2 pFace = new Vector2(p.x, p.y - lipHeight * 0.5f);
                    Vector2 halfFace = new Vector2(half.x - 2.5f, half.y - 2.5f - lipHeight * 0.5f);
                    float dFace = SdfRoundedBox(pFace, halfFace, Mathf.Max(4f, radius - 2f));

                    if (dFace <= 0f)
                    {
                        float normY = Mathf.Clamp01((pFace.y + halfFace.y) / (halfFace.y * 2f));
                        float grad = Mathf.Lerp(0.94f, 1.04f, normY);
                        if (normY > 0.82f && dFace > -4f)
                            grad = 1.15f;

                        px[y * width + x] = new Color(Mathf.Clamp01(grad), Mathf.Clamp01(grad), Mathf.Clamp01(grad), outerAlpha);
                    }
                    else
                    {
                        px[y * width + x] = new Color(0.72f, 0.72f, 0.75f, outerAlpha);
                    }
                }
            }

            tex.SetPixels(px);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(slice, slice, slice, slice));
        }

        /// <summary>
        /// Translucent white card with large radius (26-34px), soft low-opacity bluish shadow feel, no hard borders.
        /// </summary>
        internal static Sprite CreateBeveledCardSprite(int width, int height, float radius, int slice)
        {
            Texture2D tex = NewTex(width, height);
            Color[] px = new Color[width * height];
            Vector2 half = new Vector2(width * 0.5f, height * 0.5f);

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    Vector2 p = new Vector2(x + 0.5f - half.x, y + 0.5f - half.y);
                    float d = SdfRoundedBox(p, half - new Vector2(2f, 2f), radius);
                    if (d > 0.8f)
                    {
                        px[y * width + x] = Color.clear;
                        continue;
                    }

                    float alpha = Mathf.Clamp01(0.7f - d);
                    float normY = (float)y / height;
                    float body = Mathf.Lerp(0.96f, 1.02f, normY);
                    if (d > -2.2f && normY > 0.75f)
                        body = 1.08f;

                    px[y * width + x] = new Color(body, body, body, alpha);
                }
            }

            tex.SetPixels(px);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(slice, slice, slice, slice));
        }

        internal static Sprite CreateRecessedSlotSprite(int width, int height, float radius, int slice)
        {
            Texture2D tex = NewTex(width, height);
            Color[] px = new Color[width * height];
            Vector2 half = new Vector2(width * 0.5f, height * 0.5f);

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    Vector2 p = new Vector2(x + 0.5f - half.x, y + 0.5f - half.y);
                    float d = SdfRoundedBox(p, half - new Vector2(2f, 2f), radius);
                    if (d > 0.8f)
                    {
                        px[y * width + x] = Color.clear;
                        continue;
                    }

                    float alpha = Mathf.Clamp01(0.7f - d);
                    float normY = (float)y / height;
                    float shade = (normY > 0.82f) ? 0.85f : Mathf.Lerp(1.01f, 0.94f, normY);
                    px[y * width + x] = new Color(shade, shade, shade, alpha);
                }
            }

            tex.SetPixels(px);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(slice, slice, slice, slice));
        }

        /// <summary>
        /// Soft rounded Caro board cell with calm pastel bevel.
        /// </summary>
        internal static Sprite CreateBeveledCellSprite(int size)
        {
            Texture2D tex = NewTex(size, size);
            Color[] px = new Color[size * size];
            Vector2 half = new Vector2(size * 0.5f, size * 0.5f);
            float radius = 14f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    Vector2 p = new Vector2(x + 0.5f - half.x, y + 0.5f - half.y);
                    float d = SdfRoundedBox(p, half - new Vector2(2f, 2f), radius);
                    if (d > 0.8f)
                    {
                        px[y * size + x] = Color.clear;
                        continue;
                    }

                    float alpha = Mathf.Clamp01(0.7f - d);
                    float normY = (float)y / size;
                    float shade = Mathf.Lerp(0.95f, 1.03f, normY);
                    if (d > -2.0f && normY > 0.7f)
                        shade = 1.08f;

                    px[y * size + x] = new Color(shade, shade, shade, alpha);
                }
            }

            tex.SetPixels(px);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(16, 16, 16, 16));
        }

        internal static Sprite CreateRoundedRectSprite(int width, int height, float radius, int borderSlice)
        {
            Texture2D tex = NewTex(width, height);
            Color[] pixels = new Color[width * height];
            Vector2 halfSize = new Vector2(width * 0.5f, height * 0.5f);

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    Vector2 p = new Vector2(x + 0.5f - halfSize.x, y + 0.5f - halfSize.y);
                    float dist = SdfRoundedBox(p, halfSize - new Vector2(1f, 1f), radius);
                    float alpha = Mathf.Clamp01(0.6f - dist);
                    pixels[y * width + x] = new Color(1f, 1f, 1f, alpha);
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(borderSlice, borderSlice, borderSlice, borderSlice));
        }

        internal static Sprite CreateVerticalFadeSprite(int height)
        {
            Texture2D tex = NewTex(1, height);
            Color[] pixels = new Color[height];
            for (int y = 0; y < height; y++)
            {
                float t = y / (float)(height - 1);
                pixels[y] = new Color(1f, 1f, 1f, t * t * (3f - 2f * t));
            }

            tex.SetPixels(pixels);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, 1, height), new Vector2(0.5f, 0.5f), 100f);
        }

        internal static Sprite CreateCircleSprite(int size)
        {
            Texture2D tex = NewTex(size, size);
            Color[] pixels = new Color[size * size];
            float center = size * 0.5f;
            float radius = size * 0.46f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(center, center));
                    float alpha = Mathf.Clamp01(radius - d + 0.75f);
                    pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
        }

        internal static Sprite CreateHintTargetSprite(int size)
        {
            Texture2D tex = NewTex(size, size);
            Color[] pixels = new Color[size * size];
            Vector2 c = new Vector2(size * 0.5f, size * 0.5f);
            float radius = size * 0.42f;
            float thickness = size * 0.065f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    Vector2 p = new Vector2(x + 0.5f, y + 0.5f);
                    float dist = Vector2.Distance(p, c);
                    float ring = Mathf.Abs(dist - radius) - thickness;
                    float centerDot = dist - (size * 0.10f);
                    float d = Mathf.Min(ring, centerDot);

                    float alpha = Mathf.Clamp01(0.8f - d);
                    pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
                }
            }
            return FinalizeIcon(tex, pixels, size);
        }

        internal static Texture2D NewTex(int w, int h)
        {
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.filterMode = FilterMode.Bilinear;
            return tex;
        }

        internal static Sprite FinalizeIcon(Texture2D tex, Color[] px, int size)
        {
            tex.SetPixels(px);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
        }

        internal static float SdfRoundedBox(Vector2 p, Vector2 b, float r)
        {
            Vector2 q = new Vector2(Mathf.Abs(p.x), Mathf.Abs(p.y)) - (b - new Vector2(r, r));
            return Vector2.Max(q, Vector2.zero).magnitude + Mathf.Min(Mathf.Max(q.x, q.y), 0f) - r;
        }

        internal static float DistanceToSegment(Vector2 p, Vector2 a, Vector2 b)
        {
            Vector2 pa = p - a;
            Vector2 ba = b - a;
            float h = Mathf.Clamp01(Vector2.Dot(pa, ba) / Vector2.Dot(ba, ba));
            return (pa - ba * h).magnitude;
        }

        internal static Color Hex(string hex)
        {
            if (string.IsNullOrEmpty(hex)) return Color.white;
            if (hex.StartsWith("#")) hex = hex.Substring(1);
            if (hex.Length == 6 &&
                uint.TryParse(hex, System.Globalization.NumberStyles.HexNumber, null, out uint rgb))
            {
                float r = ((rgb >> 16) & 0xFF) / 255f;
                float g = ((rgb >> 8) & 0xFF) / 255f;
                float b = (rgb & 0xFF) / 255f;
                return new Color(r, g, b, 1f);
            }
            if (hex.Length == 8 &&
                uint.TryParse(hex, System.Globalization.NumberStyles.HexNumber, null, out uint rgba))
            {
                float r = ((rgba >> 24) & 0xFF) / 255f;
                float g = ((rgba >> 16) & 0xFF) / 255f;
                float b = ((rgba >> 8) & 0xFF) / 255f;
                float a = (rgba & 0xFF) / 255f;
                return new Color(r, g, b, a);
            }
            return Color.white;
        }

        internal static Color WithAlpha(Color c, float a)
        {
            c.a = a;
            return c;
        }
    }
}

