using System;
using System.Collections.Generic;
using TMPro;
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
    public CanvasGroup screenspaceCombatUICanvasGroup;
    public TMP_Text currentTurnDisplay;

    [Header("Object References")]
    public Transform combatCanvasTransform;
    public Transform combatEnvironmentTransform;
    public Transform defaultSpawnTransform;

    [Header("Game End")]
    [SerializeField] private CanvasGroup gameEndCanvasGroup;
    [SerializeField] private TMP_Text gameResultText;
    [SerializeField] private float delayBeforeShowingGameEndUI;
    [SerializeField] private float gameEndUIFadeTime;

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

        //Hide necessary UI
        SetCanvasGroupActive(screenspaceCombatUICanvasGroup, false);
        SetCanvasGroupActive(gameEndCanvasGroup, false);

        //Initialize turn text
        //Also set the current turn text
        currentTurnDisplay.text = "Player " + (currentPlayerIndex + 1) + "'s turn";
        currentTurnDisplay.color = playerList[currentPlayerIndex].playerColor;
    }

    //============================Turn Management=================================

    //Note that a turn goes like this:
    //  Top-down pullback selection
    //  Cinematic camera follow-through for actual token launch

    public void StartPlayerTurn()
    {
        SetCanvasGroupActive(screenspaceCombatUICanvasGroup, true);
        playerList[currentPlayerIndex].StartTurn();
        SetCanvasGroupActive(combatCanvasGroup, true);

        //Also set the current turn text
        currentTurnDisplay.text = "Player " + (currentPlayerIndex + 1) + "'s turn";
        currentTurnDisplay.color = playerList[currentPlayerIndex].playerColor;
    }

    //Called upon valid token launch
    public void SwitchCamToFollow(Token launchedToken, Action onCompleteAction = null)
    {
        SetCanvasGroupActive(combatCanvasGroup, false);
        camController.EnterFollowMode(launchedToken, onCompleteAction);
    }

    public void EndPlayerTurn()
    {
        currentPlayerIndex = (currentPlayerIndex + 1) % playerList.Count;

        //Switch cam back to top-down
        SwitchCamToTopDown();
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
            EndGame("Player " + (playersStillIn[0].playerID + 1) + " Win");

            //Also set the color of the game result text to the winning player's color
            gameResultText.color = playersStillIn[0].playerColor;
        }
        else if(playersStillIn.Count == 0)
        {
            //A tie
            EndGame("Players Tied");
        }
        //Otherwise, game continues
    }

    private void EndGame(string resultText)
    {
        //Set results text
        gameResultText.text = resultText;

        //Freeze camera
        CombatCameraController.instance.inCombat = false;

        //Freeze physics system
        CollisionSystem.instance.simulatePhysics = false;

        //Tween in the end game UI
        LeanTween.delayedCall(delayBeforeShowingGameEndUI, () =>
        {
            //Fade in the end game UI
            SetCanvasGroupActive(gameEndCanvasGroup, true);
            gameEndCanvasGroup.alpha = 0;
            LeanTween.value(0, 1, gameEndUIFadeTime)
            .setOnUpdate((float val) =>
            {
                gameEndCanvasGroup.alpha = val;
            })
            .setIgnoreTimeScale(true);

            //Fade out the combat UI
            LeanTween.value(1, 0, gameEndUIFadeTime)
            .setOnUpdate((float val) =>
            {
                screenspaceCombatUICanvasGroup.alpha = val;
            })
            .setOnUpdate((float val) =>
            {
                //At the end, set the combat UI completely not interactable
                SetCanvasGroupActive(screenspaceCombatUICanvasGroup, false);
            })
            .setIgnoreTimeScale(true);
        })
        .setIgnoreTimeScale(true);
    }

    //============================Getters/Setters=================================

    public Player GetCurrentPlayer()
    {
        return GetPlayer(currentPlayerIndex);
    }

    public Player GetPlayer(int index)
    {
        return playerList[index];
    }

    //============================Helper methods=================================

    private void SetCanvasGroupActive(CanvasGroup group, bool active)
    {
        group.alpha = active ? 1 : 0;
        group.blocksRaycasts = active;
        group.interactable = active;
    }
}
