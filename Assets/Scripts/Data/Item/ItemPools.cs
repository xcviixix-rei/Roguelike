using System;
using System.Collections.Generic;

namespace RogueLike.Data
{
    [Serializable]
    public class CardPool
    {
        public Dictionary<Rarity, List<CardData>> cardsByRarity = new Dictionary<Rarity, List<CardData>>();
    }

    [Serializable]
    public class RelicPool
    {
        public Dictionary<Rarity, List<RelicData>> relicsByRarity = new Dictionary<Rarity, List<RelicData>>();
    }
}