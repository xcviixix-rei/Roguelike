using Roguelike.Data;
using Roguelike.Optimization;
using Roguelike.Core.Handlers;
using Roguelike.Core.Map;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Roguelike.Core
{
    /// <summary>
    /// Defines the major states of the game loop.
    /// </summary>
    public enum GameState
    {
        PreRun,         // Game hasn't started
        OnMap,          // Player is on the map, choosing the next room
        InCombat,       // Player is in an active combat encounter
        InEvent,        // Player is presented with an event choice
        InShop,         // Player is in a shop, buying items
        AwaitingReward, // Combat is over, player is choosing rewards
        GameOver        // The run has ended
    }

    /// <summary>
    /// Represents the complete state of a single game playthrough.
    /// This class is the central hub for all runtime data.
    /// </summary>
    public class GameRun
    {
        public Hero TheHero { get; }
        public MapManager TheMap { get; }
        public Random Rng { get; }

        public GameState CurrentState { get; set; }
        public CombatManager CurrentCombat { get; set; }
        public EventChoiceSet CurrentEvent { get; set; }
        public List<CardData> CardRewardChoices { get; set; } = new List<CardData>();
        public RelicData RelicRewardChoice { get; set; }
        public ShopInventory CurrentShop { get; set; }

        public CardPool CardPool { get; }
        public RelicPool RelicPool { get; }
        public EnemyPool EnemyPool { get; }
        public EffectPool EffectPool { get; }
        public EventPool EventPool { get; }
        public Dictionary<RoomType, RoomData> RoomConfigs { get; }

        public HierarchicalGenome AppliedGenome { get; set; }

        public int CurrentFloor => TheMap.GetCurrentRoom()?.Y ?? -1;

        public GameRun(int seed, HeroData heroData, CardPool cardPool, RelicPool relicPool, EnemyPool enemyPool, EffectPool effectPool, EventPool eventPool, Dictionary<RoomType, RoomData> roomConfigs, HierarchicalGenome genome = null)
        {
            Rng = new Random(seed);
            
            CardPool = cardPool;
            RelicPool = relicPool;
            EnemyPool = enemyPool;
            EffectPool = effectPool;
            EventPool = eventPool;
            RoomConfigs = roomConfigs;
            AppliedGenome = genome;

            TheHero = new Hero(heroData, Rng);
            TheMap = new MapManager();

            TheHero.Deck.InitializeMasterDeck(heroData.StartingDeckCardIds, CardPool);
            var startingRelic = RelicPool.GetRelic(heroData.StartingRelicId);
            if (startingRelic != null)
            {
                TheHero.Relics.Add(startingRelic);
            }
            
            if (genome != null)
            {
                TheMap.GenerateNewMap(seed, genome.RoomTypeWeights, 
                                     genome.MonsterStarRatio, genome.EliteStarRatio);
            }
            else
            {
                TheMap.GenerateNewMap(seed);
            }
            CurrentState = GameState.OnMap;
        }
    }
}
