using System;
using System.Collections.Generic;
using System.Linq;

namespace Roguelike.Optimization
{
    /// <summary>
    /// Detects convergence in NSGA-II evolution based on Pareto front metrics.
    /// Enables early stopping when no significant improvement is observed.
    /// </summary>
    public class ConvergenceDetector
    {
        private readonly List<double> _hypervolumeHistory = new();
        private int _noImprovementCount = 0;
        
        /// <summary>
        /// Minimum generations before convergence can be detected
        /// </summary>
        public int MinGenerations { get; set; } = 10;
        
        /// <summary>
        /// Number of consecutive generations with no improvement to declare convergence
        /// </summary>
        public int PatienceGenerations { get; set; } = 5;
        
        /// <summary>
        /// Minimum improvement threshold (as fraction of hypervolume)
        /// </summary>
        public double ImprovementThreshold { get; set; } = 0.01; // 1%
        
        /// <summary>
        /// Checks if the evolution has converged
        /// </summary>
        public bool HasConverged(List<MultiObjectiveFitness> paretoFront, int generation)
        {
            // Need minimum generations before checking convergence
            if (generation < MinGenerations)
                return false;
            
            // Calculate hypervolume (area dominated by Pareto front)
            double hypervolume = CalculateHypervolume(paretoFront);
            _hypervolumeHistory.Add(hypervolume);
            
            // Check for improvement compared to recent history
            if (generation >= MinGenerations)
            {
                // Compare recent average to previous average
                int windowSize = Math.Min(5, _hypervolumeHistory.Count / 2);
                
                // Ensure we have enough data for both windows
                if (_hypervolumeHistory.Count >= 2 * windowSize && windowSize > 0)
                {
                    double recentAvg = _hypervolumeHistory.Skip(_hypervolumeHistory.Count - windowSize).Take(windowSize).Average();
                    double previousAvg = _hypervolumeHistory.Skip(_hypervolumeHistory.Count - 2 * windowSize).Take(windowSize).Average();
                
                    double improvement = (recentAvg - previousAvg) / Math.Max(previousAvg, 1e-6);
                    
                    if (improvement < ImprovementThreshold)
                    {
                        _noImprovementCount++;
                    }
                    else
                    {
                        _noImprovementCount = 0;
                    }
                }
            }
            
            // Converged if no improvement for patience generations
            return _noImprovementCount >= PatienceGenerations;
        }
        
        /// <summary>
        /// Calculates hypervolume indicator with reference point
        /// Reference point represents worst acceptable values for each objective
        /// </summary>
        private double CalculateHypervolume(List<MultiObjectiveFitness> paretoFront)
        {
            if (paretoFront == null || !paretoFront.Any())
                return 0.0;
            
            // Reference point (worst acceptable values for each objective)
            const float refBalance = 0.3f;
            const float refEngagement = 0.3f;
            const float refCoherence = 0.3f;
            
            double volume = 0.0;
            
            foreach (var fitness in paretoFront)
            {
                // Calculate dominated hypervolume relative to reference point
                double contribution = 
                    Math.Max(0, fitness.BalanceScore - refBalance) *
                    Math.Max(0, fitness.EngagementScore - refEngagement) *
                    Math.Max(0, fitness.CoherenceScore - refCoherence);
                
                volume += contribution;
            }
            
            return volume;
        }
        
        /// <summary>
        /// Gets convergence statistics for reporting
        /// </summary>
        public (double currentHypervolume, double bestHypervolume, int noImprovementCount) GetStatistics()
        {
            double current = _hypervolumeHistory.Any() ? _hypervolumeHistory.Last() : 0.0;
            double best = _hypervolumeHistory.Any() ? _hypervolumeHistory.Max() : 0.0;
            return (current, best, _noImprovementCount);
        }
        
        /// <summary>
        /// Resets the convergence detector for a new run
        /// </summary>
        public void Reset()
        {
            _hypervolumeHistory.Clear();
            _noImprovementCount = 0;
        }
    }
}
