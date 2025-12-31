using Roguelike.Core.Map;

namespace Roguelike.Data
{
    /// <summary>
    /// Holds configuration data for a specific type of room on the map.
    /// This data is used by the Core Logic to populate a room when the player enters it
    /// </summary>
    public class RoomData
    {
        /// <summary>
        /// The type of room this data applies to. This acts as the unique identifier
        /// </summary>
        public RoomType Type { get; set; }

        /// <summary>
        /// The display name for this room type
        /// </summary>
        public string DisplayName { get; set; }

        /// <summary>
        /// A brief description of the room, potentially for UI tooltips
        /// </summary>
        public string Description { get; set; }
        
        /// <summary>
        /// The default star rating for this room type.
        /// Monster rooms will typically range 1-2.
        /// Elite rooms will typically range 3-4.
        /// Boss rooms will be 5.
        /// </summary>
        public int StarRating { get; set; }

        /// <summary>
        /// Parameterless constructor for serialization.
        /// </summary>
        public RoomData() { }
    }
}
