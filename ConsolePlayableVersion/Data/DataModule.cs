using System;
using System.Collections.Generic;
using RoguelikeMapGen;

namespace Roguelike.Data
{
    public enum Rarity { Common, Uncommon, Rare, Legendary, Boss }
    public enum TargetType { Self, SingleOpponent, AllOpponents, RandomOpponent }
    public enum DecayType { EndOfTurn, AfterXTURNS, EndOfCombat, Permanent }
    public enum CardType { Attack, Skill, Power }
    public enum ActionType { DealDamage, GainBlock, ApplyStatusEffect, ApplyDeckEffect }
    public enum StatusEffectType { Vulnerable, Weakened, Strength, Frail, Pierced }
    public enum DeckEffectType { DrawCard, DiscardCard, FreezeCard, DuplicateCard }
    public enum EventEffectType { GainGold, LoseGold, LoseHP, HealHP, RemoveCard, GainCard, GainRelic, Quit }
    public class WeightedChoice<T>
    {
        public T Item { get; set; }
        public int Weight { get; set; }
        public WeightedChoice(T item, int weight) { Item = item; Weight = weight; }
    }
    public class CombatActionData
    {
        public ActionType Type { get; set; }
        public int Value { get; set; }
        public TargetType Target { get; set; }
        public string EffectId { get; set; }
        public CombatActionData() { }
        public CombatActionData(ActionType type, int value, TargetType target, string effectId = null)
        {
            Type = type; Value = value; Target = target; EffectId = effectId;
        }
    }

    public abstract class EffectData
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public int Value { get; set; }
        public DecayType Decay { get; set; }
        public TargetType Target { get; set; }
    }

    public class StatusEffectData : EffectData { public StatusEffectType EffectType { get; set; } }
    public class DeckEffectData : EffectData { public DeckEffectType EffectType { get; set; } }
    public class CardData
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public int ManaCost { get; set; }
        public Rarity Rarity { get; set; }
        public CardType Type { get; set; }
        public List<CombatActionData> Actions { get; set; } = new List<CombatActionData>();
    }

    public class RelicData
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public Rarity Rarity { get; set; }
        public List<EffectData> Effects { get; set; } = new List<EffectData>();
    }

    public abstract class CombatantData
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public int StartingHealth { get; set; }
        public int StartingStrength { get; set; } = 0;
    }

    public class HeroData : CombatantData
    {
        public int StartingGold { get; set; }
        public int StartingMana { get; set; }
        public int StartingHandSize { get; set; }
        public List<string> StartingDeckCardIds { get; set; } = new List<string>();
        public string StartingRelicId { get; set; }
    }

    public class EnemyData : CombatantData
    {
        public float Difficulty { get; set; }
        public List<WeightedChoice<CombatActionData>> ActionSet { get; set; } = new List<WeightedChoice<CombatActionData>>();
        public int SpecialAbilityCooldown { get; set; } = 1;
    }
    public class EventEffect { public EventEffectType Type; public int Value; public string Parameter; }
    public class EventChoice { public string ChoiceText; public List<EventEffect> Effects = new List<EventEffect>(); }
    public class EventChoiceSet { public string Id; public string EventTitle; public string EventDescription; public List<EventChoice> Choices = new List<EventChoice>(); }

    public class RoomData
    {
        public RoomType Type { get; set; }
        public string DisplayName { get; set; }
        public string Description { get; set; }
        public float MinValue { get; set; }
        public float MaxValue { get; set; }
    }
    public class CardPool
    {
        public Dictionary<string, CardData> CardsById { get; set; } = new Dictionary<string, CardData>();
        public Dictionary<Rarity, (int MinCost, int MaxCost)> CostRangesByRarity { get; set; } = new Dictionary<Rarity, (int, int)>();
        public List<CardData> GetCardsByRarity(Rarity rarity) => new List<CardData>(System.Linq.Enumerable.Where(CardsById.Values, c => c.Rarity == rarity));
        public CardData GetCard(string id) => CardsById.ContainsKey(id) ? CardsById[id] : null;
        public CardData GetRandomCardOfRarity(Rarity rarity, Random rng) { var l = GetCardsByRarity(rarity); return l.Count > 0 ? l[rng.Next(l.Count)] : null; }
        public CardData GetRandomCardUpToRarity(Rarity maxRarity, Random rng)
        {
            var list = new List<Rarity> { Rarity.Common };
            if (maxRarity >= Rarity.Uncommon) list.Add(Rarity.Uncommon);
            if (maxRarity >= Rarity.Rare) list.Add(Rarity.Rare);
            return GetRandomCardOfRarity(list[rng.Next(list.Count)], rng);
        }
    }

    public class RelicPool
    {
        public Dictionary<string, RelicData> RelicsById { get; set; } = new Dictionary<string, RelicData>();
        public Dictionary<Rarity, (int MinCost, int MaxCost)> CostRangesByRarity { get; set; } = new Dictionary<Rarity, (int, int)>();
        public List<RelicData> GetRelicsByRarity(Rarity rarity) => new List<RelicData>(System.Linq.Enumerable.Where(RelicsById.Values, r => r.Rarity == rarity));
        public RelicData GetRelic(string id) => RelicsById.ContainsKey(id) ? RelicsById[id] : null;
        public RelicData GetRandomRelicOfRarity(Rarity rarity, Random rng) { var l = GetRelicsByRarity(rarity); return l.Count > 0 ? l[rng.Next(l.Count)] : null; }
    }

    public class EnemyPool
    {
        public Dictionary<string, EnemyData> EnemiesById { get; set; } = new Dictionary<string, EnemyData>();
        public EnemyData GetEnemy(string id) => EnemiesById.ContainsKey(id) ? EnemiesById[id] : null;
        public List<EnemyData> GetEnemiesInDifficultyRange(float min, float max) => new List<EnemyData>(System.Linq.Enumerable.Where(EnemiesById.Values, e => e.Difficulty >= min && e.Difficulty <= max));
    }

    public class EffectPool { public Dictionary<string, EffectData> EffectsById { get; set; } = new Dictionary<string, EffectData>(); public EffectData GetEffect(string id) => EffectsById.ContainsKey(id) ? EffectsById[id] : null; }
    public class EventPool { public Dictionary<string, EventChoiceSet> EventsById { get; set; } = new Dictionary<string, EventChoiceSet>(); }
}