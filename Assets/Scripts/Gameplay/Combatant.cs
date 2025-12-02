using UnityEngine;
using Sirenix.OdinInspector;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Represents a single entity (player or enemy) in combat. This class holds all the
/// *runtime* state that can change during a fight, such as current health, block, and status effects.
/// It is initialized from a ScriptableObject data container (PlayerData or EnemyData).
/// </summary>
public class Combatant : MonoBehaviour
{
    [Title("Data Source")]
    [Tooltip("The ScriptableObject that provided the initial stats for this combatant.")]
    [SerializeField, ReadOnly]
    private ScriptableObject sourceData;

    [Title("Runtime Stats")]
    [ProgressBar(0, "MaxHP", ColorGetter = "GetHealthBarColor")]
    [SerializeField]
    private int currentHP;

    [SerializeField]
    private int currentBlock;

    [Title("Runtime Status Effects")]
    [ShowInInspector, ReadOnly]
    private readonly List<ActiveStatusEffect> activeStatusEffects = new List<ActiveStatusEffect>();
    public IReadOnlyList<ActiveStatusEffect> ActiveStatusEffects => activeStatusEffects;

    public int MaxHP { get; private set; }
    public int CurrentHP => currentHP;
    public int CurrentBlock => currentBlock;
    
    public int Strength { get; private set; }
    public int Dexterity { get; private set; }

    public bool IsPlayer { get; private set; }
    public string CharacterName { get; private set; }

    #region Initialization
    
    public void Initialize(PlayerData data)
    {
        sourceData = data;
        IsPlayer = true;
        CharacterName = data.characterClassName;
        
        MaxHP = data.startingHP;
        currentHP = data.startingHP;
        currentBlock = 0;

        Strength = 0;
        Dexterity = 0;
        
        gameObject.name = $"Player - {CharacterName}";
    }
    
    public void Initialize(EnemyData data)
    {
        sourceData = data;
        IsPlayer = false;
        CharacterName = data.enemyName;
        
        MaxHP = data.maxHP;
        currentHP = data.maxHP;
        currentBlock = 0;

        Strength = data.startingStrength;
        Dexterity = 0;

        gameObject.name = $"Enemy - {CharacterName}";
    }

    #endregion

    #region Combat Actions

    public void TakeDamage(int damageAmount)
    {
        if (damageAmount <= 0) return;

        int damageAbsorbedByBlock = Mathf.Min(currentBlock, damageAmount);
        int remainingDamage = damageAmount - damageAbsorbedByBlock;

        if (damageAbsorbedByBlock > 0)
        {
            currentBlock -= damageAbsorbedByBlock;
            Debug.Log($"{CharacterName} blocked {damageAbsorbedByBlock} damage.");
            // TODO (Phase 4): Invoke GameEvents.OnBlockChanged(this, currentBlock);
        }

        if (remainingDamage > 0)
        {
            currentHP -= remainingDamage;
            Debug.Log($"{CharacterName} took {remainingDamage} damage to health.");
            TickDownStatusEffects(StatusEffectDecayType.OnDamageTaken);
            // TODO (Phase 4): Invoke GameEvents.OnHealthChanged(this, currentHP);
        }
        
        if (currentHP < 0)
        {
            currentHP = 0;
        }

        if (currentHP == 0)
        {
            Die();
        }
    }

    public void GainBlock(int blockAmount)
    {
        if (blockAmount <= 0) return;

        currentBlock += blockAmount;
        Debug.Log($"{CharacterName} gained {blockAmount} block. Total: {currentBlock}");
        // TODO (Phase 4): Invoke GameEvents.OnBlockChanged(this, currentBlock);
    }

    public void Heal(int healAmount)
    {
        if (healAmount <= 0) return;

        currentHP += healAmount;
        if (currentHP > MaxHP)
        {
            currentHP = MaxHP;
        }
        
        Debug.Log($"{CharacterName} healed for {healAmount}. Current HP: {currentHP}");
        // TODO (Phase 4): Invoke GameEvents.OnHealthChanged(this, currentHP);
    }

    public void ResetBlock()
    {
        if (currentBlock > 0)
        {
            currentBlock = 0;
            Debug.Log($"{CharacterName}'s block was reset.");
            // TODO (Phase 4): Invoke GameEvents.OnBlockChanged(this, currentBlock);
        }
    }
    
    private void Die()
    {
        Debug.LogWarning($"{CharacterName} has died!");
        // TODO: Add logic for death animations, removing from combat, etc.
    }

    #endregion

    #region Status Effects

    public void ApplyStatusEffect(StatusEffectData data, int stacks)
    {
        if (data == null || stacks <= 0) return;

        var existingEffect = activeStatusEffects.FirstOrDefault(e => e.Data == data);

        if (existingEffect != null)
        {
            existingEffect.Stacks += stacks;
            Debug.Log($"{CharacterName} gained {stacks} more stacks of {data.effectName}. Total: {existingEffect.Stacks}");
        }
        else
        {
            var newEffect = new ActiveStatusEffect(data, stacks, this);
            activeStatusEffects.Add(newEffect);
            Debug.Log($"{CharacterName} received {stacks} stacks of {data.effectName}.");
        }
        
        // TODO (Phase 4): Invoke GameEvents.OnStatusEffectApplied(this, data, stacks);
    }

    public void TickDownStatusEffects(StatusEffectDecayType decayType)
    {
        for (int i = activeStatusEffects.Count - 1; i >= 0; i--)
        {
            var effect = activeStatusEffects[i];
            if (effect.Data.decayType == decayType)
            {
                effect.Stacks--;
                Debug.Log($"{CharacterName}'s {effect.Data.effectName} reduced to {effect.Stacks}.");
                // TODO (Phase 4): Invoke GameEvents.OnStatusEffectTicked(effect);

                if (effect.Stacks <= 0)
                {
                    activeStatusEffects.RemoveAt(i);
                    Debug.Log($"{effect.Data.effectName} has expired on {CharacterName}.");
                    // TODO (Phase 4): Invoke GameEvents.OnStatusEffectRemoved(effect);
                }
            }
        }
    }

    #endregion

    private Color GetHealthBarColor()
    {
        float healthPercent = (float)currentHP / MaxHP;
        if (healthPercent > 0.5f) return Color.green;
        if (healthPercent > 0.25f) return Color.yellow;
        return Color.red;
    }
}