using Roguelike.Data;
using RoguelikeMapGen;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Roguelike.Logic.Handlers
{
    public class CombatRoomHandler : IRoomHandler
    {
        public void Execute(GameRun run, Room room)
        {
            var enemyTemplates = GenerateEncounter(run, room.StarRating);

            if (!enemyTemplates.Any())
            {
                return;
            }

            run.CurrentCombat = new CombatManager(run.TheHero, enemyTemplates, run.Rng, run.EffectPool.GetEffect);
            run.CurrentCombat.StartCombat();
            run.CurrentState = GameState.InCombat;
        }

        private List<EnemyData> GenerateEncounter(GameRun run, int starRating)
        {
            var encounter = new List<EnemyData>();
            
            int enemyCount = 6 - starRating; // Pyramid enemy count

            var leader = run.EnemyPool.GetRandomEnemyOfStar(starRating, run.Rng);
            if (leader != null)
            {
                encounter.Add(leader);
            }
            else
            {
                var fallback = run.EnemyPool.GetRandomEnemyBelowStar(starRating + 1, run.Rng);
                if (fallback != null) encounter.Add(fallback);
            }

            int minionStarLimit = (starRating == 1) ? 2 : starRating;

            while (encounter.Count < enemyCount)
            {
                var minion = run.EnemyPool.GetRandomEnemyBelowStar(minionStarLimit, run.Rng);
                if (minion != null)
                {
                    encounter.Add(minion);
                }
                else
                {
                    break;
                }
            }

            return encounter;
        }

        public static void GenerateVictoryRewards(GameRun run)
        {
            var room = run.TheMap.GetCurrentRoom();
            int n = room.StarRating;

            // Gold Formula: e^(3 + k*n) + 10 with 0 < k < 1
            double goldCalc = Math.Exp(3 + (0.5 * n)) + 10;
            int goldReward = (int)Math.Floor(goldCalc);
            run.TheHero.CurrentGold += goldReward;

            run.CardRewardChoices.Clear();
            
            run.CardRewardChoices.Add(run.CardPool.GetRandomCardOfStar(n, run.Rng));

            int lowerStarLimit = (n == 1) ? 1 : n; 
            for (int i = 0; i < 2; i++)
            {
                int limit = (n == 1) ? 1 : n - 1;
                run.CardRewardChoices.Add(run.CardPool.GetRandomCardUpToStar(limit, run.Rng));
            }
            
            run.CardRewardChoices.RemoveAll(c => c == null);

            run.RelicRewardChoice = run.RelicPool.GetRandomRelicOfStar(n, run.Rng);

            run.CurrentState = GameState.AwaitingReward;
            run.CurrentCombat = null;
        }
    }
}