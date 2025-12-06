using System;
using System.Collections.Generic;

namespace RogueLike.Data
{
    [Serializable]
    public class GameState
    {
        public PlayerState playerState;
        public MapData gameMap;
        public int currentFloor;
        public int turnNumber;
        public List<RelicData> playerRelics = new List<RelicData>();
        
        public List<EnemyState> enemiesInCombat = new List<EnemyState>();
    }
}