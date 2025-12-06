using RoguelikeMapGen;

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
        /// The minimum difficulty rating for this room.
        /// For Combat Rooms (Monster, Elite, Boss): Defines the lower bound for encounter generation.
        /// For Shop Rooms: Can be used as a "shop score" to determine the quality of items for sale.
        /// For other rooms, this may not be used.
        /// </summary>
        public float MinDifficulty { get; set; }

        /// <summary>
        /// The maximum difficulty rating for this room.
        /// For Combat Rooms (Monster, Elite, Boss): Defines the upper bound for encounter generation.
        /// For Shop Rooms: Can be used as a "shop score" to determine the quality of items for sale.
        /// For other rooms, this may not be used.
        /// </summary>
        public float MaxDifficulty { get; set; }

        /// <summary>
        /// Parameterless constructor for serialization.
        /// </summary>
        public RoomData() { }
    }
}