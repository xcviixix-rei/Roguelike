namespace Roguelike.Core
{
    /// <summary>
    /// A generic wrapper for an item being sold in a shop.
    /// It holds the item itself, its price, and whether it has been sold.
    /// </summary>
    public class ShopItem<T>
    {
        public T Item { get; }
        public int Price { get; }
        public bool IsSold { get; set; }

        public ShopItem(T item, int price)
        {
            Item = item;
            Price = price;
            IsSold = false;
        }
    }
}
