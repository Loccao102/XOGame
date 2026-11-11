using System;
using UnityEngine;

namespace XOPlayable.Core
{
    [Serializable]
    public class XOThemePalette
    {
        public string id;
        public string displayName;
        public string subtitle;
        public int coinCost;
        public bool unlockedByDefault;

        // Main Background & UI Panels
        public Color backgroundTop;
        public Color backgroundBottom;
        public Color headerCardColor;
        public Color panelSurfaceColor;
        public Color panelBorderColor;

        // Board & Cells
        public Color boardFrameColor;
        public Color cellNormalColor;
        public Color cellAlternateColor;
        public Color cellHoverColor;
        public Color cellLastMoveColor;
        public Color cellWinHighlightColor;

        // X and O Pieces
        public Color xColor;
        public Color xGlowColor;
        public Color oColor;
        public Color oGlowColor;

        // Text & Accents
        public Color primaryTextColor;
        public Color secondaryTextColor;
        public Color accentColor;
        public Color goldAdColor;
    }

    [Serializable]
    public class XOPieceSkin
    {
        public string id;
        public string displayName;
        public string subtitle;
        public int coinCost;
        public bool unlockedByDefault;
    }

    public static class XOThemeDatabase
    {
        private static XOThemePalette[] _themes;
        private static XOPieceSkin[] _pieceSkins;

        public static XOThemePalette[] Themes
        {
            get
            {
                if (_themes == null)
                    _themes = CreateDefaultThemes();
                return _themes;
            }
        }

        public static XOPieceSkin[] PieceSkins
        {
            get
            {
                if (_pieceSkins == null)
                    _pieceSkins = CreateDefaultPieceSkins();
                return _pieceSkins;
            }
        }

        private static XOPieceSkin[] CreateDefaultPieceSkins()
        {
            return new XOPieceSkin[]
            {
                new XOPieceSkin
                {
                    id = "classic_xo",
                    displayName = "Neon X vs O",
                    subtitle = "Classic Glowing Tokens",
                    coinCost = 0,
                    unlockedByDefault = true
                },
                new XOPieceSkin
                {
                    id = "fire_ice",
                    displayName = "Blaze vs Frost",
                    subtitle = "Fire Flame & Ice Crystal",
                    coinCost = 100,
                    unlockedByDefault = false
                },
                new XOPieceSkin
                {
                    id = "sword_shield",
                    displayName = "Blade vs Aegis",
                    subtitle = "Royal Knights Emblem",
                    coinCost = 150,
                    unlockedByDefault = false
                },
                new XOPieceSkin
                {
                    id = "king_dragon",
                    displayName = "Crown vs Ruby",
                    subtitle = "Imperial Gold & Crimson Gem",
                    coinCost = 200,
                    unlockedByDefault = false
                },
                new XOPieceSkin
                {
                    id = "star_moon",
                    displayName = "Nova vs Lunar",
                    subtitle = "Celestial Star & Crescent Moon",
                    coinCost = 250,
                    unlockedByDefault = false
                }
            };
        }

