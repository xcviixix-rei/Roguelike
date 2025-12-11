using Roguelike.Data;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Roguelike.Logic
{
    /// <summary>
    /// Manages the state of the hero's deck throughout a run and during combat.
    /// Handles card piles, drawing, shuffling, and modifications to the master deck.
    /// </summary>
    public class DeckManager
    {
        private readonly Random rng;

        /// <summary>
        /// The complete list of cards the hero owns for the entire run
        /// </summary>
        public List<CardData> MasterDeck { get; private set; } = new List<CardData>();

        public List<CardData> DrawPile { get; private set; } = new List<CardData>();
        public List<CardData> Hand { get; private set; } = new List<CardData>();
        public List<CardData> DiscardPile { get; private set; } = new List<CardData>();
        public List<CardData> ExhaustPile { get; private set; } = new List<CardData>();

        public DeckManager(Random randomGenerator)
        {
            rng = randomGenerator;
        }

        /// <summary>
        /// Populates the MasterDeck based on a list of card IDs and the CardPool.
        /// </summary>
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

        /// <summary>
        /// Adds a card to the master deck permanently. Used for rewards.
        /// </summary>
        public void AddCardToMasterDeck(CardData card) => MasterDeck.Add(card);

        /// <summary>
        /// Removes a card from the master deck permanently. Used for shop/event removals.
        /// </summary>
        public void RemoveCardFromMasterDeck(CardData card) => MasterDeck.Remove(card);

        /// <summary>
        /// Prepares the deck for a new combat encounter.
        /// </summary>
        public void StartCombat()
        {
            DrawPile.Clear();
            Hand.Clear();
            DiscardPile.Clear();
            ExhaustPile.Clear();

            DrawPile.AddRange(MasterDeck);
            Shuffle(DrawPile);
        }

        /// <summary>
        /// Draws a specified number of cards into the hand.
        /// </summary>
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

        /// <summary>
        /// Moves a card from the hand to the discard pile.
        /// </summary>
        public void DiscardCardFromHand(CardData card)
        {
            if (Hand.Remove(card))
            {
                DiscardPile.Add(card);
            }
        }

        /// <summary>
        /// Moves all cards from the hand to the discard pile.
        /// </summary>
        public void DiscardHand()
        {
            DiscardPile.AddRange(Hand);
            Hand.Clear();
        }

        /// <summary>
        /// Moves all cards from the discard pile into the draw pile and shuffles it.
        /// </summary>
        public void ReshuffleDiscardIntoDraw()
        {
            DrawPile.AddRange(DiscardPile);
            DiscardPile.Clear();
            Shuffle(DrawPile);
        }

        /// <summary>
        /// Shuffles a list of cards using the Fisher-Yates algorithm.
        /// </summary>
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