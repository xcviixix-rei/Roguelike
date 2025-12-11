using System.Collections.Generic;
using System.Linq;
using System;

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
        /// A secondary index grouping cards by their star rating for quick access.
        /// </summary>
        public Dictionary<int, List<CardData>> CardsByStar { get; set; } = new Dictionary<int, List<CardData>>();

        /// <summary>
        /// Defines the base shop cost for cards at each star rating.
        /// </summary>
        public Dictionary<int, int> BaseShopCosts { get; set; } = new Dictionary<int, int>();

        /// <summary>
        /// Initializes the CardPool
        /// </summary>
        public void Initialize(IEnumerable<CardData> allCards)
        {
            CardsById.Clear();
            CardsByStar.Clear();
            
            foreach(var card in allCards)
            {
                CardsById[card.Id] = card;
                
                if (!CardsByStar.ContainsKey(card.StarRating))
                {
                    CardsByStar[card.StarRating] = new List<CardData>();
                }
                CardsByStar[card.StarRating].Add(card);
            }

            BaseShopCosts.Clear();
            for (int i = 1; i <= 5; i++)
            {
                BaseShopCosts[i] = 40 * i;
            }
        }

        /// <summary>
        /// Retrieves a card by its unique ID.
        /// </summary>
        public CardData GetCard(string id)
        {
            CardsById.TryGetValue(id, out var card);
            return card;
        }

        /// <summary>
        /// Retrieves a random card of the specified star rating.
        /// </summary>
        public CardData GetRandomCardOfStar(int star, System.Random rng)
        {
            if (CardsByStar.TryGetValue(star, out var list) && list.Any())
            {
                return list[rng.Next(list.Count)];
            }
            return null;
        }

        /// <summary>
        /// Gets a random card with a rating less than or equal to maxStar.
        /// </summary>
        public CardData GetRandomCardUpToStar(int maxStar, System.Random rng)
        {
            var validStars = CardsByStar.Keys.Where(k => k <= maxStar).ToList();
            if (!validStars.Any()) return null;

            int chosenStar = validStars[rng.Next(validStars.Count)];
            return GetRandomCardOfStar(chosenStar, rng);
        }
    }
}