using System;
using System.Collections.Generic;
using System.Linq;

namespace Roguelike.Optimization
{
    /// <summary>
    /// Multi-objective fitness for Pareto optimization.
    /// Instead of combining metrics into a single score, we find the trade-off frontier.
    /// </summary>
    public class MultiObjectiveFitness
    {
        // Balance Quality
        // How close are we to target win rate, victory HP, etc.
        public float BalanceScore { get; set; }
        
        // Player Engagement
        // Card diversity, build variety, meaningful decisions
        public float EngagementScore { get; set; }
        
        // Design Coherence
        // No trap cards, consistent difficulty curve, economy balance
        public float CoherenceScore { get; set; }
        
        // Additional Metrics (for reporting)
        public float WinRate { get; set; }
        public float VictoryHp { get; set; }
        public float AvgFloorOnDeath { get; set; }
        public int ViableCards { get; set; }
        public int TrapCards { get; set; }
        public float BuildVariety { get; set; }
        
        // Pareto Ranking
        public int Rank { get; set; }  // Pareto front rank (1 = non-dominated)
        public float CrowdingDistance { get; set; }  // Diversity measure within rank
        
        // Constraint Violations
        public bool IsFeasible { get; set; } = true;
        public int ConstraintViolations { get; set; } = 0;

        /// <summary>
        /// Checks if this solution dominates another (is better in all objectives)
        /// Uses constraint-domination: feasible always dominates infeasible
        /// </summary>
        public bool Dominates(MultiObjectiveFitness other)
        {
            // Constraint-domination: feasible always dominates infeasible
            if (this.IsFeasible && !other.IsFeasible) return true;
            if (!this.IsFeasible && other.IsFeasible) return false;
            
            // Both infeasible: compare constraint violations
            if (!this.IsFeasible && !other.IsFeasible)
                return this.ConstraintViolations < other.ConstraintViolations;
            
            // Both feasible: normal Pareto domination
            bool atLeastOneBetter = false;
            
            // Must be >= in all objectives
            if (BalanceScore < other.BalanceScore) return false;
            if (EngagementScore < other.EngagementScore) return false;
            if (CoherenceScore < other.CoherenceScore) return false;
            
            // And strictly better in at least one
            if (BalanceScore > other.BalanceScore) atLeastOneBetter = true;
            if (EngagementScore > other.EngagementScore) atLeastOneBetter = true;
            if (CoherenceScore > other.CoherenceScore) atLeastOneBetter = true;
            
            return atLeastOneBetter;
        }

        /// <summary>
        /// Calculate Euclidean distance in objective space (for diversity)
        /// </summary>
        public float DistanceTo(MultiObjectiveFitness other)
        {
            float dBalance = BalanceScore - other.BalanceScore;
            float dEngagement = EngagementScore - other.EngagementScore;
            float dCoherence = CoherenceScore - other.CoherenceScore;
            
            return (float)Math.Sqrt(dBalance * dBalance + 
                                   dEngagement * dEngagement + 
                                   dCoherence * dCoherence);
        }

        public override string ToString()
        {
            return $"[Rank {Rank}] Balance: {BalanceScore:F2}, " +
                   $"Engagement: {EngagementScore:F2}, " +
                   $"Coherence: {CoherenceScore:F2} " +
                   $"(WR: {WinRate:P1}, Cards: {ViableCards}, Traps: {TrapCards})";
        }
    }

    /// <summary>
    /// Multi-objective evaluator that calculates the 3 fitness objectives
    /// </summary>
    public class MultiObjectiveEvaluator
    {
        // Target metrics
        public float TargetWinRate = 0.45f;
        public float TargetVictoryHp = 0.30f;
        public float TargetAvgFloorOnDeath = 10.0f;
        
        // Constraint thresholds
        public float MinAcceptableWinRate = 0.25f;
        public float MaxAcceptableWinRate = 0.65f;
        public int MinViableCards = 10;
        public int MaxTrapCards = 2;

