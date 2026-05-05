using Roguelike.Core;
using Roguelike.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Roguelike.Tests.Combat
{
    public class DeckManagerTests
    {
        [Fact]
        public void Constructor_InitializesEmptyDeck()
        {

            var rng = new Random(42);
            var deckManager = new DeckManager(rng);


            Assert.Empty(deckManager.MasterDeck);
            Assert.Empty(deckManager.DrawPile);
            Assert.Empty(deckManager.Hand);
            Assert.Empty(deckManager.DiscardPile);
            Assert.Empty(deckManager.ExhaustPile);
        }

        [Fact]
        public void InitializeMasterDeck_PopulatesFromCardIds()
        {

            var rng = new Random(42);
            var deckManager = new DeckManager(rng);
            var cardPool = new CardPool();

            var card1 = TestHelpers.CreateBasicAttackCard("card_1");
            var card2 = TestHelpers.CreateBasicDefendCard("card_2");
            cardPool.Initialize(new[] { card1, card2 });

            var cardIds = new List<string> { "card_1", "card_2", "card_1" };


            deckManager.InitializeMasterDeck(cardIds, cardPool);


            Assert.Equal(3, deckManager.MasterDeck.Count);
            Assert.Equal(2, deckManager.MasterDeck.Count(c => c.Id == "card_1"));
            Assert.Single(deckManager.MasterDeck, c => c.Id == "card_2");
        }

        [Fact]
        public void InitializeMasterDeck_IgnoresInvalidCardIds()
        {

            var rng = new Random(42);
            var deckManager = new DeckManager(rng);
            var cardPool = new CardPool();

            var card1 = TestHelpers.CreateBasicAttackCard("card_1");
            cardPool.Initialize(new[] { card1 });

            var cardIds = new List<string> { "card_1", "nonexistent_card", "card_1" };


            deckManager.InitializeMasterDeck(cardIds, cardPool);


            Assert.Equal(2, deckManager.MasterDeck.Count);
        }

        [Fact]
        public void AddCardToMasterDeck_AddsCard()
        {

            var rng = new Random(42);
            var deckManager = new DeckManager(rng);
            var card = TestHelpers.CreateBasicAttackCard();


            deckManager.AddCardToMasterDeck(card);


            Assert.Single(deckManager.MasterDeck);
            Assert.Same(card, deckManager.MasterDeck[0]);
        }

        [Fact]
        public void RemoveCardFromMasterDeck_RemovesCard()
        {

            var rng = new Random(42);
            var deckManager = new DeckManager(rng);
            var card1 = TestHelpers.CreateBasicAttackCard("card_1");
            var card2 = TestHelpers.CreateBasicDefendCard("card_2");

            deckManager.AddCardToMasterDeck(card1);
            deckManager.AddCardToMasterDeck(card2);


            deckManager.RemoveCardFromMasterDeck(card1);


            Assert.Single(deckManager.MasterDeck);
            Assert.Same(card2, deckManager.MasterDeck[0]);
        }

        [Fact]
        public void StartCombat_CopiesMasterDeckToDrawPile()
        {

            var rng = new Random(42);
            var deckManager = new DeckManager(rng);
            var card1 = TestHelpers.CreateBasicAttackCard("card_1");
            var card2 = TestHelpers.CreateBasicDefendCard("card_2");

            deckManager.AddCardToMasterDeck(card1);
            deckManager.AddCardToMasterDeck(card2);


            deckManager.StartCombat();


            Assert.Equal(2, deckManager.DrawPile.Count);
            Assert.Empty(deckManager.Hand);
            Assert.Empty(deckManager.DiscardPile);
            Assert.Empty(deckManager.ExhaustPile);
        }

        [Fact]
        public void StartCombat_ShufflesDrawPile()
        {

            var rng = new Random(42);
            var deckManager = new DeckManager(rng);


            for (int i = 0; i < 10; i++)
            {
                deckManager.AddCardToMasterDeck(TestHelpers.CreateBasicAttackCard($"card_{i}"));
            }

            var orderBeforeShuffle = deckManager.MasterDeck.Select(c => c.Id).ToList();


            deckManager.StartCombat();

            var orderAfterShuffle = deckManager.DrawPile.Select(c => c.Id).ToList();


            Assert.NotEqual(orderBeforeShuffle, orderAfterShuffle);
        }

        [Fact]
        public void DrawCards_MovesCardsToHand()
        {

            var rng = new Random(42);
            var deckManager = new DeckManager(rng);
            deckManager.AddCardToMasterDeck(TestHelpers.CreateBasicAttackCard("card_1"));
            deckManager.AddCardToMasterDeck(TestHelpers.CreateBasicDefendCard("card_2"));
            deckManager.StartCombat();


            deckManager.DrawCards(2);


            Assert.Equal(2, deckManager.Hand.Count);
            Assert.Empty(deckManager.DrawPile);
        }

        [Fact]
        public void DrawCards_WithEmptyDrawPile_ReshufflesDiscard()
        {

            var rng = new Random(42);
            var deckManager = new DeckManager(rng);
            deckManager.AddCardToMasterDeck(TestHelpers.CreateBasicAttackCard("card_1"));
            deckManager.AddCardToMasterDeck(TestHelpers.CreateBasicDefendCard("card_2"));
            deckManager.StartCombat();


            deckManager.DrawCards(2);


            deckManager.DiscardHand();


            deckManager.DrawCards(2);


            Assert.Equal(2, deckManager.Hand.Count);
            Assert.Empty(deckManager.DrawPile);
            Assert.Empty(deckManager.DiscardPile);
        }

        [Fact]
        public void DrawCards_WithBothPilesEmpty_StopsDrawing()
        {

            var rng = new Random(42);
            var deckManager = new DeckManager(rng);
            deckManager.AddCardToMasterDeck(TestHelpers.CreateBasicAttackCard("card_1"));
            deckManager.StartCombat();


            deckManager.DrawCards(5);


            Assert.Single(deckManager.Hand);
        }

        [Fact]
        public void DiscardCardFromHand_MovesCardToDiscard()
        {

            var rng = new Random(42);
            var deckManager = new DeckManager(rng);
            var card = TestHelpers.CreateBasicAttackCard("card_1");
            deckManager.AddCardToMasterDeck(card);
            deckManager.StartCombat();
            deckManager.DrawCards(1);


            deckManager.DiscardCardFromHand(card);


            Assert.Empty(deckManager.Hand);
            Assert.Single(deckManager.DiscardPile);
            Assert.Same(card, deckManager.DiscardPile[0]);
        }

        [Fact]
        public void DiscardCardFromHand_WithCardNotInHand_DoesNothing()
        {

            var rng = new Random(42);
            var deckManager = new DeckManager(rng);
            var cardInHand = TestHelpers.CreateBasicAttackCard("card_1");
            var cardNotInHand = TestHelpers.CreateBasicDefendCard("card_2");

            deckManager.AddCardToMasterDeck(cardInHand);
            deckManager.StartCombat();
            deckManager.DrawCards(1);


            deckManager.DiscardCardFromHand(cardNotInHand);


            Assert.Single(deckManager.Hand);
            Assert.Empty(deckManager.DiscardPile);
        }

        [Fact]
        public void DiscardHand_MovesAllCardsToDiscard()
        {

            var rng = new Random(42);
            var deckManager = new DeckManager(rng);
            deckManager.AddCardToMasterDeck(TestHelpers.CreateBasicAttackCard("card_1"));
            deckManager.AddCardToMasterDeck(TestHelpers.CreateBasicDefendCard("card_2"));
            deckManager.AddCardToMasterDeck(TestHelpers.CreateBasicAttackCard("card_3"));
            deckManager.StartCombat();
            deckManager.DrawCards(3);


            deckManager.DiscardHand();


            Assert.Empty(deckManager.Hand);
            Assert.Equal(3, deckManager.DiscardPile.Count);
        }

        [Fact]
        public void ReshuffleDiscardIntoDraw_MovesAndShufflesCards()
        {

            var rng = new Random(42);
            var deckManager = new DeckManager(rng);

            for (int i = 0; i < 5; i++)
            {
                deckManager.AddCardToMasterDeck(TestHelpers.CreateBasicAttackCard($"card_{i}"));
            }

            deckManager.StartCombat();
            deckManager.DrawCards(5);
            deckManager.DiscardHand();


            deckManager.ReshuffleDiscardIntoDraw();


            Assert.Equal(5, deckManager.DrawPile.Count);
            Assert.Empty(deckManager.DiscardPile);
        }

        [Fact]
        public void Shuffle_ProducesDifferentOrdersWithDifferentSeeds()
        {

            var card1 = TestHelpers.CreateBasicAttackCard("card_1");
            var card2 = TestHelpers.CreateBasicDefendCard("card_2");
            var card3 = TestHelpers.CreateBasicAttackCard("card_3");
            var card4 = TestHelpers.CreateBasicDefendCard("card_4");
            var card5 = TestHelpers.CreateBasicAttackCard("card_5");

            var deckManager1 = new DeckManager(new Random(42));
            deckManager1.AddCardToMasterDeck(card1);
            deckManager1.AddCardToMasterDeck(card2);
            deckManager1.AddCardToMasterDeck(card3);
            deckManager1.AddCardToMasterDeck(card4);
            deckManager1.AddCardToMasterDeck(card5);

            var deckManager2 = new DeckManager(new Random(999));
            deckManager2.AddCardToMasterDeck(card1);
            deckManager2.AddCardToMasterDeck(card2);
            deckManager2.AddCardToMasterDeck(card3);
            deckManager2.AddCardToMasterDeck(card4);
            deckManager2.AddCardToMasterDeck(card5);


            deckManager1.StartCombat();
            deckManager2.StartCombat();

            var order1 = deckManager1.DrawPile.Select(c => c.Id).ToList();
            var order2 = deckManager2.DrawPile.Select(c => c.Id).ToList();


            Assert.NotEqual(order1, order2);
        }

        [Fact]
        public void Shuffle_IsDeterministicWithSameSeed()
        {

            var card1 = TestHelpers.CreateBasicAttackCard("card_1");
            var card2 = TestHelpers.CreateBasicDefendCard("card_2");
            var card3 = TestHelpers.CreateBasicAttackCard("card_3");

            var deckManager1 = new DeckManager(new Random(42));
            deckManager1.AddCardToMasterDeck(card1);
            deckManager1.AddCardToMasterDeck(card2);
            deckManager1.AddCardToMasterDeck(card3);

            var deckManager2 = new DeckManager(new Random(42));
            deckManager2.AddCardToMasterDeck(card1);
            deckManager2.AddCardToMasterDeck(card2);
            deckManager2.AddCardToMasterDeck(card3);


            deckManager1.StartCombat();
            deckManager2.StartCombat();

            var order1 = deckManager1.DrawPile.Select(c => c.Id).ToList();
            var order2 = deckManager2.DrawPile.Select(c => c.Id).ToList();


            Assert.Equal(order1, order2);
        }
    }
}
