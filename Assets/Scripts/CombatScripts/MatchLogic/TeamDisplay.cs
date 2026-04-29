using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TeamDisplay : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Button selectButton;
    [SerializeField] private TMP_Text selectButtonText;
    [SerializeField] private Transform entryParentTransform;

    [Header("Prefab")]
    [SerializeField] private GameObject gladiatorEntryGO;

    //============================================================================

    public void SetButtonInteractable(bool val)
    {
        selectButton.interactable = val;
    }

    public void SetSignature(int playerIndex)
    {
        selectButtonText.text = "Player " + (playerIndex + 1);
        selectButtonText.color = CombatManager.instance.GetPlayer(playerIndex).playerColor;
    }

    public void DisplayGladiators(List<GladiatorType> gladiatorsList)
    {
        foreach(GladiatorType type in gladiatorsList)
        {
            GameObject entryGO = Instantiate(gladiatorEntryGO);
            entryGO.transform.SetParent(entryParentTransform);
            GladiatorEntry entry = entryGO.GetComponent<GladiatorEntry>();
            entry.SetGladiator(type);
        }
    }
}
