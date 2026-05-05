using System.Collections.Generic;
using System.Linq;
using System;

namespace Roguelike.Data
{


    public class CardPool
    {


        public Dictionary<string, CardData> CardsById { get; set; } = new Dictionary<string, CardData>();


        public Dictionary<int, List<CardData>> CardsByStar { get; set; } = new Dictionary<int, List<CardData>>();


        public Dictionary<int, int> BaseShopCosts { get; set; } = new Dictionary<int, int>();


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


        public CardData GetCard(string id)
        {
            CardsById.TryGetValue(id, out var card);
            return card;
        }


        public CardData GetRandomCardOfStar(int star, System.Random rng)
        {
            if (CardsByStar.TryGetValue(star, out var list) && list.Any())
            {
                return list[rng.Next(list.Count)];
            }
            return null;
        }


        public CardData GetRandomCardUpToStar(int maxStar, System.Random rng)
        {
            var validStars = CardsByStar.Keys.Where(k => k <= maxStar).ToList();
            if (!validStars.Any()) return null;

            int chosenStar = validStars[rng.Next(validStars.Count)];
            return GetRandomCardOfStar(chosenStar, rng);
        }
    }
}
