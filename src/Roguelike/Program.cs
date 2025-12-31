using Roguelike.Core.AI;
using Roguelike.Data;
using Roguelike.Optimization;
using Roguelike.Core.Map;
using System;
using System.Collections.Generic;
using System.IO;

namespace RoguelikeGASimulator
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("=== GENETIC ALGORITHM BALANCE SYSTEM ===\n");

            string jsonPath = Path.Combine(Directory.GetCurrentDirectory(), "GameData.json");
            if (!File.Exists(jsonPath))
            {
                Console.WriteLine($"ERROR: GameData.json not found");
                return;
            }

            var (hero, cards, relics, enemies, effects, events, rooms) = 
                GameDataLoader.LoadFromJson(jsonPath);

            Console.WriteLine("Game data loaded:");
            Console.WriteLine($"  - {cards.CardsById.Count} cards");
            Console.WriteLine($"  - {enemies.EnemiesById.Count} enemies");
            Console.WriteLine($"  - {relics.RelicsById.Count} relics\n");

            int choice = DisplayMenuAndGetChoice();

            switch (choice)
            {
                case 1:
                    RunPureOptimization(hero, cards, relics, enemies, effects, events, rooms);
                    break;
                case 2:
                    RunStructureAwareSearch(hero, cards, relics, enemies, effects, events, rooms);
                    break;
                case 3:
                    RunConsoleGame(hero, cards, relics, enemies, effects, events, rooms);
                    break;
            }

            Console.WriteLine("\n\nPress any key to exit.");
            Console.ReadKey();
        }

        static int DisplayMenuAndGetChoice()
        {
            while (true)
            {
                Console.WriteLine("=== SELECT OPTIMIZATION APPROACH ===");
                Console.WriteLine("1. Pure Optimization Approach");
                Console.WriteLine("   - Traditional Genetic Algorithm");
                Console.WriteLine("   - ~500 parameters (individual card/enemy values)");
                Console.WriteLine("   - Single-objective fitness function");
                Console.WriteLine("   - Detailed per-generation reports\n");
                
                Console.WriteLine("2. Structure-Aware Search Approach");
                Console.WriteLine("   - NSGA-II Multi-Objective Optimization");
                Console.WriteLine("   - ~50 hierarchical parameters");
                Console.WriteLine("   - Pareto front optimization");
                Console.WriteLine("   - Adaptive evaluation budget\n");

                Console.WriteLine("3. Play the Game");
                Console.WriteLine("   - Play manually via console");
                Console.WriteLine("   - Test game mechanics directly\n");

                Console.Write("Enter your choice (1, 2, or 3): ");
                string input = Console.ReadLine();

                if (int.TryParse(input, out int choice) && (choice == 1 || choice == 2 || choice == 3))
                {
                    Console.WriteLine();
                    return choice;
                }

                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Invalid input. Please enter 1 or 2.\n");
                Console.ResetColor();
            }
        }

        static void RunPureOptimization(
            HeroData hero, 
            CardPool cards, 
            RelicPool relics, 
            EnemyPool enemies, 
            EffectPool effects, 
            EventPool events, 
            Dictionary<RoomType, RoomData> rooms)
        {
            Console.WriteLine("=== PURE OPTIMIZATION APPROACH ===\n");

            var rng = new Random();
            var agent = new HeuristicPlayerAI();
            var runner = new BalanceSimulationRunner(
                agent, hero, cards, relics, enemies, effects, events, rooms);

            var evaluator = new FitnessEvaluator
            {
                TargetWinRate = 0.45f,
                TargetVictoryHpPercent = 0.30f,
                TargetAvgFloorOnDeath = 10.0f
            };

            var ga = new GeneticAlgorithm(runner, evaluator, cards, enemies, effects)
            {
                PopulationSize = 100,
                Generations = 30,
                RunsPerGenome = 200,
                MutationRate = 0.05f,
                ElitismRate = 0.10f
            };

            Console.WriteLine("Configuration:");
            Console.WriteLine($"  - Population: {ga.PopulationSize}");
            Console.WriteLine($"  - Generations: {ga.Generations}");
            Console.WriteLine($"  - Runs Per Genome: {ga.RunsPerGenome}");
            Console.WriteLine($"  - Mutation Rate: {ga.MutationRate}");
            Console.WriteLine($"  - Elitism Rate: {ga.ElitismRate}\n");

            Console.WriteLine("Target Metrics:");
            Console.WriteLine($"  - Win Rate: {evaluator.TargetWinRate:P0}");
            Console.WriteLine($"  - Victory HP: {evaluator.TargetVictoryHpPercent:P0}");
            Console.WriteLine($"  - Floor on Death: {evaluator.TargetAvgFloorOnDeath:F1}\n");

            Console.WriteLine("Starting optimization...\n");
            ga.Run();
        }

        static void RunStructureAwareSearch(
            HeroData hero, 
            CardPool cards, 
            RelicPool relics, 
            EnemyPool enemies, 
            EffectPool effects, 
            EventPool events, 
            Dictionary<RoomType, RoomData> rooms)
        {
            Console.WriteLine("=== STRUCTURE-AWARE SEARCH APPROACH ===\n");

            var rng = new Random();
            var agent = new HeuristicPlayerAI();
            var runner = new HierarchicalSimulationRunner(
                agent, hero, cards, relics, enemies, effects, events, rooms);
            
            var moEvaluator = new MultiObjectiveEvaluator
            {
                TargetWinRate = 0.45f,
                TargetVictoryHp = 0.30f,
                TargetAvgFloorOnDeath = 10.0f
            };

            var ga = new ImprovedGeneticAlgorithm(runner, moEvaluator, rng, cards, enemies)
            {
                PopulationSize = 100,
                Generations = 30,
                MutationRate = 0.15f
            };

            Console.WriteLine("Configuration:");
            Console.WriteLine($"  - Population: {ga.PopulationSize}");
            Console.WriteLine($"  - Generations: {ga.Generations}");
            Console.WriteLine($"  - Mutation Rate: {ga.MutationRate}\n");

            Console.WriteLine("Target Metrics:");
            Console.WriteLine($"  - Win Rate: {moEvaluator.TargetWinRate:P0}");
            Console.WriteLine($"  - Victory HP: {moEvaluator.TargetVictoryHp:P0}");
            Console.WriteLine($"  - Floor on Death: {moEvaluator.TargetAvgFloorOnDeath:F1}\n");

            Console.WriteLine("Starting optimization...\n");
            ga.Run();

        }

        static void RunConsoleGame(
            HeroData hero, 
            CardPool cards, 
            RelicPool relics, 
            EnemyPool enemies, 
            EffectPool effects, 
            EventPool events, 
            Dictionary<RoomType, RoomData> rooms)
        {
            Console.WriteLine("=== PLAY MODE ===\n");
            
            var player = new ConsolePlayerAI();
            var runner = new HierarchicalSimulationRunner(
                player, hero, cards, relics, enemies, effects, events, rooms);
            
            Console.WriteLine("Game Initialized. Starting Run...");
            
            // Run a single simulation with a default genome
            var genome = new HierarchicalGenome(); 
            var rng = new Random();
            int seed = rng.Next();
            
            var stats = runner.Run(genome, seed);
            
            Console.WriteLine("\n=== GAME OVER ===");
            Console.WriteLine($"Result: {(stats.IsVictory ? "VICTORY!" : "DEFEAT")}");
            Console.WriteLine($"Floor Reached: {stats.FinalFloorReached}");
            Console.WriteLine($"Gold: {stats.GoldCollected}");
        }
    }
}
