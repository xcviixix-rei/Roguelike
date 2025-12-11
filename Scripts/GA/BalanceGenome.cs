using Roguelike.Data;
using System;
using System.Collections.Generic;

namespace Roguelike.GA
{
    public class BalanceGenome
    {
        // Economy
        public float GoldDropMultiplier { get; set; } = 0.5f;
        public float[] ShopPriceScalars { get; set; } = new float[6]; // Index 1-5

        // Hero
        public float HeroHealthScalar { get; set; } = 1.0f;     // Scales starting HP
        public float HeroStartGoldScalar { get; set; } = 1.0f;  // Scales starting Gold
        public int HeroManaOffset { get; set; } = 0;            // Additive modifier

        // Card
        public Dictionary<string, int> CardCostModifiers { get; set; } = new Dictionary<string, int>();
        public Dictionary<string, List<float>> CardActionScalars { get; set; } = new Dictionary<string, List<float>>();

        // Enemy
        public Dictionary<string, float> EnemyHealthScalars { get; set; } = new Dictionary<string, float>();
        public Dictionary<string, List<float>> EnemyActionWeightScalars { get; set; } = new Dictionary<string, List<float>>();
        public Dictionary<string, List<float>> EnemyActionValueScalars { get; set; } = new Dictionary<string, List<float>>();

        public BalanceGenome()
        {
            for (int i = 1; i <= 5; i++) ShopPriceScalars[i] = 1.0f;
        }

        public BalanceGenome Clone()
        {
            var clone = new BalanceGenome
            {
                GoldDropMultiplier = this.GoldDropMultiplier,
                ShopPriceScalars = (float[])this.ShopPriceScalars.Clone(),
                
                HeroHealthScalar = this.HeroHealthScalar,
                HeroStartGoldScalar = this.HeroStartGoldScalar,
                HeroManaOffset = this.HeroManaOffset,

                CardCostModifiers = new Dictionary<string, int>(this.CardCostModifiers),
                EnemyHealthScalars = new Dictionary<string, float>(this.EnemyHealthScalars),
                
                CardActionScalars = new Dictionary<string, List<float>>(),
                EnemyActionWeightScalars = new Dictionary<string, List<float>>(),
                EnemyActionValueScalars = new Dictionary<string, List<float>>()
            };

            foreach (var kv in CardActionScalars) clone.CardActionScalars[kv.Key] = new List<float>(kv.Value);
            foreach (var kv in EnemyActionWeightScalars) clone.EnemyActionWeightScalars[kv.Key] = new List<float>(kv.Value);
            foreach (var kv in EnemyActionValueScalars) clone.EnemyActionValueScalars[kv.Key] = new List<float>(kv.Value);

            return clone;
        }

        /// <summary>
        /// Randomizes the genome. 
        /// </summary>
        public void Randomize(CardPool cards, EnemyPool enemies, Random rng)
        {
            GoldDropMultiplier = (float)(0.3 + rng.NextDouble() * 0.4); 
            for (int i = 1; i <= 5; i++) ShopPriceScalars[i] = (float)(0.8 + rng.NextDouble() * 0.4);

            // Hero (Conservative randomization)
            HeroHealthScalar = (float)(0.8 + rng.NextDouble() * 0.4); // 80% - 120% HP
            HeroStartGoldScalar = (float)(0.8 + rng.NextDouble() * 0.4);
            // Mana offset: 90% chance of 0, 5% chance of +1, 5% chance of -1
            int manaRoll = rng.Next(100);
            if (manaRoll < 5) HeroManaOffset = -1;
            else if (manaRoll > 95) HeroManaOffset = 1;
            else HeroManaOffset = 0;

            // Cards
            foreach (var card in cards.CardsById.Values)
            {
                int costRoll = rng.Next(100);
                if (costRoll < 5 && card.ManaCost > 0) CardCostModifiers[card.Id] = -1;
                else if (costRoll > 95) CardCostModifiers[card.Id] = 1;
                else CardCostModifiers[card.Id] = 0;

                var scalars = new List<float>();
                foreach (var action in card.Actions)
                {
                    scalars.Add((float)(0.7 + rng.NextDouble() * 0.6)); // 0.7x to 1.3x
                }
                CardActionScalars[card.Id] = scalars;
            }

            // Enemies
            foreach (var enemy in enemies.EnemiesById.Values)
            {
                EnemyHealthScalars[enemy.Id] = (float)(0.7 + rng.NextDouble() * 0.6);

                var weights = new List<float>();
                var values = new List<float>();

                foreach (var choice in enemy.ActionSet)
                {
                    weights.Add((float)(0.5 + rng.NextDouble()));
                    values.Add((float)(0.7 + rng.NextDouble() * 0.6));
                }
                EnemyActionWeightScalars[enemy.Id] = weights;
                EnemyActionValueScalars[enemy.Id] = values;
            }
        }
    }
}