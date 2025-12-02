using System;
using UnityEngine;
using Sirenix.OdinInspector;
using System.Collections.Generic;

#region Deal Damage Effect

[Serializable]
public class DealDamageEffect : CardEffect, IGeneticParameter
{
    [Title("Effect Value")]
    public int damageAmount;

    public Dictionary<string, int> GetGenes()
    {
        return new Dictionary<string, int>
        {
            { "DamageAmount", damageAmount }
        };
    }

    public void SetGenes(Dictionary<string, int> genes)
    {
        if (genes.TryGetValue("DamageAmount", out int value))
        {
            damageAmount = value;
        }
    }

    public override string GetDescription()
    {
        return $"Deal {damageAmount} damage";
    }

    public override void GetDescriptionValues(Dictionary<string, string> values)
    {
        values["!D!"] = damageAmount.ToString();
    }

    public override void Execute(CardData sourceCard, Combatant source, Combatant target, DeckManager deckManager)
    {
        target.TakeDamage(damageAmount);
    }
}
#endregion

#region Gain Block Effect
[Serializable]
public class GainBlockEffect : CardEffect, IGeneticParameter
{
    [Title("Effect Value")]
    public int blockAmount;

    public Dictionary<string, int> GetGenes()
    {
        return new Dictionary<string, int>
        {
            { "BlockAmount", blockAmount }
        };
    }

    public void SetGenes(Dictionary<string, int> genes)
    {
        if (genes.TryGetValue("BlockAmount", out int value))
        {
            blockAmount = value;
        }
    }

    public override string GetDescription()
    {
        return $"Gain {blockAmount} Block";
    }

    public override void GetDescriptionValues(Dictionary<string, string> values)
    {
        values["!B!"] = blockAmount.ToString();
    }

    public override void Execute(CardData sourceCard, Combatant source, Combatant target, DeckManager deckManager)
    {
        source.GainBlock(blockAmount);
    }
}
#endregion

#region Apply Status Effect
[Serializable]
public class ApplyStatusEffect : CardEffect, IGeneticParameter
{
    public StatusEffectData statusEffectToApply;
    
    [Title("Effect Value")]
    public int stacksToApply;

    public Dictionary<string, int> GetGenes()
    {
        return new Dictionary<string, int>
        {
            { "StacksToApply", stacksToApply }
        };
    }

    public void SetGenes(Dictionary<string, int> genes)
    {
        if (genes.TryGetValue("StacksToApply", out int value))
        {
            stacksToApply = value;
        }
    }

    public override string GetDescription()
    {
        if (statusEffectToApply == null) return "Apply status (unassigned)";
        return $"Apply {stacksToApply} {statusEffectToApply.effectName}";
    }

    public override void GetDescriptionValues(Dictionary<string, string> values)
    {
        values["!S!"] = stacksToApply.ToString();
    }

    public override void Execute(CardData sourceCard, Combatant source, Combatant target, DeckManager deckManager)
    {
        target.ApplyStatusEffect(statusEffectToApply, stacksToApply);
    }
}
#endregion

#region Draw Cards Effect
[Serializable]
public class DrawCardsEffect : CardEffect, IGeneticParameter
{
    [Title("Effect Value")]
    public int cardsToDraw;

    public Dictionary<string, int> GetGenes()
    {
        return new Dictionary<string, int>
        {
            { "CardsToDraw", cardsToDraw }
        };
    }

    public void SetGenes(Dictionary<string, int> genes)
    {
        if (genes.TryGetValue("CardsToDraw", out int value))
        {
            cardsToDraw = value;
        }
    }

    public override string GetDescription()
    {
        return $"Draw {cardsToDraw} cards";
    }

    public override void GetDescriptionValues(Dictionary<string, string> values)
    {
        values["!C!"] = cardsToDraw.ToString();
    }

    public override void Execute(CardData sourceCard, Combatant source, Combatant target, DeckManager deckManager)
    {
        deckManager.DrawCards(cardsToDraw);
    }
}
#endregion