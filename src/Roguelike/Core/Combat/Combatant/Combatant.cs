using Roguelike.Data;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Roguelike.Core
{


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

        public void IncreaseMaxHealth(int amount)
        {
            if (amount > 0)
            {
                MaxHealth += amount;
                Heal(amount);
            }
        }


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


        public void TakePiercingDamage(int amount)
        {
            if (amount <= 0) return;

            CurrentHealth -= amount;
            if (CurrentHealth < 0)
            {
                CurrentHealth = 0;
            }
        }


        public void GainBlock(int amount)
        {
            if (amount > 0)
            {
                Block += amount;
            }
        }


        public void ApplyEffect(EffectData effectData)
        {
            var existingEffect = ActiveEffects.FirstOrDefault(e => e.SourceData.Id == effectData.Id);
            if (existingEffect != null)
            {

                if (effectData.Decay == DecayType.AfterXTURNS)
                {
                    existingEffect.RemainingDuration = effectData.Duration;
                }
            }
            else
            {
                ActiveEffects.Add(new ActiveEffect(effectData));
            }
        }

        public void Heal(int amount)
        {
            if (amount > 0)
            {
                CurrentHealth = Math.Min(CurrentHealth + amount, MaxHealth);
            }
        }


        public void TickDownEffects()
        {
            var expiredEffects = new List<ActiveEffect>();
            foreach (var effect in ActiveEffects)
            {
                if (effect.TickDown())
                {
                    expiredEffects.Add(effect);
                }
            }

            foreach (var expired in expiredEffects)
            {
                ActiveEffects.Remove(expired);
            }
        }


        public void ResetForNewCombat()
        {
            Block = 0;
            ActiveEffects.RemoveAll(e => e.SourceData.Decay != DecayType.Permanent);
        }
    }
}
