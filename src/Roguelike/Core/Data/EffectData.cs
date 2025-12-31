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
        /// For Flat: direct bonus/penalty (e.g., Strength: 3 = +3 damage)
        /// For Percentage: percentage value (e.g., Vulnerable: 50 = 50% more damage = 1.5x multiplier)
        /// </summary>
        public int Intensity { get; set; }

        /// <summary>
        /// How the intensity value should be interpreted (Flat addition or Percentage multiplier)
        /// </summary>
        public IntensityType IntensityType { get; set; }

        /// <summary>
        /// How many turns the effect lasts (only used with DecayType.AfterXTURNS)
        /// Default: 1 turn
        /// Use large values (e.g., 999) for effects that should last entire combat
        /// </summary>
        public int Duration { get; set; } = 1;

        /// <summary>
        /// When the effect is applied
        /// </summary>
        public ApplyType ApplyType { get; set; }

        /// <summary>
        /// How long the effect lasts
        /// </summary>
        public DecayType Decay { get; set; }

        /// <summary>
        /// The default target for this effect. This can be overridden by the
        /// CombatActionData that applies it
        /// </summary>
        public TargetType Target { get; set; }
    }
}
