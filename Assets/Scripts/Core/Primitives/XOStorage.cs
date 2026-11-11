using System;
using System.Collections.Generic;

namespace XOPlayable.Core
{
    /// <summary>Abstraction for persistent key-value storage so Core never depends on UnityEngine.PlayerPrefs.</summary>
    public interface IXOKeyValueStorage
    {
        int GetInt(string key, int defaultValue = 0);
        void SetInt(string key, int value);
        string GetString(string key, string defaultValue = "");
        void SetString(string key, string value);
        void Save();
    }

    /// <summary>In-memory storage used automatically in headless tests and CI.</summary>
    public sealed class XOMemoryStorage : IXOKeyValueStorage
    {
        private readonly Dictionary<string, int> _ints = new Dictionary<string, int>();
        private readonly Dictionary<string, string> _strings = new Dictionary<string, string>();

        public int GetInt(string key, int defaultValue = 0)
            => _ints.TryGetValue(key, out int v) ? v : defaultValue;

        public void SetInt(string key, int value) => _ints[key] = value;

        public string GetString(string key, string defaultValue = "")
            => _strings.TryGetValue(key, out string v) ? v : defaultValue;

        public void SetString(string key, string value) => _strings[key] = value;

        public void Save() { }
    }

    /// <summary>
    /// Persistence gateway for Core progression and themes.
    /// Defaults to an in-memory store in pure .NET CI; Unity configures it with PlayerPrefs at startup.
    /// </summary>
    public static class XOStorage
    {
        private static IXOKeyValueStorage _backend = new XOMemoryStorage();
        private static Action<string> _warningLogger;
        private static bool _warnedWriteFailure;

        public static IXOKeyValueStorage Backend => _backend;

        public static void Configure(IXOKeyValueStorage backend, Action<string> warningLogger = null)
        {
            _backend = backend ?? new XOMemoryStorage();
            _warningLogger = warningLogger;
            _warnedWriteFailure = false;
        }

        public static void ResetToMemory() => Configure(new XOMemoryStorage(), null);

        public static int GetInt(string key, int def = 0)
        {
            try { return _backend.GetInt(key, def); }
            catch (Exception e) { WarnOnce("GetInt", e); return def; }
        }

        public static void SetInt(string key, int val)
        {
            try { _backend.SetInt(key, val); }
            catch (Exception e) { WarnOnce("SetInt", e); }
        }

        public static string GetString(string key, string def = "")
        {
            try { return _backend.GetString(key, def); }
            catch (Exception e) { WarnOnce("GetString", e); return def; }
        }

        public static void SetString(string key, string val)
        {
            try { _backend.SetString(key, val); }
            catch (Exception e) { WarnOnce("SetString", e); }
        }

        public static void Save()
        {
            try { _backend.Save(); }
            catch (Exception e) { WarnOnce("Save", e); }
        }

        private static void WarnOnce(string op, Exception e)
        {
            if (_warnedWriteFailure) return;
            _warnedWriteFailure = true;
            _warningLogger?.Invoke("[XO Storage] " + op + " failed; progress may not persist: " + e.Message);
        }
    }
}

