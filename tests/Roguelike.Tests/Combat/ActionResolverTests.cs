using Roguelike.Core;
using Roguelike.Data;
using System;
using Xunit;

namespace Roguelike.Tests.Combat
{
    public class ActionResolverTests
    {

        private class TestCombatant : Combatant
        {
            public TestCombatant(CombatantData sourceData) : base(sourceData) { }
        }

        [Fact]
        public void ApplyDamage_BaseDamage_ReducesHealthCorrectly()
        {

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


            ActionResolver.Resolve(action, attacker, target, id => null);


            Assert.Equal(40, target.CurrentHealth);
        }

        [Fact]
        public void ApplyDamage_WithBlock_BlockAbsorbsDamageFirst()
        {

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


            ActionResolver.Resolve(action, attacker, target, id => null);


            Assert.Equal(45, target.CurrentHealth);
            Assert.Equal(0, target.Block);
        }

        [Fact]
        public void ApplyDamage_WithFlatStrength_IncreasesDamage()
        {

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


            ActionResolver.Resolve(action, attacker, target, id => null);


            Assert.Equal(37, target.CurrentHealth);
        }

        [Fact]
        public void ApplyDamage_WithPercentageStrength_MultipliesDamage()
        {

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


            ActionResolver.Resolve(action, attacker, target, id => null);


            Assert.Equal(35, target.CurrentHealth);
        }

        [Fact]
        public void ApplyDamage_WithVulnerable_IncreasesDamageTaken()
        {

            var attackerData = TestHelpers.CreateBasicEnemyData("attacker");
            var targetData = TestHelpers.CreateBasicEnemyData("target", health: 50);
            var attacker = new TestCombatant(attackerData);
            var target = new TestCombatant(targetData);

            var vulnEffect = TestHelpers.CreateVulnerableEffect(intensity: 50);
            target.ApplyEffect(vulnEffect);

            var action = new CombatActionData
            {
                Type = ActionType.DealDamage,
                Value = 10,
                Target = TargetType.SingleOpponent
            };


            ActionResolver.Resolve(action, attacker, target, id => null);


            Assert.Equal(35, target.CurrentHealth);
        }

        [Fact]
        public void ApplyDamage_WithWeakened_ReducesDamageDealt()
        {

            var attackerData = TestHelpers.CreateBasicEnemyData("attacker");
            var targetData = TestHelpers.CreateBasicEnemyData("target", health: 50);
            var attacker = new TestCombatant(attackerData);
            var target = new TestCombatant(targetData);

            var weakEffect = TestHelpers.CreateWeakenedEffect(intensity: 25);
            attacker.ApplyEffect(weakEffect);

            var action = new CombatActionData
            {
                Type = ActionType.DealDamage,
                Value = 20,
                Target = TargetType.SingleOpponent
            };


            ActionResolver.Resolve(action, attacker, target, id => null);


            Assert.Equal(35, target.CurrentHealth);
        }

        [Fact]
        public void ApplyDamage_WithMultipleModifiers_CalculatesCorrectly()
        {

            var attackerData = TestHelpers.CreateBasicEnemyData("attacker");
            var targetData = TestHelpers.CreateBasicEnemyData("target", health: 100);
            var attacker = new TestCombatant(attackerData);
            var target = new TestCombatant(targetData);


            var strengthEffect = TestHelpers.CreateStrengthEffect(intensity: 2, type: IntensityType.Flat);
            var weakEffect = TestHelpers.CreateWeakenedEffect(intensity: 25);
            attacker.ApplyEffect(strengthEffect);
            attacker.ApplyEffect(weakEffect);


            var vulnEffect = TestHelpers.CreateVulnerableEffect(intensity: 50);
            target.ApplyEffect(vulnEffect);

            var action = new CombatActionData
            {
                Type = ActionType.DealDamage,
                Value = 10,
                Target = TargetType.SingleOpponent
            };


            ActionResolver.Resolve(action, attacker, target, id => null);


            Assert.Equal(87, target.CurrentHealth);
        }

        [Fact]
        public void ApplyDamage_WithPierced_BypassesBlock()
        {

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


            ActionResolver.Resolve(action, attacker, target, id => null);


            Assert.Equal(40, target.CurrentHealth);
            Assert.Equal(20, target.Block);
        }

        [Fact]
        public void ApplyDamage_NegativeResult_DealsZeroDamage()
        {

            var attackerData = TestHelpers.CreateBasicEnemyData("attacker");
            var targetData = TestHelpers.CreateBasicEnemyData("target", health: 50);
            var attacker = new TestCombatant(attackerData);
            var target = new TestCombatant(targetData);


            var weakEffect = TestHelpers.CreateWeakenedEffect(intensity: 200);
            attacker.ApplyEffect(weakEffect);

            var action = new CombatActionData
            {
                Type = ActionType.DealDamage,
                Value = 10,
                Target = TargetType.SingleOpponent
            };


            ActionResolver.Resolve(action, attacker, target, id => null);


            Assert.Equal(50, target.CurrentHealth);
        }

        [Fact]
        public void ApplyBlock_BaseBlock_IncreasesBlock()
        {

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


            ActionResolver.Resolve(action, source, target, id => null);


            Assert.Equal(10, target.Block);
        }

        [Fact]
        public void ApplyBlock_WithFrail_ReducesBlockGained()
        {

            var sourceData = TestHelpers.CreateBasicEnemyData("source");
            var targetData = TestHelpers.CreateBasicEnemyData("target");
            var source = new TestCombatant(sourceData);
            var target = new TestCombatant(targetData);

            var frailEffect = TestHelpers.CreateFrailEffect(intensity: 25);
            target.ApplyEffect(frailEffect);

            var action = new CombatActionData
            {
                Type = ActionType.GainBlock,
                Value = 20,
                Target = TargetType.Self
            };


            ActionResolver.Resolve(action, source, target, id => null);


            Assert.Equal(15, target.Block);
        }

        [Fact]
        public void ApplyStatusEffect_AppliesEffectToTarget()
        {

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


            ActionResolver.Resolve(action, source, target, effectLookup);


            Assert.Single(target.ActiveEffects);
            Assert.Equal("test_vulnerable", target.ActiveEffects[0].SourceData.Id);
        }

        [Fact]
        public void ApplyStatusEffect_WithInvalidEffectId_DoesNothing()
        {

            var sourceData = TestHelpers.CreateBasicEnemyData("source");
            var targetData = TestHelpers.CreateBasicEnemyData("target");
            var source = new TestCombatant(sourceData);
            var target = new TestCombatant(targetData);

            var effectLookup = TestHelpers.CreateEffectLookup();

            var action = new CombatActionData
            {
                Type = ActionType.ApplyStatusEffect,
                EffectId = "nonexistent_effect",
                Target = TargetType.SingleOpponent
            };


            ActionResolver.Resolve(action, source, target, effectLookup);


            Assert.Empty(target.ActiveEffects);
        }

        [Fact]
        public void ApplyDeckEffect_OnNonHero_DoesNothing()
        {

            var sourceData = TestHelpers.CreateBasicEnemyData("source");
            var targetData = TestHelpers.CreateBasicEnemyData("target");
            var source = new TestCombatant(sourceData);
            var target = new TestCombatant(targetData);

            var drawEffect = TestHelpers.CreateDrawEffect();
            var effectLookup = TestHelpers.CreateEffectLookup(drawEffect);

            var action = new CombatActionData
            {
                Type = ActionType.ApplyDeckEffect,
                EffectId = "test_draw",
                Value = 2,
                Target = TargetType.Self
            };


            ActionResolver.Resolve(action, source, target, effectLookup);


        }
    }
}
