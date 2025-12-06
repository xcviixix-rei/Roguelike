namespace Roguelike.Data
{
    /// <summary>
    /// Represents a single outcome or consequence of an EventChoice
    /// </summary>
    public class EventEffect
    {
        /// <summary>
        /// The type of effect that will occur.
        /// </summary>
        public EventEffectType Type { get; set; }

        /// <summary>
        /// The numerical value associated with the effect
        /// </summary>
        public int Value { get; set; }

        /// <summary>
        /// An optional string parameter for more complex effects.
        /// For GainCard/GainRelic, this could specify a rarity ("Rare") or a specific item ID
        /// </summary>
        public string Parameter { get; set; }
    }
}