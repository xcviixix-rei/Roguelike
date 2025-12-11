using System;
using System.Collections.Generic;
using System.Linq;

namespace Roguelike.GA
{
    public class FitnessEvaluator
    {
        // Outcome Weights
        public float TargetWinRate = 0.45f;
        public float WinRateWeight = 100.0f;

        // Quality / Player Experience Weights
        public float TargetVictoryHpPercent = 0.30f;
        public float VictoryHpWeight = 20.0f;

        // Pacing Weights
        // public float FloorOfDeathWeight = 10.0f;

        // Diversity Weights
        public float CardViabilityWeight = 5.0f;


        /// <summary>
        /// Calculates the final fitness score for a genome based on a batch of simulation results.
        /// </summary>
        /// <param name="results">A list of SimulationStats, one for each run with this genome.</param>
        /// <returns>A single float representing the genome's fitness. Higher is better.</returns>
        public float CalculateFitness(List<SimulationStats> results)
        {
            if (results == null || results.Count == 0) return 0f;

            // float winRateScore = CalculateWinRateScore(results);
            float winRatePenalty = CalculateWinRatePenalty(results);
            if (winRatePenalty < 0.1f)
            {
                return winRatePenalty * WinRateWeight;
            }

            float victoryHpScore = CalculateVictoryHpScore(results);
            float floorOfDeathScore = CalculateFloorOfDeathScore(results);
            float cardViabilityScore = CalculateCardViabilityScore(results);

            float totalFitness =
                (winRatePenalty * WinRateWeight) +
                (victoryHpScore * VictoryHpWeight) +
                // (floorOfDeathScore * FloorOfDeathWeight) +
                (cardViabilityScore * CardViabilityWeight);

            return totalFitness;
        }

        private float CalculateWinRateScore(List<SimulationStats> results)
        {
            float actualWinRate = (float)results.Count(r => r.IsVictory) / results.Count;

            float winRateSigma = 0.08f;
            float diff = Math.Abs(actualWinRate - TargetWinRate);

            return (float)Math.Exp(-(diff * diff) / (2 * winRateSigma * winRateSigma));
        }

        private float CalculateWinRatePenalty(List<SimulationStats> results)
        {
            float actualWinRate = (float)results.Count(r => r.IsVictory) / results.Count;

            float diff = actualWinRate - TargetWinRate;
            float penalty = 1.0f - (diff * diff * 4.0f);

            return Math.Max(0, penalty);
        }

        private float CalculateVictoryHpScore(List<SimulationStats> results)
        {
            var winningRuns = results.Where(r => r.IsVictory).ToList();
            if (!winningRuns.Any()) return 0f;

            float averageVictoryHp = winningRuns.Average(r => r.FinalHPPercent);
            
            float HpSigma = 0.08f;
            float diff = Math.Abs(averageVictoryHp - TargetVictoryHpPercent);
            return (float)Math.Exp(-(diff * diff) / (2 * HpSigma * HpSigma));
        }

        private float CalculateFloorOfDeathScore(List<SimulationStats> results)
        {
            var losingRuns = results.Where(r => !r.IsVictory).ToList();
            if (!losingRuns.Any()) return 1.0f;

            float averageDeathFloor = (float)losingRuns.Average(r => r.FinalFloorReached);
            return averageDeathFloor / 15.0f;
        }

        private float CalculateCardViabilityScore(List<SimulationStats> results)
        {
            // Build pick rate distribution
            var pickCounts = new Dictionary<string, int>();
            foreach (var run in results)
            {
                // Count each card only once per deck
                foreach (var cardId in new HashSet<string>(run.MasterDeckIds))
                {
                    if (!pickCounts.ContainsKey(cardId)) pickCounts[cardId] = 0;
                    pickCounts[cardId]++;
                }
            }

            if (pickCounts.Count < 2) return 0f;

            // Calculate three complementary metrics
            float entropyScore = CalculateShannonEntropy(pickCounts, results.Count);
            float giniScore = CalculateGiniCoefficient(pickCounts);
            float trapCardPenalty = CalculateTrapCardPenalty(results, pickCounts);

            // Combine metrics (weighted average)
            return (entropyScore * 0.5f) + (giniScore * 0.3f) + (trapCardPenalty * 0.2f);
        }

