using Roguelike.Core.AI;
using Roguelike.Data;
using Roguelike.Core.Map;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Newtonsoft.Json;

namespace Roguelike.Optimization
{
    /// <summary>
    /// Improved genetic algorithm using:
    /// - Hierarchical genome (50 params instead of 500)
    /// - Multi-objective optimization (NSGA-II)
    /// - Adaptive evaluation budget
    /// - Better genetic operators
    /// </summary>
    public class ImprovedGeneticAlgorithm
    {
        // Configuration
        public int PopulationSize { get; set; } = 100;
        public int Generations { get; set; } = 30;
        public float MutationRate { get; set; } = 0.15f;
        
        // Components
        private readonly NSGA2Optimizer _optimizer;
        private readonly AdaptiveEvaluator _evaluator;
        private readonly ConvergenceDetector _convergence;
        private readonly Random _rng;
        
        // Logging
        private string _logDirectory;
        private StreamWriter _logWriter;

        public ImprovedGeneticAlgorithm(
            ISimulationRunner runner,
            MultiObjectiveEvaluator moEvaluator,
            Random rng,
            CardPool cardPool = null,
            EnemyPool enemyPool = null)
        {
            _rng = rng;
            _optimizer = new NSGA2Optimizer(rng, cardPool, enemyPool) 
            { 
                PopulationSize = PopulationSize,
                MutationRate = MutationRate 
            };
            _evaluator = new AdaptiveEvaluator(runner, moEvaluator, rng);
            _evaluator.TotalGenerations = Generations;  // Set for progressive threshold tightening
            _convergence = new ConvergenceDetector
            {
                MinGenerations = 10,
                PatienceGenerations = 5,
                ImprovementThreshold = 0.01
            };
        }

        public void Run()
        {
            SetupLogging();
            var totalTime = Stopwatch.StartNew();
            
            LogHeader();
            
            // Initialize population
            Console.WriteLine("Initializing population...");
            var population = _optimizer.InitializePopulation();
            
            for (int gen = 0; gen < Generations; gen++)
            {
                var genTime = Stopwatch.StartNew();
                Console.WriteLine($"\n{'=',60}");
                Console.WriteLine($"GENERATION {gen + 1} / {Generations}");
                Console.WriteLine($"{'=',60}");
                
                // Evaluate population
                Console.WriteLine($"Evaluating {population.Count} individuals...");
                EvaluatePopulation(population, gen);
                
                // Perform non-dominated sorting and get Pareto front
                var fronts = _optimizer.FastNonDominatedSort(population);
                var paretoFront = fronts[0];
                
                genTime.Stop();
                
                // Log and display results
                LogGeneration(gen, paretoFront, fronts, genTime.Elapsed);
                DisplayGenerationSummary(gen, paretoFront);
                
                // Check for convergence
                var paretoFitnesses = paretoFront.Select(i => i.Fitness).ToList();
                if (_convergence.HasConverged(paretoFitnesses, gen))
                {
                    Console.WriteLine($"\n*** CONVERGED at generation {gen + 1} ***");
                    Console.WriteLine("No significant improvement detected. Stopping early.");
                    
                    var stats = _convergence.GetStatistics();
                    _logWriter.WriteLine($"\n*** EARLY STOPPING ***");
                    _logWriter.WriteLine($"Converged at generation {gen + 1}");
                    _logWriter.WriteLine($"Current Hypervolume: {stats.currentHypervolume:F4}");
                    _logWriter.WriteLine($"Best Hypervolume: {stats.bestHypervolume:F4}");
                    _logWriter.WriteLine($"Generations without improvement: {stats.noImprovementCount}");
                    _logWriter.Flush();
                    break;
                }
                
                // Save Pareto front solutions
                SaveParetoFront(gen, paretoFront);
                
                // Create next generation
                if (gen < Generations - 1)
                {
                    // Create offspring (unevaluated)
                    var offspring = _optimizer.EvolveGeneration(population);
                    
                    // Combine current population with offspring
                    var combined = population.Concat(offspring).ToList();
                    
                    // Evaluate offspring only (parents already evaluated)
                    Console.WriteLine($"\nEvaluating {offspring.Count} offspring...");
                    EvaluatePopulation(offspring, gen + 1);
                    
                    // Perform NSGA-II selection on combined population
                    var combinedFronts = _optimizer.FastNonDominatedSort(combined);
                    
                    // Calculate crowding distance
                    foreach (var front in combinedFronts)
                    {
                        _optimizer.CalculateCrowdingDistance(front);
                    }
                    
                    // Select next generation (elitism + diversity)
                    population = new List<Individual>();
                    foreach (var front in combinedFronts)
                    {
                        if (population.Count + front.Count <= PopulationSize)
                        {
                            population.AddRange(front);
                        }
                        else
                        {
                            int remaining = PopulationSize - population.Count;
                            var sorted = front.OrderByDescending(ind => ind.Fitness.CrowdingDistance)
                                             .Take(remaining);
                            population.AddRange(sorted);
                            break;
                        }
                    }
                }
            }
            
            totalTime.Stop();
            LogFooter(totalTime.Elapsed);
            _logWriter?.Close();
            
            Console.WriteLine($"\n\n{'=',60}");
            Console.WriteLine("OPTIMIZATION COMPLETE");
            Console.WriteLine($"{'=',60}");
            Console.WriteLine($"Total Time: {totalTime.Elapsed.TotalMinutes:F2} minutes");
            Console.WriteLine($"Results saved to: {_logDirectory}");
        }

