using System.Collections.Generic;
using UnityEngine;

public class CombatManager : MonoBehaviour
{
    public static CombatManager instance;

    [Header("Player Management")]
    public List<Player> playerList;
    public int currentPlayerIndex;

    [Header("Component References")]
    public Canvas combatCanvas;

    [Header("Object References")]
    public Transform combatCanvasTransform;
    public Transform combatEnvironmentTransform;
    public Transform defaultSpawnTransform;

    //============================================================================
    
    private void Awake()
    {
        instance = this;
    }

    private void Start()
    {
        //TODO: Get a fun way to choose who goes first
        currentPlayerIndex = 0;

        //Do initial turn
        StartPlayerTurn();
    }

    //============================Turn Management=================================

    //Note that a turn goes like this:
    //  Top-down pullback selection
    //  Cinematic camera follow-through for actual token launch

    public void StartPlayerTurn()
    {
        playerList[currentPlayerIndex].StartTurn();
    }

    public void EndPlayerTurn()
    {
        currentPlayerIndex = (currentPlayerIndex + 1) % playerList.Count;

        //Do next turn
        StartPlayerTurn();
    }

    //============================Getters/Setters=================================

    public Player GetCurrentPlayer()
    {
        return playerList[currentPlayerIndex];
    }
}
