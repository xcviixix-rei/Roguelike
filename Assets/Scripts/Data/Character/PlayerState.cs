using System;
using System.Collections.Generic;

namespace RogueLike.Data
{
    [Serializable]
    public class PlayerState : CharacterState
    {
        public int maxMana;
        public int currentMana;
        public int currentGold;

        public List<CardData> deck = new List<CardData>();
        public List<CardData> hand = new List<CardData>();
        public List<CardData> discardPile = new List<CardData>();
        public List<CardData> exhaustPile = new List<CardData>();
    }
}