using System.Collections.Generic;

namespace Roguelike.Data
{
    /// <summary>
    /// A data container that holds all possible EventChoiceSet templates for the game.
    /// </summary>
    public class EventPool
    {
        public Dictionary<string, EventChoiceSet> EventsById { get; set; } = new Dictionary<string, EventChoiceSet>();

        // TODO: Implement methods to retrieve events by ID.
    }
}