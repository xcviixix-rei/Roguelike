using Roguelike.Data;
using Roguelike.Core;
using Roguelike.Core.Map;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Roguelike.Core.AI
{
    /// <summary>
    /// A player agent that interacts via the console for human play.
    /// </summary>
    public class ConsolePlayerAI : IPlayerAgent
    {
        public int ChooseMapNode(GameRun run)
        {
            var possibleRooms = run.TheMap.GetPossibleNextNodes();
            if (!possibleRooms.Any()) return -1;

            if (possibleRooms.Count == 1)
            {
                Console.WriteLine($"\nOnly one path forward: {possibleRooms[0].Type}");
                return possibleRooms[0].Id;
            }

            // Display full map
            DisplayMap(run);
            
            Console.WriteLine($"\n=== FLOOR {run.CurrentFloor} ===");
            DisplayHeroStatus(run.TheHero);
            
            Console.WriteLine("\nChoose next room:");
            for (int i = 0; i < possibleRooms.Count; i++)
            {
                var room = possibleRooms[i];
                string starRating = room.StarRating > 0 ? $" ({room.StarRating}★)" : "";
                Console.WriteLine($"{i + 1}. [{room.Type}]{starRating}");
            }

            int choice = GetUserChoice(1, possibleRooms.Count) - 1;
            return possibleRooms[choice].Id;
        }

        public CombatDecision GetCombatDecision(GameRun run)
        {
            var hero = run.TheHero;
            var combat = run.CurrentCombat;
            var enemies = combat.Enemies;

            Console.Clear();
            Console.WriteLine($"=== COMBAT - Turn {combat.TurnNumber} ===");
            DisplayHeroStatus(hero);
            
            Console.WriteLine("\nENEMIES:");
            for (int i = 0; i < enemies.Count; i++)
            {
                var enemy = enemies[i];
                if (enemy.CurrentHealth <= 0)
                {
                    Console.WriteLine($"{i + 1}. {enemy.SourceEnemyData.Name} (DEAD)");
                    continue;
                }

                string intentStr = "Unknown";
                if (combat.CurrentEnemyIntents.TryGetValue(enemy, out var intent))
                {
                    intentStr = FormatIntent(intent);
                }

                string effects = FormatStatusEffects(enemy.ActiveEffects);
                if (!string.IsNullOrEmpty(effects)) effects = $" [{effects}]";

                Console.WriteLine($"{i + 1}. {enemy.SourceEnemyData.Name}: {enemy.CurrentHealth}/{enemy.MaxHealth} HP{effects} - {intentStr}");
            }

            Console.WriteLine("\nHAND:");
            for (int i = 0; i < hero.Deck.Hand.Count; i++)
            {
                var card = hero.Deck.Hand[i];
                Console.WriteLine($"{i + 1}. {card.Name} ({card.ManaCost} mana) - {card.Description}");
            }

            Console.WriteLine("\nACTIONS:");
            Console.WriteLine("1-N: Play card #N");
            Console.WriteLine("E: End Turn");
            Console.Write("Enter choice: ");

            while (true)
            {
                string input = Console.ReadLine()?.ToUpper()?.Trim();
                if (input == "E")
                {
                    return CombatDecision.EndTurn();
                }

                if (int.TryParse(input, out int cardIndex) && cardIndex >= 1 && cardIndex <= hero.Deck.Hand.Count)
                {
                    cardIndex--; // 0-indexed
                    var card = hero.Deck.Hand[cardIndex];
                    
                    if (card.ManaCost > hero.CurrentMana)
                    {
                        Console.WriteLine("Not enough mana!");
                        continue;
                    }

                    // Check for target
                    if (card.Actions.Any(a => a.Target == TargetType.SingleOpponent))
                    {
                        var aliveEnemies = enemies.Select((e, idx) => new { Enemy = e, Index = idx }).Where(x => x.Enemy.CurrentHealth > 0).ToList();
                        if (aliveEnemies.Count == 0) return CombatDecision.EndTurn(); // Should not happen if combat is ongoing

                        if (aliveEnemies.Count == 1)
                        {
                            return CombatDecision.Play(cardIndex, aliveEnemies[0].Index);
                        }

                        Console.WriteLine("Select target enemy index (from list above):");
                        while(true)
                        {
                             if (int.TryParse(Console.ReadLine(), out int targetInput))
                             {
                                 int actualTargetIndex = targetInput - 1;
                                 if (actualTargetIndex >= 0 && actualTargetIndex < enemies.Count && enemies[actualTargetIndex].CurrentHealth > 0)
                                 {
                                     return CombatDecision.Play(cardIndex, actualTargetIndex);
                                 }
                             }
                             Console.WriteLine("Invalid target. Try again.");
                        }
                    }
                    else
                    {
                        return CombatDecision.Play(cardIndex, 0); // No target needed
                    }
                }
                
                Console.WriteLine("Invalid input. Try again (1-N or E):");
            }
        }

        public ShopDecision GetShopDecision(GameRun run)
        {
            var shop = run.CurrentShop;
            Console.WriteLine("\n=== SHOP ===");
            Console.WriteLine($"Gold: {run.TheHero.CurrentGold}");

            var options = new List<string>();
            var actions = new List<Func<ShopDecision>>();

            // Relics
            for (int i = 0; i < shop.RelicsForSale.Count; i++)
            {
                var item = shop.RelicsForSale[i];
                string status = item.IsSold ? "[SOLD]" : $"{item.Price}g";
                options.Add($"Buy Relic: {item.Item.Name} ({item.Item.Description}) - {status}");
                
                int index = i; // Closure capture
                actions.Add(() => ShopDecision.BuyRelic(index));
            }

            // Cards
            for (int i = 0; i < shop.CardsForSale.Count; i++)
            {
                var item = shop.CardsForSale[i];
                string status = item.IsSold ? "[SOLD]" : $"{item.Price}g";
                options.Add($"Buy Card: {item.Item.Name} ({item.Item.Description}) - {status}");

                int index = i; // Closure capture
                actions.Add(() => ShopDecision.BuyCard(index));
            }

            // Leave
            options.Add("Leave Shop");
            actions.Add(() => ShopDecision.Leave());

            for (int i = 0; i < options.Count; i++)
            {
                Console.WriteLine($"{i + 1}. {options[i]}");
            }

            int choice = GetUserChoice(1, options.Count) - 1;
            return actions[choice]();
        }

        public int ChooseEventOption(GameRun run)
        {
            var evt = run.CurrentEvent;
            Console.WriteLine($"\n=== EVENT: {evt.EventTitle} ===");
            Console.WriteLine(evt.EventDescription);
            
            for (int i = 0; i < evt.Choices.Count; i++)
            {
                Console.WriteLine($"{i + 1}. {evt.Choices[i].ChoiceText}");
            }

            return GetUserChoice(1, evt.Choices.Count) - 1;
        }

        public int ChooseCardReward(GameRun run)
        {
            var choices = run.CardRewardChoices;
            Console.WriteLine("\n=== CARD REWARD ===");
            Console.WriteLine("Choose a card to add to your deck:");

            for (int i = 0; i < choices.Count; i++)
            {
                Console.WriteLine($"{i + 1}. {choices[i].Name}: {choices[i].Description} (Star: {choices[i].StarRating})");
            }
            Console.WriteLine($"{choices.Count + 1}. Skip");

            int input = GetUserChoice(1, choices.Count + 1);
            if (input == choices.Count + 1) return -1;
            return input - 1;
        }

        // --- Helpers ---

        private int GetUserChoice(int min, int max)
        {
            while (true)
            {
                Console.Write($"Enter choice ({min}-{max}): ");
                if (int.TryParse(Console.ReadLine(), out int result) && result >= min && result <= max)
                {
                    return result;
                }
                Console.WriteLine("Invalid input.");
            }
        }

        private string FormatIntent(CombatActionData intent)
        {
            switch (intent.Type)
            {
                case ActionType.DealDamage: return $"Attacks for {intent.Value}";
                case ActionType.GainBlock: return $"Blocks for {intent.Value}";
                case ActionType.ApplyStatusEffect: return $"Debuffs ({intent.EffectId})";
                case ActionType.ApplyDeckEffect: return "Modifies Deck";
                default: return intent.Type.ToString();
            }
        }

        // --- Display Helpers ---

        private void DisplayMap(GameRun run)
        {
            var map = run.TheMap.CurrentMap;
            var currentRoom = run.TheMap.GetCurrentRoom();
            var possibleNext = run.TheMap.GetPossibleNextNodes().Select(r => r.Id).ToHashSet();

            Console.WriteLine("\n=== MAP ===");
            Console.WriteLine("Legend: [M]onster [E]lite [S]hop [R]est [?]Event [B]oss | @ = Current | * = Available");
            Console.WriteLine();

            // Display from top to bottom (floor 15 to 0)
            for (int y = MapGraph.Height; y >= 0; y--)
            {
                Console.Write($"F{y,2}: ");
                
                for (int x = 0; x < MapGraph.Width; x++)
                {
                    var room = map.GetRoomAt(x, y);
                    if (room == null)
                    {
                        Console.Write("   ");
                    }
                    else
                    {
                        string marker = " ";
                        if (currentRoom != null && room.Id == currentRoom.Id)
                            marker = "@";
                        else if (possibleNext.Contains(room.Id))
                            marker = "*";

                        string roomChar = room.Type switch
                        {
                            RoomType.Monster => "M",
                            RoomType.Elite => "E",
                            RoomType.Shop => "S",
                            RoomType.Rest => "R",
                            RoomType.Event => "?",
                            RoomType.Boss => "B",
                            _ => " "
                        };

                        Console.Write($"{marker}{roomChar} ");
                    }
                }
                Console.WriteLine();
            }
            Console.WriteLine();
        }

        private void DisplayHeroStatus(Hero hero)
        {
            // Get Strength value if present
            int strengthValue = 0;
            var strengthEffect = hero.ActiveEffects.FirstOrDefault(e => e.SourceData is StatusEffectData s && s.EffectType == StatusEffectType.Strength);
            if (strengthEffect != null)
            {
                var strengthData = (StatusEffectData)strengthEffect.SourceData;
                strengthValue = strengthData.Intensity;
            }

            string statusEffects = FormatStatusEffects(hero.ActiveEffects);
            string effectsDisplay = !string.IsNullOrEmpty(statusEffects) ? $" | Effects: {statusEffects}" : "";

            Console.WriteLine($"HERO: {hero.CurrentHealth}/{hero.MaxHealth} HP  |  {hero.Block} Block  |  {hero.CurrentMana}/{hero.MaxMana} Mana  |  Strength: {(strengthValue > 0 ? "+" : "")}{strengthValue}{effectsDisplay}");
        }

        private string FormatStatusEffects(List<ActiveEffect> effects)
        {
            if (effects == null || !effects.Any()) return "";
            
            var result = new List<string>();
            
            foreach (var e in effects)
            {
                string effectName;
                if (e.SourceData is StatusEffectData s)
                {
                    effectName = s.EffectType.ToString();
                    int duration = e.RemainingDuration == int.MaxValue ? 999 : e.RemainingDuration;
                    result.Add($"{effectName}({duration})");
                }
                else
                {
                    result.Add(e.SourceData.Name);
                }
            }
            
            return string.Join(", ", result);
        }
    }
}
