using RoguelikeMapGen;
using System;

namespace Roguelike.Logic.Handlers
{
    public class RestRoomHandler : IRoomHandler
    {
        public void Execute(GameRun run, Room room)
        {
            float healPercentage = 0.30f; 
            int healAmount = (int)Math.Floor(run.TheHero.MaxHealth * healPercentage);
            run.TheHero.Heal(healAmount);
        }
    }
}