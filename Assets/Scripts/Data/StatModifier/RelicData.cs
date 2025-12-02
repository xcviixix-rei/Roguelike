using UnityEngine;
using Sirenix.OdinInspector;

[CreateAssetMenu(menuName = "Data/Relic Data")]
public class RelicData : ScriptableObject
{
    [TitleGroup("Core Information")]
    [HorizontalGroup("Core Information/Split", 100, LabelWidth = 100)]
    [PreviewField(100, ObjectFieldAlignment.Left), HideLabel]
    public Sprite icon;

    [VerticalGroup("Core Information/Split/Right")]
    public string relicID;
    [VerticalGroup("Core Information/Split/Right")]
    public string relicName;
    
    [Title("Description")]
    [TextArea(3, 5)]
    public string description;
}