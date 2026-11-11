using System;
using UnityEngine;
using XOPlayable.Core;

namespace XOPlayable.Visuals
{
    /// <summary>
    /// Conversion helpers between framework-free Core primitives (<see cref="XOColor"/>, <see cref="XOCellPos"/>)
    /// and UnityEngine structs (<see cref="Color"/>, <see cref="Vector2Int"/>).
    /// </summary>
    public static class XOUnityExtensions
    {
        public static Color ToUnityColor(this XOColor c)
        {
            return new Color(c.r, c.g, c.b, c.a);
        }

        public static XOColor ToXOColor(this Color c)
        {
            return new XOColor(c.r, c.g, c.b, c.a);
        }

        public static Vector2Int ToVector2Int(this XOCellPos pos)
        {
            return new Vector2Int(pos.x, pos.y);
        }

        public static XOCellPos ToCellPos(this Vector2Int pos)
        {
            return new XOCellPos(pos.x, pos.y);
        }
    }

    /// <summary>
    /// Backs <see cref="XOStorage"/> with Unity's <see cref="PlayerPrefs"/> at runtime and in Editor.
    /// </summary>
    public sealed class XOPlayerPrefsStorage : IXOKeyValueStorage
    {
        public int GetInt(string key, int defaultValue = 0) => PlayerPrefs.GetInt(key, defaultValue);
        public void SetInt(string key, int value) => PlayerPrefs.SetInt(key, value);
        public string GetString(string key, string defaultValue = "") => PlayerPrefs.GetString(key, defaultValue);
        public void SetString(string key, string value) => PlayerPrefs.SetString(key, value);
        public void DeleteKey(string key) => PlayerPrefs.DeleteKey(key);
        public void Save() => PlayerPrefs.Save();
    }

    /// <summary>
    /// Registers Unity's PlayerPrefs storage backend and warning logger into Core before any scene loads.
    /// </summary>
    public static class XOStorageUnityBootstrap
    {
        private static bool _configured;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void OnBeforeSceneLoad()
        {
            Configure();
        }

        public static void Configure()
        {
            if (_configured) return;
            _configured = true;
            XOStorage.Configure(new XOPlayerPrefsStorage(), msg => Debug.LogWarning(msg));
        }
    }
}

