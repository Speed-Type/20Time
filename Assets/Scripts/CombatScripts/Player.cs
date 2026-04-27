using System;
using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour
{
    [Header("Player Info")]
    public int playerID;
    public Color playerColor;

    public List<BaseCombatEntity> ownedEntities;
    public GameObject testEntityPrefab;
    public bool isThisPlayersTurn = false;

    //============================================================================

    private void Awake()
    {
        ownedEntities = new List<BaseCombatEntity>();
    }

    //============================================================================

    public void StartTurn()
    {
        isThisPlayersTurn = true;
        
        //Set all owned combat entities to interactable
        foreach(BaseCombatEntity entity in ownedEntities)
        {
            entity.SetInteractable(true);
        }
    }

    public void EndTurn()
    {
        isThisPlayersTurn = false;

        //Set all owned combat entities to not interactable
        foreach(BaseCombatEntity entity in ownedEntities)
        {
            entity.SetInteractable(false);
        }

        CombatManager.instance.EndPlayerTurn();
    }

    //============================================================================

    public void SpawnEntity()
    {
        //TEMP: spawned game object will be temp prefab
        SpawnEntity(testEntityPrefab, CombatManager.instance.defaultSpawnTransform.position);
    }

    public void SpawnEntity(GameObject entityPrefab, Vector3 pSpawnPosition)
    {
        GameObject spawnedEntity = Instantiate(entityPrefab); //Instantiate the prefab

        CombatEntitySpawnHelper spawnHelper = spawnedEntity.GetComponent<CombatEntitySpawnHelper>();

        //Save this entity to the owned list
        ownedEntities.Add(spawnHelper.GetBaseCombatEntity());

        spawnHelper.SetEntityOwner(this);
        spawnHelper.PositionObjects(pSpawnPosition);
    }

    //============================================================================

    public bool hasEntities()
    {
        return ownedEntities.Count > 0;
    }
}
