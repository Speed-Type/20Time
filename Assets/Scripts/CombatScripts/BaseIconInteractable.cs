using System;
using UnityEngine;
using UnityEngine.EventSystems;

public class BaseIconInteractable : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    [Tooltip("Child TrajectoryArrow component for preview drawing")]
    [SerializeField] private TrajectoryArrow trajectoryArrow;

    //============================================================================

    //Component references
    private BaseCombatEntity baseCombatEntity;
    private RectTransform rectTransform;
    private Canvas canvas;
    private Camera worldCamera;

    //Action references
    private Action<Vector3, float> onReleaseAction;

    //Pullback variables
    private Vector2 pullbackVector;
    private Vector3 originWorldPos;
    private Vector3 currentDragWorldPos;

    //State variables
    private bool isDragging;

    //============================================================================

    private void Awake()
    {
        //Initialize references
        rectTransform = GetComponent<RectTransform>();
        canvas = GetComponentInParent<Canvas>();
        worldCamera = Camera.main;
    }

    public void SetBaseCombatEntity(BaseCombatEntity pBaseCombatEntity)
    {
        baseCombatEntity = pBaseCombatEntity;
    }

    public void SetOnReleaseAction(Action<Vector3, float> pOnReleaseAction)
    {
        onReleaseAction = pOnReleaseAction;
    }

    //=============================Input handlers==================================

    public void OnPointerDown(PointerEventData eventData)
    {
        if(baseCombatEntity.isStatic) return;
        if(!baseCombatEntity.isCorrectTurn) return;

        isDragging = true;

        originWorldPos = rectTransform.position;
        currentDragWorldPos = originWorldPos;

        trajectoryArrow?.SetVisible(false); //Show arrow
    }

    public void OnDrag(PointerEventData eventData)
    {
        if(baseCombatEntity.isStatic) return;
        if(!baseCombatEntity.isCorrectTurn) return;
        if(!isDragging) return;

        Vector3 pointerWorld = ScreenToWorld(eventData.position);

        //Offset from origin, clamped to maxDragDistance
        Vector3 dragOffset = pointerWorld - originWorldPos;
        if(dragOffset.magnitude > baseCombatEntity.maxPullback)
            dragOffset = dragOffset.normalized * baseCombatEntity.maxPullback;

        currentDragWorldPos = originWorldPos + dragOffset;

        //Launch direction is OPPOSITE to the drag direction (angry-birds style)
        Vector3 launchDir = -dragOffset;

        //Update arrow preview
        trajectoryArrow?.UpdateArrow(originWorldPos, launchDir, dragOffset.magnitude / baseCombatEntity.maxPullback);
        trajectoryArrow?.SetVisible(dragOffset.magnitude > 0.05f);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if(baseCombatEntity.isStatic) return;
        if(!baseCombatEntity.isCorrectTurn) return;
        if(!isDragging) return;

        isDragging = false;

        trajectoryArrow?.SetVisible(false);

        Vector3 dragOffset = currentDragWorldPos - originWorldPos;

        //Only launch if there was a meaningful drag
        if(dragOffset.magnitude < 0.05f) return;

        Vector3 launchDirection = -dragOffset.normalized;
        float launchSpeed = (dragOffset.magnitude / baseCombatEntity.maxPullback);

        onReleaseAction(launchDirection, launchSpeed);
    }

    //=============================Helper Methods=================================

    private Vector3 ScreenToWorld(Vector2 screenPos)
    {
        //Use the canvas plane's Z depth (distance from camera to canvas)
        float canvasZ = worldCamera.WorldToScreenPoint(canvas.transform.position).z;

        Vector3 screenPoint = new Vector3(screenPos.x, screenPos.y, canvasZ);
        return worldCamera.ScreenToWorldPoint(screenPoint);
    }
}
