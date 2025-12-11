using System.Collections.Generic;
using System.Linq;

namespace Roguelike.GA
{
    public class CardViabilityInfo
    {
        public string CardId { get; set; }
        public float PickRate { get; set; }      // % of runs where this card was in the final deck
        public float WinRateWhenPicked { get; set; } // % of wins with this card (when picked)
        public float AvgPlayCount { get; set; }  // Average times played per run (when picked)
        public bool IsTrapCard { get; set; }
        public bool IsMustPick { get; set; }
    }
    
    public class EvaluationReport
    {
        public int TotalRuns { get; set; }
        public float OverallFitness { get; set; }
        
        public float WinRate { get; set; }
        public float AvgHpOnVictory { get; set; }
        public Dictionary<int, int> FloorOfDeathHistogram { get; set; } = new Dictionary<int, int>();

        public List<CardViabilityInfo> CardViability { get; set; } = new List<CardViabilityInfo>();

        public float ShannonEntropy { get; set; } // 0-1, higher = better diversity
        public float GiniCoefficient { get; set; } // 0-1, lower = more inequality
        public float EffectiveNumberOfCards { get; set; } // How many cards are "actually viable"
        public int TotalUniqueCardsPicked { get; set; }
        public int UnusedCards { get; set; }

        public List<CardViabilityInfo> GetTrapCards(float winRateThreshold = 0.35f, float pickRateThreshold = 0.15f)
        {
            return CardViability.Where(c => c.PickRate > pickRateThreshold && c.WinRateWhenPicked < winRateThreshold).ToList();
        }
        
        public List<CardViabilityInfo> GetDudCards(float pickRateThreshold = 0.05f)
        {
            return CardViability.Where(c => c.PickRate < pickRateThreshold).ToList();
        }

        public List<CardViabilityInfo> GetMustPickCards(float winRateThreshold = 0.70f, float pickRateThreshold = 0.50f)
        {
            return CardViability.Where(c => c.PickRate > pickRateThreshold && c.WinRateWhenPicked > winRateThreshold).ToList();
        }

        public List<CardViabilityInfo> GetBalancedCards(float minWinRate = 0.40f, float maxWinRate = 0.60f, float minPickRate = 0.10f)
        {
            return CardViability.Where(c => 
                c.PickRate >= minPickRate && 
                c.WinRateWhenPicked >= minWinRate && 
                c.WinRateWhenPicked <= maxWinRate
            ).ToList();
        }
    }
}