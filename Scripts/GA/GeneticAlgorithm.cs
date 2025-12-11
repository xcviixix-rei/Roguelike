using Newtonsoft.Json;
using Roguelike.Agents;
using Roguelike.Data;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;

namespace Roguelike.GA
{
    public class GeneticAlgorithm
    {
        // GA Parameters
        public int PopulationSize = 100;
        public int Generations = 50;
        public int RunsPerGenome = 200;
        public float MutationRate = 0.05f;
        public float ElitismRate = 0.10f;

        // Dependencies
        private readonly SimulationRunner _runner;
        private readonly FitnessEvaluator _evaluator;
        private readonly Random _rng;
        private readonly object _rngLock = new object();

        // Base data pools
        private readonly CardPool _baseCards;
        private readonly EnemyPool _baseEnemies;

        public GeneticAlgorithm(SimulationRunner runner, FitnessEvaluator evaluator, CardPool baseCards, EnemyPool baseEnemies)
        {
            _runner = runner;
            _evaluator = evaluator;
            _rng = new Random();
            _baseCards = baseCards;
            _baseEnemies = baseEnemies;
        }

        public void Run()
        {
            Stopwatch totalTime = Stopwatch.StartNew();
            List<BalanceGenome> population = InitializePopulation();
            
            for (int gen = 0; gen < Generations; gen++)
            {
                Stopwatch genTime = Stopwatch.StartNew();
                Console.WriteLine($"\n===== GENERATION {gen + 1} / {Generations} =====");

                int genomesEvaluated = 0;
                Console.WriteLine($"Evaluating {PopulationSize} genomes ({RunsPerGenome} runs each)...");

                var fitnessScores = population.AsParallel()
                                              .Select(genome =>
                                              {
                                                  var fitness = EvaluateGenome(genome);
                                                  // Safely increment a counter to show progress
                                                  Interlocked.Increment(ref genomesEvaluated);
                                                  Console.Write($"\rProgress: {genomesEvaluated}/{PopulationSize} genomes evaluated...");
                                                  return fitness;
                                              })
                                              .ToList();
                Console.WriteLine("\nEvaluation complete");

                var rankedPopulation = population.Zip(fitnessScores, (genome, score) => new { Genome = genome, Fitness = score })
                                                 .OrderByDescending(x => x.Fitness)
                                                 .ToList();

                var bestGenome = rankedPopulation.First().Genome;
                var bestFitness = rankedPopulation.First().Fitness;

                genTime.Stop();
                Console.WriteLine($"Best Fitness: {bestFitness:F4} (Generation took {genTime.Elapsed.TotalSeconds:F2}s)");
                
                SaveBestGenomeReport(bestGenome, bestFitness, gen);

                population = CreateNextGeneration(rankedPopulation.Select(x => x.Genome).ToList());
            }
            
            totalTime.Stop();
            Console.WriteLine($"\nGA run complete in {totalTime.Elapsed.TotalMinutes:F2} minutes. Final best genome report saved.");
        }

        private List<BalanceGenome> InitializePopulation()
        {
            var population = new List<BalanceGenome>();
            for (int i = 0; i < PopulationSize; i++)
            {
                var genome = new BalanceGenome();
                genome.Randomize(_baseCards, _baseEnemies, _rng);
                population.Add(genome);
            }
            return population;
        }

        private float EvaluateGenome(BalanceGenome genome)
        {
            var results = new List<SimulationStats>();
            for (int i = 0; i < RunsPerGenome; i++)
            {
                int seed;
                lock (_rngLock)
                {
                    seed = _rng.Next();
                }
                results.Add(_runner.Run(genome, seed));
            }
            return _evaluator.CalculateFitness(results);
        }

        private List<BalanceGenome> CreateNextGeneration(List<BalanceGenome> rankedGenomes)
        {
            var nextGeneration = new List<BalanceGenome>();

            // Elitism
            int eliteCount = (int)(PopulationSize * ElitismRate);
            nextGeneration.AddRange(rankedGenomes.Take(eliteCount).Select(g => g.Clone()));

            while (nextGeneration.Count < PopulationSize)
            {
                var parent1 = TournamentSelect(rankedGenomes);
                var parent2 = TournamentSelect(rankedGenomes);

                // Crossover
                var child = Crossover(parent1, parent2);

                // Mutate
                Mutate(child);

                nextGeneration.Add(child);
            }
            return nextGeneration;
        }

