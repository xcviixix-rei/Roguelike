namespace Roguelike.Data
{


    public abstract class EffectData
    {


        public string Id { get; set; }


        public string Name { get; set; }


        public string Description { get; set; }


        public int Intensity { get; set; }


        public IntensityType IntensityType { get; set; }


        public int Duration { get; set; } = 1;


        public ApplyType ApplyType { get; set; }


        public DecayType Decay { get; set; }


        public TargetType Target { get; set; }
    }
}
