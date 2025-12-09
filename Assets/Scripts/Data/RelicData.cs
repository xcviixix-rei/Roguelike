using System.Collections.Generic;

namespace Roguelike.Data
{
    /// <summary>
    /// Data container for a single relic's template.
    /// Relics provide passive, ongoing effects to the hero
    /// </summary>
    public class RelicData
    {
        /// <summary>
        /// A unique, machine-readable identifier for this relic (e.g., "ancient_potion")
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// The display name of the relic
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// The descriptive text that appears for the relic, explaining its effect
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// The power level of the relic, from 1 to 5 (or higher for Boss relics).
        /// </summary>
        public int StarRating { get; set; }

        /// <summary>
        /// Indicates if this relic is a boss relic, which are typically more powerful and only obtained after defeating a boss.
        /// </summary>
        public bool IsBossRelic { get; set; } = false;

        /// <summary>
        /// The list of passive effects this relic grants.
        /// For relics, the DecayType on these effects will typically be 'Permanent'.
        /// The logic for when and how these effects trigger (e.g., "at the start of combat")
        /// will be handled by the Core Logic module, but the data for the effect itself is stored here.
        /// </summary>
        public List<EffectData> Effects { get; set; } = new List<EffectData>();

        /// <summary>
        /// Parameterless constructor, useful for serialization frameworks.
        /// </summary>
        public RelicData() { }
    }
}