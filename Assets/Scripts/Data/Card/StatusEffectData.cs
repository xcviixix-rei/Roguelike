using UnityEngine;
using Sirenix.OdinInspector;

[CreateAssetMenu(menuName = "Data/Status Effect Data")]
public class StatusEffectData : ScriptableObject
{
    [TitleGroup("Core Information")]
    [HorizontalGroup("Core Information/Split", 100, LabelWidth = 100)]
    [PreviewField(100, ObjectFieldAlignment.Left), HideLabel]
    public Sprite icon;

    [VerticalGroup("Core Information/Split/Right")]
    public string effectID;
    [VerticalGroup("Core Information/Split/Right")]
    public string effectName;

    [TitleGroup("Gameplay Properties")]
    public StatusEffectType effectType;
    [TitleGroup("Gameplay Properties")]
    public StatusEffectDecayType decayType;
    [TitleGroup("Gameplay Properties")]
    public bool isStackable; // Can you have more than one instance? (e.g., Poison stacks, Strength stacks)

    [TitleGroup("Description")]
    [TextArea(3, 5)]
    public string description;
}