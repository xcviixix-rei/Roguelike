using System.Collections.Generic;

namespace Roguelike.Data
{


    public class EventChoiceSet
    {


        public string Id { get; set; }


        public string EventTitle { get; set; }


        public string EventDescription { get; set; }


        public List<EventChoice> Choices { get; set; } = new List<EventChoice>();
    }
}
