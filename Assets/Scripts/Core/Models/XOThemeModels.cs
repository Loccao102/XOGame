using System;

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
        public XOColor backgroundTop;
        public XOColor backgroundBottom;
        public XOColor headerCardColor;
        public XOColor panelSurfaceColor;
        public XOColor panelBorderColor;

        // Board & Cells
        public XOColor boardFrameColor;
        public XOColor cellNormalColor;
        public XOColor cellAlternateColor;
        public XOColor cellHoverColor;
        public XOColor cellLastMoveColor;
        public XOColor cellWinHighlightColor;

        // X and O Pieces
        public XOColor xColor;
        public XOColor xGlowColor;
        public XOColor oColor;
        public XOColor oGlowColor;

        // Text & Accents
        public XOColor primaryTextColor;
        public XOColor secondaryTextColor;
        public XOColor accentColor;
        public XOColor goldAdColor;
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
}

