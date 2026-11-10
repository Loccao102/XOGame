using System;
using System.Collections.Generic;
using UnityEngine;

namespace XOPlayable.Visuals
{
    /// <summary>
    /// Soft, calm, tactile "Pastel Clay" procedural vector/SDF Sprite & UI generator + Audio Synthesizer.
    /// Hand-crafted vector geometry: uniform 2.4-stroke equivalent, round caps, round joins,
    /// soft pastel clay palette, zero AI-slop, zero harsh dark outlines, zero external font/emoji dependencies.
    /// </summary>
    public static class XOProceduralAssets
    {
        private static readonly Dictionary<string, Sprite> _spriteCache = new Dictionary<string, Sprite>();
        private static readonly Dictionary<string, AudioClip> _audioClips = new Dictionary<string, AudioClip>();

        // =====================================================================
        // PASTEL CLAY PALETTE (Matches exact CSS variables)
        // =====================================================================
        public static readonly Color ColInk        = Hex("#2A3566");
        public static readonly Color ColMuted      = Hex("#7380AE");
        public static readonly Color ColMint       = Hex("#4ED6A4");
        public static readonly Color ColMintEdge   = Hex("#2EA375");
        public static readonly Color ColSky        = Hex("#4FB0FF");
        public static readonly Color ColSkyEdge    = Hex("#258FDE");
        public static readonly Color ColLilac      = Hex("#A998FF");
        public static readonly Color ColLilacEdge  = Hex("#8570E3");
        public static readonly Color ColPeach      = Hex("#FFA96B");
        public static readonly Color ColPeachEdge  = Hex("#DE8240");
        public static readonly Color ColSun        = Hex("#FFCF5C");
        public static readonly Color ColSunEdge    = Hex("#DBA02E");
        public static readonly Color ColCoral      = Hex("#FF6F85");
        public static readonly Color ColCoralEdge  = Hex("#E04B62");
        public static readonly Color ColTileEdge   = Hex("#D6DEEF");
        public static readonly Color ColCardBg     = new Color(1f, 1f, 1f, 0.92f);

        // =====================================================================
        // TACTILE CLAY 3D UI FRAMES, BUTTONS & BOARD TILES
        // =====================================================================

        public static Sprite RoundedRectSprite => GetOrBuild("ui_rounded_rect", () => CreateRoundedRectSprite(64, 64, 18f, 18));
        public static Sprite SoftCellSprite    => GetOrBuild("ui_soft_cell", () => CreateBeveledCellSprite(96));
        public static Sprite PillSprite        => GetOrBuild("ui_pill", () => CreateRoundedRectSprite(64, 64, 28f, 28));
        public static Sprite CircleSprite      => GetOrBuild("ui_circle", () => CreateCircleSprite(64));
        public static Sprite HintTargetSprite  => GetOrBuild("ui_hint_target", () => CreateHintTargetSprite(128));

        public static Sprite CardPanelSprite   => GetOrBuild("ui_card_panel", () => CreateBeveledCardSprite(128, 128, 28f, 32));
        public static Sprite RecessedSlotSprite => GetOrBuild("ui_recessed_slot", () => CreateRecessedSlotSprite(96, 96, 22f, 24));
        public static Sprite Button3DSprite    => GetOrBuild("ui_btn_3d", () => CreateTactile3DButtonSprite(128, 128, 26f, 32));
        public static Sprite Pill3DSprite      => GetOrBuild("ui_pill_3d", () => CreateTactile3DButtonSprite(128, 80, 36f, 36));

        // =====================================================================
        // ILLUSTRATED PASTEL VECTOR ICONS (CONSISTENT OPTICAL WEIGHT & SOFT CORNERS)
        // =====================================================================

        public static Sprite XPieceSprite => GetOrBuild("piece_x_neon", () => CreateXPieceSprite(128));
        public static Sprite OPieceSprite => GetOrBuild("piece_o_neon", () => CreateOPieceSprite(128));

