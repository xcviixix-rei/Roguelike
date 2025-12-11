// Roguelike/Data/EnemyData.cs

using System.Collections.Generic;

namespace Roguelike.Data
{
    /// <summary>
    /// Data container for an enemy's template.
    /// Inherits base properties from CombatantData.
    /// </summary>
    public class EnemyData : CombatantData
    {
        /// <summary>
        /// A numerical rating of the enemy's power level, from 1 to 5.
        /// This is used for encounter generation and reward calculation.
        /// </summary>
        public int StarRating { get; set; }
        
        /// <summary>
        /// Indicates if this enemy is a boss-type enemy
        /// </summary>
        public bool IsBoss { get; set; } = false;

        /// <summary>
        /// This list defines the *proportions* for the enemy's bucket filll
        /// </summary>
        public List<WeightedChoice<CombatActionData>> ActionSet { get; set; } = new List<WeightedChoice<CombatActionData>>();

        /// <summary>
        /// The minimum number of turns that must pass between an enemy using a "special"
        /// non-Attack/Block action (e.g., applying a powerful buff or debuff). A value of 0 means no cooldown.
        /// The Core Logic will enforce this rule when building/drawing from the action bucket
        /// </summary>
        public int SpecialAbilityCooldown { get; set; } = 1;
    }
}