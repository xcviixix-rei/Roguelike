using Roguelike.Data;
using System;
using System.Collections.Generic;

namespace Roguelike.Core
{
    /// <summary>
    /// Represents an enemy combatant, holding its runtime state and AI logic.
    /// </summary>
    public class Enemy : Combatant
    {
        public EnemyData SourceEnemyData => (EnemyData)SourceData;
        
        private readonly Random rng;
        public Queue<CombatActionData> ActionBucket { get; private set; } = new Queue<CombatActionData>();

        public Enemy(EnemyData sourceData, Random rng) : base(sourceData)
        {
            this.rng = rng;
            InitializeActionBucket();
        }

        /// <summary>
        /// Fills the action bucket based on the weights in the enemy's data template.
        /// </summary>
        public void InitializeActionBucket()
        {
            ActionBucket.Clear();
            var actionsToShuffle = new List<CombatActionData>();

            foreach (var weightedAction in SourceEnemyData.ActionSet)
            {
                for (int i = 0; i < weightedAction.Weight; i++)
                {
                    actionsToShuffle.Add(weightedAction.Item);
                }
            }
            
            Shuffle(actionsToShuffle);

            foreach (var action in actionsToShuffle)
            {
                ActionBucket.Enqueue(action);
            }
        }
        
        /// <summary>
        /// Retrieves the next action from the bucket, refilling it if necessary.
        /// </summary>
        public CombatActionData GetNextAction()
        {
            if (ActionBucket.Count == 0)
            {
                InitializeActionBucket();
            }
            return ActionBucket.Dequeue();
        }

        /// <summary>
        /// Returns the upcoming action without consuming it from the bucket.
        /// </summary>
        public CombatActionData PeekNextAction()
        {
            if (ActionBucket.Count == 0)
            {
                InitializeActionBucket();
            }
            return ActionBucket.Peek();
        }

        /// <summary>
        /// Shuffles a list using the Fisher-Yates algorithm.
        /// </summary>
        private void Shuffle(List<CombatActionData> list)
        {
            int n = list.Count;
            while (n > 1)
            {
                n--;
                int k = rng.Next(n + 1);
                var value = list[k];
                list[k] = list[n];
                list[n] = value;
            }
        }
    }
}
