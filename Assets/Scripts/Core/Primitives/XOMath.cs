using System;

namespace XOPlayable.Core
{
    /// <summary>Engine-agnostic math helpers replacing UnityEngine.Mathf inside Core.</summary>
    public static class XOMath
    {
        public const float PI = (float)Math.PI;

        public static int Clamp(int value, int min, int max)
            => value < min ? min : (value > max ? max : value);

        public static float Clamp(float value, float min, float max)
            => value < min ? min : (value > max ? max : value);

        public static float Clamp01(float value) => Clamp(value, 0f, 1f);

        public static int Min(int a, int b) => a < b ? a : b;
        public static float Min(float a, float b) => a < b ? a : b;

        public static int Max(int a, int b) => a > b ? a : b;
        public static float Max(float a, float b) => a > b ? a : b;

        public static int Abs(int value) => value < 0 ? -value : value;
        public static float Abs(float value) => Math.Abs(value);

        public static int FloorToInt(float value) => (int)Math.Floor(value);
        public static int RoundToInt(float value) => (int)Math.Round(value);
    }
}

