using System.Collections.Generic;
using UnityEngine;

public class TeamSpawner : MonoBehaviour
{
    [SerializeField] private List<Transform> spawnPositions;

    public void SpawnGladiators(int playerOwner, List<GameObject> gladiatorsToSpawn)
    {
        //Note: the function we're calling is SpawnEntity(GameObject entityPrefab, Vector3 pSpawnPosition)
        for(int i = 0; i < gladiatorsToSpawn.Count; i++)
        {
            CombatManager.instance.GetPlayer(playerOwner).SpawnEntity(gladiatorsToSpawn[i].gameObject, spawnPositions[i].position);
        }
    }
}
