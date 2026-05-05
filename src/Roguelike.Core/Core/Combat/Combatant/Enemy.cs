using Roguelike.Data;
using System;
using System.Collections.Generic;

namespace Roguelike.Core
{


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


        public CombatActionData GetNextAction()
        {
            if (ActionBucket.Count == 0)
            {
                InitializeActionBucket();
            }


            int checks = 0;
            int maxChecks = ActionBucket.Count + 1;

            while (checks < maxChecks)
            {
                var candidate = ActionBucket.Peek();
                bool isSpecial = candidate.Type == ActionType.ApplyStatusEffect || candidate.Type == ActionType.ApplyDeckEffect;


                if (isSpecial && _turnsSinceLastSpecial < SourceEnemyData.SpecialAbilityCooldown)
                {

                    ActionBucket.Enqueue(ActionBucket.Dequeue());
                    checks++;
                }
                else
                {

                    var action = ActionBucket.Dequeue();
                    if (isSpecial)
                    {
                        _turnsSinceLastSpecial = 0;
                    }
                    return action;
                }
            }


            return ActionBucket.Dequeue();
        }

        public void TickCooldowns()
        {
            _turnsSinceLastSpecial++;
        }


        public CombatActionData PeekNextAction()
        {
            if (ActionBucket.Count == 0)
            {
                InitializeActionBucket();
            }
            return ActionBucket.Peek();
        }


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
