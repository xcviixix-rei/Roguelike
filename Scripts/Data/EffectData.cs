namespace Roguelike.Data
{
    /// <summary>
    /// Abstract base class for all special effects in the game (both status and deck effects).
    /// It holds the common data shared by all effects
    /// </summary>
    public abstract class EffectData
    {
        /// <summary>
        /// A unique, machine-readable identifier for this effect (e.g., "vulnerable_debuff").
        /// This is used to look up the effect from a dictionary or data pool
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// The display name of the effect (e.g., "Vulnerable")
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// A description of what the effect does, for UI tooltips
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// The magnitude or potency of the effect.
        /// For StatusEffects, this is the number of stacks (e.g., 2 stacks of Strength).
        /// For DeckEffects, this is the number of cards (e.g., Draw 3 cards)
        /// </summary>
        public int Value { get; set; }

        /// <summary>
        /// How long the effect lasts.
        /// </summary>
        public DecayType Decay { get; set; }

        /// <summary>
        /// The default target for this effect. This can be overridden by the
        /// CombatActionData that applies it
        /// </summary>
        public TargetType Target { get; set; }
    }
}