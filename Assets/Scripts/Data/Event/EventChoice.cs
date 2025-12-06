using System.Collections.Generic;

namespace Roguelike.Data
{
    /// <summary>
    /// Represents one of the options a player can choose during an event
    /// </summary>
    public class EventChoice
    {
        /// <summary>
        /// The text displayed on the button for this choice
        /// </summary>
        public string ChoiceText { get; set; }

        /// <summary>
        /// A list of all the effects that will be triggered if this choice is made.
        /// Can be one or more effects
        /// </summary>
        public List<EventEffect> Effects { get; set; } = new List<EventEffect>();
    }
}