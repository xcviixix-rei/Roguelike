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

            var enemyData = TestHelpers.CreateBasicEnemyData("test_enemy", health: 50);
            var rng = new System.Random(42);


            var enemy = new Enemy(enemyData, rng);


            Assert.Equal(50, enemy.MaxHealth);
            Assert.Equal(50, enemy.CurrentHealth);
            Assert.Equal(0, enemy.Block);
            Assert.Same(enemyData, enemy.SourceData);
        }

        [Fact]
        public void Enemy_InheritsCombatantBehavior()
        {

            var enemyData = TestHelpers.CreateBasicEnemyData("test_enemy", health: 50);
            var rng = new System.Random(42);
            var enemy = new Enemy(enemyData, rng);


            enemy.TakeDamage(15);
            enemy.GainBlock(8);
            enemy.Heal(5);


            Assert.Equal(40, enemy.CurrentHealth);
            Assert.Equal(8, enemy.Block);
        }

        [Fact]
        public void Enemy_CanApplyEffects()
        {

            var enemyData = TestHelpers.CreateBasicEnemyData("test_enemy", health: 50);
            var rng = new System.Random(42);
            var enemy = new Enemy(enemyData, rng);
            var strengthEffect = TestHelpers.CreateStrengthEffect();


            enemy.ApplyEffect(strengthEffect);


            Assert.Single(enemy.ActiveEffects);
        }

        [Fact]
        public void Enemy_CanBeDefeated()
        {

            var enemyData = TestHelpers.CreateBasicEnemyData("test_enemy", health: 50);
            var rng = new System.Random(42);
            var enemy = new Enemy(enemyData, rng);


            enemy.TakeDamage(100);


            Assert.Equal(0, enemy.CurrentHealth);
        }
    }
}
