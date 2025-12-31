using System.Collections.Generic;

namespace Roguelike.Data
{
    /// <summary>
    /// Data container for a single card's template.
    /// This class defines a card's properties and the actions it performs when played.
    /// It is a pure data object and contains no game logic
    /// </summary>
    public class CardData
    {
        /// <summary>
        /// A unique, machine-readable identifier for this card (e.g., "player_strike").
        /// This is used for looking up cards and saving deck lists
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// The display name of the card
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// The descriptive text that appears on the card, explaining its effect.
        /// This may contain placeholders like "Deal {0} damage." that are dynamically filled
        /// from the 'Actions' list by the presentation/UI layer
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// The amount of mana required to play this card
        /// </summary>
        public int ManaCost { get; set; }

        /// <summary>
        /// The power level of the card, from 1 to 5.
        /// </summary>
        public int StarRating { get; set; }

        /// <summary>
        /// The classification of the card
        /// </summary>
        public CardType Type { get; set; }

        /// <summary>
        /// The list of combat actions this card executes when played
        /// </summary>
        public List<CombatActionData> Actions { get; set; } = new List<CombatActionData>();

        /// <summary>
        /// Parameterless constructor, useful for serialization frameworks.
        /// </summary>
        public CardData() { }
    }
}
