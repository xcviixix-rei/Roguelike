using Roguelike.Logic;

namespace Roguelike.Agents
{
    /// <summary>
    /// Represents the types of actions a player can take during their turn in combat.
    /// </summary>
    public enum CombatActionType
    {
        PlayCard,
        EndTurn
    }

    /// <summary>
    /// A structure representing a single decision during combat.
    /// </summary>
    public struct CombatDecision
    {
        public CombatActionType Type;
        public int HandIndex;
        public int TargetIndex;

        public static CombatDecision EndTurn() => new CombatDecision { Type = CombatActionType.EndTurn };
        public static CombatDecision Play(int handIndex, int targetIndex) => 
            new CombatDecision { Type = CombatActionType.PlayCard, HandIndex = handIndex, TargetIndex = targetIndex };
    }

    /// <summary>
    /// Represents the types of actions a player can take in a shop.
    /// </summary>
    public enum ShopActionType
    {
        BuyCard,
        BuyRelic,
        Leave
    }

    /// <summary>
    /// A structure representing a single decision inside a shop.
    /// </summary>
    public struct ShopDecision
    {
        public ShopActionType Type;
        public int ShopIndex;

        public static ShopDecision Leave() => new ShopDecision { Type = ShopActionType.Leave };
        public static ShopDecision BuyCard(int index) => new ShopDecision { Type = ShopActionType.BuyCard, ShopIndex = index };
        public static ShopDecision BuyRelic(int index) => new ShopDecision { Type = ShopActionType.BuyRelic, ShopIndex = index };
    }

    /// <summary>
    /// The interface that any AI or Player Wrapper must implement.
    /// It maps specific GameStates to decision methods.
    /// </summary>
    public interface IPlayerAgent
    {
        /// <summary>
        /// Called when GameState is OnMap.
        /// Should return the ID of the room to travel to next.
        /// </summary>
        int ChooseMapNode(GameRun run);

        /// <summary>
        /// Called when GameState is InCombat and it is the Player's turn.
        /// Should return the next action to take (Play a specific card or End Turn).
        /// </summary>
        CombatDecision GetCombatDecision(GameRun run);

        /// <summary>
        /// Called when GameState is InEvent.
        /// Should return the index of the choice to select.
        /// </summary>
        int ChooseEventOption(GameRun run);

        /// <summary>
        /// Called when GameState is InShop.
        /// Should return what to buy or whether to leave.
        /// </summary>
        ShopDecision GetShopDecision(GameRun run);

        /// <summary>
        /// Called when GameState is AwaitingReward.
        /// Should return the index of the card reward to pick, or -1 to skip.
        /// </summary>
        int ChooseCardReward(GameRun run);
    }
}