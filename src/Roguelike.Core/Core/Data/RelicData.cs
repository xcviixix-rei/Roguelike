using System.Collections.Generic;

namespace Roguelike.Data
{


    public class RelicData
    {


        public string Id { get; set; }


        public string Name { get; set; }


        public string Description { get; set; }


        public int StarRating { get; set; }


        public bool IsBossRelic { get; set; } = false;


        public List<EffectData> Effects { get; set; } = new List<EffectData>();


        public RelicData() { }
    }
}
