using System.Collections.Generic;

namespace Roguelike.GA
{
    /// <summary>
    /// Records the raw telemetry data from a single game simulation.
    /// Used by the FitnessEvaluator to calculate aggregate metrics.
    /// </summary>
    public class SimulationStats
    {
        // Outcome
        public bool IsVictory { get; set; }
        public int FinalFloorReached { get; set; }
        public float FinalHPPercent { get; set; }

        // Deck & Build Data
        public List<string> MasterDeckIds { get; set; } = new List<string>();
        public List<string> RelicIds { get; set; } = new List<string>();

        // Pacing & Difficulty Data
        public int ElitesDefeated { get; set; }
        public int ElitesEncountered { get; set; }
        public float TotalDamageTakenAtElites { get; set; }
        
        // Economy
        public int GoldCollected { get; set; }
        public int GoldSpent { get; set; }

        // Combat Data
        public Dictionary<string, int> CardPlayCounts { get; set; } = new Dictionary<string, int>();

        public SimulationStats() { }
    }
}