using System.Collections.Generic;
using System.Linq;

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
        /// Gets a list of all enemies that fall within a given difficulty range (inclusive).
        /// </summary>
        /// <param name="minDifficulty">The minimum difficulty.</param>
        /// <param name="maxDifficulty">The maximum difficulty.</param>
        public List<EnemyData> GetEnemiesInDifficultyRange(float minDifficulty, float maxDifficulty)
        {
            return EnemiesById.Values.Where(enemy => enemy.Difficulty >= minDifficulty && enemy.Difficulty <= maxDifficulty).ToList();
        }
        
        /// <summary>
        /// Retrieves an enemy by its unique ID.
        /// </summary>
        public EnemyData GetEnemy(string id)
        {
            EnemiesById.TryGetValue(id, out var enemy);
            return enemy;
        }
    }
}