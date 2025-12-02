/// <summary>
/// Defines the primary function of a card.
/// </summary>
public enum CardType
{
    Attack,
    Skill,
    Power
}

/// <summary>
/// Defines the rarity of a card.
/// </summary>
public enum CardRarity
{
    Common,
    Uncommon,
    Rare,
    Legendary
}

/// <summary>
/// Defines who or what a card can be targeted at.
/// </summary>
public enum TargetType
{
    Self,
    SingleEnemy,
    AllEnemies,
    RandomEnemy
}

/// <summary>
/// Categorizes a status effect as beneficial or detrimental.
/// </summary>
public enum StatusEffectType
{
    Buff,
    Debuff
}

/// <summary>
/// Defines the condition under which a status effect's duration decreases or expires.
/// </summary>
public enum StatusEffectDecayType
{
    OnTurnEnd,      // Effect ticks down at the end of a combatant's turn (e.g., Vulnerable)
    OnDamageTaken,  // Effect is removed after taking damage (e.g., Temporary Shield)
    Permanent       // Effect does not decay naturally (e.g., Strength Up)
}