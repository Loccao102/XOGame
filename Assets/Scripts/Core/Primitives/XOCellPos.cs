using System;

namespace XOPlayable.Core
{
    /// <summary>Engine-agnostic integer grid coordinate replacing UnityEngine.Vector2Int inside Core.</summary>
    [Serializable]
    public readonly struct XOCellPos : IEquatable<XOCellPos>
    {
        public readonly int x;
        public readonly int y;

        public static readonly XOCellPos Invalid = new XOCellPos(-1, -1);
        public static readonly XOCellPos Zero = new XOCellPos(0, 0);

        public bool IsValid => x >= 0 && y >= 0;

        public XOCellPos(int x, int y)
        {
            this.x = x;
            this.y = y;
        }

        public bool Equals(XOCellPos other) => x == other.x && y == other.y;
        public override bool Equals(object obj) => obj is XOCellPos other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                return (x * 397) ^ y;
            }
        }

        public static bool operator ==(XOCellPos left, XOCellPos right) => left.Equals(right);
        public static bool operator !=(XOCellPos left, XOCellPos right) => !left.Equals(right);

        public override string ToString() => $"({x}, {y})";
    }
}

