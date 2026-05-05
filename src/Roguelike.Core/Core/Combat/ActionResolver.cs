using Roguelike.Data;
using System;
using System.Linq;

namespace Roguelike.Core
{


    public static class ActionResolver
    {


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


            foreach (var effect in source.ActiveEffects)
            {
                if (effect.SourceData is StatusEffectData s && s.EffectType == StatusEffectType.Strength)
                {
                    if (s.IntensityType == IntensityType.Flat)
                    {
                        finalDamage += s.Intensity;
                    }
                    else
                    {
                        finalDamage *= (1 + s.Intensity / 100f);
                    }
                }
            }


            var weakEffect = source.ActiveEffects.FirstOrDefault(e =>
                e.SourceData is StatusEffectData s && s.EffectType == StatusEffectType.Weakened);

            if (weakEffect != null)
            {
                var weakData = (StatusEffectData)weakEffect.SourceData;
                if (weakData.IntensityType == IntensityType.Percentage)
                {

                    finalDamage *= (1 - weakData.Intensity / 100f);
                }
            }


            var vulEffect = target.ActiveEffects.FirstOrDefault(e =>
                e.SourceData is StatusEffectData s && s.EffectType == StatusEffectType.Vulnerable);

            if (vulEffect != null)
            {
                var vulData = (StatusEffectData)vulEffect.SourceData;
                if (vulData.IntensityType == IntensityType.Percentage)
                {

                    finalDamage *= (1 + vulData.Intensity / 100f);
                }
            }

            int damageInt = (int)Math.Floor(finalDamage);
            if (damageInt < 0) damageInt = 0;


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


            var frailEffect = target.ActiveEffects.FirstOrDefault(e =>
                e.SourceData is StatusEffectData s && s.EffectType == StatusEffectType.Frail);

            if (frailEffect != null)
            {
                var frailData = (StatusEffectData)frailEffect.SourceData;
                if (frailData.IntensityType == IntensityType.Percentage)
                {

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

                            break;
                        case DeckEffectType.DuplicateCard:

                            break;
                    }
                }
            }
        }
    }
}