        private void EvaluatePopulation(List<Individual> population, int generation)
        {
            int evaluated = 0;
            
            _evaluator.EvaluatePopulation(
                population,
                generation,
                (current, total) => 
                {
                    Console.Write($"\rProgress: {current}/{total} individuals evaluated");
                });
            
            Console.WriteLine("\nEvaluation complete");
        }

        private void LogGeneration(
            int gen, 
            List<Individual> paretoFront, 
            List<List<Individual>> allFronts,
            TimeSpan elapsed)
        {
            _logWriter.WriteLine($"\n{'=',60}");
            _logWriter.WriteLine($"GENERATION {gen + 1}");
            _logWriter.WriteLine($"{'=',60}");
            _logWriter.WriteLine($"Time: {elapsed.TotalSeconds:F2}s");
            _logWriter.WriteLine($"Pareto Front Size: {paretoFront.Count}");
            _logWriter.WriteLine();
            
            // Log Pareto front solutions
            _logWriter.WriteLine("PARETO FRONT SOLUTIONS:");
            for (int i = 0; i < Math.Min(5, paretoFront.Count); i++)
            {
                var ind = paretoFront.OrderByDescending(x => x.Fitness.BalanceScore).ElementAt(i);
                _logWriter.WriteLine($"\nSolution {i + 1}:");
                _logWriter.WriteLine($"  Balance:    {ind.Fitness.BalanceScore:F3}");
                _logWriter.WriteLine($"  Engagement: {ind.Fitness.EngagementScore:F3}");
                _logWriter.WriteLine($"  Coherence:  {ind.Fitness.CoherenceScore:F3}");
                _logWriter.WriteLine($"  Win Rate:   {ind.Fitness.WinRate:P1}");
                _logWriter.WriteLine($"  Victory HP: {ind.Fitness.VictoryHp:P1}");
                _logWriter.WriteLine($"  Viable Cards: {ind.Fitness.ViableCards}");
                _logWriter.WriteLine($"  Trap Cards: {ind.Fitness.TrapCards}");
                _logWriter.WriteLine($"  Crowding Distance: {ind.Fitness.CrowdingDistance:F3}");
            }
            
            // Population diversity metrics
            _logWriter.WriteLine($"\n\nPOPULATION STATISTICS:");
            _logWriter.WriteLine($"  Total Fronts: {allFronts.Count}");
            _logWriter.WriteLine($"  Front Sizes: {string.Join(", ", allFronts.Take(5).Select(f => f.Count))}");
            
            var allBalanceScores = allFronts.SelectMany(f => f).Select(i => i.Fitness.BalanceScore).ToList();
            _logWriter.WriteLine($"  Balance Score Range: [{allBalanceScores.Min():F3}, {allBalanceScores.Max():F3}]");
            _logWriter.WriteLine($"  Balance Score Avg: {allBalanceScores.Average():F3}");
            
            // Diversity metrics
            var allIndividuals = allFronts.SelectMany(f => f).ToList();
            float genotypeDiversity = DiversityMaintenance.CalculateGenotypeDiversity(allIndividuals);
            float phenotypeDiversity = DiversityMaintenance.CalculatePhenotypeDiversity(allIndividuals);
            _logWriter.WriteLine($"\nDIVERSITY METRICS:");
            _logWriter.WriteLine($"  Genotype Diversity (parameter space): {genotypeDiversity:F4}");
            _logWriter.WriteLine($"  Phenotype Diversity (objective space): {phenotypeDiversity:F4}");
            
            _logWriter.Flush();
        }

