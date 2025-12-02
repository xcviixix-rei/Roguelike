using System.Collections.Generic;
using System;
using UnityEngine;
using Sirenix.OdinInspector;

/// <summary>
/// A serializable class to represent a single action an enemy can take.
/// This is NOT a ScriptableObject; it's just a data container used by EnemyData.
/// </summary>
[Serializable]
public class EnemyMove
{
    public enum MoveType { Attack, Defend, Buff, Debuff }
    
    public MoveType moveType;
    public Sprite intentIcon;
    public int value; // Damage amount, block amount, status stacks, etc.
}

[CreateAssetMenu(menuName = "Data/Enemy Data")]
public class EnemyData : ScriptableObject
{
    [TitleGroup("Core Information")]
    public string enemyID;
    public string enemyName;
    public GameObject enemyPrefab;

    // --- GENETIC ALGORITHM WILL TUNE THESE VALUES ---
    [TitleGroup("Balancing Values (The 'Genes')")]
    [BoxGroup("Balancing Values (The 'Genes')/Stats")]
    public int maxHP;
    [BoxGroup("Balancing Values (The 'Genes')/Stats")]
    public int startingStrength;
    // ------------------------------------------------

    [Title("AI Behavior")]
    [InfoBox("This is the sequence of moves the enemy will cycle through.")]
    public List<EnemyMove> movePattern = new List<EnemyMove>();
}