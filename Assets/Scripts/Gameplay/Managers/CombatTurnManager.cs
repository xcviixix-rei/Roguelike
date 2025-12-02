using UnityEngine;
using Sirenix.OdinInspector;
using System.Collections.Generic;
using System.Collections;

public class CombatTurnManager : MonoBehaviour
{
    [Title("Combat State")]
    [ShowInInspector, ReadOnly]
    private CombatState currentState;
    public CombatState CurrentState => currentState;

    [Title("Participants")]
    [ShowInInspector, ReadOnly]
    private Combatant player;
    [ShowInInspector, ReadOnly]
    private List<Combatant> enemies = new List<Combatant>();
    [ShowInInspector, ReadOnly]
    private DeckManager deckManager;
    
    [Title("Configuration")]
    [SerializeField] private int startingCardsPerHand = 5;
    
    [Title("Scene References")]
    [SerializeField] private Transform playerSpawnPoint;
    [SerializeField] private List<Transform> enemySpawnPoints;
    
    [Title("Turn Management")]
    [ShowInInspector, ReadOnly]
    private int turnNumber = 0;
    [ShowInInspector, ReadOnly]
    private int playerEnergy;
    private int maxPlayerEnergy;

    private void ChangeState(CombatState newState)
    {
        if (currentState == newState) return;
        currentState = newState;
        Debug.Log($"<color=yellow>Combat State changed to: {newState}</color>");
    }
    
    #region Combat Flow

    public void StartCombat(PlayerData playerData, List<EnemyData> enemyGroup, DeckManager playerDeckManager)
    {
        Debug.Log("--- STARTING COMBAT ---");
        ChangeState(CombatState.Setup);

        deckManager = playerDeckManager;
        
        GameObject playerObj = Instantiate(playerData.playerPrefab, playerSpawnPoint.position, Quaternion.identity);
        player = playerObj.GetComponent<Combatant>();
        player.Initialize(playerData);
        maxPlayerEnergy = playerData.startingEnergyPerTurn;

        enemies.Clear();
        for (int i = 0; i < enemyGroup.Count; i++)
        {
            if (i >= enemySpawnPoints.Count)
            {
                Debug.LogError("Not enough enemy spawn points for the given enemy group!");
                break;
            }
            EnemyData enemyData = enemyGroup[i];
            GameObject enemyObj = Instantiate(enemyData.enemyPrefab, enemySpawnPoints[i].position, Quaternion.identity);
            Combatant enemy = enemyObj.GetComponent<Combatant>();
            enemy.Initialize(enemyData);
            enemies.Add(enemy);
        }

        deckManager.Initialize(playerData.startingDeck.cards);
        
        BeginPlayerTurn();
    }

    private void BeginPlayerTurn()
    {
        ChangeState(CombatState.PlayerTurn);
        turnNumber++;
        Debug.Log($"<color=cyan>--- Turn {turnNumber} (Player) ---</color>");

        playerEnergy = maxPlayerEnergy;
        GameEvents.InvokePlayerEnergyChanged(playerEnergy);
        
        player.ResetBlock();

        player.TickDownStatusEffects(StatusEffectDecayType.OnTurnEnd);
        foreach (var enemy in enemies)
        {
            enemy.TickDownStatusEffects(StatusEffectDecayType.OnTurnEnd);
        }
        
        GameEvents.InvokeTurnStart(true);
        deckManager.DrawCards(startingCardsPerHand);
    }

    /// <summary>
    /// Kicks off the enemy turn by starting the execution coroutine.
    /// </summary>
    private void BeginEnemyTurn()
    {
        ChangeState(CombatState.EnemyTurn);
        Debug.Log("<color=orange>--- Enemy Turn ---</color>");

        deckManager.DiscardHand();
        GameEvents.InvokeTurnStart(false);

        foreach (var enemy in enemies)
        {
            enemy.ResetBlock();
        }

        StartCoroutine(ExecuteEnemyTurn());
    }

