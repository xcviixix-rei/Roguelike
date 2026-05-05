using System;
using System.Collections.Generic;
using System.Linq;

namespace Roguelike.Optimization
{


    public class MultiObjectiveFitness
    {


        public float BalanceScore { get; set; }


        public float EngagementScore { get; set; }


        public float CoherenceScore { get; set; }


        public float WinRate { get; set; }
        public float VictoryHp { get; set; }
        public float AvgFloorOnDeath { get; set; }
        public int ViableCards { get; set; }
        public int TrapCards { get; set; }
        public float BuildVariety { get; set; }


        public int Rank { get; set; }
        public float CrowdingDistance { get; set; }


        public bool IsFeasible { get; set; } = true;
        public int ConstraintViolations { get; set; } = 0;


        public bool Dominates(MultiObjectiveFitness other)
        {

            if (this.IsFeasible && !other.IsFeasible) return true;
            if (!this.IsFeasible && other.IsFeasible) return false;


            if (!this.IsFeasible && !other.IsFeasible)
                return this.ConstraintViolations < other.ConstraintViolations;


            bool atLeastOneBetter = false;


            if (BalanceScore < other.BalanceScore) return false;
            if (EngagementScore < other.EngagementScore) return false;
            if (CoherenceScore < other.CoherenceScore) return false;


            if (BalanceScore > other.BalanceScore) atLeastOneBetter = true;
            if (EngagementScore > other.EngagementScore) atLeastOneBetter = true;
            if (CoherenceScore > other.CoherenceScore) atLeastOneBetter = true;

            return atLeastOneBetter;
        }


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


    public class MultiObjectiveEvaluator
    {

        public float TargetWinRate = 0.45f;
        public float TargetVictoryHp = 0.30f;
        public float TargetAvgFloorOnDeath = 10.0f;


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


            fitness.WinRate = (float)results.Count(r => r.IsVictory) / results.Count;

            var winningRuns = results.Where(r => r.IsVictory).ToList();
            fitness.VictoryHp = winningRuns.Any()
                ? winningRuns.Average(r => r.FinalHPPercent)
                : 0f;

            var losingRuns = results.Where(r => !r.IsVictory).ToList();
            fitness.AvgFloorOnDeath = losingRuns.Any()
                ? (float)losingRuns.Average(r => r.FinalFloorReached)
                : 15f;


            var (viableCards, trapCards, buildVariety) = AnalyzeCardDiversity(results);
            fitness.ViableCards = viableCards;
            fitness.TrapCards = trapCards;
            fitness.BuildVariety = buildVariety;


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


            fitness.BalanceScore = CalculateBalanceScore(fitness);
            fitness.EngagementScore = CalculateEngagementScore(fitness);
            fitness.CoherenceScore = CalculateCoherenceScore(fitness);

            return fitness;
        }

        private float CalculateBalanceScore(MultiObjectiveFitness f)
        {

            float winRateDiff = Math.Abs(f.WinRate - TargetWinRate);
            float victoryHpDiff = Math.Abs(f.VictoryHp - TargetVictoryHp);
            float floorDeathDiff = Math.Abs(f.AvgFloorOnDeath - TargetAvgFloorOnDeath);


            const float WIN_RATE_SENSITIVITY = 10.0f;


            const float VICTORY_HP_SENSITIVITY = 8.0f;


            const float FLOOR_DEATH_SENSITIVITY = 0.05f;

            float wrScore = (float)Math.Exp(-WIN_RATE_SENSITIVITY * winRateDiff * winRateDiff);
            float hpScore = (float)Math.Exp(-VICTORY_HP_SENSITIVITY * victoryHpDiff * victoryHpDiff);
            float floorScore = (float)Math.Exp(-FLOOR_DEATH_SENSITIVITY * floorDeathDiff * floorDeathDiff);

            return (wrScore + hpScore + floorScore) / 3.0f;
        }

        private float CalculateEngagementScore(MultiObjectiveFitness f)
        {

            float diversityScore = Math.Min(1.0f, f.ViableCards / 18.0f);
            float varietyScore = f.BuildVariety;

            return (diversityScore + varietyScore) / 2.0f;
        }

        private float CalculateCoherenceScore(MultiObjectiveFitness f)
        {

            float trapPenalty = Math.Max(0f, 1.0f - (f.TrapCards * 0.2f));


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


            int viable = pickCounts.Count(kv => (float)kv.Value / results.Count >= 0.10f);


            const int MIN_PICKS_FOR_TRAP_DETECTION = 10;

            int traps = 0;
            foreach (var cardId in pickCounts.Keys)
            {
                int picks = pickCounts[cardId];
                float pickRate = (float)picks / results.Count;
                float winRate = (float)pickWins[cardId] / picks;


                if (picks < MIN_PICKS_FOR_TRAP_DETECTION) continue;

                if (pickRate > 0.10f && winRate < 0.30f)
                    traps++;
            }


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
            return 1.0f - avgSimilarity;
        }
    }
}
