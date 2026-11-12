using System;
using System.Collections.Generic;
using UnityEngine;

namespace XOPlayable.Visuals
{
    /// <summary>
    /// Public facade for the Pastel Clay procedural sprite cache and audio synthesizer.
    /// Delegates pixel and waveform synthesis to focused helper classes in <c>XOPlayable.Visuals</c>.
    /// </summary>
    public static class XOProceduralAssets
    {
        private static readonly Dictionary<string, Sprite> _spriteCache = new Dictionary<string, Sprite>();

        // =====================================================================
        // PASTEL CLAY PALETTE
        // =====================================================================
        public static readonly Color ColInk       = XOTexturePrimitives.ColInk;
        public static readonly Color ColMuted     = XOTexturePrimitives.ColMuted;
        public static readonly Color ColMint      = XOTexturePrimitives.ColMint;
        public static readonly Color ColMintEdge  = XOTexturePrimitives.ColMintEdge;
        public static readonly Color ColSky       = XOTexturePrimitives.ColSky;
        public static readonly Color ColSkyEdge   = XOTexturePrimitives.ColSkyEdge;
        public static readonly Color ColLilac     = XOTexturePrimitives.ColLilac;
        public static readonly Color ColLilacEdge = XOTexturePrimitives.ColLilacEdge;
        public static readonly Color ColPeach     = XOTexturePrimitives.ColPeach;
        public static readonly Color ColPeachEdge = XOTexturePrimitives.ColPeachEdge;
        public static readonly Color ColSun       = XOTexturePrimitives.ColSun;
        public static readonly Color ColSunEdge   = XOTexturePrimitives.ColSunEdge;
        public static readonly Color ColCoral     = XOTexturePrimitives.ColCoral;
        public static readonly Color ColCoralEdge = XOTexturePrimitives.ColCoralEdge;
        public static readonly Color ColTileEdge  = XOTexturePrimitives.ColTileEdge;
        public static readonly Color ColCardBg    = XOTexturePrimitives.ColCardBg;

        // =====================================================================
        // TACTILE CLAY 3D UI FRAMES, BUTTONS & BOARD TILES
        // =====================================================================
        public static Sprite RoundedRectSprite  => GetOrBuild("ui_rounded_rect", () => XOTexturePrimitives.CreateRoundedRectSprite(64, 64, 18f, 18));
        public static Sprite SoftCellSprite     => GetOrBuild("ui_soft_cell", () => XOTexturePrimitives.CreateBeveledCellSprite(96));
        public static Sprite PillSprite         => GetOrBuild("ui_pill", () => XOTexturePrimitives.CreateRoundedRectSprite(64, 64, 28f, 28));
        public static Sprite CircleSprite       => GetOrBuild("ui_circle", () => XOTexturePrimitives.CreateCircleSprite(64));
        public static Sprite HintTargetSprite   => GetOrBuild("ui_hint_target", () => XOTexturePrimitives.CreateHintTargetSprite(128));
        public static Sprite CardPanelSprite    => GetOrBuild("ui_card_panel", () => XOTexturePrimitives.CreateBeveledCardSprite(128, 128, 28f, 32));
        public static Sprite RecessedSlotSprite => GetOrBuild("ui_recessed_slot", () => XOTexturePrimitives.CreateRecessedSlotSprite(96, 96, 22f, 24));
        public static Sprite Button3DSprite     => GetOrBuild("ui_btn_3d", () => XOTexturePrimitives.CreateTactile3DButtonSprite(128, 128, 26f, 32));
        public static Sprite Pill3DSprite       => GetOrBuild("ui_pill_3d", () => XOTexturePrimitives.CreateTactile3DButtonSprite(128, 80, 36f, 36));
        /// <summary>White strip that is opaque at the top and fully transparent at the bottom; tint it to blend a top colour into whatever sits behind it.</summary>
        public static Sprite GridTileSprite => GetOrBuild("ui_grid_tile", () => XOTexturePrimitives.CreateGridTileSprite(80));
        public static Sprite VerticalFadeSprite => GetOrBuild("ui_vertical_fade", () => XOTexturePrimitives.CreateVerticalFadeSprite(128));

