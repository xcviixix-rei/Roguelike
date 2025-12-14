using System;
using System.Collections.Generic;
using System.Linq;

namespace Roguelike.Data
{
    /// <summary>
    /// A generic class representing a single item with an associated weight.
    /// Used for making weighted random selections
    /// </summary>
    /// <typeparam name="T">The type of the item being chosen</typeparam>
    public class WeightedChoice<T>
    {
        public T Item { get; set; }
        public int Weight { get; set; }

        public WeightedChoice(T item, int weight)
        {
            if (weight <= 0)
            {
                Console.Error.WriteLine("weight must be > 0");
            }
            Item = item;
            Weight = weight;
        }
    }

    /// <summary>
    /// A static helper class to perform weighted random selections on a list of WeightedChoice objects
    /// </summary>
    public static class WeightedRandom
    {
        /// <summary>
        /// Picks an item from a list of weighted choices
        /// </summary>
        /// <param name="choices">A list of items with associated weights</param>
        /// <param name="rng">An instance of Random to use for the selection</param>
        /// <returns>The chosen item of type T, or default(T) if the list is empty or all weights are zero</returns>
        public static T Pick<T>(List<WeightedChoice<T>> choices, Random rng)
        {
            if (choices == null || choices.Count == 0)
            {
                return default(T);
            }

            int totalWeight = choices.Sum(c => c.Weight);
            if (totalWeight <= 0)
            {
                return default(T);
            }

            int randomNumber = rng.Next(0, totalWeight);
            int cumulativeWeight = 0;

            foreach (var choice in choices)
            {
                cumulativeWeight += choice.Weight;
                if (randomNumber < cumulativeWeight)
                {
                    return choice.Item;
                }
            }

            return choices.Last().Item; 
        }
    }
}
