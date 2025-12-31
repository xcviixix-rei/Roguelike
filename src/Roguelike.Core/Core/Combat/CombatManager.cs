using Roguelike.Data;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Roguelike.Core
{
    public enum CombatState
    {
        Ongoing_PlayerTurn,
        Ongoing_EnemyTurn,
        Victory,
        Defeat
    }

    public class CombatManager
    {
        public Hero TheHero { get; }
        public List<Enemy> Enemies { get; }
        public int TurnNumber { get; private set; }
        public CombatState State { get; private set; }

        public event Action<CardData> OnCardPlayed;

        private readonly Random rng;
        private readonly Func<string, EffectData> getEffectById;
        
        private int cardsPlayedThisTurn;
        private int attacksPlayedThisTurn;

        public Dictionary<Enemy, CombatActionData> CurrentEnemyIntents { get; } = new Dictionary<Enemy, CombatActionData>();

        public CombatManager(Hero hero, List<EnemyData> enemyTemplates, Random random, Func<string, EffectData> effectLookup)
        {
            TheHero = hero;
            rng = random;
            getEffectById = effectLookup;
            State = CombatState.Ongoing_PlayerTurn;
            TurnNumber = 0;

            Enemies = enemyTemplates.Select(template => new Enemy(template, rng)).ToList();
        }

        public void StartCombat()
        {
            TheHero.ResetForNewCombat();
            TheHero.Deck.StartCombat();
            
            BeginPlayerTurn();
            
            // Apply relics AFTER starting turn, so Block isn't wiped by StartTurn()
            ApplyRelicEffects(TheHero, ApplyType.StartOfCombat);
        }

        public bool PlayCard(CardData card, Enemy target)
        {
            bool isFirstAttack = card.Type == CardType.Attack && attacksPlayedThisTurn == 0;

            if (isFirstAttack)
            {
                // Logic hook for "first attack of turn" checks (future expansion)
            }

            int effectiveManaCost = card.ManaCost;


            if (State != CombatState.Ongoing_PlayerTurn || TheHero.CurrentMana < effectiveManaCost)
            {
                return false;
            }

            TheHero.CurrentMana -= effectiveManaCost;

            foreach (var action in card.Actions)
            {
                var targets = GetTargets(TheHero, target, action.Target);
                foreach (var t in targets)
                {
                    ActionResolver.Resolve(action, TheHero, t, getEffectById);
                }
            }

            TheHero.Deck.DiscardCardFromHand(card);
            OnCardPlayed?.Invoke(card);

            cardsPlayedThisTurn++;
            if (card.Type == CardType.Attack)
            {
                attacksPlayedThisTurn++;
            }
            CheckCombatStatus();
            return true;
        }

        public void EndPlayerTurn()
        {
            if (State != CombatState.Ongoing_PlayerTurn) return;
            
            TheHero.Deck.DiscardHand();
            TheHero.TickDownEffects();

            BeginEnemyTurn();
        }

        private void BeginPlayerTurn()
        {
            TurnNumber++;
            State = CombatState.Ongoing_PlayerTurn;
            
            if (TurnNumber > 100)
            {
                State = CombatState.Defeat;
                return;
            }

            CheckCombatStatus();
            if (State != CombatState.Ongoing_PlayerTurn) return;

            cardsPlayedThisTurn = 0;
            attacksPlayedThisTurn = 0;

            TheHero.StartTurn();

            ApplyRelicEffects(TheHero, ApplyType.StartOfTurn);

            CurrentEnemyIntents.Clear();
            foreach (var enemy in Enemies.Where(e => e.CurrentHealth > 0))
            {
                CurrentEnemyIntents[enemy] = enemy.PeekNextAction();
            }
        }

        private void BeginEnemyTurn()
        {
            State = CombatState.Ongoing_EnemyTurn;

            foreach (var enemy in Enemies.Where(e => e.CurrentHealth > 0))
            {
                var action = enemy.GetNextAction();
                var targets = GetTargets(enemy, TheHero, action.Target);

                foreach (var t in targets)
                {
                    ActionResolver.Resolve(action, enemy, t, getEffectById);
                }

                CheckCombatStatus();
                if (State == CombatState.Defeat) return;
            }

            foreach (var enemy in Enemies)
            {
                enemy.TickDownEffects();
                enemy.TickCooldowns();
            }

            CheckCombatStatus();

            if (State == CombatState.Ongoing_EnemyTurn)
            {
                BeginPlayerTurn();
            }
        }
        
        private void CheckCombatStatus()
        {
            if (State == CombatState.Victory || State == CombatState.Defeat) return;

            if (TheHero.CurrentHealth <= 0)
            {
                State = CombatState.Defeat;
            }
            else if (Enemies.All(e => e.CurrentHealth <= 0))
            {
                State = CombatState.Victory;
            }
        }

        private IEnumerable<Combatant> GetTargets(Combatant source, Combatant chosenTarget, TargetType targetType)
        {
            var livingEnemies = Enemies.Where(e => e.CurrentHealth > 0).ToList();

            switch (targetType)
            {
                case TargetType.Self:
                    return new[] { source };
                
                case TargetType.SingleOpponent:
                    if (source is Hero) return chosenTarget != null ? new[] { chosenTarget } : Enumerable.Empty<Combatant>();
                    return new[] { TheHero };

                case TargetType.AllOpponents:
                    if (source is Hero) return livingEnemies.Cast<Combatant>();
                    return new[] { TheHero };

                case TargetType.RandomOpponent:
                    if (source is Hero && livingEnemies.Any())
                    {
                        return new[] { livingEnemies[rng.Next(livingEnemies.Count)] };
                    }
                    return new[] { TheHero };
            }
            return Enumerable.Empty<Combatant>();
        }

        /// <summary>
        /// Applies relic effects of a specific ApplyType
        /// </summary>
        private void ApplyRelicEffects(Hero hero, ApplyType applyType)
        {
            foreach (var relic in hero.Relics)
            {
                foreach (var effect in relic.Effects.Where(e => e.ApplyType == applyType))
                {
                    if (effect is StatusEffectData statusEffect)
                    {
                        // Determine targets based on the effect's Target property
                        var targets = GetTargetsForRelicEffect(statusEffect.Target);
                        
                        foreach (var target in targets)
                        {
                            if (statusEffect.EffectType == StatusEffectType.ImmediateBlock)
                            {
                                target.GainBlock(statusEffect.Intensity);
                            }
                            else
                            {
                                target.ApplyEffect(statusEffect);
                            }
                        }
                    }
                    else if (effect is DeckEffectData deckEffect)
                    {
                        ActionResolver.Resolve(
                            new CombatActionData(ActionType.ApplyDeckEffect, effect.Intensity, TargetType.Self, effect.Id),
                            hero,
                            hero,
                            getEffectById
                        );
                    }
                }
            }
        }

        /// <summary>
        /// Gets the appropriate targets for a relic effect based on its TargetType
        /// </summary>
        private IEnumerable<Combatant> GetTargetsForRelicEffect(TargetType targetType)
        {
            switch (targetType)
            {
                case TargetType.Self:
                    return new[] { TheHero };
                
                case TargetType.AllOpponents:
                    return Enemies.Where(e => e.CurrentHealth > 0).Cast<Combatant>();
                
                case TargetType.RandomOpponent:
                    var livingEnemies = Enemies.Where(e => e.CurrentHealth > 0).ToList();
                    if (livingEnemies.Any())
                    {
                        return new[] { livingEnemies[rng.Next(livingEnemies.Count)] };
                    }
                    return Enumerable.Empty<Combatant>();
                
                case TargetType.SingleOpponent:
                    // For relics, we'll default to a random opponent
                    var livingEnemies2 = Enemies.Where(e => e.CurrentHealth > 0).ToList();
                    if (livingEnemies2.Any())
                    {
                        return new[] { livingEnemies2[rng.Next(livingEnemies2.Count)] };
                    }
                    return Enumerable.Empty<Combatant>();
                
                default:
                    return new[] { TheHero };
            }
        }
    }
}
