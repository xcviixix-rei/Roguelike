using System.Collections.Generic;

namespace Roguelike.Data
{


    public class CardData
    {


        public string Id { get; set; }


        public string Name { get; set; }


        public string Description { get; set; }


        public int ManaCost { get; set; }


        public int StarRating { get; set; }


        public CardType Type { get; set; }


        public List<CombatActionData> Actions { get; set; } = new List<CombatActionData>();


        public CardData() { }
    }
}
