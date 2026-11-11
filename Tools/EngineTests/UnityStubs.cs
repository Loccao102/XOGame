// Minimal UnityEngine stand-ins so the pure-logic engine (Assets/Scripts/Core) can be compiled and
// tested with plain `dotnet test` / `dotnet run` on any OS, without a Unity installation.
// Only the members the Core scripts actually use are provided.
using System;
using System.Collections.Generic;

namespace UnityEngine
{
    public struct Vector2Int : IEquatable<Vector2Int>
    {
        public int x, y;
        public Vector2Int(int x, int y) { this.x = x; this.y = y; }
        public bool Equals(Vector2Int o) => x == o.x && y == o.y;
        public override bool Equals(object o) => o is Vector2Int v && Equals(v);
        public override int GetHashCode() => (x * 397) ^ y;
        public static bool operator ==(Vector2Int a, Vector2Int b) => a.Equals(b);
        public static bool operator !=(Vector2Int a, Vector2Int b) => !a.Equals(b);
        public override string ToString() => $"({x}, {y})";
    }

    public struct Color
    {
        public float r, g, b, a;
        public Color(float r, float g, float b, float a = 1f) { this.r = r; this.g = g; this.b = b; this.a = a; }
        public static Color white => new Color(1, 1, 1, 1);
        public static Color black => new Color(0, 0, 0, 1);
        public static Color clear => new Color(0, 0, 0, 0);
    }

    public static class Mathf
    {
        public static int Max(int a, int b) => a > b ? a : b;
        public static float Max(float a, float b) => a > b ? a : b;
        public static int Min(int a, int b) => a < b ? a : b;
        public static float Min(float a, float b) => a < b ? a : b;
        public static int Abs(int v) => v < 0 ? -v : v;
        public static float Abs(float v) => v < 0 ? -v : v;
        public static int Clamp(int v, int lo, int hi) => v < lo ? lo : (v > hi ? hi : v);
        public static float Clamp(float v, float lo, float hi) => v < lo ? lo : (v > hi ? hi : v);
        public static float Clamp01(float v) => Clamp(v, 0f, 1f);
        public static int FloorToInt(float v) => (int)Math.Floor(v);
        public static int RoundToInt(float v) => (int)Math.Round(v);
        public static int CeilToInt(float v) => (int)Math.Ceiling(v);
        public static float Lerp(float a, float b, float t) => a + (b - a) * Clamp01(t);
    }

    public static class Debug
    {
        public static void Log(object m) { }
        public static void LogWarning(object m) { }
        public static void LogError(object m) { Console.Error.WriteLine(m); }
    }

    public static class PlayerPrefs
    {
        static readonly Dictionary<string, object> Store = new Dictionary<string, object>();
        public static void SetInt(string k, int v) => Store[k] = v;
        public static void SetString(string k, string v) => Store[k] = v;
        public static int GetInt(string k, int d = 0) => Store.TryGetValue(k, out var v) && v is int i ? i : d;
        public static string GetString(string k, string d = "") => Store.TryGetValue(k, out var v) && v is string s ? s : d;
        public static bool HasKey(string k) => Store.ContainsKey(k);
        public static void DeleteKey(string k) => Store.Remove(k);
        public static void DeleteAll() => Store.Clear();
        public static void Save() { }
    }
}

namespace UnityEngine
{
    public class Sprite { }
}

namespace XOPlayable.Visuals
{
    using UnityEngine;

    // Presentation constants referenced by Core data tables. The real implementation lives in
    // Assets/Scripts/Visuals; tests never look at the values.
    public static class XOProceduralAssets
    {
        public static readonly Color ColLilac = Color.white, ColMint = Color.white, ColMuted = Color.white,
                                     ColSky = Color.white, ColSun = Color.white;
        public static Sprite IconBomb, IconChest, IconCoin, IconCrown, IconHeart, IconHint, IconRelic,
                             IconRock, IconShield, IconSwords, IconTrophy, IconUndo;
    }
}
