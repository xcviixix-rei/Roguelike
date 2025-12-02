using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "charname's StartingDeck", menuName = "Data/Starting Deck")]
public class StartingDeck : ScriptableObject
{
    public List<CardData> cards;
}