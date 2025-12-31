using Roguelike.Core;
using Roguelike.Data;
using System;
using System.Linq;
using Xunit;

namespace Roguelike.Tests.Combat
{
    public class CombatantTests
    {
        // We need a concrete implementation for testing the abstract Combatant class
        private class TestCombatant : Combatant
        {
            public TestCombatant(CombatantData sourceData) : base(sourceData) { }
        }

        [Fact]
        public void Constructor_InitializesHealthFromSourceData()
        {
            // Arrange
            var data = TestHelpers.CreateBasicEnemyData(health: 50);

            // Act
            var combatant = new TestCombatant(data);

            // Assert
            Assert.Equal(50, combatant.MaxHealth);
            Assert.Equal(50, combatant.CurrentHealth);
            Assert.Equal(0, combatant.Block);
        }

        [Fact]
        public void TakeDamage_WithNoBlock_ReducesHealth()
        {
            // Arrange
            var data = TestHelpers.CreateBasicEnemyData(health: 50);
            var combatant = new TestCombatant(data);

            // Act
            combatant.TakeDamage(10);

            // Assert
            Assert.Equal(40, combatant.CurrentHealth);
            Assert.Equal(0, combatant.Block);
        }

        [Fact]
        public void TakeDamage_WithSufficientBlock_OnlyReducesBlock()
        {
            // Arrange
            var data = TestHelpers.CreateBasicEnemyData(health: 50);
            var combatant = new TestCombatant(data);
            combatant.GainBlock(15);

            // Act
            combatant.TakeDamage(10);

            // Assert
            Assert.Equal(50, combatant.CurrentHealth);
            Assert.Equal(5, combatant.Block);
        }

        [Fact]
        public void TakeDamage_WithInsufficientBlock_ReducesBothBlockAndHealth()
        {
            // Arrange
            var data = TestHelpers.CreateBasicEnemyData(health: 50);
            var combatant = new TestCombatant(data);
            combatant.GainBlock(5);

            // Act
            combatant.TakeDamage(10);

            // Assert
            Assert.Equal(45, combatant.CurrentHealth);
            Assert.Equal(0, combatant.Block);
        }

        [Fact]
        public void TakeDamage_Lethal_SetsHealthToZero()
        {
            // Arrange
            var data = TestHelpers.CreateBasicEnemyData(health: 50);
            var combatant = new TestCombatant(data);

            // Act
            combatant.TakeDamage(100);

            // Assert
            Assert.Equal(0, combatant.CurrentHealth);
        }

        [Fact]
        public void TakeDamage_Negative_DoesNothing()
        {
            // Arrange
            var data = TestHelpers.CreateBasicEnemyData(health: 50);
            var combatant = new TestCombatant(data);

            // Act
            combatant.TakeDamage(-10);

            // Assert
            Assert.Equal(50, combatant.CurrentHealth);
        }

        [Fact]
        public void TakeDamage_Zero_DoesNothing()
        {
            // Arrange
            var data = TestHelpers.CreateBasicEnemyData(health: 50);
            var combatant = new TestCombatant(data);

            // Act
            combatant.TakeDamage(0);

            // Assert
            Assert.Equal(50, combatant.CurrentHealth);
        }

        [Fact]
        public void TakePiercingDamage_BypassesBlock()
        {
            // Arrange
            var data = TestHelpers.CreateBasicEnemyData(health: 50);
            var combatant = new TestCombatant(data);
            combatant.GainBlock(20);

            // Act
            combatant.TakePiercingDamage(10);

            // Assert
            Assert.Equal(40, combatant.CurrentHealth);
            Assert.Equal(20, combatant.Block); // Block unchanged
        }

        [Fact]
        public void TakePiercingDamage_Lethal_SetsHealthToZero()
        {
            // Arrange
            var data = TestHelpers.CreateBasicEnemyData(health: 50);
            var combatant = new TestCombatant(data);

            // Act
            combatant.TakePiercingDamage(100);

            // Assert
            Assert.Equal(0, combatant.CurrentHealth);
        }

