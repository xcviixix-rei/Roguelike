using Roguelike.Core;
using Roguelike.Data;
using Xunit;

namespace Roguelike.Tests.Combat
{
    public class EnemyTests
    {
        [Fact]
        public void Constructor_InitializesFromEnemyData()
        {
            // Arrange
            var enemyData = TestHelpers.CreateBasicEnemyData("test_enemy", health: 50);
            var rng = new System.Random(42);

            // Act
            var enemy = new Enemy(enemyData, rng);

            // Assert
            Assert.Equal(50, enemy.MaxHealth);
            Assert.Equal(50, enemy.CurrentHealth);
            Assert.Equal(0, enemy.Block);
            Assert.Same(enemyData, enemy.SourceData);
        }

        [Fact]
        public void Enemy_InheritsCombatantBehavior()
        {
            // Arrange
            var enemyData = TestHelpers.CreateBasicEnemyData("test_enemy", health: 50);
            var rng = new System.Random(42);
            var enemy = new Enemy(enemyData, rng);

            // Act - test inherited methods
            enemy.TakeDamage(15);
            enemy.GainBlock(8);
            enemy.Heal(5);

            // Assert
            Assert.Equal(40, enemy.CurrentHealth); // 50 - 15 + 5
            Assert.Equal(8, enemy.Block);
        }

        [Fact]
        public void Enemy_CanApplyEffects()
        {
            // Arrange
            var enemyData = TestHelpers.CreateBasicEnemyData("test_enemy", health: 50);
            var rng = new System.Random(42);
            var enemy = new Enemy(enemyData, rng);
            var strengthEffect = TestHelpers.CreateStrengthEffect();

            // Act
            enemy.ApplyEffect(strengthEffect);

            // Assert
            Assert.Single(enemy.ActiveEffects);
        }

        [Fact]
        public void Enemy_CanBeDefeated()
        {
            // Arrange
            var enemyData = TestHelpers.CreateBasicEnemyData("test_enemy", health: 50);
            var rng = new System.Random(42);
            var enemy = new Enemy(enemyData, rng);

            // Act
            enemy.TakeDamage(100);

            // Assert
            Assert.Equal(0, enemy.CurrentHealth);
        }
    }
}
