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
        /// </summary>
        public bool Dominates(MultiObjectiveFitness other)
        {
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
            float wrScore = (float)Math.Exp(-10.0 * winRateDiff * winRateDiff);
            float hpScore = (float)Math.Exp(-8.0 * victoryHpDiff * victoryHpDiff);
            float floorScore = (float)Math.Exp(-0.05 * floorDeathDiff * floorDeathDiff);
            
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
            int traps = 0;
            foreach (var cardId in pickCounts.Keys)
            {
                float pickRate = (float)pickCounts[cardId] / results.Count;
                float winRate = (float)pickWins[cardId] / pickCounts[cardId];
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