        /// <summary>
        /// Shannon Entropy measures the diversity of card picks.
        /// Higher entropy = more uniform distribution = better diversity.
        /// Returns a normalized score between 0 (all cards picked equally) and 1 (perfect diversity).
        /// </summary>
        private float CalculateShannonEntropy(Dictionary<string, int> pickCounts, int totalRuns)
        {
            if (pickCounts.Count == 0) return 0f;

            double entropy = 0.0;
            int totalPicks = pickCounts.Values.Sum();

            foreach (var count in pickCounts.Values)
            {
                if (count == 0) continue;
                double probability = (double)count / totalPicks;
                entropy -= probability * Math.Log(probability, 2);
            }

            // Normalize: max entropy is log2(n) where n = number of unique cards
            double maxEntropy = Math.Log(pickCounts.Count, 2);
            if (maxEntropy == 0) return 0f;

            return (float)(entropy / maxEntropy);
        }

        /// <summary>
        /// Gini Coefficient measures inequality in card pick distribution.
        /// Lower Gini = more equal distribution = better diversity.
        /// </summary>
        /// <returns>A normalized score where 1 = perfect equality, 0 = maximum inequality</returns>
        private float CalculateGiniCoefficient(Dictionary<string, int> pickCounts)
        {
            var counts = pickCounts.Values.OrderBy(x => x).ToList();
            if (counts.Count == 0) return 0f;

            int n = counts.Count;
            double sum = 0.0;
            double totalPicks = counts.Sum();

            if (totalPicks == 0) return 0f;

            for (int i = 0; i < n; i++)
            {
                sum += (2.0 * (i + 1) - n - 1) * counts[i];
            }

            double gini = sum / (n * totalPicks);

            return 1.0f - (float)Math.Abs(gini);
        }

        /// <summary>
        /// Identifies and penalizes "trap cards" - cards that are frequently picked but lead to losses
        /// </summary>
        /// <returns>A  score between 0 and 1, where 1 = no trap cards detected</returns>
        private float CalculateTrapCardPenalty(List<SimulationStats> results, Dictionary<string, int> pickCounts)
        {
            if (results.Count == 0) return 1f;

            var cardWinRates = new Dictionary<string, float>();
            
            foreach (var cardId in pickCounts.Keys)
            {
                var runsWithCard = results.Where(r => r.MasterDeckIds.Contains(cardId)).ToList();
                if (!runsWithCard.Any()) continue;

                float winRate = (float)runsWithCard.Count(r => r.IsVictory) / runsWithCard.Count;
                float pickRate = (float)pickCounts[cardId] / results.Count;

                cardWinRates[cardId] = winRate;

                // A trap card is picked frequently (>15%) but has poor win rate (<35%)
                bool isTrapCard = pickRate > 0.15f && winRate < 0.35f;
                if (isTrapCard)
                {
                    return 0f;
                }
            }

            if (cardWinRates.Count < 2) return 1f;

            float avgWinRate = cardWinRates.Values.Average();
            float variance = cardWinRates.Values.Select(wr => (wr - avgWinRate) * (wr - avgWinRate)).Average();
            float stdDev = (float)Math.Sqrt(variance);

            // Lower variance = cards perform more consistently = better
            // Normalize to 0-1 range (assume stdDev rarely exceeds 0.3)
            float consistencyScore = Math.Max(0f, 1f - (stdDev / 0.3f));

            return consistencyScore;
        }

        /// <summary>
        /// Calculates the effective number of cards
        /// This represents how many cards are "actually viable" in the meta
        /// </summary>
        private float CalculateEffectiveNumberOfCards(Dictionary<string, int> pickCounts)
        {
            if (pickCounts.Count == 0) return 0f;

            int totalPicks = pickCounts.Values.Sum();
            if (totalPicks == 0) return 0f;

            double entropy = 0.0;
            foreach (var count in pickCounts.Values)
            {
                if (count == 0) continue;
                double probability = (double)count / totalPicks;
                entropy -= probability * Math.Log(probability, 2);
            }

            return (float)Math.Pow(2, entropy);
        }
    }
}