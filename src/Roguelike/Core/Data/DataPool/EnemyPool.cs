using System.Collections.Generic;
using System.Linq;
using System;

namespace Roguelike.Data
{


    public class EnemyPool
    {


        public Dictionary<string, EnemyData> EnemiesById { get; set; } = new Dictionary<string, EnemyData>();


        public Dictionary<int, List<EnemyData>> EnemiesByStar { get; set; } = new Dictionary<int, List<EnemyData>>();


        public void Initialize(IEnumerable<EnemyData> allEnemies)
        {
            EnemiesById.Clear();
            EnemiesByStar.Clear();

            foreach(var enemy in allEnemies)
            {
                EnemiesById[enemy.Id] = enemy;

                if (!EnemiesByStar.ContainsKey(enemy.StarRating))
                {
                    EnemiesByStar[enemy.StarRating] = new List<EnemyData>();
                }
                EnemiesByStar[enemy.StarRating].Add(enemy);
            }
        }


        public EnemyData GetEnemy(string id)
        {
            EnemiesById.TryGetValue(id, out var enemy);
            return enemy;
        }


        public EnemyData GetRandomEnemyOfStar(int star, Random rng)
        {
            if (EnemiesByStar.TryGetValue(star, out var list) && list.Any())
            {
                return list[rng.Next(list.Count)];
            }
            if (star > 1) return GetRandomEnemyOfStar(star - 1, rng);
            return null;
        }


        public EnemyData GetRandomEnemyBelowStar(int starLimit, System.Random rng)
        {
            var validStars = EnemiesByStar.Keys.Where(k => k < starLimit).ToList();

            if (!validStars.Any())
            {
                if (EnemiesByStar.Count > 0)
                {
                    int min = EnemiesByStar.Keys.Min();
                    return GetRandomEnemyOfStar(min, rng);
                }
                return null;
            }

            int chosenStar = validStars[rng.Next(validStars.Count)];
            return GetRandomEnemyOfStar(chosenStar, rng);
        }
    }
}
