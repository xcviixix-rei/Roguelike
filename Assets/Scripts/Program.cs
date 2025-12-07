using System;
using System.Collections.Generic;
using System.Linq;
using Roguelike.Data;
using Roguelike.Logic;
using Roguelike.Logic.Handlers;
using RoguelikeMapGen;

namespace Roguelike.ConsoleRunner
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Initializing Roguelike Core...");

            // 1. Initialize empty pools
            var cardPool = new CardPool();
            var relicPool = new RelicPool();
            var enemyPool = new EnemyPool();
            var effectPool = new EffectPool();
            var eventPool = new EventPool();
            var roomConfigs = new Dictionary<RoomType, RoomData>();

            // 2. Populate with Mock Data (The "Content")
            MockDataLoader.LoadAll(cardPool, relicPool, enemyPool, effectPool, eventPool, roomConfigs);

            // 3. Setup Controller
            var controller = new GameController(cardPool, relicPool, enemyPool, effectPool, eventPool, roomConfigs);

            // 4. Create Hero Template
            var heroData = MockDataLoader.CreateHero();

            // 5. Start Run
            controller.StartNewRun(seed: 12345, heroData);
            Console.WriteLine("Run Started! Welcome to the Spire (Console Edition).");

            // 6. Main Game Loop
            while (controller.CurrentRun.CurrentState != GameState.GameOver)
            {
                var run = controller.CurrentRun;
                Console.WriteLine("\n------------------------------------------------");
                Console.WriteLine($"[Floor {run.CurrentFloor + 1}] State: {run.CurrentState}");
                Console.WriteLine($"Hero: {run.TheHero.CurrentHealth}/{run.TheHero.MaxHealth} HP | {run.TheHero.CurrentGold} Gold");

                switch (run.CurrentState)
                {
                    case GameState.OnMap:
                        HandleMap(controller);
                        break;
                    case GameState.InCombat:
                        HandleCombat(controller);
                        break;
                    case GameState.InEvent:
                        HandleEvent(controller);
                        break;
                    case GameState.InShop:
                        HandleShop(controller);
                        break;
                    case GameState.AwaitingReward:
                        HandleRewards(controller);
                        break;
                }
            }

            Console.WriteLine("GAME OVER.");
            Console.ReadLine();
        }

        // --- State Handlers ---

        static void HandleMap(GameController controller)
        {
            var nextRooms = controller.CurrentRun.TheMap.GetPossibleNextNodes();
            Console.WriteLine("Available Rooms:");
            foreach (var room in nextRooms)
            {
                // Show Type and Difficulty info if it's a combat room
                string extra = "";
                if(room.Type == RoomType.Monster || room.Type == RoomType.Elite)
                    extra = $"(Diff: {controller.CurrentRun.RoomConfigs[room.Type].MinValue}-{controller.CurrentRun.RoomConfigs[room.Type].MaxValue})";
                
                Console.WriteLine($" - ID {room.Id}: {room.Type} {extra}");
            }

            Console.Write("> Enter 'move <id>': ");
            var input = Console.ReadLine()?.Split(' ');
            if (input?.Length == 2 && input[0] == "move" && int.TryParse(input[1], out int id))
            {
                if (!controller.ChooseMapNode(id)) Console.WriteLine("Invalid move.");
            }
            else Console.WriteLine("Unknown command.");
        }

        static void HandleCombat(GameController controller)
        {
            var cm = controller.CurrentRun.CurrentCombat;
            
            // 1. Show Enemies
            Console.WriteLine("\nEnemies:");
            for (int i = 0; i < cm.Enemies.Count; i++)
            {
                var e = cm.Enemies[i];
                if (e.CurrentHealth <= 0) continue;

                var intent = cm.CurrentEnemyIntents.ContainsKey(e) ? cm.CurrentEnemyIntents[e] : null;
                string intentStr = intent != null ? $"{intent.Type} {intent.Value}" : "Unknown";
                
                // Show status effects
                string statuses = string.Join(", ", e.ActiveEffects.Select(ef => $"{ef.SourceData.Name}({ef.Stacks})"));

                Console.WriteLine($" [{i}] {e.SourceData.Name}: {e.CurrentHealth}/{e.MaxHealth} HP (Block {e.Block}) | Intent: {intentStr} | {statuses}");
            }

            // 2. Show Hero Details
            var hero = controller.CurrentRun.TheHero;
            string heroStatuses = string.Join(", ", hero.ActiveEffects.Select(ef => $"{ef.SourceData.Name}({ef.Stacks})"));
            Console.WriteLine($"\nHero: Mana {hero.CurrentMana}/{hero.MaxMana} | Block {hero.Block} | {heroStatuses}");

            // 3. Show Hand
            Console.WriteLine("Hand:");
            for (int i = 0; i < hero.Deck.Hand.Count; i++)
            {
                var card = hero.Deck.Hand[i];
                Console.WriteLine($" ({i}) {card.Name} [{card.ManaCost}]: {card.Description}");
            }

            // 4. Input
            Console.Write("> 'play <card_idx> <target_idx>' or 'end': ");
            var input = Console.ReadLine()?.Split(' ');
            if (input == null) return;

            if (input[0] == "end")
            {
                controller.EndTurn();
                Console.WriteLine("--- Enemy Turn ---");
            }
            else if (input[0] == "play" && input.Length >= 2)
            {
                if (int.TryParse(input[1], out int cIdx))
                {
                    int tIdx = 0; // Default target
                    if (input.Length > 2) int.TryParse(input[2], out tIdx);

                    if (!controller.PlayCard(cIdx, tIdx))
                        Console.WriteLine("Invalid play (Check mana? Check target?)");
                    else
                        Console.WriteLine("Card played.");
                }
            }
        }

        static void HandleEvent(GameController controller)
        {
            var evt = controller.CurrentRun.CurrentEvent;
            Console.WriteLine($"\nEVENT: {evt.EventTitle}");
            Console.WriteLine(evt.EventDescription);
            for (int i = 0; i < evt.Choices.Count; i++)
            {
                Console.WriteLine($" {i}: {evt.Choices[i].ChoiceText}");
            }

            Console.Write("> 'choose <index>': ");
            var input = Console.ReadLine()?.Split(' ');
            if (input?.Length == 2 && input[0] == "choose" && int.TryParse(input[1], out int idx))
            {
                controller.ChooseEventOption(idx);
            }
        }

        static void HandleShop(GameController controller)
        {
            var shop = controller.CurrentRun.CurrentShop;
            Console.WriteLine($"\nSHOP (Gold: {controller.CurrentRun.TheHero.CurrentGold})");
            
            Console.WriteLine("Cards:");
            for(int i=0; i<shop.CardsForSale.Count; i++)
            {
                var item = shop.CardsForSale[i];
                string status = item.IsSold ? "[SOLD]" : $"Price {item.Price}";
                Console.WriteLine($" {i}: {item.Item.Name} ({item.Item.Rarity}) - {status}");
            }

            Console.WriteLine("Relics:");
            for (int i = 0; i < shop.RelicsForSale.Count; i++)
            {
                var item = shop.RelicsForSale[i];
                string status = item.IsSold ? "[SOLD]" : $"Price {item.Price}";
                Console.WriteLine($" {i}: {item.Item.Name} ({item.Item.Rarity}) - {status}");
            }

            Console.Write("> 'buycard <idx>', 'buyrelic <idx>', or 'leave': ");
            var input = Console.ReadLine()?.Split(' ');
            if (input == null) return;

            if (input[0] == "leave") controller.LeaveShop();
            else if (input[0] == "buycard" && int.TryParse(input[1], out int cIdx)) controller.BuyShopCard(cIdx);
            else if (input[0] == "buyrelic" && int.TryParse(input[1], out int rIdx)) controller.BuyShopRelic(rIdx);
        }

        static void HandleRewards(GameController controller)
        {
            Console.WriteLine("\nVICTORY! Choose a reward:");
            var rewards = controller.CurrentRun.CardRewardChoices;
            for (int i = 0; i < rewards.Count; i++)
            {
                Console.WriteLine($" {i}: {rewards[i].Name} ({rewards[i].Rarity}) - {rewards[i].Description}");
            }
            if (controller.CurrentRun.RelicRewardChoice != null)
            {
                Console.WriteLine($" [Auto-Obtained Relic]: {controller.CurrentRun.RelicRewardChoice.Name}");
            }

            Console.Write("> 'pick <index>': ");
            var input = Console.ReadLine()?.Split(' ');
            if (input?.Length == 2 && input[0] == "pick" && int.TryParse(input[1], out int idx))
            {
                controller.ConfirmRewards(idx);
            }
        }
    }

    // --- Mock Data Loader ---
    public static class MockDataLoader
    {
        public static void LoadAll(CardPool cards, RelicPool relics, EnemyPool enemies, EffectPool effects, EventPool events, Dictionary<RoomType, RoomData> rooms)
        {
            // 1. Effects
            var vul = new StatusEffectData { Id = "vuln", Name = "Vulnerable", EffectType = StatusEffectType.Vulnerable, Decay = DecayType.AfterXTURNS, Value = 150 };
            var str = new StatusEffectData { Id = "str", Name = "Strength", EffectType = StatusEffectType.Strength, Decay = DecayType.Permanent, Value = 1 };
            effects.EffectsById["vuln"] = vul;
            effects.EffectsById["str"] = str;

            // 2. Cards
            // Strike
            var strike = new CardData { Id = "strike", Name = "Strike", ManaCost = 1, Type = CardType.Attack, Rarity = Rarity.Common, Description = "Deal 6 Dmg" };
            strike.Actions.Add(new CombatActionData(ActionType.DealDamage, 6, TargetType.SingleOpponent));
            cards.CardsById["strike"] = strike;

            // Defend
            var defend = new CardData { Id = "defend", Name = "Defend", ManaCost = 1, Type = CardType.Skill, Rarity = Rarity.Common, Description = "Gain 5 Block" };
            defend.Actions.Add(new CombatActionData(ActionType.GainBlock, 5, TargetType.Self));
            cards.CardsById["defend"] = defend;

            // Bash
            var bash = new CardData { Id = "bash", Name = "Bash", ManaCost = 2, Type = CardType.Attack, Rarity = Rarity.Common, Description = "8 Dmg, 2 Vuln" };
            bash.Actions.Add(new CombatActionData(ActionType.DealDamage, 8, TargetType.SingleOpponent));
            bash.Actions.Add(new CombatActionData(ActionType.ApplyStatusEffect, 2, TargetType.SingleOpponent, "vuln"));
            cards.CardsById["bash"] = bash;

            // Rare Card (Demon Form ish)
            var demon = new CardData { Id = "demon", Name = "Demon Strength", ManaCost = 3, Type = CardType.Power, Rarity = Rarity.Rare, Description = "Gain 3 Str" };
            demon.Actions.Add(new CombatActionData(ActionType.ApplyStatusEffect, 3, TargetType.Self, "str"));
            cards.CardsById["demon"] = demon;

            // Shop Prices
            cards.CostRangesByRarity[Rarity.Common] = (40, 60);
            cards.CostRangesByRarity[Rarity.Uncommon] = (70, 90);
            cards.CostRangesByRarity[Rarity.Rare] = (100, 150);
            cards.CostRangesByRarity[Rarity.Legendary] = (200, 300);

            // 3. Relics
            var relic = new RelicData { Id = "burning_blood", Name = "Burning Blood", Rarity = Rarity.Common, Description = "Starter Relic" };
            relics.RelicsById["burning_blood"] = relic;
            relics.CostRangesByRarity[Rarity.Common] = (150, 200);

            // 4. Enemies
            // Weak Enemy
            var louse = new EnemyData { Id = "louse", Name = "Louse", StartingHealth = 15, Difficulty = 1.0f };
            louse.ActionSet.Add(new WeightedChoice<CombatActionData>(new CombatActionData(ActionType.DealDamage, 5, TargetType.SingleOpponent), 70));
            louse.ActionSet.Add(new WeightedChoice<CombatActionData>(new CombatActionData(ActionType.GainBlock, 5, TargetType.Self), 30));
            enemies.EnemiesById["louse"] = louse;

            // Medium Enemy
            var goblin = new EnemyData { Id = "goblin", Name = "Goblin", StartingHealth = 30, Difficulty = 2.0f };
            goblin.ActionSet.Add(new WeightedChoice<CombatActionData>(new CombatActionData(ActionType.DealDamage, 8, TargetType.SingleOpponent), 60));
            enemies.EnemiesById["goblin"] = goblin;

            // Boss
            var boss = new EnemyData { Id = "slime", Name = "Slime Boss", StartingHealth = 100, Difficulty = 5.0f };
            boss.ActionSet.Add(new WeightedChoice<CombatActionData>(new CombatActionData(ActionType.DealDamage, 15, TargetType.SingleOpponent), 50));
            boss.ActionSet.Add(new WeightedChoice<CombatActionData>(new CombatActionData(ActionType.DealDamage, 30, TargetType.SingleOpponent), 10)); // Big hit
            enemies.EnemiesById["slime"] = boss;

            // 5. Events
            var evt = new EventChoiceSet { Id = "fountain", EventTitle = "Golden Fountain", EventDescription = "Drink? Gain 50g but lose 5 HP." };
            var choice1 = new EventChoice { ChoiceText = "Drink" };
            choice1.Effects.Add(new EventEffect { Type = EventEffectType.GainGold, Value = 50 });
            choice1.Effects.Add(new EventEffect { Type = EventEffectType.LoseHP, Value = 5 });
            var choice2 = new EventChoice { ChoiceText = "Leave" };
            choice2.Effects.Add(new EventEffect { Type = EventEffectType.Quit });
            evt.Choices.Add(choice1);
            evt.Choices.Add(choice2);
            events.EventsById["fountain"] = evt;

            // 6. Room Configs
            // Setup ranges so our enemy difficulties (1, 2, 5) get picked
            rooms[RoomType.Monster] = new RoomData { Type = RoomType.Monster, MinValue = 1.0f, MaxValue = 2.0f };
            rooms[RoomType.Elite] = new RoomData { Type = RoomType.Elite, MinValue = 3.0f, MaxValue = 4.0f };
            rooms[RoomType.Boss] = new RoomData { Type = RoomType.Boss, MinValue = 5.0f, MaxValue = 10.0f };
            rooms[RoomType.Event] = new RoomData { Type = RoomType.Event };
            rooms[RoomType.Shop] = new RoomData { Type = RoomType.Shop };
            rooms[RoomType.Rest] = new RoomData { Type = RoomType.Rest, MinValue=30, MaxValue=30 }; // 30% heal
        }

        public static HeroData CreateHero()
        {
            return new HeroData
            {
                Id = "ironclad",
                Name = "Ironclad",
                StartingHealth = 80,
                StartingGold = 99,
                StartingMana = 3,
                StartingHandSize = 5,
                StartingRelicId = "burning_blood",
                StartingDeckCardIds = new List<string> { "strike", "strike", "strike", "strike", "defend", "defend", "defend", "defend", "bash" }
            };
        }
    }
}