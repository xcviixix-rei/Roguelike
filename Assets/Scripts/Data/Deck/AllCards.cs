using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "charname's AllCards", menuName = "Data/All Cards")]
public class AllCards : ScriptableObject
{
    public List<CardData> cards;
}