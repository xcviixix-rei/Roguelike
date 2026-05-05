using Roguelike.Core.Map;
using System.Collections.Generic;
using System.Linq;

namespace Roguelike.Core
{


    public class MapManager
    {
        public MapGraph CurrentMap { get; private set; }
        public int CurrentNodeId { get; private set; } = -1;


        public void GenerateNewMap(int seed, Dictionary<RoomType, float> roomWeights = null,
                                   float monsterStarRatio = 0.5f, float eliteStarRatio = 0.5f)
        {
            Dictionary<RoomType, int> intWeights = null;
            if (roomWeights != null)
            {
                intWeights = new Dictionary<RoomType, int>();
                foreach (var kv in roomWeights)
                {
                    intWeights[kv.Key] = (int)Math.Round(kv.Value);
                }
            }

            var generator = new MapGenerator(seed, intWeights, monsterStarRatio, eliteStarRatio);
            CurrentMap = generator.Generate();
        }


        public Room GetCurrentRoom()
        {
            return CurrentNodeId == -1 ? null : CurrentMap.Rooms[CurrentNodeId];
        }


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
