using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TeamGenerator : MonoBehaviour
{
    [Serializable]
    public class Team
    {
        public List<GladiatorType> gladiatorsOnThisTeam;
        [Tooltip("The indices of the other teams in the teamList that this team should go against")]
        public List<int> matchups;
    }

    public static TeamGenerator instance;

    //============================================================================

    [Header("UI References")]
    [SerializeField] private GameObject teamSelectScreenParent;
    [SerializeField] private TMP_Text choosingPlayerText;

    //============================================================================

    [Header("Team Spawn Helpers (one for each player)")]
    public List<TeamSpawner> spawnHelpers;
    
    [Header("Team Display Objects")]
    [SerializeField] private List<TeamDisplay> teamDisplaysList;

    [Header("Gladiators")]
    public List<GameObject> gladiatorList;

    [Header("Matchups")]
    public List<Team> teamList;

    //============================================================================

    private List<Team> generatedTeams;
    private int[] teamOwners;
    private int currentChoosingPlayer;
    private List<int> playersLeftToChoose;

    //============================================================================

    private void Awake()
    {
        instance = this;
    }

    private void Start()
    {
        GenerateTeams();
    }

    //============================================================================

    public void GenerateTeams()
    {
        //Initialize all lists
        generatedTeams = new List<Team>();
        teamOwners = new int[CombatManager.instance.playerList.Count];
        playersLeftToChoose = new List<int>();

        //Initialize the player who will start choosing
        for(int i = 0; i < CombatManager.instance.playerList.Count; i++)
        {
            playersLeftToChoose.Add(i);
        }
        SelectRandomPlayer();

        //Generate initial team
        generatedTeams.Add(teamList[UnityEngine.Random.Range(0, teamList.Count)]);

        for(int i = 1; i < CombatManager.instance.playerList.Count; i++)
        {
            //Generate a team from the matchup list of the previously generated team
            generatedTeams.Add(teamList[generatedTeams[i-1].matchups[UnityEngine.Random.Range(0, generatedTeams[i-1].matchups.Count)]]);
        }

        //For each of the generated teams, display properly
        for(int i = 0; i < generatedTeams.Count; i++)
        {
            teamDisplaysList[i].DisplayGladiators(generatedTeams[i].gladiatorsOnThisTeam);
        }
    }

    public void SelectTeam(int teamToSelect)
    {
        teamOwners[teamToSelect - 1] = currentChoosingPlayer; //Set team owner

        //Make this button no longer active
        teamDisplaysList[teamToSelect - 1].SetButtonInteractable(false);

        //Also fill in player signature
        teamDisplaysList[teamToSelect - 1].SetSignature(currentChoosingPlayer);

        //Check to start match
        if(playersLeftToChoose.Count == 0)
        {
            StartMatch();
        }
        else
        {
            SelectRandomPlayer();
        }
    }

    //============================================================================

    private void SelectRandomPlayer()
    {
        //Pick a random player and remove it from playersLeftToChoose
        int index = UnityEngine.Random.Range(0, playersLeftToChoose.Count);
        currentChoosingPlayer = playersLeftToChoose[index];
        playersLeftToChoose.RemoveAt(index);

        //Update text content and color
        choosingPlayerText.text = "Player " + (currentChoosingPlayer + 1) + ": Select a team";
        choosingPlayerText.color = CombatManager.instance.GetPlayer(currentChoosingPlayer).playerColor;
    }
    
    private void StartMatch()
    {
        ScreenTransitionManager.instance.Transition_VisibleToBlocked(() =>
        {
            //On complete...
            //Spawn in all gladiators accordingly
            for(int i = 0; i < generatedTeams.Count; i++)
            {
                spawnHelpers[teamOwners[i]].SpawnGladiators(teamOwners[i], GladiatorsEnumToPrefab(generatedTeams[i].gladiatorsOnThisTeam));
            }

            //Hide team select screen
            teamSelectScreenParent.SetActive(false);

            //Set cam to top down
            CombatCameraController.instance.inCombat = true;

            //Transition back
            ScreenTransitionManager.instance.Transition_BlockedToVisible(() =>
            {
                //Do initial turn
                CombatManager.instance.StartPlayerTurn();
            });
        });
    }

    private List<GameObject> GladiatorsEnumToPrefab(List<GladiatorType> enumList)
    {
        List<GameObject> listToReturn = new List<GameObject>();
        
        foreach(GladiatorType gladiatorType in enumList)
        {
            listToReturn.Add(gladiatorList[(int)gladiatorType]);
        }

        return listToReturn;
    }
}

public enum GladiatorType
{
    Test,
    Secutor,
    Thrace,
    Murmillo
}
