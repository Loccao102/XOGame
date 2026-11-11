using System;
using System.Collections.Generic;

namespace XOPlayable.Core
{
    /// <summary>
    /// Instance-based state container for one Roguelike Tower run and match flags.
    /// Allows XOCaroEngine and tests to operate without reading mutable global statics.
    /// </summary>
    public sealed class XORoguelikeRunState
    {
        public const int MaxLives = 1;
        public const int MaxRelicSlots = 4;

        private readonly IXOKeyValueStorage _storage;
        private readonly XORandomStream _rng = new XORandomStream();
        private List<CaroRelicType> _equippedRelics;

        public XORoguelikeRunState(IXOKeyValueStorage storage = null)
        {
            _storage = storage;
        }

        private int GetInt(string key, int def = 0)
            => _storage != null ? _storage.GetInt(key, def) : XOStorage.GetInt(key, def);

        private void SetInt(string key, int val)
        {
            if (_storage != null) _storage.SetInt(key, val);
            else XOStorage.SetInt(key, val);
        }

        private string GetString(string key, string def = "")
            => _storage != null ? _storage.GetString(key, def) : XOStorage.GetString(key, def);

        private void SetString(string key, string val)
        {
            if (_storage != null) _storage.SetString(key, val);
            else XOStorage.SetString(key, val);
        }

        private void SaveStorage()
        {
            if (_storage != null) _storage.Save();
            else XOStorage.Save();
        }

        public XORandomStream Random => _rng;

        public int CurrentRunSeed
        {
            get
            {
                int s = GetInt("XO_Rogue_RunSeed", 0);
                if (s == 0)
                {
                    s = _rng.Range(10000, 99999);
                    SetInt("XO_Rogue_RunSeed", s);
                    SaveStorage();
                }
                return s;
            }
            set
            {
                SetInt("XO_Rogue_RunSeed", value);
                SaveStorage();
            }
        }

        public string FormattedSeed => $"#{CurrentRunSeed}";

        public int Lives
        {
            get => GetInt("XO_Rogue_Lives", MaxLives);
            set => SetInt("XO_Rogue_Lives", XOMath.Clamp(value, 0, MaxLives));
        }

        public int CurrentFloor
        {
            get => GetInt("XO_Rogue_Floor", 1);
            set => SetInt("XO_Rogue_Floor", XOMath.Clamp(value, 1, 10));
        }

        public bool IsRunActive
        {
            get => GetInt("XO_Rogue_Active", 0) == 1;
            set => SetInt("XO_Rogue_Active", value ? 1 : 0);
        }

        public int RunTotalCoinsEarned
        {
            get => GetInt("XO_Rogue_CoinsEarned", 0);
            set => SetInt("XO_Rogue_CoinsEarned", value);
        }

        public List<CaroRelicType> EquippedRelics
        {
            get
            {
                if (_equippedRelics == null) LoadEquippedRelics();
                return _equippedRelics;
            }
        }

        public int PlayerMovesThisMatch { get; set; }
        public bool TwinThreatTriggeredThisMatch { get; set; }
        public bool GuardianAegisTriggeredThisMatch { get; set; }
        public bool ShatterHammerTriggeredThisMatch { get; set; }
        public bool PhoenixSigilTriggeredThisMatch { get; set; }
        public CaroRelicType? DisabledRelicThisMatch { get; set; }
        public bool IsTowerMatchActive { get; set; }

        public TowerFloorBlind GetActiveBlind()
            => XORoguelikeCatalog.GetFloorBlind(CurrentFloor);

        public void StartNewRun(int? customSeed = null)
        {
            int seed = (customSeed.HasValue && customSeed.Value != 0) ? customSeed.Value : _rng.Range(10000, 99999);
            CurrentRunSeed = seed;
            _rng.Reseed(seed);
            CurrentFloor = 1;
            Lives = MaxLives;
            IsRunActive = true;
            RunTotalCoinsEarned = 0;
            EquippedRelics.Clear();
            SaveEquippedRelics();
            ResetMatchState();
        }

