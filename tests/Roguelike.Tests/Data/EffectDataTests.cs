using Roguelike.Data;
using Xunit;

namespace Roguelike.Tests.Data
{
    public class EffectDataTests
    {
        [Fact]
        public void StatusEffectData_CanBeCreatedWithAllProperties()
        {
            // Arrange & Act
            var effect = new StatusEffectData
            {
                Id = "test_strength",
                Name = "Strength",
                Description = "Increases damage",
                EffectType = StatusEffectType.Strength,
                Intensity = 2,
                IntensityType = IntensityType.Flat,
                Duration = 3,
                Decay = DecayType.AfterXTURNS,
                ApplyType = ApplyType.RightAway
            };

            // Assert
            Assert.Equal("test_strength", effect.Id);
            Assert.Equal("Strength", effect.Name);
            Assert.Equal(StatusEffectType.Strength, effect.EffectType);
            Assert.Equal(2, effect.Intensity);
            Assert.Equal(IntensityType.Flat, effect.IntensityType);
            Assert.Equal(3, effect.Duration);
            Assert.Equal(DecayType.AfterXTURNS, effect.Decay);
            Assert.Equal(ApplyType.RightAway, effect.ApplyType);
        }

        [Fact]
        public void StatusEffectData_SupportsAllEffectTypes()
        {
            // Arrange & Act & Assert
            var vulnerable = new StatusEffectData { EffectType = StatusEffectType.Vulnerable };
            var weakened = new StatusEffectData { EffectType = StatusEffectType.Weakened };
            var strength = new StatusEffectData { EffectType = StatusEffectType.Strength };
            var frail = new StatusEffectData { EffectType = StatusEffectType.Frail };
            var pierced = new StatusEffectData { EffectType = StatusEffectType.Pierced };
            var philosophical = new StatusEffectData { EffectType = StatusEffectType.Philosophical };
            var immediateBlock = new StatusEffectData { EffectType = StatusEffectType.ImmediateBlock };

            Assert.Equal(StatusEffectType.Vulnerable, vulnerable.EffectType);
            Assert.Equal(StatusEffectType.Weakened, weakened.EffectType);
            Assert.Equal(StatusEffectType.Strength, strength.EffectType);
            Assert.Equal(StatusEffectType.Frail, frail.EffectType);
            Assert.Equal(StatusEffectType.Pierced, pierced.EffectType);
            Assert.Equal(StatusEffectType.Philosophical, philosophical.EffectType);
            Assert.Equal(StatusEffectType.ImmediateBlock, immediateBlock.EffectType);
        }

        [Fact]
        public void StatusEffectData_SupportsBothIntensityTypes()
        {
            // Arrange & Act
            var flatEffect = new StatusEffectData
            {
                IntensityType = IntensityType.Flat,
                Intensity = 5
            };

            var percentEffect = new StatusEffectData
            {
                IntensityType = IntensityType.Percentage,
                Intensity = 50
            };

            // Assert
            Assert.Equal(IntensityType.Flat, flatEffect.IntensityType);
            Assert.Equal(IntensityType.Percentage, percentEffect.IntensityType);
        }

        [Fact]
        public void StatusEffectData_SupportsBothDecayTypes()
        {
            // Arrange & Act
            var temporaryEffect = new StatusEffectData
            {
                Decay = DecayType.AfterXTURNS,
                Duration = 3
            };

            var permanentEffect = new StatusEffectData
            {
                Decay = DecayType.Permanent,
                Duration = 0
            };

            // Assert
            Assert.Equal(DecayType.AfterXTURNS, temporaryEffect.Decay);
            Assert.Equal(3, temporaryEffect.Duration);
            Assert.Equal(DecayType.Permanent, permanentEffect.Decay);
        }

        [Fact]
        public void DeckEffectData_SupportsAllDeckEffectTypes()
        {
            // Arrange & Act & Assert
            var draw = new DeckEffectData { EffectType = DeckEffectType.DrawCard };
            var discard = new DeckEffectData { EffectType = DeckEffectType.DiscardCard };
            var freeze = new DeckEffectData { EffectType = DeckEffectType.FreezeCard };
            var duplicate = new DeckEffectData { EffectType = DeckEffectType.DuplicateCard };

            Assert.Equal(DeckEffectType.DrawCard, draw.EffectType);
            Assert.Equal(DeckEffectType.DiscardCard, discard.EffectType);
            Assert.Equal(DeckEffectType.FreezeCard, freeze.EffectType);
            Assert.Equal(DeckEffectType.DuplicateCard, duplicate.EffectType);
        }

        [Fact]
        public void EffectData_SupportsAllApplyTypes()
        {
            // Arrange & Act & Assert
            var rightAway = new StatusEffectData { ApplyType = ApplyType.RightAway };
            var startOfCombat = new StatusEffectData { ApplyType = ApplyType.StartOfCombat };
            var startOfTurn = new StatusEffectData { ApplyType = ApplyType.StartOfTurn };

            Assert.Equal(ApplyType.RightAway, rightAway.ApplyType);
            Assert.Equal(ApplyType.StartOfCombat, startOfCombat.ApplyType);
            Assert.Equal(ApplyType.StartOfTurn, startOfTurn.ApplyType);
        }
    }
}
