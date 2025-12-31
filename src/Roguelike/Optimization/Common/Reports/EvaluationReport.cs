public class CardViabilityInfo
{
    public string CardId { get; set; }
    public float PickRate { get; set; }
    public float WinRateWhenPicked { get; set; }
    public float AvgPlayCount { get; set; }
    public bool IsTrapCard { get; set; }
    public bool IsMustPick { get; set; }
    public bool IsBalanced { get; set; }
}

public class EvaluationReport
{
    // Basic Metrics
    public int TotalRuns { get; set; }
    public float OverallFitness { get; set; }
    public float WinRate { get; set; }
    public float AvgHpOnVictory { get; set; }
    public Dictionary<int, int> FloorOfDeathHistogram { get; set; } = new Dictionary<int, int>();
    public float AvgFloorOnDeath { get; set; }

    // Elite Metrics
    public float AvgElitesDefeated { get; set; }
    public float AvgElitesEncountered { get; set; }
    public float EliteKillRate { get; set; }
    public float AvgDamagePerElite { get; set; }

    // Economy Metrics
    public float AvgGoldCollected { get; set; }
    public float AvgGoldSpent { get; set; }
    public float AvgGoldEfficiency { get; set; }

    // Card Viability
    public List<CardViabilityInfo> CardViability { get; set; } = new List<CardViabilityInfo>();
    
    // Diversity Metrics
    public float ShannonEntropy { get; set; }
    public float GiniCoefficient { get; set; }
    public float EffectiveNumberOfCards { get; set; }
    public int TotalUniqueCardsPicked { get; set; }
    public int ViableCards { get; set; }
    public int BalancedCards { get; set; }
    public int UnusedCards { get; set; }
    public float BuildVarietyScore { get; set; }

    // Consistency
    public float FloorConsistency { get; set; }

    // Quality Flags
    public bool HasCriticalIssues { get; set; }
    public bool HasBalanceIssues { get; set; }
    public float QualityScore { get; set; }

    // Helper Methods
    public List<CardViabilityInfo> GetTrapCards(float winRateThreshold = 0.30f, float pickRateThreshold = 0.10f)
    {
        return CardViability.Where(c => 
            c.PickRate > pickRateThreshold && 
            c.WinRateWhenPicked < winRateThreshold
        ).ToList();
    }

    public List<CardViabilityInfo> GetDudCards(float pickRateThreshold = 0.05f)
    {
        return CardViability.Where(c => c.PickRate < pickRateThreshold).ToList();
    }

    public List<CardViabilityInfo> GetMustPickCards(float winRateThreshold = 0.55f, float pickRateThreshold = 0.40f)
    {
        return CardViability.Where(c => 
            c.PickRate > pickRateThreshold && 
            c.WinRateWhenPicked > winRateThreshold
        ).ToList();
    }

    public List<CardViabilityInfo> GetBalancedCards()
    {
        return CardViability.Where(c => c.IsBalanced).ToList();
    }

    public string GetSummary()
    {
        return $@"
=== EVALUATION REPORT ===
Overall Fitness: {OverallFitness:F2}
Quality Score: {QualityScore:F2}/100

CORE METRICS:
- Win Rate: {WinRate:P1} (Target: 45%)
- Avg Victory HP: {AvgHpOnVictory:P1} (Target: 30%)
- Avg Floor on Death: {AvgFloorOnDeath:F1} (Target: 8)

CARD DIVERSITY:
- Viable Cards (>10% pick): {ViableCards}
- Balanced Cards: {BalancedCards}
- Trap Cards: {GetTrapCards().Count}
- Build Variety: {BuildVarietyScore:F2}

ELITE PERFORMANCE:
- Elite Kill Rate: {EliteKillRate:P1}
- Avg Damage/Elite: {AvgDamagePerElite:F1}

FLAGS:
{(HasCriticalIssues ? "⚠ CRITICAL ISSUES DETECTED" : "✓ No Critical Issues")}
{(HasBalanceIssues ? "⚠ Balance Issues Present" : "✓ Good Balance")}
";
    }
}
