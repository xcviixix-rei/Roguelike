using Roguelike.Core;
using Roguelike.Data;
using System;
using Xunit;

namespace Roguelike.Tests.Combat
{
    public class ActionResolverTests
    {
        // Helper class for testing
        private class TestCombatant : Combatant
        {
            public TestCombatant(CombatantData sourceData) : base(sourceData) { }
        }

        [Fact]
        public void ApplyDamage_BaseDamage_ReducesHealthCorrectly()
        {
            // Arrange
            var attackerData = TestHelpers.CreateBasicEnemyData("attacker");
            var targetData = TestHelpers.CreateBasicEnemyData("target", health: 50);
            var attacker = new TestCombatant(attackerData);
            var target = new TestCombatant(targetData);

            var action = new CombatActionData
            {
                Type = ActionType.DealDamage,
                Value = 10,
                Target = TargetType.SingleOpponent
            };

            // Act
            ActionResolver.Resolve(action, attacker, target, id => null);

            // Assert
            Assert.Equal(40, target.CurrentHealth);
        }

        [Fact]
        public void ApplyDamage_WithBlock_BlockAbsorbsDamageFirst()
        {
            // Arrange
            var attackerData = TestHelpers.CreateBasicEnemyData("attacker");
            var targetData = TestHelpers.CreateBasicEnemyData("target", health: 50);
            var attacker = new TestCombatant(attackerData);
            var target = new TestCombatant(targetData);
            target.GainBlock(5);

            var action = new CombatActionData
            {
                Type = ActionType.DealDamage,
                Value = 10,
                Target = TargetType.SingleOpponent
            };

            // Act
            ActionResolver.Resolve(action, attacker, target, id => null);

            // Assert
            Assert.Equal(45, target.CurrentHealth);
            Assert.Equal(0, target.Block);
        }

        [Fact]
        public void ApplyDamage_WithFlatStrength_IncreasesDamage()
        {
            // Arrange
            var attackerData = TestHelpers.CreateBasicEnemyData("attacker");
            var targetData = TestHelpers.CreateBasicEnemyData("target", health: 50);
            var attacker = new TestCombatant(attackerData);
            var target = new TestCombatant(targetData);

            var strengthEffect = TestHelpers.CreateStrengthEffect(intensity: 3, type: IntensityType.Flat);
            attacker.ApplyEffect(strengthEffect);

            var action = new CombatActionData
            {
                Type = ActionType.DealDamage,
                Value = 10,
                Target = TargetType.SingleOpponent
            };

            // Act
            ActionResolver.Resolve(action, attacker, target, id => null);

            // Assert
            Assert.Equal(37, target.CurrentHealth); // 50 - (10 + 3)
        }

        [Fact]
        public void ApplyDamage_WithPercentageStrength_MultipliesDamage()
        {
            // Arrange
            var attackerData = TestHelpers.CreateBasicEnemyData("attacker");
            var targetData = TestHelpers.CreateBasicEnemyData("target", health: 50);
            var attacker = new TestCombatant(attackerData);
            var target = new TestCombatant(targetData);

            var strengthEffect = TestHelpers.CreateStrengthEffect(intensity: 50, type: IntensityType.Percentage);
            attacker.ApplyEffect(strengthEffect);

            var action = new CombatActionData
            {
                Type = ActionType.DealDamage,
                Value = 10,
                Target = TargetType.SingleOpponent
            };

            // Act
            ActionResolver.Resolve(action, attacker, target, id => null);

            // Assert
            Assert.Equal(35, target.CurrentHealth); // 50 - (10 * 1.5) = 50 - 15
        }

        [Fact]
        public void ApplyDamage_WithVulnerable_IncreasesDamageTaken()
        {
            // Arrange
            var attackerData = TestHelpers.CreateBasicEnemyData("attacker");
            var targetData = TestHelpers.CreateBasicEnemyData("target", health: 50);
            var attacker = new TestCombatant(attackerData);
            var target = new TestCombatant(targetData);

            var vulnEffect = TestHelpers.CreateVulnerableEffect(intensity: 50); // 50% more damage
            target.ApplyEffect(vulnEffect);

            var action = new CombatActionData
            {
                Type = ActionType.DealDamage,
                Value = 10,
                Target = TargetType.SingleOpponent
            };

            // Act
            ActionResolver.Resolve(action, attacker, target, id => null);

            // Assert
            Assert.Equal(35, target.CurrentHealth); // 50 - (10 * 1.5) = 50 - 15
        }

