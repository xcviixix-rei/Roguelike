using System;
using UnityEngine;
using Sirenix.OdinInspector;
using System.Collections.Generic;

[Serializable]
public abstract class CardEffect
{
    [HideInInspector] 
    public virtual string descriptionForEditor => "Base Card Effect";

    public virtual string GetDescription()
    {
        return "No effect.";
    }

    public virtual void GetDescriptionValues(Dictionary<string, string> values) { }

    public abstract void Execute(CardData sourceCard, Combatant source, Combatant target, DeckManager deckManager);
}