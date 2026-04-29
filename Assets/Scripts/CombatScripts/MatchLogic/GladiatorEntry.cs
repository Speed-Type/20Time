using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

public class GladiatorEntry : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("UI References")]
    [SerializeField] private TMP_Text nameText;

    private BaseCombatEntity entity;

    //============================================================================

    public void SetGladiator(GladiatorType gladiatorType)
    {
        //Get owned baseCombatEntity (lots of wiring)
        entity = TeamGenerator.instance.gladiatorList[(int)gladiatorType].GetComponent<CombatEntitySpawnHelper>().GetBaseCombatEntity();
    
        //Set text
        nameText.text = gladiatorType.ToString();
    }

    //============================================================================

    public void OnPointerEnter(PointerEventData eventData)
    {
        //Show tooltip
        string healthText = "Health: " + entity.maxHealth.ToString("F2");
        TooltipManager.instance.ShowTooltip(entity.entityName, healthText, entity.entityLongDescription);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        TooltipManager.instance.HideTooltip();
    }
}
