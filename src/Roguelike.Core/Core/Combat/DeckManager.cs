using Roguelike.Data;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Roguelike.Core
{


    public class DeckManager
    {
        private readonly Random rng;


        public List<CardData> MasterDeck { get; private set; } = new List<CardData>();

        public List<CardData> DrawPile { get; private set; } = new List<CardData>();
        public List<CardData> Hand { get; private set; } = new List<CardData>();
        public List<CardData> DiscardPile { get; private set; } = new List<CardData>();
        public List<CardData> ExhaustPile { get; private set; } = new List<CardData>();

        public DeckManager(Random randomGenerator)
        {
            rng = randomGenerator;
        }


        public void InitializeMasterDeck(IEnumerable<string> cardIds, CardPool pool)
        {
            MasterDeck.Clear();
            foreach (var id in cardIds)
            {
                var card = pool.GetCard(id);
                if (card != null)
                {
                    MasterDeck.Add(card);
                }
            }
        }


        public void AddCardToMasterDeck(CardData card) => MasterDeck.Add(card);


        public void RemoveCardFromMasterDeck(CardData card) => MasterDeck.Remove(card);


        public void StartCombat()
        {
            DrawPile.Clear();
            Hand.Clear();
            DiscardPile.Clear();
            ExhaustPile.Clear();

            DrawPile.AddRange(MasterDeck);
            Shuffle(DrawPile);
        }


        public void DrawCards(int amount)
        {
            for (int i = 0; i < amount; i++)
            {
                if (DrawPile.Count == 0)
                {
                    if (DiscardPile.Count == 0)
                    {
                        break;
                    }
                    ReshuffleDiscardIntoDraw();
                }

                var cardToDraw = DrawPile[0];
                DrawPile.RemoveAt(0);
                Hand.Add(cardToDraw);
            }
        }


        public void DiscardCardFromHand(CardData card)
        {
            if (Hand.Remove(card))
            {
                DiscardPile.Add(card);
            }
        }


        public void DiscardHand()
        {
            DiscardPile.AddRange(Hand);
            Hand.Clear();
        }


        public void ReshuffleDiscardIntoDraw()
        {
            DrawPile.AddRange(DiscardPile);
            DiscardPile.Clear();
            Shuffle(DrawPile);
        }


        private void Shuffle(List<CardData> list)
        {
            int n = list.Count;
            while (n > 1)
            {
                n--;
                int k = rng.Next(n + 1);
                CardData value = list[k];
                list[k] = list[n];
                list[n] = value;
            }
        }
    }
}
