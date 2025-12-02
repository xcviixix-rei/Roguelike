using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// A simple test harness to start a combat encounter with pre-defined data.
/// </summary>
public class TestCombatStarter : MonoBehaviour
{
    [Header("References")]
    [Tooltip("The CombatTurnManager in the scene.")]
    [SerializeField] private CombatTurnManager combatTurnManager;

    [Header("Combat Data")]
    [Tooltip("The PlayerData for the character starting combat.")]
    [SerializeField] private PlayerData playerData;
    
    [Tooltip("The list of enemies to fight.")]
    [SerializeField] private List<EnemyData> enemyGroup;
    
    private DeckManager deckManager;

    void Awake()
    {
        deckManager = new DeckManager();
    }

    void Start()
    {
        if (combatTurnManager != null && playerData != null && enemyGroup.Count > 0)
        {
            combatTurnManager.StartCombat(playerData, enemyGroup, deckManager);
        }
        else
        {
            Debug.LogError("TestCombatStarter is missing necessary references to start combat!");
        }
    }
}