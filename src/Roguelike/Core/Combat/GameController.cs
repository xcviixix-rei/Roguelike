using Roguelike.Data;
using Roguelike.Optimization;
using Roguelike.Core.Handlers;
using Roguelike.Core.Map;
using System.Collections.Generic;

namespace Roguelike.Core
{
    /// <summary>
    /// The master controller for the game.
    /// It coordinates the GameRun, RoomHandlers, and CombatManager.
    /// External systems (GA, Unity UI) calls methods on this class to interact with the game.
    /// </summary>
    public class GameController
    {
        public GameRun CurrentRun { get; private set; }
        private readonly CardPool _cardPool;
        private readonly RelicPool _relicPool;
        private readonly EnemyPool _enemyPool;
        private readonly EffectPool _effectPool;
        private readonly EventPool _eventPool;
        private readonly Dictionary<RoomType, RoomData> _roomConfigs;

        private readonly Dictionary<RoomType, IRoomHandler> _handlers;

        public GameController(CardPool cp, RelicPool rp, EnemyPool ep, EffectPool efp, EventPool evp, Dictionary<RoomType, RoomData> rc)
        {
            _cardPool = cp;
            _relicPool = rp;
            _enemyPool = ep;
            _effectPool = efp;
            _eventPool = evp;
            _roomConfigs = rc;

            _handlers = new Dictionary<RoomType, IRoomHandler>
            {
                { RoomType.Monster, new CombatRoomHandler() }, // Difficulty of 1, 2
                { RoomType.Elite, new CombatRoomHandler() }, // Difficulty of 3, 4
                { RoomType.Boss, new BossRoomHandler() }, // Difficulty of 5
                { RoomType.Event, new EventRoomHandler() },
                { RoomType.Shop, new ShopRoomHandler() },
                { RoomType.Rest, new RestRoomHandler() },
            };
        }

        /// <summary>
        /// Starts a completely new game run with the specified seed and hero.
        /// </summary>
        public void StartNewRun(int seed, HeroData heroData, HierarchicalGenome genome = null)
        {
            CurrentRun = new GameRun(seed, heroData, _cardPool, _relicPool, _enemyPool, _effectPool, _eventPool, _roomConfigs, genome);
        }

#region MAP ACTIONS

        /// <summary>
        /// Attempts to move the player to a connected room on the map.
        /// </summary>
        public bool ChooseMapNode(int nodeId)
        {
            if (CurrentRun.CurrentState != GameState.OnMap) return false;

            if (!CurrentRun.TheMap.MoveToNode(nodeId))
            {
                return false;
            }

            var room = CurrentRun.TheMap.GetCurrentRoom();
            if (_handlers.TryGetValue(room.Type, out var handler))
            {
                handler.Execute(CurrentRun, room);
                
                if (room.Type == RoomType.Rest)
                {
                }
            }
            return true;
        }
#endregion

#region COMBAT ACTIONS

        public bool PlayCard(int handIndex, int targetEnemyIndex)
        {
            if (CurrentRun.CurrentState != GameState.InCombat || CurrentRun.CurrentCombat == null) return false;

            var hero = CurrentRun.TheHero;
            if (handIndex < 0 || handIndex >= hero.Deck.Hand.Count) return false;
            
            var card = hero.Deck.Hand[handIndex];
            
            Enemy target = null;
            if (CurrentRun.CurrentCombat.Enemies.Count > targetEnemyIndex && targetEnemyIndex >= 0)
            {
                var candidate = CurrentRun.CurrentCombat.Enemies[targetEnemyIndex];
                if (candidate.CurrentHealth > 0)
                {
                    target = candidate;
                }
            }

            bool success = CurrentRun.CurrentCombat.PlayCard(card, target);
            if (success)
            {
                CheckCombatResult();
            }
            return success;
        }

        public bool EndTurn()
        {
            if (CurrentRun.CurrentState != GameState.InCombat || CurrentRun.CurrentCombat == null) return false;

            CurrentRun.CurrentCombat.EndPlayerTurn();
            CheckCombatResult();
            return true;
        }

         private void CheckCombatResult()
        {
            if (CurrentRun.CurrentCombat.State == CombatState.Victory)
            {
                CombatRoomHandler.GenerateVictoryRewards(CurrentRun);
            }
            else if (CurrentRun.CurrentCombat.State == CombatState.Defeat)
            {
                CurrentRun.CurrentState = GameState.GameOver;
            }
        }
#endregion

#region EVENT & SHOP ACTIONS

        public void ChooseEventOption(int choiceIndex)
        {
            if (CurrentRun.CurrentState != GameState.InEvent) return;
            EventRoomHandler.ResolveChoice(CurrentRun, choiceIndex);
        }

        public bool BuyShopCard(int index)
        {
            return ShopRoomHandler.PurchaseCard(CurrentRun, index);
        }

        public bool BuyShopRelic(int index)
        {
            return ShopRoomHandler.PurchaseRelic(CurrentRun, index);
        }

        public void LeaveShop()
        {
            if (CurrentRun.CurrentState != GameState.InShop) return;
            ShopRoomHandler.LeaveShop(CurrentRun);
        }
#endregion

#region REWARD ACTIONS

        /// <summary>
        /// Picks a card reward and proceeds from the Reward screen.
        /// </summary>
        /// <param name="cardIndex">Index of the card choice, or -1 to skip card.</param>
        public void ConfirmRewards(int cardIndex)
        {
            if (CurrentRun.CurrentState != GameState.AwaitingReward) return;

            if (cardIndex >= 0 && cardIndex < CurrentRun.CardRewardChoices.Count)
            {
                var chosenCard = CurrentRun.CardRewardChoices[cardIndex];
                CurrentRun.TheHero.Deck.AddCardToMasterDeck(chosenCard);
            }

            if (CurrentRun.RelicRewardChoice != null)
            {
                CurrentRun.TheHero.Relics.Add(CurrentRun.RelicRewardChoice);
            }

            CurrentRun.CardRewardChoices.Clear();
            CurrentRun.RelicRewardChoice = null;


            if (CurrentRun.TheMap.GetCurrentRoom().Type == RoomType.Boss)
            {
                CurrentRun.CurrentState = GameState.GameOver;
            }
            else
            {
                CurrentRun.CurrentState = GameState.OnMap;
            }
        }
    }
}
#endregion
