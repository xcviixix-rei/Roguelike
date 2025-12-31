using Roguelike.Data;
using System.Collections.Generic;

namespace Roguelike.Tests
{
    /// <summary>
    /// Helper methods and factory functions for creating test data objects.
    /// Reduces code duplication across test files.
    /// </summary>
    public static class TestHelpers
    {
        // ============= Card Creation Helpers =============
        
        public static CardData CreateBasicAttackCard(string id = "test_attack", int damage = 6, int manaCost = 1)
        {
            return new CardData
            {
                Id = id,
                Name = "Test Attack",
                Description = "Deal damage",
                ManaCost = manaCost,
                StarRating = 1,
                Type = CardType.Attack,
                Actions = new List<CombatActionData>
                {
                    new CombatActionData
                    {
                        Type = ActionType.DealDamage,
                        Target = TargetType.SingleOpponent,
                        Value = damage
                    }
                }
            };
        }

        public static CardData CreateBasicDefendCard(string id = "test_defend", int block = 5, int manaCost = 1)
        {
            return new CardData
            {
                Id = id,
                Name = "Test Defend",
                Description = "Gain block",
                ManaCost = manaCost,
                StarRating = 1,
                Type = CardType.Skill,
                Actions = new List<CombatActionData>
                {
                    new CombatActionData
                    {
                        Type = ActionType.GainBlock,
                        Target = TargetType.Self,
                        Value = block
                    }
                }
            };
        }

        // ============= Effect Creation Helpers =============

        public static StatusEffectData CreateStrengthEffect(int intensity = 2, IntensityType type = IntensityType.Flat, DecayType decay = DecayType.Permanent)
        {
            return new StatusEffectData
            {
                Id = "test_strength",
                Name = "Strength",
                Description = "Increases damage",
                EffectType = StatusEffectType.Strength,
                Intensity = intensity,
                IntensityType = type,
                Duration = decay == DecayType.AfterXTURNS ? 3 : 0,
                Decay = decay,
                ApplyType = ApplyType.RightAway
            };
        }

        public static StatusEffectData CreateVulnerableEffect(int intensity = 50, int duration = 2)
        {
            return new StatusEffectData
            {
                Id = "test_vulnerable",
                Name = "Vulnerable",
                Description = "Takes more damage",
                EffectType = StatusEffectType.Vulnerable,
                Intensity = intensity,
                IntensityType = IntensityType.Percentage,
                Duration = duration,
                Decay = DecayType.AfterXTURNS,
                ApplyType = ApplyType.RightAway
            };
        }

        public static StatusEffectData CreateWeakenedEffect(int intensity = 25, int duration = 2)
        {
            return new StatusEffectData
            {
                Id = "test_weakened",
                Name = "Weakened",
                Description = "Deals less damage",
                EffectType = StatusEffectType.Weakened,
                Intensity = intensity,
                IntensityType = IntensityType.Percentage,
                Duration = duration,
                Decay = DecayType.AfterXTURNS,
                ApplyType = ApplyType.RightAway
            };
        }

        public static StatusEffectData CreateFrailEffect(int intensity = 25, int duration = 2)
        {
            return new StatusEffectData
            {
                Id = "test_frail",
                Name = "Frail",
                Description = "Gains less block",
                EffectType = StatusEffectType.Frail,
                Intensity = intensity,
                IntensityType = IntensityType.Percentage,
                Duration = duration,
                Decay = DecayType.AfterXTURNS,
                ApplyType = ApplyType.RightAway
            };
        }

        public static StatusEffectData CreatePiercedEffect(int duration = 1)
        {
            return new StatusEffectData
            {
                Id = "test_pierced",
                Name = "Pierced",
                Description = "Damage bypasses block",
                EffectType = StatusEffectType.Pierced,
                Intensity = 0,
                IntensityType = IntensityType.Flat,
                Duration = duration,
                Decay = DecayType.AfterXTURNS,
                ApplyType = ApplyType.RightAway
            };
        }

        public static DeckEffectData CreateDrawEffect()
        {
            return new DeckEffectData
            {
                Id = "test_draw",
                Name = "Draw",
                Description = "Draw cards",
                EffectType = DeckEffectType.DrawCard,
                Intensity = 0,
                IntensityType = IntensityType.Flat,
                Duration = 0,
                Decay = DecayType.Permanent,
                ApplyType = ApplyType.RightAway
            };
        }

        // ============= Combatant Creation Helpers =============

        public static HeroData CreateBasicHeroData(string id = "test_hero", int health = 75)
        {
            return new HeroData
            {
                Id = id,
                Name = "Test Hero",
                StartingHealth = health,
                StartingDeckCardIds = new List<string> { "test_attack", "test_defend" },
                StartingGold = 100
            };
        }

        public static EnemyData CreateBasicEnemyData(string id = "test_enemy", int health = 50)
        {
            return new EnemyData
            {
                Id = id,
                Name = "Test Enemy",
                StartingHealth = health
            };
        }

        // ============= Effect Lookup Helper =============

        /// <summary>
        /// Creates a simple effect lookup function for testing ActionResolver
        /// </summary>
        public static System.Func<string, EffectData> CreateEffectLookup(params EffectData[] effects)
        {
            var dict = new Dictionary<string, EffectData>();
            foreach (var effect in effects)
            {
                dict[effect.Id] = effect;
            }
            return id => dict.ContainsKey(id) ? dict[id] : null;
        }
    }
}
