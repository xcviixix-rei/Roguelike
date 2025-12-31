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

        private int _turnsSinceLastSpecial = 999; 

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
        /// Respects SpecialAbilityCooldown.
        /// </summary>
        public CombatActionData GetNextAction()
        {
            if (ActionBucket.Count == 0)
            {
                InitializeActionBucket();
            }

            // Try to find a valid action that meets cooldown requirements
            int checks = 0;
            int maxChecks = ActionBucket.Count + 1; // Safety limit

            while (checks < maxChecks)
            {
                var candidate = ActionBucket.Peek();
                bool isSpecial = candidate.Type == ActionType.ApplyStatusEffect || candidate.Type == ActionType.ApplyDeckEffect;
                
                // Check Cooldown: If special and not enough turns passed, skip it
                if (isSpecial && _turnsSinceLastSpecial < SourceEnemyData.SpecialAbilityCooldown)
                {
                    // Move to back
                    ActionBucket.Enqueue(ActionBucket.Dequeue()); 
                    checks++;
                }
                else
                {
                    // Valid action found
                    var action = ActionBucket.Dequeue();
                    if (isSpecial) 
                    {
                        _turnsSinceLastSpecial = 0;
                    }
                    return action;
                }
            }

            // Fallback: If no valid action found (e.g. all special and all on cooldown), just take the next one
            return ActionBucket.Dequeue();
        }

        public void TickCooldowns()
        {
            _turnsSinceLastSpecial++;
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
