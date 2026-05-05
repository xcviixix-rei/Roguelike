using System.Collections.Generic;

namespace Roguelike.Data
{


    public class EventChoice
    {


        public string ChoiceText { get; set; }


        public List<EventEffect> Effects { get; set; } = new List<EventEffect>();
    }
}
