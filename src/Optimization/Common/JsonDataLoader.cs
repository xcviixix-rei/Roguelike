using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Roguelike.Data;
using Roguelike.Core.Map;
using System;
using System.Collections.Generic;
using System.IO;

namespace RoguelikeGASimulator
{
    /// <summary>
    /// Root data structure for the entire game database
    /// </summary>
    public class GameDataRoot
    {
        public List<StatusEffectDataJson> StatusEffects { get; set; }
        public List<DeckEffectDataJson> DeckEffects { get; set; }
        public List<CardDataJson> Cards { get; set; }
        public List<EnemyDataJson> Enemies { get; set; }
        public List<RelicDataJson> Relics { get; set; }
        public HeroDataJson Hero { get; set; }
        public List<EventChoiceSet> Events { get; set; }
        public Dictionary<string, RoomDataJson> RoomConfigs { get; set; }
    }

    public class StatusEffectDataJson
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public int Value { get; set; }
        public string ApplyType { get; set; }
        public string Decay { get; set; }
        public string EffectType { get; set; }
        public string Target { get; set; }
    }

    public class DeckEffectDataJson
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public int Value { get; set; }
        public string ApplyType { get; set; }
        public string Decay { get; set; }
        public string EffectType { get; set; }
        public string Target { get; set; }
    }

    public class CardDataJson
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public int ManaCost { get; set; }
        public int StarRating { get; set; }
        public string Type { get; set; }
        public List<CombatActionDataJson> Actions { get; set; }
    }

    public class CombatActionDataJson
    {
        public string Type { get; set; }
        public int Value { get; set; }
        public string Target { get; set; }
        public string EffectId { get; set; }
    }

    public class EnemyDataJson
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public int StarRating { get; set; }
        public int StartingHealth { get; set; }
        public bool IsBoss { get; set; }
        public List<WeightedActionJson> ActionSet { get; set; }
    }

    public class WeightedActionJson
    {
        public int Weight { get; set; }
        public CombatActionDataJson Action { get; set; }
    }

    public class RelicDataJson
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public int StarRating { get; set; }
        public bool IsBossRelic { get; set; }
        public List<string> EffectIds { get; set; }
    }

    public class HeroDataJson
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public int StartingHealth { get; set; }
        public int StartingMana { get; set; }
        public int StartingGold { get; set; }
        public int StartingHandSize { get; set; }
        public List<string> StartingDeckCardIds { get; set; }
        public string StartingRelicId { get; set; }
    }

    public class RoomDataJson
    {
        public string Type { get; set; }
        public string DisplayName { get; set; }
        public string Description { get; set; }
        public int StarRating { get; set; }
    }

    /// <summary>
    /// Loads game data from JSON files
    /// </summary>
    public static class GameDataLoader
    {
        public static (HeroData, CardPool, RelicPool, EnemyPool, EffectPool, EventPool, Dictionary<RoomType, RoomData>) 
            LoadFromJson(string jsonFilePath)
        {
            var settings = new JsonSerializerSettings
            {
                Converters = new List<JsonConverter> { new StringEnumConverter() }
            };

            string json = File.ReadAllText(jsonFilePath);
            var root = JsonConvert.DeserializeObject<GameDataRoot>(json, settings);

            // Effect Pool
            var effectPool = new EffectPool();
            
            foreach (var seJson in root.StatusEffects)
            {
                var effect = new StatusEffectData
                {
                    Id = seJson.Id,
                    Name = seJson.Name,
                    Description = seJson.Description,
                    Value = seJson.Value,
                    ApplyType = Enum.Parse<ApplyType>(seJson.ApplyType),
                    Decay = Enum.Parse<DecayType>(seJson.Decay),
                    EffectType = Enum.Parse<StatusEffectType>(seJson.EffectType),
                    Target = Enum.Parse<TargetType>(seJson.Target)
                };
                effectPool.EffectsById[effect.Id] = effect;
            }

            foreach (var deJson in root.DeckEffects)
            {
                var effect = new DeckEffectData
                {
                    Id = deJson.Id,
                    Name = deJson.Name,
                    Description = deJson.Description,
                    Value = deJson.Value,
                    ApplyType = Enum.Parse<ApplyType>(deJson.ApplyType),
                    Decay = Enum.Parse<DecayType>(deJson.Decay),
                    EffectType = Enum.Parse<DeckEffectType>(deJson.EffectType),
                    Target = Enum.Parse<TargetType>(deJson.Target)
                };
                effectPool.EffectsById[effect.Id] = effect;
            }

            // Card Pool
            var cardPool = new CardPool();
            var allCards = new List<CardData>();
            
            foreach (var cardJson in root.Cards)
            {
                var card = new CardData
                {
                    Id = cardJson.Id,
                    Name = cardJson.Name,
                    Description = cardJson.Description,
                    ManaCost = cardJson.ManaCost,
                    StarRating = cardJson.StarRating,
                    Type = Enum.Parse<CardType>(cardJson.Type),
                    Actions = new List<CombatActionData>()
                };

                foreach (var actionJson in cardJson.Actions)
                {
                    card.Actions.Add(new CombatActionData(
                        Enum.Parse<ActionType>(actionJson.Type),
                        actionJson.Value,
                        Enum.Parse<TargetType>(actionJson.Target),
                        actionJson.EffectId
                    ));
                }

                allCards.Add(card);
            }
            cardPool.Initialize(allCards);

            // Enemy Pool
            var enemyPool = new EnemyPool();
            var allEnemies = new List<EnemyData>();
            
            foreach (var enemyJson in root.Enemies)
            {
                var enemy = new EnemyData
                {
                    Id = enemyJson.Id,
                    Name = enemyJson.Name,
                    StarRating = enemyJson.StarRating,
                    StartingHealth = enemyJson.StartingHealth,
                    IsBoss = enemyJson.IsBoss,
                    ActionSet = new List<WeightedChoice<CombatActionData>>()
                };

                foreach (var weightedJson in enemyJson.ActionSet)
                {
                    var action = new CombatActionData(
                        Enum.Parse<ActionType>(weightedJson.Action.Type),
                        weightedJson.Action.Value,
                        Enum.Parse<TargetType>(weightedJson.Action.Target),
                        weightedJson.Action.EffectId
                    );

                    enemy.ActionSet.Add(new WeightedChoice<CombatActionData>(action, weightedJson.Weight));
                }

                allEnemies.Add(enemy);
            }
            enemyPool.Initialize(allEnemies);

            // Relic Pool
            var relicPool = new RelicPool();
            var allRelics = new List<RelicData>();
            
            foreach (var relicJson in root.Relics)
            {
                var relic = new RelicData
                {
                    Id = relicJson.Id,
                    Name = relicJson.Name,
                    Description = relicJson.Description,
                    StarRating = relicJson.StarRating,
                    IsBossRelic = relicJson.IsBossRelic,
                    Effects = new List<EffectData>()
                };

                foreach (var effectId in relicJson.EffectIds)
                {
                    if (effectPool.EffectsById.TryGetValue(effectId, out var effect))
                    {
                        relic.Effects.Add(effect);
                    }
                }

                allRelics.Add(relic);
            }
            relicPool.Initialize(allRelics);

            // Hero
            var heroData = new HeroData
            {
                Id = root.Hero.Id,
                Name = root.Hero.Name,
                StartingHealth = root.Hero.StartingHealth,
                StartingMana = root.Hero.StartingMana,
                StartingGold = root.Hero.StartingGold,
                StartingHandSize = root.Hero.StartingHandSize,
                StartingDeckCardIds = root.Hero.StartingDeckCardIds,
                StartingRelicId = root.Hero.StartingRelicId
            };

            // Event Pool
            var eventPool = new EventPool();
            foreach (var evt in root.Events)
            {
                eventPool.EventsById[evt.Id] = evt;
            }

            // Room Configs
            var roomConfigs = new Dictionary<RoomType, RoomData>();
            foreach (var kvp in root.RoomConfigs)
            {
                var roomType = Enum.Parse<RoomType>(kvp.Key);
                var roomJson = kvp.Value;
                
                roomConfigs[roomType] = new RoomData
                {
                    Type = roomType,
                    DisplayName = roomJson.DisplayName,
                    Description = roomJson.Description,
                    StarRating = roomJson.StarRating
                };
            }

            return (heroData, cardPool, relicPool, enemyPool, effectPool, eventPool, roomConfigs);
        }
    }
}
