using Roguelike.Data;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Roguelike.Logic
{
    /// <summary>
    /// Represents the generated inventory for a single shop visit.
    /// </summary>
    public class ShopInventory
    {
        public List<ShopItem<CardData>> CardsForSale { get; } = new List<ShopItem<CardData>>();
        public List<ShopItem<RelicData>> RelicsForSale { get; } = new List<ShopItem<RelicData>>();

        public ShopInventory(CardPool cardPool, RelicPool relicPool, Random rng)
        {
            foreach (Rarity rarity in Enum.GetValues(typeof(Rarity)))
            {
                if (rarity == Rarity.Boss) continue;

                // Generate Card
                var card = cardPool.GetRandomCardOfRarity(rarity, rng);
                if (card != null && cardPool.CostRangesByRarity.TryGetValue(rarity, out var costRange))
                {
                    int price = rng.Next(costRange.MinCost, costRange.MaxCost + 1);
                    CardsForSale.Add(new ShopItem<CardData>(card, price));
                }
                
                // Generate Relic
                var relic = relicPool.GetRandomRelicOfRarity(rarity, rng);
                if (relic != null && relicPool.CostRangesByRarity.TryGetValue(rarity, out var costRangeRelic))
                {
                    int price = rng.Next(costRangeRelic.MinCost, costRangeRelic.MaxCost + 1);
                    RelicsForSale.Add(new ShopItem<RelicData>(relic, price));
                }
            }
        }
    }
}