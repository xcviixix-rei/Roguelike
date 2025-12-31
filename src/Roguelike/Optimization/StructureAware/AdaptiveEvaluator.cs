using System;
using System.Collections.Generic;
using System.Linq;

namespace Roguelike.Optimization
{
    /// <summary>
    /// Adaptive evaluator that allocates more simulation budget to promising candidates
    /// and early-stops clearly bad solutions.
    /// </summary>
    public class AdaptiveEvaluator
    {
        private readonly ISimulationRunner _runner;
        private readonly MultiObjectiveEvaluator _evaluator;
        private readonly Random _rng;
        
        // Evaluation phases
        // Phase 1: Quick screening with sufficient sample size for statistical confidence
        // 75 runs gives 95% CI of ±11% for 45% win rate (better than 50 runs at ±14%)
        public int Phase1Runs { get; set; } = 75;    // Quick screening
        public int Phase2Runs { get; set; } = 25;    // Standard eval (total 100)
        public int Phase3Runs { get; set; } = 100;   // Detailed eval for promising candidates (total 200)
        
        // Progressive threshold tightening
        // Total generations for calculating progression (set from GA)
        public int TotalGenerations { get; set; } = 30;
        
        // Early stopping thresholds (will be tightened progressively)
        // These are the FINAL (tightest) thresholds
        public float MinAcceptableWinRate { get; set; } = 0.30f;
        public float MaxAcceptableWinRate { get; set; } = 0.60f;

        public AdaptiveEvaluator(ISimulationRunner runner, MultiObjectiveEvaluator evaluator, Random rng)
        {
            _runner = runner;
            _evaluator = evaluator;
            _rng = rng;
        }
        
        /// <summary>
        /// Gets acceptable win rate range based on generation progress
        /// Progressive tightening: wide early exploration → tight late refinement
        /// </summary>
        private (float min, float max) GetAcceptableWinRateRange(int generation)
        {
            float progress = (float)generation / Math.Max(1, TotalGenerations);
            
            // Early phase (first 1/3): Wide exploration
            if (progress < 0.33f)
                return (0.15f, 0.75f);
            
            // Middle phase (1/3 to 2/3): Medium exploitation
            if (progress < 0.67f)
                return (0.25f, 0.65f);
            
            // Late phase (final 1/3): Tight refinement
            return (MinAcceptableWinRate, MaxAcceptableWinRate);
        }

        /// <summary>
        /// Evaluates a genome with adaptive budget allocation
        /// </summary>
        public (MultiObjectiveFitness fitness, List<SimulationStats> results) Evaluate(
            HierarchicalGenome genome, 
            bool isElite = false,
            int generation = 0)
        {
            var results = new List<SimulationStats>();
            
            // Quick Screening
            for (int i = 0; i < Phase1Runs; i++)
            {
                results.Add(RunSimulation(genome));
            }
            
            float phase1WinRate = (float)results.Count(r => r.IsVictory) / results.Count;
            
            // Progressive threshold tightening based on generation
            var (minWR, maxWR) = GetAcceptableWinRateRange(generation);
            
            if (phase1WinRate < minWR || phase1WinRate > maxWR)
            {
                var fitness = _evaluator.Evaluate(results);
                fitness.IsFeasible = false;
                return (fitness, results);
            }
            
            // Standard Evaluation
            for (int i = 0; i < Phase2Runs; i++)
            {
                results.Add(RunSimulation(genome));
            }
            
            bool isPromising = IsPromising(results);
            
            if (isElite || isPromising)
            {
                // Detailed Evaluation
                for (int i = 0; i < Phase3Runs; i++)
                {
                    results.Add(RunSimulation(genome));
                }
            }
            
            var finalFitness = _evaluator.Evaluate(results);
            return (finalFitness, results);
        }

