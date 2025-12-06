using System;

namespace RogueLike.Data
{
    [Serializable]
    public class RelicData
    {
        public string relicID;
        public string name;
        public string description;
        public Rarity rarity;
        public int goldCost;
    }
}