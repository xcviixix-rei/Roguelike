using System;
using System.Collections.Generic;

namespace RogueLike.Data
{
    [Serializable]
    public abstract class CharacterState
    {
        public int maxHealth;
        public int currentHealth;
        public int currentBlock;
        public List<StatusEffectData> statusEffects = new List<StatusEffectData>();
    }
}