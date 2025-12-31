namespace Roguelike.Data
{
    /// <summary>
    /// Represents a single, discrete combat action that can be part of a card or enemy ability
    /// </summary>
    public class CombatActionData
    {
        /// <summary>
        /// The type of action to be performed (e.g., DealDamage, GainBlock)
        /// </summary>
        public ActionType Type { get; set; }

        /// <summary>
        /// The numerical value associated with the action
        /// </summary>
        public int Value { get; set; }

        /// <summary>
        /// Who the action is directed at
        /// </summary>
        public TargetType Target { get; set; }

        /// <summary>
        /// The unique identifier for an effect to be applied.
        /// This is only used when the 'Type' is ApplyStatusEffect or ApplyDeckEffect.
        /// It will correspond to the 'Id' property of a StatusEffectData or DeckEffectData object.
        /// </summary>
        public string EffectId { get; set; }

        public CombatActionData() { }

        public CombatActionData(ActionType type, int value, TargetType target, string effectId = null)
        {
            Type = type;
            Value = value;
            Target = target;
            EffectId = effectId;
        }
    }
}
