using Roguelike.Data;
using System;
using System.Collections.Generic;

namespace Roguelike.Logic
{
    public class ShopInventory
    {
        public List<ShopItem<CardData>> CardsForSale { get; } = new List<ShopItem<CardData>>();
        public List<ShopItem<RelicData>> RelicsForSale { get; } = new List<ShopItem<RelicData>>();

        public ShopInventory(CardPool cardPool, RelicPool relicPool, Random rng)
        {
            for (int star = 1; star <= 5; star++)
            {
                var card = cardPool.GetRandomCardOfStar(star, rng);
                if (card != null)
                {
                    int basePrice = 40 * star; 
                    int price = rng.Next(basePrice, basePrice + 30);
                    // TODO: Adjust price formula
                    CardsForSale.Add(new ShopItem<CardData>(card, price));
                }

                var relic = relicPool.GetRandomRelicOfStar(star, rng);
                if (relic != null)
                {
                    int basePrice = 60 * star;
                    int price = rng.Next(basePrice, basePrice + 50);
                    // TODO: Adjust price formula
                    RelicsForSale.Add(new ShopItem<RelicData>(relic, price));
                }
            }
        }
    }
}