        /// <summary>
        /// Determines if a solution is promising based on partial evaluation
        /// Multi-objective approach: promising if above-average in ANY objective
        /// </summary>
        private bool IsPromising(List<SimulationStats> results)
        {
            float winRate = (float)results.Count(r => r.IsVictory) / results.Count;
            
            // Calculate card diversity
            var uniqueCards = results
                .SelectMany(r => r.MasterDeckIds)
                .Where(id => id != "strike" && id != "defend" && id != "quick_jab" && id != "cycle")
                .Distinct()
                .Count();
            
            // Estimate trap cards (lightweight version)
            var (_, trapCards) = EstimateCardMetrics(results);
            
            // Score across all objectives (0-1 normalized)
            float balanceScore = 1.0f - Math.Abs(winRate - 0.45f) / 0.45f;
            float engagementScore = Math.Min(1.0f, uniqueCards / 20.0f);
            float coherenceScore = Math.Max(0f, 1.0f - (trapCards / 5.0f));
            
            // Promising if above-average in ANY objective
            return (balanceScore > 0.7f || engagementScore > 0.7f || coherenceScore > 0.8f);
        }
        
        /// <summary>
        /// Lightweight card metrics estimation for promising detection
        /// </summary>
        private (int viableCards, int trapCards) EstimateCardMetrics(List<SimulationStats> results)
        {
            var pickCounts = new Dictionary<string, int>();
            var pickWins = new Dictionary<string, int>();
            
            foreach (var run in results)
            {
                var pickedCards = new HashSet<string>(
                    run.MasterDeckIds.Where(id => id != "strike" && id != "defend" && id != "quick_jab" && id != "cycle")
                );
                
                foreach (var cardId in pickedCards)
                {
                    if (!pickCounts.ContainsKey(cardId))
                    {
                        pickCounts[cardId] = 0;
                        pickWins[cardId] = 0;
                    }
                    pickCounts[cardId]++;
                    if (run.IsVictory) pickWins[cardId]++;
                }
            }
            
            int viable = pickCounts.Count(kv => (float)kv.Value / results.Count >= 0.10f);
            int traps = 0;
            
            const int MIN_PICKS = 10;
            foreach (var cardId in pickCounts.Keys)
            {
                int picks = pickCounts[cardId];
                if (picks < MIN_PICKS) continue;
                
                float pickRate = (float)picks / results.Count;
                float winRate = (float)pickWins[cardId] / picks;
                if (pickRate > 0.10f && winRate < 0.30f)
                    traps++;
            }
            
            return (viable, traps);
        }

        private SimulationStats RunSimulation(HierarchicalGenome genome)
        {
            int seed;
            lock (_rng)
            {
                seed = _rng.Next();
            }
            return _runner.Run(genome, seed);
        }

        /// <summary>
        /// Batch evaluate population with progress tracking
        /// </summary>
        public void EvaluatePopulation(
            List<Individual> population, 
            int generation = 0,
            Action<int, int> progressCallback = null)
        {
            int evaluated = 0;
            
            // Identify elites
            var elites = population.Where(ind => ind.Fitness?.Rank == 1).ToList();
            var eliteIds = new HashSet<Individual>(elites);
            
            foreach (var individual in population)
            {
                if (individual.Fitness != null)
                {
                    // Already evaluated
                    evaluated++;
                    progressCallback?.Invoke(evaluated, population.Count);
                    continue;
                }
                
                bool isElite = eliteIds.Contains(individual);
                var (fitness, results) = Evaluate(individual.Genome, isElite, generation);
                individual.Fitness = fitness;
                
                evaluated++;
                progressCallback?.Invoke(evaluated, population.Count);
            }
        }

        /// <summary>
        /// Statistical confidence estimation for win rate
        /// </summary>
        public (float lowerBound, float upperBound) GetWinRateConfidenceInterval(
            List<SimulationStats> results, 
            float confidence = 0.95f)
        {
            int n = results.Count;
            int wins = results.Count(r => r.IsVictory);
            float p = (float)wins / n;
            
            // Wilson score interval
            float z = confidence == 0.95f ? 1.96f : 2.576f; // 95% or 99%
            
            float denominator = 1 + z * z / n;
            float center = p + z * z / (2 * n);
            float spread = z * (float)Math.Sqrt(p * (1 - p) / n + z * z / (4 * n * n));
            
            float lower = (center - spread) / denominator;
            float upper = (center + spread) / denominator;
            
            return (Math.Max(0, lower), Math.Min(1, upper));
        }
    }
}
