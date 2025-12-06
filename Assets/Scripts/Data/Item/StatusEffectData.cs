using System;

namespace RogueLike.Data
{
    [Serializable]
    public class StatusEffectData
    {
        public StatusEffectType type;
        public StatusDecayType decayType;
        
        /// <summary>
        /// The potency of the effect (e.g., the '5' in Poison 5, or the '2' in Strength 2).
        /// </summary>
        public int amount;

        /// <summary>
        /// The number of turns the effect lasts. Primarily for 'DecaysByTurn' types.
        /// </summary>
        public int duration;
    }
}