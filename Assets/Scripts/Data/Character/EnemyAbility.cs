using System;
using System.Collections.Generic;

namespace RogueLike.Data
{
    [Serializable]
    public class EnemyAbility
    {
        public string abilityName;
        // The weight for the AI to pick this move from its available moveset.
        public int weight;
        public List<CardEffectData> effects = new List<CardEffectData>();
    }
}