using Roguelike.Data;
using System.Collections.Generic;
using Xunit;

namespace Roguelike.Tests.Data
{
    public class CardDataTests
    {
        [Fact]
        public void CardData_CanBeCreatedWithProperties()
        {
            // Arrange & Act
            var card = new CardData
            {
                Id = "test_card",
                Name = "Test Card",
                Description = "Test Description",
                ManaCost = 2,
                StarRating = 3,
                Type = CardType.Attack
            };

            // Assert
            Assert.Equal("test_card", card.Id);
            Assert.Equal("Test Card", card.Name);
            Assert.Equal("Test Description", card.Description);
            Assert.Equal(2, card.ManaCost);
            Assert.Equal(3, card.StarRating);
            Assert.Equal(CardType.Attack, card.Type);
        }

        [Fact]
        public void CardData_Actions_InitializesAsEmptyList()
        {
            // Arrange & Act
            var card = new CardData();

            // Assert
            Assert.NotNull(card.Actions);
            Assert.Empty(card.Actions);
        }

        [Fact]
        public void CardData_CanContainMultipleActions()
        {
            // Arrange
            var card = new CardData
            {
                Id = "combo_card",
                Name = "Combo Card",
                Type = CardType.Attack,
                Actions = new List<CombatActionData>
                {
                    new CombatActionData { Type = ActionType.DealDamage, Value = 8 },
                    new CombatActionData { Type = ActionType.GainBlock, Value = 5 }
                }
            };

            // Act & Assert
            Assert.Equal(2, card.Actions.Count);
            Assert.Equal(ActionType.DealDamage, card.Actions[0].Type);
            Assert.Equal(ActionType.GainBlock, card.Actions[1].Type);
        }

        [Fact]
        public void CardData_SupportsAllCardTypes()
        {
            // Arrange & Act & Assert
            var attack = new CardData { Type = CardType.Attack };
            var skill = new CardData { Type = CardType.Skill };
            var power = new CardData { Type = CardType.Power };

            Assert.Equal(CardType.Attack, attack.Type);
            Assert.Equal(CardType.Skill, skill.Type);
            Assert.Equal(CardType.Power, power.Type);
        }
    }
}
