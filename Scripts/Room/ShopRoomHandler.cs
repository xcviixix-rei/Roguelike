using Roguelike.Data;
using RoguelikeMapGen;

namespace Roguelike.Logic.Handlers
{
    public class ShopRoomHandler : IRoomHandler
    {
        /// <summary>
        /// Generates a new shop inventory and transitions the game state to await player actions.
        /// </summary>
        public void Execute(GameRun run, Room room)
        {
            run.CurrentShop = new ShopInventory(run.CardPool, run.RelicPool, run.Rng);
            
            run.CurrentState = GameState.InShop;
        }

        /// <summary>
        /// Attempts to purchase a card from the shop. Called by the GameController.
        /// </summary>
        /// <returns>True if the purchase was successful, false otherwise.</returns>
        public static bool PurchaseCard(GameRun run, int cardIndex)
        {
            if (run.CurrentState != GameState.InShop || run.CurrentShop == null) return false;
            if (cardIndex < 0 || cardIndex >= run.CurrentShop.CardsForSale.Count) return false;

            var item = run.CurrentShop.CardsForSale[cardIndex];

            if (item.IsSold || run.TheHero.CurrentGold < item.Price)
            {
                return false;
            }

            run.TheHero.CurrentGold -= item.Price;
            run.TheHero.Deck.AddCardToMasterDeck(item.Item);
            item.IsSold = true;

            return true;
        }

        /// <summary>
        /// Attempts to purchase a relic from the shop. Called by the GameController.
        /// </summary>
        /// <returns>True if the purchase was successful, false otherwise.</returns>
        public static bool PurchaseRelic(GameRun run, int relicIndex)
        {
            if (run.CurrentState != GameState.InShop || run.CurrentShop == null) return false;
            if (relicIndex < 0 || relicIndex >= run.CurrentShop.RelicsForSale.Count) return false;

            var item = run.CurrentShop.RelicsForSale[relicIndex];

            if (item.IsSold || run.TheHero.CurrentGold < item.Price)
            {
                return false;
            }

            run.TheHero.CurrentGold -= item.Price;
            run.TheHero.Relics.Add(item.Item);
            item.IsSold = true;

            return true;
        }

        /// <summary>
        /// Cleans up shop state and returns to the map. Called by the GameController.
        /// </summary>
        public static void LeaveShop(GameRun run)
        {
            run.CurrentShop = null;
            run.CurrentState = GameState.OnMap;
        }
    }
}