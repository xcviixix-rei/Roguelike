using System.Collections.Generic;

namespace Roguelike.Optimization
{


    public interface ISimulationRunner
    {
        SimulationStats Run(HierarchicalGenome genome, int seed);
    }
}
