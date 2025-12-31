using Roguelike.Data;
using System;
using System.Linq;

namespace Roguelike.Core
{
    /// <summary>
    /// A static utility class responsible for calculating and applying combat actions.
    /// It handles the math for Damage (Strength, Vulnerable) and Block (Frail),
    /// as well as the application of status and deck effects.
    /// </summary>
    public static class ActionResolver
    {
        /// <summary>
        /// Resolves a single action against a specific target.
        /// </summary>
        /// <param name="action">The action data to execute.</param>
        /// <param name="source">The combatant performing the action.</param>
        /// <param name="target">The combatant receiving the action.</param>
        /// <param name="getEffectById">A function/delegate to look up EffectData by its string ID.</param>
        public static void Resolve(CombatActionData action, Combatant source, Combatant target, Func<string, EffectData> getEffectById)
        {
            switch (action.Type)
            {
                case ActionType.DealDamage:
                    ApplyDamage(action.Value, source, target);
                    break;

                case ActionType.GainBlock:
                    ApplyBlock(action.Value, source, target);
                    break;

                case ActionType.ApplyStatusEffect:
                    ApplyStatusEffect(action.EffectId, target, getEffectById);
                    break;

                case ActionType.ApplyDeckEffect:
                    ApplyDeckEffect(action.EffectId, action.Value, target, getEffectById);
                    break;
            }
        }

        private static void ApplyDamage(int baseDamage, Combatant source, Combatant target)
        {
            float finalDamage = baseDamage;

            // Apply all Strength effects
            foreach (var effect in source.ActiveEffects)
            {
                if (effect.SourceData is StatusEffectData s && s.EffectType == StatusEffectType.Strength)
                {
                    if (s.IntensityType == IntensityType.Flat)
                    {
                        finalDamage += s.Intensity;
                    }
                    else // Percentage
                    {
                        finalDamage *= (1 + s.Intensity / 100f);
                    }
                }
            }

            // Check for Weakened (reduces outgoing damage)
            var weakEffect = source.ActiveEffects.FirstOrDefault(e => 
                e.SourceData is StatusEffectData s && s.EffectType == StatusEffectType.Weakened);
            
            if (weakEffect != null)
            {
                var weakData = (StatusEffectData)weakEffect.SourceData;
                if (weakData.IntensityType == IntensityType.Percentage)
                {
                    // Intensity represents reduction percentage (e.g., 25 = -25% damage = 0.75x multiplier)
                    finalDamage *= (1 - weakData.Intensity / 100f);
                }
            }

            // Check for Vulnerable (increases incoming damage)
            var vulEffect = target.ActiveEffects.FirstOrDefault(e => 
                e.SourceData is StatusEffectData s && s.EffectType == StatusEffectType.Vulnerable);
                
            if (vulEffect != null)
            {
                var vulData = (StatusEffectData)vulEffect.SourceData;
                if (vulData.IntensityType == IntensityType.Percentage)
                {
                    // Intensity represents increase percentage (e.g., 50 = +50% damage = 1.5x multiplier)
                    finalDamage *= (1 + vulData.Intensity / 100f);
                }
            }

            int damageInt = (int)Math.Floor(finalDamage);
            if (damageInt < 0) damageInt = 0;

            // Check if target has Pierced effect
            bool hasPierced = target.ActiveEffects.Any(e => 
                e.SourceData is StatusEffectData s && s.EffectType == StatusEffectType.Pierced);
            
            if (hasPierced)
            {
                target.TakePiercingDamage(damageInt);
            }
            else
            {
                target.TakeDamage(damageInt);
            }
        }

        private static void ApplyBlock(int baseBlock, Combatant source, Combatant target)
        {
            float finalBlock = baseBlock;

            // Check for Frail (reduces block gained)
            var frailEffect = target.ActiveEffects.FirstOrDefault(e => 
                e.SourceData is StatusEffectData s && s.EffectType == StatusEffectType.Frail);

            if (frailEffect != null)
            {
                var frailData = (StatusEffectData)frailEffect.SourceData;
                if (frailData.IntensityType == IntensityType.Percentage)
                {
                    // Intensity represents reduction percentage (e.g., 25 = -25% block = 0.75x multiplier)
                    finalBlock *= (1 - frailData.Intensity / 100f);
                }
            }

            int blockInt = (int)Math.Floor(finalBlock);
            target.GainBlock(blockInt);
        }

        private static void ApplyStatusEffect(string effectId, Combatant target, Func<string, EffectData> getEffectById)
        {
            var effectData = getEffectById(effectId);
            if (effectData != null && effectData is StatusEffectData)
            {
                target.ApplyEffect(effectData);
            }
        }

        private static void ApplyDeckEffect(string effectId, int value, Combatant target, Func<string, EffectData> getEffectById)
        {
            if (target is Hero hero)
            {
                var effectData = getEffectById(effectId);
                if (effectData != null && effectData is DeckEffectData deckEffect)
                {
                    switch (deckEffect.EffectType)
                    {
                        case DeckEffectType.DrawCard:
                            hero.Deck.DrawCards(value);
                            break;
                        case DeckEffectType.DiscardCard:
                            if (hero.Deck.Hand.Count > 0)
                            {
                                var card = hero.Deck.Hand[0]; 
                                hero.Deck.DiscardCardFromHand(card);
                            }
                            break;
                        case DeckEffectType.FreezeCard:
                            // TODO: Implement card freezing logic
                            break;
                        case DeckEffectType.DuplicateCard:
                            // TODO: Implement card duplication logic
                            break;
                    }
                }
            }
        }
    }
}
