using System.Collections.Generic;
using System.Linq;

namespace Roguelike.Data
{
    /// <summary>
    /// A data container that holds all possible CardData templates for the game.
    /// Acts as a central database for cards
    /// </summary>
    public class CardPool
    {
        /// <summary>
        /// The primary storage for all cards, indexed by their unique string ID for fast lookups.
        /// </summary>
        public Dictionary<string, CardData> CardsById { get; set; } = new Dictionary<string, CardData>();

        /// <summary>
        /// Defines the gold cost range for cards of a specific rarity when they appear in a shop.
        /// The tuple represents (minimum cost, maximum cost).
        /// </summary>
        public Dictionary<Rarity, (int MinCost, int MaxCost)> CostRangesByRarity { get; set; } = new Dictionary<Rarity, (int, int)>();


        /// <summary>
        /// Gets a list of all cards matching a specific rarity.
        /// </summary>
        public List<CardData> GetCardsByRarity(Rarity rarity)
        {
            return CardsById.Values.Where(card => card.Rarity == rarity).ToList();
        }

        /// <summary>
        /// Retrieves a card by its unique ID.
        /// </summary>
        public CardData GetCard(string id)
        {
            CardsById.TryGetValue(id, out var card);
            return card;
        }
    }
}