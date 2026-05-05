namespace Roguelike.Core
{


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
