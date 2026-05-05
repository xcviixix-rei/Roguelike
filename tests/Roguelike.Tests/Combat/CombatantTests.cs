using Roguelike.Core;
using Roguelike.Data;
using System;
using System.Linq;
using Xunit;

namespace Roguelike.Tests.Combat
{
    public class CombatantTests
    {

        private class TestCombatant : Combatant
        {
            public TestCombatant(CombatantData sourceData) : base(sourceData) { }
        }

        [Fact]
        public void Constructor_InitializesHealthFromSourceData()
        {

            var data = TestHelpers.CreateBasicEnemyData(health: 50);


            var combatant = new TestCombatant(data);


            Assert.Equal(50, combatant.MaxHealth);
            Assert.Equal(50, combatant.CurrentHealth);
            Assert.Equal(0, combatant.Block);
        }

        [Fact]
        public void TakeDamage_WithNoBlock_ReducesHealth()
        {

            var data = TestHelpers.CreateBasicEnemyData(health: 50);
            var combatant = new TestCombatant(data);


            combatant.TakeDamage(10);


            Assert.Equal(40, combatant.CurrentHealth);
            Assert.Equal(0, combatant.Block);
        }

        [Fact]
        public void TakeDamage_WithSufficientBlock_OnlyReducesBlock()
        {

            var data = TestHelpers.CreateBasicEnemyData(health: 50);
            var combatant = new TestCombatant(data);
            combatant.GainBlock(15);


            combatant.TakeDamage(10);


            Assert.Equal(50, combatant.CurrentHealth);
            Assert.Equal(5, combatant.Block);
        }

        [Fact]
        public void TakeDamage_WithInsufficientBlock_ReducesBothBlockAndHealth()
        {

            var data = TestHelpers.CreateBasicEnemyData(health: 50);
            var combatant = new TestCombatant(data);
            combatant.GainBlock(5);


            combatant.TakeDamage(10);


            Assert.Equal(45, combatant.CurrentHealth);
            Assert.Equal(0, combatant.Block);
        }

        [Fact]
        public void TakeDamage_Lethal_SetsHealthToZero()
        {

            var data = TestHelpers.CreateBasicEnemyData(health: 50);
            var combatant = new TestCombatant(data);


            combatant.TakeDamage(100);


            Assert.Equal(0, combatant.CurrentHealth);
        }

        [Fact]
        public void TakeDamage_Negative_DoesNothing()
        {

            var data = TestHelpers.CreateBasicEnemyData(health: 50);
            var combatant = new TestCombatant(data);


            combatant.TakeDamage(-10);


            Assert.Equal(50, combatant.CurrentHealth);
        }

        [Fact]
        public void TakeDamage_Zero_DoesNothing()
        {

            var data = TestHelpers.CreateBasicEnemyData(health: 50);
            var combatant = new TestCombatant(data);


            combatant.TakeDamage(0);


            Assert.Equal(50, combatant.CurrentHealth);
        }

        [Fact]
        public void TakePiercingDamage_BypassesBlock()
        {

            var data = TestHelpers.CreateBasicEnemyData(health: 50);
            var combatant = new TestCombatant(data);
            combatant.GainBlock(20);


            combatant.TakePiercingDamage(10);


            Assert.Equal(40, combatant.CurrentHealth);
            Assert.Equal(20, combatant.Block);
        }

        [Fact]
        public void TakePiercingDamage_Lethal_SetsHealthToZero()
        {

            var data = TestHelpers.CreateBasicEnemyData(health: 50);
            var combatant = new TestCombatant(data);


            combatant.TakePiercingDamage(100);


            Assert.Equal(0, combatant.CurrentHealth);
        }

        [Fact]
        public void GainBlock_IncreasesBlock()
        {

            var data = TestHelpers.CreateBasicEnemyData(health: 50);
            var combatant = new TestCombatant(data);


            combatant.GainBlock(10);


            Assert.Equal(10, combatant.Block);
        }

        [Fact]
        public void GainBlock_Multiple_Stacks()
        {

            var data = TestHelpers.CreateBasicEnemyData(health: 50);
            var combatant = new TestCombatant(data);


            combatant.GainBlock(5);
            combatant.GainBlock(8);


            Assert.Equal(13, combatant.Block);
        }

        [Fact]
        public void GainBlock_Negative_DoesNothing()
        {

            var data = TestHelpers.CreateBasicEnemyData(health: 50);
            var combatant = new TestCombatant(data);


            combatant.GainBlock(-5);


            Assert.Equal(0, combatant.Block);
        }

        [Fact]
        public void Heal_IncreasesHealth()
        {

            var data = TestHelpers.CreateBasicEnemyData(health: 50);
            var combatant = new TestCombatant(data);
            combatant.TakeDamage(20);


            combatant.Heal(10);


            Assert.Equal(40, combatant.CurrentHealth);
        }

        [Fact]
        public void Heal_CappedAtMaxHealth()
        {

            var data = TestHelpers.CreateBasicEnemyData(health: 50);
            var combatant = new TestCombatant(data);
            combatant.TakeDamage(10);


            combatant.Heal(50);


            Assert.Equal(50, combatant.CurrentHealth);
        }

        [Fact]
        public void IncreaseMaxHealth_IncreasesMaxAndCurrentHealth()
        {

            var data = TestHelpers.CreateBasicEnemyData(health: 50);
            var combatant = new TestCombatant(data);


            combatant.IncreaseMaxHealth(10);


            Assert.Equal(60, combatant.MaxHealth);
            Assert.Equal(60, combatant.CurrentHealth);
        }

