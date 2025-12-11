using RoguelikeMapGen;
using System.Collections.Generic;
using System.Linq;

namespace Roguelike.Logic
{
    /// <summary>
    /// Manages the state of the game map for a single run, including the
    /// player's current position and available paths.
    /// </summary>
    public class MapManager
    {
        public MapGraph CurrentMap { get; private set; }
        public int CurrentNodeId { get; private set; } = -1;

        /// <summary>
        /// Generates a new map using the provided seed.
        /// </summary>
        public void GenerateNewMap(int seed)
        {
            var generator = new MapGenerator(seed);
            CurrentMap = generator.Generate();
        }

        /// <summary>
        /// Retrieves the Room object for the player's current position.
        /// </summary>
        public Room GetCurrentRoom()
        {
            return CurrentNodeId == -1 ? null : CurrentMap.Rooms[CurrentNodeId];
        }

        /// <summary>
        /// Gets a list of rooms the player can move to from their current position.
        /// </summary>
        public List<Room> GetPossibleNextNodes()
        {
            if (CurrentMap == null) return new List<Room>();

            if (CurrentNodeId == -1)
            {
                return CurrentMap.RoomsOnFloor(0).ToList();
            }

            var currentRoom = GetCurrentRoom();
            if (currentRoom == null) return new List<Room>();

            return currentRoom.Outgoing.Select(id => CurrentMap.Rooms[id]).ToList();
        }

        /// <summary>
        /// Attempts to move the player to a new node.
        /// </summary>
        /// <param name="nodeId">The ID of the destination room.</param>
        /// <returns>True if the move was valid and successful, false otherwise.</returns>
        public bool MoveToNode(int nodeId)
        {
            var possibleMoves = GetPossibleNextNodes();
            if (possibleMoves.Any(r => r.Id == nodeId))
            {
                CurrentNodeId = nodeId;
                return true;
            }
            return false;
        }
    }
}