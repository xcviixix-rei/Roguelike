using System;
using System.Collections.Generic;

namespace RogueLike.Data
{
    [Serializable]
    public class CardData
    {
        public string cardID;
        public string cardName;
        public int manaCost;
        public int goldCost;
        public CardType type;
        public Rarity rarity;
        public TargetType defaultTarget;
        
        public string descriptionText; 

        public List<CardEffectData> effects = new List<CardEffectData>();
    }
}