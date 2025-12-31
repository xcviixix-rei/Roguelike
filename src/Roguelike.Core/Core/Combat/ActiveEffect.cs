using Roguelike.Data;

namespace Roguelike.Core
{
    /// <summary>
    /// Represents a live instance of a status effect currently affecting a combatant.
    /// It wraps the static EffectData with stateful information like remaining duration
    /// </summary>
    public class ActiveEffect
    {
        /// <summary>
        /// A reference to the immutable data template for this effect
        /// </summary>
        public EffectData SourceData { get; }

        /// <summary>
        /// The remaining duration in turns. Only used for effects with DecayType.AfterXTURNS.
        /// A value of 0 means it has expired
        /// </summary>
        public int RemainingDuration { get; set; }

        public ActiveEffect(EffectData sourceData)
        {
            SourceData = sourceData;

            if (sourceData.Decay == DecayType.AfterXTURNS)
            {
                RemainingDuration = sourceData.Duration;
            }
            else
            {
                RemainingDuration = int.MaxValue; // Permanent effects never expire
            }
        }

        /// <summary>
        /// Reduces the duration of the effect by one turn.
        /// This should be called at the end of a combatant's turn.
        /// </summary>
        /// <returns>True if the effect has expired after ticking down, otherwise false</returns>
        public bool TickDown()
        {
            if (SourceData.Decay == DecayType.AfterXTURNS)
            {
                RemainingDuration--;
                return RemainingDuration <= 0;
            }
            return false; // Permanent effects never expire
        }
    }
}
