using System.Collections.Generic;

public interface IGeneticParameter
{
    Dictionary<string, int> GetGenes();

    void SetGenes(Dictionary<string, int> genes);
}