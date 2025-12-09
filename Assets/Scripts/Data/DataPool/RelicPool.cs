using System.Collections.Generic;
using System.Linq;
using System;

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
        /// A secondary index grouping relics by their star rating for quick access.
        /// </summary>
        public Dictionary<int, List<RelicData>> RelicsByStar { get; set; } = new Dictionary<int, List<RelicData>>();

        /// <summary>
        /// Defines the min and max gold cost ranges for relics at each star rating.
        /// </summary>
        public Dictionary<int, (int MinCost, int MaxCost)> CostRangesByStar { get; set; } = new Dictionary<int, (int, int)>();

        /// <summary>
        /// Initializes the RelicPool
        /// </summary>
        public void Initialize(IEnumerable<RelicData> allRelics)
        {
            RelicsById.Clear();
            RelicsByStar.Clear();

            foreach(var relic in allRelics)
            {
                RelicsById[relic.Id] = relic;
                
                if (!RelicsByStar.ContainsKey(relic.StarRating))
                {
                    RelicsByStar[relic.StarRating] = new List<RelicData>();
                }
                RelicsByStar[relic.StarRating].Add(relic);
            }
        }

        /// <summary>
        /// Retrieves a relic by its unique ID.
        /// </summary>
        public RelicData GetRelic(string id)
        {
            RelicsById.TryGetValue(id, out var relic);
            return relic;
        }

        /// <summary>
        /// Retrieves a random relic of the specified star rating.
        /// </summary>
        public RelicData GetRandomRelicOfStar(int star, System.Random rng)
        {
            if (RelicsByStar.TryGetValue(star, out var list) && list.Any())
            {
                return list[rng.Next(list.Count)];
            }
            return null;
        }
    }
}