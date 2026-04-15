using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour
{
    public List<BaseCombatEntity> ownedEntities;

    //============================================================================

    private void Awake()
    {
        //For testing purposes, set all owned combat entities' owned player
        foreach(BaseCombatEntity entity in ownedEntities)
        {
            entity.parentPlayer = this;
        }
    }

    //============================================================================

    public void StartTurn()
    {
        //Set all owned combat entities to interactable
        foreach(BaseCombatEntity entity in ownedEntities)
        {
            entity.SetInteractable(true);
        }
    }

    public void EndTurn()
    {
        //Set all owned combat entities to not interactable
        foreach(BaseCombatEntity entity in ownedEntities)
        {
            entity.SetInteractable(false);
        }

        CombatManager.instance.EndPlayerTurn();
    }

    //============================================================================

    public void SpawnEntity(BaseCombatEntity entity)
    {
        
    }
}