        public MultiObjectiveFitness Evaluate(List<SimulationStats> results)
        {
            var fitness = new MultiObjectiveFitness();
            
            if (results == null || !results.Any())
            {
                fitness.IsFeasible = false;
                fitness.ConstraintViolations = 999;
                return fitness;
            }

            // Calculate raw metrics
            fitness.WinRate = (float)results.Count(r => r.IsVictory) / results.Count;
            
            var winningRuns = results.Where(r => r.IsVictory).ToList();
            fitness.VictoryHp = winningRuns.Any() 
                ? winningRuns.Average(r => r.FinalHPPercent) 
                : 0f;

            var losingRuns = results.Where(r => !r.IsVictory).ToList();
            fitness.AvgFloorOnDeath = losingRuns.Any()
                ? (float)losingRuns.Average(r => r.FinalFloorReached)
                : 15f;

            // Card viability analysis
            var (viableCards, trapCards, buildVariety) = AnalyzeCardDiversity(results);
            fitness.ViableCards = viableCards;
            fitness.TrapCards = trapCards;
            fitness.BuildVariety = buildVariety;

            // Check constraints
            fitness.ConstraintViolations = 0;
            
            if (fitness.WinRate < MinAcceptableWinRate || 
                fitness.WinRate > MaxAcceptableWinRate)
            {
                fitness.IsFeasible = false;
                fitness.ConstraintViolations++;
            }
            
            if (viableCards < MinViableCards)
            {
                fitness.IsFeasible = false;
                fitness.ConstraintViolations++;
            }
            
            if (trapCards > MaxTrapCards)
            {
                fitness.IsFeasible = false;
                fitness.ConstraintViolations++;
            }

            // Calculate objective scores
            fitness.BalanceScore = CalculateBalanceScore(fitness);
            fitness.EngagementScore = CalculateEngagementScore(fitness);
            fitness.CoherenceScore = CalculateCoherenceScore(fitness);

            return fitness;
        }

        private float CalculateBalanceScore(MultiObjectiveFitness f)
        {
            // How close are we to ideal balance targets?
            float winRateDiff = Math.Abs(f.WinRate - TargetWinRate);
            float victoryHpDiff = Math.Abs(f.VictoryHp - TargetVictoryHp);
            float floorDeathDiff = Math.Abs(f.AvgFloorOnDeath - TargetAvgFloorOnDeath);
            
            // Gaussian-like scoring (1.0 = perfect, 0.0 = far off)
            // Sensitivity constants control how quickly score drops with deviation
            // Higher value = more sensitive to small deviations
            
            // WIN_RATE_SENSITIVITY = 10.0
            // At 10% deviation: score = exp(-10 * 0.1^2) = 0.37
            // At 20% deviation: score = exp(-10 * 0.2^2) = 0.14
            const float WIN_RATE_SENSITIVITY = 10.0f;
            
            // VICTORY_HP_SENSITIVITY = 8.0
            // At 10% deviation: score = exp(-8 * 0.1^2) = 0.45
            // At 20% deviation: score = exp(-8 * 0.2^2) = 0.20
            const float VICTORY_HP_SENSITIVITY = 8.0f;
            
            // FLOOR_DEATH_SENSITIVITY = 0.05
            // At 5 floor deviation: score = exp(-0.05 * 5^2) = 0.29
            // At 10 floor deviation: score = exp(-0.05 * 10^2) = 0.01
            const float FLOOR_DEATH_SENSITIVITY = 0.05f;
            
            float wrScore = (float)Math.Exp(-WIN_RATE_SENSITIVITY * winRateDiff * winRateDiff);
            float hpScore = (float)Math.Exp(-VICTORY_HP_SENSITIVITY * victoryHpDiff * victoryHpDiff);
            float floorScore = (float)Math.Exp(-FLOOR_DEATH_SENSITIVITY * floorDeathDiff * floorDeathDiff);
            
            return (wrScore + hpScore + floorScore) / 3.0f;
        }

