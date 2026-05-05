using System;
using System.Collections.Generic;
using System.Linq;

namespace Roguelike.Data
{


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


    public static class WeightedRandom
    {


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
