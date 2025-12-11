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
                if (card != null && cardPool.BaseShopCosts.TryGetValue(star, out int baseCardCost))
                {
                    int price = rng.Next(baseCardCost, baseCardCost + 30);
                    CardsForSale.Add(new ShopItem<CardData>(card, price));
                }

                var relic = relicPool.GetRandomRelicOfStar(star, rng);
                if (relic != null && relicPool.BaseShopCosts.TryGetValue(star, out int baseRelicCost))
                {
                    int price = rng.Next(baseRelicCost, baseRelicCost + 50);
                    RelicsForSale.Add(new ShopItem<RelicData>(relic, price));
                }
            }
        }
    }
}