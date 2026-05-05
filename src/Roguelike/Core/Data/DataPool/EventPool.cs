using System.Collections.Generic;

namespace Roguelike.Data
{


    public class EventPool
    {
        public Dictionary<string, EventChoiceSet> EventsById { get; set; } = new Dictionary<string, EventChoiceSet>();


    }
}
