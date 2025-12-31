using System.Collections.Generic;

namespace Roguelike.Data
{
    /// <summary>
    /// Data container for the hero's starting template.
    /// Inherits base properties from CombatantData
    /// </summary>
    public class HeroData : CombatantData
    {
        /// <summary>
        /// The amount of gold the hero starts the run with
        /// </summary>
        public int StartingGold { get; set; }

        /// <summary>
        /// The amount of mana the hero has each turn
        /// </summary>
        public int StartingMana { get; set; }

        /// <summary>
        /// The number of cards the hero draws at the start of each turn
        /// </summary>
        public int StartingHandSize { get; set; }

        /// <summary>
        /// A list of card IDs that make up the hero's starting deck
        /// </summary>
        public List<string> StartingDeckCardIds { get; set; } = new List<string>();

        /// <summary>
        /// The ID of the relic the hero starts the run with
        /// </summary>
        public string StartingRelicId { get; set; }
    }
}
