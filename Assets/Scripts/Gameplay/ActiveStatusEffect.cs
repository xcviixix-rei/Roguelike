/// <summary>
/// A runtime instance of a status effect applied to a Combatant.
/// This is a plain C# class that holds the state of an effect during combat.
/// </summary>
public class ActiveStatusEffect
{
    /// <summary>
    /// The ScriptableObject that defines the behavior of this effect.
    /// </summary>
    public StatusEffectData Data { get; private set; }

    /// <summary>
    /// The number of stacks or turns remaining for this effect.
    /// </summary>
    public int Stacks { get; set; }

    /// <summary>
    /// The Combatant that this effect is applied to.
    /// </summary>
    public Combatant Target { get; private set; }

    /// <summary>
    /// Constructor to create a new active status effect.
    /// </summary>
    /// <param name="data">The SO defining the effect.</param>
    /// <param name="stacks">The initial number of stacks/turns.</param>
    /// <param name="target">The combatant this is applied to.</param>
    public ActiveStatusEffect(StatusEffectData data, int stacks, Combatant target)
    {
        Data = data;
        Stacks = stacks;
        Target = target;
    }
}