        [Fact]
        public void GainBlock_IncreasesBlock()
        {
            // Arrange
            var data = TestHelpers.CreateBasicEnemyData(health: 50);
            var combatant = new TestCombatant(data);

            // Act
            combatant.GainBlock(10);

            // Assert
            Assert.Equal(10, combatant.Block);
        }

        [Fact]
        public void GainBlock_Multiple_Stacks()
        {
            // Arrange
            var data = TestHelpers.CreateBasicEnemyData(health: 50);
            var combatant = new TestCombatant(data);

            // Act
            combatant.GainBlock(5);
            combatant.GainBlock(8);

            // Assert
            Assert.Equal(13, combatant.Block);
        }

        [Fact]
        public void GainBlock_Negative_DoesNothing()
        {
            // Arrange
            var data = TestHelpers.CreateBasicEnemyData(health: 50);
            var combatant = new TestCombatant(data);

            // Act
            combatant.GainBlock(-5);

            // Assert
            Assert.Equal(0, combatant.Block);
        }

        [Fact]
        public void Heal_IncreasesHealth()
        {
            // Arrange
            var data = TestHelpers.CreateBasicEnemyData(health: 50);
            var combatant = new TestCombatant(data);
            combatant.TakeDamage(20);

            // Act
            combatant.Heal(10);

            // Assert
            Assert.Equal(40, combatant.CurrentHealth);
        }

        [Fact]
        public void Heal_CappedAtMaxHealth()
        {
            // Arrange
            var data = TestHelpers.CreateBasicEnemyData(health: 50);
            var combatant = new TestCombatant(data);
            combatant.TakeDamage(10);

            // Act
            combatant.Heal(50);

            // Assert
            Assert.Equal(50, combatant.CurrentHealth);
        }

        [Fact]
        public void IncreaseMaxHealth_IncreasesMaxAndCurrentHealth()
        {
            // Arrange
            var data = TestHelpers.CreateBasicEnemyData(health: 50);
            var combatant = new TestCombatant(data);

            // Act
            combatant.IncreaseMaxHealth(10);

            // Assert
            Assert.Equal(60, combatant.MaxHealth);
            Assert.Equal(60, combatant.CurrentHealth);
        }

        [Fact]
        public void IncreaseMaxHealth_Negative_DoesNothing()
        {
            // Arrange
            var data = TestHelpers.CreateBasicEnemyData(health: 50);
            var combatant = new TestCombatant(data);

            // Act
            combatant.IncreaseMaxHealth(-10);

            // Assert
            Assert.Equal(50, combatant.MaxHealth);
            Assert.Equal(50, combatant.CurrentHealth);
        }

        [Fact]
        public void ApplyEffect_AddsNewEffect()
        {
            // Arrange
            var data = TestHelpers.CreateBasicEnemyData(health: 50);
            var combatant = new TestCombatant(data);
            var effectData = TestHelpers.CreateVulnerableEffect();

            // Act
            combatant.ApplyEffect(effectData);

            // Assert
            Assert.Single(combatant.ActiveEffects);
            Assert.Equal("test_vulnerable", combatant.ActiveEffects[0].SourceData.Id);
        }

        [Fact]
        public void ApplyEffect_RefreshesDurationWhenReapplied()
        {
            // Arrange
            var data = TestHelpers.CreateBasicEnemyData(health: 50);
            var combatant = new TestCombatant(data);
            var effectData = TestHelpers.CreateVulnerableEffect(duration: 3);

            combatant.ApplyEffect(effectData);
            combatant.ActiveEffects[0].RemainingDuration = 1; // Simulate effect wearing off

            // Act
            combatant.ApplyEffect(effectData);

            // Assert
            Assert.Single(combatant.ActiveEffects); // Still only one effect
            Assert.Equal(3, combatant.ActiveEffects[0].RemainingDuration); // Duration refreshed
        }

        [Fact]
        public void ApplyEffect_MultipleEffects_AllStored()
        {
            // Arrange
            var data = TestHelpers.CreateBasicEnemyData(health: 50);
            var combatant = new TestCombatant(data);
            var vulnerable = TestHelpers.CreateVulnerableEffect();
            var weakened = TestHelpers.CreateWeakenedEffect();

            // Act
            combatant.ApplyEffect(vulnerable);
            combatant.ApplyEffect(weakened);

            // Assert
            Assert.Equal(2, combatant.ActiveEffects.Count);
        }

