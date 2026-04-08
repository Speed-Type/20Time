using System;
using UnityEngine;
using UnityEngine.EventSystems;

public class BaseIconInteractable : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    //References
    private BaseCombatEntity baseCombatEntity;
    private RectTransform rectTransform;
    private Canvas canvas;

    //Action references
    private Action<Vector2> onReleaseAction;

    //Pullback variables
    private Vector2 pullbackVector;

    //============================================================================

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvas = GetComponentInParent<Canvas>();
    }

    public void SetBaseCombatEntity(BaseCombatEntity pBaseCombatEntity)
    {
        baseCombatEntity = pBaseCombatEntity;
    }

    public void SetOnReleaseAction(Action<Vector2> pOnReleaseAction)
    {
        onReleaseAction = pOnReleaseAction;
    }

    //=============================Input handlers==================================

    public void OnPointerDown(PointerEventData eventData)
    {
        if(!baseCombatEntity.isMovable) return;

        Debug.Log("Begin drag");

        //Reset pullback vector
        pullbackVector = new Vector2(0, 0);

        //Todo: instantiate an arrow indicator or something here
    }

    public void OnDrag(PointerEventData eventData)
    {
        if(!baseCombatEntity.isMovable) return;

        //rectTransform.anchoredPosition += eventData.delta / canvas.scaleFactor;

        //Update pullback vector
        pullbackVector -= eventData.delta / canvas.scaleFactor;
        pullbackVector = Vector2.ClampMagnitude(pullbackVector, baseCombatEntity.maxPullback);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if(!baseCombatEntity.isMovable) return;

        Debug.Log(pullbackVector);
    }
}
