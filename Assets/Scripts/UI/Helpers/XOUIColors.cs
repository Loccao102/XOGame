using UnityEngine;
using XOPlayable.Visuals;

namespace XOPlayable.UI
{
    /// <summary>
    /// Named colours for UI accents, status chips and text tones. Names follow the Tailwind palette (family + shade, lower
    /// number is lighter), so a colour can be found by what it looks like and changed in one place.
    /// The Pastel Clay base colours (ink, mint, sky...) stay in <see cref="XOProceduralAssets"/>.
    /// </summary>
    public static class XOUIColors
    {
        // Slate
        public static readonly Color Slate50 = XOProceduralAssets.Hex("#F8FAFC");
        public static readonly Color Slate100 = XOProceduralAssets.Hex("#F1F5F9");
        public static readonly Color Slate200 = XOProceduralAssets.Hex("#E2E8F0");
        public static readonly Color Slate600 = XOProceduralAssets.Hex("#475569");
        public static readonly Color Slate700 = XOProceduralAssets.Hex("#334155");
        public static readonly Color Slate800 = XOProceduralAssets.Hex("#1E293B");
        public static readonly Color Slate900 = XOProceduralAssets.Hex("#0F172A");

        // Red
        public static readonly Color Red500 = XOProceduralAssets.Hex("#EF4444");
        public static readonly Color Red600 = XOProceduralAssets.Hex("#DC2626");
        public static readonly Color Red900 = XOProceduralAssets.Hex("#7F1D1D");

        // Orange
        public static readonly Color Orange500 = XOProceduralAssets.Hex("#F97316");

        // Amber
        public static readonly Color Amber50 = XOProceduralAssets.Hex("#FFFBEB");
        public static readonly Color Amber100 = XOProceduralAssets.Hex("#FEF3C7");
        public static readonly Color Amber300 = XOProceduralAssets.Hex("#FCD34D");
        public static readonly Color Amber400 = XOProceduralAssets.Hex("#FBBF24");
        public static readonly Color Amber500 = XOProceduralAssets.Hex("#F59E0B");
        public static readonly Color Amber600 = XOProceduralAssets.Hex("#D97706");
        public static readonly Color Amber700 = XOProceduralAssets.Hex("#B45309");

        // Yellow
        public static readonly Color Yellow50 = XOProceduralAssets.Hex("#FEFCE8");
        public static readonly Color Yellow100 = XOProceduralAssets.Hex("#FEF9C3");
        public static readonly Color Yellow200 = XOProceduralAssets.Hex("#FEF08A");
        public static readonly Color Yellow300 = XOProceduralAssets.Hex("#FDE047");

        // Green
        public static readonly Color Green50 = XOProceduralAssets.Hex("#F0FDF4");
        public static readonly Color Green500 = XOProceduralAssets.Hex("#22C55E");
        public static readonly Color Green600 = XOProceduralAssets.Hex("#16A34A");

        // Emerald
        public static readonly Color Emerald50 = XOProceduralAssets.Hex("#ECFDF5");
        public static readonly Color Emerald100 = XOProceduralAssets.Hex("#D1FAE5");
        public static readonly Color Emerald600 = XOProceduralAssets.Hex("#059669");
        public static readonly Color Emerald800 = XOProceduralAssets.Hex("#065F46");

        // Cyan
        public static readonly Color Cyan400 = XOProceduralAssets.Hex("#22D3EE");

        // Sky
        public static readonly Color Sky100 = XOProceduralAssets.Hex("#E0F2FE");
        public static readonly Color Sky200 = XOProceduralAssets.Hex("#BAE6FD");
        public static readonly Color Sky400 = XOProceduralAssets.Hex("#38BDF8");
        public static readonly Color Sky500 = XOProceduralAssets.Hex("#0EA5E9");
        public static readonly Color Sky600 = XOProceduralAssets.Hex("#0284C7");

        // Indigo
        public static readonly Color Indigo50 = XOProceduralAssets.Hex("#EEF2FF");

        // Violet
        public static readonly Color Violet100 = XOProceduralAssets.Hex("#EDE9FE");
        public static readonly Color Violet500 = XOProceduralAssets.Hex("#8B5CF6");
        public static readonly Color Violet600 = XOProceduralAssets.Hex("#7C3AED");
        public static readonly Color Violet800 = XOProceduralAssets.Hex("#5B21B6");

        // Purple
        public static readonly Color Purple500 = XOProceduralAssets.Hex("#A855F7");
        public static readonly Color Purple600 = XOProceduralAssets.Hex("#9333EA");

        // Rose
        public static readonly Color Rose100 = XOProceduralAssets.Hex("#FFE4E6");
        public static readonly Color Rose200 = XOProceduralAssets.Hex("#FECDD3");
        public static readonly Color Rose400 = XOProceduralAssets.Hex("#FB7185");
        public static readonly Color Rose600 = XOProceduralAssets.Hex("#E11D48");
    }
}
