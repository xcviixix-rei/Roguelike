using System;
using System.Collections.Generic;

namespace RogueLike.Data
{
    [Serializable]
    public class EnemyData
    {
        public string enemyID;
        public string name;
        public int maxHealthMin;
        public int maxHealthMax;

        public List<EnemyAbility> abilityDeck = new List<EnemyAbility>();
    }
}