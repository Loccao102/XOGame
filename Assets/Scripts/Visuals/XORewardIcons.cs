using UnityEngine;
using static XOPlayable.Visuals.XOTexturePrimitives;

namespace XOPlayable.Visuals
{
    /// <summary>
    /// Procedural SDF vector icons for relics, trophies, crowns, swords, shields, and rewarded video ads.
    /// </summary>
    internal static class XORewardIcons
    {
        /// <summary>
        /// 15c. Relic / Joker: Shiny diamond talisman for roguelike relics.
        /// </summary>
        internal static Sprite CreateRelicJokerIcon(int size)
        {
            Texture2D tex = NewTex(size, size);
            Color[] px = new Color[size * size];
            Vector2 c = new Vector2(size * 0.5f, size * 0.5f);
            float strokeW = size * 0.085f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    Vector2 p = new Vector2(x + 0.5f, y + 0.5f) - c;
                    float dDiamond = (Mathf.Abs(p.x) * 0.95f + Mathf.Abs(p.y) * 1.05f) - size * 0.38f;
                    if (dDiamond > 0.8f)
                    {
                        px[y * size + x] = Color.clear;
                        continue;
                    }

                    float alpha = Mathf.Clamp01(0.7f - dDiamond);
                    if (dDiamond > -strokeW)
                    {
                        px[y * size + x] = WithAlpha(ColInk, alpha);
                        continue;
                    }

                    Color facetCol = (p.y > 0f && p.x < 0f) ? ColSun
                        : (p.y > 0f) ? ColMint
                        : (p.x < 0f) ? ColSky
                        : ColLilac;
                    px[y * size + x] = WithAlpha(facetCol, alpha);
                }
            }
            return FinalizeIcon(tex, px, size);
        }

        /// <summary>
        /// 16. Trophy: Clean golden chalice cup on a pedestal base.
        /// </summary>
        internal static Sprite CreateTrophyIcon(int size)
        {
            Texture2D tex = NewTex(size, size);
            Color[] px = new Color[size * size];
            Vector2 c = new Vector2(size * 0.5f, size * 0.5f);
            float strokeW = size * 0.085f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    Vector2 p = new Vector2(x + 0.5f, y + 0.5f) - c;
                    float cupW = Mathf.Lerp(size * 0.17f, size * 0.34f, Mathf.Clamp01((p.y + size * 0.06f) / (size * 0.42f)));
                    float dBowl = SdfRoundedBox(p - new Vector2(0f, size * 0.15f), new Vector2(cupW, size * 0.25f), 10f);
                    float dStem = SdfRoundedBox(p - new Vector2(0f, -size * 0.16f), new Vector2(size * 0.08f, size * 0.14f), 4f);
                    float dBase = SdfRoundedBox(p - new Vector2(0f, -size * 0.32f), new Vector2(size * 0.28f, size * 0.08f), 6f);
                    float d = Mathf.Min(dBowl, Mathf.Min(dStem, dBase));

                    if (d > 0.8f)
                    {
                        px[y * size + x] = Color.clear;
                        continue;
                    }

                    float alpha = Mathf.Clamp01(0.7f - d);
                    px[y * size + x] = WithAlpha((d > -strokeW) ? ColInk : ColSun, alpha);
                }
            }
            return FinalizeIcon(tex, px, size);
        }

        /// <summary>
        /// 17. Crown: Clean 3-point royal crown.
        /// </summary>
        internal static Sprite CreateCrownIcon(int size)
        {
            Texture2D tex = NewTex(size, size);
            Color[] px = new Color[size * size];
            Vector2 c = new Vector2(size * 0.5f, size * 0.46f);
            float strokeW = size * 0.085f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    Vector2 p = new Vector2(x + 0.5f, y + 0.5f);
                    float dBase = SdfRoundedBox(p - (c + new Vector2(0f, -size * 0.20f)), new Vector2(size * 0.40f, size * 0.09f), 6f);
                    float dLeft = DistanceToSegment(p, c + new Vector2(-size * 0.35f, -size * 0.12f), c + new Vector2(-size * 0.37f, size * 0.25f)) - size * 0.09f;
                    float dMid = DistanceToSegment(p, c + new Vector2(0f, -size * 0.12f), c + new Vector2(0f, size * 0.32f)) - size * 0.10f;
                    float dRight = DistanceToSegment(p, c + new Vector2(size * 0.35f, -size * 0.12f), c + new Vector2(size * 0.37f, size * 0.25f)) - size * 0.09f;
                    float dBody = SdfRoundedBox(p - (c + new Vector2(0f, -size * 0.04f)), new Vector2(size * 0.35f, size * 0.11f), 6f);
                    float d = Mathf.Min(Mathf.Min(dBase, dBody), Mathf.Min(dMid, Mathf.Min(dLeft, dRight)));

                    if (d > 0.8f)
                    {
                        px[y * size + x] = Color.clear;
                        continue;
                    }

                    float alpha = Mathf.Clamp01(0.7f - d);
                    px[y * size + x] = WithAlpha((d > -strokeW) ? ColInk : ColSun, alpha);
                }
            }
            return FinalizeIcon(tex, px, size);
        }

        internal static Sprite CreateVideoAdClapperIcon(int size)
        {
            Texture2D tex = NewTex(size, size);
            Color[] px = new Color[size * size];
            Vector2 c = new Vector2(size * 0.5f, size * 0.5f);
            float strokeW = size * 0.085f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    Vector2 p = new Vector2(x + 0.5f, y + 0.5f) - c;
                    float dBox = SdfRoundedBox(p, new Vector2(size * 0.44f, size * 0.35f), 14f);
                    if (dBox > 0.8f)
                    {
                        px[y * size + x] = Color.clear;
                        continue;
                    }

                    float alpha = Mathf.Clamp01(0.7f - dBox);
                    if (dBox > -strokeW)
                    {
                        px[y * size + x] = WithAlpha(ColInk, alpha);
                        continue;
                    }

                    float tx = p.x + size * 0.08f;
                    float ty = Mathf.Abs(p.y);
                    bool inPlayTri = tx > 0f && tx < size * 0.25f && ty < (size * 0.25f - tx) * 0.68f;
                    px[y * size + x] = WithAlpha(inPlayTri ? ColInk : ColSun, alpha);
                }
            }
            return FinalizeIcon(tex, px, size);
        }

        internal static Sprite CreateCrossedSwordsIcon(int size)
        {
            Texture2D tex = NewTex(size, size);
            Color[] px = new Color[size * size];
            Vector2 c = new Vector2(size * 0.5f, size * 0.5f);
            float strokeW = size * 0.095f;

            Vector2 s1A = c + new Vector2(-size * 0.38f, -size * 0.38f);
            Vector2 s1B = c + new Vector2(size * 0.38f, size * 0.38f);
            Vector2 s2A = c + new Vector2(-size * 0.38f, size * 0.38f);
            Vector2 s2B = c + new Vector2(size * 0.38f, -size * 0.38f);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    Vector2 p = new Vector2(x + 0.5f, y + 0.5f);
                    float d = Mathf.Min(DistanceToSegment(p, s1A, s1B) - strokeW, DistanceToSegment(p, s2A, s2B) - strokeW);
                    if (d > 0.8f)
                    {
                        px[y * size + x] = Color.clear;
                        continue;
                    }

                    float alpha = Mathf.Clamp01(0.7f - d);
                    px[y * size + x] = WithAlpha(ColSky, alpha);
                }
            }
            return FinalizeIcon(tex, px, size);
        }

        internal static Sprite CreateShieldIcon(int size)
        {
            Texture2D tex = NewTex(size, size);
            Color[] px = new Color[size * size];
            Vector2 c = new Vector2(size * 0.5f, size * 0.52f);
            float strokeW = size * 0.085f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    Vector2 p = new Vector2(x + 0.5f, y + 0.5f) - c;
                    float taper = (p.y < 0f) ? Mathf.Lerp(size * 0.38f, size * 0.05f, Mathf.Clamp01(-p.y / (size * 0.44f))) : size * 0.38f;
                    float d = SdfRoundedBox(p, new Vector2(taper, size * 0.40f), 10f);
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

