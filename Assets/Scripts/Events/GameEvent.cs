using System;

/// <summary>
/// A static class that holds all major gameplay events.
/// Systems can subscribe to these events to react to changes in the game state
/// without being directly coupled to the class that causes the change.
/// </summary>
public static class GameEvents
{
    // --- Combatant Events ---
    public static event Action<Combatant, int> OnHealthChanged;
    public static void InvokeHealthChanged(Combatant combatant, int newHP) => OnHealthChanged?.Invoke(combatant, newHP);

    public static event Action<Combatant, int> OnBlockChanged;
    public static void InvokeBlockChanged(Combatant combatant, int newBlock) => OnBlockChanged?.Invoke(combatant, newBlock);

    // --- Player Resource Events ---
    public static event Action<int> OnPlayerEnergyChanged;
    public static void InvokePlayerEnergyChanged(int newEnergy) => OnPlayerEnergyChanged?.Invoke(newEnergy);

    // --- Deck & Card Events ---
    public static event Action<RuntimeCard> OnCardDrawn;
    public static void InvokeCardDrawn(RuntimeCard card) => OnCardDrawn?.Invoke(card);

    // --- Turn Management Events ---
    public static event Action<bool> OnTurnStart; // bool isPlayerTurn
    public static void InvokeTurnStart(bool isPlayerTurn) => OnTurnStart?.Invoke(isPlayerTurn);

    public static event Action<bool> OnCombatEnd; // bool playerWon
    public static void InvokeCombatEnd(bool playerWon) => OnCombatEnd?.Invoke(playerWon);
}