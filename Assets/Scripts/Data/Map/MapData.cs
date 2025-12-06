using System;
using System.Collections.Generic;

namespace RogueLike.Data
{
    [Serializable]
    public class MapData
    {
        public List<List<MapNode>> nodesByFloor = new List<List<MapNode>>();
    }
}