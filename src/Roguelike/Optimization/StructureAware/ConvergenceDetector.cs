using System;
using System.Collections.Generic;
using System.Linq;

namespace Roguelike.Optimization
{


    public class ConvergenceDetector
    {
        private readonly List<double> _hypervolumeHistory = new();
        private int _noImprovementCount = 0;


        public int MinGenerations { get; set; } = 10;


        public int PatienceGenerations { get; set; } = 5;


        public double ImprovementThreshold { get; set; } = 0.01;


        public bool HasConverged(List<MultiObjectiveFitness> paretoFront, int generation)
        {

            if (generation < MinGenerations)
                return false;


            double hypervolume = CalculateHypervolume(paretoFront);
            _hypervolumeHistory.Add(hypervolume);


            if (generation >= MinGenerations)
            {

                int windowSize = Math.Min(5, _hypervolumeHistory.Count / 2);


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


            return _noImprovementCount >= PatienceGenerations;
        }


        private double CalculateHypervolume(List<MultiObjectiveFitness> paretoFront)
        {
            if (paretoFront == null || !paretoFront.Any())
                return 0.0;


            const float refBalance = 0.3f;
            const float refEngagement = 0.3f;
            const float refCoherence = 0.3f;

            double volume = 0.0;

            foreach (var fitness in paretoFront)
            {

                double contribution =
                    Math.Max(0, fitness.BalanceScore - refBalance) *
                    Math.Max(0, fitness.EngagementScore - refEngagement) *
                    Math.Max(0, fitness.CoherenceScore - refCoherence);

                volume += contribution;
            }

            return volume;
        }


        public (double currentHypervolume, double bestHypervolume, int noImprovementCount) GetStatistics()
        {
            double current = _hypervolumeHistory.Any() ? _hypervolumeHistory.Last() : 0.0;
            double best = _hypervolumeHistory.Any() ? _hypervolumeHistory.Max() : 0.0;
            return (current, best, _noImprovementCount);
        }


        public void Reset()
        {
            _hypervolumeHistory.Clear();
            _noImprovementCount = 0;
        }
    }
}