        [Fact]
        public void IncreaseMaxHealth_Negative_DoesNothing()
        {

            var data = TestHelpers.CreateBasicEnemyData(health: 50);
            var combatant = new TestCombatant(data);


            combatant.IncreaseMaxHealth(-10);


            Assert.Equal(50, combatant.MaxHealth);
            Assert.Equal(50, combatant.CurrentHealth);
        }

        [Fact]
        public void ApplyEffect_AddsNewEffect()
        {

            var data = TestHelpers.CreateBasicEnemyData(health: 50);
            var combatant = new TestCombatant(data);
            var effectData = TestHelpers.CreateVulnerableEffect();


            combatant.ApplyEffect(effectData);


            Assert.Single(combatant.ActiveEffects);
            Assert.Equal("test_vulnerable", combatant.ActiveEffects[0].SourceData.Id);
        }

        [Fact]
        public void ApplyEffect_RefreshesDurationWhenReapplied()
        {

            var data = TestHelpers.CreateBasicEnemyData(health: 50);
            var combatant = new TestCombatant(data);
            var effectData = TestHelpers.CreateVulnerableEffect(duration: 3);

            combatant.ApplyEffect(effectData);
            combatant.ActiveEffects[0].RemainingDuration = 1;


            combatant.ApplyEffect(effectData);


            Assert.Single(combatant.ActiveEffects);
            Assert.Equal(3, combatant.ActiveEffects[0].RemainingDuration);
        }

        [Fact]
        public void ApplyEffect_MultipleEffects_AllStored()
        {

            var data = TestHelpers.CreateBasicEnemyData(health: 50);
            var combatant = new TestCombatant(data);
            var vulnerable = TestHelpers.CreateVulnerableEffect();
            var weakened = TestHelpers.CreateWeakenedEffect();


            combatant.ApplyEffect(vulnerable);
            combatant.ApplyEffect(weakened);


            Assert.Equal(2, combatant.ActiveEffects.Count);
        }

        [Fact]
        public void TickDownEffects_RemovesExpiredEffects()
        {

            var data = TestHelpers.CreateBasicEnemyData(health: 50);
            var combatant = new TestCombatant(data);
            var effectData = TestHelpers.CreateVulnerableEffect(duration: 1);
            combatant.ApplyEffect(effectData);


            combatant.TickDownEffects();


            Assert.Empty(combatant.ActiveEffects);
        }

        [Fact]
        public void TickDownEffects_KeepsNonExpiredEffects()
        {

            var data = TestHelpers.CreateBasicEnemyData(health: 50);
            var combatant = new TestCombatant(data);
            var effectData = TestHelpers.CreateVulnerableEffect(duration: 3);
            combatant.ApplyEffect(effectData);


            combatant.TickDownEffects();


            Assert.Single(combatant.ActiveEffects);
            Assert.Equal(2, combatant.ActiveEffects[0].RemainingDuration);
        }

        [Fact]
        public void TickDownEffects_KeepsPermanentEffects()
        {

            var data = TestHelpers.CreateBasicEnemyData(health: 50);
            var combatant = new TestCombatant(data);
            var effectData = TestHelpers.CreateStrengthEffect(decay: DecayType.Permanent);
            combatant.ApplyEffect(effectData);


            combatant.TickDownEffects();
            combatant.TickDownEffects();
            combatant.TickDownEffects();


            Assert.Single(combatant.ActiveEffects);
        }

        [Fact]
        public void ResetForNewCombat_ClearsBlock()
        {

            var data = TestHelpers.CreateBasicEnemyData(health: 50);
            var combatant = new TestCombatant(data);
            combatant.GainBlock(20);


            combatant.ResetForNewCombat();


            Assert.Equal(0, combatant.Block);
        }

        [Fact]
        public void ResetForNewCombat_RemovesTemporaryEffects()
        {

            var data = TestHelpers.CreateBasicEnemyData(health: 50);
            var combatant = new TestCombatant(data);
            var tempEffect = TestHelpers.CreateVulnerableEffect(duration: 2);
            combatant.ApplyEffect(tempEffect);


            combatant.ResetForNewCombat();


            Assert.Empty(combatant.ActiveEffects);
        }

        [Fact]
        public void ResetForNewCombat_KeepsPermanentEffects()
        {

            var data = TestHelpers.CreateBasicEnemyData(health: 50);
            var combatant = new TestCombatant(data);
            var permEffect = TestHelpers.CreateStrengthEffect(decay: DecayType.Permanent);
            combatant.ApplyEffect(permEffect);


            combatant.ResetForNewCombat();


            Assert.Single(combatant.ActiveEffects);
        }

        [Fact]
        public void ResetForNewCombat_MixedEffects_OnlyKeepsPermanent()
        {

            var data = TestHelpers.CreateBasicEnemyData(health: 50);
            var combatant = new TestCombatant(data);
            var permEffect = TestHelpers.CreateStrengthEffect(decay: DecayType.Permanent);
            var tempEffect1 = TestHelpers.CreateVulnerableEffect();
            var tempEffect2 = TestHelpers.CreateWeakenedEffect();

            combatant.ApplyEffect(permEffect);
            combatant.ApplyEffect(tempEffect1);
            combatant.ApplyEffect(tempEffect2);


            combatant.ResetForNewCombat();


            Assert.Single(combatant.ActiveEffects);
            Assert.Equal("test_strength", combatant.ActiveEffects[0].SourceData.Id);
        }
    }
}
