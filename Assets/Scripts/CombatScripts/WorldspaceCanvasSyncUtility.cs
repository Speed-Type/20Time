using UnityEngine;

public class WorldspaceCanvasSyncUtility : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera worldCamera;
    [SerializeField] private Canvas canvas;
    [SerializeField] private RectTransform draggableRect;
    [SerializeField] private Transform targetObject;

    [Header("Optional Offset")]
    [Tooltip("Local offset in canvas units applied after snapping, e.g. to nudge vertically")]
    [SerializeField] private Vector2 canvasOffset = Vector2.zero;

    private void Awake()
    {
        worldCamera = Camera.main;
        canvas = CombatManager.instance.combatCanvas;
        if(draggableRect == null) draggableRect = GetComponent<RectTransform>();
    }

    //============================================================================

    public void SnapUIToObject()
    {
        SnapUIToObject(targetObject.position);
    }

    public void SnapUIToObject(Vector3 worldPosition)
    {
        //Calculate world to screen
        Vector3 screenPos = worldCamera.WorldToScreenPoint(worldPosition);

        //Calculate screen to local space
        //Passing worldCamera is required for World Space canvases
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvas.GetComponent<RectTransform>(),
            screenPos,
            worldCamera,
            out Vector2 localPoint
        );

        //Apply position (plus optional nudge offset)
        draggableRect.anchoredPosition = localPoint + canvasOffset;
    }
}
