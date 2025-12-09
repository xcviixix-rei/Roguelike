using Roguelike.Data;
using Roguelike.Logic;
using RoguelikeMapGen;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Roguelike.Agents
{
    public class HeuristicPlayerAI : IPlayerAgent
    {
        #region MAP PATHING
        public int ChooseMapNode(GameRun run)
        {
            var possibleRooms = run.TheMap.GetPossibleNextNodes();
            if (!possibleRooms.Any()) return -1;

            Room bestRoom = null;
            double bestScore = double.MinValue;

            foreach (var room in possibleRooms)
            {
                double score = EvaluateRoom(run, room);
                if (score > bestScore)
                {
                    bestScore = score;
                    bestRoom = room;
                }
            }
            return bestRoom?.Id ?? possibleRooms[0].Id;
        }

        private double EvaluateRoom(GameRun run, Room room)
        {
            double hpPercent = (double)run.TheHero.CurrentHealth / run.TheHero.MaxHealth;
            switch (room.Type)
            {
                case RoomType.Elite:
                case RoomType.Monster:
                    double starValue = room.StarRating * 10.0;
                    if (hpPercent > 0.7) return starValue; 
                    if (hpPercent < 0.5) return -starValue;
                    return starValue * 0.5;
                case RoomType.Shop: return run.TheHero.CurrentGold > 150 ? 35.0 : 5.0;
                case RoomType.Event: return 15.0;
                case RoomType.Monster: return 10.0;
                case RoomType.Boss: return 1000.0;
                default: return 0.0;
            }
        }
        #endregion

        #region COMBAT TACTICS
        public CombatDecision GetCombatDecision(GameRun run)
        {
            var hero = run.TheHero;
            var enemies = run.CurrentCombat.Enemies.Where(e => e.CurrentHealth > 0).ToList();

            if (!enemies.Any() || hero.Deck.Hand.Count == 0 || hero.CurrentMana <= 0)
                return CombatDecision.EndTurn();

            // 1. Precise Incoming Damage Calculation
            int totalIncomingDamage = CalculateIncomingDamage(run, enemies);
            int neededBlock = Math.Max(0, totalIncomingDamage - hero.Block);

            // 2. PRIORITY: SURVIVAL (Smart Blocking)
            if (neededBlock > 0)
            {
                var defensiveMove = FindBestDefensiveMove(run, hero, enemies, neededBlock);
                if (defensiveMove.HasValue)
                {
                    return defensiveMove.Value;
                }
            }

            // 3. PRIORITY: KILL
            for (int i = 0; i < hero.Deck.Hand.Count; i++)
            {
                var card = hero.Deck.Hand[i];
                if (card.ManaCost > hero.CurrentMana || card.Type != CardType.Attack) continue;

                var damageAction = card.Actions.FirstOrDefault(a => a.Type == ActionType.DealDamage);
                if (damageAction != null)
                {
                    for (int e = 0; e < enemies.Count; e++)
                    {
                        int predictedDamage = PredictDamage(damageAction.Value, hero, enemies[e]);
                        if (enemies[e].CurrentHealth <= predictedDamage)
                        {
                            int actualIndex = run.CurrentCombat.Enemies.IndexOf(enemies[e]);
                            return CombatDecision.Play(i, actualIndex);
                        }
                    }
                }
            }

            // 4. PRIORITY: DUMP ENERGY (Score-based)
            return GetBestGeneralMove(run, hero, enemies, neededBlock);
        }

        /// <summary>
        /// Duplicates ActionResolver logic to predict exact incoming damage.
        /// </summary>
        private int CalculateIncomingDamage(GameRun run, List<Enemy> enemies)
        {
            int total = 0;
            foreach (var enemy in enemies)
            {
                if (!run.CurrentCombat.CurrentEnemyIntents.TryGetValue(enemy, out var intent)) continue;
                if (intent.Type != ActionType.DealDamage) continue;

                float dmg = intent.Value;

                // Strength (Enemy)
                var strength = enemy.ActiveEffects.FirstOrDefault(e => e.SourceData is StatusEffectData s && s.EffectType == StatusEffectType.Strength);
                if (strength != null) dmg += (strength.Stacks * strength.SourceData.Value);

                // Weakened (Enemy) - applied to source
                var weakened = enemy.ActiveEffects.FirstOrDefault(e => e.SourceData is StatusEffectData s && s.EffectType == StatusEffectType.Weakened);
                if (weakened != null) dmg *= (weakened.SourceData.Value / 100f);

                // Vulnerable (Hero) - applied to target
                var vulnerable = run.TheHero.ActiveEffects.FirstOrDefault(e => e.SourceData is StatusEffectData s && s.EffectType == StatusEffectType.Vulnerable);
                if (vulnerable != null) dmg *= (vulnerable.SourceData.Value / 100f);

                total += (int)Math.Floor(dmg);
            }
            return total;
        }

        /// <summary>
        /// Predicts damage a card action would deal from Source(Hero) to Target(Enemy).
        /// </summary>
        private int PredictDamage(int baseDamage, Hero hero, Enemy target)
        {
            float finalDamage = baseDamage;

            var strength = hero.ActiveEffects.FirstOrDefault(e => e.SourceData is StatusEffectData s && s.EffectType == StatusEffectType.Strength);
            if (strength != null) finalDamage += (strength.Stacks * strength.SourceData.Value);

            var weakened = hero.ActiveEffects.FirstOrDefault(e => e.SourceData is StatusEffectData s && s.EffectType == StatusEffectType.Weakened);
            if (weakened != null) finalDamage *= (weakened.SourceData.Value / 100f);

            var vulnerable = target.ActiveEffects.FirstOrDefault(e => e.SourceData is StatusEffectData s && s.EffectType == StatusEffectType.Vulnerable);
            if (vulnerable != null) finalDamage *= (vulnerable.SourceData.Value / 100f);

            return (int)Math.Floor(finalDamage);
        }

        private CombatDecision? FindBestDefensiveMove(GameRun run, Hero hero, List<Enemy> enemies, int neededBlock)
        {
            // 0. Check for Hero Status Effects (Frail)
            bool isFrail = hero.ActiveEffects.Any(e => 
                e.SourceData is StatusEffectData s && s.EffectType == StatusEffectType.Frail);

            // 1. Identify the best target for a potential 'Weak' debuff.
            // This is the attacking enemy, not currently weakened, who would deal the most damage.
            Enemy bestWeakTarget = null;
            int maxDamageFromSingleEnemy = 0;

            foreach (var enemy in enemies)
            {
                if (run.CurrentCombat.CurrentEnemyIntents.TryGetValue(enemy, out var intent) 
                    && intent.Type == ActionType.DealDamage)
                {
                    bool isAlreadyWeak = enemy.ActiveEffects.Any(e => 
                        e.SourceData is StatusEffectData s && s.EffectType == StatusEffectType.Weakened);
                    
                    if (!isAlreadyWeak)
                    {
                        // Calculate precise damage from this specific enemy
                        int currentEnemyDamage = (int)Math.Floor(PredictEnemyDamage(enemy, run.TheHero));
                        if (currentEnemyDamage > maxDamageFromSingleEnemy)
                        {
                            maxDamageFromSingleEnemy = currentEnemyDamage;
                            bestWeakTarget = enemy;
                        }
                    }
                }
            }

            // 2. Evaluate all affordable cards for their total defensive value
            int bestCardIndex = -1;
            int bestTargetIndex = 0;
            double bestEfficiency = -1.0; 

            for (int i = 0; i < hero.Deck.Hand.Count; i++)
            {
                var card = hero.Deck.Hand[i];
                if (card.ManaCost > hero.CurrentMana) continue;

                // --- Calculate Block Value ---
                int blockGain = 0;
                var blockAction = card.Actions.FirstOrDefault(a => a.Type == ActionType.GainBlock);
                if (blockAction != null)
                {
                    float finalBlock = blockAction.Value;
                    if (isFrail) finalBlock *= 0.75f; 
                    blockGain = (int)Math.Floor(finalBlock);
                }

                // --- Calculate Weak Value (Prevented Damage) ---
                int preventedDamage = 0;
                int potentialTarget = 0;

                var weakAction = card.Actions.FirstOrDefault(a => a.Type == ActionType.ApplyStatusEffect && a.EffectId.Contains("weak"));
                
                if (weakAction != null && bestWeakTarget != null)
                {
                    // Simulate applying Weak to the best target
                    var originalDamage = PredictEnemyDamage(bestWeakTarget, hero);
                    
                    // Temporarily add a "fake" Weak effect to the target for prediction
                    bestWeakTarget.ActiveEffects.Add(new ActiveEffect(new StatusEffectData { EffectType = StatusEffectType.Weakened, Value = 75 }, 1));
                    var weakenedDamage = PredictEnemyDamage(bestWeakTarget, hero);
                    
                    // Remove the fake effect
                    bestWeakTarget.ActiveEffects.RemoveAt(bestWeakTarget.ActiveEffects.Count - 1);

                    preventedDamage = (int)Math.Floor(originalDamage) - (int)Math.Floor(weakenedDamage);
                    potentialTarget = run.CurrentCombat.Enemies.IndexOf(bestWeakTarget);
                }

                // --- Efficiency Calculation ---
                int totalDefensiveValue = blockGain + preventedDamage;

                if (totalDefensiveValue > 0)
                {
                    double efficiency = (double)totalDefensiveValue / Math.Max(1, card.ManaCost);

                    if (efficiency > bestEfficiency)
                    {
                        bestEfficiency = efficiency;
                        bestCardIndex = i;
                        bestTargetIndex = potentialTarget;
                    }
                }
            }

            if (bestCardIndex != -1)
                return CombatDecision.Play(bestCardIndex, bestTargetIndex);

            return null;
        }

        /// <summary>
        /// A helper to predict damage from a single enemy, for precise targeting.
        /// </summary>
        private float PredictEnemyDamage(Enemy enemy, Hero hero)
        {
            if (!run.CurrentCombat.CurrentEnemyIntents.TryGetValue(enemy, out var intent) || intent.Type != ActionType.DealDamage)
                return 0f;

            float dmg = intent.Value;
            
            var strength = enemy.ActiveEffects.FirstOrDefault(e => e.SourceData is StatusEffectData s && s.EffectType == StatusEffectType.Strength);
            if (strength != null) dmg += (strength.Stacks * strength.SourceData.Value);
            
            var weakened = enemy.ActiveEffects.FirstOrDefault(e => e.SourceData is StatusEffectData s && s.EffectType == StatusEffectType.Weakened);
            if (weakened != null) dmg *= (weakened.SourceData.Value / 100f);

            var vulnerable = hero.ActiveEffects.FirstOrDefault(e => e.SourceData is StatusEffectData s && s.EffectType == StatusEffectType.Vulnerable);
            if (vulnerable != null) dmg *= (vulnerable.SourceData.Value / 100f);
            
            return dmg;
        }

        /// <summary>
        /// Calculates total incoming damage from ALL enemies.
        /// </summary>
        private int CalculateIncomingDamage(GameRun run, List<Enemy> enemies)
        {
            int total = 0;
            foreach (var enemy in enemies)
            {
                total += (int)Math.Floor(PredictEnemyDamage(enemy, run.TheHero));
            }
            return total;
        }

        private CombatDecision GetBestGeneralMove(GameRun run, Hero hero, List<Enemy> enemies, int neededBlock)
        {
            // Basic scoring for dumping energy
            int bestIndex = -1;
            int bestScore = -1;
            int bestTarget = 0;

            for (int i = 0; i < hero.Deck.Hand.Count; i++)
            {
                var card = hero.Deck.Hand[i];
                if (card.ManaCost > hero.CurrentMana) continue;

                int score = 0;
                int target = 0;

                if (card.Type == CardType.Power) score = 50;
                else if (card.Type == CardType.Attack)
                {
                    var dmg = PredictDamage(card.Actions.FirstOrDefault(a => a.Type == ActionType.DealDamage)?.Value ?? 0, hero, enemies[0]);
                    score = dmg;
                    // Target weakest
                    var weakEnemy = enemies.OrderBy(e => e.CurrentHealth).First();
                    target = run.CurrentCombat.Enemies.IndexOf(weakEnemy);
                }
                else if (card.Type == CardType.Skill)
                {
                    // Only value skills if they do something useful besides block (if block isn't needed)
                    if (neededBlock <= 0 && card.Actions.Any(a => a.Type == ActionType.GainBlock)) score = 0;
                    else score = 10;
                }

                if (score > bestScore)
                {
                    bestScore = score;
                    bestIndex = i;
                    bestTarget = target;
                }
            }

            if (bestIndex != -1) return CombatDecision.Play(bestIndex, bestTarget);
            return CombatDecision.EndTurn();
        }
        #endregion

        #region SHOPPING
        public ShopDecision GetShopDecision(GameRun run)
        {
            var shop = run.CurrentShop;
            int currentGold = run.TheHero.CurrentGold;

            for (int i = 0; i < shop.RelicsForSale.Count; i++)
            {
                var item = shop.RelicsForSale[i];
                if (!item.IsSold && item.Price <= currentGold)
                {
                    if (item.Item.StarRating >= 4) return ShopDecision.BuyRelic(i);
                }
            }

            for (int i = 0; i < shop.CardsForSale.Count; i++)
            {
                var item = shop.CardsForSale[i];
                if (!item.IsSold && item.Price <= currentGold)
                {
                    if (item.Item.StarRating >= 4) return ShopDecision.BuyCard(i);
                }
            }

            if (currentGold > 200)
            {
                for (int i = 0; i < shop.RelicsForSale.Count; i++)
                {
                    if (!shop.RelicsForSale[i].IsSold && shop.RelicsForSale[i].Price <= currentGold && shop.RelicsForSale[i].Item.StarRating >= 3)
                        return ShopDecision.BuyRelic(i);
                }
            }

            return ShopDecision.Leave();
        }
        #endregion

        #region EVENTS & REWARDS
        public int ChooseEventOption(GameRun run)
        {
            var choices = run.CurrentEvent.Choices;
            int bestChoiceIndex = 0;
            double bestScore = double.MinValue;

            for (int i = 0; i < choices.Count; i++)
            {
                double score = 0;
                foreach (var effect in choices[i].Effects)
                {
                    score += EvaluateEventEffect(run, effect);
                }

                if (score > bestScore)
                {
                    bestScore = score;
                    bestChoiceIndex = i;
                }
            }
            return bestChoiceIndex;
        }

        private double EvaluateEventEffect(GameRun run, EventEffect effect)
        {
            double hpRatio = (double)run.TheHero.CurrentHealth / run.TheHero.MaxHealth;

            switch (effect.Type)
            {
                case EventEffectType.GainGold:
                    // 1 Gold = 0.5 Score.
                    return effect.Value * 0.5;

                case EventEffectType.LoseGold:
                    // Losing gold is bad, but less bad if we are rich.
                    return -effect.Value * 0.5;

                case EventEffectType.HealHP:
                    // Healing is very valuable if low HP.
                    // Value * 2 if < 50% HP, else Value * 1.
                    return hpRatio < 0.5 ? effect.Value * 2.0 : effect.Value * 1.0;

                case EventEffectType.LoseHP:
                    // Losing HP is very bad if low HP.
                    // Cost * 3 if < 30% HP (Risk of death).
                    return hpRatio < 0.3 ? -effect.Value * 3.0 : -effect.Value * 1.2;

                case EventEffectType.GainCard:
                    // Arbitrary value for a card. A "good" card is worth ~50 gold.
                    return 25.0; 

                case EventEffectType.RemoveCard:
                    // Removing a bad card (Strike) is high value.
                    return 40.0; 

                case EventEffectType.GainRelic:
                    // Relics are high value.
                    return 60.0;

                case EventEffectType.Quit:
                    return 0.0;

                default:
                    return 0.0;
            }
        }

        public int ChooseCardReward(GameRun run)
        {
            var choices = run.CardRewardChoices;
            if (choices.Count == 0) return -1;

            bool allLowStar = choices.All(c => c.StarRating <= 2);
            if (run.TheHero.Deck.MasterDeck.Count > 20 && allLowStar) return -1;
            
            var bestCard = choices.OrderByDescending(c => c.StarRating).First();
            return choices.IndexOf(bestCard);
        }
        #endregion
    }
}