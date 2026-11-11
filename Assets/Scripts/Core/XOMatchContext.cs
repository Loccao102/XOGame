namespace XOPlayable.Core
{
    /// <summary>
    /// Everything that is remembered for the length of one Tower match: move counter, once-per-match relic triggers and the relic
    /// disabled by the Arm blind. A plain object so the rule engine can be given its own copy instead of reading global state.
    /// </summary>
    public sealed class XOMatchContext
    {
        public int PlayerMoves { get; set; }
        public bool TwinThreatTriggered { get; set; }
        public bool GuardianAegisTriggered { get; set; }
        public bool ShatterHammerTriggered { get; set; }
        public bool PhoenixSigilTriggered { get; set; }
        public CaroRelicType? DisabledRelic { get; set; }
    }
}
