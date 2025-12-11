// RoguelikeGASimulator/Program.cs

using Roguelike.Agents;
using Roguelike.Data;
using Roguelike.GA;
using RoguelikeMapGen;
using System;
using System.Collections.Generic;

namespace RoguelikeGASimulator
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Setting up GA Simulation Environment...");

            var (hero, cards, relics, enemies, effects, events, rooms) = LoadBaseGameData();

            var agent = new HeuristicPlayerAI();
            var runner = new SimulationRunner(agent, hero, cards, relics, enemies, effects, events, rooms);
            var evaluator = new FitnessEvaluator();
            var ga = new GeneticAlgorithm(runner, evaluator, cards, enemies);

            ga.PopulationSize = 100;
            ga.Generations = 40;
            ga.RunsPerGenome = 400;

            Console.WriteLine("Setup complete. Starting Genetic Algorithm...");
            Console.WriteLine("=============================================");

            ga.Run();

            Console.WriteLine("=============================================");
            Console.WriteLine("Simulation finished. Press any key to exit.");
            Console.ReadKey();
        }

        /// <summary>
        /// Hard-coded database
        /// TODO: Create data in json and load it here
        /// </summary>
        private static (HeroData, CardPool, RelicPool, EnemyPool, EffectPool, EventPool, Dictionary<RoomType, RoomData>) LoadBaseGameData()
        {
            // 1. Effects must be defined first, as cards and enemies will reference them by ID.
            var allEffects = new List<EffectData>
            {
                // Status Effects
                new StatusEffectData { Id = "vulnerable_low", Name = "Vulnerable", Description = "Takes 25% more damage from attacks.", Decay = DecayType.AfterXTURNS, Value = 125, EffectType = StatusEffectType.Vulnerable },
                new StatusEffectData { Id = "vulnerable_moderate", Name = "Vulnerable", Description = "Takes 50% more damage from attacks.", Decay = DecayType.AfterXTURNS, Value = 150, EffectType = StatusEffectType.Vulnerable },
                new StatusEffectData { Id = "weakened_low", Name = "Weak", Description = "Deals 25% less damage.", Decay = DecayType.AfterXTURNS, Value = 75, EffectType = StatusEffectType.Weakened },
                new StatusEffectData { Id = "weakened_moderate", Name = "Weak", Description = "Deals 50% less damage.", Decay = DecayType.AfterXTURNS, Value = 50, EffectType = StatusEffectType.Weakened },
                new StatusEffectData { Id = "strength_low", Name = "Strength", Description = "Deals +1 additional damage per stack.", Decay = DecayType.Permanent, Value = 1, EffectType = StatusEffectType.Strength },
                new StatusEffectData { Id = "strength_moderate", Name = "Strength", Description = "Deals +3 additional damage per stack.", Decay = DecayType.Permanent, Value = 3, EffectType = StatusEffectType.Strength },
                new StatusEffectData { Id = "frail_low", Name = "Frail", Description = "Gains 25% less Block.", Decay = DecayType.AfterXTURNS, Value = 75, EffectType = StatusEffectType.Frail },
                new StatusEffectData { Id = "frail_moderate", Name = "Frail", Description = "Gains 50% less Block.", Decay = DecayType.AfterXTURNS, Value = 50, EffectType = StatusEffectType.Frail },
                new StatusEffectData { Id = "pierced_low", Name = "Pierced", Description = "Damage bypasses Block.", Decay = DecayType.EndOfTurn, Value = 1, EffectType = StatusEffectType.Pierced }, // Value is a flag here
                new StatusEffectData { Id = "pierced_moderate", Name = "Pierced", Description = "Damage bypasses Block.", Decay = DecayType.AfterXTURNS, Value = 1, EffectType = StatusEffectType.Pierced },
                new StatusEffectData { Id = "philosophical_low", Name = "Clarity", Description = "Gain 1 Mana at the start of your turn.", Decay = DecayType.Permanent, Value = 1, EffectType = StatusEffectType.Philosophical },
                new StatusEffectData { Id = "philosophical_moderate", Name = "Enlightened", Description = "Gain 2 Mana at the start of your turn.", Decay = DecayType.Permanent, Value = 2, EffectType = StatusEffectType.Philosophical },

                // Deck Effects
                new DeckEffectData { Id = "draw_low", Name = "Draw", Decay = DecayType.EndOfTurn, Value = 1, EffectType = DeckEffectType.DrawCard },
                new DeckEffectData { Id = "draw_moderate", Name = "Draw", Decay = DecayType.EndOfTurn, Value = 2, EffectType = DeckEffectType.DrawCard },
                new DeckEffectData { Id = "discard_low", Name = "Discard", Decay = DecayType.EndOfTurn, Value = 1, EffectType = DeckEffectType.DiscardCard },
                new DeckEffectData { Id = "discard_moderate", Name = "Discard", Decay = DecayType.EndOfTurn, Value = 2, EffectType = DeckEffectType.DiscardCard },
                new DeckEffectData { Id = "freeze_low", Name = "Freeze", Decay = DecayType.EndOfTurn, Value = 1, EffectType = DeckEffectType.FreezeCard },
                new DeckEffectData { Id = "freeze_moderate", Name = "Freeze", Decay = DecayType.EndOfTurn, Value = 2, EffectType = DeckEffectType.FreezeCard },
                new DeckEffectData { Id = "duplicate_low", Name = "Duplicate", Decay = DecayType.EndOfTurn, Value = 1, EffectType = DeckEffectType.DuplicateCard },
                new DeckEffectData { Id = "duplicate_moderate", Name = "Duplicate", Decay = DecayType.EndOfTurn, Value = 2, EffectType = DeckEffectType.DuplicateCard }
            };
            var effectPool = new EffectPool();
            foreach (var effect in allEffects) effectPool.EffectsById[effect.Id] = effect;


            // 2. CARDS
            var allCards = new List<CardData>
            {
                // 1 Star
                new CardData { Id = "strike", Name = "Strike", ManaCost = 1, StarRating = 1, Type = CardType.Attack, Actions = new List<CombatActionData> { new CombatActionData(ActionType.DealDamage, 6, TargetType.SingleOpponent) }},
                new CardData { Id = "defend", Name = "Defend", ManaCost = 1, StarRating = 1, Type = CardType.Skill, Actions = new List<CombatActionData> { new CombatActionData(ActionType.GainBlock, 5, TargetType.Self) }},
                new CardData { Id = "quick_jab", Name = "Quick Jab", ManaCost = 0, StarRating = 1, Type = CardType.Attack, Actions = new List<CombatActionData> { new CombatActionData(ActionType.DealDamage, 3, TargetType.SingleOpponent) }},
                new CardData { Id = "cycle", Name = "Cycle", ManaCost = 0, StarRating = 1, Type = CardType.Skill, Actions = new List<CombatActionData> { new CombatActionData(ActionType.ApplyDeckEffect, 1, TargetType.Self, "draw_low") }},
                // 2 Star
                new CardData { Id = "heavy_blow", Name = "Heavy Blow", ManaCost = 2, StarRating = 2, Type = CardType.Attack, Actions = new List<CombatActionData> { new CombatActionData(ActionType.DealDamage, 14, TargetType.SingleOpponent) }},
                new CardData { Id = "double_tap", Name = "Double Tap", ManaCost = 1, StarRating = 2, Type = CardType.Attack, Actions = new List<CombatActionData> { new CombatActionData(ActionType.DealDamage, 5, TargetType.SingleOpponent), new CombatActionData(ActionType.DealDamage, 5, TargetType.SingleOpponent) }},
                new CardData { Id = "shield_bash", Name = "Shield Bash", ManaCost = 1, StarRating = 2, Type = CardType.Skill, Actions = new List<CombatActionData> { new CombatActionData(ActionType.GainBlock, 6, TargetType.Self), new CombatActionData(ActionType.ApplyStatusEffect, 1, TargetType.SingleOpponent, "weakened_low") }},
                new CardData { Id = "scrounge", Name = "Scrounge", ManaCost = 1, StarRating = 2, Type = CardType.Skill, Actions = new List<CombatActionData> { new CombatActionData(ActionType.ApplyDeckEffect, 2, TargetType.Self, "draw_moderate") }},
                // 3 Star
                new CardData { Id = "power_up", Name = "Power Up", ManaCost = 1, StarRating = 3, Type = CardType.Power, Actions = new List<CombatActionData> { new CombatActionData(ActionType.ApplyStatusEffect, 2, TargetType.Self, "strength_low") }},
                new CardData { Id = "cleave", Name = "Cleave", ManaCost = 2, StarRating = 3, Type = CardType.Attack, Actions = new List<CombatActionData> { new CombatActionData(ActionType.DealDamage, 10, TargetType.AllOpponents) }},
                new CardData { Id = "bunker_down", Name = "Bunker Down", ManaCost = 2, StarRating = 3, Type = CardType.Skill, Actions = new List<CombatActionData> { new CombatActionData(ActionType.GainBlock, 20, TargetType.Self) }},
                new CardData { Id = "expose_weakness", Name = "Expose Weakness", ManaCost = 1, StarRating = 3, Type = CardType.Skill, Actions = new List<CombatActionData> { new CombatActionData(ActionType.ApplyStatusEffect, 2, TargetType.SingleOpponent, "vulnerable_moderate") }},
                // 4 Star
                new CardData { Id = "piercing_shot", Name = "Piercing Shot", ManaCost = 2, StarRating = 4, Type = CardType.Attack, Actions = new List<CombatActionData> { new CombatActionData(ActionType.ApplyStatusEffect, 1, TargetType.SingleOpponent, "pierced_low"), new CombatActionData(ActionType.DealDamage, 12, TargetType.SingleOpponent) }},
                new CardData { Id = "catalyst", Name = "Catalyst", ManaCost = 0, StarRating = 4, Type = CardType.Power, Actions = new List<CombatActionData> { new CombatActionData(ActionType.ApplyStatusEffect, 1, TargetType.Self, "philosophical_low") }},
                new CardData { Id = "barricade", Name = "Barricade", ManaCost = 3, StarRating = 4, Type = CardType.Power, Actions = new List<CombatActionData> { new CombatActionData(ActionType.GainBlock, 30, TargetType.Self) }},
                new CardData { Id = "battle_trance", Name = "Battle Trance", ManaCost = 0, StarRating = 4, Type = CardType.Skill, Actions = new List<CombatActionData> { new CombatActionData(ActionType.ApplyDeckEffect, 3, TargetType.Self, "draw_low") }},
                // 5 Star
                new CardData { Id = "demon_form", Name = "Demon Form", ManaCost = 3, StarRating = 5, Type = CardType.Power, Actions = new List<CombatActionData> { new CombatActionData(ActionType.ApplyStatusEffect, 3, TargetType.Self, "strength_moderate") }},
                new CardData { Id = "annihilate", Name = "Annihilate", ManaCost = 4, StarRating = 5, Type = CardType.Attack, Actions = new List<CombatActionData> { new CombatActionData(ActionType.DealDamage, 50, TargetType.SingleOpponent) }},
                new CardData { Id = "adrenaline", Name = "Adrenaline", ManaCost = 0, StarRating = 5, Type = CardType.Skill, Actions = new List<CombatActionData> { new CombatActionData(ActionType.ApplyStatusEffect, 1, TargetType.Self, "philosophical_low"), new CombatActionData(ActionType.ApplyDeckEffect, 2, TargetType.Self, "draw_moderate") }},
                new CardData { Id = "ethereal_form", Name = "Ethereal Form", ManaCost = 3, StarRating = 5, Type = CardType.Skill, Actions = new List<CombatActionData> { new CombatActionData(ActionType.GainBlock, 99, TargetType.Self) }},
            };
            var cardPool = new CardPool();
            cardPool.Initialize(allCards);

            // 3. ENEMIES
            var allEnemies = new List<EnemyData>
            {
                // 1 Star
                new EnemyData 
                { 
                    Id = "goblin", 
                    Name = "Goblin", 
                    StarRating = 1, 
                    StartingHealth = 15, 
                    ActionSet = new List<WeightedChoice<CombatActionData>> 
                    { 
                        new WeightedChoice<CombatActionData>(new CombatActionData(ActionType.DealDamage, 3, TargetType.SingleOpponent), 1), 
                        new WeightedChoice<CombatActionData>(new CombatActionData(ActionType.GainBlock, 4, TargetType.Self), 1) 
                    }
                },
                new EnemyData 
                { 
                    Id = "slime", 
                    Name = "Slime", 
                    StarRating = 1, 
                    StartingHealth = 10, 
                    ActionSet = new List<WeightedChoice<CombatActionData>> 
                    { 
                        new WeightedChoice<CombatActionData>(new CombatActionData(ActionType.DealDamage, 4, TargetType.SingleOpponent), 2), 
                        new WeightedChoice<CombatActionData>(new CombatActionData(ActionType.GainBlock, 3, TargetType.Self), 1) 
                    }
                },
                new EnemyData 
                { 
                    Id = "kobold", 
                    Name = "Kobold", 
                    StarRating = 1, 
                    StartingHealth = 12, 
                    ActionSet = new List<WeightedChoice<CombatActionData>> 
                    { 
                        new WeightedChoice<CombatActionData>(new CombatActionData(ActionType.DealDamage, 6, TargetType.SingleOpponent), 1), 
                        new WeightedChoice<CombatActionData>(new CombatActionData(ActionType.ApplyStatusEffect, 1, TargetType.SingleOpponent, "vulnerable_low"), 1) 
                    }
                },
                new EnemyData 
                { 
                    Id = "gremlin", 
                    Name = "Gremlin", 
                    StarRating = 1, 
                    StartingHealth = 8, 
                    ActionSet = new List<WeightedChoice<CombatActionData>> 
                    { 
                        new WeightedChoice<CombatActionData>(new CombatActionData(ActionType.DealDamage, 4, TargetType.SingleOpponent), 2), 
                        new WeightedChoice<CombatActionData>(new CombatActionData(ActionType.ApplyStatusEffect, 1, TargetType.SingleOpponent, "weakened_low"), 1) 
                    }
                },
                // 2 Star
                new EnemyData 
                { 
                    Id = "brute", 
                    Name = "Brute", 
                    StarRating = 2, 
                    StartingHealth = 40, 
                    ActionSet = new List<WeightedChoice<CombatActionData>> 
                    { 
                        new WeightedChoice<CombatActionData>(new CombatActionData(ActionType.DealDamage, 10, TargetType.SingleOpponent), 1), 
                        new WeightedChoice<CombatActionData>(new CombatActionData(ActionType.GainBlock, 5, TargetType.Self), 1) 
                    }
                },
                new EnemyData 
                { 
                    Id = "cultist", 
                    Name = "Cultist", 
                    StarRating = 2, 
                    StartingHealth = 25, 
                    ActionSet = new List<WeightedChoice<CombatActionData>> 
                    { 
                        new WeightedChoice<CombatActionData>(new CombatActionData(ActionType.ApplyStatusEffect, 1, TargetType.Self, "strength_low"), 1), 
                        new WeightedChoice<CombatActionData>(new CombatActionData(ActionType.DealDamage, 7, TargetType.SingleOpponent), 2) 
                    }
                },
                new EnemyData 
                { 
                    Id = "assassin", 
                    Name = "Assassin", 
                    StarRating = 2, 
                    StartingHealth = 20, 
                    ActionSet = new List<WeightedChoice<CombatActionData>> 
                    { 
                        new WeightedChoice<CombatActionData>(new CombatActionData(ActionType.DealDamage, 6, TargetType.SingleOpponent), 2), 
                        new WeightedChoice<CombatActionData>(new CombatActionData(ActionType.ApplyStatusEffect, 1, TargetType.SingleOpponent, "vulnerable_low"), 1) 
                    }
                },
                new EnemyData 
                { 
                    Id = "guardian_orb", 
                    Name = "Guardian Orb", 
                    StarRating = 2, 
                    StartingHealth = 30, 
                    ActionSet = new List<WeightedChoice<CombatActionData>> 
                    { 
                        new WeightedChoice<CombatActionData>(new CombatActionData(ActionType.GainBlock, 10, TargetType.Self), 1), 
                        new WeightedChoice<CombatActionData>(new CombatActionData(ActionType.DealDamage, 8, TargetType.SingleOpponent), 1) 
                    }
                },
                // 3 Star (Elites)
                new EnemyData 
                { 
                    Id = "slaver", 
                    Name = "Slaver", 
                    StarRating = 3, 
                    StartingHealth = 50, 
                    ActionSet = new List<WeightedChoice<CombatActionData>> 
                    { 
                        new WeightedChoice<CombatActionData>(new CombatActionData(ActionType.DealDamage, 15, TargetType.SingleOpponent), 2), 
                        new WeightedChoice<CombatActionData>(new CombatActionData(ActionType.GainBlock, 10, TargetType.Self), 1), 
                        new WeightedChoice<CombatActionData>(new CombatActionData(ActionType.ApplyStatusEffect, 2, TargetType.SingleOpponent, "weakened_low"), 1) 
                    }
                },
                new EnemyData 
                { 
                    Id = "ogre", 
                    Name = "Ogre", 
                    StarRating = 3, 
                    StartingHealth = 70, 
                    ActionSet = new List<WeightedChoice<CombatActionData>> 
                    { 
                        new WeightedChoice<CombatActionData>(new CombatActionData(ActionType.DealDamage, 18, TargetType.SingleOpponent), 2), 
                        new WeightedChoice<CombatActionData>(new CombatActionData(ActionType.GainBlock, 10, TargetType.Self), 1), 
                        new WeightedChoice<CombatActionData>(new CombatActionData(ActionType.ApplyStatusEffect, 1, TargetType.SingleOpponent, "frail_low"), 1) 
                    }
                },
                new EnemyData 
                { 
                    Id = "sentinel", 
                    Name = "Sentinel", 
                    StarRating = 3, 
                    StartingHealth = 45, 
                    ActionSet = new List<WeightedChoice<CombatActionData>> 
                    { 
                        new WeightedChoice<CombatActionData>(new CombatActionData(ActionType.DealDamage, 10, TargetType.SingleOpponent), 2), 
                        new WeightedChoice<CombatActionData>(new CombatActionData(ActionType.DealDamage, 20, TargetType.SingleOpponent), 1), 
                        new WeightedChoice<CombatActionData>(new CombatActionData(ActionType.GainBlock, 8, TargetType.Self), 1) 
                    }
                },
                new EnemyData 
                { 
                    Id = "gremlin_leader", 
                    Name = "Gremlin Leader", 
                    StarRating = 3, 
                    StartingHealth = 60, 
                    ActionSet = new List<WeightedChoice<CombatActionData>> 
                    { 
                        new WeightedChoice<CombatActionData>(new CombatActionData(ActionType.DealDamage, 6, TargetType.SingleOpponent), 2), 
                        new WeightedChoice<CombatActionData>(new CombatActionData(ActionType.GainBlock, 8, TargetType.Self), 1), 
                        new WeightedChoice<CombatActionData>(new CombatActionData(ActionType.ApplyStatusEffect, 1, TargetType.Self, "strength_moderate"), 1) 
                    }
                },
                // 4 Star (Harder Elites)
                new EnemyData 
                { 
                    Id = "book_of_stabbing", 
                    Name = "Book of Stabbing", 
                    StarRating = 4, 
                    StartingHealth = 80, 
                    ActionSet = new List<WeightedChoice<CombatActionData>> 
                    { 
                        new WeightedChoice<CombatActionData>(new CombatActionData(ActionType.DealDamage, 5, TargetType.SingleOpponent), 3), 
                        new WeightedChoice<CombatActionData>(new CombatActionData(ActionType.GainBlock, 10, TargetType.Self), 1), 
                        new WeightedChoice<CombatActionData>(new CombatActionData(ActionType.ApplyStatusEffect, 1, TargetType.SingleOpponent, "pierced_low"), 1) 
                    }
                },
                new EnemyData 
                { 
                    Id = "giant_head", 
                    Name = "Giant Head", 
                    StarRating = 4, 
                    StartingHealth = 100, 
                    ActionSet = new List<WeightedChoice<CombatActionData>> 
                    { 
                        new WeightedChoice<CombatActionData>(new CombatActionData(ActionType.ApplyStatusEffect, 2, TargetType.SingleOpponent, "weakened_moderate"), 1), 
                        new WeightedChoice<CombatActionData>(new CombatActionData(ActionType.GainBlock, 10, TargetType.Self), 1), 
                        new WeightedChoice<CombatActionData>(new CombatActionData(ActionType.DealDamage, 25, TargetType.SingleOpponent), 1) 
                    }
                },
                new EnemyData 
                { 
                    Id = "nemesis", 
                    Name = "Nemesis", 
                    StarRating = 4, 
                    StartingHealth = 90, 
                    ActionSet = new List<WeightedChoice<CombatActionData>> 
                    { 
                        new WeightedChoice<CombatActionData>(new CombatActionData(ActionType.DealDamage, 20, TargetType.SingleOpponent), 1), 
                        new WeightedChoice<CombatActionData>(new CombatActionData(ActionType.DealDamage, 10, TargetType.SingleOpponent), 1), 
                        new WeightedChoice<CombatActionData>(new CombatActionData(ActionType.GainBlock, 15, TargetType.Self), 1) 
                    }
                },
                new EnemyData 
                { 
                    Id = "reptomancer", 
                    Name = "Reptomancer", 
                    StarRating = 4, 
                    StartingHealth = 75, 
                    ActionSet = new List<WeightedChoice<CombatActionData>> 
                    { 
                        new WeightedChoice<CombatActionData>(new CombatActionData(ActionType.ApplyStatusEffect, 1, TargetType.AllOpponents, "vulnerable_low"), 1), 
                        new WeightedChoice<CombatActionData>(new CombatActionData(ActionType.GainBlock, 10, TargetType.Self), 1), 
                        new WeightedChoice<CombatActionData>(new CombatActionData(ActionType.DealDamage, 10, TargetType.SingleOpponent), 2) 
                    }
                },
                // 5 Star (Bosses)
                new EnemyData 
                { 
                    Id = "dragon", 
                    Name = "Dragon", 
                    StarRating = 5, 
                    IsBoss = true, 
                    StartingHealth = 250, 
                    ActionSet = new List<WeightedChoice<CombatActionData>> 
                    { 
                        new WeightedChoice<CombatActionData>(new CombatActionData(ActionType.DealDamage, 20, TargetType.SingleOpponent), 2), 
                        new WeightedChoice<CombatActionData>(new CombatActionData(ActionType.DealDamage, 8, TargetType.SingleOpponent), 1),
                        new WeightedChoice<CombatActionData>(new CombatActionData(ActionType.GainBlock, 10, TargetType.Self), 1),
                        new WeightedChoice<CombatActionData>(new CombatActionData(ActionType.ApplyStatusEffect, 2, TargetType.SingleOpponent, "frail_moderate"), 1) 
                    }
                },
                new EnemyData 
                { 
                    Id = "time_eater", 
                    Name = "Time Eater", 
                    StarRating = 5, 
                    IsBoss = true, 
                    StartingHealth = 300, 
                    ActionSet = new List<WeightedChoice<CombatActionData>> 
                    { 
                        new WeightedChoice<CombatActionData>(new CombatActionData(ActionType.ApplyStatusEffect, 2, TargetType.SingleOpponent, "frail_moderate"), 2),
                        new WeightedChoice<CombatActionData>(new CombatActionData(ActionType.DealDamage, 15, TargetType.SingleOpponent), 2), 
                        new WeightedChoice<CombatActionData>(new CombatActionData(ActionType.GainBlock, 20, TargetType.Self), 1) 
                    }
                },
                new EnemyData 
                { 
                    Id = "bronze_automaton", 
                    Name = "Bronze Automaton", 
                    StarRating = 5, 
                    IsBoss = true, 
                    StartingHealth = 350, 
                    ActionSet = new List<WeightedChoice<CombatActionData>> 
                    { 
                        new WeightedChoice<CombatActionData>(new CombatActionData(ActionType.GainBlock, 30, TargetType.Self), 1), 
                        new WeightedChoice<CombatActionData>(new CombatActionData(ActionType.DealDamage, 10, TargetType.AllOpponents), 1), 
                        new WeightedChoice<CombatActionData>(new CombatActionData(ActionType.ApplyStatusEffect, 1, TargetType.Self, "strength_moderate"), 1) 
                    }
                },
                new EnemyData 
                { 
                    Id = "lich_king", 
                    Name = "Lich King", 
                    StarRating = 5, 
                    IsBoss = true, 
                    StartingHealth = 220, 
                    ActionSet = new List<WeightedChoice<CombatActionData>> 
                    { 
                        new WeightedChoice<CombatActionData>(new CombatActionData(ActionType.DealDamage, 22, TargetType.SingleOpponent), 2), 
                        new WeightedChoice<CombatActionData>(new CombatActionData(ActionType.DealDamage, 10, TargetType.SingleOpponent), 1),
                        new WeightedChoice<CombatActionData>(new CombatActionData(ActionType.ApplyStatusEffect, 2, TargetType.SingleOpponent, "vulnerable_moderate"), 1), 
                        new WeightedChoice<CombatActionData>(new CombatActionData(ActionType.ApplyStatusEffect, 2, TargetType.AllOpponents, "weakened_low"), 1) 
                    }
                },
            };
            var enemyPool = new EnemyPool();
            enemyPool.Initialize(allEnemies);

            // 4. RELICS
            var allRelics = new List<RelicData>
            {
                // 1 Star
                new RelicData { Id = "base_relic", Name = "Base Relic", StarRating = 1, Description = "A simple starting relic." },
                new RelicData { Id = "iron_plate", Name = "Iron Plate", StarRating = 1, Description = "Gain 5 Block at the start of each combat." },
                new RelicData { Id = "old_coin", Name = "Old Coin", StarRating = 1, Description = "Gain 50 additional Gold at the start of a run." },
                new RelicData { Id = "tough_bandages", Name = "Tough Bandages", StarRating = 1, Description = "Increase Max HP by 7." },
                // 2 Star
                new RelicData { Id = "whetstone", Name = "Whetstone", StarRating = 2, Description = "Gain 1 Strength at the start of each combat." },
                new RelicData { Id = "blood_vial", Name = "Blood Vial", StarRating = 2, Description = "Heal 2 HP at the end of each combat." },
                new RelicData { Id = "lantern", Name = "Lantern", StarRating = 2, Description = "Gain 1 Mana on the first turn of each combat." },
                new RelicData { Id = "bag_of_marbles", Name = "Bag of Marbles", StarRating = 2, Description = "Apply 1 Vulnerable to all enemies at the start of combat." },
                // 3 Star
                new RelicData { Id = "prayer_wheel", Name = "Prayer Wheel", StarRating = 3, Description = "Card rewards contain an additional card choice." },
                new RelicData { Id = "the_boot", Name = "The Boot", StarRating = 3, Description = "Whenever you would deal 4 or less unblocked attack damage, deal 5 damage instead." },
                new RelicData { Id = "hand_drill", Name = "Hand Drill", StarRating = 3, Description = "The first attack you play each turn is free." },
                new RelicData { Id = "ink_bottle", Name = "Ink Bottle", StarRating = 3, Description = "Whenever you play 5 cards, draw 1 card." },
                // 4 Star
                new RelicData { Id = "cursed_key", Name = "Cursed Key", StarRating = 4, Description = "Gain 1 Mana at the start of each turn. Chests are cursed." },
                new RelicData { Id = "shuriken", Name = "Shuriken", StarRating = 4, Description = "Every time you play 3 Attacks in a single turn, gain 1 Strength." },
                new RelicData { Id = "mummified_hand", Name = "Mummified Hand", StarRating = 4, Description = "Whenever you play a Power card, a random card in your hand costs 0 this turn." },
                new RelicData { Id = "dead_branch", Name = "Dead Branch", StarRating = 4, Description = "Whenever you Exhaust a card, add a random card to your hand." },
                // 5 Star (Boss Relics)
                new RelicData { Id = "sozu", Name = "Sozu", StarRating = 5, IsBossRelic = true, Description = "Gain 1 Mana at the start of each turn. You can no longer obtain potions." },
                new RelicData { Id = "fusion_hammer", Name = "Fusion Hammer", StarRating = 5, IsBossRelic = true, Description = "Gain 1 Mana at the start of each turn. You can no longer rest at campfires." },
                new RelicData { Id = "busted_crown", Name = "Busted Crown", StarRating = 5, IsBossRelic = true, Description = "Gain 1 Mana at the start of each turn. Card rewards have only 1 card choice." },
                new RelicData { Id = "black_star", Name = "Black Star", StarRating = 5, IsBossRelic = true, Description = "Elites drop an additional Relic." },
            };
            var relicPool = new RelicPool();
            relicPool.Initialize(allRelics);

            // 5. HERO
            var heroData = new HeroData
            {
                Id = "player",
                Name = "The Player",
                StartingHealth = 80,
                StartingMana = 3,
                StartingGold = 100,
                StartingHandSize = 5,
                StartingDeckCardIds = new List<string> { "strike", "strike", "strike", "strike", "defend", "defend", "defend", "defend", "quick_jab", "cycle" },
                StartingRelicId = "base_relic"
            };

            // 6. EVENTS
            var allEvents = new List<EventChoiceSet>
            {
                new EventChoiceSet
                {
                    Id = "golden_idol", EventTitle = "Golden Idol", EventDescription = "You find a golden idol on a pedestal.",
                    Choices = new List<EventChoice>
                    {
                        new EventChoice { ChoiceText = "Take it (Lose 10 HP)", Effects = new List<EventEffect> { new EventEffect { Type = EventEffectType.LoseHP, Value = 10 }, new EventEffect { Type = EventEffectType.GainGold, Value = 70 } } },
                        new EventChoice { ChoiceText = "Pry it loose (Lose 20 HP)", Effects = new List<EventEffect> { new EventEffect { Type = EventEffectType.LoseHP, Value = 20 }, new EventEffect { Type = EventEffectType.GainGold, Value = 150 } } },
                        new EventChoice { ChoiceText = "Leave", Effects = new List<EventEffect> { new EventEffect { Type = EventEffectType.Quit } } }
                    }
                },
                new EventChoiceSet
                {
                    Id = "shady_deal", EventTitle = "Shady Deal", EventDescription = "A mysterious figure offers you a deal.",
                    Choices = new List<EventChoice>
                    {
                        new EventChoice { ChoiceText = "Pay with blood (Lose 15 HP)", Effects = new List<EventEffect> { new EventEffect { Type = EventEffectType.LoseHP, Value = 15 } } },
                        new EventChoice { ChoiceText = "Pay with coin (Lose 100 Gold)", Effects = new List<EventEffect> { new EventEffect { Type = EventEffectType.LoseGold, Value = 100 } } }
                    }
                },
                new EventChoiceSet
                {
                    Id = "bonfire_spirits", EventTitle = "Bonfire Spirits", EventDescription = "Spirits dance around a bonfire, beckoning you to give something up.",
                    Choices = new List<EventChoice>
                    {
                        new EventChoice { ChoiceText = "Offer a memory (Remove a card)", Effects = new List<EventEffect> { new EventEffect { Type = EventEffectType.RemoveCard } } }
                    }
                }
            };
            var eventPool = new EventPool();
            foreach (var evt in allEvents) eventPool.EventsById[evt.Id] = evt;

            // 7. ROOM CONFIGS
            var roomConfigs = new Dictionary<RoomType, RoomData>
            {
                [RoomType.Monster] = new RoomData { Type = RoomType.Monster, DisplayName = "Monster", Description = "A standard combat encounter.", StarRating = 1 },
                [RoomType.Elite] = new RoomData { Type = RoomType.Elite, DisplayName = "Elite", Description = "A difficult combat with a better reward.", StarRating = 3 },
                [RoomType.Boss] = new RoomData { Type = RoomType.Boss, DisplayName = "Boss", Description = "The ultimate challenge of the floor.", StarRating = 5 },
                [RoomType.Event] = new RoomData { Type = RoomType.Event, DisplayName = "Event", Description = "A strange occurrence. A choice awaits.", StarRating = 0 },
                [RoomType.Shop] = new RoomData { Type = RoomType.Shop, DisplayName = "Shop", Description = "Purchase new cards and relics.", StarRating = 0 },
                [RoomType.Rest] = new RoomData { Type = RoomType.Rest, DisplayName = "Rest Site", Description = "Rest to recover health.", StarRating = 0 }
            };

            return (heroData, cardPool, relicPool, enemyPool, effectPool, eventPool, roomConfigs);
        }
    }
}