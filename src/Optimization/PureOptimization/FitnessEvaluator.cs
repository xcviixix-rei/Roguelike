using System;
using System.Collections.Generic;
using System.Linq;

namespace Roguelike.Optimization
{
    public class FitnessEvaluator
    {
        // Target metrics
        public float TargetWinRate = 0.75f;
        public float MinAcceptableWinRate = 0.6f;
        public float MaxAcceptableWinRate = 0.9f;
        
        public float TargetVictoryHpPercent = 0.10f;
        public float MinAcceptableVictoryHp = 0.05f;
        public float MaxAcceptableVictoryHp = 0.20f;
        
        public float TargetAvgFloorOnDeath = 13.0f;
        
        // Weights
        public float WinRateWeight = 35.0f;
        public float VictoryHpWeight = 20.0f;
        public float GameLengthWeight = 10.0f;
        public float CardViabilityWeight = 20.0f;
        public float ElitePerformanceWeight = 10.0f;
        public float ConsistencyWeight = 5.0f;
        
        // Starting cards
        private static readonly HashSet<string> StartingCards = new HashSet<string>
        {
            "strike", "defend", "quick_jab", "cycle"
        };

        /// <summary>
        /// Calculates fitness and returns detailed breakdown for logging
        /// </summary>
        public FitnessBreakdown CalculateFitnessWithBreakdown(List<SimulationStats> results)
        {
            var breakdown = new FitnessBreakdown();
            
            if (results == null || results.Count == 0)
            {
                return breakdown;
            }

            // Calculate individual scores
            breakdown.WinRateScore = CalculateWinRateScore(results);
            breakdown.VictoryHpScore = CalculateVictoryHpScore(results);
            breakdown.GameLengthScore = CalculateGameLengthScore(results);
            breakdown.CardViabilityScore = CalculateCardViabilityScore(results);
            breakdown.EliteScore = CalculateElitePerformanceScore(results);
            breakdown.ConsistencyScore = CalculateConsistencyScore(results);

            // Calculate raw metrics for reporting
            breakdown.WinRate = (float)results.Count(r => r.IsVictory) / results.Count;
            
            var winningRuns = results.Where(r => r.IsVictory).ToList();
            breakdown.AvgVictoryHp = winningRuns.Any() 
                ? winningRuns.Average(r => r.FinalHPPercent) 
                : 0f;

            var losingRuns = results.Where(r => !r.IsVictory).ToList();
            breakdown.AvgFloorOnDeath = losingRuns.Any()
                ? (float)losingRuns.Average(r => r.FinalFloorReached)
                : 15f;

            // Count viable cards and trap cards
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

            breakdown.ViableCards = pickCounts.Count(kv => (float)kv.Value / results.Count >= 0.10f);
            
            breakdown.TrapCards = 0;
            foreach (var cardId in pickCounts.Keys)
            {
                float pickRate = (float)pickCounts[cardId] / results.Count;
                float winRate = (float)pickWins[cardId] / pickCounts[cardId];
                if (pickRate > 0.10f && winRate < 0.30f)
                {
                    breakdown.TrapCards++;
                }
            }

            // Elite metrics
            var runsWithElites = results.Where(r => r.ElitesEncountered > 0).ToList();
            if (runsWithElites.Any())
            {
                float avgElitesDefeated = (float)runsWithElites.Average(r => r.ElitesDefeated);
                float avgElitesEncountered = (float)runsWithElites.Average(r => r.ElitesEncountered);
                breakdown.EliteKillRate = avgElitesDefeated / avgElitesEncountered;
            }

            // Check hard constraints
            if (breakdown.WinRate < MinAcceptableWinRate)
            {
                breakdown.TotalFitness = breakdown.WinRateScore * WinRateWeight * 0.1f;
                breakdown.IsCriticalFailure = true;
                return breakdown;
            }

            if (breakdown.WinRate > MaxAcceptableWinRate)
            {
                breakdown.TotalFitness = breakdown.WinRateScore * WinRateWeight * 0.5f;
                breakdown.IsCriticalFailure = true;
                return breakdown;
            }

            // Normal calculation
            breakdown.TotalFitness =
                (breakdown.WinRateScore * WinRateWeight) +
                (breakdown.VictoryHpScore * VictoryHpWeight) +
                (breakdown.GameLengthScore * GameLengthWeight) +
                (breakdown.CardViabilityScore * CardViabilityWeight) +
                (breakdown.EliteScore * ElitePerformanceWeight) +
                (breakdown.ConsistencyScore * ConsistencyWeight);

            return breakdown;
        }

