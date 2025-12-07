using Roguelike.Data;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Roguelike.Logic
{
    /// <summary>
    /// An abstract base class for any entity that participates in combat (Heroes and Enemies).
    /// This class holds the runtime state of a combatant.
    /// </summary>
    public abstract class Combatant
    {
        public CombatantData SourceData { get; }
        public int CurrentHealth { get; protected set; }
        public int MaxHealth { get; protected set; }
        public int Block { get; protected set; }
        public List<ActiveEffect> ActiveEffects { get; } = new List<ActiveEffect>();

        protected Combatant(CombatantData sourceData)
        {
            SourceData = sourceData;
            MaxHealth = sourceData.StartingHealth;
            CurrentHealth = MaxHealth;
            Block = 0;
        }

        /// <summary>
        /// Applies damage to the combatant, first reducing block and then health.
        /// </summary>
        public void TakeDamage(int amount)
        {
            if (amount <= 0) return;

            int damageToBlock = Math.Min(amount, Block);
            Block -= damageToBlock;

            int remainingDamage = amount - damageToBlock;
            if (remainingDamage > 0)
            {
                CurrentHealth -= remainingDamage;
                if (CurrentHealth < 0)
                {
                    CurrentHealth = 0;
                }
            }
        }

        /// <summary>
        /// Applies damage that bypasses block, directly reducing health.
        /// </summary>
        /// <param name="amount"></param>
        public void TakePiercingDamage(int amount)
        {
            if (amount <= 0) return;

            CurrentHealth -= amount;
            if (CurrentHealth < 0)
            {
                CurrentHealth = 0;
            }
        }

        /// <summary>
        /// Adds block to the combatant.
        /// </summary>
        public void GainBlock(int amount)
        {
            if (amount > 0)
            {
                Block += amount;
            }
        }

        /// <summary>
        /// Applies a new effect or adds stacks to an existing one.
        /// </summary>
        public void ApplyEffect(EffectData effectData, int stacks)
        {
            var existingEffect = ActiveEffects.FirstOrDefault(e => e.SourceData.Id == effectData.Id);
            if (existingEffect != null)
            {
                existingEffect.Stacks += stacks;
                if (existingEffect.SourceData.Decay == DecayType.AfterXTURNS)
                {
                    existingEffect.Duration += stacks;
                }
            }
            else
            {
                ActiveEffects.Add(new ActiveEffect(effectData, stacks));
            }
        }

        public void Heal(int amount)
        {
            if (amount > 0)
            {
                CurrentHealth = Math.Min(CurrentHealth + amount, MaxHealth);
            }
        }

        /// <summary>
        /// Processes all active effects at the end of a turn, removing expired ones.
        /// </summary>
        public void TickDownEffects()
        {
            var expiredEffects = new List<ActiveEffect>();
            foreach (var effect in ActiveEffects)
            {
                if (effect.SourceData.Decay == DecayType.EndOfTurn)
                {
                    expiredEffects.Add(effect);
                }
                else if (effect.TickDown())
                {
                    expiredEffects.Add(effect);
                }
            }

            foreach (var expired in expiredEffects)
            {
                ActiveEffects.Remove(expired);
            }
        }
        
        /// <summary>
        /// Resets combat-specific state (like block) and temporary effects.
        /// </summary>
        public void ResetForNewCombat()
        {
            Block = 0;
            ActiveEffects.RemoveAll(e => e.SourceData.Decay != DecayType.Permanent);
        }
    }
}