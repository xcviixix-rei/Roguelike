// Roguelike/Logic/Handlers/CombatRoomHandler.cs

using Roguelike.Data;
using RoguelikeMapGen;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Roguelike.Logic.Handlers
{
    public class CombatRoomHandler : IRoomHandler
    {
        /// <summary>
        /// Sets up a combat encounter and transitions the game state.
        /// </summary>
        public void Execute(GameRun run, Room room)
        {
            var roomConfig = run.RoomConfigs[room.Type];
            var enemyTemplates = GenerateEncounter(run, roomConfig);

            if (!enemyTemplates.Any())
            {
                return;
            }

            run.CurrentCombat = new CombatManager(run.TheHero, enemyTemplates, run.Rng, run.EffectPool.GetEffect);
            run.CurrentCombat.StartCombat();
            
            run.CurrentState = GameState.InCombat;
        }

        /// <summary>
        /// Generates a list of enemies for an encounter, trying to match a target difficulty.
        /// </summary>
        private List<EnemyData> GenerateEncounter(GameRun run, RoomData config)
        {
            var encounter = new List<EnemyData>();
            float targetDifficulty = (config.MinValue + config.MaxValue) / 2.0f;
            float remainingDifficulty = targetDifficulty;
            const int MAX_ENEMIES = 4;

            var allPossibleEnemies = run.EnemyPool.EnemiesById.Values.ToList();

            while (encounter.Count < MAX_ENEMIES && remainingDifficulty > 0)
            {
                var validCandidates = allPossibleEnemies.Where(e => e.Difficulty <= remainingDifficulty).ToList();

                if (!validCandidates.Any())
                {
                    break;
                }

                var chosenEnemy = validCandidates[run.Rng.Next(validCandidates.Count)];
                encounter.Add(chosenEnemy);
                remainingDifficulty -= chosenEnemy.Difficulty;
            }
            return encounter;
        }

        /// <summary>
        /// Generates gold, card, and relic rewards after a victory.
        /// This method is called by the GameController after combat is won.
        /// </summary>
        public static void GenerateVictoryRewards(GameRun run)
        {
            float totalDifficulty = run.CurrentCombat.Enemies.Sum(e => e.SourceEnemyData.Difficulty);

            int goldReward = (int)Math.Floor(200.0 / (6.0 - totalDifficulty));
            run.TheHero.CurrentGold += goldReward;

            run.CardRewardChoices.Clear();
            int difficultyTier = (int)Math.Round(totalDifficulty);
            switch (difficultyTier)
            {
                case 1:
                    for (int i=0; i<3; i++) run.CardRewardChoices.Add(run.CardPool.GetRandomCardOfRarity(Rarity.Common, run.Rng));
                    break;
                case 2:
                    run.CardRewardChoices.Add(run.CardPool.GetRandomCardOfRarity(Rarity.Uncommon, run.Rng));
                    for (int i=0; i<2; i++) run.CardRewardChoices.Add(run.CardPool.GetRandomCardOfRarity(Rarity.Common, run.Rng));
                    break;
                case 3:
                    run.CardRewardChoices.Add(run.CardPool.GetRandomCardOfRarity(Rarity.Rare, run.Rng));
                    for (int i=0; i<2; i++) run.CardRewardChoices.Add(run.CardPool.GetRandomCardUpToRarity(Rarity.Uncommon, run.Rng));
                    break;
                case 4:
                    run.CardRewardChoices.Add(run.CardPool.GetRandomCardOfRarity(Rarity.Legendary, run.Rng));
                    for (int i=0; i<2; i++) run.CardRewardChoices.Add(run.CardPool.GetRandomCardUpToRarity(Rarity.Rare, run.Rng));
                    break;
                default: // Difficulty 5+
                    for (int i=0; i<2; i++) run.CardRewardChoices.Add(run.CardPool.GetRandomCardOfRarity(Rarity.Legendary, run.Rng));
                    run.CardRewardChoices.Add(run.CardPool.GetRandomCardUpToRarity(Rarity.Rare, run.Rng));
                    break;
            }
            run.CardRewardChoices.RemoveAll(c => c == null);

            Rarity relicRarity = Rarity.Common;
            switch (difficultyTier)
            {
                case 2: if (run.Rng.Next(3) == 0) relicRarity = Rarity.Uncommon; break;
                case 3: relicRarity = (run.Rng.Next(3) == 0) ? Rarity.Rare : Rarity.Uncommon; break;
                case 4: relicRarity = (run.Rng.Next(3) == 0) ? Rarity.Legendary : Rarity.Rare; break;
                case 5: relicRarity = Rarity.Legendary; break;
            }
            run.RelicRewardChoice = run.RelicPool.GetRandomRelicOfRarity(relicRarity, run.Rng);

            run.CurrentState = GameState.AwaitingReward;
            run.CurrentCombat = null;
        }
    }
}