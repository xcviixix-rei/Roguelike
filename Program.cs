using Roguelike.Agents;
using Roguelike.Data;
using Roguelike.GA;
using RoguelikeMapGen;
using System;
using System.Collections.Generic;
using System.IO;

namespace RoguelikeGASimulator
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Setting up GA Simulation Environment...");

            string jsonPath = Path.Combine(Directory.GetCurrentDirectory(), "GameData.json");
            
            if (!File.Exists(jsonPath))
            {
                Console.WriteLine($"ERROR: GameData.json not found at {jsonPath}");
                Console.WriteLine("Press any key to exit.");
                Console.ReadKey();
                return;
            }

            var (hero, cards, relics, enemies, effects, events, rooms) = GameDataLoader.LoadFromJson(jsonPath);

            Console.WriteLine("Game data loaded successfully!");
            Console.WriteLine($"  - {cards.CardsById.Count} cards");
            Console.WriteLine($"  - {enemies.EnemiesById.Count} enemies");
            Console.WriteLine($"  - {relics.RelicsById.Count} relics");
            Console.WriteLine($"  - {effects.EffectsById.Count} effects");

            var agent = new HeuristicPlayerAI();
            var runner = new SimulationRunner(agent, hero, cards, relics, enemies, effects, events, rooms);
            var evaluator = new FitnessEvaluator();
            var ga = new GeneticAlgorithm(runner, evaluator, cards, enemies);

            ga.PopulationSize = 200;
            ga.Generations = 100;
            ga.RunsPerGenome = 400;

            Console.WriteLine("Setup complete. Starting Genetic Algorithm...");
            Console.WriteLine("=============================================");

            ga.Run();

            Console.WriteLine("=============================================");
            Console.WriteLine("Simulation finished. Press any key to exit.");
            Console.ReadKey();
        }
    }
}