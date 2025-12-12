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
    public class FitnessBreakdown
    {
        public float WinRateScore { get; set; }
        public float VictoryHpScore { get; set; }
        public float GameLengthScore { get; set; }
        public float CardViabilityScore { get; set; }
        public float EliteScore { get; set; }
        public float ConsistencyScore { get; set; }
        public float TotalFitness { get; set; }

        public float WinRate { get; set; }
        public float AvgVictoryHp { get; set; }
        public float AvgFloorOnDeath { get; set; }
        public int ViableCards { get; set; }
        public int TrapCards { get; set; }
        public float EliteKillRate { get; set; }
        public bool IsCriticalFailure { get; set; }
    }
    
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
        private readonly EffectPool _baseEffects;

        public GeneticAlgorithm(SimulationRunner runner, FitnessEvaluator evaluator, CardPool baseCards, EnemyPool baseEnemies, EffectPool baseEffects)
        {
            _runner = runner;
            _evaluator = evaluator;
            _rng = new Random();
            _baseCards = baseCards;
            _baseEnemies = baseEnemies;
            _baseEffects = baseEffects;
        }

        public void Run()
        {
            Stopwatch totalTime = Stopwatch.StartNew();
            List<BalanceGenome> population = InitializePopulation();
            
            // Create logs directory
            string logDir = Path.Combine(Directory.GetCurrentDirectory(), "GA_Logs");
            Directory.CreateDirectory(logDir);
            string logFile = Path.Combine(logDir, $"GA_Run_{DateTime.Now:yyyyMMdd_HHmmss}.log");

            using (StreamWriter log = new StreamWriter(logFile))
            {
                log.WriteLine("=== GENETIC ALGORITHM RUN ===");
                log.WriteLine($"Started: {DateTime.Now}");
                log.WriteLine($"Population Size: {PopulationSize}");
                log.WriteLine($"Generations: {Generations}");
                log.WriteLine($"Runs Per Genome: {RunsPerGenome}");
                log.WriteLine($"Mutation Rate: {MutationRate}");
                log.WriteLine($"Elitism Rate: {ElitismRate}");
                log.WriteLine();
                log.WriteLine("TARGET METRICS:");
                log.WriteLine($"  Win Rate: {_evaluator.TargetWinRate:P0}");
                log.WriteLine($"  Victory HP: {_evaluator.TargetVictoryHpPercent:P0}");
                log.WriteLine($"  Floor on Death: {_evaluator.TargetAvgFloorOnDeath:F1}");
                log.WriteLine();

                for (int gen = 0; gen < Generations; gen++)
                {
                    Stopwatch genTime = Stopwatch.StartNew();
                    Console.WriteLine($"\n===== GENERATION {gen + 1} / {Generations} =====");
                    log.WriteLine($"\n{'=',60}");
                    log.WriteLine($"GENERATION {gen + 1} / {Generations}");
                    log.WriteLine($"{'=',60}");

                    int genomesEvaluated = 0;
                    Console.WriteLine($"Evaluating {PopulationSize} genomes ({RunsPerGenome} runs each)...");

                    // Evaluate with breakdowns
                    var evaluations = population.AsParallel()
                                              .Select(genome =>
                                              {
                                                  var results = EvaluateGenomeWithResults(genome);
                                                  var breakdown = _evaluator.CalculateFitnessWithBreakdown(results);
                                                  
                                                  Interlocked.Increment(ref genomesEvaluated);
                                                  Console.Write($"\rProgress: {genomesEvaluated}/{PopulationSize} genomes evaluated...");
                                                  
                                                  return new { Genome = genome, Breakdown = breakdown, Results = results };
                                              })
                                              .ToList();
                    
                    Console.WriteLine("\nEvaluation complete");

                    // Sort by fitness
                    var rankedPopulation = evaluations
                        .OrderByDescending(x => x.Breakdown.TotalFitness)
                        .ToList();

                    var best = rankedPopulation.First();

                    genTime.Stop();

                    // Log and display results
                    LogGenerationResults(gen, best, log, genTime.Elapsed, rankedPopulation.Cast<dynamic>().ToList());
                    PrintConsoleSummary(gen, best);

                    // Save best genome report
                    SaveBestGenomeReport(best.Genome, best.Results, best.Breakdown, gen);

                    // Create next generation
                    population = CreateNextGeneration(rankedPopulation.Select(x => x.Genome).ToList());
                    
                    log.Flush(); // Ensure log is written after each generation
                }

                totalTime.Stop();
                
                log.WriteLine($"\n\n{'=',60}");
                log.WriteLine("GA RUN COMPLETE");
                log.WriteLine($"{'=',60}");
                log.WriteLine($"Total Time: {totalTime.Elapsed.TotalMinutes:F2} minutes");
                log.WriteLine($"Average Time per Generation: {totalTime.Elapsed.TotalMinutes / Generations:F2} minutes");
            }

            Console.WriteLine($"\n\n{'=',60}");
            Console.WriteLine("GA RUN COMPLETE");
            Console.WriteLine($"{'=',60}");
            Console.WriteLine($"Total Time: {totalTime.Elapsed.TotalMinutes:F2} minutes");
            Console.WriteLine($"Log saved to: {logFile}");
            Console.WriteLine("\nPress any key to exit.");
        }

        private List<SimulationStats> EvaluateGenomeWithResults(BalanceGenome genome)
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
            return results;
        }

        private void LogGenerationResults(int gen, dynamic best, StreamWriter log, TimeSpan elapsed, List<dynamic> rankedPopulation)
        {
            var b = best.Breakdown as FitnessBreakdown;

            log.WriteLine($"Time: {elapsed.TotalSeconds:F2}s");
            log.WriteLine($"Best Fitness: {b.TotalFitness:F2}");
            log.WriteLine();

            // Component breakdown
            log.WriteLine("FITNESS COMPONENTS:");
            log.WriteLine($"  Win Rate:      {b.WinRateScore * _evaluator.WinRateWeight,6:F2} / {_evaluator.WinRateWeight,6:F2}  ({b.WinRateScore,5:P0})  [Actual: {b.WinRate:P1}]");
            log.WriteLine($"  Victory HP:    {b.VictoryHpScore * _evaluator.VictoryHpWeight,6:F2} / {_evaluator.VictoryHpWeight,6:F2}  ({b.VictoryHpScore,5:P0})  [Actual: {b.AvgVictoryHp:P1}]");
            log.WriteLine($"  Game Length:   {b.GameLengthScore * _evaluator.GameLengthWeight,6:F2} / {_evaluator.GameLengthWeight,6:F2}  ({b.GameLengthScore,5:P0})  [Floor: {b.AvgFloorOnDeath:F1}]");
            log.WriteLine($"  Card Viab:     {b.CardViabilityScore * _evaluator.CardViabilityWeight,6:F2} / {_evaluator.CardViabilityWeight,6:F2}  ({b.CardViabilityScore,5:P0})");
            log.WriteLine($"  Elite Perf:    {b.EliteScore * _evaluator.ElitePerformanceWeight,6:F2} / {_evaluator.ElitePerformanceWeight,6:F2}  ({b.EliteScore,5:P0})  [Kill: {b.EliteKillRate:P1}]");
            log.WriteLine($"  Consistency:   {b.ConsistencyScore * _evaluator.ConsistencyWeight,6:F2} / {_evaluator.ConsistencyWeight,6:F2}  ({b.ConsistencyScore,5:P0})");
            log.WriteLine();

            // Card diversity details
            var report = ReportGenerator.Generate(best.Results, b.TotalFitness);
            log.WriteLine("CARD DIVERSITY:");
            log.WriteLine($"  Viable Cards: {b.ViableCards} (>10% pick rate)");
            log.WriteLine($"  Trap Cards: {b.TrapCards}");
            log.WriteLine($"  Build Variety: {report.BuildVarietyScore:F2}");
            log.WriteLine();

            // Quality assessment
            if (b.IsCriticalFailure)
            {
                log.WriteLine("⚠⚠⚠ CRITICAL FAILURE ⚠⚠⚠");
                if (b.WinRate < _evaluator.MinAcceptableWinRate) 
                    log.WriteLine($"  - Win rate too low ({b.WinRate:P1} < {_evaluator.MinAcceptableWinRate:P1})");
                if (b.WinRate > _evaluator.MaxAcceptableWinRate) 
                    log.WriteLine($"  - Win rate too high ({b.WinRate:P1} > {_evaluator.MaxAcceptableWinRate:P1})");
                log.WriteLine();
            }
            else
            {
                // Check for issues
                var issues = new List<string>();
                
                if (b.WinRate < 0.40f) issues.Add($"Win rate low ({b.WinRate:P1})");
                if (b.WinRate > 0.50f) issues.Add($"Win rate high ({b.WinRate:P1})");
                if (b.AvgVictoryHp < 0.25f) issues.Add($"Victory HP low ({b.AvgVictoryHp:P1})");
                if (b.AvgVictoryHp > 0.40f) issues.Add($"Victory HP high ({b.AvgVictoryHp:P1})");
                if (b.ViableCards < 12) issues.Add($"Limited diversity ({b.ViableCards} viable)");
                if (b.TrapCards > 0) issues.Add($"{b.TrapCards} trap card(s)");
                
                if (issues.Any())
                {
                    log.WriteLine("⚠ BALANCE ISSUES:");
                    foreach (var issue in issues)
                    {
                        log.WriteLine($"  - {issue}");
                    }
                    log.WriteLine();
                }
                else
                {
                    log.WriteLine("✓ NO MAJOR ISSUES DETECTED");
                    log.WriteLine();
                }
            }

            // Top 5 cards
            log.WriteLine("TOP 5 MOST PICKED CARDS:");
            foreach (var card in Enumerable.Take(report.CardViability, 5))
            {
                string flag = "";
                if (card.IsTrapCard) flag = " [TRAP]";
                else if (card.IsMustPick) flag = " [MUST]";
                else if (card.IsBalanced) flag = " [BAL]";
                
                log.WriteLine($"  {card.CardId,-20} Pick: {card.PickRate,5:P1}  Win: {card.WinRateWhenPicked,5:P1}{flag}");
            }
            log.WriteLine();

            // Population statistics
            var fitnesses = rankedPopulation.Select(x => (x.Breakdown as FitnessBreakdown).TotalFitness).ToList();
            log.WriteLine("POPULATION STATS:");
            log.WriteLine($"  Best:    {fitnesses.First():F2}");
            log.WriteLine($"  Worst:   {fitnesses.Last():F2}");
            log.WriteLine($"  Average: {fitnesses.Average():F2}");
            log.WriteLine($"  StdDev:  {Math.Sqrt(fitnesses.Select(f => Math.Pow(f - fitnesses.Average(), 2)).Average()):F2}");
            log.WriteLine();
        }

        private void PrintConsoleSummary(int gen, dynamic best)
        {
            var b = best.Breakdown as FitnessBreakdown;

            Console.WriteLine();
            Console.WriteLine($"GENERATION {gen + 1} SUMMARY:");
            Console.WriteLine($"{'─',60}");
            Console.WriteLine($"Total Fitness: {b.TotalFitness:F2}");
            Console.WriteLine();
            
            // Metrics with target comparison
            PrintMetric("Win Rate", b.WinRate, _evaluator.TargetWinRate, 0.40f, 0.50f);
            PrintMetric("Victory HP", b.AvgVictoryHp, _evaluator.TargetVictoryHpPercent, 0.25f, 0.40f);
            PrintMetric("Floor on Death", b.AvgFloorOnDeath, _evaluator.TargetAvgFloorOnDeath, 8f, 12f);
            
            Console.WriteLine($"Viable Cards:  {b.ViableCards}");
            Console.WriteLine($"Trap Cards:    {b.TrapCards}");
            Console.WriteLine();

            // Status indicator
            if (b.IsCriticalFailure)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("⚠⚠⚠ CRITICAL FAILURE - Genome Unplayable");
                Console.ResetColor();
            }
            else if (b.WinRate < 0.40f || b.WinRate > 0.50f || b.ViableCards < 12 || b.TrapCards > 0)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("⚠ Balance Issues Present");
                Console.ResetColor();
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("✓ Good Balance Achieved");
                Console.ResetColor();
            }

            Console.WriteLine($"{'─',60}");
        }

        private void PrintMetric(string name, float actual, float target, float minGood, float maxGood)
        {
            string status;
            ConsoleColor color;

            if (actual >= minGood && actual <= maxGood)
            {
                status = "✓";
                color = ConsoleColor.Green;
            }
            else if (Math.Abs(actual - target) < Math.Abs(minGood - target) * 1.5f)
            {
                status = "~";
                color = ConsoleColor.Yellow;
            }
            else
            {
                status = "✗";
                color = ConsoleColor.Red;
            }

            Console.ForegroundColor = color;
            Console.Write(status);
            Console.ResetColor();
            
            if (name == "Floor on Death")
            {
                Console.WriteLine($" {name,-15} {actual,6:F1}  (Target: {target:F1})");
            }
            else
            {
                Console.WriteLine($" {name,-15} {actual,6:P1}  (Target: {target:P0})");
            }
        }

        private List<BalanceGenome> InitializePopulation()
        {
            var population = new List<BalanceGenome>();
            for (int i = 0; i < PopulationSize; i++)
            {
                var genome = new BalanceGenome();
                genome.Randomize(_baseCards, _baseEnemies, _baseEffects, _rng);
                population.Add(genome);
            }
            return population;
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

                var child = Crossover(parent1, parent2);
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

        private BalanceGenome Crossover(BalanceGenome parent1, BalanceGenome parent2)
        {
            var child = new BalanceGenome();
            
            lock(_rngLock)
            {
                child.GoldDropMultiplier = (_rng.NextDouble() < 0.5) ? parent1.GoldDropMultiplier : parent2.GoldDropMultiplier;
                for (int i = 1; i <= 5; i++)
                {
                    child.ShopPriceScalars[i] = (_rng.NextDouble() < 0.5) ? parent1.ShopPriceScalars[i] : parent2.ShopPriceScalars[i];
                }

                child.HeroHealthScalar = (_rng.NextDouble() < 0.5) ? parent1.HeroHealthScalar : parent2.HeroHealthScalar;
                child.HeroStartGoldScalar = (_rng.NextDouble() < 0.5) ? parent1.HeroStartGoldScalar : parent2.HeroStartGoldScalar;
                child.HeroManaOffset = (_rng.NextDouble() < 0.5) ? parent1.HeroManaOffset : parent2.HeroManaOffset;

                foreach (var key in parent1.CardCostModifiers.Keys)
                {
                    child.CardCostModifiers[key] = (_rng.NextDouble() < 0.5) ? parent1.CardCostModifiers[key] : parent2.CardCostModifiers[key];
                }
                
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
                
                foreach (var key in parent1.EnemyHealthScalars.Keys)
                {
                    child.EnemyHealthScalars[key] = (_rng.NextDouble() < 0.5) ? parent1.EnemyHealthScalars[key] : parent2.EnemyHealthScalars[key];
                }
                
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
                
                foreach (var key in parent1.EffectValueScalars.Keys)
                {
                    child.EffectValueScalars[key] = (_rng.NextDouble() < 0.5) ? parent1.EffectValueScalars[key] : parent2.EffectValueScalars[key];
                }
            }
            return child;
        }

        private void Mutate(BalanceGenome genome)
        {
            lock (_rngLock)
            {
                if (_rng.NextDouble() < MutationRate)
                    genome.GoldDropMultiplier += (float)(_rng.NextDouble() * 0.2 - 0.1);
                genome.GoldDropMultiplier = Math.Clamp(genome.GoldDropMultiplier, 0.1f, 1.0f);

                for (int i = 1; i <= 5; i++)
                {
                    if (_rng.NextDouble() < MutationRate)
                        genome.ShopPriceScalars[i] += (float)(_rng.NextDouble() * 0.2 - 0.1);
                    genome.ShopPriceScalars[i] = Math.Clamp(genome.ShopPriceScalars[i], 0.5f, 2.0f);
                }

                if (_rng.NextDouble() < MutationRate)
                    genome.HeroHealthScalar += (float)(_rng.NextDouble() * 0.1 - 0.05);
                genome.HeroHealthScalar = Math.Clamp(genome.HeroHealthScalar, 0.8f, 1.2f);

                if (_rng.NextDouble() < MutationRate)
                    genome.HeroStartGoldScalar += (float)(_rng.NextDouble() * 0.2 - 0.1);
                genome.HeroStartGoldScalar = Math.Clamp(genome.HeroStartGoldScalar, 0.7f, 1.5f);

                if (_rng.NextDouble() < (MutationRate * 0.1))
                    genome.HeroManaOffset += _rng.Next(3) - 1;
                genome.HeroManaOffset = Math.Clamp(genome.HeroManaOffset, -1, 1);

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
                
                foreach (var key in genome.EffectValueScalars.Keys.ToList())
                {
                    if (_rng.NextDouble() < MutationRate)
                        genome.EffectValueScalars[key] += (float)(_rng.NextDouble() * 0.2 - 0.1);
                    genome.EffectValueScalars[key] = Math.Clamp(genome.EffectValueScalars[key], 0.5f, 2.0f);
                }
            }
        }

        private void SaveBestGenomeReport(BalanceGenome genome, List<SimulationStats> evaluationResults, FitnessBreakdown breakdown, int generation)
        {
            // Run additional simulations for detailed report
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

            var report = ReportGenerator.Generate(reportResults, breakdown.TotalFitness);
            
            var output = new
            {
                Generation = generation,
                Fitness = breakdown.TotalFitness,
                FitnessComponents = new
                {
                    WinRate = new 
                    { 
                        Score = breakdown.WinRateScore * _evaluator.WinRateWeight, 
                        MaxScore = _evaluator.WinRateWeight,
                        Percentage = breakdown.WinRateScore,
                        Actual = breakdown.WinRate,
                        Target = _evaluator.TargetWinRate
                    },
                    VictoryHP = new 
                    { 
                        Score = breakdown.VictoryHpScore * _evaluator.VictoryHpWeight, 
                        MaxScore = _evaluator.VictoryHpWeight,
                        Percentage = breakdown.VictoryHpScore,
                        Actual = breakdown.AvgVictoryHp,
                        Target = _evaluator.TargetVictoryHpPercent
                    },
                    GameLength = new 
                    { 
                        Score = breakdown.GameLengthScore * _evaluator.GameLengthWeight, 
                        MaxScore = _evaluator.GameLengthWeight,
                        Percentage = breakdown.GameLengthScore,
                        Actual = breakdown.AvgFloorOnDeath,
                        Target = _evaluator.TargetAvgFloorOnDeath
                    },
                    CardViability = new 
                    { 
                        Score = breakdown.CardViabilityScore * _evaluator.CardViabilityWeight, 
                        MaxScore = _evaluator.CardViabilityWeight,
                        Percentage = breakdown.CardViabilityScore,
                        ViableCards = breakdown.ViableCards,
                        TrapCards = breakdown.TrapCards
                    },
                    ElitePerformance = new 
                    { 
                        Score = breakdown.EliteScore * _evaluator.ElitePerformanceWeight, 
                        MaxScore = _evaluator.ElitePerformanceWeight,
                        Percentage = breakdown.EliteScore,
                        KillRate = breakdown.EliteKillRate
                    },
                    Consistency = new 
                    { 
                        Score = breakdown.ConsistencyScore * _evaluator.ConsistencyWeight, 
                        MaxScore = _evaluator.ConsistencyWeight,
                        Percentage = breakdown.ConsistencyScore
                    }
                },
                IsCriticalFailure = breakdown.IsCriticalFailure,
                Genome = genome,
                Report = report
            };

            string json = JsonConvert.SerializeObject(output, Formatting.Indented);
            
            string path = Path.Combine(Directory.GetCurrentDirectory(), "GA_Results");
            Directory.CreateDirectory(path);
            
            File.WriteAllText(Path.Combine(path, $"Generation_{generation:D3}_Report.json"), json);
        }
    }
}