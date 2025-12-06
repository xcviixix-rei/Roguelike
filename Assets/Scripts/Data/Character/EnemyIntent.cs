using System;

namespace RogueLike.Data
{
    [Serializable]
    public class EnemyIntent
    {
        public IntentType type;
        public int damageValue;
        public int blockValue;
    }
}