using System;
using System.Collections.Generic;
using System.Linq;

namespace Roguelike.GA
{
    public static class ReportGenerator
    {
        public static EvaluationReport Generate(List<SimulationStats> results, float fitness)
        {
            if (results == null || !results.Any()) return new EvaluationReport();

            var report = new EvaluationReport
            {
                TotalRuns = results.Count,
                OverallFitness = fitness
            };

            report.WinRate = (float)results.Count(r => r.IsVictory) / results.Count;

            var winningRuns = results.Where(r => r.IsVictory).ToList();
            report.AvgHpOnVictory = winningRuns.Any() ? winningRuns.Average(r => r.FinalHPPercent) : 0f;

            var losingRuns = results.Where(r => !r.IsVictory).ToList();
            report.FloorOfDeathHistogram = losingRuns
                .GroupBy(r => r.FinalFloorReached)
                .ToDictionary(g => g.Key, g => g.Count());

            var allCardIds = results.SelectMany(r => r.MasterDeckIds).Distinct().ToList();
            var pickCounts = new Dictionary<string, int>();
            
            foreach (var run in results)
            {
                foreach (var cardId in new HashSet<string>(run.MasterDeckIds))
                {
                    if (!pickCounts.ContainsKey(cardId)) pickCounts[cardId] = 0;
                    pickCounts[cardId]++;
                }
            }

            foreach (var cardId in allCardIds)
            {
                var runsWithCard = results.Where(r => r.MasterDeckIds.Contains(cardId)).ToList();
                if (!runsWithCard.Any()) continue;

                var winsWithCard = runsWithCard.Count(r => r.IsVictory);
                float pickRate = (float)runsWithCard.Count / results.Count;
                float winRate = (float)winsWithCard / runsWithCard.Count;
                
                var info = new CardViabilityInfo
                {
                    CardId = cardId,
                    PickRate = pickRate,
                    WinRateWhenPicked = winRate,
                    AvgPlayCount = (float)runsWithCard.Average(r => r.CardPlayCounts.ContainsKey(cardId) ? r.CardPlayCounts[cardId] : 0),
                    IsTrapCard = pickRate > 0.15f && winRate < 0.35f,
                    IsMustPick = pickRate > 0.50f && winRate > 0.70f
                };
                report.CardViability.Add(info);
            }
            
            report.CardViability = report.CardViability.OrderByDescending(c => c.PickRate).ToList();

            report.TotalUniqueCardsPicked = pickCounts.Count;
            report.ShannonEntropy = CalculateShannonEntropy(pickCounts);
            report.GiniCoefficient = CalculateGiniCoefficient(pickCounts);
            report.EffectiveNumberOfCards = CalculateEffectiveNumberOfCards(pickCounts);
            report.UnusedCards = allCardIds.Count - pickCounts.Count;

            return report;
        }

        private static float CalculateShannonEntropy(Dictionary<string, int> pickCounts)
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

            double maxEntropy = Math.Log(pickCounts.Count, 2);
            if (maxEntropy == 0) return 0f;

            return (float)(entropy / maxEntropy);
        }

        private static float CalculateGiniCoefficient(Dictionary<string, int> pickCounts)
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
            return (float)Math.Abs(gini);
        }

        private static float CalculateEffectiveNumberOfCards(Dictionary<string, int> pickCounts)
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