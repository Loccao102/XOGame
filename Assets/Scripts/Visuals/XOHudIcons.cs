using UnityEngine;
using static XOPlayable.Visuals.XOTexturePrimitives;

namespace XOPlayable.Visuals
{
    /// <summary>
    /// Procedural SDF vector icons for Lobby HUD, economy, navigation, and modal entrypoints.
    /// </summary>
    internal static class XOHudIcons
    {
        /// <summary>
        /// 1. Coin: Flat filled circle with thin inner concentric ring.
        /// </summary>
        internal static Sprite CreateCoinIcon(int size)
        {
            Texture2D tex = NewTex(size, size);
            Color[] px = new Color[size * size];
            Vector2 c = new Vector2(size * 0.5f, size * 0.5f);
            float rOuter = size * 0.44f;
            float rInner = size * 0.26f;
            float strokeW = size * 0.085f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    Vector2 p = new Vector2(x + 0.5f, y + 0.5f);
                    float dist = Vector2.Distance(p, c);
                    float dOuter = dist - rOuter;

                    if (dOuter > 0.8f)
                    {
                        px[y * size + x] = Color.clear;
                        continue;
                    }

                    float alpha = Mathf.Clamp01(0.7f - dOuter);
                    if (dOuter > -strokeW)
                    {
                        px[y * size + x] = WithAlpha(ColInk, alpha);
                        continue;
                    }

                    float dInnerRing = Mathf.Abs(dist - rInner) - (strokeW * 0.45f);
                    px[y * size + x] = WithAlpha((dInnerRing < 0f) ? ColInk : ColSun, alpha);
                }
            }
            return FinalizeIcon(tex, px, size);
        }

        /// <summary>
        /// 2. Streak Flame: Smooth continuous teardrop flame with soft inner tongue.
        /// </summary>
        internal static Sprite CreateFlameIcon(int size)
        {
            Texture2D tex = NewTex(size, size);
            Color[] px = new Color[size * size];
            Vector2 c = new Vector2(size * 0.5f, size * 0.38f);
            float strokeW = size * 0.085f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    Vector2 p = new Vector2(x + 0.5f, y + 0.5f);
                    float dBottom = Vector2.Distance(p, c) - size * 0.36f;
                    float dTip = DistanceToSegment(p, c, c + new Vector2(0f, size * 0.48f)) - Mathf.Lerp(size * 0.36f, size * 0.05f, Mathf.Clamp01((p.y - c.y) / (size * 0.50f)));
                    float dFlame = Mathf.Min(dBottom, dTip);

                    if (dFlame > 0.8f)
                    {
                        px[y * size + x] = Color.clear;
                        continue;
                    }

                    float alpha = Mathf.Clamp01(0.7f - dFlame);
                    if (dFlame > -strokeW)
                    {
                        px[y * size + x] = WithAlpha(ColInk, alpha);
                        continue;
                    }

                    float dTongue = Vector2.Distance(p, c + new Vector2(0f, -size * 0.03f)) - size * 0.17f;
                    px[y * size + x] = WithAlpha((dTongue < 0f) ? ColSun : ColPeach, alpha);
                }
            }
            return FinalizeIcon(tex, px, size);
        }

        /// <summary>
        /// 3. Sound on/off: Rounded speaker horn with concentric waves (on) or diagonal slash (off).
        /// </summary>
        internal static Sprite CreateSoundIcon(int size, bool muted)
        {
            Texture2D tex = NewTex(size, size);
            Color[] px = new Color[size * size];
            Vector2 c = new Vector2(size * 0.38f, size * 0.5f);
            float strokeW = size * 0.085f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    Vector2 p = new Vector2(x + 0.5f, y + 0.5f);
                    float dBox = SdfRoundedBox(p - (c + new Vector2(-size * 0.18f, 0f)), new Vector2(size * 0.11f, size * 0.14f), 5f);
                    float dCone = DistanceToSegment(p, c + new Vector2(-size * 0.11f, 0f), c + new Vector2(size * 0.10f, 0f)) - Mathf.Lerp(size * 0.14f, size * 0.34f, Mathf.Clamp01((p.x - (c.x - size * 0.11f)) / (size * 0.22f)));
                    float dSpeaker = Mathf.Min(dBox, dCone);

                    float dExtra = 999f;
                    if (!muted)
                    {
                        Vector2 rel = p - c;
                        float angle = Mathf.Abs(Mathf.Atan2(rel.y, rel.x) * Mathf.Rad2Deg);
                        if (angle < 46f)
                        {
                            float w1 = Mathf.Abs(rel.magnitude - size * 0.26f) - (strokeW * 0.5f);
                            float w2 = Mathf.Abs(rel.magnitude - size * 0.42f) - (strokeW * 0.5f);
                            dExtra = Mathf.Min(w1, w2);
                        }
                    }
                    else
                    {
                        Vector2 slashA = new Vector2(size * 0.14f, size * 0.14f);
                        Vector2 slashB = new Vector2(size * 0.86f, size * 0.86f);
                        dExtra = DistanceToSegment(p, slashA, slashB) - (strokeW * 0.65f);
                    }

                    float d = Mathf.Min(dSpeaker, dExtra);
                    if (d > 0.8f)
                    {
                        px[y * size + x] = Color.clear;
                        continue;
                    }

                    float alpha = Mathf.Clamp01(0.7f - d);
                    if (dExtra < dSpeaker)
                    {
                        px[y * size + x] = WithAlpha(muted ? ColCoral : ColSky, alpha);
                        continue;
                    }

                    px[y * size + x] = WithAlpha((dSpeaker > -strokeW) ? ColInk : ColSky, alpha);
                }
            }
            return FinalizeIcon(tex, px, size);
        }

        /// <summary>
        /// 4. Play: Centered triangle with rounded vertices pointing right.
        /// </summary>
        internal static Sprite CreatePlayStarIcon(int size)
        {
            Texture2D tex = NewTex(size, size);
            Color[] px = new Color[size * size];
            Vector2 c = new Vector2(size * 0.52f, size * 0.5f);
            float strokeW = size * 0.085f;

            Vector2 v1 = c + new Vector2(-size * 0.26f, -size * 0.36f);
            Vector2 v2 = c + new Vector2(-size * 0.26f, size * 0.36f);
            Vector2 v3 = c + new Vector2(size * 0.34f, 0f);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    Vector2 p = new Vector2(x + 0.5f, y + 0.5f);
                    float d1 = DistanceToSegment(p, v1, v2);
                    float d2 = DistanceToSegment(p, v2, v3);
                    float d3 = DistanceToSegment(p, v3, v1);
                    float edgeDist = Mathf.Min(d1, Mathf.Min(d2, d3));

                    bool inside = (p.x >= v1.x) &&
                                  ((p.y - v1.y) * (v3.x - v1.x) - (p.x - v1.x) * (v3.y - v1.y) >= 0) &&
                                  ((p.y - v2.y) * (v3.x - v2.x) - (p.x - v2.x) * (v3.y - v2.y) <= 0);

                    float d = (inside ? -edgeDist : edgeDist) - (size * 0.07f);
                    if (d > 0.8f)
                    {
                        px[y * size + x] = Color.clear;
                        continue;
                    }

                    float alpha = Mathf.Clamp01(0.7f - d);
                    px[y * size + x] = WithAlpha((d > -strokeW) ? ColInk : Color.white, alpha);
                }
            }
            return FinalizeIcon(tex, px, size);
        }

        /// <summary>
        /// 5. Rivals: Three stepped rounded vertical bars on a baseline.
        /// </summary>
        internal static Sprite CreateLeaderboardPodiumIcon(int size)
        {
            Texture2D tex = NewTex(size, size);
            Color[] px = new Color[size * size];
            Vector2 c = new Vector2(size * 0.5f, size * 0.48f);
            float strokeW = size * 0.085f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    Vector2 p = new Vector2(x + 0.5f, y + 0.5f) - c;
                    float dBase = DistanceToSegment(p, new Vector2(-size * 0.44f, -size * 0.36f), new Vector2(size * 0.44f, -size * 0.36f)) - (strokeW * 0.5f);
                    float dCenter = SdfRoundedBox(p - new Vector2(0f, -size * 0.02f), new Vector2(size * 0.13f, size * 0.34f), 6f);
                    float dLeft = SdfRoundedBox(p - new Vector2(-size * 0.28f, -size * 0.10f), new Vector2(size * 0.12f, size * 0.24f), 6f);
                    float dRight = SdfRoundedBox(p - new Vector2(size * 0.28f, -size * 0.17f), new Vector2(size * 0.12f, size * 0.17f), 6f);

                    float dBars = Mathf.Min(dCenter, Mathf.Min(dLeft, dRight));
                    float d = Mathf.Min(dBase, dBars);
                    if (d > 0.8f)
                    {
                        px[y * size + x] = Color.clear;
                        continue;
                    }

                    float alpha = Mathf.Clamp01(0.7f - d);
                    if (dBars <= 0f)
                    {
                        Color barCol = (dBars > -strokeW * 0.8f) ? ColInk
                            : (dCenter <= 0f) ? ColSun
                            : (dLeft <= 0f) ? ColSky
                            : ColPeach;
                        px[y * size + x] = WithAlpha(barCol, alpha);
                        continue;
                    }

                    px[y * size + x] = WithAlpha(ColInk, alpha);
                }
            }
            return FinalizeIcon(tex, px, size);
        }

        /// <summary>
        /// 6. Spin wheel: Outer circle with center hub, 4 spoke lines and top indicator pointer.
        /// </summary>
        internal static Sprite CreateLuckyWheelIcon(int size)
        {
            Texture2D tex = NewTex(size, size);
            Color[] px = new Color[size * size];
            Vector2 c = new Vector2(size * 0.5f, size * 0.46f);
            float rOuter = size * 0.42f;
            float rHub = size * 0.12f;
            float strokeW = size * 0.085f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    Vector2 p = new Vector2(x + 0.5f, y + 0.5f) - c;
                    float dist = p.magnitude;
                    float dCircle = dist - rOuter;
                    Vector2 pPtr = p - new Vector2(0f, rOuter + size * 0.04f);
                    float dPtr = (Mathf.Abs(pPtr.x) * 1.2f + pPtr.y) - size * 0.09f;

                    float d = Mathf.Min(dCircle, dPtr);
                    if (d > 0.8f)
                    {
                        px[y * size + x] = Color.clear;
                        continue;
                    }

                    float alpha = Mathf.Clamp01(0.7f - d);
                    if (dPtr <= 0f)
                    {
                        px[y * size + x] = WithAlpha(ColCoral, alpha);
                        continue;
                    }
                    if (dCircle > -strokeW)
                    {
                        px[y * size + x] = WithAlpha(ColInk, alpha);
                        continue;
                    }
                    if (dist < rHub)
                    {
                        px[y * size + x] = WithAlpha(ColSun, alpha);
                        continue;
                    }

                    float dSpokeH = Mathf.Abs(p.y) - (strokeW * 0.5f);
                    float dSpokeV = Mathf.Abs(p.x) - (strokeW * 0.5f);
                    if (dSpokeH < 0f || dSpokeV < 0f)
                    {
                        px[y * size + x] = WithAlpha(ColInk, alpha);
                        continue;
                    }

                    bool right = p.x > 0f;
                    bool top = p.y > 0f;
                    Color sliceCol = (top && right) ? ColSky : ((!top && right) ? ColMint : ((top && !right) ? ColLilac : ColPeach));
                    px[y * size + x] = WithAlpha(sliceCol, alpha);
                }
            }
            return FinalizeIcon(tex, px, size);
        }

        /// <summary>
        /// 7. Skins: Organic rounded paint palette with thumb hole and 3 pigment wells.
        /// </summary>
        internal static Sprite CreatePaletteShopIcon(int size)
        {
            Texture2D tex = NewTex(size, size);
            Color[] px = new Color[size * size];
            Vector2 c = new Vector2(size * 0.5f, size * 0.5f);
            float strokeW = size * 0.085f;

            Vector2 dot1 = c + new Vector2(-size * 0.18f, size * 0.14f);
            Vector2 dot2 = c + new Vector2(size * 0.03f, size * 0.22f);
            Vector2 dot3 = c + new Vector2(size * 0.22f, size * 0.10f);
            float dotR = size * 0.085f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    Vector2 p = new Vector2(x + 0.5f, y + 0.5f);
                    float dMain = Vector2.Distance(p, c) - size * 0.43f;
                    float dThumb = Vector2.Distance(p, c + new Vector2(-size * 0.15f, -size * 0.15f)) - size * 0.11f;
                    float d = Mathf.Max(dMain, -dThumb);

                    if (d > 0.8f)
                    {
                        px[y * size + x] = Color.clear;
                        continue;
                    }

                    float alpha = Mathf.Clamp01(0.7f - d);
                    if (d > -strokeW || dThumb < strokeW * 0.8f)
                    {
                        px[y * size + x] = WithAlpha(ColInk, alpha);
                        continue;
                    }

                    Color wellCol = (Vector2.Distance(p, dot1) < dotR) ? ColMint
                        : (Vector2.Distance(p, dot2) < dotR) ? ColSun
                        : (Vector2.Distance(p, dot3) < dotR) ? ColCoral
                        : ColLilac;
                    px[y * size + x] = WithAlpha(wellCol, alpha);
                }
            }
            return FinalizeIcon(tex, px, size);
        }

        /// <summary>
        /// 8. Chest: Rounded treasure chest with arched lid, tub base, seam, and latch.
        /// </summary>
        internal static Sprite CreateChestIcon(int size)
        {
            Texture2D tex = NewTex(size, size);
            Color[] px = new Color[size * size];
            Vector2 c = new Vector2(size * 0.5f, size * 0.48f);
            float strokeW = size * 0.085f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    Vector2 p = new Vector2(x + 0.5f, y + 0.5f) - c;
                    float dLid = SdfRoundedBox(p - new Vector2(0f, size * 0.12f), new Vector2(size * 0.42f, size * 0.20f), 14f);
                    float dTub = SdfRoundedBox(p - new Vector2(0f, -size * 0.15f), new Vector2(size * 0.42f, size * 0.20f), 10f);
                    float d = Mathf.Min(dLid, dTub);

                    if (d > 0.8f)
                    {
                        px[y * size + x] = Color.clear;
                        continue;
                    }

                    float alpha = Mathf.Clamp01(0.7f - d);
                    if (d > -strokeW)
                    {
                        px[y * size + x] = WithAlpha(ColInk, alpha);
                        continue;
                    }

                    float dLock = SdfRoundedBox(p - new Vector2(0f, -size * 0.01f), new Vector2(size * 0.09f, size * 0.10f), 4f);
                    if (dLock < 0f)
                    {
                        px[y * size + x] = WithAlpha(ColSun, alpha);
                        continue;
                    }

                    if (Mathf.Abs(p.y - size * 0.02f) < strokeW * 0.5f)
                    {
                        px[y * size + x] = WithAlpha(ColInk, alpha);
                        continue;
                    }

                    px[y * size + x] = WithAlpha((p.y > 0f) ? ColSun : ColPeach, alpha);
                }
            }
            return FinalizeIcon(tex, px, size);
        }
    }
}

