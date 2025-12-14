using Roguelike.Data;
using Roguelike.Core;
using Roguelike.Core.Map;
using System;
using System.Linq;

namespace Roguelike.Optimization
{
    /// <summary>
    /// Applies a hierarchical genome to game data using multiplicative scaling layers
    /// </summary>
    public static class HierarchicalApplicator
    {
        public static void Apply(
            HierarchicalGenome genome, 
            CardPool cardPool, 
            EnemyPool enemyPool, 
            RelicPool relicPool,
            HeroData hero,
            int currentFloor = 1)
        {
            ApplyToHero(genome, hero);
            ApplyToCards(genome, cardPool, currentFloor);
            ApplyToEnemies(genome, enemyPool, currentFloor);
            ApplyToEconomy(genome, cardPool, relicPool);
        }

        private static void ApplyToHero(HierarchicalGenome genome, HeroData hero)
        {
            // Scale hero health
            hero.StartingHealth = (int)Math.Round(hero.StartingHealth * genome.HeroHealthScalar);
            hero.StartingHealth = Math.Max(30, hero.StartingHealth); // Min 30 HP
            
            // Scale starting gold
            hero.StartingGold = (int)Math.Round(hero.StartingGold * genome.HeroStartGoldScalar);
            hero.StartingGold = Math.Max(50, hero.StartingGold); // Min 50 gold
            
            // Adjust mana
            hero.StartingMana += genome.HeroManaOffset;
            hero.StartingMana = Math.Max(2, Math.Min(4, hero.StartingMana)); // 2-4 mana
        }

        private static void ApplyToCards(HierarchicalGenome genome, CardPool cardPool, int floor)
        {
            foreach (var card in cardPool.CardsById.Values)
            {
                for (int i = 0; i < card.Actions.Count; i++)
                {
                    var action = card.Actions[i];
                    
                    switch (action.Type)
                    {
                        case ActionType.DealDamage:
                            action.Value = CalculateCardDamage(genome, card, action.Value, floor);
                            break;
                            
                        case ActionType.GainBlock:
                            action.Value = CalculateCardBlock(genome, card, action.Value, floor);
                            break;
                            
                        case ActionType.ApplyStatusEffect:
                            action.Value = CalculateCardEffectStacks(genome, card, action.Value);
                            break;
                    }
                }
                
                int originalCost = card.ManaCost;
                card.ManaCost = CalculateCardCost(genome, card, originalCost);
            }
        }

        private static int CalculateCardDamage(
            HierarchicalGenome genome, 
            CardData card, 
            int baseValue, 
            int floor)
        {
            float scalar = genome.GlobalDamageMultiplier;
            scalar *= genome.CardTypeScalars[card.Type];
            scalar *= genome.CardStarScalars[card.StarRating];
            scalar *= genome.GetProgressionScalar(floor, HierarchicalGenome.ScalingType.Damage);
            
            if (genome.CardDamageOverrides.TryGetValue(card.Id, out float ovr))
                scalar *= ovr;
            
            return Math.Max(1, (int)Math.Round(baseValue * scalar));
        }

        private static int CalculateCardBlock(
            HierarchicalGenome genome, 
            CardData card, 
            int baseValue, 
            int floor)
        {
            float scalar = genome.GlobalBlockMultiplier;
            scalar *= genome.CardTypeScalars[card.Type];
            scalar *= genome.CardStarScalars[card.StarRating];
            scalar *= genome.GetProgressionScalar(floor, HierarchicalGenome.ScalingType.Block);
            
            return Math.Max(1, (int)Math.Round(baseValue * scalar));
        }

        private static int CalculateCardEffectStacks(
            HierarchicalGenome genome,
            CardData card,
            int baseValue)
        {
            float scalar = genome.CardStarScalars[card.StarRating];
            return Math.Max(1, (int)Math.Round(baseValue * scalar));
        }

        private static int CalculateCardCost(
            HierarchicalGenome genome,
            CardData card,
            int baseCost)
        {
            float scalar = genome.GlobalManaCostMultiplier;
            
            if (genome.CardManaCostOverrides.TryGetValue(card.Id, out float ovr))
                scalar *= ovr;
            
            int newCost = (int)Math.Round(baseCost * scalar);
            return Math.Max(0, newCost);
        }

        private static void ApplyToEnemies(HierarchicalGenome genome, EnemyPool enemyPool, int floor)
        {
            foreach (var enemy in enemyPool.EnemiesById.Values)
            {
                float healthScalar = genome.GlobalHealthMultiplier;
                healthScalar *= genome.EnemyStarScalars[enemy.StarRating];
                healthScalar *= genome.GetProgressionScalar(floor, HierarchicalGenome.ScalingType.Health);
                
                if (genome.EnemyHealthOverrides.TryGetValue(enemy.Id, out float hpOvr))
                    healthScalar *= hpOvr;
                
                enemy.StartingHealth = Math.Max(1, (int)Math.Round(enemy.StartingHealth * healthScalar));
                
                foreach (var weightedAction in enemy.ActionSet)
                {
                    var action = weightedAction.Item;
                    
                    switch (action.Type)
                    {
                        case ActionType.DealDamage:
                            float dmgScalar = genome.GlobalDamageMultiplier;
                            dmgScalar *= genome.EnemyStarScalars[enemy.StarRating];
                            dmgScalar *= genome.GetProgressionScalar(floor, HierarchicalGenome.ScalingType.Damage);
                            action.Value = Math.Max(1, (int)Math.Round(action.Value * dmgScalar));
                            break;
                            
                        case ActionType.GainBlock:
                            float blockScalar = genome.GlobalBlockMultiplier;
                            blockScalar *= genome.EnemyStarScalars[enemy.StarRating];
                            blockScalar *= genome.GetProgressionScalar(floor, HierarchicalGenome.ScalingType.Block);
                            action.Value = Math.Max(1, (int)Math.Round(action.Value * blockScalar));
                            break;
                    }
                }
            }
        }

        private static void ApplyToEconomy(
            HierarchicalGenome genome, 
            CardPool cardPool, 
            RelicPool relicPool)
        {
            for (int star = 1; star <= 5; star++)
            {
                if (cardPool.BaseShopCosts.ContainsKey(star))
                {
                    int baseCost = cardPool.BaseShopCosts[star];
                    cardPool.BaseShopCosts[star] = (int)Math.Round(baseCost * genome.GlobalGoldMultiplier);
                }
                
                if (relicPool.BaseShopCosts.ContainsKey(star))
                {
                    int baseCost = relicPool.BaseShopCosts[star];
                    relicPool.BaseShopCosts[star] = (int)Math.Round(baseCost * genome.GlobalGoldMultiplier);
                }
            }
        }

        /// <summary>
        /// Apply genome specifically for combat encounters (dynamic scaling based on current floor)
        /// </summary>
        public static void ApplyForFloor(
            HierarchicalGenome genome,
            CardPool cardPool,
            EnemyPool enemyPool,
            int floor)
        {
            ApplyToCards(genome, cardPool, floor);
            ApplyToEnemies(genome, enemyPool, floor);
        }
    }
}