        /// <summary>
        /// Standard fitness calculation (backwards compatible)
        /// </summary>
        public float CalculateFitness(List<SimulationStats> results)
        {
            return CalculateFitnessWithBreakdown(results).TotalFitness;
        }

        private float CalculateWinRateScore(List<SimulationStats> results)
        {
            float actualWinRate = (float)results.Count(r => r.IsVictory) / results.Count;
            float diff = actualWinRate - TargetWinRate;
            
            float k = 20.0f;
            float score = 1.0f / (1.0f + k * diff * diff);
            
            return score;
        }

        private float CalculateVictoryHpScore(List<SimulationStats> results)
        {
            var winningRuns = results.Where(r => r.IsVictory).ToList();
            if (!winningRuns.Any()) return 0f;

            float avgVictoryHp = winningRuns.Average(r => r.FinalHPPercent);
            
            float diff = Math.Abs(avgVictoryHp - TargetVictoryHpPercent);
            
            // score = e^(-k * diff^2)
            float k = 8.0f;
            float score = (float)Math.Exp(-k * diff * diff);
            
            return score;
        }

        private float CalculateGameLengthScore(List<SimulationStats> results)
        {
            var losingRuns = results.Where(r => !r.IsVictory).ToList();
            if (!losingRuns.Any()) 
            {
                var avgFloor = results.Average(r => r.FinalFloorReached);
                return avgFloor >= 13 ? 1.0f : 0.8f;
            }

            float avgDeathFloor = (float)losingRuns.Average(r => r.FinalFloorReached);
            
            float diff = Math.Abs(avgDeathFloor - TargetAvgFloorOnDeath);
            
            float k = 0.15f;
            float score = (float)Math.Exp(-k * diff * diff);
            
            return score;
        }

        private float CalculateCardViabilityScore(List<SimulationStats> results)
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

            if (pickCounts.Count < 2) return 0f;

            float diversityScore = CalculatePickDiversity(pickCounts);
            float balanceScore = CalculateWinRateBalance(pickCounts, pickWins);
            float noTrapsScore = CalculateNoTrapsScore(pickCounts, pickWins, results.Count);
            float varietyScore = CalculateBuildVariety(results);

            return (diversityScore * 0.3f) + 
                   (balanceScore * 0.3f) + 
                   (noTrapsScore * 0.2f) + 
                   (varietyScore * 0.2f);
        }

        private float CalculatePickDiversity(Dictionary<string, int> pickCounts)
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

        private float CalculateWinRateBalance(Dictionary<string, int> pickCounts, Dictionary<string, int> pickWins)
        {
            var winRates = new List<float>();
            
            foreach (var cardId in pickCounts.Keys)
            {
                if (pickCounts[cardId] < 5) continue;
                float winRate = (float)pickWins[cardId] / pickCounts[cardId];
                winRates.Add(winRate);
            }

            if (winRates.Count < 2) return 1f;

            float avgWinRate = winRates.Average();
            float variance = winRates.Select(wr => (wr - avgWinRate) * (wr - avgWinRate)).Average();
            float stdDev = (float)Math.Sqrt(variance);

            return Math.Max(0f, 1.0f - (stdDev / 0.3f));
        }

