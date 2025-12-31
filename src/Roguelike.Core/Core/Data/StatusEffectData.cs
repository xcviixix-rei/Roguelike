namespace Roguelike.Data
{
    /// <summary>
    /// Data container for a status effect that can be applied to a combatant.
    /// Inherits common properties from EffectData.
    /// </summary>
    public class StatusEffectData : EffectData
    {
        /// <summary>
        /// The specific type of status effect this is, which dictates its in-game logic.
        /// </summary>
        public StatusEffectType EffectType { get; set; }
    }
}
