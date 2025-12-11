namespace Roguelike.Data
{
    /// <summary>
    /// Specifies the target of an action or effect
    /// </summary>
    public enum TargetType
    {
        Self,             // The caster of the effect
        SingleOpponent,   // A single chosen opponent
        AllOpponents,     // All opponents
        RandomOpponent    // A randomly chosen opponent
    }

    /// <summary>
    /// Determines how long an effect lasts
    /// </summary>
    public enum DecayType
    {
        EndOfTurn,        // The effect wears off at the end of the current turn
        AfterXTURNS,      // The effect lasts for a specific number of turns
        EndOfCombat,      // The effect lasts until the current combat ends
        Permanent         // The effect lasts for the entire run
    }

    /// <summary>
    /// Classifies the primary function of a card
    /// </summary>
    public enum CardType
    {
        Attack,  // Primarily deals damage to enemies
        Skill,   // Primarily provides Block, card manipulation, or applies effects
        Power    // Provides a buff that typically lasts for the entire combat
    }

    /// <summary>
    /// Defines the fundamental types of actions that can be performed in combat
    /// </summary>
    public enum ActionType
    {
        DealDamage,
        GainBlock,
        ApplyStatusEffect,
        ApplyDeckEffect
    }

    /// <summary>
    /// The specific types of status effects that can be applied to a combatant.
    /// The game's logic will use this to determine the effect's behavior
    /// </summary>
    public enum StatusEffectType
    {
        Vulnerable, // Takes more damage
        Weakened,  // Deals less damage
        Strength,   // Deals more damage
        Frail,      // Gains less block
        Pierced,     // Damage bypasses block
        Philosophical // Gains mana
    }

    /// <summary>
    /// The specific types of effects that can be applied to the hero's deck/hand
    /// </summary>
    public enum DeckEffectType
    {
        DrawCard,
        DiscardCard,
        FreezeCard,
        DuplicateCard
    }

    /// <summary>
    /// Defines the specific outcomes that can occur as a result of an event choice.
    /// </summary>
    public enum EventEffectType
    {
        GainGold,
        LoseGold,
        LoseHP,
        HealHP,
        RemoveCard,
        GainCard,
        GainRelic,
        Quit
    }
}