using Roguelike.Data;
using System;
using System.Linq;

namespace Roguelike.Logic
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
                    ApplyStatusEffect(action.EffectId, action.Value, target, getEffectById);
                    break;

                case ActionType.ApplyDeckEffect:
                    ApplyDeckEffect(action.EffectId, action.Value, target, getEffectById);
                    break;
            }
        }

        private static void ApplyDamage(int baseDamage, Combatant source, Combatant target)
        {
            float finalDamage = baseDamage;

            var strength = source.ActiveEffects.FirstOrDefault(e => e.SourceData is StatusEffectData s && s.EffectType == StatusEffectType.Strength);
            if (strength != null)
            {
                finalDamage += (strength.SourceData.Value * strength.Stacks);
            }

            var weakened = source.ActiveEffects.FirstOrDefault(e => e.SourceData is StatusEffectData s && s.EffectType == StatusEffectType.Weakened);
            if (weakened != null)
            {
                finalDamage *= (weakened.SourceData.Value / 100f);
            }

            var vulnerable = target.ActiveEffects.FirstOrDefault(e => e.SourceData is StatusEffectData s && s.EffectType == StatusEffectType.Vulnerable);
            if (vulnerable != null)
            {
                finalDamage *= (vulnerable.SourceData.Value / 100f);
            }

            int damageInt = (int)Math.Floor(finalDamage);
            if (damageInt < 0) damageInt = 0;

            var pierced = target.ActiveEffects.FirstOrDefault(e => e.SourceData is StatusEffectData s && s.EffectType == StatusEffectType.Pierced);
            
            if (pierced != null)
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

            var frail = target.ActiveEffects.FirstOrDefault(e => e.SourceData is StatusEffectData s && s.EffectType == StatusEffectType.Frail);
            if (frail != null)
            {
                finalBlock *= (frail.SourceData.Value / 100f);
            }

            int blockInt = (int)Math.Floor(finalBlock);
            target.GainBlock(blockInt);
        }

        private static void ApplyStatusEffect(string effectId, int stacks, Combatant target, Func<string, EffectData> getEffectById)
        {
            var effectData = getEffectById(effectId);
            if (effectData != null && effectData is StatusEffectData)
            {
                target.ApplyEffect(effectData, stacks);
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