    /// <summary>
    /// Coroutine that iterates through each enemy and executes their chosen move with a delay.
    /// </summary>
    private IEnumerator ExecuteEnemyTurn()
    {
        yield return new WaitForSeconds(0.2f);

        foreach (var enemy in enemies)
        {
            if (enemy.CurrentHP <= 0) continue;

            EnemyMove currentMove = enemy.GetNextMove();

            if (currentMove != null)
            {
                Debug.Log($"{enemy.CharacterName} uses '{currentMove.moveType}' for {currentMove.value}.");
                
                // TODO: Show enemy intent UI

                switch (currentMove.moveType)
                {
                    case EnemyMove.MoveType.Attack:
                        player.TakeDamage(currentMove.value);
                        break;
                    case EnemyMove.MoveType.Defend:
                        enemy.GainBlock(currentMove.value);
                        break;
                    case EnemyMove.MoveType.Buff:
                        Debug.Log("Enemy used a Buff (not yet implemented).");
                        // e.g., enemy.ApplyStatusEffect(BuffData, move.value);
                        break;
                    case EnemyMove.MoveType.Debuff:
                        Debug.Log("Enemy used a Debuff (not yet implemented).");
                        // e.g., player.ApplyStatusEffect(DebuffData, move.value);
                        break;
                }
            }

            yield return new WaitForSeconds(1.0f);
        }

        EndTurnCycle();
    }
    
    public void EndPlayerTurn()
    {
        if (currentState != CombatState.PlayerTurn)
        {
            Debug.LogWarning("Tried to end player turn, but it's not the player's turn.");
            return;
        }
        
        BeginEnemyTurn();
    }

    private void EndTurnCycle()
    {
        CheckForCombatEnd();
        if (currentState == CombatState.Victory || currentState == CombatState.Defeat)
        {
            return;
        }

        BeginPlayerTurn();
    }

    private void HandleVictory()
    {
        ChangeState(CombatState.Victory);
        Debug.Log("<color=green>--- VICTORY ---</color>");
        GameEvents.InvokeCombatEnd(true);
    }

    private void HandleDefeat()
    {
        ChangeState(CombatState.Defeat);
        Debug.Log("<color=red>--- DEFEAT ---</color>");
        GameEvents.InvokeCombatEnd(false);
    }

    #endregion

    #region Player Actions

    /// <summary>
    /// Processes the player's attempt to play a card on a target.
    /// This is the primary method for player interaction during their turn.
    /// </summary>
    /// <param name="card">The specific instance of the card being played.</param>
    /// <param name="target">The combatant being targeted by the card.</param>
    public void PlayerPlaysCard(RuntimeCard card, Combatant target)
    {
        if (currentState != CombatState.PlayerTurn)
        {
            Debug.LogWarning("Cannot play card: Not the player's turn.");
            return;
        }
        if (card == null)
        {
            Debug.LogError("Cannot play card: Card is null.");
            return;
        }
        if (target == null)
        {
            Debug.LogError("Cannot play card: Target is null.");
            return;
        }
        if (playerEnergy < card.CurrentEnergyCost)
        {
            Debug.Log($"Cannot play card: Not enough energy. Need {card.CurrentEnergyCost}, have {playerEnergy}.");
            // TODO: Show some UI feedback for insufficient energy
            return;
        }
        
        playerEnergy -= card.CurrentEnergyCost;
        GameEvents.InvokePlayerEnergyChanged(playerEnergy);
        
        deckManager.PlayCard(card);
        Debug.Log($"Player played {card.GetName()} on {target.CharacterName} for {card.CurrentEnergyCost} energy.");

        foreach (var effect in card.BaseData.effectsToExecute)
        {
            effect.Execute(card.BaseData, player, target, deckManager);
        }

        CheckForCombatEnd();
    }
    
    private void CheckForCombatEnd()
    {
        enemies.RemoveAll(e => e.CurrentHP <= 0);

        if (player.CurrentHP <= 0)
        {
            HandleDefeat();
        }
        else if (enemies.Count == 0)
        {
            HandleVictory();
        }
    }

    #endregion
}