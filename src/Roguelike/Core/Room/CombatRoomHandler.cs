using Roguelike.Data;
using Roguelike.Core.Map;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Roguelike.Core.Handlers
{
    public class CombatRoomHandler : IRoomHandler
    {
        public void Execute(GameRun run, Room room)
        {
            var enemyTemplates = GenerateEncounter(run, room.StarRating);

            if (!enemyTemplates.Any())
            {
                run.CurrentState = GameState.OnMap;
                return;
            }

            run.CurrentCombat = new CombatManager(run.TheHero, enemyTemplates, run.Rng, run.EffectPool.GetEffect);
            run.CurrentCombat.StartCombat();
            run.CurrentState = GameState.InCombat;
        }


        private List<EnemyData> GenerateEncounter(GameRun run, int starRating)
        {
            var encounter = new List<EnemyData>();

            var leader = run.EnemyPool.GetRandomEnemyOfStar(starRating, run.Rng);
            if (leader == null)
            {
                Console.WriteLine($"CRITICAL ERROR: No enemies found for Star Rating {starRating}. Aborting encounter generation.");
                return encounter;
            }
            encounter.Add(leader);

            int minionCount = 0;
            int maxMinionStar = 1;

            switch (starRating)
            {
                case 1:
                    minionCount = 0;
                    maxMinionStar = 1;
                    break;
                case 2:
                    minionCount = run.Rng.Next(1, 3);
                    maxMinionStar = 1;
                    break;
                case 3:
                    minionCount = run.Rng.Next(1, 2);
                    maxMinionStar = 2;
                    break;
                case 4:
                    minionCount = run.Rng.Next(1, 3);
                    maxMinionStar = 3;
                    break;
            }

            for (int i = 0; i < minionCount; i++)
            {
                var minion = run.EnemyPool.GetRandomEnemyBelowStar(maxMinionStar + 1, run.Rng);
                if (minion != null)
                {
                    encounter.Add(minion);
                }
            }

            return encounter;
        }

        public static void GenerateVictoryRewards(GameRun run)
        {
            var room = run.TheMap.GetCurrentRoom();
            var hero = run.TheHero;
            int n = room.StarRating;


            double goldCalc = Math.Exp(3 + (0.5 * n)) + 10;
            int goldReward = (int)Math.Floor(goldCalc);
            hero.CurrentGold += goldReward;

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