        private float CalculateEngagementScore(MultiObjectiveFitness f)
        {
            // Card diversity and build variety
            float diversityScore = Math.Min(1.0f, f.ViableCards / 18.0f); // 18+ viable = perfect
            float varietyScore = f.BuildVariety; // Already 0-1
            
            return (diversityScore + varietyScore) / 2.0f;
        }

        private float CalculateCoherenceScore(MultiObjectiveFitness f)
        {
            // Penalize design problems
            float trapPenalty = Math.Max(0f, 1.0f - (f.TrapCards * 0.2f)); // -0.2 per trap card
            
            // Reward consistency
            float wrConsistency = 1.0f;
            if (f.WinRate < 0.35f || f.WinRate > 0.55f)
                wrConsistency = 0.5f;
            
            return (trapPenalty + wrConsistency) / 2.0f;
        }

        private static readonly HashSet<string> StartingCards = new() 
        { 
            "strike", "defend", "quick_jab", "cycle" 
        };

        private (int viableCards, int trapCards, float buildVariety) AnalyzeCardDiversity(
            List<SimulationStats> results)
        {
            var pickCounts = new Dictionary<string, int>();
            var pickWins = new Dictionary<string, int>();
            
            foreach (var run in results)
            {
                var pickedCards = new HashSet<string>(
                    run.MasterDeckIds.Where(id => !StartingCards.Contains(id))
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

            // Viable cards: >10% pick rate
            int viable = pickCounts.Count(kv => (float)kv.Value / results.Count >= 0.10f);
            
            // Trap cards: >10% pick rate but <30% win rate
            // Minimum sample size required for statistical validity
            const int MIN_PICKS_FOR_TRAP_DETECTION = 10;
            
            int traps = 0;
            foreach (var cardId in pickCounts.Keys)
            {
                int picks = pickCounts[cardId];
                float pickRate = (float)picks / results.Count;
                float winRate = (float)pickWins[cardId] / picks;
                
                // Skip cards with insufficient sample size
                if (picks < MIN_PICKS_FOR_TRAP_DETECTION) continue;
                
                if (pickRate > 0.10f && winRate < 0.30f)
                    traps++;
            }

            // Build variety: Measure deck diversity among winning runs
            float variety = CalculateBuildVariety(results);

            return (viable, traps, variety);
        }

        private float CalculateBuildVariety(List<SimulationStats> results)
        {
            var winningRuns = results.Where(r => r.IsVictory).ToList();
            if (winningRuns.Count < 2) return 0f;

            float totalSimilarity = 0f;
            int comparisons = 0;
            int maxComparisons = Math.Min(50, winningRuns.Count * (winningRuns.Count - 1) / 2);

            for (int i = 0; i < winningRuns.Count - 1 && comparisons < maxComparisons; i++)
            {
                for (int j = i + 1; j < winningRuns.Count && comparisons < maxComparisons; j++)
                {
                    var deck1 = new HashSet<string>(
                        winningRuns[i].MasterDeckIds.Where(id => !StartingCards.Contains(id))
                    );
                    var deck2 = new HashSet<string>(
                        winningRuns[j].MasterDeckIds.Where(id => !StartingCards.Contains(id))
                    );

                    if (deck1.Count == 0 || deck2.Count == 0) continue;

                    int intersection = deck1.Intersect(deck2).Count();
                    int union = deck1.Union(deck2).Count();

                    float jaccard = (float)intersection / union;
                    totalSimilarity += jaccard;
                    comparisons++;
                }
            }

            if (comparisons == 0) return 0.5f;

            float avgSimilarity = totalSimilarity / comparisons;
            return 1.0f - avgSimilarity; // Lower similarity = higher variety
        }
    }
}
