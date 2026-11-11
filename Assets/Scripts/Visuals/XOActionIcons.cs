using UnityEngine;
using static XOPlayable.Visuals.XOTexturePrimitives;

namespace XOPlayable.Visuals
{
    /// <summary>
    /// Procedural SDF vector icons for battle tactical tools, navigation, and special board tiles.
    /// </summary>
    internal static class XOActionIcons
    {
        /// <summary>
        /// 11. Hint: Soft rounded lightbulb globe with screw base.
        /// </summary>
        internal static Sprite CreateHintBulbIcon(int size)
        {
            Texture2D tex = NewTex(size, size);
            Color[] px = new Color[size * size];
            Vector2 globeCenter = new Vector2(size * 0.5f, size * 0.56f);
            float strokeW = size * 0.085f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    Vector2 p = new Vector2(x + 0.5f, y + 0.5f);
                    float dGlobe = Vector2.Distance(p, globeCenter) - size * 0.34f;
                    float dNeck = SdfRoundedBox(p - new Vector2(size * 0.5f, size * 0.26f), new Vector2(size * 0.18f, size * 0.12f), 5f);
                    float dScrew = SdfRoundedBox(p - new Vector2(size * 0.5f, size * 0.13f), new Vector2(size * 0.14f, size * 0.09f), 5f);
                    float d = Mathf.Min(Mathf.Min(dGlobe, dNeck), dScrew);

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

                    Color fill = (p.y < size * 0.20f) ? ColTileEdge : new Color(1f, 0.98f, 0.90f);
                    px[y * size + x] = WithAlpha(fill, alpha);
                }
            }
            return FinalizeIcon(tex, px, size);
        }

        /// <summary>
        /// 12. Undo: Smooth circular 270-deg arrow arc with rounded arrowhead.
        /// </summary>
        internal static Sprite CreateUndoArrowIcon(int size)
        {
            Texture2D tex = NewTex(size, size);
            Color[] px = new Color[size * size];
            Vector2 c = new Vector2(size * 0.52f, size * 0.48f);
            float strokeW = size * 0.085f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    Vector2 p = new Vector2(x + 0.5f, y + 0.5f);
                    Vector2 rel = p - c;
                    float dist = rel.magnitude;
                    float angle = Mathf.Atan2(rel.y, rel.x) * Mathf.Rad2Deg;

                    float dRing = Mathf.Abs(dist - size * 0.33f) - strokeW;
                    if (angle > 135f || angle < -145f) dRing = 999f;

                    Vector2 headTip = c + new Vector2(-size * 0.37f, size * 0.14f);
                    Vector2 headA = c + new Vector2(-size * 0.14f, size * 0.42f);
                    Vector2 headB = c + new Vector2(-size * 0.14f, 0f);
                    float dHead1 = DistanceToSegment(p, headTip, headA) - (strokeW * 0.9f);
                    float dHead2 = DistanceToSegment(p, headTip, headB) - (strokeW * 0.9f);
                    float d = Mathf.Min(dRing, Mathf.Min(dHead1, dHead2));

                    if (d > 0.8f)
                    {
                        px[y * size + x] = Color.clear;
                        continue;
                    }

                    float alpha = Mathf.Clamp01(0.7f - d);
                    px[y * size + x] = WithAlpha((d > -strokeW * 0.45f) ? ColInk : Color.white, alpha);
                }
            }
            return FinalizeIcon(tex, px, size);
        }

        /// <summary>
        /// 13. Bomb: Clean smooth sphere, curved fuse, sun-gold spark.
        /// </summary>
        internal static Sprite CreateBombIcon(int size)
        {
            Texture2D tex = NewTex(size, size);
            Color[] px = new Color[size * size];
            Vector2 bodyCenter = new Vector2(size * 0.44f, size * 0.42f);
            Vector2 sparkCenter = new Vector2(size * 0.78f, size * 0.80f);
            float strokeW = size * 0.085f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    Vector2 p = new Vector2(x + 0.5f, y + 0.5f);
                    float dBody = Vector2.Distance(p, bodyCenter) - size * 0.34f;
                    float dFuse = DistanceToSegment(p, bodyCenter + new Vector2(size * 0.20f, size * 0.22f), sparkCenter) - (strokeW * 0.7f);
                    float dSpark = Vector2.Distance(p, sparkCenter) - size * 0.14f;
                    float d = Mathf.Min(dBody, Mathf.Min(dFuse, dSpark));

                    if (d > 0.8f)
                    {
                        px[y * size + x] = Color.clear;
                        continue;
                    }

                    float alpha = Mathf.Clamp01(0.7f - d);
                    if (dSpark <= 0f)
                    {
                        px[y * size + x] = WithAlpha(ColSun, alpha);
                        continue;
                    }
                    if (dFuse <= 0f)
                    {
                        px[y * size + x] = WithAlpha(ColTileEdge, alpha);
                        continue;
                    }

                    float dHighlight = Vector2.Distance(p, bodyCenter + new Vector2(-size * 0.10f, size * 0.12f)) - size * 0.08f;
                    px[y * size + x] = WithAlpha((dHighlight <= 0f) ? Color.white : ColInk, alpha);
                }
            }
            return FinalizeIcon(tex, px, size);
        }

        /// <summary>
        /// 14. Freeze: Clean 6-arm snowflake with round ends.
        /// </summary>
        internal static Sprite CreateFreezeSnowflakeIcon(int size)
        {
            Texture2D tex = NewTex(size, size);
            Color[] px = new Color[size * size];
            Vector2 c = new Vector2(size * 0.5f, size * 0.5f);
            float armLen = size * 0.44f;
            float strokeW = size * 0.095f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    Vector2 p = new Vector2(x + 0.5f, y + 0.5f);
                    float d = 999f;

                    for (int a = 0; a < 3; a++)
                    {
                        float rad = a * 60f * Mathf.Deg2Rad;
                        Vector2 dir = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)) * armLen;
                        d = Mathf.Min(d, DistanceToSegment(p, c - dir, c + dir) - strokeW);
                    }

                    if (d > 0.8f)
                    {
                        px[y * size + x] = Color.clear;
                        continue;
                    }

                    float alpha = Mathf.Clamp01(0.7f - d);
                    px[y * size + x] = WithAlpha((d > -strokeW * 0.45f) ? ColInk : Color.white, alpha);
                }
            }
            return FinalizeIcon(tex, px, size);
        }

        /// <summary>
        /// 15. Home: Clean rounded gable roof, house base, and arched center door.
        /// </summary>
        internal static Sprite CreateHomeIcon(int size)
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
                    float dBody = SdfRoundedBox(p - (c + new Vector2(0f, -size * 0.10f)), new Vector2(size * 0.34f, size * 0.25f), 5f);
                    Vector2 pRoof = p - (c + new Vector2(0f, size * 0.13f));
                    float dRoof = (Mathf.Abs(pRoof.x) * 0.95f + pRoof.y * 1.05f) - size * 0.44f;
                    float dDoor = SdfRoundedBox(p - (c + new Vector2(0f, -size * 0.19f)), new Vector2(size * 0.11f, size * 0.15f), 4f);
                    float dHouse = Mathf.Min(dBody, dRoof);

                    if (dHouse > 0.8f)
                    {
                        px[y * size + x] = Color.clear;
                        continue;
                    }

                    float alpha = Mathf.Clamp01(0.7f - dHouse);
                    if (dHouse > -strokeW || dDoor < 0f)
                    {
                        px[y * size + x] = WithAlpha(ColInk, alpha);
                    }
                    else
                    {
                        px[y * size + x] = WithAlpha((pRoof.y > 0f) ? ColSky : Color.white, alpha);
                    }
                }
            }
            return FinalizeIcon(tex, px, size);
        }

        /// <summary>
        /// 15b. Heart: Tactile pastel-coral heart icon for lives / health.
        /// </summary>
        internal static Sprite CreateHeartIcon(int size)
        {
            Texture2D tex = NewTex(size, size);
            Color[] px = new Color[size * size];
            Vector2 c = new Vector2(size * 0.5f, size * 0.54f);
            float r = size * 0.22f;
            Vector2 c1 = c + new Vector2(-r * 0.88f, r * 0.25f);
            Vector2 c2 = c + new Vector2(r * 0.88f, r * 0.25f);
            Vector2 tip = c + new Vector2(0f, -size * 0.38f);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    Vector2 p = new Vector2(x + 0.5f, y + 0.5f);
                    float dCircles = Mathf.Min(Vector2.Distance(p, c1) - r, Vector2.Distance(p, c2) - r);
                    float dy = p.y - tip.y;
                    float dx = Mathf.Abs(p.x - tip.x);
                    bool inCone = (p.y <= c.y + r * 0.25f) && (dx <= dy * 0.95f);

                    if (dCircles > 0f && !inCone)
                    {
                        px[y * size + x] = Color.clear;
                        continue;
                    }

                    if (p.y < c.y - size * 0.22f)
                        px[y * size + x] = ColCoralEdge;
                    else if (p.y > c.y + r * 0.2f && p.x < c.x)
                        px[y * size + x] = Color.Lerp(ColCoral, Color.white, 0.35f);
                    else
                        px[y * size + x] = ColCoral;
                }
            }
            return FinalizeIcon(tex, px, size);
        }

        internal static Sprite CreateSwapArrowsIcon(int size)
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
                    float r = p.magnitude;
                    float angle = Mathf.Atan2(p.y, p.x) * Mathf.Rad2Deg;

                    float ringDist = Mathf.Abs(r - size * 0.33f) - strokeW;
                    float dArcTop = (angle > 18f && angle < 162f) ? ringDist : 999f;
                    float dArcBot = (angle < -18f && angle > -162f) ? ringDist : 999f;

                    Vector2 tip1 = new Vector2(size * 0.33f, 0f);
                    float dHead1 = DistanceToSegment(p, tip1, tip1 + new Vector2(-size * 0.12f, size * 0.14f)) - strokeW * 0.8f;
                    float dHead2 = DistanceToSegment(p, tip1, tip1 + new Vector2(size * 0.12f, size * 0.14f)) - strokeW * 0.8f;
                    float dArrow1 = Mathf.Min(dArcTop, Mathf.Min(dHead1, dHead2));

                    Vector2 tip2 = new Vector2(-size * 0.33f, 0f);
                    float dHead3 = DistanceToSegment(p, tip2, tip2 + new Vector2(-size * 0.12f, -size * 0.14f)) - strokeW * 0.8f;
                    float dHead4 = DistanceToSegment(p, tip2, tip2 + new Vector2(size * 0.12f, -size * 0.14f)) - strokeW * 0.8f;
                    float dArrow2 = Mathf.Min(dArcBot, Mathf.Min(dHead3, dHead4));

                    float d = Mathf.Min(dArrow1, dArrow2);
                    if (d > 0.8f)
                    {
                        px[y * size + x] = Color.clear;
                        continue;
                    }

                    float alpha = Mathf.Clamp01(0.7f - d);
                    Color col = (d > -strokeW * 0.45f) ? ColInk : ((dArrow1 < dArrow2) ? Color.white : ColSun);
                    px[y * size + x] = WithAlpha(col, alpha);
                }
            }
            return FinalizeIcon(tex, px, size);
        }

        internal static Sprite CreateLightningBoltIcon(int size)
        {
            Texture2D tex = NewTex(size, size);
            Color[] px = new Color[size * size];
            Vector2 c = new Vector2(size * 0.5f, size * 0.5f);
            float strokeW = size * 0.095f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    Vector2 p = new Vector2(x + 0.5f, y + 0.5f) - c;
                    float d1 = DistanceToSegment(p, new Vector2(size * 0.12f, size * 0.44f), new Vector2(-size * 0.12f, 0f)) - strokeW;
                    float d2 = DistanceToSegment(p, new Vector2(-size * 0.12f, 0f), new Vector2(size * 0.12f, 0f)) - strokeW;
                    float d3 = DistanceToSegment(p, new Vector2(size * 0.12f, 0f), new Vector2(-size * 0.10f, -size * 0.44f)) - strokeW;
                    float d = Mathf.Min(d1, Mathf.Min(d2, d3));

                    if (d > 0.8f)
                    {
                        px[y * size + x] = Color.clear;
                        continue;
                    }

                    float alpha = Mathf.Clamp01(0.7f - d);
                    px[y * size + x] = WithAlpha(ColSun, alpha);
                }
            }
            return FinalizeIcon(tex, px, size);
        }

        internal static Sprite CreateRockObstacleIcon(int size)
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
                    float d = SdfRoundedBox(p, new Vector2(size * 0.38f, size * 0.36f), size * 0.16f);
                    if (d > 0.8f)
                    {
                        px[y * size + x] = Color.clear;
                        continue;
                    }

                    float alpha = Mathf.Clamp01(0.7f - d);
                    px[y * size + x] = WithAlpha((d > -strokeW) ? ColInk : ColTileEdge, alpha);
                }
            }
            return FinalizeIcon(tex, px, size);
        }
    }
}