        [Fact]
        public void ApplyDamage_WithWeakened_ReducesDamageDealt()
        {
            // Arrange
            var attackerData = TestHelpers.CreateBasicEnemyData("attacker");
            var targetData = TestHelpers.CreateBasicEnemyData("target", health: 50);
            var attacker = new TestCombatant(attackerData);
            var target = new TestCombatant(targetData);

            var weakEffect = TestHelpers.CreateWeakenedEffect(intensity: 25); // 25% less damage
            attacker.ApplyEffect(weakEffect);

            var action = new CombatActionData
            {
                Type = ActionType.DealDamage,
                Value = 20,
                Target = TargetType.SingleOpponent
            };

            // Act
            ActionResolver.Resolve(action, attacker, target, id => null);

            // Assert
            Assert.Equal(35, target.CurrentHealth); // 50 - (20 * 0.75) = 50 - 15
        }

        [Fact]
        public void ApplyDamage_WithMultipleModifiers_CalculatesCorrectly()
        {
            // Arrange
            var attackerData = TestHelpers.CreateBasicEnemyData("attacker");
            var targetData = TestHelpers.CreateBasicEnemyData("target", health: 100);
            var attacker = new TestCombatant(attackerData);
            var target = new TestCombatant(targetData);

            // Attacker has +2 flat strength and 25% weakness
            var strengthEffect = TestHelpers.CreateStrengthEffect(intensity: 2, type: IntensityType.Flat);
            var weakEffect = TestHelpers.CreateWeakenedEffect(intensity: 25);
            attacker.ApplyEffect(strengthEffect);
            attacker.ApplyEffect(weakEffect);

            // Target is vulnerable 50%
            var vulnEffect = TestHelpers.CreateVulnerableEffect(intensity: 50);
            target.ApplyEffect(vulnEffect);

            var action = new CombatActionData
            {
                Type = ActionType.DealDamage,
                Value = 10,
                Target = TargetType.SingleOpponent
            };

            // Act
            ActionResolver.Resolve(action, attacker, target, id => null);

            // Assert
            // Calculation: (10 + 2) * 0.75 (weak) * 1.5 (vuln) = 12 * 0.75 * 1.5 = 13.5 -> 13 (floor)
            Assert.Equal(87, target.CurrentHealth); // 100 - 13
        }

        [Fact]
        public void ApplyDamage_WithPierced_BypassesBlock()
        {
            // Arrange
            var attackerData = TestHelpers.CreateBasicEnemyData("attacker");
            var targetData = TestHelpers.CreateBasicEnemyData("target", health: 50);
            var attacker = new TestCombatant(attackerData);
            var target = new TestCombatant(targetData);
            target.GainBlock(20);

            var piercedEffect = TestHelpers.CreatePiercedEffect();
            target.ApplyEffect(piercedEffect);

            var action = new CombatActionData
            {
                Type = ActionType.DealDamage,
                Value = 10,
                Target = TargetType.SingleOpponent
            };

            // Act
            ActionResolver.Resolve(action, attacker, target, id => null);

            // Assert
            Assert.Equal(40, target.CurrentHealth); // Damage bypasses block
            Assert.Equal(20, target.Block); // Block unchanged
        }

        [Fact]
        public void ApplyDamage_NegativeResult_DealsZeroDamage()
        {
            // Arrange
            var attackerData = TestHelpers.CreateBasicEnemyData("attacker");
            var targetData = TestHelpers.CreateBasicEnemyData("target", health: 50);
            var attacker = new TestCombatant(attackerData);
            var target = new TestCombatant(targetData);

            // Extreme weakness to make damage negative
            var weakEffect = TestHelpers.CreateWeakenedEffect(intensity: 200);
            attacker.ApplyEffect(weakEffect);

            var action = new CombatActionData
            {
                Type = ActionType.DealDamage,
                Value = 10,
                Target = TargetType.SingleOpponent
            };

            // Act
            ActionResolver.Resolve(action, attacker, target, id => null);

            // Assert
            Assert.Equal(50, target.CurrentHealth); // No damage dealt
        }

