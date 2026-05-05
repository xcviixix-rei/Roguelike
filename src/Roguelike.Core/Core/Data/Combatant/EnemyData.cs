

using System.Collections.Generic;

namespace Roguelike.Data
{


    public class EnemyData : CombatantData
    {


        public int StarRating { get; set; }


        public bool IsBoss { get; set; } = false;


        public List<WeightedChoice<CombatActionData>> ActionSet { get; set; } = new List<WeightedChoice<CombatActionData>>();


        public int SpecialAbilityCooldown { get; set; } = 1;
    }
}
