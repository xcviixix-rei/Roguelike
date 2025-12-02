using System.Threading;

/// <summary>
/// Represents a unique, in-play instance of a CardData ScriptableObject.
/// This class holds all runtime-specific data for a card, such as temporary cost
/// modifications or unique identifiers. It acts as a wrapper around the static CardData.
/// </summary>
public class RuntimeCard
{
    /// <summary>
    /// A simple static counter to ensure every card instance gets a unique ID.
    /// Interlocked.Increment is used to make it thread-safe, which is good practice,
    /// though not strictly necessary in a single-threaded Unity game loop.
    /// </summary>
    private static long nextInstanceId = 0;
    
    /// <summary>
    /// The unique identifier for this specific card instance.
    /// </summary>
    public long InstanceID { get; private set; }

    /// <summary>
    /// A reference to the base ScriptableObject that defines this card's permanent properties.
    /// </summary>
    public CardData BaseData { get; private set; }
    
    /// <summary>
    /// The current energy cost to play this card. Can be modified by effects.
    /// </summary>
    public int CurrentEnergyCost { get; set; }
    public int CurrentDamage { get; set; }
    public int CurrentBlock { get; set; }
    
    /// <summary>
    /// Constructor for creating a new runtime instance from a CardData template.
    /// </summary>
    /// <param name="cardData">The ScriptableObject to base this instance on.</param>
    public RuntimeCard(CardData cardData)
    {
        InstanceID = Interlocked.Increment(ref nextInstanceId);
        
        BaseData = cardData;
        
        InitializeFromBaseData();
    }

    /// <summary>
    // Resets any temporary modifications back to the card's base values.
    // This would be called when the card moves to the discard pile, for example.
    /// </summary>
    public void InitializeFromBaseData()
    {
        CurrentEnergyCost = BaseData.energyCost;
    }

    /// <summary>
    /// A helper method to easily access the card's name from its base data.
    /// </summary>
    public string GetName() => BaseData != null ? BaseData.cardName : "Uninitialized Card";
    
    /// <summary>
    /// Gets the dynamic description from the base data. In the future, this could be
    /// modified to reflect runtime changes (e.g., showing increased damage in green text).
    /// </summary>
    public string GetDynamicDescription()
    {
        if (BaseData == null) return "Error: No Card Data";
        
        return BaseData.GetDynamicDescription();
    }
}