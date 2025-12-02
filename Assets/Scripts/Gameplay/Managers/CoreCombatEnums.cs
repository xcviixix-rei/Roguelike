/// <summary>
/// Defines the distinct states that a combat encounter can be in.
/// </summary>
public enum CombatState
{
    /// <summary>
    /// The combat is being set up. Characters are being spawned and initialized.
    /// </summary>
    Setup,

    /// <summary>
    /// It is the player's turn to act.
    /// </summary>
    PlayerTurn,

    /// <summary>
    /// The enemies are performing their actions.
    /// </summary>
    EnemyTurn,
    
    /// <summary>
    /// The combat has ended with the player winning.
    /// </summary>
    Victory,

    /// <summary>
    /// The combat has ended with the player losing.
    /// </summary>
    Defeat
}