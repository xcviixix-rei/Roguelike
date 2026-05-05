using System.Collections.Generic;

namespace Roguelike.Data
{


    public class HeroData : CombatantData
    {


        public int StartingGold { get; set; }


        public int StartingMana { get; set; }


        public int StartingHandSize { get; set; }


        public List<string> StartingDeckCardIds { get; set; } = new List<string>();


        public string StartingRelicId { get; set; }
    }
}
