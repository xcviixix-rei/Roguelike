using Roguelike.Data;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Roguelike.Logic
{
    /// <summary>
    /// Represents the possible states of a combat encounter.
    /// </summary>
    public enum CombatState
    {
        Ongoing_PlayerTurn,
        Ongoing_EnemyTurn,
        Victory,
        Defeat
    }

    /// <summary>
    /// Manages the state and flow of a single combat encounter.
    /// It orchestrates player turns, enemy turns, and resolves actions.
    /// </summary>
    public class CombatManager
    {
        public Hero TheHero { get; }
        public List<Enemy> Enemies { get; }
        public int TurnNumber { get; private set; }
        public CombatState State { get; private set; }

        private readonly Random rng;
        private readonly Func<string, EffectData> getEffectById;

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

        /// <summary>
        /// Kicks off the combat, preparing the hero and starting the first turn.
        /// </summary>
        public void StartCombat()
        {
            TheHero.ResetForNewCombat();
            TheHero.Deck.StartCombat();
            BeginPlayerTurn();
        }

        /// <summary>
        /// Executes the action of a player playing a card on a target.
        /// </summary>
        /// <returns>True if the card was successfully played, false otherwise.</returns>
        public bool PlayCard(CardData card, Enemy target)
        {
            if (State != CombatState.Ongoing_PlayerTurn || TheHero.CurrentMana < card.ManaCost)
            {
                return false;
            }

            TheHero.CurrentMana -= card.ManaCost;

            foreach (var action in card.Actions)
            {
                var targets = GetTargets(TheHero, target, action.Target);
                foreach (var t in targets)
                {
                    ActionResolver.Resolve(action, TheHero, t, getEffectById);
                }
            }

            TheHero.Deck.DiscardCardFromHand(card);
            CheckCombatStatus();
            return true;
        }

        /// <summary>
        /// Ends the player's current turn and transitions to the enemy's turn.
        /// </summary>
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

            TheHero.StartTurn();

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

        /// <summary>
        /// A helper method to resolve a TargetType into a list of actual Combatant objects.
        /// </summary>
        private IEnumerable<Combatant> GetTargets(Combatant source, Combatant chosenTarget, TargetType targetType)
        {
            var livingEnemies = Enemies.Where(e => e.CurrentHealth > 0).ToList();

            switch (targetType)
            {
                case TargetType.Self:
                    return new[] { source };
                
                case TargetType.SingleOpponent:
                    if (source is Hero) return new[] { chosenTarget };
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
    }
}