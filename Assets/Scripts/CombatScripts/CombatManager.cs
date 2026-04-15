using System.Collections.Generic;
using UnityEngine;

public class CombatManager : MonoBehaviour
{
    public static CombatManager instance;

    [Header("Player Management")]
    public List<Player> playerList;
    public int currentPlayerIndex;

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
}
