using Roguelike.Data;
using Roguelike.Core;
using System;
using System.Collections.Generic;

namespace Roguelike.Optimization
{
    public static class GenomeApplicator
    {
        public static void Apply(BalanceGenome genome, EnemyPool enemyPool, CardPool cardPool, RelicPool relicPool, EffectPool effectPool, HeroData hero)
        {
            ApplyGlobalEconomy(genome, cardPool, relicPool);
            ApplyHeroStats(genome, hero);
            ApplyToEffects(genome, effectPool);
            ApplyToCards(genome, cardPool);
            ApplyToEnemies(genome, enemyPool);
        }

        private static void ApplyGlobalEconomy(BalanceGenome genome, CardPool cards, RelicPool relics)
        {
            for (int i = 1; i <= 5; i++)
            {
                if (cards.BaseShopCosts.ContainsKey(i))
                    cards.BaseShopCosts[i] = (int)(cards.BaseShopCosts[i] * genome.ShopPriceScalars[i]);

                if (relics.BaseShopCosts.ContainsKey(i))
                    relics.BaseShopCosts[i] = (int)(relics.BaseShopCosts[i] * genome.ShopPriceScalars[i]);
            }
        }

        private static void ApplyHeroStats(BalanceGenome genome, HeroData hero)
        {
            hero.StartingHealth = (int)(hero.StartingHealth * genome.HeroHealthScalar);
            if (hero.StartingHealth < 1) hero.StartingHealth = 1;

            hero.StartingGold = (int)(hero.StartingGold * genome.HeroStartGoldScalar);
            
            hero.StartingMana += genome.HeroManaOffset;
            if (hero.StartingMana < 1) hero.StartingMana = 1;
        }

        public static void ApplyRestHealing(BalanceGenome genome, GameRun run)
        {
            float baseHealPercentage = 0.30f;
            float adjustedHealPercentage = baseHealPercentage * genome.RestHealingScalar;
            
            int healAmount = (int)Math.Floor(run.TheHero.MaxHealth * adjustedHealPercentage);
            run.TheHero.Heal(healAmount);
        }

        private static void ApplyToEffects(BalanceGenome genome, EffectPool pool)
        {
            foreach (var effect in pool.EffectsById.Values)
            {
                if (genome.EffectValueScalars.TryGetValue(effect.Id, out float scalar))
                {
                    effect.Value = (int)Math.Round(effect.Value * scalar);
                    if (effect.Value < 1) effect.Value = 1;
                }
            }
        }

        private static void ApplyToCards(BalanceGenome genome, CardPool pool)
        {
            foreach (var card in pool.CardsById.Values)
            {
                // Cost Modifier
                if (genome.CardCostModifiers.TryGetValue(card.Id, out int costMod))
                {
                    card.ManaCost += costMod;
                    if (card.ManaCost < 0) card.ManaCost = 0;
                }

                // Action Values
                if (genome.CardActionScalars.TryGetValue(card.Id, out var scalars))
                {
                    for (int i = 0; i < card.Actions.Count && i < scalars.Count; i++)
                    {
                        var action = card.Actions[i];
                        action.Value = (int)Math.Round(action.Value * scalars[i]);
                    }
                }
            }
        }

        private static void ApplyToEnemies(BalanceGenome genome, EnemyPool pool)
        {
            foreach (var enemy in pool.EnemiesById.Values)
            {
                //  Health
                if (genome.EnemyHealthScalars.TryGetValue(enemy.Id, out float hpScalar))
                {
                    enemy.StartingHealth = (int)(enemy.StartingHealth * hpScalar);
                    if (enemy.StartingHealth < 1) enemy.StartingHealth = 1;
                }

                bool hasWeights = genome.EnemyActionWeightScalars.TryGetValue(enemy.Id, out var weightScalars);
                bool hasValues = genome.EnemyActionValueScalars.TryGetValue(enemy.Id, out var valueScalars);

                if (hasWeights || hasValues)
                {
                    for (int i = 0; i < enemy.ActionSet.Count; i++)
                    {
                        var choice = enemy.ActionSet[i];

                        // Apply Weight Scalar
                        if (hasWeights && i < weightScalars.Count)
                        {
                            choice.Weight = Math.Max(1, (int)Math.Round(choice.Weight * weightScalars[i]));
                        }

                        // Apply Value Scalar
                        if (hasValues && i < valueScalars.Count)
                        {
                            float valScalar = valueScalars[i];
                            if (choice.Item.Value > 0)
                            {
                                choice.Item.Value = (int)Math.Round(choice.Item.Value * valScalar);
                                if (choice.Item.Value < 1) choice.Item.Value = 1;
                            }
                        }
                    }
                }
            }
        }
    }
}
