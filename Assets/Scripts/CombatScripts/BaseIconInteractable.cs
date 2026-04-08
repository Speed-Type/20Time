using System;
using UnityEngine;
using UnityEngine.EventSystems;

public class BaseIconInteractable : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    private BaseCombatEntity baseCombatEntity;
    private Action onReleaseAction;
    private RectTransform rectTransform;
    private Canvas canvas;

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

    public void SetOnReleaseAction(Action pOnReleaseAction)
    {
        onReleaseAction = pOnReleaseAction;
    }

    //=============================Input handlers==================================

    public void OnPointerDown(PointerEventData eventData)
    {
        if(!baseCombatEntity.isMovable) return;

        Debug.Log("Begin drag");
    }

    public void OnDrag(PointerEventData eventData)
    {
        if(!baseCombatEntity.isMovable) return;

        //rectTransform.anchoredPosition += eventData.delta / canvas.scaleFactor;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if(!baseCombatEntity.isMovable) return;

        
    }
}
