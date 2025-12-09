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
            var cardPool = new CardPool();
            var relicPool = new RelicPool();
            var enemyPool = new EnemyPool();
            var effectPool = new EffectPool();
            var eventPool = new EventPool();
            var roomConfigs = new Dictionary<RoomType, RoomData>();
            MockDataLoader.LoadAll(cardPool, relicPool, enemyPool, effectPool, eventPool, roomConfigs);
            var controller = new GameController(cardPool, relicPool, enemyPool, effectPool, eventPool, roomConfigs);
            var heroData = MockDataLoader.CreateHero();
            controller.StartNewRun(seed: 12345, heroData);
            Console.WriteLine("Run Started! Welcome to the Spire (Console Edition).");
            PrintMapWithConnections(controller.CurrentRun);
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

        static void PrintMapWithConnections(GameRun run)
        {
            var map = run.TheMap.CurrentMap;
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("\n=== MAP STRUCTURE ===");
            var boss = map.Rooms.Values.FirstOrDefault(r => r.Type == RoomType.Boss);
            if(boss != null)
            {
                Console.WriteLine($"[FLOOR 15] BOSS (ID: {boss.Id})");
                Console.WriteLine("    ^");
                Console.WriteLine("    | (All Floor 14 rooms connect here)");
                Console.WriteLine("    |");
            }
            for (int y = 14; y >= 0; y--)
            {
                var rooms = map.RoomsOnFloor(y).ToList();
                Console.Write($"[FLOOR {y+1:00}] ");

                List<string> roomStrings = new List<string>();
                foreach(var r in rooms)
                {
                    string sym = GetRoomSymbol(r.Type);
                    string marker = (run.TheMap.GetCurrentRoom()?.Id == r.Id) ? "*" : "";
                    string outStr = string.Join(",", r.Outgoing);
                    string s = $"{marker}[{r.Id}:{sym}]->({outStr})";
                    roomStrings.Add(s);
                }
                Console.WriteLine(string.Join("   ", roomStrings));
            }
            Console.WriteLine("=====================");
            Console.ResetColor();
            Console.WriteLine("Legend: M=Monster, E=Elite, ?=Event, $=Shop, R=Rest");
            Console.WriteLine("Format: [ID:Type]->(Connected Next Room IDs)");
        }

        static string GetRoomSymbol(RoomType t)
        {
            switch (t) {
                case RoomType.Monster: return "M";
                case RoomType.Elite: return "E";
                case RoomType.Event: return "?";
                case RoomType.Shop: return "$";
                case RoomType.Rest: return "R";
                case RoomType.Boss: return "B";
                default: return " ";
            }
        }
        static void HandleMap(GameController controller)
        {
            var nextRooms = controller.CurrentRun.TheMap.GetPossibleNextNodes();
            Console.WriteLine("Available Rooms:");
            foreach (var room in nextRooms)
            {
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
            Console.WriteLine("\nEnemies:");
            for (int i = 0; i < cm.Enemies.Count; i++)
            {
                var e = cm.Enemies[i];
                if (e.CurrentHealth <= 0) continue;

                var intent = cm.CurrentEnemyIntents.ContainsKey(e) ? cm.CurrentEnemyIntents[e] : null;
                string intentStr = intent != null ? $"{intent.Type} {intent.Value}" : "Unknown";
                string statuses = string.Join(", ", e.ActiveEffects.Select(ef => $"{ef.SourceData.Name}({ef.Stacks})"));

                Console.WriteLine($" [{i}] {e.SourceData.Name}: {e.CurrentHealth}/{e.MaxHealth} HP (Block {e.Block}) | Intent: {intentStr} | {statuses}");
            }
            var hero = controller.CurrentRun.TheHero;
            string heroStatuses = string.Join(", ", hero.ActiveEffects.Select(ef => $"{ef.SourceData.Name}({ef.Stacks})"));
            Console.WriteLine($"\nHero: Mana {hero.CurrentMana}/{hero.MaxMana} | Block {hero.Block} | {heroStatuses}");
            Console.WriteLine("Hand:");
            for (int i = 0; i < hero.Deck.Hand.Count; i++)
            {
                var card = hero.Deck.Hand[i];
                Console.WriteLine($" ({i}) {card.Name} [{card.ManaCost}]: {card.Description}");
            }
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
                    int tIdx = 0;
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
    public static class MockDataLoader
    {
        public static void LoadAll(CardPool cards, RelicPool relics, EnemyPool enemies, EffectPool effects, EventPool events, Dictionary<RoomType, RoomData> rooms)
        {
            var vul = new StatusEffectData { Id = "vuln", Name = "Vulnerable", EffectType = StatusEffectType.Vulnerable, Decay = DecayType.AfterXTURNS, Value = 150 };
            var str = new StatusEffectData { Id = "str", Name = "Strength", EffectType = StatusEffectType.Strength, Decay = DecayType.Permanent, Value = 1 };
            var wkn = new StatusEffectData { Id = "weak", Name = "Weakend", EffectType = StatusEffectType.Weakened, Decay = DecayType.AfterXTURNS, Value = 60 };
            var frl = new StatusEffectData { Id = "frail", Name = "Frail", EffectType = StatusEffectType.Frail, Decay = DecayType.AfterXTURNS, Value = 50 };
            var prc = new StatusEffectData { Id = "pierced", Name = "Pierced", EffectType = StatusEffectType.Pierced, Decay = DecayType.AfterXTURNS, Value = 0 };

            var drw = new DeckEffectData { Id = "draw", Name = "Draw Card", EffectType = DeckEffectType.DrawCard, Decay = DecayType.EndOfTurn, Value = 3 };
            var dsc = new DeckEffectData { Id = "discard", Name = "Discard Card", EffectType = DeckEffectType.DiscardCard, Decay = DecayType.EndOfTurn, Value = 1 };

            effects.EffectsById["vuln"] = vul;
            effects.EffectsById["str"] = str;
            effects.EffectsById["weak"] = wkn;
            effects.EffectsById["frail"] = frl;
            effects.EffectsById["pierced"] = prc;

            effects.EffectsById["draw"] = drw;
            effects.EffectsById["discard"] = dsc;
            var strike = new CardData { Id = "strike", Name = "Strike", ManaCost = 1, Type = CardType.Attack, Rarity = Rarity.Common, Description = "Deal 6 Dmg" };
            strike.Actions.Add(new CombatActionData(ActionType.DealDamage, 6, TargetType.SingleOpponent));
            cards.CardsById["strike"] = strike;
            var defend = new CardData { Id = "defend", Name = "Defend", ManaCost = 1, Type = CardType.Skill, Rarity = Rarity.Common, Description = "Gain 5 Block" };
            defend.Actions.Add(new CombatActionData(ActionType.GainBlock, 5, TargetType.Self));
            cards.CardsById["defend"] = defend;
            var bash = new CardData { Id = "bash", Name = "Bash", ManaCost = 2, Type = CardType.Attack, Rarity = Rarity.Common, Description = "8 Dmg, 2 Vuln" };
            bash.Actions.Add(new CombatActionData(ActionType.DealDamage, 8, TargetType.SingleOpponent));
            bash.Actions.Add(new CombatActionData(ActionType.ApplyStatusEffect, 2, TargetType.SingleOpponent, "vuln"));
            cards.CardsById["bash"] = bash;
            var humiliate = new CardData { Id = "humiliate", Name = "Humiliate", ManaCost = 1, Type = CardType.Skill, Rarity = Rarity.Uncommon, Description = "Apply 2 Weak and 2 Frail" };
            humiliate.Actions.Add(new CombatActionData(ActionType.ApplyStatusEffect, 2, TargetType.SingleOpponent, "weak"));
            humiliate.Actions.Add(new CombatActionData(ActionType.ApplyStatusEffect, 2, TargetType.SingleOpponent, "frail"));
            cards.CardsById["humiliate"] = humiliate;
            var fireball = new CardData { Id = "fireball", Name = "Fireball", ManaCost = 2, Type = CardType.Attack, Rarity = Rarity.Uncommon, Description = "Deal 10 Dmg and Draw 1 Card" };
            fireball.Actions.Add(new CombatActionData(ActionType.DealDamage, 10, TargetType.SingleOpponent));
            fireball.Actions.Add(new CombatActionData(ActionType.ApplyDeckEffect, 1, TargetType.Self, "draw"));
            cards.CardsById["fireball"] = fireball;
            var lucky = new CardData { Id = "lucky_day", Name = "Lucky Day", ManaCost = 0, Type = CardType.Skill, Rarity = Rarity.Uncommon, Description = "Discard 1 Card, Draw 3 Cards" };
            lucky.Actions.Add(new CombatActionData(ActionType.ApplyDeckEffect, 1, TargetType.Self, "discard"));
            lucky.Actions.Add(new CombatActionData(ActionType.ApplyDeckEffect, 3, TargetType.Self, "draw"));
            cards.CardsById["lucky_day"] = lucky;
            var pierce = new CardData { Id = "pierce", Name = "Pierce", ManaCost = 1, Type = CardType.Attack, Rarity = Rarity.Rare, Description = "Deal 2 Dmg. Apply Pierced." };
            pierce.Actions.Add(new CombatActionData(ActionType.DealDamage, 2, TargetType.SingleOpponent));
            pierce.Actions.Add(new CombatActionData(ActionType.ApplyStatusEffect, 1, TargetType.SingleOpponent, "pierced"));
            cards.CardsById["pierce"] = pierce;
            var demon = new CardData { Id = "demon", Name = "Demon Strength", ManaCost = 3, Type = CardType.Power, Rarity = Rarity.Rare, Description = "Gain 3 Str" };
            demon.Actions.Add(new CombatActionData(ActionType.ApplyStatusEffect, 3, TargetType.Self, "str"));
            cards.CardsById["demon"] = demon;
            cards.CostRangesByRarity[Rarity.Common] = (40, 60);
            cards.CostRangesByRarity[Rarity.Uncommon] = (70, 90);
            cards.CostRangesByRarity[Rarity.Rare] = (150, 200);
            cards.CostRangesByRarity[Rarity.Legendary] = (250, 300);
            var burningBlood = new RelicData { Id = "burning_blood", Name = "Burning Blood", Rarity = Rarity.Common, Description = "Heal 6 HP after combat." };
            relics.RelicsById["burning_blood"] = burningBlood;
            relics.CostRangesByRarity[Rarity.Common] = (150, 200);
            var vajra = new RelicData { Id = "vajra", Name = "Vajra", Rarity = Rarity.Common, Description = "Start with 1 Strength." };
            var strEffect = new StatusEffectData { Id = "str", Name = "Strength", EffectType = StatusEffectType.Strength, Decay = DecayType.Permanent, Value = 1 };
            vajra.Effects.Add(strEffect);
            relics.RelicsById["vajra"] = vajra;
            var louse = new EnemyData { Id = "louse", Name = "Louse", StartingHealth = 15, Difficulty = 1.0f };
            louse.ActionSet.Add(new WeightedChoice<CombatActionData>(new CombatActionData(ActionType.DealDamage, 5, TargetType.SingleOpponent), 70));
            louse.ActionSet.Add(new WeightedChoice<CombatActionData>(new CombatActionData(ActionType.GainBlock, 5, TargetType.Self), 30));
            enemies.EnemiesById["louse"] = louse;
            var goblin = new EnemyData { Id = "goblin", Name = "Goblin", StartingHealth = 30, Difficulty = 2.0f };
            goblin.ActionSet.Add(new WeightedChoice<CombatActionData>(new CombatActionData(ActionType.DealDamage, 8, TargetType.SingleOpponent), 60));
            enemies.EnemiesById["goblin"] = goblin;

            var orc = new EnemyData { Id = "orc", Name = "Orc", StartingHealth = 40, Difficulty = 2.0f };
            orc.ActionSet.Add(new WeightedChoice<CombatActionData>(new CombatActionData(ActionType.DealDamage, 6, TargetType.SingleOpponent), 50));
            orc.ActionSet.Add(new WeightedChoice<CombatActionData>(new CombatActionData(ActionType.GainBlock, 8, TargetType.Self), 50));
            enemies.EnemiesById["orc"] = orc;
            var ogre = new EnemyData { Id = "ogre", Name = "Ogre", StartingHealth = 60, Difficulty = 3.0f };
            ogre.ActionSet.Add(new WeightedChoice<CombatActionData>(new CombatActionData(ActionType.DealDamage, 12, TargetType.SingleOpponent), 50));
            ogre.ActionSet.Add(new WeightedChoice<CombatActionData>(new CombatActionData(ActionType.GainBlock, 12, TargetType.Self), 30));
            ogre.ActionSet.Add(new WeightedChoice<CombatActionData>(new CombatActionData(ActionType.ApplyStatusEffect, 2, TargetType.SingleOpponent, "weak"), 20));
            enemies.EnemiesById["ogre"] = ogre;

            var troll = new EnemyData { Id = "troll", Name = "Troll", StartingHealth = 70, Difficulty = 3.0f };
            troll.ActionSet.Add(new WeightedChoice<CombatActionData>(new CombatActionData(ActionType.DealDamage, 10, TargetType.SingleOpponent), 40));
            troll.ActionSet.Add(new WeightedChoice<CombatActionData>(new CombatActionData(ActionType.GainBlock, 15, TargetType.Self), 40));
            troll.ActionSet.Add(new WeightedChoice<CombatActionData>(new CombatActionData(ActionType.ApplyStatusEffect, 3, TargetType.Self, "str"), 20));
            enemies.EnemiesById["troll"] = troll;
            var giant = new EnemyData { Id = "giant", Name = "Giant", StartingHealth = 90, Difficulty = 4.0f };
            giant.ActionSet.Add(new WeightedChoice<CombatActionData>(new CombatActionData(ActionType.DealDamage, 15, TargetType.SingleOpponent), 30));
            giant.ActionSet.Add(new WeightedChoice<CombatActionData>(new CombatActionData(ActionType.GainBlock, 20, TargetType.Self), 30));
            giant.ActionSet.Add(new WeightedChoice<CombatActionData>(new CombatActionData(ActionType.ApplyStatusEffect, 3, TargetType.SingleOpponent, "vuln"), 20));
            giant.ActionSet.Add(new WeightedChoice<CombatActionData>(new CombatActionData(ActionType.ApplyStatusEffect, 4, TargetType.Self, "str"), 20));
            enemies.EnemiesById["giant"] = giant;
            var boss = new EnemyData { Id = "slime", Name = "Slime Boss", StartingHealth = 100, Difficulty = 5.0f };
            boss.ActionSet.Add(new WeightedChoice<CombatActionData>(new CombatActionData(ActionType.DealDamage, 15, TargetType.SingleOpponent), 50));
            boss.ActionSet.Add(new WeightedChoice<CombatActionData>(new CombatActionData(ActionType.DealDamage, 30, TargetType.SingleOpponent), 10));
            enemies.EnemiesById["slime"] = boss;
            var evt = new EventChoiceSet { Id = "fountain", EventTitle = "Golden Fountain", EventDescription = "Drink? Gain 50g but lose 5 HP." };
            var choice1 = new EventChoice { ChoiceText = "Drink" };
            choice1.Effects.Add(new EventEffect { Type = EventEffectType.GainGold, Value = 50 });
            choice1.Effects.Add(new EventEffect { Type = EventEffectType.LoseHP, Value = 5 });
            var choice2 = new EventChoice { ChoiceText = "Leave" };
            choice2.Effects.Add(new EventEffect { Type = EventEffectType.Quit });
            evt.Choices.Add(choice1);
            evt.Choices.Add(choice2);
            events.EventsById["fountain"] = evt;

            var evt2 = new EventChoiceSet { Id = "sacrifice", EventTitle = "Sacrifice", EventDescription = "Remove 1 card, heal 30 HP" };
            var choice21 = new EventChoice { ChoiceText = "Sacrifice" };
            choice21.Effects.Add(new EventEffect { Type = EventEffectType.RemoveCard, Value = 50 });
            choice21.Effects.Add(new EventEffect { Type = EventEffectType.HealHP, Value = 30 });
            var choice22 = new EventChoice { ChoiceText = "Leave" };
            choice22.Effects.Add(new EventEffect { Type = EventEffectType.Quit });
            evt2.Choices.Add(choice21);
            evt2.Choices.Add(choice22);
            events.EventsById["sacrifice"] = evt2;
            rooms[RoomType.Monster] = new RoomData { Type = RoomType.Monster, MinValue = 2.0f, MaxValue = 4.0f };
            rooms[RoomType.Elite] = new RoomData { Type = RoomType.Elite, MinValue = 5.0f, MaxValue = 8.0f };
            rooms[RoomType.Boss] = new RoomData { Type = RoomType.Boss, MinValue = 5.0f, MaxValue = 10.0f };
            rooms[RoomType.Event] = new RoomData { Type = RoomType.Event };
            rooms[RoomType.Shop] = new RoomData { Type = RoomType.Shop };
            rooms[RoomType.Rest] = new RoomData { Type = RoomType.Rest, MinValue=30, MaxValue=30 };
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
                StartingDeckCardIds = new List<string> { "strike", "strike", "strike", "strike", "defend", "defend", "defend", "defend", "bash", "fireball", "lucky_day", "humiliate", "pierce", "demon" }
            };
        }
    }
}