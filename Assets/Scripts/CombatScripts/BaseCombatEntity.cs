using UnityEngine;

public class BaseCombatEntity : MonoBehaviour
{
    [Header("References")]
    public BaseIconInteractable iconController;

    [Header("General Parameters")]
    [Tooltip("Whether or not a player can actually move this entity")]
    public bool isMovable;

    [Header("Movement")]
    [Tooltip("The max pullback (radius) for the icon interactable")]
    public float maxPullback;

    //============================================================================

    private void Awake()
    {
        //Call setup for icon controller
        iconController.SetBaseCombatEntity(this);
        iconController.SetOnReleaseAction(Release);
    }

    private void Release(Vector2 releaseVector)
    {
        
    }
}
