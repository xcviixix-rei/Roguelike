namespace Roguelike.Data
{


    public enum TargetType
    {
        Self,
        SingleOpponent,
        AllOpponents,
        RandomOpponent
    }


    public enum ApplyType
    {
        RightAway,
        StartOfCombat,
        StartOfTurn
    }


    public enum IntensityType
    {
        Flat,
        Percentage
    }


    public enum DecayType
    {
        AfterXTURNS,
        Permanent
    }


    public enum CardType
    {
        Attack,
        Skill,
        Power
    }


    public enum ActionType
    {
        DealDamage,
        GainBlock,
        ApplyStatusEffect,
        ApplyDeckEffect
    }


    public enum StatusEffectType
    {
        Vulnerable,
        Weakened,
        Strength,
        Frail,
        Pierced,
        Philosophical,
        ImmediateBlock
    }


    public enum DeckEffectType
    {
        DrawCard,
        DiscardCard,
        FreezeCard,
        DuplicateCard
    }


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
