using Roguelike.Core.Map;

namespace Roguelike.Core.Handlers
{


    public interface IRoomHandler
    {


        void Execute(GameRun run, Room room);
    }
}
