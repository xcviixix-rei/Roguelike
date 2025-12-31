using Roguelike.Data;
using Roguelike.Core.Map;
using System;
using System.Collections.Generic;

namespace Roguelike.Optimization
{
    public class BalanceGenome
    {
        // Economy
        public float GoldDropMultiplier { get; set; } = 0.5f;
        public float[] ShopPriceScalars { get; set; } = new float[6];

        // Hero
        public float HeroHealthScalar { get; set; } = 1.0f;
        public float HeroStartGoldScalar { get; set; } = 1.0f;
        public int HeroManaOffset { get; set; } = 0;

        // Room
        public Dictionary<RoomType, float> RoomTypeWeights { get; set; } = new Dictionary<RoomType, float>();
        public float MonsterStarRatio { get; set; } = 0.5f;
        public float EliteStarRatio { get; set; } = 0.5f;

        // Rest healing
        public float RestHealingScalar { get; set; } = 1.0f;

        // Card
        public Dictionary<string, int> CardCostModifiers { get; set; } = new Dictionary<string, int>();
        public Dictionary<string, List<float>> CardActionScalars { get; set; } = new Dictionary<string, List<float>>();

        // Enemy
        public Dictionary<string, float> EnemyHealthScalars { get; set; } = new Dictionary<string, float>();
        public Dictionary<string, List<float>> EnemyActionWeightScalars { get; set; } = new Dictionary<string, List<float>>();
        public Dictionary<string, List<float>> EnemyActionValueScalars { get; set; } = new Dictionary<string, List<float>>();

        // Effect Value Scalars
        public Dictionary<string, float> EffectValueScalars { get; set; } = new Dictionary<string, float>();

        public BalanceGenome()
        {
            for (int i = 1; i <= 5; i++) ShopPriceScalars[i] = 1.0f;

            RoomTypeWeights[RoomType.Monster] = 45f;
            RoomTypeWeights[RoomType.Elite] = 12f;
            RoomTypeWeights[RoomType.Event] = 22f;
            RoomTypeWeights[RoomType.Shop] = 8f;
            RoomTypeWeights[RoomType.Rest] = 13f;
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

                RoomTypeWeights = new Dictionary<RoomType, float>(this.RoomTypeWeights),
                MonsterStarRatio = this.MonsterStarRatio,
                EliteStarRatio = this.EliteStarRatio,

                RestHealingScalar = this.RestHealingScalar,

                CardCostModifiers = new Dictionary<string, int>(this.CardCostModifiers),
                EnemyHealthScalars = new Dictionary<string, float>(this.EnemyHealthScalars),
                
                CardActionScalars = new Dictionary<string, List<float>>(),
                EnemyActionWeightScalars = new Dictionary<string, List<float>>(),
                EnemyActionValueScalars = new Dictionary<string, List<float>>(),
                
                EffectValueScalars = new Dictionary<string, float>(this.EffectValueScalars)
            };

            foreach (var kv in CardActionScalars) clone.CardActionScalars[kv.Key] = new List<float>(kv.Value);
            foreach (var kv in EnemyActionWeightScalars) clone.EnemyActionWeightScalars[kv.Key] = new List<float>(kv.Value);
            foreach (var kv in EnemyActionValueScalars) clone.EnemyActionValueScalars[kv.Key] = new List<float>(kv.Value);

            return clone;
        }

        public void Randomize(CardPool cards, EnemyPool enemies, EffectPool effects, Random rng)
        {
            GoldDropMultiplier = (float)(0.3 + rng.NextDouble() * 0.4); 
            for (int i = 1; i <= 5; i++) ShopPriceScalars[i] = (float)(0.8 + rng.NextDouble() * 0.4);

            // Hero
            HeroHealthScalar = (float)(0.6 + rng.NextDouble() * 0.8); // 60% - 140% HP
            HeroStartGoldScalar = (float)(0.8 + rng.NextDouble() * 0.4);

            // ROom
            RoomTypeWeights[RoomType.Monster] = (float)(30 + rng.NextDouble() * 30);  // 30-60
            RoomTypeWeights[RoomType.Elite] = (float)(10 + rng.NextDouble() * 15);     // 10-25
            RoomTypeWeights[RoomType.Event] = (float)(10 + rng.NextDouble() * 25);    // 10-35
            RoomTypeWeights[RoomType.Shop] = (float)(5 + rng.NextDouble() * 10);      // 5-15
            RoomTypeWeights[RoomType.Rest] = (float)(5 + rng.NextDouble() * 10);      // 5-15
            
            // Star rating distribution
            MonsterStarRatio = (float)(0.3 + rng.NextDouble() * 0.4);  // 0.3-0.7
            EliteStarRatio = (float)(0.3 + rng.NextDouble() * 0.4);

            RestHealingScalar = (float)(0.5 + rng.NextDouble() * 1.0); // 50% - 150% healing
            
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
                    scalars.Add((float)(0.5 + rng.NextDouble() * 1.0)); //0.5 - 1.5
                }
                CardActionScalars[card.Id] = scalars;
            }

            // Enemies
            foreach (var enemy in enemies.EnemiesById.Values)
            {
                EnemyHealthScalars[enemy.Id] = (float)(0.4 + rng.NextDouble() * 1.4); // 0.4 - 1.8

                var weights = new List<float>();
                var values = new List<float>();

                foreach (var choice in enemy.ActionSet)
                {
                    weights.Add((float)(0.5 + rng.NextDouble()));
                    values.Add((float)(0.5 + rng.NextDouble() * 1.3)); // 0.5 - 1.8
                }
                EnemyActionWeightScalars[enemy.Id] = weights;
                EnemyActionValueScalars[enemy.Id] = values;
            }

            // Effects
            foreach (var effect in effects.EffectsById.Values)
            {
                EffectValueScalars[effect.Id] = (float)(0.5 + rng.NextDouble() * 1.0); // 0.5 - 1.5
            }
        }
    }
}
