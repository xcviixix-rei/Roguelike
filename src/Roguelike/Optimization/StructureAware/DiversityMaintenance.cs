using System;
using System.Collections.Generic;
using System.Linq;

namespace Roguelike.Optimization
{


    public static class DiversityMaintenance
    {


        public static float CalculateGenotypeDiversity(List<Individual> population)
        {
            if (population == null || population.Count < 2) return 0f;

            float totalDistance = 0f;
            int comparisons = 0;


            int maxComparisons = Math.Min(100, population.Count * (population.Count - 1) / 2);

            for (int i = 0; i < population.Count - 1 && comparisons < maxComparisons; i++)
            {
                for (int j = i + 1; j < population.Count && comparisons < maxComparisons; j++)
                {
                    float distance = CalculateParameterDistance(
                        population[i].Genome,
                        population[j].Genome
                    );
                    totalDistance += distance;
                    comparisons++;
                }
            }

            return comparisons > 0 ? totalDistance / comparisons : 0f;
        }


        private static float CalculateParameterDistance(
            HierarchicalGenome g1,
            HierarchicalGenome g2)
        {
            float sumSquaredDiff = 0f;
            int paramCount = 0;


            sumSquaredDiff += Sq(g1.GlobalDamageMultiplier - g2.GlobalDamageMultiplier);
            sumSquaredDiff += Sq(g1.GlobalHealthMultiplier - g2.GlobalHealthMultiplier);
            sumSquaredDiff += Sq(g1.GlobalBlockMultiplier - g2.GlobalBlockMultiplier);
            sumSquaredDiff += Sq(g1.GlobalManaCostMultiplier - g2.GlobalManaCostMultiplier);
            sumSquaredDiff += Sq(g1.GlobalGoldMultiplier - g2.GlobalGoldMultiplier);
            paramCount += 5;


            sumSquaredDiff += Sq(g1.EarlyGameDamageScaling - g2.EarlyGameDamageScaling);
            sumSquaredDiff += Sq(g1.MidGameDamageScaling - g2.MidGameDamageScaling);
            sumSquaredDiff += Sq(g1.LateGameDamageScaling - g2.LateGameDamageScaling);
            sumSquaredDiff += Sq(g1.EarlyGameHealthScaling - g2.EarlyGameHealthScaling);
            sumSquaredDiff += Sq(g1.MidGameHealthScaling - g2.MidGameHealthScaling);
            sumSquaredDiff += Sq(g1.LateGameHealthScaling - g2.LateGameHealthScaling);
            sumSquaredDiff += Sq(g1.EarlyGameBlockScaling - g2.EarlyGameBlockScaling);
            sumSquaredDiff += Sq(g1.MidGameBlockScaling - g2.MidGameBlockScaling);
            sumSquaredDiff += Sq(g1.LateGameBlockScaling - g2.LateGameBlockScaling);
            paramCount += 9;


            foreach (var key in g1.CardTypeScalars.Keys)
            {
                sumSquaredDiff += Sq(g1.CardTypeScalars[key] - g2.CardTypeScalars[key]);
                paramCount++;
            }

            foreach (var key in g1.CardStarScalars.Keys)
            {
                sumSquaredDiff += Sq(g1.CardStarScalars[key] - g2.CardStarScalars[key]);
                paramCount++;
            }

            foreach (var key in g1.EnemyStarScalars.Keys)
            {
                sumSquaredDiff += Sq(g1.EnemyStarScalars[key] - g2.EnemyStarScalars[key]);
                paramCount++;
            }


            foreach (var key in g1.RoomTypeWeights.Keys)
            {
                sumSquaredDiff += Sq(g1.RoomTypeWeights[key] - g2.RoomTypeWeights[key]);
                paramCount++;
            }
            sumSquaredDiff += Sq(g1.MonsterStarRatio - g2.MonsterStarRatio);
            sumSquaredDiff += Sq(g1.EliteStarRatio - g2.EliteStarRatio);
            sumSquaredDiff += Sq(g1.RestHealingScalar - g2.RestHealingScalar);
            paramCount += 3;


            sumSquaredDiff += Sq(g1.HeroHealthScalar - g2.HeroHealthScalar);
            sumSquaredDiff += Sq(g1.HeroStartGoldScalar - g2.HeroStartGoldScalar);
            sumSquaredDiff += Sq(g1.HeroManaOffset - g2.HeroManaOffset);
            sumSquaredDiff += Sq(g1.DifficultyProgressionRate - g2.DifficultyProgressionRate);
            paramCount += 4;


            return (float)Math.Sqrt(sumSquaredDiff / paramCount);
        }


        public static float CalculatePhenotypeDiversity(List<Individual> population)
        {
            if (population == null || population.Count < 2) return 0f;

            var withFitness = population.Where(ind => ind.Fitness != null).ToList();
            if (withFitness.Count < 2) return 0f;

            float totalDistance = 0f;
            int comparisons = 0;
            int maxComparisons = Math.Min(100, withFitness.Count * (withFitness.Count - 1) / 2);

            for (int i = 0; i < withFitness.Count - 1 && comparisons < maxComparisons; i++)
            {
                for (int j = i + 1; j < withFitness.Count && comparisons < maxComparisons; j++)
                {
                    float distance = withFitness[i].Fitness.DistanceTo(withFitness[j].Fitness);
                    totalDistance += distance;
                    comparisons++;
                }
            }

            return comparisons > 0 ? totalDistance / comparisons : 0f;
        }

        private static float Sq(float x) => x * x;
    }
}