        private void DisplayGenerationSummary(int gen, List<Individual> paretoFront)
        {
            Console.WriteLine($"\nGENERATION {gen + 1} SUMMARY:");
            Console.WriteLine($"{'─',60}");
            Console.WriteLine($"Pareto Front Size: {paretoFront.Count}");
            
            // Show best solution for each objective
            var bestBalance = paretoFront.OrderByDescending(i => i.Fitness.BalanceScore).First();
            var bestEngagement = paretoFront.OrderByDescending(i => i.Fitness.EngagementScore).First();
            var bestCoherence = paretoFront.OrderByDescending(i => i.Fitness.CoherenceScore).First();
            
            Console.WriteLine("\nBest for Balance:");
            Console.WriteLine($"  {FormatSolution(bestBalance)}");
            
            Console.WriteLine("\nBest for Engagement:");
            Console.WriteLine($"  {FormatSolution(bestEngagement)}");
            
            Console.WriteLine("\nBest for Coherence:");
            Console.WriteLine($"  {FormatSolution(bestCoherence)}");
            
            Console.WriteLine($"{'─',60}");
        }

        private string FormatSolution(Individual ind)
        {
            return $"B:{ind.Fitness.BalanceScore:F2} " +
                   $"E:{ind.Fitness.EngagementScore:F2} " +
                   $"C:{ind.Fitness.CoherenceScore:F2} | " +
                   $"WR:{ind.Fitness.WinRate:P0} " +
                   $"Cards:{ind.Fitness.ViableCards} " +
                   $"Traps:{ind.Fitness.TrapCards}";
        }

        private void SaveParetoFront(int gen, List<Individual> paretoFront)
        {
            var frontData = paretoFront.Select((ind, idx) => new
            {
                Index = idx,
                Generation = gen,
                Fitness = ind.Fitness,
                Genome = ind.Genome
            }).ToList();
            
            string json = JsonConvert.SerializeObject(frontData, Formatting.Indented);
            string filename = Path.Combine(_logDirectory, $"Gen{gen:D3}_ParetoFront.json");
            File.WriteAllText(filename, json);
            
            // Also save the "best compromise" solution
            var bestCompromise = paretoFront
                .OrderByDescending(i => i.Fitness.BalanceScore + 
                                       i.Fitness.EngagementScore + 
                                       i.Fitness.CoherenceScore)
                .First();
            
            var compromiseData = new
            {
                Generation = gen,
                Description = "Best compromise solution (highest sum of objectives)",
                Fitness = bestCompromise.Fitness,
                Genome = bestCompromise.Genome
            };
            
            string compromiseJson = JsonConvert.SerializeObject(compromiseData, Formatting.Indented);
            string compromiseFile = Path.Combine(_logDirectory, $"Gen{gen:D3}_BestCompromise.json");
            File.WriteAllText(compromiseFile, compromiseJson);
        }

        private void SetupLogging()
        {
            _logDirectory = Path.Combine(Directory.GetCurrentDirectory(), 
                                        $"GA_Improved_{DateTime.Now:yyyyMMdd_HHmmss}");
            Directory.CreateDirectory(_logDirectory);
            
            string logFile = Path.Combine(_logDirectory, "optimization_log.txt");
            _logWriter = new StreamWriter(logFile);
        }

        private void LogHeader()
        {
            _logWriter.WriteLine("=== IMPROVED GENETIC ALGORITHM ===");
            _logWriter.WriteLine($"Started: {DateTime.Now}");
            _logWriter.WriteLine($"Population Size: {PopulationSize}");
            _logWriter.WriteLine($"Generations: {Generations}");
            _logWriter.WriteLine($"Mutation Rate: {MutationRate}");
            _logWriter.WriteLine();
            _logWriter.WriteLine("IMPROVEMENTS:");
            _logWriter.WriteLine("  - Hierarchical genome (~50 params instead of ~500)");
            _logWriter.WriteLine("  - Multi-objective optimization (NSGA-II)");
            _logWriter.WriteLine("  - Adaptive evaluation budget");
            _logWriter.WriteLine("  - Improved genetic operators (SBX, polynomial mutation)");
            _logWriter.WriteLine();
            _logWriter.Flush();
        }

        private void LogFooter(TimeSpan totalTime)
        {
            _logWriter.WriteLine($"\n\n{'=',60}");
            _logWriter.WriteLine("OPTIMIZATION COMPLETE");
            _logWriter.WriteLine($"{'=',60}");
            _logWriter.WriteLine($"Total Time: {totalTime.TotalMinutes:F2} minutes");
            _logWriter.WriteLine($"Average per Generation: {totalTime.TotalMinutes / Generations:F2} minutes");
            _logWriter.Flush();
        }
    }
}
