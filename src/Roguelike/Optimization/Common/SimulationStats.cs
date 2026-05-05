using System.Collections.Generic;

namespace Roguelike.Optimization
{


    public class SimulationStats
    {

        public bool IsVictory { get; set; }
        public int FinalFloorReached { get; set; }
        public float FinalHPPercent { get; set; }


        public List<string> MasterDeckIds { get; set; } = new List<string>();
        public List<string> RelicIds { get; set; } = new List<string>();


        public int ElitesDefeated { get; set; }
        public int ElitesEncountered { get; set; }
        public float TotalDamageTakenAtElites { get; set; }


        public int GoldCollected { get; set; }
        public int GoldSpent { get; set; }


        public Dictionary<string, int> CardPlayCounts { get; set; } = new Dictionary<string, int>();

        public SimulationStats() { }
    }
}
