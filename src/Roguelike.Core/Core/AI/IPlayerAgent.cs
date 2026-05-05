using Roguelike.Core;

namespace Roguelike.Core.AI
{


    public enum CombatActionType
    {
        PlayCard,
        EndTurn
    }


    public struct CombatDecision
    {
        public CombatActionType Type;
        public int HandIndex;
        public int TargetIndex;

        public static CombatDecision EndTurn() => new CombatDecision { Type = CombatActionType.EndTurn };
        public static CombatDecision Play(int handIndex, int targetIndex) =>
            new CombatDecision { Type = CombatActionType.PlayCard, HandIndex = handIndex, TargetIndex = targetIndex };
    }


    public enum ShopActionType
    {
        BuyCard,
        BuyRelic,
        Leave
    }


    public struct ShopDecision
    {
        public ShopActionType Type;
        public int ShopIndex;

        public static ShopDecision Leave() => new ShopDecision { Type = ShopActionType.Leave };
        public static ShopDecision BuyCard(int index) => new ShopDecision { Type = ShopActionType.BuyCard, ShopIndex = index };
        public static ShopDecision BuyRelic(int index) => new ShopDecision { Type = ShopActionType.BuyRelic, ShopIndex = index };
    }


    public interface IPlayerAgent
    {


        int ChooseMapNode(GameRun run);


        CombatDecision GetCombatDecision(GameRun run);


        int ChooseEventOption(GameRun run);


        ShopDecision GetShopDecision(GameRun run);


        int ChooseCardReward(GameRun run);
    }
}
