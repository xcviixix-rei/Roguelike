using RoguelikeMapGen;
using System;

namespace Roguelike.Logic.Handlers
{
    public class RestRoomHandler : IRoomHandler
    {
        public void Execute(GameRun run, Room room)
        {
            var roomConfig = run.RoomConfigs[room.Type];
            int healAmount = (int)Math.Floor(run.Rng.Next((int)roomConfig.MinValue, (int)roomConfig.MaxValue) * run.TheHero.MaxHealth / 100f);
            run.TheHero.Heal(healAmount);
        }
    }
}