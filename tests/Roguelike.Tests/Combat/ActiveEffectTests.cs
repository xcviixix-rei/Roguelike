using Roguelike.Core;
using Roguelike.Data;
using System.Linq;
using Xunit;

namespace Roguelike.Tests.Combat
{
    public class ActiveEffectTests
    {
        [Fact]
        public void Constructor_WithTemporaryEffect_InitializesRemainingDuration()
        {
            // Arrange
            var effectData = TestHelpers.CreateVulnerableEffect(intensity: 50, duration: 3);

            // Act
            var activeEffect = new ActiveEffect(effectData);

            // Assert
            Assert.Equal(3, activeEffect.RemainingDuration);
            Assert.Same(effectData, activeEffect.SourceData);
        }

        [Fact]
        public void Constructor_WithPermanentEffect_SetsMaxDuration()
        {
            // Arrange
            var effectData = TestHelpers.CreateStrengthEffect(intensity: 2, decay: DecayType.Permanent);

            // Act
            var activeEffect = new ActiveEffect(effectData);

            // Assert
            Assert.Equal(int.MaxValue, activeEffect.RemainingDuration);
        }

        [Fact]
        public void TickDown_TemporaryEffect_DecrementsDuration()
        {
            // Arrange
            var effectData = TestHelpers.CreateVulnerableEffect(intensity: 50, duration: 3);
            var activeEffect = new ActiveEffect(effectData);

            // Act
            bool expired = activeEffect.TickDown();

            // Assert
            Assert.Equal(2, activeEffect.RemainingDuration);
            Assert.False(expired);
        }

        [Fact]
        public void TickDown_TemporaryEffect_ReturnsTrue_WhenExpired()
        {
            // Arrange
            var effectData = TestHelpers.CreateVulnerableEffect(intensity: 50, duration: 1);
            var activeEffect = new ActiveEffect(effectData);

            // Act
            bool expired = activeEffect.TickDown();

            // Assert
            Assert.Equal(0, activeEffect.RemainingDuration);
            Assert.True(expired);
        }

        [Fact]
        public void TickDown_PermanentEffect_NeverExpires()
        {
            // Arrange
            var effectData = TestHelpers.CreateStrengthEffect(intensity: 2, decay: DecayType.Permanent);
            var activeEffect = new ActiveEffect(effectData);

            // Act
            bool expired1 = activeEffect.TickDown();
            bool expired2 = activeEffect.TickDown();
            bool expired3 = activeEffect.TickDown();

            // Assert
            Assert.False(expired1);
            Assert.False(expired2);
            Assert.False(expired3);
            Assert.Equal(int.MaxValue, activeEffect.RemainingDuration);
        }

        [Fact]
        public void TickDown_MultipleTurns_CountsDownCorrectly()
        {
            // Arrange
            var effectData = TestHelpers.CreateWeakenedEffect(intensity: 25, duration: 5);
            var activeEffect = new ActiveEffect(effectData);

            // Act & Assert - Turn 1
            Assert.False(activeEffect.TickDown());
            Assert.Equal(4, activeEffect.RemainingDuration);

            // Turn 2
            Assert.False(activeEffect.TickDown());
            Assert.Equal(3, activeEffect.RemainingDuration);

            // Turn 3
            Assert.False(activeEffect.TickDown());
            Assert.Equal(2, activeEffect.RemainingDuration);

            // Turn 4
            Assert.False(activeEffect.TickDown());
            Assert.Equal(1, activeEffect.RemainingDuration);

            // Turn 5 - should expire
            Assert.True(activeEffect.TickDown());
            Assert.Equal(0, activeEffect.RemainingDuration);
        }

        [Fact]
        public void RemainingDuration_CanBeModifiedExternally()
        {
            // Arrange
            var effectData = TestHelpers.CreateVulnerableEffect(intensity: 50, duration: 2);
            var activeEffect = new ActiveEffect(effectData);

            // Act - Simulate refreshing the effect
            activeEffect.RemainingDuration = 3;

            // Assert
            Assert.Equal(3, activeEffect.RemainingDuration);
        }
    }
}
