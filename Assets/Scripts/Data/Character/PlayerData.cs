using UnityEngine;
using Sirenix.OdinInspector;

[CreateAssetMenu(menuName = "Data/Player Data")]
public class PlayerData : ScriptableObject
{
    [Title("Character Class")]
    public string characterClassName;
    public GameObject playerPrefab;

    // --- GENETIC ALGORITHM WILL TUNE THESE VALUES ---
    [TitleGroup("Balancing Values (The 'Genes')")]
    [BoxGroup("Balancing Values (The 'Genes')/Stats")]
    public int startingHP;
    [BoxGroup("Balancing Values (The 'Genes')/Stats")]
    public int startingEnergyPerTurn;
    [BoxGroup("Balancing Values (The 'Genes')/Stats")]
    public int startingGold;
    // ------------------------------------------------

    [Title("Starting Inventory")]
    [InfoBox("Drag StartingDeck assets here to define the character's starting deck.")]
    public StartingDeck startingDeck;

    [Title("All Available Cards")]
    public AllCards allCards;

    [Title("Starting Relic")]
    public RelicData startingRelic;
}