        private BalanceGenome TournamentSelect(List<BalanceGenome> rankedPopulation)
        {
            int tournamentSize = Math.Min(5, rankedPopulation.Count);
            
            var tournament = new List<BalanceGenome>();
            var selectedIndices = new HashSet<int>();
            
            lock (_rngLock)
            {
                while (tournament.Count < tournamentSize)
                {
                    int index = _rng.Next(rankedPopulation.Count);
                    if (selectedIndices.Add(index))
                    {
                        tournament.Add(rankedPopulation[index]);
                    }
                }
            }
            
            return tournament.OrderBy(genome => rankedPopulation.IndexOf(genome)).First();
        }

        private BalanceGenome TournamentSelectWithFitness(List<BalanceGenome> rankedPopulation, List<float> fitnessScores)
        {
            int tournamentSize = Math.Min(5, rankedPopulation.Count);
            
            BalanceGenome bestGenome = null;
            float bestFitness = float.MinValue;
            
            lock (_rngLock)
            {
                for (int i = 0; i < tournamentSize; i++)
                {
                    int index = _rng.Next(rankedPopulation.Count);
                    var genome = rankedPopulation[index];
                    var fitness = fitnessScores[index];
                    
                    if (fitness > bestFitness)
                    {
                        bestFitness = fitness;
                        bestGenome = genome;
                    }
                }
            }
            
            return bestGenome;
        }

        private BalanceGenome Crossover(BalanceGenome parent1, BalanceGenome parent2)
        {
            var child = new BalanceGenome();
            
            lock(_rngLock)
            {
                // Global economy
                child.GoldDropMultiplier = (_rng.NextDouble() < 0.5) ? parent1.GoldDropMultiplier : parent2.GoldDropMultiplier;
                for (int i = 1; i <= 5; i++)
                {
                    child.ShopPriceScalars[i] = (_rng.NextDouble() < 0.5) ? parent1.ShopPriceScalars[i] : parent2.ShopPriceScalars[i];
                }

                // Hero stats
                child.HeroHealthScalar = (_rng.NextDouble() < 0.5) ? parent1.HeroHealthScalar : parent2.HeroHealthScalar;
                child.HeroStartGoldScalar = (_rng.NextDouble() < 0.5) ? parent1.HeroStartGoldScalar : parent2.HeroStartGoldScalar;
                child.HeroManaOffset = (_rng.NextDouble() < 0.5) ? parent1.HeroManaOffset : parent2.HeroManaOffset;

                // Card Cost Modifiers
                foreach (var key in parent1.CardCostModifiers.Keys)
                {
                    child.CardCostModifiers[key] = (_rng.NextDouble() < 0.5) ? parent1.CardCostModifiers[key] : parent2.CardCostModifiers[key];
                }
                // Card Action Scalars
                foreach (var key in parent1.CardActionScalars.Keys)
                {
                    var p1_actions = parent1.CardActionScalars[key];
                    var p2_actions = parent2.CardActionScalars[key];
                    var child_actions = new List<float>();
                    for (int i = 0; i < p1_actions.Count; i++)
                    {
                        child_actions.Add((_rng.NextDouble() < 0.5) ? p1_actions[i] : p2_actions[i]);
                    }
                    child.CardActionScalars[key] = child_actions;
                }
                
                // Enemy Health
                foreach (var key in parent1.EnemyHealthScalars.Keys)
                {
                    child.EnemyHealthScalars[key] = (_rng.NextDouble() < 0.5) ? parent1.EnemyHealthScalars[key] : parent2.EnemyHealthScalars[key];
                }
                // Enemy Action Weights
                foreach (var key in parent1.EnemyActionWeightScalars.Keys)
                {
                    var p1_weights = parent1.EnemyActionWeightScalars[key];
                    var p2_weights = parent2.EnemyActionWeightScalars[key];
                    var child_weights = new List<float>();
                    for (int i = 0; i < p1_weights.Count; i++)
                    {
                        child_weights.Add((_rng.NextDouble() < 0.5) ? p1_weights[i] : p2_weights[i]);
                    }
                    child.EnemyActionWeightScalars[key] = child_weights;
                }
                // Enemy Action Values
                foreach (var key in parent1.EnemyActionValueScalars.Keys)
                {
                    var p1_values = parent1.EnemyActionValueScalars[key];
                    var p2_values = parent2.EnemyActionValueScalars[key];
                    var child_values = new List<float>();
                    for (int i = 0; i < p1_values.Count; i++)
                    {
                        child_values.Add((_rng.NextDouble() < 0.5) ? p1_values[i] : p2_values[i]);
                    }
                    child.EnemyActionValueScalars[key] = child_values;
                }
            }
            return child;
        }

