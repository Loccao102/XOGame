using UnityEngine;
using static XOPlayable.Visuals.XOTexturePrimitives;

namespace XOPlayable.Visuals
{
    /// <summary>
    /// Procedural sprites for X/O board tokens and collectible skin pieces (Ruby Gem, Crescent Moon).
    /// </summary>
    internal static class XOPieceSprites
    {
        /// <summary>
        /// 9. X Game Token: Clean rounded crossing strokes in pastel clay style.
        /// </summary>
        internal static Sprite CreateXPieceSprite(int size)
        {
            Texture2D tex = NewTex(size, size);
            Color[] pixels = new Color[size * size];
            Vector2 c = new Vector2(size * 0.5f, size * 0.5f);
            float armLen = size * 0.35f;
            float thickness = size * 0.13f;

            Vector2 a1 = c + new Vector2(-armLen, -armLen);
            Vector2 b1 = c + new Vector2(armLen, armLen);
            Vector2 a2 = c + new Vector2(-armLen, armLen);
            Vector2 b2 = c + new Vector2(armLen, -armLen);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    Vector2 p = new Vector2(x + 0.5f, y + 0.5f);
                    float d1 = DistanceToSegment(p, a1, b1) - thickness;
                    float d2 = DistanceToSegment(p, a2, b2) - thickness;
                    float d = Mathf.Min(d1, d2);

                    if (d > 0.8f)
                    {
                        pixels[y * size + x] = Color.clear;
                        continue;
                    }

                    float alpha = Mathf.Clamp01(0.7f - d);
                    float shade = (d < -thickness * 0.45f) ? 1.08f : 0.96f;
                    pixels[y * size + x] = new Color(ColSky.r * shade, ColSky.g * shade, ColSky.b * shade, alpha);
                }
            }
            return FinalizeIcon(tex, pixels, size);
        }

        /// <summary>
        /// 10. O Game Token: Clean rounded circular ring in pastel clay style.
        /// </summary>
        internal static Sprite CreateOPieceSprite(int size)
        {
            Texture2D tex = NewTex(size, size);
            Color[] pixels = new Color[size * size];
            Vector2 c = new Vector2(size * 0.5f, size * 0.5f);
            float radius = size * 0.35f;
            float thickness = size * 0.125f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    Vector2 p = new Vector2(x + 0.5f, y + 0.5f);
                    float dist = Vector2.Distance(p, c);
                    float d = Mathf.Abs(dist - radius) - thickness;

                    if (d > 0.8f)
                    {
                        pixels[y * size + x] = Color.clear;
                        continue;
                    }

                    float alpha = Mathf.Clamp01(0.7f - d);
                    float shade = (d < -thickness * 0.45f) ? 1.08f : 0.96f;
                    pixels[y * size + x] = new Color(ColCoral.r * shade, ColCoral.g * shade, ColCoral.b * shade, alpha);
                }
            }
            return FinalizeIcon(tex, pixels, size);
        }

        internal static Sprite CreateRubyGemIcon(int size)
        {
            Texture2D tex = NewTex(size, size);
            Color[] px = new Color[size * size];
            Vector2 c = new Vector2(size * 0.5f, size * 0.5f);
            float strokeW = size * 0.085f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    Vector2 rel = new Vector2(x + 0.5f, y + 0.5f) - c;
                    float d = (Mathf.Abs(rel.x) * 1.1f + Mathf.Abs(rel.y)) - size * 0.44f;

                    if (d > 0.8f)
                    {
                        px[y * size + x] = Color.clear;
                        continue;
                    }

                    float alpha = Mathf.Clamp01(0.7f - d);
                    px[y * size + x] = WithAlpha((d > -strokeW) ? ColInk : ColCoral, alpha);
                }
            }
            return FinalizeIcon(tex, px, size);
        }

        internal static Sprite CreateCrescentMoonIcon(int size)
        {
            Texture2D tex = NewTex(size, size);
            Color[] px = new Color[size * size];
            Vector2 c = new Vector2(size * 0.5f, size * 0.5f);
            float strokeW = size * 0.085f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    Vector2 p = new Vector2(x + 0.5f, y + 0.5f);
                    float dOuter = Vector2.Distance(p, c) - size * 0.43f;
                    float dCut = Vector2.Distance(p, c + new Vector2(size * 0.20f, size * 0.12f)) - size * 0.35f;
                    float d = Mathf.Max(dOuter, -dCut);

                    if (d > 0.8f)
                    {
                        px[y * size + x] = Color.clear;
                        continue;
                    }

                    float alpha = Mathf.Clamp01(0.7f - d);
                    px[y * size + x] = WithAlpha((d > -strokeW) ? ColInk : ColLilac, alpha);
                }
            }
            return FinalizeIcon(tex, px, size);
        }
    }
}

