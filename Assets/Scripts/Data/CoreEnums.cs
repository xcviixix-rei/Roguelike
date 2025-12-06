namespace RogueLike.Data
{
    // General
    public enum Rarity { Common, Uncommon, Rare, Special, Boss }
    public enum TargetType { Self, SingleEnemy, AllEnemies, RandomEnemy }

    // Card Specific
    public enum CardType { Attack, Skill, Power }
    
    // Effect & Status Specific
    public enum EffectType { Damage, Block, Draw, ApplyStatus, GainMana, Heal }
    public enum StatusEffectType { Vulnerable, Weak, Strength, Poison, Thorns, Regen }
    public enum StatusDecayType { Temporary, DecaysByTurn, DecaysOnUse, Permanent }

    // Map & Room Specific
    public enum NodeType { Combat, EliteCombat, Rest, Shop, Event, Boss }
    
    // Enemy Specific
    public enum IntentType { Attack, AttackDefend, Defend, Buff, Debuff }
}