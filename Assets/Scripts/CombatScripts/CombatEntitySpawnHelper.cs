using System;
using System.Collections.Generic;
using UnityEngine;

public class CombatEntitySpawnHelper : MonoBehaviour
{
    public BaseCombatEntity entity;
    public BaseIconInteractable icon;

    //============================================================================

    public BaseCombatEntity GetBaseCombatEntity()
    {
        return entity;
    }

    public void SetEntityOwner(Player player)
    {
        entity.parentPlayer = player;
    }

    public void PositionObjects(Vector3 pSpawnPosition)
    {
        //Parent entity and icon under the right objects
        entity.gameObject.transform.SetParent(CombatManager.instance.combatEnvironmentTransform);
        icon.gameObject.transform.SetParent(CombatManager.instance.combatCanvasTransform);

        //Position the actual entity in the right position
        entity.gameObject.transform.position = pSpawnPosition;

        //Position the icon correctly
        entity.InitializeInteractable();

        Destroy(this.gameObject, 1f); //Destroy this object 1 second after spawning, just to avoid clutter
    }
}
