using System;
using System.Collections.Generic;

namespace RogueLike.Data
{
    [Serializable]
    public class CardEffectData
    {
        public EffectType type;
        public int value;
        public TargetType targetOverride;
        public StatusEffectType statusToApply;
        public int statusAmount;
    }
}