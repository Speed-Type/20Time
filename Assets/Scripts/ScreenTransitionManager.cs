using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ScreenTransitionManager : MonoBehaviour
{
    public static ScreenTransitionManager instance;

    //============================================================================

    [Header("References")]
    public CanvasGroup transitionCanvasGroup;

    [Header("Transition Parameters")]
    public float introTransitionDelay;
    public float transitionLength;
    public float afterFadeToBlackDelay;

    //============================================================================

    private void Awake()
    {
        instance = this;
    }

    private void Start()
    {
        LeanTween.value(0, 0, introTransitionDelay).setOnComplete(() => {Transition_BlockedToVisible();});
    }

    //=================================Scene Management===========================================

    public void TransitionToScene(string sceneName)
    {
        Transition_VisibleToBlocked(() =>
        {
            SceneManager.LoadScene(sceneName);
        });
    }

    public void ReloadScene()
    {
        TransitionToScene(SceneManager.GetActiveScene().name);
    }

    //===============================Transitions=============================================

    public void Transition_BlockedToVisible(Action onComplete = null)
    {
        BaseTransition(1, 0, onComplete);
    }

    public void Transition_VisibleToBlocked(Action onComplete = null)
    {
        BaseTransition(0, 1, () =>
        {
            LeanTween.delayedCall(afterFadeToBlackDelay, onComplete);
        });
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
