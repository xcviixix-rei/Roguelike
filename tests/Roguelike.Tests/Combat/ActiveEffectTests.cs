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

            var effectData = TestHelpers.CreateVulnerableEffect(intensity: 50, duration: 3);


            var activeEffect = new ActiveEffect(effectData);


            Assert.Equal(3, activeEffect.RemainingDuration);
            Assert.Same(effectData, activeEffect.SourceData);
        }

        [Fact]
        public void Constructor_WithPermanentEffect_SetsMaxDuration()
        {

            var effectData = TestHelpers.CreateStrengthEffect(intensity: 2, decay: DecayType.Permanent);


            var activeEffect = new ActiveEffect(effectData);


            Assert.Equal(int.MaxValue, activeEffect.RemainingDuration);
        }

        [Fact]
        public void TickDown_TemporaryEffect_DecrementsDuration()
        {

            var effectData = TestHelpers.CreateVulnerableEffect(intensity: 50, duration: 3);
            var activeEffect = new ActiveEffect(effectData);


            bool expired = activeEffect.TickDown();


            Assert.Equal(2, activeEffect.RemainingDuration);
            Assert.False(expired);
        }

        [Fact]
        public void TickDown_TemporaryEffect_ReturnsTrue_WhenExpired()
        {

            var effectData = TestHelpers.CreateVulnerableEffect(intensity: 50, duration: 1);
            var activeEffect = new ActiveEffect(effectData);


            bool expired = activeEffect.TickDown();


            Assert.Equal(0, activeEffect.RemainingDuration);
            Assert.True(expired);
        }

        [Fact]
        public void TickDown_PermanentEffect_NeverExpires()
        {

            var effectData = TestHelpers.CreateStrengthEffect(intensity: 2, decay: DecayType.Permanent);
            var activeEffect = new ActiveEffect(effectData);


            bool expired1 = activeEffect.TickDown();
            bool expired2 = activeEffect.TickDown();
            bool expired3 = activeEffect.TickDown();


            Assert.False(expired1);
            Assert.False(expired2);
            Assert.False(expired3);
            Assert.Equal(int.MaxValue, activeEffect.RemainingDuration);
        }

        [Fact]
        public void TickDown_MultipleTurns_CountsDownCorrectly()
        {

            var effectData = TestHelpers.CreateWeakenedEffect(intensity: 25, duration: 5);
            var activeEffect = new ActiveEffect(effectData);


            Assert.False(activeEffect.TickDown());
            Assert.Equal(4, activeEffect.RemainingDuration);


            Assert.False(activeEffect.TickDown());
            Assert.Equal(3, activeEffect.RemainingDuration);


            Assert.False(activeEffect.TickDown());
            Assert.Equal(2, activeEffect.RemainingDuration);


            Assert.False(activeEffect.TickDown());
            Assert.Equal(1, activeEffect.RemainingDuration);


            Assert.True(activeEffect.TickDown());
            Assert.Equal(0, activeEffect.RemainingDuration);
        }

        [Fact]
        public void RemainingDuration_CanBeModifiedExternally()
        {

            var effectData = TestHelpers.CreateVulnerableEffect(intensity: 50, duration: 2);
            var activeEffect = new ActiveEffect(effectData);


            activeEffect.RemainingDuration = 3;


            Assert.Equal(3, activeEffect.RemainingDuration);
        }
    }
}
