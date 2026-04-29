using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.InputSystem;
using System.Collections;

public class TooltipManager : MonoBehaviour
{
    public static TooltipManager instance;

    [Header("References")]
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI healthText;
    [SerializeField] private TextMeshProUGUI descriptionText;
    [SerializeField] private RectTransform tooltipBackground; //Background of the tooltip
    [SerializeField] private RectTransform tooltipParentRectTransform;
    [SerializeField] private CanvasGroup tooltipCanvasGroup;

    [Header("Other Settings")]
    [SerializeField] private float mouseOffset = 14f;
    [SerializeField] private float tooltipShowDelay = 0.4f;
    [SerializeField] private float tooltipHideDelay = 0.1f;
    [SerializeField] private float fadeDuration = 0.15f;

    private Coroutine showTooltipCoroutine;
    private Coroutine hideTooltipCoroutine;

    void Awake()
    {
        //Set instance
        instance = this;

        //Hide tooltip for now
        HideTooltip();
    }

    void Update()
    {
        //Dynamically position and anchor the tooltip based on mouse position
        //Make sure it doesn't go off screen by clamping (flipping pivot didnt' really work)
        Vector2 screenPoint = Mouse.current.position.ReadValue() + new Vector2(mouseOffset, -mouseOffset);

        float width = tooltipBackground.sizeDelta.x;
        float height = tooltipBackground.sizeDelta.y;

        screenPoint.x = Mathf.Min(screenPoint.x, Screen.width - width);
        screenPoint.y = Mathf.Max(screenPoint.y, height);

        tooltipParentRectTransform.position = screenPoint;
    }

    //Coroutine to wait before showing the tooltip
    IEnumerator ShowTooltipDelayed(float start)
    {
        yield return new WaitForSeconds(tooltipShowDelay);

        tooltipParentRectTransform.gameObject.SetActive(true);

        yield return FadeTooltip(start, 0.85f);
    }

    //Coroutine to fade in or out
    IEnumerator FadeTooltip(float start, float end)
    {
        float time = 0f;

        tooltipCanvasGroup.alpha = start;

        while (time < fadeDuration)
        {
            time += Time.deltaTime;
            tooltipCanvasGroup.alpha = Mathf.Lerp(start, end, time / fadeDuration);
            yield return null;
        }

        tooltipCanvasGroup.alpha = end;
    }

    //Helper method to set the text of one chunk of the tooltip
    private void SetSingleTooltipText(TextMeshProUGUI tmp, string text)
    {
        if(text.Equals(""))
        {
            tmp.gameObject.SetActive(false);
        }
        else
        {
            tmp.gameObject.SetActive(true);
            tmp.text = text;
        }
    }

    //Shows tooltip with set text for title, health, and description
    public void ShowTooltip(string title, string health, string description)
    {
        //Necessary for grace period between shows/hides
        if(hideTooltipCoroutine != null)
        {
            StopCoroutine(hideTooltipCoroutine);
            hideTooltipCoroutine = null;

            tooltipCanvasGroup.alpha = 0.85f;
        }

        //Set texts
        SetSingleTooltipText(titleText, title);
        SetSingleTooltipText(healthText, health);
        SetSingleTooltipText(descriptionText, description);

        //If we're going from object to object, don't have to delay or fade in/out
        if(tooltipParentRectTransform.gameObject.activeSelf)
        {
            return;
        }

        if(showTooltipCoroutine != null)
        {
            StopCoroutine(showTooltipCoroutine);
        }

        showTooltipCoroutine = StartCoroutine(ShowTooltipDelayed(0f));
    }

    //Shows tooltip with simple text
    public void ShowTooltip(string simpleText)
    {
        //Just call multi-parameter function with every other field but description empty
        ShowTooltip("", "", simpleText);
    }

    //Hides tooltip
    public void HideTooltip()
    {
        //Stop the tooltip delay, like a dash cancel
        if(showTooltipCoroutine != null)
        {
            StopCoroutine(showTooltipCoroutine);
        }

        hideTooltipCoroutine = StartCoroutine(HideTooltipDelayed());
    }

    //Coroutine to give a bit of grace period before hiding
    IEnumerator HideTooltipDelayed()
    {
        yield return new WaitForSeconds(tooltipHideDelay);

        yield return FadeTooltip(0.85f, 0f);

        tooltipParentRectTransform.gameObject.SetActive(false);
    }
}