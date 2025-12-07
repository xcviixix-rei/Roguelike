using Roguelike.Data;
using RoguelikeMapGen;
using System.Collections.Generic;
using System.Linq;
using System;

namespace Roguelike.Logic.Handlers
{
    public class BossRoomHandler : IRoomHandler
    {
        public void Execute(GameRun run, Room room)
        {
            var bossTemplates = GenerateBossEncounter(run);

            if (!bossTemplates.Any())
            {
                return;
            }

            run.CurrentCombat = new CombatManager(run.TheHero, bossTemplates, run.Rng, run.EffectPool.GetEffect);
            run.CurrentCombat.StartCombat();
            
            run.CurrentState = GameState.InCombat;
        }

        private List<EnemyData> GenerateBossEncounter(GameRun run)
        {
            var bosses = run.EnemyPool.GetEnemiesInDifficultyRange(5.0f, 10.0f);
            
            if (!bosses.Any()) return new List<EnemyData>();

            int index = run.Rng.Next(bosses.Count);
            return new List<EnemyData> { bosses[index] };
        }
        
        /// <summary>
        /// Generates Boss-tier rewards. Called by GameController after victory.
        /// </summary>
        public static void GenerateBossRewards(GameRun run)
        {
            int goldReward = (int)Math.Floor(200.0 / (6.0 - 5.0)); // TODO: Change to global config as normal combat room rewards
            run.TheHero.CurrentGold += goldReward;

            run.CardRewardChoices.Clear();
            
            for (int i=0; i<2; i++) run.CardRewardChoices.Add(run.CardPool.GetRandomCardOfRarity(Rarity.Legendary, run.Rng));
            run.CardRewardChoices.Add(run.CardPool.GetRandomCardUpToRarity(Rarity.Rare, run.Rng));

            run.RelicRewardChoice = run.RelicPool.GetRandomRelicOfRarity(Rarity.Boss, run.Rng);

            run.CurrentState = GameState.AwaitingReward;
            run.CurrentCombat = null;
        }
    }
}