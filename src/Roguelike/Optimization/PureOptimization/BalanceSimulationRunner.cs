using Newtonsoft.Json;
using Roguelike.Core.AI;
using Roguelike.Data;
using Roguelike.Core;
using Roguelike.Core.Map;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Roguelike.Optimization
{
    public class BalanceSimulationRunner
    {
        private readonly HeroData _baseHero;
        private readonly CardPool _baseCards;
        private readonly RelicPool _baseRelics;
        private readonly EnemyPool _baseEnemies;
        private readonly EffectPool _baseEffects;
        private readonly EventPool _baseEvents;
        private readonly Dictionary<RoomType, RoomData> _baseRoomConfigs;

        private readonly IPlayerAgent _agent;
        
        public BalanceSimulationRunner(IPlayerAgent agent, HeroData baseHero, CardPool cards, RelicPool relics, EnemyPool enemies, EffectPool effects, EventPool events, Dictionary<RoomType, RoomData> roomConfigs)
        {
            _agent = agent;
            _baseHero = baseHero;
            _baseCards = cards;
            _baseRelics = relics;
            _baseEnemies = enemies;
            _baseEffects = effects;
            _baseEvents = events;
            _baseRoomConfigs = roomConfigs;
        }

        public SimulationStats Run(BalanceGenome genome, int seed)
        {
            var simHero = DeepClone(_baseHero);
            var simCards = DeepCloneAndInitCardPool(_baseCards);
            var simRelics = DeepCloneAndInitRelicPool(_baseRelics);
            var simEnemies = DeepCloneAndInitEnemyPool(_baseEnemies);
            var simEffects = DeepCloneEffectPool(_baseEffects);
            
            GenomeApplicator.Apply(genome, simEnemies, simCards, simRelics, simEffects, simHero);

            var controller = new GameController(simCards, simRelics, simEnemies, simEffects, _baseEvents, _baseRoomConfigs);
            controller.StartNewRun(seed, simHero, null);

            var runState = controller.CurrentRun;
            var stats = new SimulationStats();
            
            Action<CardData> cardPlayedHandler = (card) => {
                if (!stats.CardPlayCounts.ContainsKey(card.Id)) stats.CardPlayCounts[card.Id] = 0;
                stats.CardPlayCounts[card.Id]++;
            };

            while (runState.CurrentState != GameState.GameOver)
            {
                var room = runState.TheMap.GetCurrentRoom();

                switch (runState.CurrentState)
                {
                    case GameState.OnMap:
                        var nextNodeId = _agent.ChooseMapNode(runState);
                        if (controller.ChooseMapNode(nextNodeId))
                        {
                            var newRoom = runState.TheMap.GetCurrentRoom();
                            if(newRoom.Type == RoomType.Elite) stats.ElitesEncountered++;
                        }
                        else
                        {
                            runState.CurrentState = GameState.GameOver;
                        }
                        break;

                    case GameState.InCombat:
                        var activeCombat = runState.CurrentCombat;
                        var combatRoomType = room.Type;
                        float hpBeforeCombat = runState.TheHero.CurrentHealth;

                        activeCombat.OnCardPlayed += cardPlayedHandler;

                        int actionCounter = 0;
                        const int MAX_ACTIONS_PER_TURN = 50;

                        while(runState.CurrentState == GameState.InCombat)
                        {
                            var decision = _agent.GetCombatDecision(runState);
                            if (decision.Type == CombatActionType.PlayCard)
                            {
                                bool success = controller.PlayCard(decision.HandIndex, decision.TargetIndex);
                                if (!success)
                                {
                                    controller.EndTurn();
                                }
                                
                                actionCounter++;
                            }
                            else
                            {
                                controller.EndTurn();
                                actionCounter = 0;
                            }

                            if (actionCounter > MAX_ACTIONS_PER_TURN)
                            {
                                controller.EndTurn();
                                actionCounter = 0;
                            }
                        }

                        activeCombat.OnCardPlayed -= cardPlayedHandler;
                        
                        if(activeCombat.State == CombatState.Victory && combatRoomType == RoomType.Elite)
                        {
                            stats.ElitesDefeated++;
                            stats.TotalDamageTakenAtElites += hpBeforeCombat - runState.TheHero.CurrentHealth;
                        }

                        break;
                        
                    case GameState.InEvent:
                        var choice = _agent.ChooseEventOption(runState);
                        controller.ChooseEventOption(choice);
                        break;

                    case GameState.InShop:
                        int goldBeforeShop = runState.TheHero.CurrentGold;
                        while (runState.CurrentState == GameState.InShop)
                        {
                            var shopDecision = _agent.GetShopDecision(runState);
                            if (shopDecision.Type == ShopActionType.Leave)
                            {
                                controller.LeaveShop();
                            }
                            else if (shopDecision.Type == ShopActionType.BuyCard)
                            {
                                controller.BuyShopCard(shopDecision.ShopIndex);
                            }
                            else if (shopDecision.Type == ShopActionType.BuyRelic)
                            {
                                controller.BuyShopRelic(shopDecision.ShopIndex);
                            }
                        }
                        stats.GoldSpent += goldBeforeShop - runState.TheHero.CurrentGold;
                        break;

                    case GameState.AwaitingReward:
                        int goldBeforeReward = runState.TheHero.CurrentGold;
                        var cardRewardIndex = _agent.ChooseCardReward(runState);
                        controller.ConfirmRewards(cardRewardIndex);
                        stats.GoldCollected += runState.TheHero.CurrentGold - goldBeforeReward;
                        break;
                }
            }

            stats.IsVictory = runState.TheHero.CurrentHealth > 0;
            stats.FinalFloorReached = runState.CurrentFloor;
            stats.FinalHPPercent = (float)runState.TheHero.CurrentHealth / runState.TheHero.MaxHealth;
            stats.MasterDeckIds = runState.TheHero.Deck.MasterDeck.Select(c => c.Id).ToList();
            stats.RelicIds = runState.TheHero.Relics.Select(r => r.Id).ToList();

            stats.CardPlayCounts.Remove("subscribed");
            
            return stats;
        }

        #region DEEP CLONING
        private T DeepClone<T>(T obj)
        {
            var settings = new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.Auto,
                PreserveReferencesHandling = PreserveReferencesHandling.Objects
            };

            string json = JsonConvert.SerializeObject(obj, settings);
            return JsonConvert.DeserializeObject<T>(json, settings);
        }

        private CardPool DeepCloneAndInitCardPool(CardPool original)
        {
            return DeepClone(original);
        }

        private EnemyPool DeepCloneAndInitEnemyPool(EnemyPool original)
        {
            return DeepClone(original);
        }

        private RelicPool DeepCloneAndInitRelicPool(RelicPool original)
        {
            return DeepClone(original);
        }

        private EffectPool DeepCloneEffectPool(EffectPool original)
        {
            return DeepClone(original);
        }

        #endregion
    }
}
