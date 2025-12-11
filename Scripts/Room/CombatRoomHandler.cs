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
                run.CurrentState = GameState.OnMap;
                return;
            }

            run.CurrentCombat = new CombatManager(run.TheHero, enemyTemplates, run.Rng, run.EffectPool.GetEffect);
            run.CurrentCombat.StartCombat();
            run.CurrentState = GameState.InCombat;
        }

        /// <summary>
        /// A method for generating enemy encounters based on a star rating.
        /// </summary>
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
                case 1: // Easy monster
                    minionCount = run.Rng.Next(0, 2); // 0 or 1 minion
                    maxMinionStar = 1;
                    break;
                case 2: // Harder monster
                    minionCount = run.Rng.Next(1, 3); // 1 or 2 minions
                    maxMinionStar = 1;
                    break;
                case 3: // Easy elite
                    minionCount = run.Rng.Next(1, 2); // 1 minion
                    maxMinionStar = 2;
                    break;
                case 4: // Harder elite
                    minionCount = run.Rng.Next(1, 3); // 1 or 2 minions
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

            if (hero.Relics.Any(r => r.Id == "blood_vial"))
            {
                hero.Heal(2);
            }

            // Gold Formula: e^(3 + k*n) + 10 with 0 < k < 1
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
            
            if (hero.Relics.Any(r => r.Id == "prayer_wheel"))
            {
                int limit = (n == 1) ? 1 : n - 1;
                run.CardRewardChoices.Add(run.CardPool.GetRandomCardUpToStar(limit, run.Rng));
            }
            
            run.CardRewardChoices.RemoveAll(c => c == null);

            run.RelicRewardChoice = run.RelicPool.GetRandomRelicOfStar(n, run.Rng);
            
            if (room.Type == RoomType.Elite && hero.Relics.Any(r => r.Id == "black_star"))
            {
                var extraRelic = run.RelicPool.GetRandomRelicOfStar(n, run.Rng);
                if (extraRelic != null)
                {
                    hero.Relics.Add(extraRelic);
                }
            }

            run.CurrentState = GameState.AwaitingReward;
            run.CurrentCombat = null;
        }
    }
}