        public static Sprite IconCoin       => GetOrBuild("icon_coin", () => CreateCoinIcon(128));
        public static Sprite IconChest      => GetOrBuild("icon_chest", () => CreateChestIcon(128));
        public static Sprite IconHint       => GetOrBuild("icon_hint", () => CreateHintBulbIcon(128));
        public static Sprite IconUndo       => GetOrBuild("icon_undo", () => CreateUndoArrowIcon(128));
        public static Sprite IconBomb       => GetOrBuild("icon_bomb", () => CreateBombIcon(128));
        public static Sprite IconFreeze     => GetOrBuild("icon_freeze", () => CreateFreezeSnowflakeIcon(128));
        public static Sprite IconSkinShop   => GetOrBuild("icon_skin", () => CreatePaletteShopIcon(128));
        public static Sprite IconVideoAd    => GetOrBuild("icon_video_ad", () => CreateVideoAdClapperIcon(128));
        public static Sprite IconTrophy     => GetOrBuild("icon_trophy", () => CreateTrophyIcon(128));
        public static Sprite IconCrown      => GetOrBuild("icon_crown", () => CreateCrownIcon(128));
        public static Sprite IconFlame      => GetOrBuild("icon_flame", () => CreateFlameIcon(128));
        public static Sprite IconSwords     => GetOrBuild("icon_swords", () => CreateCrossedSwordsIcon(128));
        public static Sprite IconShield     => GetOrBuild("icon_shield", () => CreateShieldIcon(128));
        public static Sprite IconSoundOn    => GetOrBuild("icon_sound_on", () => CreateSoundIcon(128, false));
        public static Sprite IconSoundOff   => GetOrBuild("icon_sound_off", () => CreateSoundIcon(128, true));
        public static Sprite IconNewGame    => GetOrBuild("icon_new_game", () => CreatePlayStarIcon(128));
        public static Sprite IconSwap       => GetOrBuild("icon_swap", () => CreateSwapArrowsIcon(128));
        public static Sprite IconLightning  => GetOrBuild("icon_lightning", () => CreateLightningBoltIcon(128));
        public static Sprite IconRock       => GetOrBuild("icon_rock", () => CreateRockObstacleIcon(128));
        public static Sprite IconLeaderboard => GetOrBuild("icon_leaderboard", () => CreateLeaderboardPodiumIcon(128));
        public static Sprite IconLuckyWheel => GetOrBuild("icon_lucky_wheel", () => CreateLuckyWheelIcon(128));
        public static Sprite IconHome       => GetOrBuild("icon_home", () => CreateHomeIcon(128));
        public static Sprite IconHeart      => GetOrBuild("icon_heart", () => CreateHeartIcon(128));
        public static Sprite IconRelic      => GetOrBuild("icon_relic", () => CreateRelicJokerIcon(128));

        public static Sprite GetPieceSkinSprite(string skinId, bool isX)
        {
            return skinId switch
            {
                "fire_ice"     => isX ? IconFlame : IconFreeze,
                "sword_shield" => isX ? IconSwords : IconShield,
                "king_dragon"  => isX ? IconCrown : GetOrBuild("piece_ruby_gem", () => CreateRubyGemIcon(128)),
                "star_moon"    => isX ? IconNewGame : GetOrBuild("piece_crescent_moon", () => CreateCrescentMoonIcon(128)),
                _              => isX ? XPieceSprite : OPieceSprite
            };
        }

        private static Sprite GetOrBuild(string key, Func<Sprite> builder)
        {
            if (_spriteCache.TryGetValue(key, out Sprite s) && s != null)
                return s;

            Sprite resSprite = Resources.Load<Sprite>("XOUI/" + key);
            if (resSprite != null)
            {
                _spriteCache[key] = resSprite;
                return resSprite;
            }

            s = builder();
            s.name = key;
            _spriteCache[key] = s;
            return s;
        }

        // =====================================================================
        // TACTILE CLAY BUTTONS, CARDS & TILES
        // =====================================================================

        /// <summary>
        /// Chunky rounded button with solid darker bottom edge (box-shadow 0 7px 0 darker shade)
        /// and soft inner top highlight. Pure white-tinted face so Image.color tints it cleanly.
        /// </summary>
        private static Sprite CreateTactile3DButtonSprite(int width, int height, float radius, int slice)
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

                    // Top face shifted up by lipHeight * 0.5f
                    Vector2 pFace = new Vector2(p.x, p.y - lipHeight * 0.5f);
                    Vector2 halfFace = new Vector2(half.x - 2.5f, half.y - 2.5f - lipHeight * 0.5f);
                    float dFace = SdfRoundedBox(pFace, halfFace, Mathf.Max(4f, radius - 2f));

                    if (dFace <= 0f)
                    {
                        // Inside top button face (smooth soft gradient with inner top highlight)
                        float normY = Mathf.Clamp01((pFace.y + halfFace.y) / (halfFace.y * 2f));
                        float grad = Mathf.Lerp(0.94f, 1.04f, normY);

                        // Subtle inner top highlight rim
                        if (normY > 0.82f && dFace > -4f)
                        {
                            grad = 1.15f;
                        }
                        px[y * width + x] = new Color(Mathf.Clamp01(grad), Mathf.Clamp01(grad), Mathf.Clamp01(grad), outerAlpha);
                    }
                    else
                    {
                        // Solid darker bottom 3D lip (shaded ~0.72 so color tint creates darker sibling edge)
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
        private static Sprite CreateBeveledCardSprite(int width, int height, float radius, int slice)
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

                    // Soft subtle rim highlight at top
                    float body = Mathf.Lerp(0.96f, 1.02f, normY);
                    if (d > -2.2f && normY > 0.75f)
                    {
                        body = 1.08f;
                    }

                    px[y * width + x] = new Color(body, body, body, alpha);
                }
            }

