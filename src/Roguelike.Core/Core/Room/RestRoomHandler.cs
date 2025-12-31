using Roguelike.Core.Map;
using System;

namespace Roguelike.Core.Handlers
{
    public class RestRoomHandler : IRoomHandler
    {
        public void Execute(GameRun run, Room room)
        {
            float baseHealPercentage = 0.30f;
            
            int healAmount = (int)Math.Floor(run.TheHero.MaxHealth * baseHealPercentage);
            run.TheHero.Heal(healAmount);
            run.CurrentState = GameState.OnMap;
        }
    }
}
