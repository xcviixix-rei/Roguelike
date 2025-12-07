using Roguelike.Data;

namespace Roguelike.Logic
{
    /// <summary>
    /// Represents a live instance of a status effect currently affecting a combatant.
    /// It wraps the static EffectData with stateful information like stacks and duration
    /// </summary>
    public class ActiveEffect
    {
        /// <summary>
        /// A reference to the immutable data template for this effect
        /// </summary>
        public EffectData SourceData { get; }

        /// <summary>
        /// The current number of stacks of the effect
        /// </summary>
        public int Stacks { get; set; }

        /// <summary>
        /// The remaining duration in turns. Only used for effects with DecayType.AfterXTURNS.
        /// A value of 0 means it has expired
        /// </summary>
        public int Duration { get; set; }

        public ActiveEffect(EffectData sourceData, int initialStacks)
        {
            SourceData = sourceData;
            Stacks = initialStacks;

            // A common convention: for effects that last X turns, the initial duration
            // is often equal to the number of stacks applied.
            if (sourceData.Decay == DecayType.AfterXTURNS)
            {
                Duration = initialStacks;
            }
        }

        /// <summary>
        /// Reduces the duration of the effect by one turn.
        /// This should be called at the start or end of a combatant's turn.
        /// </summary>
        /// <returns>True if the effect has expired after ticking down, otherwise false</returns>
        public bool TickDown()
        {
            if (SourceData.Decay == DecayType.AfterXTURNS)
            {
                Duration--;
                if (Duration <= 0)
                {
                    return true;
                }
            }
            return false;
        }
    }
}