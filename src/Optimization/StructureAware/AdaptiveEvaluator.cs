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
        public int Phase1Runs { get; set; } = 30;    // Quick screening
        public int Phase2Runs { get; set; } = 70;    // Standard eval
        public int Phase3Runs { get; set; } = 100;   // Detailed eval for promising candidates
        
        // Early stopping thresholds
        public float MinAcceptableWinRate { get; set; } = 0.20f;
        public float MaxAcceptableWinRate { get; set; } = 0.70f;

        public AdaptiveEvaluator(ISimulationRunner runner, MultiObjectiveEvaluator evaluator, Random rng)
        {
            _runner = runner;
            _evaluator = evaluator;
            _rng = rng;
        }

        /// <summary>
        /// Evaluates a genome with adaptive budget allocation
        /// </summary>
        public (MultiObjectiveFitness fitness, List<SimulationStats> results) Evaluate(
            HierarchicalGenome genome, 
            bool isElite = false)
        {
            var results = new List<SimulationStats>();
            
            // Quick Screening
            for (int i = 0; i < Phase1Runs; i++)
            {
                results.Add(RunSimulation(genome));
            }
            
            float phase1WinRate = (float)results.Count(r => r.IsVictory) / results.Count;
            
            if (phase1WinRate < MinAcceptableWinRate || phase1WinRate > MaxAcceptableWinRate)
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
        /// </summary>
        private bool IsPromising(List<SimulationStats> results)
        {
            float winRate = (float)results.Count(r => r.IsVictory) / results.Count;
            
            // Close to target win rate
            if (Math.Abs(winRate - 0.45f) < 0.10f)
                return true;
            
            // High card diversity
            var uniqueCards = results
                .SelectMany(r => r.MasterDeckIds)
                .Where(id => id != "strike" && id != "defend" && id != "quick_jab" && id != "cycle")
                .Distinct()
                .Count();
            
            if (uniqueCards >= 15)
                return true;
            
            return false;
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
                var (fitness, results) = Evaluate(individual.Genome, isElite);
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
