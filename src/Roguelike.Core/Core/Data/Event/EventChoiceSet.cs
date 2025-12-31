using System.Collections.Generic;

namespace Roguelike.Data
{
    /// <summary>
    /// Represents a complete event encounter, including its descriptive text
    /// and all the available choices for the player
    /// </summary>
    public class EventChoiceSet
    {
        /// <summary>
        /// A unique, machine-readable identifier for this event
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// The title of the event displayed to the player
        /// </summary>
        public string EventTitle { get; set; }

        /// <summary>
        /// The main story or descriptive text of the event
        /// </summary>
        public string EventDescription { get; set; }

        /// <summary>
        /// The list of choices available to the player in this event
        /// </summary>
        public List<EventChoice> Choices { get; set; } = new List<EventChoice>();
    }
}