        [Fact]
        public void TickDownEffects_RemovesExpiredEffects()
        {
            // Arrange
            var data = TestHelpers.CreateBasicEnemyData(health: 50);
            var combatant = new TestCombatant(data);
            var effectData = TestHelpers.CreateVulnerableEffect(duration: 1);
            combatant.ApplyEffect(effectData);

            // Act
            combatant.TickDownEffects();

            // Assert
            Assert.Empty(combatant.ActiveEffects);
        }

        [Fact]
        public void TickDownEffects_KeepsNonExpiredEffects()
        {
            // Arrange
            var data = TestHelpers.CreateBasicEnemyData(health: 50);
            var combatant = new TestCombatant(data);
            var effectData = TestHelpers.CreateVulnerableEffect(duration: 3);
            combatant.ApplyEffect(effectData);

            // Act
            combatant.TickDownEffects();

            // Assert
            Assert.Single(combatant.ActiveEffects);
            Assert.Equal(2, combatant.ActiveEffects[0].RemainingDuration);
        }

        [Fact]
        public void TickDownEffects_KeepsPermanentEffects()
        {
            // Arrange
            var data = TestHelpers.CreateBasicEnemyData(health: 50);
            var combatant = new TestCombatant(data);
            var effectData = TestHelpers.CreateStrengthEffect(decay: DecayType.Permanent);
            combatant.ApplyEffect(effectData);

            // Act
            combatant.TickDownEffects();
            combatant.TickDownEffects();
            combatant.TickDownEffects();

            // Assert
            Assert.Single(combatant.ActiveEffects);
        }

        [Fact]
        public void ResetForNewCombat_ClearsBlock()
        {
            // Arrange
            var data = TestHelpers.CreateBasicEnemyData(health: 50);
            var combatant = new TestCombatant(data);
            combatant.GainBlock(20);

            // Act
            combatant.ResetForNewCombat();

            // Assert
            Assert.Equal(0, combatant.Block);
        }

        [Fact]
        public void ResetForNewCombat_RemovesTemporaryEffects()
        {
            // Arrange
            var data = TestHelpers.CreateBasicEnemyData(health: 50);
            var combatant = new TestCombatant(data);
            var tempEffect = TestHelpers.CreateVulnerableEffect(duration: 2);
            combatant.ApplyEffect(tempEffect);

            // Act
            combatant.ResetForNewCombat();

            // Assert
            Assert.Empty(combatant.ActiveEffects);
        }

        [Fact]
        public void ResetForNewCombat_KeepsPermanentEffects()
        {
            // Arrange
            var data = TestHelpers.CreateBasicEnemyData(health: 50);
            var combatant = new TestCombatant(data);
            var permEffect = TestHelpers.CreateStrengthEffect(decay: DecayType.Permanent);
            combatant.ApplyEffect(permEffect);

            // Act
            combatant.ResetForNewCombat();

            // Assert
            Assert.Single(combatant.ActiveEffects);
        }

        [Fact]
        public void ResetForNewCombat_MixedEffects_OnlyKeepsPermanent()
        {
            // Arrange
            var data = TestHelpers.CreateBasicEnemyData(health: 50);
            var combatant = new TestCombatant(data);
            var permEffect = TestHelpers.CreateStrengthEffect(decay: DecayType.Permanent);
            var tempEffect1 = TestHelpers.CreateVulnerableEffect();
            var tempEffect2 = TestHelpers.CreateWeakenedEffect();

            combatant.ApplyEffect(permEffect);
            combatant.ApplyEffect(tempEffect1);
            combatant.ApplyEffect(tempEffect2);

            // Act
            combatant.ResetForNewCombat();

            // Assert
            Assert.Single(combatant.ActiveEffects);
            Assert.Equal("test_strength", combatant.ActiveEffects[0].SourceData.Id);
        }
    }
}
