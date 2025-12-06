using System.Collections.Generic;
using System.Linq;

namespace Roguelike.Data
{
    /// <summary>
    /// A data container that holds all possible RelicData templates for the game.
    /// Acts as a central database for relics
    /// </summary>
    public class RelicPool
    {
        /// <summary>
        /// The primary storage for all relics, indexed by their unique string ID for fast lookups.
        /// </summary>
        public Dictionary<string, RelicData> RelicsById { get; set; } = new Dictionary<string, RelicData>();
        
        /// <summary>
        /// Defines the gold cost range for relics of a specific rarity when they appear in a shop.
        /// The tuple represents (minimum cost, maximum cost)
        /// </summary>
        public Dictionary<Rarity, (int MinCost, int MaxCost)> CostRangesByRarity { get; set; } = new Dictionary<Rarity, (int, int)>();

        /// <summary>
        /// Gets a list of all relics matching a specific rarity.
        /// </summary>
        public List<RelicData> GetRelicsByRarity(Rarity rarity)
        {
            return RelicsById.Values.Where(relic => relic.Rarity == rarity).ToList();
        }

        /// <summary>
        /// Retrieves a relic by its unique ID.
        /// </summary>
        public RelicData GetRelic(string id)
        {
            RelicsById.TryGetValue(id, out var relic);
            return relic;
        }
    }
}