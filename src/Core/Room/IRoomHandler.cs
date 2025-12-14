using Roguelike.Core.Map;

namespace Roguelike.Core.Handlers
{
    /// <summary>
    /// An interface for classes that handle the logic for a specific room type.
    /// </summary>
    public interface IRoomHandler
    {
        /// <summary>
        /// Executes the logic for the given room, modifying the game state.
        /// </summary>
        /// <param name="run">The current state of the game run.</param>
        /// <param name="room">The specific room the player has entered.</param>
        void Execute(GameRun run, Room room);
    }
}
