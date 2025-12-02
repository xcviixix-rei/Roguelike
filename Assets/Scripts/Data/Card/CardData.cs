
using Sirenix.OdinInspector;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Data/Card Data")]
public class CardData : ScriptableObject, IGeneticParameter
{
    [TitleGroup("Core Information")]
    [HorizontalGroup("Core Information/Split", 100, LabelWidth = 100)]
    [PreviewField(100, ObjectFieldAlignment.Left), HideLabel]
    public Sprite cardArt;

    [VerticalGroup("Core Information/Split/Right")]
    public string cardID; // Unique identifier
    [VerticalGroup("Core Information/Split/Right")]
    public string cardName;

    [TitleGroup("Card Properties")]
    public CardType cardType;
    [TitleGroup("Card Properties")]
    public CardRarity cardRarity;
    [TitleGroup("Card Properties")]
    public TargetType defaultTarget;

    [TitleGroup("Description")]
    [TextArea(3, 5)]
    public string descriptionTemplate;

    [TitleGroup("Balancing Values")]
    public int energyCost;

    [TitleGroup("Card Effects")]
    [ListDrawerSettings(ListElementLabelName = "GetDescription")]
    [SerializeReference]
    public List<CardEffect> effectsToExecute = new List<CardEffect>();

    [TitleGroup("Upgrades")]
    public CardData upgradedVersion;

    public Dictionary<string, int> GetGenes() 
    {
        var genes = new Dictionary<string, int>();
        genes.Add("EnergyCost", energyCost);
        for (int i = 0; i < effectsToExecute.Count; i++)
        {
            var effectGenes = effectsToExecute[i] as IGeneticParameter;
            if (effectGenes != null)
            {
                var effectGeneDict = effectGenes.GetGenes();
                foreach (var kvp in effectGeneDict)
                {
                    string uniqueKey = $"Effect{i}_{kvp.Key}";
                    genes.Add(uniqueKey, kvp.Value);
                }
            }
        }
        return genes;
    }

    public void SetGenes(Dictionary<string, int> genes)
    {
        if (genes.ContainsKey("EnergyCost"))
            energyCost = genes["EnergyCost"];
    }

    public string GetDynamicDescription()
    {
        if (effectsToExecute == null || effectsToExecute.Count == 0)
            return descriptionTemplate;
        string dynamicDescription = descriptionTemplate;
        for (int i = 0; i < effectsToExecute.Count; i++)
        {
            string placeholder = $"{{Effect{i + 1}}}";
            string effectDesc = effectsToExecute[i].GetDescription();
            dynamicDescription = dynamicDescription.Replace(placeholder, effectDesc);
        }
        return dynamicDescription;
    }

    public string GetDynamicDescriptionByValue()
    {
        if (string.IsNullOrEmpty(descriptionTemplate)) return "Missing Description";

        var descriptionValues = new Dictionary<string, string>();
        foreach (var effect in effectsToExecute)
        {
            effect.GetDescriptionValues(descriptionValues);
        }

        string finalDescription = descriptionTemplate;
        foreach (var entry in descriptionValues)
        {
            finalDescription = finalDescription.Replace(entry.Key, entry.Value);
        }
        
        return finalDescription;
    }
}