        private static XOThemePalette[] CreateDefaultThemes()
        {
            return new XOThemePalette[]
            {
                // 0: Pastel Clay (Default)
                new XOThemePalette
                {
                    id = "pastel_clay",
                    displayName = "Pastel Clay",
                    subtitle = "Soft & Calm Tactile",
                    coinCost = 0,
                    unlockedByDefault = true,
                    backgroundTop = Hex("#F3E6FF"),
                    backgroundBottom = Hex("#C9DEFF"),
                    headerCardColor = Hex("#FFFFFF"),
                    panelSurfaceColor = Hex("#FFFFFF"),
                    panelBorderColor = Hex("#D6DEEF"),
                    boardFrameColor = Hex("#FFFFFF"),
                    cellNormalColor = Hex("#FFFFFF"),
                    cellAlternateColor = Hex("#F5F8FF"),
                    cellHoverColor = Hex("#EAF4FF"),
                    cellLastMoveColor = Hex("#E0EDFF"),
                    cellWinHighlightColor = Hex("#D9FBEB"),
                    xColor = Hex("#4FB0FF"),
                    xGlowColor = Hex("#258FDE"),
                    oColor = Hex("#FF6F85"),
                    oGlowColor = Hex("#E04B62"),
                    primaryTextColor = Hex("#2A3566"),
                    secondaryTextColor = Hex("#7380AE"),
                    accentColor = Hex("#4ED6A4"),
                    goldAdColor = Hex("#FFCF5C")
                },

                // 1: Classic Notebook Paper
                new XOThemePalette
                {
                    id = "notebook_paper",
                    displayName = "Notebook Grid",
                    subtitle = "School Paper Style",
                    coinCost = 100,
                    unlockedByDefault = false,
                    backgroundTop = Hex("#F5F0E6"),
                    backgroundBottom = Hex("#E8DFCA"),
                    headerCardColor = Hex("#FFFDF9"),
                    panelSurfaceColor = Hex("#FFFDF9"),
                    panelBorderColor = Hex("#CBD5E1"),
                    boardFrameColor = Hex("#93C5FD"),
                    cellNormalColor = Hex("#FFFFFF"),
                    cellAlternateColor = Hex("#F1F5F9"),
                    cellHoverColor = Hex("#E0F2FE"),
                    cellLastMoveColor = Hex("#FEF3C7"),
                    cellWinHighlightColor = Hex("#FDE047"),
                    xColor = Hex("#1D4ED8"),
                    xGlowColor = Hex("#3B82F6"),
                    oColor = Hex("#DC2626"),
                    oGlowColor = Hex("#EF4444"),
                    primaryTextColor = Hex("#1E293B"),
                    secondaryTextColor = Hex("#64748B"),
                    accentColor = Hex("#2563EB"),
                    goldAdColor = Hex("#F59E0B")
                },

                // 2: Royal Wood
                new XOThemePalette
                {
                    id = "royal_wood",
                    displayName = "Royal Wood",
                    subtitle = "Grandmaster Board",
                    coinCost = 150,
                    unlockedByDefault = false,
                    backgroundTop = Hex("#271914"),
                    backgroundBottom = Hex("#1A0F0C"),
                    headerCardColor = Hex("#3E2723"),
                    panelSurfaceColor = Hex("#3E2723"),
                    panelBorderColor = Hex("#5D4037"),
                    boardFrameColor = Hex("#4E342E"),
                    cellNormalColor = Hex("#D7CCC8"),
                    cellAlternateColor = Hex("#EFEBE9"),
                    cellHoverColor = Hex("#FFE0B2"),
                    cellLastMoveColor = Hex("#FFCC80"),
                    cellWinHighlightColor = Hex("#FFB300"),
                    xColor = Hex("#00796B"),
                    xGlowColor = Hex("#004D40"),
                    oColor = Hex("#D84315"),
                    oGlowColor = Hex("#BF360C"),
                    primaryTextColor = Hex("#FFF8E1"),
                    secondaryTextColor = Hex("#D7CCC8"),
                    accentColor = Hex("#FFB74D"),
                    goldAdColor = Hex("#FFD54F")
                },

                // 3: Pastel Candy
                new XOThemePalette
                {
                    id = "pastel_candy",
                    displayName = "Pastel Candy",
                    subtitle = "Sweet & Cozy",
                    coinCost = 200,
                    unlockedByDefault = false,
                    backgroundTop = Hex("#FDF2F8"),
                    backgroundBottom = Hex("#EDE9FE"),
                    headerCardColor = Hex("#FFFFFF"),
                    panelSurfaceColor = Hex("#FFFFFF"),
                    panelBorderColor = Hex("#FBCFE8"),
                    boardFrameColor = Hex("#F472B6"),
                    cellNormalColor = Hex("#FFF1F2"),
                    cellAlternateColor = Hex("#FDF4FF"),
                    cellHoverColor = Hex("#FCE7F3"),
                    cellLastMoveColor = Hex("#E0E7FF"),
                    cellWinHighlightColor = Hex("#FDE047"),
                    xColor = Hex("#0284C7"),
                    xGlowColor = Hex("#38BDF8"),
                    oColor = Hex("#E11D48"),
                    oGlowColor = Hex("#FB7185"),
                    primaryTextColor = Hex("#334155"),
                    secondaryTextColor = Hex("#64748B"),
                    accentColor = Hex("#EC4899"),
                    goldAdColor = Hex("#F59E0B")
                }
            };
        }

