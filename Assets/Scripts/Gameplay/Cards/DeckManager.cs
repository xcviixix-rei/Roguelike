using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Manages the state of all card piles for a combatant (typically the player).
/// This is a plain C# class that handles the logic of drawing, discarding,
/// shuffling, and moving cards between piles.
/// </summary>
public class DeckManager
{
    private List<RuntimeCard> drawPile = new List<RuntimeCard>();
    private List<RuntimeCard> hand = new List<RuntimeCard>();
    private List<RuntimeCard> discardPile = new List<RuntimeCard>();
    private List<RuntimeCard> exhaustPile = new List<RuntimeCard>();
    
    private System.Random rng = new System.Random();

    public IReadOnlyList<RuntimeCard> Hand => hand;
    public IReadOnlyList<RuntimeCard> DrawPile => drawPile;
    public IReadOnlyList<RuntimeCard> DiscardPile => discardPile;
    public IReadOnlyList<RuntimeCard> ExhaustPile => exhaustPile;
    
    #region Initialization

    /// <summary>
    /// Initializes the DeckManager with a list of CardData, creating RuntimeCard instances
    /// and preparing the draw pile for the start of combat.
    /// </summary>
    /// <param name="startingCards">A list of CardData ScriptableObjects representing the full deck.</param>
    public void Initialize(List<CardData> startingCards)
    {
        drawPile.Clear();
        hand.Clear();
        discardPile.Clear();
        exhaustPile.Clear();

        foreach (var cardData in startingCards)
        {
            drawPile.Add(new RuntimeCard(cardData));
        }

        Debug.Log($"DeckManager Initialized with {drawPile.Count} cards.");

        Shuffle();
    }

    /// <summary>
    /// Shuffles the draw pile using the Fisher-Yates algorithm.
    /// </summary>
    private void Shuffle()
    {
        int n = drawPile.Count;
        while (n > 1)
        {
            n--;
            int k = rng.Next(n + 1);
            (drawPile[k], drawPile[n]) = (drawPile[n], drawPile[k]);
        }
        Debug.Log("Draw pile has been shuffled.");
    }
    
    #endregion

    #region Core Gameplay Actions

    /// <summary>
    /// Draws a specified number of cards from the draw pile into the hand.
    /// If the draw pile is empty, it shuffles the discard pile back into it.
    /// </summary>
    /// <param name="amount">The number of cards to draw.</param>
    public void DrawCards(int amount)
    {
        for (int i = 0; i < amount; i++)
        {
            if (drawPile.Count == 0)
            {
                if (discardPile.Count == 0)
                {
                    Debug.Log("No cards left in draw or discard pile to draw.");
                    break;
                }
                
                ReshuffleDiscardPile();
            }

            RuntimeCard cardToDraw = drawPile[drawPile.Count - 1];
            drawPile.RemoveAt(drawPile.Count - 1);
            hand.Add(cardToDraw);

            Debug.Log($"Drew card: {cardToDraw.GetName()}");
            GameEvents.InvokeCardDrawn(cardToDraw);
        }
    }

    /// <summary>
    /// Moves a specific card from the hand to the discard pile.
    /// </summary>
    /// <param name="card">The card instance to play.</param>
    public void PlayCard(RuntimeCard card)
    {
        if (hand.Remove(card))
        {
            discardPile.Add(card);
            Debug.Log($"Played card: {card.GetName()}");
            // TODO (Phase 4): Invoke GameEvents.OnCardPlayed(card);
        }
        else
        {
            Debug.LogError($"Attempted to play card '{card.GetName()}' not found in hand!");
        }
    }

    /// <summary>
    /// Moves all cards currently in the hand to the discard pile.
    /// Typically called at the end of the player's turn.
    /// </summary>
    public void DiscardHand()
    {
        if (hand.Count == 0) return;
        
        discardPile.AddRange(hand);
        hand.Clear();
        
        Debug.Log("Player's hand was discarded.");
        // TODO (Phase 4): Invoke GameEvents.OnHandDiscarded();
    }

    /// <summary>
    /// Moves the discard pile into the draw pile and shuffles it.
    /// </summary>
    private void ReshuffleDiscardPile()
    {
        Debug.Log("Reshuffling discard pile into draw pile.");
        drawPile.AddRange(discardPile);
        discardPile.Clear();
        Shuffle();
        // TODO (Phase 4): Invoke GameEvents.OnDeckReshuffled();
    }

    #endregion
}