            tex.SetPixels(px);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(slice, slice, slice, slice));
        }

        private static Sprite CreateRecessedSlotSprite(int width, int height, float radius, int slice)
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
        private static Sprite CreateBeveledCellSprite(int size)
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

                    // Soft subtle bevel
                    float shade = Mathf.Lerp(0.95f, 1.03f, normY);
                    if (d > -2.0f && normY > 0.7f)
                    {
                        shade = 1.08f;
                    }

                    px[y * size + x] = new Color(shade, shade, shade, alpha);
                }
            }

            tex.SetPixels(px);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(16, 16, 16, 16));
        }

        private static Sprite CreateRoundedRectSprite(int width, int height, float radius, int borderSlice)
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

        private static Sprite CreateCircleSprite(int size)
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

        // =====================================================================
        // =====================================================================
        // SOFT PASTEL VECTOR ICONS (HAND-CRAFTED 2.4-STROKE EQUIVALENT SDF GEOMETRY)
        // =====================================================================
        // Optically maximized geometry: 85-92% bounding box fill, bold 2.4-stroke (strokeW ~ 0.085-0.095f),
        // smooth rounded corners and clean pastel clay fills.

        /// <summary>
        /// 1. Coin: Flat filled circle with thin inner concentric ring (no $ character, no messy gradients).
        /// </summary>
        private static Sprite CreateCoinIcon(int size)
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

                    // Outer stroke border
                    if (dOuter > -strokeW)
                    {
                        px[y * size + x] = WithAlpha(ColInk, alpha);
                        continue;
                    }

                    // Inner concentric ring
                    float dInnerRing = Mathf.Abs(dist - rInner) - (strokeW * 0.45f);
                    if (dInnerRing < 0f)
                    {
                        px[y * size + x] = WithAlpha(ColInk, alpha);
                        continue;
                    }

                    // Flat warm sun fill
                    px[y * size + x] = WithAlpha(ColSun, alpha);
                }
            }
            return FinalizeIcon(tex, px, size);
        }

        /// <summary>
        /// 2. Streak Flame: Smooth continuous teardrop flame with soft inner flick/tongue.
        /// </summary>
        private static Sprite CreateFlameIcon(int size)
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
                    // Teardrop outer contour
                    float dBottom = Vector2.Distance(p, c) - size * 0.36f;
                    float dTip = DistanceToSegment(p, c, c + new Vector2(0f, size * 0.48f)) - Mathf.Lerp(size * 0.36f, size * 0.05f, Mathf.Clamp01((p.y - c.y) / (size * 0.50f)));
                    float dFlame = Mathf.Min(dBottom, dTip);

                    if (dFlame > 0.8f)
                    {
                        px[y * size + x] = Color.clear;
                        continue;
                    }

                    float alpha = Mathf.Clamp01(0.7f - dFlame);

                    // Outer stroke
                    if (dFlame > -strokeW)
                    {
                        px[y * size + x] = WithAlpha(ColInk, alpha);
                        continue;
                    }

                    // Inner gentle tongue
                    float dTongue = Vector2.Distance(p, c + new Vector2(0f, -size * 0.03f)) - size * 0.17f;
                    if (dTongue < 0f)
                    {
                        px[y * size + x] = WithAlpha(ColSun, alpha);
                        continue;
                    }

                    // Peach flame body
                    px[y * size + x] = WithAlpha(ColPeach, alpha);
                }
            }
            return FinalizeIcon(tex, px, size);
        }

        /// <summary>
        /// 3. Sound on/off: Rounded speaker horn with smooth concentric waves (on) or clean diagonal slash (off).
        /// </summary>
        private static Sprite CreateSoundIcon(int size, bool muted)
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

                    if (dSpeaker > -strokeW)
                    {
                        px[y * size + x] = WithAlpha(ColInk, alpha);
                    }
                    else
                    {
                        px[y * size + x] = WithAlpha(ColSky, alpha);
                    }
                }
            }
            return FinalizeIcon(tex, px, size);
        }

        /// <summary>
        /// 4. Play: Centered equilateral triangle with rounded vertices pointing right, flat filled.
        /// </summary>
        private static Sprite CreatePlayStarIcon(int size)
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

                    // Point-in-triangle check
                    bool inside = (p.x >= v1.x) &&
                                  ((p.y - v1.y) * (v3.x - v1.x) - (p.x - v1.x) * (v3.y - v1.y) >= 0) &&
                                  ((p.y - v2.y) * (v3.x - v2.x) - (p.x - v2.x) * (v3.y - v2.y) <= 0);

                    float d = inside ? -edgeDist : edgeDist;
                    d -= (size * 0.07f); // Rounded corners

                    if (d > 0.8f)
                    {
                        px[y * size + x] = Color.clear;
                        continue;
                    }

                    float alpha = Mathf.Clamp01(0.7f - d);
                    if (d > -strokeW)
                    {
                        px[y * size + x] = WithAlpha(ColInk, alpha);
                    }
                    else
                    {
                        px[y * size + x] = WithAlpha(Color.white, alpha);
                    }
                }
            }
            return FinalizeIcon(tex, px, size);
        }

        /// <summary>
        /// 5. Rivals: Three stepped rounded vertical bars (center tall, left mid, right short) on a baseline.
        /// </summary>
        private static Sprite CreateLeaderboardPodiumIcon(int size)
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

                    // Baseline
                    float dBase = DistanceToSegment(p, new Vector2(-size * 0.44f, -size * 0.36f), new Vector2(size * 0.44f, -size * 0.36f)) - (strokeW * 0.5f);

                    // Center 1st bar (tall)
                    float dCenter = SdfRoundedBox(p - new Vector2(0f, -size * 0.02f), new Vector2(size * 0.13f, size * 0.34f), 6f);
                    // Left 2nd bar (mid)
                    float dLeft = SdfRoundedBox(p - new Vector2(-size * 0.28f, -size * 0.10f), new Vector2(size * 0.12f, size * 0.24f), 6f);
                    // Right 3rd bar (short)
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
                        if (dBars > -strokeW * 0.8f)
                        {
                            px[y * size + x] = WithAlpha(ColInk, alpha);
                        }
                        else if (dCenter <= 0f)
                        {
                            px[y * size + x] = WithAlpha(ColSun, alpha);
                        }
                        else if (dLeft <= 0f)
                        {
                            px[y * size + x] = WithAlpha(ColSky, alpha);
                        }
                        else
                        {
                            px[y * size + x] = WithAlpha(ColPeach, alpha);
                        }
                        continue;
                    }

                    px[y * size + x] = WithAlpha(ColInk, alpha);
                }
            }
            return FinalizeIcon(tex, px, size);
        }

        /// <summary>
        /// 6. Spin wheel: Outer circle with center hub, 4 orthogonal spoke lines and top indicator pointer.
        /// </summary>
        private static Sprite CreateLuckyWheelIcon(int size)
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

                    // Top indicator pointer
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

                    // 4 orthogonal spoke lines
                    float dSpokeH = Mathf.Abs(p.y) - (strokeW * 0.5f);
                    float dSpokeV = Mathf.Abs(p.x) - (strokeW * 0.5f);
                    if (dSpokeH < 0f || dSpokeV < 0f)
                    {
                        px[y * size + x] = WithAlpha(ColInk, alpha);
                        continue;
                    }

                    // 4 pastel quadrant slices
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
        private static Sprite CreatePaletteShopIcon(int size)
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

                    // Outer stroke
                    if (d > -strokeW || dThumb < strokeW * 0.8f)
                    {
                        px[y * size + x] = WithAlpha(ColInk, alpha);
                        continue;
                    }

                    // 3 pigment wells
                    if (Vector2.Distance(p, dot1) < dotR)
                    {
                        px[y * size + x] = WithAlpha(ColMint, alpha);
                        continue;
                    }
                    if (Vector2.Distance(p, dot2) < dotR)
                    {
                        px[y * size + x] = WithAlpha(ColSun, alpha);
                        continue;
                    }
                    if (Vector2.Distance(p, dot3) < dotR)
                    {
                        px[y * size + x] = WithAlpha(ColCoral, alpha);
                        continue;
                    }

                    // Smooth soft lilac body
                    px[y * size + x] = WithAlpha(ColLilac, alpha);
                }
            }
            return FinalizeIcon(tex, px, size);
        }

        /// <summary>
        /// 8. Chest: Rounded treasure chest with arched lid, tub base, dividing seam, center lock latch.
        /// </summary>
        private static Sprite CreateChestIcon(int size)
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

                    // Arched lid & rectangular tub
                    float dLid = SdfRoundedBox(p - new Vector2(0f, size * 0.12f), new Vector2(size * 0.42f, size * 0.20f), 14f);
                    float dTub = SdfRoundedBox(p - new Vector2(0f, -size * 0.15f), new Vector2(size * 0.42f, size * 0.20f), 10f);
                    float d = Mathf.Min(dLid, dTub);

                    if (d > 0.8f)
                    {
                        px[y * size + x] = Color.clear;
                        continue;
                    }

                    float alpha = Mathf.Clamp01(0.7f - d);

                    // Outer stroke
                    if (d > -strokeW)
                    {
                        px[y * size + x] = WithAlpha(ColInk, alpha);
                        continue;
                    }

                    // Center lock latch
                    float dLock = SdfRoundedBox(p - new Vector2(0f, -size * 0.01f), new Vector2(size * 0.09f, size * 0.10f), 4f);
                    if (dLock < 0f)
                    {
                        px[y * size + x] = WithAlpha(ColSun, alpha);
                        continue;
                    }

                    // Horizontal dividing seam
                    if (Mathf.Abs(p.y - size * 0.02f) < strokeW * 0.5f)
                    {
                        px[y * size + x] = WithAlpha(ColInk, alpha);
                        continue;
                    }

                    // Warm peach/sun chest body
                    px[y * size + x] = WithAlpha((p.y > 0f) ? ColSun : ColPeach, alpha);
                }
            }
            return FinalizeIcon(tex, px, size);
        }

        /// <summary>
        /// 9. X Game Token: Clean rounded crossing strokes in pastel clay style.
        /// </summary>
        private static Sprite CreateXPieceSprite(int size)
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

                    // Soft clay shading: subtle top inner highlight
                    float shade = (d < -thickness * 0.45f) ? 1.08f : 0.96f;
                    Color col = new Color(ColSky.r * shade, ColSky.g * shade, ColSky.b * shade, alpha);
                    pixels[y * size + x] = col;
                }
            }
            return FinalizeIcon(tex, pixels, size);
        }

        /// <summary>
        /// 10. O Game Token: Clean rounded circular ring in pastel clay style.
        /// </summary>
        private static Sprite CreateOPieceSprite(int size)
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

                    // Soft clay shading
                    float shade = (d < -thickness * 0.45f) ? 1.08f : 0.96f;
                    Color col = new Color(ColCoral.r * shade, ColCoral.g * shade, ColCoral.b * shade, alpha);
                    pixels[y * size + x] = col;
                }
            }
            return FinalizeIcon(tex, pixels, size);
        }

        /// <summary>
        /// 11. Hint: Soft rounded lightbulb globe with screw base.
        /// </summary>
        private static Sprite CreateHintBulbIcon(int size)
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

                    if (p.y < size * 0.20f)
                    {
                        px[y * size + x] = WithAlpha(ColTileEdge, alpha);
                    }
                    else
                    {
                        px[y * size + x] = WithAlpha(new Color(1f, 0.98f, 0.90f), alpha);
                    }
                }
            }
            return FinalizeIcon(tex, px, size);
        }

        /// <summary>
        /// 12. Undo: Smooth circular 270-deg arrow arc with rounded arrowhead.
        /// </summary>
        private static Sprite CreateUndoArrowIcon(int size)
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

                    float dRing = Mathf.Abs(dist - size * 0.33f) - (strokeW * 1.0f);
                    if (angle > 135f || angle < -145f) dRing = 999f;

                    // Arrowhead
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
                    if (d > -strokeW * 0.45f)
                    {
                        px[y * size + x] = WithAlpha(ColInk, alpha);
                    }
                    else
                    {
                        px[y * size + x] = WithAlpha(Color.white, alpha);
                    }
                }
            }
            return FinalizeIcon(tex, px, size);
        }

        /// <summary>
        /// 13. Bomb: Clean smooth sphere, curved fuse, coral 4-point spark.
        /// </summary>
        private static Sprite CreateBombIcon(int size)
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
                    if (dHighlight <= 0f)
                    {
                        px[y * size + x] = WithAlpha(Color.white, alpha);
                    }
                    else
                    {
                        px[y * size + x] = WithAlpha(ColInk, alpha);
                    }
                }
            }
            return FinalizeIcon(tex, px, size);
        }

        /// <summary>
        /// 14. Freeze: Clean 6-arm snowflake with round ends.
        /// </summary>
        private static Sprite CreateFreezeSnowflakeIcon(int size)
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
                    if (d > -strokeW * 0.45f)
                    {
                        px[y * size + x] = WithAlpha(ColInk, alpha);
                    }
                    else
                    {
                        px[y * size + x] = WithAlpha(Color.white, alpha);
                    }
                }
            }
            return FinalizeIcon(tex, px, size);
        }

        /// <summary>
        /// 15. Home: Clean rounded gable roof, house base, and arched center door.
        /// </summary>
        private static Sprite CreateHomeIcon(int size)
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

                    if (dHouse > -strokeW)
                    {
                        px[y * size + x] = WithAlpha(ColInk, alpha);
                        continue;
                    }

                    if (dDoor < 0f)
                    {
                        px[y * size + x] = WithAlpha(ColInk, alpha);
                    }
                    else if (pRoof.y > 0f)
                    {
                        px[y * size + x] = WithAlpha(ColSky, alpha);
                    }
                    else
                    {
                        px[y * size + x] = WithAlpha(Color.white, alpha);
                    }
                }
            }
            return FinalizeIcon(tex, px, size);
        }


        /// <summary>
        /// 15b. Heart: Tactile pastel-coral heart icon for lives / health.
        /// </summary>
        private static Sprite CreateHeartIcon(int size)
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
                    float d1 = Vector2.Distance(p, c1) - r;
                    float d2 = Vector2.Distance(p, c2) - r;
                    float dCircles = Mathf.Min(d1, d2);

                    float dy = p.y - tip.y;
                    float dx = Mathf.Abs(p.x - tip.x);
                    bool inCone = (p.y <= c.y + r * 0.25f) && (dx <= dy * 0.95f);
                    bool inside = (dCircles <= 0f) || inCone;

                    if (!inside)
                    {
                        px[y * size + x] = Color.clear;
                        continue;
                    }

                    if (p.y < c.y - size * 0.22f)
                    {
                        px[y * size + x] = ColCoralEdge;
                    }
                    else if (p.y > c.y + r * 0.2f && p.x < c.x)
                    {
                        px[y * size + x] = Color.Lerp(ColCoral, Color.white, 0.35f);
                    }
                    else
                    {
                        px[y * size + x] = ColCoral;
                    }
                }
            }
            return FinalizeIcon(tex, px, size);
        }


        /// <summary>
        /// 15c. Relic / Joker: Shiny gemstone/card talisman for roguelike relics.
        /// </summary>
        private static Sprite CreateRelicJokerIcon(int size)
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

                    if (p.y > 0f && p.x < 0f)
                    {
                        px[y * size + x] = WithAlpha(ColSun, alpha);
                    }
                    else if (p.y > 0f)
                    {
                        px[y * size + x] = WithAlpha(ColMint, alpha);
                    }
                    else if (p.x < 0f)
                    {
                        px[y * size + x] = WithAlpha(ColSky, alpha);
                    }
                    else
                    {
                        px[y * size + x] = WithAlpha(ColLilac, alpha);
                    }
                }
            }
            return FinalizeIcon(tex, px, size);
        }

        /// <summary>
        /// 16. Trophy: Clean golden chalice cup on a pedestal base.
        /// </summary>
        private static Sprite CreateTrophyIcon(int size)
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
                    if (d > -strokeW)
                    {
                        px[y * size + x] = WithAlpha(ColInk, alpha);
                    }
                    else
                    {
                        px[y * size + x] = WithAlpha(ColSun, alpha);
                    }
                }
            }
            return FinalizeIcon(tex, px, size);
        }

        /// <summary>
        /// 17. Crown: Clean 3-point royal crown with pearl tips.
        /// </summary>
        private static Sprite CreateCrownIcon(int size)
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
                    if (d > -strokeW)
                    {
                        px[y * size + x] = WithAlpha(ColInk, alpha);
                    }
                    else
                    {
                        px[y * size + x] = WithAlpha(ColSun, alpha);
                    }
                }
            }
            return FinalizeIcon(tex, px, size);
        }

        private static Sprite CreateVideoAdClapperIcon(int size)
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

                    // Play triangle in center
                    float tx = p.x + size * 0.08f;
                    float ty = Mathf.Abs(p.y);
                    if (tx > 0f && tx < size * 0.25f && ty < (size * 0.25f - tx) * 0.68f)
                    {
                        px[y * size + x] = WithAlpha(ColInk, alpha);
                        continue;
                    }

                    px[y * size + x] = WithAlpha(ColSun, alpha);
                }
            }
            return FinalizeIcon(tex, px, size);
        }

        private static Sprite CreateCrossedSwordsIcon(int size)
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
                    float d1 = DistanceToSegment(p, s1A, s1B) - strokeW;
                    float d2 = DistanceToSegment(p, s2A, s2B) - strokeW;
                    float d = Mathf.Min(d1, d2);

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

        private static Sprite CreateShieldIcon(int size)
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
                    if (d > -strokeW)
                    {
                        px[y * size + x] = WithAlpha(ColInk, alpha);
                        continue;
                    }
                    else
                    {
                        px[y * size + x] = WithAlpha(ColLilac, alpha);
                    }
                }
            }
            return FinalizeIcon(tex, px, size);
        }

        private static Sprite CreateRubyGemIcon(int size)
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
                    if (d > -strokeW)
                    {
                        px[y * size + x] = WithAlpha(ColInk, alpha);
                    }
                    else
                    {
                        px[y * size + x] = WithAlpha(ColCoral, alpha);
                    }
                }
            }
            return FinalizeIcon(tex, px, size);
        }

        private static Sprite CreateCrescentMoonIcon(int size)
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
                    if (d > -strokeW)
                    {
                        px[y * size + x] = WithAlpha(ColInk, alpha);
                    }
                    else
                    {
                        px[y * size + x] = WithAlpha(ColLilac, alpha);
                    }
                }
            }
            return FinalizeIcon(tex, px, size);
        }

        private static Sprite CreateHintTargetSprite(int size)
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

        private static Sprite CreateSwapArrowsIcon(int size)
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
                    bool topArc = (angle > 18f && angle < 162f);
                    bool botArc = (angle < -18f && angle > -162f);
                    float dArcTop = topArc ? ringDist : 999f;
                    float dArcBot = botArc ? ringDist : 999f;

                    // Arrowhead 1: at top right (pointing right/down)
                    Vector2 tip1 = new Vector2(size * 0.33f, 0f);
                    float dHead1 = DistanceToSegment(p, tip1, tip1 + new Vector2(-size * 0.12f, size * 0.14f)) - strokeW * 0.8f;
                    float dHead2 = DistanceToSegment(p, tip1, tip1 + new Vector2(size * 0.12f, size * 0.14f)) - strokeW * 0.8f;
                    float dArrow1 = Mathf.Min(dArcTop, Mathf.Min(dHead1, dHead2));

                    // Arrowhead 2: at bottom left (pointing left/up)
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
                    if (d > -strokeW * 0.45f)
                    {
                        px[y * size + x] = WithAlpha(ColInk, alpha);
                    }
                    else
                    {
                        Color col = (dArrow1 < dArrow2) ? Color.white : ColSun;
                        px[y * size + x] = WithAlpha(col, alpha);
                    }
                }
            }
            return FinalizeIcon(tex, px, size);
        }

        private static Sprite CreateLightningBoltIcon(int size)
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

        private static Sprite CreateRockObstacleIcon(int size)
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
                    if (d > -strokeW)
                    {
                        px[y * size + x] = WithAlpha(ColInk, alpha);
                    }
                    else
                    {
                        px[y * size + x] = WithAlpha(ColTileEdge, alpha);
                    }
                }
            }
            return FinalizeIcon(tex, px, size);
        }

        // =====================================================================
        // MATH & TEXTURE HELPERS
        // =====================================================================

        private static Texture2D NewTex(int w, int h)
        {
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.filterMode = FilterMode.Bilinear;
            return tex;
        }

        private static Sprite FinalizeIcon(Texture2D tex, Color[] px, int size)
        {
            tex.SetPixels(px);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
        }

        private static float SdfRoundedBox(Vector2 p, Vector2 b, float r)
        {
            Vector2 q = new Vector2(Mathf.Abs(p.x), Mathf.Abs(p.y)) - (b - new Vector2(r, r));
            return Vector2.Max(q, Vector2.zero).magnitude + Mathf.Min(Mathf.Max(q.x, q.y), 0f) - r;
        }

        private static float DistanceToSegment(Vector2 p, Vector2 a, Vector2 b)
        {
            Vector2 pa = p - a;
            Vector2 ba = b - a;
            float h = Mathf.Clamp01(Vector2.Dot(pa, ba) / Vector2.Dot(ba, ba));
            return (pa - ba * h).magnitude;
        }

        public static Color Hex(string hex)
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

        public static Color WithAlpha(Color c, float a)
        {
            c.a = a;
            return c;
        }

        // =====================================================================
        // PROCEDURAL AUDIO SYNTHESIZER
        // =====================================================================

        public static AudioClip GetSound(string key)
        {
            if (_audioClips.TryGetValue(key, out AudioClip clip) && clip != null)
                return clip;

            clip = key switch
            {
                "place_x" => CreateToneSequence("place_x", new float[] { 520f, 680f }, 0.075f, 0.25f),
                "place_o" => CreateToneSequence("place_o", new float[] { 440f, 560f }, 0.075f, 0.25f),
                "click"   => CreateToneSequence("click", new float[] { 600f }, 0.04f, 0.18f),
                "undo"    => CreateToneSequence("undo", new float[] { 587f, 440f }, 0.09f, 0.25f),
                "hint"    => CreateToneSequence("hint", new float[] { 523.25f, 659.25f, 783.99f, 1046.5f }, 0.08f, 0.28f),
                "win"     => CreateToneSequence("win", new float[] { 523.25f, 659.25f, 783.99f, 1046.5f, 1318.5f }, 0.11f, 0.32f),
                "lose"    => CreateToneSequence("lose", new float[] { 392f, 349.23f, 311.13f }, 0.14f, 0.28f),
                "reward"  => CreateToneSequence("reward", new float[] { 587.33f, 783.99f, 987.77f, 1174.66f }, 0.09f, 0.30f),
                "bomb"      => CreateToneSequence("bomb", new float[] { 240f, 160f, 95f }, 0.09f, 0.38f),
                "freeze"    => CreateToneSequence("freeze", new float[] { 880f, 1174.66f, 1567.98f }, 0.07f, 0.28f),
                "swap"      => CreateToneSequence("swap", new float[] { 440f, 659.25f, 880f, 1174.66f }, 0.065f, 0.30f),
                "lightning" => CreateToneSequence("lightning", new float[] { 783.99f, 1046.5f, 1567.98f }, 0.055f, 0.32f),
                "callout"   => CreateToneSequence("callout", new float[] { 659.25f, 880f, 1046.5f }, 0.07f, 0.28f),
                "bot_chat"  => CreateToneSequence("bot_chat", new float[] { 480f, 620f }, 0.045f, 0.20f),
                "chest"     => CreateToneSequence("chest", new float[] { 523.25f, 659.25f, 783.99f, 1046.5f, 1318.5f, 1567.98f }, 0.09f, 0.34f),
                "gobble"    => CreateToneSequence("gobble", new float[] { 349.23f, 523.25f, 698.46f }, 0.065f, 0.32f),
                "spin_tick" => CreateToneSequence("spin_tick", new float[] { 820f }, 0.028f, 0.22f),
                "jackpot"   => CreateToneSequence("jackpot", new float[] { 523.25f, 659.25f, 783.99f, 1046.5f, 1318.5f, 1567.98f, 2093f }, 0.095f, 0.35f),
                _         => CreateToneSequence("default", new float[] { 500f }, 0.05f, 0.2f)
            };

            _audioClips[key] = clip;
            return clip;
        }

        /// <summary>
        /// Generates a seamless 6.4-second chill Synthwave / Lo-Fi Pentatonic Ambient BGM Loop in RAM (0 KB file size).
        /// </summary>
        public static AudioClip GetAmbientBgmLoop()
        {
            if (_audioClips.TryGetValue("bgm_loop", out AudioClip cached) && cached != null)
                return cached;

            int sampleRate = 22050;
            float[] melodyNotes =
            {
                261.63f, 329.63f, 392.00f, 440.00f,
                392.00f, 329.63f, 293.66f, 329.63f,
                261.63f, 329.63f, 392.00f, 523.25f,
                440.00f, 392.00f, 329.63f, 293.66f
            };
            float[] bassRoots = { 130.81f, 110.00f, 146.83f, 98.00f };

            float stepDuration = 0.40f;
            int samplesPerStep = Mathf.RoundToInt(sampleRate * stepDuration);
            int totalSamples = samplesPerStep * melodyNotes.Length;
            float[] data = new float[totalSamples];

            for (int step = 0; step < melodyNotes.Length; step++)
            {
                float melFreq = melodyNotes[step];
                float bassFreq = bassRoots[(step / 4) % bassRoots.Length];
                int startSample = step * samplesPerStep;

                for (int i = 0; i < samplesPerStep; i++)
                {
                    float tGlobal = (startSample + i) / (float)sampleRate;
                    float progress = i / (float)samplesPerStep;

                    float melEnv = Mathf.Clamp01(progress * 10f) * Mathf.Exp(-3.2f * progress);
                    float melWave = Mathf.Sin(2f * Mathf.PI * melFreq * tGlobal) + 0.3f * Mathf.Sin(4f * Mathf.PI * melFreq * tGlobal);

                    float bassWave = Mathf.Sin(2f * Mathf.PI * bassFreq * tGlobal) * 0.55f;
                    data[startSample + i] = (melWave * melEnv * 0.055f) + (bassWave * 0.035f);
                }
            }

            AudioClip bgm = AudioClip.Create("XO_AmbientBGM", totalSamples, 1, sampleRate, false);
            bgm.SetData(data, 0);
            _audioClips["bgm_loop"] = bgm;
            return bgm;
        }

        private static AudioClip CreateToneSequence(string name, float[] frequencies, float noteDuration, float volume)
        {
            int sampleRate = 22050;
            int samplesPerNote = Mathf.Max(1, Mathf.RoundToInt(sampleRate * noteDuration));
            int totalSamples = samplesPerNote * frequencies.Length;

            float[] data = new float[totalSamples];

            for (int n = 0; n < frequencies.Length; n++)
            {
                float freq = frequencies[n];
                int startSample = n * samplesPerNote;

                for (int i = 0; i < samplesPerNote; i++)
                {
                    float t = (float)i / sampleRate;
                    float progress = (float)i / samplesPerNote;
                    float attack = Mathf.Clamp01(progress * 14f);
                    float decay = Mathf.Pow(1f - progress, 1.5f);
                    float env = attack * decay;

                    float wave = Mathf.Sin(2f * Mathf.PI * freq * t) + 0.25f * Mathf.Sin(4f * Mathf.PI * freq * t);
                    data[startSample + i] = wave * env * volume;
                }
            }

            AudioClip clip = AudioClip.Create("XO_" + name, totalSamples, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
