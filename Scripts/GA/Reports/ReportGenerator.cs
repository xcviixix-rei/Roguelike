using System;
using System.Collections.Generic;
using System.Linq;

namespace Roguelike.GA
{
    public static class ReportGenerator
    {
        private static readonly HashSet<string> StartingCards = new HashSet<string>
        {
            "strike", "defend", "quick_jab", "cycle"
        };

        public static EvaluationReport Generate(List<SimulationStats> results, float fitness)
        {
            if (results == null || !results.Any()) return new EvaluationReport();

            var report = new EvaluationReport
            {
                TotalRuns = results.Count,
                OverallFitness = fitness
            };

            // === BASIC METRICS ===
            report.WinRate = (float)results.Count(r => r.IsVictory) / results.Count;

            var winningRuns = results.Where(r => r.IsVictory).ToList();
            report.AvgHpOnVictory = winningRuns.Any() ? winningRuns.Average(r => r.FinalHPPercent) : 0f;

            var losingRuns = results.Where(r => !r.IsVictory).ToList();
            report.FloorOfDeathHistogram = losingRuns
                .GroupBy(r => r.FinalFloorReached)
                .ToDictionary(g => g.Key, g => g.Count());

            report.AvgFloorOnDeath = losingRuns.Any() 
                ? (float)losingRuns.Average(r => r.FinalFloorReached) 
                : 15f;

            // === ELITE METRICS ===
            var runsWithElites = results.Where(r => r.ElitesEncountered > 0).ToList();
            if (runsWithElites.Any())
            {
                report.AvgElitesDefeated = (float)runsWithElites.Average(r => r.ElitesDefeated);
                report.AvgElitesEncountered = (float)runsWithElites.Average(r => r.ElitesEncountered);
                report.EliteKillRate = report.AvgElitesDefeated / report.AvgElitesEncountered;
                
                var eliteWinners = runsWithElites.Where(r => r.ElitesDefeated > 0).ToList();
                report.AvgDamagePerElite = eliteWinners.Any()
                    ? (float)eliteWinners.Average(r => r.TotalDamageTakenAtElites / Math.Max(1, r.ElitesDefeated))
                    : 0f;
            }

            // === ECONOMY METRICS ===
            report.AvgGoldCollected = (float)results.Average(r => r.GoldCollected);
            report.AvgGoldSpent = (float)results.Average(r => r.GoldSpent);
            report.AvgGoldEfficiency = report.AvgGoldSpent > 0 
                ? report.AvgGoldSpent / report.AvgGoldCollected 
                : 0f;

            // === CARD VIABILITY (Excluding Starting Cards) ===
            var pickCounts = new Dictionary<string, int>();
            var pickWins = new Dictionary<string, int>();
            var pickPlayCounts = new Dictionary<string, int>();

            foreach (var run in results)
            {
                // Only count non-starting cards
                var pickedCards = new HashSet<string>(
                    run.MasterDeckIds.Where(id => !StartingCards.Contains(id))
                );

                foreach (var cardId in pickedCards)
                {
                    if (!pickCounts.ContainsKey(cardId))
                    {
                        pickCounts[cardId] = 0;
                        pickWins[cardId] = 0;
                        pickPlayCounts[cardId] = 0;
                    }

                    pickCounts[cardId]++;
                    if (run.IsVictory) pickWins[cardId]++;
                    
                    if (run.CardPlayCounts.TryGetValue(cardId, out int plays))
                    {
                        pickPlayCounts[cardId] += plays;
                    }
                }
            }

            // Build card viability info
            foreach (var cardId in pickCounts.Keys)
            {
                int picks = pickCounts[cardId];
                int wins = pickWins[cardId];
                
                float pickRate = (float)picks / results.Count;
                float winRate = (float)wins / picks;
                float avgPlays = (float)pickPlayCounts[cardId] / picks;

                var info = new CardViabilityInfo
                {
                    CardId = cardId,
                    PickRate = pickRate,
                    WinRateWhenPicked = winRate,
                    AvgPlayCount = avgPlays,
                    IsTrapCard = pickRate > 0.10f && winRate < 0.30f,
                    IsMustPick = pickRate > 0.40f && winRate > 0.55f,
                    IsBalanced = pickRate >= 0.10f && winRate >= 0.35f && winRate <= 0.55f
                };

                report.CardViability.Add(info);
            }

            report.CardViability = report.CardViability.OrderByDescending(c => c.PickRate).ToList();

            // === DIVERSITY METRICS ===
            report.TotalUniqueCardsPicked = pickCounts.Count;
            report.ViableCards = report.CardViability.Count(c => c.PickRate >= 0.10f);
            report.BalancedCards = report.CardViability.Count(c => c.IsBalanced);
            report.UnusedCards = report.CardViability.Count(c => c.PickRate < 0.05f);

            if (pickCounts.Count > 0)
            {
                report.ShannonEntropy = CalculateShannonEntropy(pickCounts);
                report.GiniCoefficient = CalculateGiniCoefficient(pickCounts);
                report.EffectiveNumberOfCards = CalculateEffectiveNumberOfCards(pickCounts);
            }

            // === BUILD VARIETY ===
            report.BuildVarietyScore = CalculateBuildVariety(results);

            // === CONSISTENCY METRICS ===
            var floors = results.Select(r => r.FinalFloorReached).ToList();
            float avgFloor = (float)floors.Average();
            float variance = floors.Select(f => (f - avgFloor) * (f - avgFloor)).Average();
            report.FloorConsistency = (float)Math.Sqrt(variance);

            // === QUALITY FLAGS ===
            report.HasCriticalIssues = CheckCriticalIssues(report);
            report.HasBalanceIssues = CheckBalanceIssues(report);
            report.QualityScore = CalculateQualityScore(report);

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

        private static float CalculateBuildVariety(List<SimulationStats> results)
        {
            var winningRuns = results.Where(r => r.IsVictory).ToList();
            if (winningRuns.Count < 2) return 0f;

            float totalSimilarity = 0f;
            int comparisons = 0;
            int maxComparisons = Math.Min(100, winningRuns.Count * (winningRuns.Count - 1) / 2);

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

        private static bool CheckCriticalIssues(EvaluationReport report)
        {
            // Win rate out of acceptable range
            if (report.WinRate < 0.20f || report.WinRate > 0.70f) return true;

            // Too few viable cards
            if (report.ViableCards < 8) return true;

            // Multiple trap cards
            if (report.GetTrapCards().Count >= 3) return true;

            // Victory HP too low (frustrating wins)
            if (report.WinRate > 0.10f && report.AvgHpOnVictory < 0.15f) return true;

            return false;
        }

        private static bool CheckBalanceIssues(EvaluationReport report)
        {
            // Win rate not ideal but acceptable
            if (report.WinRate < 0.35f || report.WinRate > 0.55f) return true;

            // Limited card diversity
            if (report.ViableCards < 12) return true;

            // Few balanced cards
            if (report.BalancedCards < 8) return true;

            // High build similarity (low variety)
            if (report.BuildVarietyScore < 0.40f) return true;

            // Any trap cards exist
            if (report.GetTrapCards().Any()) return true;

            return false;
        }

        private static float CalculateQualityScore(EvaluationReport report)
        {
            float score = 100f;

            // Win rate (max -40 points)
            float winRateDiff = Math.Abs(report.WinRate - 0.45f);
            score -= winRateDiff * 80f;

            // Victory HP (max -20 points)
            float hpDiff = Math.Abs(report.AvgHpOnVictory - 0.30f);
            score -= hpDiff * 40f;

            // Card diversity (max -20 points)
            if (report.ViableCards < 15)
            {
                score -= (15 - report.ViableCards) * 2f;
            }

            // Trap cards (max -20 points)
            int trapCount = report.GetTrapCards().Count;
            score -= trapCount * 10f;

            return Math.Max(0f, Math.Min(100f, score));
        }
    }
}