        public static bool IsThemeUnlocked(string id)
        {
            foreach (var t in Themes)
            {
                if (t.id == id && t.unlockedByDefault) return true;
            }
            return SafePrefs.GetInt("XO_Theme_Unlocked_" + id, 0) == 1;
        }

        public static void UnlockTheme(string id)
        {
            SafePrefs.SetInt("XO_Theme_Unlocked_" + id, 1);
            SafePrefs.Save();
        }

        public static int GetSelectedThemeIndex()
        {
            int idx = SafePrefs.GetInt("XO_Selected_Theme", 0);
            if (idx < 0 || idx >= Themes.Length) idx = 0;
            if (!IsThemeUnlocked(Themes[idx].id)) idx = 0;
            return idx;
        }

        public static void SetSelectedThemeIndex(int index)
        {
            if (index >= 0 && index < Themes.Length)
            {
                SafePrefs.SetInt("XO_Selected_Theme", index);
                SafePrefs.Save();
            }
        }

        public static bool IsPieceSkinUnlocked(string id)
        {
            foreach (var s in PieceSkins)
            {
                if (s.id == id && s.unlockedByDefault) return true;
            }
            return SafePrefs.GetInt("XO_PieceSkin_Unlocked_" + id, 0) == 1;
        }

        public static void UnlockPieceSkin(string id)
        {
            SafePrefs.SetInt("XO_PieceSkin_Unlocked_" + id, 1);
            SafePrefs.Save();
        }

        public static int GetSelectedPieceSkinIndex()
        {
            int idx = SafePrefs.GetInt("XO_Selected_PieceSkin", 0);
            if (idx < 0 || idx >= PieceSkins.Length) idx = 0;
            if (!IsPieceSkinUnlocked(PieceSkins[idx].id)) idx = 0;
            return idx;
        }

        public static void SetSelectedPieceSkinIndex(int index)
        {
            if (index >= 0 && index < PieceSkins.Length)
            {
                SafePrefs.SetInt("XO_Selected_PieceSkin", index);
                SafePrefs.Save();
            }
        }

        public static Color Hex(string hex)
        {
            if (string.IsNullOrEmpty(hex)) return Color.white;
            if (hex[0] == '#') hex = hex.Substring(1);

            if (hex.Length == 6 &&
                byte.TryParse(hex.Substring(0, 2), System.Globalization.NumberStyles.HexNumber, null, out byte r) &&
                byte.TryParse(hex.Substring(2, 2), System.Globalization.NumberStyles.HexNumber, null, out byte g) &&
                byte.TryParse(hex.Substring(4, 2), System.Globalization.NumberStyles.HexNumber, null, out byte b))
            {
                return new Color(r / 255f, g / 255f, b / 255f, 1f);
            }

            if (hex.Length == 8 &&
                byte.TryParse(hex.Substring(0, 2), System.Globalization.NumberStyles.HexNumber, null, out byte r8) &&
                byte.TryParse(hex.Substring(2, 2), System.Globalization.NumberStyles.HexNumber, null, out byte g8) &&
                byte.TryParse(hex.Substring(4, 2), System.Globalization.NumberStyles.HexNumber, null, out byte b8) &&
                byte.TryParse(hex.Substring(6, 2), System.Globalization.NumberStyles.HexNumber, null, out byte a8))
            {
                return new Color(r8 / 255f, g8 / 255f, b8 / 255f, a8 / 255f);
            }

            return Color.white;
        }
    }
}
