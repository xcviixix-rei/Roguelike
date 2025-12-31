using Roguelike.Core;
using Roguelike.Data;
using System;
using Xunit;

namespace Roguelike.Tests.Combat
{
    public class HeroTests
    {
        [Fact]
        public void Constructor_InitializesFromHeroData()
        {
            // Arrange
            var heroData = TestHelpers.CreateBasicHeroData("test_hero", health: 75);
            var rng = new Random(42);

            // Act
            var hero = new Hero(heroData, rng);

            // Assert
            Assert.Equal(75, hero.MaxHealth);
            Assert.Equal(75, hero.CurrentHealth);
            Assert.Equal(0, hero.Block);
            Assert.Same(heroData, hero.SourceData);
        }

        [Fact]
        public void Constructor_InitializesDeckManager()
        {
            // Arrange
            var heroData = TestHelpers.CreateBasicHeroData("test_hero");
            var rng = new Random(42);

            // Act
            var hero = new Hero(heroData, rng);

            // Assert
            Assert.NotNull(hero.Deck);
        }

        [Fact]
        public void Constructor_InitializesGold()
        {
            // Arrange
            var heroData = TestHelpers.CreateBasicHeroData("test_hero");
            heroData.StartingGold = 100;
            var rng = new Random(42);

            // Act
            var hero = new Hero(heroData, rng);

            // Assert
            Assert.Equal(100, hero.CurrentGold);
        }

        [Fact]
        public void Constructor_InitializesRelicsList()
        {
            // Arrange
            var heroData = TestHelpers.CreateBasicHeroData("test_hero");
            var rng = new Random(42);

            // Act
            var hero = new Hero(heroData, rng);

            // Assert
            Assert.NotNull(hero.Relics);
            Assert.Empty(hero.Relics);
        }

        [Fact]
        public void Hero_InheritsCombatantBehavior()
        {
            // Arrange
            var heroData = TestHelpers.CreateBasicHeroData("test_hero", health: 75);
            var rng = new Random(42);
            var hero = new Hero(heroData, rng);

            // Act - test inherited methods
            hero.TakeDamage(10);
            hero.GainBlock(5);
            hero.Heal(5);

            // Assert
            Assert.Equal(70, hero.CurrentHealth); // 75 - 10 + 5
            Assert.Equal(5, hero.Block);
        }
    }
}