        private float CalculateNoTrapsScore(Dictionary<string, int> pickCounts, Dictionary<string, int> pickWins, int totalRuns)
        {
            float totalPenalty = 0f;
            int cardsEvaluated = 0;

            foreach (var cardId in pickCounts.Keys)
            {
                int picks = pickCounts[cardId];
                int wins = pickWins[cardId];
                
                float pickRate = (float)picks / totalRuns;
                float winRate = (float)wins / picks;
                
                if (pickRate < 0.10f) continue;
                
                cardsEvaluated++;
                
                if (winRate < 0.30f)
                {
                    float trapSeverity = (0.30f - winRate) / 0.30f;
                    totalPenalty += trapSeverity * pickRate;
                }
            }

            if (cardsEvaluated == 0) return 1f;

            return Math.Max(0f, 1.0f - totalPenalty * 3.0f);
        }

        private float CalculateBuildVariety(List<SimulationStats> results)
        {
            var winningRuns = results.Where(r => r.IsVictory).ToList();
            if (winningRuns.Count < 2) return 0f;

            float totalSimilarity = 0f;
            int comparisons = 0;

            for (int i = 0; i < winningRuns.Count - 1; i++)
            {
                for (int j = i + 1; j < winningRuns.Count; j++)
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
                    
                    if (comparisons > 100) break;
                }
                if (comparisons > 100) break;
            }

            if (comparisons == 0) return 0.5f;

            float avgSimilarity = totalSimilarity / comparisons;
            return 1.0f - avgSimilarity;
        }

        private float CalculateElitePerformanceScore(List<SimulationStats> results)
        {
            var runsWithElites = results.Where(r => r.ElitesEncountered > 0).ToList();
            if (!runsWithElites.Any()) return 0.5f;

            float avgElitesDefeated = (float)runsWithElites.Average(r => r.ElitesDefeated);
            float avgElitesEncountered = (float)runsWithElites.Average(r => r.ElitesEncountered);
            float eliteKillRate = avgElitesDefeated / avgElitesEncountered;

            var eliteWins = runsWithElites.Where(r => r.ElitesDefeated > 0).ToList();
            float avgDamagePerElite = eliteWins.Any() 
                ? (float)eliteWins.Average(r => r.TotalDamageTakenAtElites / Math.Max(1, r.ElitesDefeated))
                : 50f;

            float damageScore = 1.0f - Math.Max(0f, Math.Min(1f, (avgDamagePerElite - 20f) / 30f));

            return (eliteKillRate * 0.7f) + (damageScore * 0.3f);
        }

        private float CalculateConsistencyScore(List<SimulationStats> results)
        {
            if (results.Count < 10) return 0.5f;

            var floors = results.Select(r => r.FinalFloorReached).ToList();
            float avgFloor = (float)floors.Average();
            float floorVariance = floors.Select(f => (f - avgFloor) * (f - avgFloor)).Average();
            float floorStdDev = (float)Math.Sqrt(floorVariance);

            float floorConsistency = Math.Max(0f, 1.0f - (floorStdDev / 5.0f));

            int chunkSize = Math.Max(20, results.Count / 5);
            var winRateChunks = new List<float>();
            
            for (int i = 0; i < results.Count; i += chunkSize)
            {
                var chunk = results.Skip(i).Take(chunkSize).ToList();
                if (chunk.Count < 10) continue;
                
                float chunkWinRate = (float)chunk.Count(r => r.IsVictory) / chunk.Count;
                winRateChunks.Add(chunkWinRate);
            }

            float winRateConsistency = 0.5f;
            if (winRateChunks.Count >= 2)
            {
                float avgChunkWinRate = winRateChunks.Average();
                float wrVariance = winRateChunks.Select(wr => (wr - avgChunkWinRate) * (wr - avgChunkWinRate)).Average();
                float wrStdDev = (float)Math.Sqrt(wrVariance);
                
                winRateConsistency = Math.Max(0f, 1.0f - (wrStdDev / 0.2f));
            }

            return (floorConsistency * 0.6f) + (winRateConsistency * 0.4f);
        }
    }
}
