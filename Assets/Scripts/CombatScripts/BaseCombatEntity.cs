using UnityEngine;
using UnityEngine.UI;

public class BaseCombatEntity : MonoBehaviour
{
    [Header("References")]
    public BaseIconInteractable iconController;
    public Player parentPlayer;

    [Header("General Parameters")]
    [Tooltip("Whether or not a player can ever move this entity")]
    public bool isStatic = false;

    [Header("Movement")]
    [Tooltip("The max pullback (radius) for the icon interactable")]
    public float maxPullback;

    //============================================================================

    [HideInInspector] public bool isCorrectTurn = false;
    private WorldspaceCanvasSyncUtility syncUtility;

    //============================================================================

    private void Awake()
    {
        //Call setup for icon controller
        iconController.SetBaseCombatEntity(this);
        iconController.SetOnReleaseAction(Release);

        //Initialize references
        syncUtility = iconController.gameObject.GetComponent<WorldspaceCanvasSyncUtility>();
    }

    //============================================================================

    private void Release(Vector3 launchDirection, float launchSpeed)
    {
        Debug.Log(launchDirection);
        Debug.Log(launchSpeed);

        transform.Translate(launchDirection * launchSpeed);

        //TEMP
        parentPlayer.EndTurn();
    }

    public void SetInteractable(bool interactable)
    {
        isCorrectTurn = interactable;
        iconController.gameObject.GetComponent<Image>().raycastTarget = interactable;
        syncUtility.SnapUIToObject();
    }
}