        private void Mutate(BalanceGenome genome)
        {
            lock (_rngLock)
            {
                // Global economy
                if (_rng.NextDouble() < MutationRate)
                    genome.GoldDropMultiplier += (float)(_rng.NextDouble() * 0.2 - 0.1); // Nudge by +/- 0.1
                genome.GoldDropMultiplier = Math.Clamp(genome.GoldDropMultiplier, 0.1f, 1.0f);

                for (int i = 1; i <= 5; i++)
                {
                    if (_rng.NextDouble() < MutationRate)
                        genome.ShopPriceScalars[i] += (float)(_rng.NextDouble() * 0.2 - 0.1);
                    genome.ShopPriceScalars[i] = Math.Clamp(genome.ShopPriceScalars[i], 0.5f, 2.0f); // 50% to 200% price
                }

                // Hero stats
                if (_rng.NextDouble() < MutationRate)
                    genome.HeroHealthScalar += (float)(_rng.NextDouble() * 0.1 - 0.05);
                genome.HeroHealthScalar = Math.Clamp(genome.HeroHealthScalar, 0.8f, 1.2f);

                if (_rng.NextDouble() < MutationRate)
                    genome.HeroStartGoldScalar += (float)(_rng.NextDouble() * 0.2 - 0.1);
                genome.HeroStartGoldScalar = Math.Clamp(genome.HeroStartGoldScalar, 0.7f, 1.5f);

                if (_rng.NextDouble() < (MutationRate * 0.1))
                    genome.HeroManaOffset += _rng.Next(3) - 1; // -1, 0, +1
                genome.HeroManaOffset = Math.Clamp(genome.HeroManaOffset, -1, 1); // +/- 1 mana

                // Cards
                foreach (var key in genome.CardCostModifiers.Keys.ToList())
                {
                    if (_rng.NextDouble() < MutationRate)
                    {
                        int originalCost = _baseCards.GetCard(key).ManaCost;
                        genome.CardCostModifiers[key] += _rng.Next(3) - 1;
                        int minCostMod = originalCost > 0 ? -(originalCost - 1) : 0;
                        genome.CardCostModifiers[key] = Math.Clamp(genome.CardCostModifiers[key], minCostMod, 1);
                    }
                }
                foreach (var key in genome.CardActionScalars.Keys.ToList())
                {
                    for(int i=0; i<genome.CardActionScalars[key].Count; i++)
                    {
                        if (_rng.NextDouble() < MutationRate)
                            genome.CardActionScalars[key][i] += (float)(_rng.NextDouble() * 0.2 - 0.1);
                        genome.CardActionScalars[key][i] = Math.Clamp(genome.CardActionScalars[key][i], 0.5f, 2.0f);
                    }
                }

                // Enemies
                foreach (var key in genome.EnemyHealthScalars.Keys.ToList())
                {
                    if (_rng.NextDouble() < MutationRate)
                        genome.EnemyHealthScalars[key] += (float)(_rng.NextDouble() * 0.2 - 0.1);
                    genome.EnemyHealthScalars[key] = Math.Clamp(genome.EnemyHealthScalars[key], 0.5f, 2.0f);
                }
                foreach (var key in genome.EnemyActionWeightScalars.Keys.ToList())
                {
                    for(int i=0; i<genome.EnemyActionWeightScalars[key].Count; i++)
                    {
                        if (_rng.NextDouble() < MutationRate)
                            genome.EnemyActionWeightScalars[key][i] += (float)(_rng.NextDouble() * 0.4 - 0.2);
                        genome.EnemyActionWeightScalars[key][i] = Math.Max(0.1f, genome.EnemyActionWeightScalars[key][i]);
                    }
                }
                foreach (var key in genome.EnemyActionValueScalars.Keys.ToList())
                {
                    for(int i=0; i<genome.EnemyActionValueScalars[key].Count; i++)
                    {
                        if (_rng.NextDouble() < MutationRate)
                            genome.EnemyActionValueScalars[key][i] += (float)(_rng.NextDouble() * 0.2 - 0.1);
                        genome.EnemyActionValueScalars[key][i] = Math.Clamp(genome.EnemyActionValueScalars[key][i], 0.5f, 2.0f);
                    }
                }
            }
        }

        private void SaveBestGenomeReport(BalanceGenome genome, float fitness, int generation)
        {
            var reportResults = new List<SimulationStats>();
            for (int i = 0; i < 1000; i++)
            {
                int seed;
                lock (_rngLock)
                {
                    seed = _rng.Next();
                }
                reportResults.Add(_runner.Run(genome, seed));
            }

            var report = ReportGenerator.Generate(reportResults, fitness);
            
            var output = new
            {
                Generation = generation,
                Fitness = fitness,
                Genome = genome,
                Report = report
            };

            string json = JsonConvert.SerializeObject(output, Formatting.Indented);
            
            string path = Path.Combine(Directory.GetCurrentDirectory(), "GA_Results");
            Directory.CreateDirectory(path);
            
            File.WriteAllText(Path.Combine(path, $"Generation_{generation}_Report.json"), json);
        }
    }
}