using Roguelike.Data;
using Roguelike.Core.Map;
using System.Linq;

namespace Roguelike.Core.Handlers
{
    public class BossRoomHandler : IRoomHandler
    {
        public void Execute(GameRun run, Room room)
        {
            
            var boss = run.EnemyPool.GetRandomEnemyOfStar(5, run.Rng);
            if (boss == null) return;

            var encounter = new System.Collections.Generic.List<EnemyData> { boss };

            run.CurrentCombat = new CombatManager(run.TheHero, encounter, run.Rng, run.EffectPool.GetEffect);
            run.CurrentCombat.StartCombat();
            run.CurrentState = GameState.InCombat;
        }
    }
}
