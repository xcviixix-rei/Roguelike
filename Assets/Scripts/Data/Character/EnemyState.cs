using System;

namespace RogueLike.Data
{
    [Serializable]
    public class EnemyState : CharacterState
    {
        public EnemyData sourceData; 
        public EnemyIntent currentIntent;
    }
}