using System;
using System.Globalization;

namespace XOPlayable.Core
{
    /// <summary>Engine-agnostic RGBA color struct (0..1 components) replacing UnityEngine.Color inside Core.</summary>
    [Serializable]
    public readonly struct XOColor : IEquatable<XOColor>
    {
        public readonly float r;
        public readonly float g;
        public readonly float b;
        public readonly float a;

        public static readonly XOColor White = new XOColor(1f, 1f, 1f, 1f);
        public static readonly XOColor Black = new XOColor(0f, 0f, 0f, 1f);
        public static readonly XOColor Clear = new XOColor(0f, 0f, 0f, 0f);

        public XOColor(float r, float g, float b, float a = 1f)
        {
            this.r = r;
            this.g = g;
            this.b = b;
            this.a = a;
        }

        public static XOColor Lerp(XOColor from, XOColor to, float t)
        {
            t = XOMath.Clamp01(t);
            return new XOColor(
                from.r + (to.r - from.r) * t,
                from.g + (to.g - from.g) * t,
                from.b + (to.b - from.b) * t,
                from.a + (to.a - from.a) * t);
        }

        public static XOColor FromHex(string hex)
        {
            if (string.IsNullOrEmpty(hex)) return White;
            if (hex[0] == '#') hex = hex.Substring(1);

            if (hex.Length == 6 &&
                byte.TryParse(hex.Substring(0, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out byte r8) &&
                byte.TryParse(hex.Substring(2, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out byte g8) &&
                byte.TryParse(hex.Substring(4, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out byte b8))
            {
                return new XOColor(r8 / 255f, g8 / 255f, b8 / 255f, 1f);
            }

            if (hex.Length == 8 &&
                byte.TryParse(hex.Substring(0, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out byte r32) &&
                byte.TryParse(hex.Substring(2, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out byte g32) &&
                byte.TryParse(hex.Substring(4, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out byte b32) &&
                byte.TryParse(hex.Substring(6, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out byte a32))
            {
                return new XOColor(r32 / 255f, g32 / 255f, b32 / 255f, a32 / 255f);
            }

            return White;
        }

        public bool Equals(XOColor other)
            => r.Equals(other.r) && g.Equals(other.g) && b.Equals(other.b) && a.Equals(other.a);

        public override bool Equals(object obj) => obj is XOColor other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = r.GetHashCode();
                hash = (hash * 397) ^ g.GetHashCode();
                hash = (hash * 397) ^ b.GetHashCode();
                hash = (hash * 397) ^ a.GetHashCode();
                return hash;
            }
        }

        public static bool operator ==(XOColor left, XOColor right) => left.Equals(right);
        public static bool operator !=(XOColor left, XOColor right) => !left.Equals(right);
    }
}

