namespace Roguelike.Data
{
    /// <summary>
    /// Data container for a special effect that can be applied to the hero's deck.
    /// Inherits common properties from EffectData
    /// </summary>
    public class DeckEffectData : EffectData
    {
        /// <summary>
        /// The specific type of deck effect this is, which dictates its in-game logic.
        /// </summary>
        public DeckEffectType EffectType { get; set; }
    }
}