        [Fact]
        public void ApplyBlock_BaseBlock_IncreasesBlock()
        {
            // Arrange
            var sourceData = TestHelpers.CreateBasicEnemyData("source");
            var targetData = TestHelpers.CreateBasicEnemyData("target");
            var source = new TestCombatant(sourceData);
            var target = new TestCombatant(targetData);

            var action = new CombatActionData
            {
                Type = ActionType.GainBlock,
                Value = 10,
                Target = TargetType.Self
            };

            // Act
            ActionResolver.Resolve(action, source, target, id => null);

            // Assert
            Assert.Equal(10, target.Block);
        }

        [Fact]
        public void ApplyBlock_WithFrail_ReducesBlockGained()
        {
            // Arrange
            var sourceData = TestHelpers.CreateBasicEnemyData("source");
            var targetData = TestHelpers.CreateBasicEnemyData("target");
            var source = new TestCombatant(sourceData);
            var target = new TestCombatant(targetData);

            var frailEffect = TestHelpers.CreateFrailEffect(intensity: 25); // 25% less block
            target.ApplyEffect(frailEffect);

            var action = new CombatActionData
            {
                Type = ActionType.GainBlock,
                Value = 20,
                Target = TargetType.Self
            };

            // Act
            ActionResolver.Resolve(action, source, target, id => null);

            // Assert
            Assert.Equal(15, target.Block); // 20 * 0.75 = 15
        }

        [Fact]
        public void ApplyStatusEffect_AppliesEffectToTarget()
        {
            // Arrange
            var sourceData = TestHelpers.CreateBasicEnemyData("source");
            var targetData = TestHelpers.CreateBasicEnemyData("target");
            var source = new TestCombatant(sourceData);
            var target = new TestCombatant(targetData);

            var vulnEffect = TestHelpers.CreateVulnerableEffect();
            var effectLookup = TestHelpers.CreateEffectLookup(vulnEffect);

            var action = new CombatActionData
            {
                Type = ActionType.ApplyStatusEffect,
                EffectId = "test_vulnerable",
                Target = TargetType.SingleOpponent
            };

            // Act
            ActionResolver.Resolve(action, source, target, effectLookup);

            // Assert
            Assert.Single(target.ActiveEffects);
            Assert.Equal("test_vulnerable", target.ActiveEffects[0].SourceData.Id);
        }

        [Fact]
        public void ApplyStatusEffect_WithInvalidEffectId_DoesNothing()
        {
            // Arrange
            var sourceData = TestHelpers.CreateBasicEnemyData("source");
            var targetData = TestHelpers.CreateBasicEnemyData("target");
            var source = new TestCombatant(sourceData);
            var target = new TestCombatant(targetData);

            var effectLookup = TestHelpers.CreateEffectLookup(); // Empty lookup

            var action = new CombatActionData
            {
                Type = ActionType.ApplyStatusEffect,
                EffectId = "nonexistent_effect",
                Target = TargetType.SingleOpponent
            };

            // Act
            ActionResolver.Resolve(action, source, target, effectLookup);

            // Assert
            Assert.Empty(target.ActiveEffects);
        }

        [Fact]
        public void ApplyDeckEffect_OnNonHero_DoesNothing()
        {
            // Arrange
            var sourceData = TestHelpers.CreateBasicEnemyData("source");
            var targetData = TestHelpers.CreateBasicEnemyData("target");
            var source = new TestCombatant(sourceData);
            var target = new TestCombatant(targetData); // Not a Hero

            var drawEffect = TestHelpers.CreateDrawEffect();
            var effectLookup = TestHelpers.CreateEffectLookup(drawEffect);

            var action = new CombatActionData
            {
                Type = ActionType.ApplyDeckEffect,
                EffectId = "test_draw",
                Value = 2,
                Target = TargetType.Self
            };

            // Act - should not throw exception
            ActionResolver.Resolve(action, source, target, effectLookup);

            // Assert - no exception means test passes
        }
    }
}
