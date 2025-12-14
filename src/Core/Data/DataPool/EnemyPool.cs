using System.Collections.Generic;
using System.Linq;
using System;

namespace Roguelike.Data
{
    /// <summary>
    /// A data container that holds all possible EnemyData templates for the game.
    /// Acts as a central database for enemies
    /// </summary>
    public class EnemyPool
    {
        /// <summary>
        /// The primary storage for all enemies, indexed by their unique string ID for fast lookups
        /// </summary>
        public Dictionary<string, EnemyData> EnemiesById { get; set; } = new Dictionary<string, EnemyData>();

        /// <summary>
        /// A secondary index grouping enemies by their star rating for quick access.
        /// </summary>
        public Dictionary<int, List<EnemyData>> EnemiesByStar { get; set; } = new Dictionary<int, List<EnemyData>>();
        
        /// <summary>
        /// Initializes the EnemyPool
        /// </summary>
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
        
        /// <summary>
        /// Retrieves an enemy by its unique ID.
        /// </summary>
        public EnemyData GetEnemy(string id)
        {
            EnemiesById.TryGetValue(id, out var enemy);
            return enemy;
        }

        /// <summary>
        /// Gets a random enemy of a specific Star Rating.
        /// </summary>
        public EnemyData GetRandomEnemyOfStar(int star, Random rng)
        {
            if (EnemiesByStar.TryGetValue(star, out var list) && list.Any())
            {
                return list[rng.Next(list.Count)];
            }
            if (star > 1) return GetRandomEnemyOfStar(star - 1, rng);
            return null;
        }
        
        /// <summary>
        /// Gets a random enemy with Star Rating strictly less than the provided value.
        /// Used for filling out encounters (minions).
        /// </summary>
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