        // =====================================================================
        // ILLUSTRATED PASTEL VECTOR ICONS & PIECES
        // =====================================================================
        public static Sprite XPieceSprite    => GetOrBuild("piece_x_neon", () => XOPieceSprites.CreateXPieceSprite(128));
        public static Sprite OPieceSprite    => GetOrBuild("piece_o_neon", () => XOPieceSprites.CreateOPieceSprite(128));
        public static Sprite IconCoin        => GetOrBuild("icon_coin", () => XOHudIcons.CreateCoinIcon(128));
        public static Sprite IconChest       => GetOrBuild("icon_chest", () => XOHudIcons.CreateChestIcon(128));
        public static Sprite IconHint        => GetOrBuild("icon_hint", () => XOActionIcons.CreateHintBulbIcon(128));
        public static Sprite IconUndo        => GetOrBuild("icon_undo", () => XOActionIcons.CreateUndoArrowIcon(128));
        public static Sprite IconBomb        => GetOrBuild("icon_bomb", () => XOActionIcons.CreateBombIcon(128));
        public static Sprite IconFreeze      => GetOrBuild("icon_freeze", () => XOActionIcons.CreateFreezeSnowflakeIcon(128));
        public static Sprite IconSkinShop    => GetOrBuild("icon_skin", () => XOHudIcons.CreatePaletteShopIcon(128));
        public static Sprite IconVideoAd     => GetOrBuild("icon_video_ad", () => XORewardIcons.CreateVideoAdClapperIcon(128));
        public static Sprite IconTrophy      => GetOrBuild("icon_trophy", () => XORewardIcons.CreateTrophyIcon(128));
        public static Sprite IconCrown       => GetOrBuild("icon_crown", () => XORewardIcons.CreateCrownIcon(128));
        public static Sprite IconFlame       => GetOrBuild("icon_flame", () => XOHudIcons.CreateFlameIcon(128));
        public static Sprite IconSwords      => GetOrBuild("icon_swords", () => XORewardIcons.CreateCrossedSwordsIcon(128));
        public static Sprite IconShield      => GetOrBuild("icon_shield", () => XORewardIcons.CreateShieldIcon(128));
        public static Sprite IconSoundOn     => GetOrBuild("icon_sound_on", () => XOHudIcons.CreateSoundIcon(128, false));
        public static Sprite IconSoundOff    => GetOrBuild("icon_sound_off", () => XOHudIcons.CreateSoundIcon(128, true));
        public static Sprite IconNewGame     => GetOrBuild("icon_new_game", () => XOHudIcons.CreatePlayStarIcon(128));
        public static Sprite IconSwap        => GetOrBuild("icon_swap", () => XOActionIcons.CreateSwapArrowsIcon(128));
        public static Sprite IconLightning   => GetOrBuild("icon_lightning", () => XOActionIcons.CreateLightningBoltIcon(128));
        public static Sprite IconRock        => GetOrBuild("icon_rock", () => XOActionIcons.CreateRockObstacleIcon(128));
        public static Sprite IconLeaderboard => GetOrBuild("icon_leaderboard", () => XOHudIcons.CreateLeaderboardPodiumIcon(128));
        public static Sprite IconLuckyWheel  => GetOrBuild("icon_lucky_wheel", () => XOHudIcons.CreateLuckyWheelIcon(128));
        public static Sprite IconHome        => GetOrBuild("icon_home", () => XOActionIcons.CreateHomeIcon(128));
        public static Sprite IconHeart       => GetOrBuild("icon_heart", () => XOActionIcons.CreateHeartIcon(128));
        public static Sprite IconRelic       => GetOrBuild("icon_relic", () => XORewardIcons.CreateRelicJokerIcon(128));

        public static Sprite GetPieceSkinSprite(string skinId, bool isX)
        {
            return skinId switch
            {
                "fire_ice"     => isX ? IconFlame : IconFreeze,
                "sword_shield" => isX ? IconSwords : IconShield,
                "king_dragon"  => isX ? IconCrown : GetOrBuild("piece_ruby_gem", () => XOPieceSprites.CreateRubyGemIcon(128)),
                "star_moon"    => isX ? IconNewGame : GetOrBuild("piece_crescent_moon", () => XOPieceSprites.CreateCrescentMoonIcon(128)),
                _              => isX ? XPieceSprite : OPieceSprite
            };
        }

        public static Color Hex(string hex) => XOTexturePrimitives.Hex(hex);
        public static Color WithAlpha(Color c, float a) => XOTexturePrimitives.WithAlpha(c, a);
        public static AudioClip GetSound(string key) => XOProceduralAudio.GetSound(key);
        public static AudioClip GetAmbientBgmLoop() => XOProceduralAudio.GetAmbientBgmLoop();

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
    }
}
