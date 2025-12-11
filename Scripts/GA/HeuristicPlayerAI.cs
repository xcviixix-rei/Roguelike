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
                case RoomType.Rest: return hpPercent < 0.5 ? 50.0 : 10.0;
                
                case RoomType.Elite:
                case RoomType.Monster:
                    double starValue = room.StarRating * 10.0;
                    if (hpPercent > 0.7) return starValue; 
                    if (hpPercent < 0.5) return -starValue;
                    return starValue * 0.5;
                
                case RoomType.Shop: return run.TheHero.CurrentGold > 150 ? 35.0 : 5.0;
                case RoomType.Event: return 15.0;
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
            var combat = run.CurrentCombat;

            if (combat == null || combat.State != CombatState.Ongoing_PlayerTurn)
            {
                return CombatDecision.EndTurn();
            }

            if (!enemies.Any())
            {
                return CombatDecision.EndTurn();
            }
                
            if (hero.Deck.Hand.Count == 0 || hero.CurrentMana <= 0)
            {
                return CombatDecision.EndTurn();
            }
            
            bool hasAffordableCard = false;
            bool isFirstAttack = combat.TurnNumber > 0;
            bool hasHandDrill = hero.Relics.Any(r => r.Id == "hand_drill");
            
            for (int i = 0; i < hero.Deck.Hand.Count; i++)
            {
                var card = hero.Deck.Hand[i];
                int effectiveCost = card.ManaCost;
                
                if (hasHandDrill && card.Type == CardType.Attack && isFirstAttack)
                {
                    effectiveCost = 0;
                }
                
                if (effectiveCost <= hero.CurrentMana)
                {
                    hasAffordableCard = true;
                    break;
                }
            }
            
            if (!hasAffordableCard)
            {
                return CombatDecision.EndTurn();
            }

            // Precise Incoming Damage Calculation
            int totalIncomingDamage = CalculateIncomingDamage(run, enemies);
            int neededBlock = Math.Max(0, totalIncomingDamage - hero.Block);

            // 1st PRIORITY: SURVIVAL
            if (neededBlock > 0)
            {
                var defensiveMove = FindBestDefensiveMove(run, hero, enemies, neededBlock);
                if (defensiveMove.HasValue)
                {
                    return defensiveMove.Value;
                }
            }

            // 2nd PRIORITY: LETHAL DMG
            var lethalMove = FindLethalMove(run, hero, enemies);
            if (lethalMove.HasValue)
            {
                return lethalMove.Value;
            }

            // LEFT-OVER: BEST GENERAL MOVE (Score-based)
            var finalDecision = GetBestGeneralMove(run, hero, enemies);
    
            if (finalDecision.Type == CombatActionType.PlayCard)
            {
                if (finalDecision.HandIndex < 0 || finalDecision.HandIndex >= hero.Deck.Hand.Count)
                {
                    return CombatDecision.EndTurn();
                }
                if (finalDecision.TargetIndex < 0 || finalDecision.TargetIndex >= combat.Enemies.Count)
                {
                    return CombatDecision.EndTurn();
                }
            }
            
            return finalDecision;
        }

        private CombatDecision? FindLethalMove(GameRun run, Hero hero, List<Enemy> enemies)
        {
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
            return null;
        }
        
        private CombatDecision GetBestGeneralMove(GameRun run, Hero hero, List<Enemy> enemies)
        {
            int bestCardIndex = -1;
            int bestTargetIndex = 0;
            double bestScore = -1.0;

            for (int i = 0; i < hero.Deck.Hand.Count; i++)
            {
                var card = hero.Deck.Hand[i];
                if (card.ManaCost > hero.CurrentMana) continue;

                if (card.Actions.Any(a => a.Target == TargetType.SingleOpponent))
                {
                    var targetEnemy = enemies.OrderBy(e => e.CurrentHealth).First();
                    int targetIndex = run.CurrentCombat.Enemies.IndexOf(targetEnemy);
                    
                    double score = ScoreCard(card, hero, targetEnemy);
                    double scorePerMana = score / (Math.Max(card.ManaCost, 1));

                    if (scorePerMana > bestScore)
                    {
                        bestScore = scorePerMana;
                        bestCardIndex = i;
                        bestTargetIndex = targetIndex;
                    }
                }
                else
                {
                    double score = ScoreCard(card, hero, null);
                    double scorePerMana = score / (Math.Max(card.ManaCost, 1));

                    if (scorePerMana > bestScore)
                    {
                        bestScore = scorePerMana;
                        bestCardIndex = i;
                        bestTargetIndex = 0;
                    }
                }
            }
            
            if (bestCardIndex != -1)
            {
                return CombatDecision.Play(bestCardIndex, bestTargetIndex);
            }

            return CombatDecision.EndTurn();
        }

        /// <summary>
        /// A scoring function for a card's general value.
        /// </summary>
        private double ScoreCard(CardData card, Hero hero, Enemy target)
        {
            double score = 0;

            score += 1; 

            switch (card.Type)
            {
                case CardType.Power:
                    score += 20; 
                    break;

                case CardType.Attack:
                    if (target != null)
                    {
                        int totalDamage = 0;
                        foreach (var action in card.Actions.Where(a => a.Type == ActionType.DealDamage))
                        {
                            totalDamage += PredictDamage(action.Value, hero, target);
                        }
                        score += totalDamage;
                    }
                    break;
                
                case CardType.Skill:
                    foreach(var action in card.Actions)
                    {
                        if(action.Type == ActionType.GainBlock)
                        {
                            score += action.Value * 0.5;
                        }
                        else if (action.Type == ActionType.ApplyDeckEffect && action.EffectId.Contains("draw"))
                        {
                            score += action.Value * 10;
                        }
                        else if (action.Type == ActionType.ApplyStatusEffect)
                        {
                            score += 15;
                        }
                    }
                    break;
            }
            return score;
        }

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
            var frail = hero.ActiveEffects.FirstOrDefault(e => e.SourceData is StatusEffectData s && s.EffectType == StatusEffectType.Frail);

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
                        int currentEnemyDamage = (int)Math.Floor(PredictEnemyDamage(run, enemy, run.TheHero));
                        if (currentEnemyDamage > maxDamageFromSingleEnemy)
                        {
                            maxDamageFromSingleEnemy = currentEnemyDamage;
                            bestWeakTarget = enemy;
                        }
                    }
                }
            }

            int bestCardIndex = -1;
            int bestTargetIndex = 0;
            double bestEfficiency = -1.0; 

            for (int i = 0; i < hero.Deck.Hand.Count; i++)
            {
                var card = hero.Deck.Hand[i];
                if (card.ManaCost > hero.CurrentMana) continue;

                int blockGain = 0;
                var blockAction = card.Actions.FirstOrDefault(a => a.Type == ActionType.GainBlock);
                if (blockAction != null)
                {
                    float finalBlock = blockAction.Value;
                    if (frail != null)
                    {
                        finalBlock *= (frail.SourceData.Value / 100f);
                    }
                    blockGain = (int)Math.Floor(finalBlock);
                }

                int preventedDamage = 0;
                int potentialTarget = 0;

                var weakAction = card.Actions.FirstOrDefault(a => a.Type == ActionType.ApplyStatusEffect && a.EffectId.Contains("weakened"));
                
                if (weakAction != null && bestWeakTarget != null)
                {
                    var originalDamage = PredictEnemyDamage(run, bestWeakTarget, hero);
                    var effectData = run.EffectPool.GetEffect(weakAction.EffectId) as StatusEffectData;
                    if (effectData != null)
                    {
                        bestWeakTarget.ActiveEffects.Add(new ActiveEffect(new StatusEffectData { EffectType = StatusEffectType.Weakened, Value = effectData.Value }, weakAction.Value));
                        var weakenedDamage = PredictEnemyDamage(run, bestWeakTarget, hero);
                        bestWeakTarget.ActiveEffects.RemoveAt(bestWeakTarget.ActiveEffects.Count - 1);
                        preventedDamage = (int)Math.Floor(originalDamage) - (int)Math.Floor(weakenedDamage);
                        potentialTarget = run.CurrentCombat.Enemies.IndexOf(bestWeakTarget);
                    }
                }

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
            {
                return CombatDecision.Play(bestCardIndex, bestTargetIndex);
            }

            return null;
        }

        private float PredictEnemyDamage(GameRun run, Enemy enemy, Hero hero)
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

        private int CalculateIncomingDamage(GameRun run, List<Enemy> enemies)
        {
            int total = 0;
            foreach (var enemy in enemies)
            {
                total += (int)Math.Floor(PredictEnemyDamage(run, enemy, run.TheHero));
            }
            return total;
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
                case EventEffectType.GainGold: return effect.Value * 0.5;
                case EventEffectType.LoseGold: return -effect.Value * 0.5;
                case EventEffectType.HealHP: return hpRatio < 0.5 ? effect.Value * 2.0 : effect.Value * 1.0;
                case EventEffectType.LoseHP: return hpRatio < 0.3 ? -effect.Value * 3.0 : -effect.Value * 1.2;
                case EventEffectType.GainCard: return 25.0; 
                case EventEffectType.RemoveCard: return 40.0; 
                case EventEffectType.GainRelic: return 60.0;
                default: return 0.0;
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