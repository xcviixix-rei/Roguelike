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
    /// Determines when an effect is applied
    /// </summary>
    public enum ApplyType
    {
        RightAway,      // Applied immediately
        StartOfCombat,  // Applied at the start of combat
        StartOfTurn     // Applied at the start of each turn
    }

    /// <summary>
    /// Determines how the intensity of an effect is interpreted
    /// </summary>
    public enum IntensityType
    {
        Flat,        // Direct addition/subtraction (e.g., Strength: +3 damage)
        Percentage   // Multiplier as percentage (e.g., Vulnerable: 50 = 50% more = 1.5x total)
    }

    /// <summary>
    /// Determines how long an effect lasts
    /// </summary>
    public enum DecayType
    {
        AfterXTURNS,      // The effect lasts for a specific number of turns (specified by Duration field)
        Permanent         // The effect lasts for the entire run
    }

    /// <summary>
    /// Classifies the primary function of a card
    /// </summary>
    public enum CardType
    {
        Attack,  // Primarily deals damage to enemies
        Skill,   // Primarily provides Block, card manipulation, or applies effects
        Heal,    // Primarily heals the player (NEW)
        Power    // Provides a buff that typically lasts for the entire combat
    }

    /// <summary>
    /// Defines the fundamental types of actions that can be performed in combat
    /// </summary>
    public enum ActionType
    {
        DealDamage,
        GainBlock,
        GainHealth,  // NEW: Heal the player
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
        Philosophical, // Gains mana
        ImmediateBlock // Gains block immediately (one-time)
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
