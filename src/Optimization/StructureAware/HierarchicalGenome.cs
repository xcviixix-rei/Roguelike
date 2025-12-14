using Roguelike.Data;
using Roguelike.Core.Map;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Roguelike.Optimization
{
    /// <summary>
    /// A hierarchical genome that reduces the parameter space from ~500 to ~50 parameters
    /// by using multiplicative layers and only overriding specific outliers.
    /// </summary>
    public class HierarchicalGenome
    {
        // TOP LEVEL: Global Multipliers (5 params)
        public float GlobalDamageMultiplier { get; set; } = 1.0f;
        public float GlobalHealthMultiplier { get; set; } = 1.0f;
        public float GlobalBlockMultiplier { get; set; } = 1.0f;
        public float GlobalManaCostMultiplier { get; set; } = 1.0f;
        public float GlobalGoldMultiplier { get; set; } = 1.0f;

        // MID LEVEL: Progression Scaling (9 params)
        public float EarlyGameDamageScaling { get; set; } = 1.0f;   // Floors 1-5
        public float MidGameDamageScaling { get; set; } = 1.0f;     // Floors 6-10
        public float LateGameDamageScaling { get; set; } = 1.0f;    // Floors 11-15
        
        public float EarlyGameHealthScaling { get; set; } = 1.0f;
        public float MidGameHealthScaling { get; set; } = 1.0f;
        public float LateGameHealthScaling { get; set; } = 1.0f;
        
        public float EarlyGameBlockScaling { get; set; } = 1.0f;
        public float MidGameBlockScaling { get; set; } = 1.0f;
        public float LateGameBlockScaling { get; set; } = 1.0f;

        // CATEGORY LEVEL: Type-Based Scaling (15 params)
        public Dictionary<CardType, float> CardTypeScalars { get; set; } = new()
        {
            { CardType.Attack, 1.0f },
            { CardType.Skill, 1.0f },
            { CardType.Power, 1.0f }
        };
        
        public Dictionary<int, float> CardStarScalars { get; set; } = new()
        {
            { 1, 1.0f }, { 2, 1.0f }, { 3, 1.0f }, { 4, 1.0f }, { 5, 1.0f }
        };
        
        public Dictionary<int, float> EnemyStarScalars { get; set; } = new()
        {
            { 1, 1.0f }, { 2, 1.0f }, { 3, 1.0f }, { 4, 1.0f }, { 5, 1.0f }
        };

        // ROOM DISTRIBUTION (8 params)
        public Dictionary<RoomType, float> RoomTypeWeights { get; set; } = new()
        {
            { RoomType.Monster, 45f },
            { RoomType.Elite, 12f },
            { RoomType.Event, 22f },
            { RoomType.Shop, 8f },
            { RoomType.Rest, 13f }
        };
        
        public float MonsterStarRatio { get; set; } = 0.5f;
        public float EliteStarRatio { get; set; } = 0.5f;
        public float RestHealingScalar { get; set; } = 1.0f;

        // HERO BASELINE (3 params)
        public float HeroHealthScalar { get; set; } = 1.0f;
        public float HeroStartGoldScalar { get; set; } = 1.0f;
        public int HeroManaOffset { get; set; } = 0;

        // INDIVIDUAL OVERRIDES (Sparse - only for problem cards/enemies)
        public Dictionary<string, float> CardDamageOverrides { get; set; } = new();
        public Dictionary<string, float> CardManaCostOverrides { get; set; } = new();
        public Dictionary<string, float> EnemyHealthOverrides { get; set; } = new();

        // INITIALIZATION
        public HierarchicalGenome() { }

        public void Randomize(Random rng)
        {
            // Global multipliers: 0.7 - 1.3
            GlobalDamageMultiplier = (float)(0.7 + rng.NextDouble() * 0.6);
            GlobalHealthMultiplier = (float)(0.7 + rng.NextDouble() * 0.6);
            GlobalBlockMultiplier = (float)(0.7 + rng.NextDouble() * 0.6);
            GlobalManaCostMultiplier = (float)(0.85 + rng.NextDouble() * 0.3);
            GlobalGoldMultiplier = (float)(0.7 + rng.NextDouble() * 0.6);

            // Progression scaling: 0.8 - 1.4
            EarlyGameDamageScaling = (float)(0.8 + rng.NextDouble() * 0.3);
            MidGameDamageScaling = (float)(1.0 + rng.NextDouble() * 0.3);
            LateGameDamageScaling = (float)(1.2 + rng.NextDouble() * 0.3);
            
            EarlyGameHealthScaling = (float)(0.8 + rng.NextDouble() * 0.3);
            MidGameHealthScaling = (float)(1.0 + rng.NextDouble() * 0.3);
            LateGameHealthScaling = (float)(1.2 + rng.NextDouble() * 0.3);
            
            EarlyGameBlockScaling = (float)(0.9 + rng.NextDouble() * 0.2);
            MidGameBlockScaling = (float)(1.0 + rng.NextDouble() * 0.2);
            LateGameBlockScaling = (float)(1.0 + rng.NextDouble() * 0.2);

            // Card type scaling: 0.85 - 1.15
            foreach (var key in CardTypeScalars.Keys.ToList())
                CardTypeScalars[key] = (float)(0.85 + rng.NextDouble() * 0.3);

            // Star rating scaling: 0.9 - 1.1
            foreach (var key in CardStarScalars.Keys.ToList())
                CardStarScalars[key] = (float)(0.9 + rng.NextDouble() * 0.2);
            
            foreach (var key in EnemyStarScalars.Keys.ToList())
                EnemyStarScalars[key] = (float)(0.9 + rng.NextDouble() * 0.2);

            // Room weights
            RoomTypeWeights[RoomType.Monster] = (float)(30 + rng.NextDouble() * 30);
            RoomTypeWeights[RoomType.Elite] = (float)(5 + rng.NextDouble() * 20);
            RoomTypeWeights[RoomType.Event] = (float)(10 + rng.NextDouble() * 25);
            RoomTypeWeights[RoomType.Shop] = (float)(5 + rng.NextDouble() * 15);
            RoomTypeWeights[RoomType.Rest] = (float)(5 + rng.NextDouble() * 20);
            
            MonsterStarRatio = (float)(0.3 + rng.NextDouble() * 0.4);
            EliteStarRatio = (float)(0.3 + rng.NextDouble() * 0.4);
            RestHealingScalar = (float)(0.6 + rng.NextDouble() * 0.8);

            // Hero
            HeroHealthScalar = (float)(0.7 + rng.NextDouble() * 0.6);
            HeroStartGoldScalar = (float)(0.8 + rng.NextDouble() * 0.4);
            
            int manaRoll = rng.Next(100);
            if (manaRoll < 5) HeroManaOffset = -1;
            else if (manaRoll > 95) HeroManaOffset = 1;
            else HeroManaOffset = 0;
        }

        public HierarchicalGenome Clone()
        {
            var clone = new HierarchicalGenome
            {
                GlobalDamageMultiplier = this.GlobalDamageMultiplier,
                GlobalHealthMultiplier = this.GlobalHealthMultiplier,
                GlobalBlockMultiplier = this.GlobalBlockMultiplier,
                GlobalManaCostMultiplier = this.GlobalManaCostMultiplier,
                GlobalGoldMultiplier = this.GlobalGoldMultiplier,
                
                EarlyGameDamageScaling = this.EarlyGameDamageScaling,
                MidGameDamageScaling = this.MidGameDamageScaling,
                LateGameDamageScaling = this.LateGameDamageScaling,
                EarlyGameHealthScaling = this.EarlyGameHealthScaling,
                MidGameHealthScaling = this.MidGameHealthScaling,
                LateGameHealthScaling = this.LateGameHealthScaling,
                EarlyGameBlockScaling = this.EarlyGameBlockScaling,
                MidGameBlockScaling = this.MidGameBlockScaling,
                LateGameBlockScaling = this.LateGameBlockScaling,
                
                CardTypeScalars = new Dictionary<CardType, float>(this.CardTypeScalars),
                CardStarScalars = new Dictionary<int, float>(this.CardStarScalars),
                EnemyStarScalars = new Dictionary<int, float>(this.EnemyStarScalars),
                
                RoomTypeWeights = new Dictionary<RoomType, float>(this.RoomTypeWeights),
                MonsterStarRatio = this.MonsterStarRatio,
                EliteStarRatio = this.EliteStarRatio,
                RestHealingScalar = this.RestHealingScalar,
                
                HeroHealthScalar = this.HeroHealthScalar,
                HeroStartGoldScalar = this.HeroStartGoldScalar,
                HeroManaOffset = this.HeroManaOffset,
                
                CardDamageOverrides = new Dictionary<string, float>(this.CardDamageOverrides),
                CardManaCostOverrides = new Dictionary<string, float>(this.CardManaCostOverrides),
                EnemyHealthOverrides = new Dictionary<string, float>(this.EnemyHealthOverrides)
            };
            return clone;
        }

        // SCALING CALCULATION METHODS
        
        public float GetProgressionScalar(int floor, ScalingType type)
        {
            float early, mid, late;
            
            switch (type)
            {
                case ScalingType.Damage:
                    early = EarlyGameDamageScaling;
                    mid = MidGameDamageScaling;
                    late = LateGameDamageScaling;
                    break;
                case ScalingType.Health:
                    early = EarlyGameHealthScaling;
                    mid = MidGameHealthScaling;
                    late = LateGameHealthScaling;
                    break;
                case ScalingType.Block:
                    early = EarlyGameBlockScaling;
                    mid = MidGameBlockScaling;
                    late = LateGameBlockScaling;
                    break;
                default:
                    return 1.0f;
            }
            
            if (floor <= 5) return early;
            if (floor <= 10) return mid;
            return late;
        }
        
        public int GetScaledCardDamage(CardData card, int floor)
        {
            float scalar = GlobalDamageMultiplier;
            scalar *= CardTypeScalars[card.Type];
            scalar *= CardStarScalars[card.StarRating];
            scalar *= GetProgressionScalar(floor, ScalingType.Damage);
            
            if (CardDamageOverrides.TryGetValue(card.Id, out float ovr))
                scalar *= ovr;
            
            var damageAction = card.Actions.FirstOrDefault(a => a.Type == ActionType.DealDamage);
            if (damageAction == null) return 0;
            
            return Math.Max(1, (int)Math.Round(damageAction.Value * scalar));
        }
        
        public int GetScaledCardBlock(CardData card, int floor)
        {
            float scalar = GlobalBlockMultiplier;
            scalar *= CardTypeScalars[card.Type];
            scalar *= CardStarScalars[card.StarRating];
            scalar *= GetProgressionScalar(floor, ScalingType.Block);
            
            var blockAction = card.Actions.FirstOrDefault(a => a.Type == ActionType.GainBlock);
            if (blockAction == null) return 0;
            
            return Math.Max(1, (int)Math.Round(blockAction.Value * scalar));
        }
        
        public int GetScaledCardCost(CardData card)
        {
            float scalar = GlobalManaCostMultiplier;
            
            if (CardManaCostOverrides.TryGetValue(card.Id, out float ovr))
                scalar *= ovr;
            
            int newCost = (int)Math.Round(card.ManaCost * scalar);
            return Math.Max(0, newCost);
        }
        
        public int GetScaledEnemyHealth(EnemyData enemy, int floor)
        {
            float scalar = GlobalHealthMultiplier;
            scalar *= EnemyStarScalars[enemy.StarRating];
            scalar *= GetProgressionScalar(floor, ScalingType.Health);
            
            if (EnemyHealthOverrides.TryGetValue(enemy.Id, out float ovr))
                scalar *= ovr;
            
            return Math.Max(1, (int)Math.Round(enemy.StartingHealth * scalar));
        }

        public enum ScalingType
        {
            Damage,
            Health,
            Block
        }
    }
}
