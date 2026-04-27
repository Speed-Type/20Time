using System;
using System.Collections.Generic;
using UnityEngine;

public class CombatManager : MonoBehaviour
{
    public static CombatManager instance;

    [Header("Player Management")]
    public List<Player> playerList;
    public int currentPlayerIndex;

    [Header("Camera Controller")]
    public CombatCameraController camController;

    [Header("Component References")]
    public Canvas combatCanvas;

    [Header("Object References")]
    public Transform combatCanvasTransform;
    public Transform combatEnvironmentTransform;
    public Transform defaultSpawnTransform;

    //============================================================================
    [HideInInspector] public CanvasGroup combatCanvasGroup;
    
    private void Awake()
    {
        instance = this;

        //Initialize references
        combatCanvasGroup = combatCanvas.GetComponent<CanvasGroup>();
    }

    private void Start()
    {
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
        combatCanvasGroup.alpha = 1;
    }

    //Called upon valid token launch
    public void SwitchCamToFollow(Token launchedToken, Action onCompleteAction = null)
    {
        combatCanvasGroup.alpha = 0;
        camController.EnterFollowMode(launchedToken, onCompleteAction);
    }

    public void EndPlayerTurn()
    {
        currentPlayerIndex = (currentPlayerIndex + 1) % playerList.Count;

        //Switch cam back to top-down
        SwitchCamToTopDown();

        //End game
        CheckGameEnd();
    }

    public void SwitchCamToTopDown()
    {
        //Cam transition to top-down
        camController.ExitFollowMode(() => //After the transition is done:
        {
            //Do next turn
            StartPlayerTurn();
        });
    }

    public void CheckGameEnd()
    {
        List<Player> playersStillIn = new List<Player>();

        foreach(Player player in playerList)
        {
            if(player.hasEntities())
            {
                playersStillIn.Add(player);
            }
        }

        if(playersStillIn.Count == 1)
        {
            //A player won
        }
        else if(playersStillIn.Count == 0)
        {
            //A tie
        }
        //Otherwise, game continues
    }

    //============================Getters/Setters=================================

    public Player GetCurrentPlayer()
    {
        return playerList[currentPlayerIndex];
    }
}