        public void ResetMatchState()
        {
            PlayerMovesThisMatch = 0;
            TwinThreatTriggeredThisMatch = false;
            GuardianAegisTriggeredThisMatch = false;
            ShatterHammerTriggeredThisMatch = false;
            PhoenixSigilTriggeredThisMatch = false;
            DisabledRelicThisMatch = null;

            TowerFloorBlind blind = GetActiveBlind();
            if (blind.blindType == BossBlindType.TheArm && EquippedRelics.Count > 0)
            {
                int rIdx = _rng.Range(0, EquippedRelics.Count);
                DisabledRelicThisMatch = EquippedRelics[rIdx];
            }
        }

        public bool HasRelic(CaroRelicType type, bool isTowerContext)
        {
            if (!isTowerContext) return false;
            if (DisabledRelicThisMatch.HasValue && DisabledRelicThisMatch.Value == type) return false;
            return EquippedRelics.Contains(type);
        }

        public bool HasRelic(CaroRelicType type) => HasRelic(type, IsTowerMatchActive);

        public bool EquipRelic(CaroRelicType type)
        {
            if (EquippedRelics.Count >= MaxRelicSlots || EquippedRelics.Contains(type)) return false;
            EquippedRelics.Add(type);
            SaveEquippedRelics();
            return true;
        }

        public bool SellRelic(CaroRelicType type, out int refundCoins)
        {
            refundCoins = 0;
            if (!EquippedRelics.Contains(type)) return false;

            CaroRelic r = XORoguelikeCatalog.GetRelicData(type);
            refundCoins = (r != null) ? r.cost / 2 : 15;
            EquippedRelics.Remove(type);
            SaveEquippedRelics();
            return true;
        }

        public bool LoseHeart()
        {
            Lives = XOMath.Max(0, Lives - 1);
            if (Lives <= 0)
            {
                IsRunActive = false;
                CurrentFloor = 1;
                EquippedRelics.Clear();
                SaveEquippedRelics();
                CurrentRunSeed = _rng.Range(10000, 99999);
                SetInt("XO_Rogue_Floor", 1);
                SetInt("XO_Rogue_Lives", 0);
                SetInt("XO_Rogue_Active", 0);
                SaveStorage();
                return true;
            }
            return false;
        }

        public void RestoreHeart() => Lives = XOMath.Min(MaxLives, Lives + 1);

        public List<CaroRelic> GetRandomRelicDraft(int count = 3, int rerollCount = 0)
        {
            var pool = new List<CaroRelic>();
            foreach (var kvp in XORoguelikeCatalog.RelicDatabase)
            {
                if (!EquippedRelics.Contains(kvp.Key))
                    pool.Add(kvp.Value);
            }

            int seedHash = unchecked(CurrentRunSeed * 7919 + CurrentFloor * 313 + rerollCount * 1013 + 17);
            var seededRnd = new Random(seedHash);

            for (int i = 0; i < pool.Count; i++)
            {
                int r = seededRnd.Next(i, pool.Count);
                (pool[i], pool[r]) = (pool[r], pool[i]);
            }

            return pool.GetRange(0, XOMath.Min(count, pool.Count));
        }

        public int GetFloorSeed(int floor)
            => unchecked(CurrentRunSeed * 1013 + floor * 701 + 29);

        public void ReloadEquippedRelics() => LoadEquippedRelics();

        private void LoadEquippedRelics()
        {
            _equippedRelics = new List<CaroRelicType>();
            string saved = GetString("XO_Rogue_EquippedRelics", "");
            if (string.IsNullOrEmpty(saved)) return;

            string[] tokens = saved.Split(',');
            foreach (string t in tokens)
            {
                if (Enum.TryParse(t.Trim(), out CaroRelicType relic)
                    && !_equippedRelics.Contains(relic)
                    && _equippedRelics.Count < MaxRelicSlots)
                {
                    _equippedRelics.Add(relic);
                }
            }
        }

        private void SaveEquippedRelics()
        {
            var tokens = new List<string>(EquippedRelics.Count);
            foreach (CaroRelicType r in EquippedRelics)
                tokens.Add(r.ToString());
            SetString("XO_Rogue_EquippedRelics", string.Join(",", tokens));
            SaveStorage();
        }
    }
}

