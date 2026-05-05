using Roguelike.Data;

namespace Roguelike.Core
{


    public class ActiveEffect
    {


        public EffectData SourceData { get; }


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
                RemainingDuration = int.MaxValue;
            }
        }


        public bool TickDown()
        {
            if (SourceData.Decay == DecayType.AfterXTURNS)
            {
                RemainingDuration--;
                return RemainingDuration <= 0;
            }
            return false;
        }
    }
}
