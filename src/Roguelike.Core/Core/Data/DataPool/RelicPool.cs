using System.Collections.Generic;
using System.Linq;
using System;

namespace Roguelike.Data
{


    public class RelicPool
    {


        public Dictionary<string, RelicData> RelicsById { get; set; } = new Dictionary<string, RelicData>();


        public Dictionary<int, List<RelicData>> RelicsByStar { get; set; } = new Dictionary<int, List<RelicData>>();


        public Dictionary<int, int> BaseShopCosts { get; set; } = new Dictionary<int, int>();


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

            BaseShopCosts.Clear();
            for (int i = 1; i <= 5; i++)
            {
                BaseShopCosts[i] = 60 * i;
            }
        }


        public RelicData GetRelic(string id)
        {
            RelicsById.TryGetValue(id, out var relic);
            return relic;
        }


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
