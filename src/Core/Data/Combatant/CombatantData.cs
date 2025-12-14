namespace Roguelike.Data
{
    /// <summary>
    /// An abstract base class for any entity that participates in combat (Heroes and Enemies).
    /// This class contains the properties common to all combatants
    /// </summary>
    public abstract class CombatantData
    {
        /// <summary>
        /// A unique, machine-readable identifier
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// The display name of the combatant
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// The base health of the combatant at the start of a run (for heroes) or combat (for enemies)
        /// </summary>
        public int StartingHealth { get; set; }

        /// <summary>
        /// The base strength of the combatant. Most will start at 0
        /// </summary>
        public int StartingStrength { get; set; } = 0;
    }
}
