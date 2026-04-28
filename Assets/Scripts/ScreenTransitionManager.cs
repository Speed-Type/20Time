using System;
using UnityEngine;

public class ScreenTransitionManager : MonoBehaviour
{
    public static ScreenTransitionManager instance;

    //============================================================================

    [Header("References")]
    public CanvasGroup transitionCanvasGroup;

    [Header("Transition Parameters")]
    public float introTransitionDelay;
    public float transitionLength;

    //============================================================================

    private void Awake()
    {
        instance = this;
    }

    private void Start()
    {
        LeanTween.value(0, 0, introTransitionDelay).setOnComplete(() => {Transition_BlockedToVisible();});
    }

    //============================================================================

    public void Transition_BlockedToVisible(Action onComplete = null)
    {
        BaseTransition(1, 0, onComplete);
    }

    public void Transition_VisibleToBlocked(Action onComplete = null)
    {
        BaseTransition(0, 1, onComplete);
    }

    //============================================================================

    private void BaseTransition(float start, float end, Action onComplete = null)
    {
        transitionCanvasGroup.alpha = start;
        transitionCanvasGroup.interactable = true;
        transitionCanvasGroup.blocksRaycasts = true;

        LeanTween.value(start, end, transitionLength)
        .setOnUpdate((float val) =>
        {
            transitionCanvasGroup.alpha = val;
        })
        .setOnComplete(() =>
        {
            transitionCanvasGroup.interactable = false;
            transitionCanvasGroup.blocksRaycasts = false;

            if(onComplete != null) onComplete();
        });
    }
}
