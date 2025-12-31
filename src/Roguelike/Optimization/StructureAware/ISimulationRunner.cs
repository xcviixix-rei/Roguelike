using System.Collections.Generic;

namespace Roguelike.Optimization
{
    /// <summary>
    /// Interface for simulation runners that can execute game simulations with a hierarchical genome
    /// </summary>
    public interface ISimulationRunner
    {
        SimulationStats Run(HierarchicalGenome genome, int seed);
    }
}
