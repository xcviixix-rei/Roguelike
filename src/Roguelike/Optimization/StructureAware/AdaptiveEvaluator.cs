using System;
using System.Collections.Generic;
using System.Linq;

namespace Roguelike.Optimization
{


    public class AdaptiveEvaluator
    {
        private readonly ISimulationRunner _runner;
        private readonly MultiObjectiveEvaluator _evaluator;
        private readonly Random _rng;


        public int Phase1Runs { get; set; } = 75;
        public int Phase2Runs { get; set; } = 25;
        public int Phase3Runs { get; set; } = 100;


        public int TotalGenerations { get; set; } = 30;


        public float MinAcceptableWinRate { get; set; } = 0.30f;
        public float MaxAcceptableWinRate { get; set; } = 0.60f;

        public AdaptiveEvaluator(ISimulationRunner runner, MultiObjectiveEvaluator evaluator, Random rng)
        {
            _runner = runner;
            _evaluator = evaluator;
            _rng = rng;
        }


        private (float min, float max) GetAcceptableWinRateRange(int generation)
        {
            float progress = (float)generation / Math.Max(1, TotalGenerations);


            if (progress < 0.33f)
                return (0.15f, 0.75f);


            if (progress < 0.67f)
                return (0.25f, 0.65f);


            return (MinAcceptableWinRate, MaxAcceptableWinRate);
        }


        public (MultiObjectiveFitness fitness, List<SimulationStats> results) Evaluate(
            HierarchicalGenome genome,
            bool isElite = false,
            int generation = 0)
        {
            var results = new List<SimulationStats>();


            for (int i = 0; i < Phase1Runs; i++)
            {
                results.Add(RunSimulation(genome));
            }

            float phase1WinRate = (float)results.Count(r => r.IsVictory) / results.Count;


            var (minWR, maxWR) = GetAcceptableWinRateRange(generation);

            if (phase1WinRate < minWR || phase1WinRate > maxWR)
            {
                var fitness = _evaluator.Evaluate(results);
                fitness.IsFeasible = false;
                return (fitness, results);
            }


            for (int i = 0; i < Phase2Runs; i++)
            {
                results.Add(RunSimulation(genome));
            }

            bool isPromising = IsPromising(results);

            if (isElite || isPromising)
            {

                for (int i = 0; i < Phase3Runs; i++)
                {
                    results.Add(RunSimulation(genome));
                }
            }

            var finalFitness = _evaluator.Evaluate(results);
            return (finalFitness, results);
        }


        private bool IsPromising(List<SimulationStats> results)
        {
            float winRate = (float)results.Count(r => r.IsVictory) / results.Count;


            var uniqueCards = results
                .SelectMany(r => r.MasterDeckIds)
                .Where(id => id != "strike" && id != "defend" && id != "quick_jab" && id != "cycle")
                .Distinct()
                .Count();


            var (_, trapCards) = EstimateCardMetrics(results);


            float balanceScore = 1.0f - Math.Abs(winRate - 0.45f) / 0.45f;
            float engagementScore = Math.Min(1.0f, uniqueCards / 20.0f);
            float coherenceScore = Math.Max(0f, 1.0f - (trapCards / 5.0f));


            return (balanceScore > 0.7f || engagementScore > 0.7f || coherenceScore > 0.8f);
        }


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


        public void EvaluatePopulation(
            List<Individual> population,
            int generation = 0,
            Action<int, int> progressCallback = null)
        {
            int evaluated = 0;


            var elites = population.Where(ind => ind.Fitness?.Rank == 1).ToList();
            var eliteIds = new HashSet<Individual>(elites);

            foreach (var individual in population)
            {
                if (individual.Fitness != null)
                {

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


        public (float lowerBound, float upperBound) GetWinRateConfidenceInterval(
            List<SimulationStats> results,
            float confidence = 0.95f)
        {
            int n = results.Count;
            int wins = results.Count(r => r.IsVictory);
            float p = (float)wins / n;


            float z = confidence == 0.95f ? 1.96f : 2.576f;

            float denominator = 1 + z * z / n;
            float center = p + z * z / (2 * n);
            float spread = z * (float)Math.Sqrt(p * (1 - p) / n + z * z / (4 * n * n));

            float lower = (center - spread) / denominator;
            float upper = (center + spread) / denominator;

            return (Math.Max(0, lower), Math.Min(1, upper));
        }
    }
}
