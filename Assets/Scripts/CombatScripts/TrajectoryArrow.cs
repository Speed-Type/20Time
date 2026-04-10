using UnityEngine;
using UnityEngine.UI;

public class TrajectoryArrow : MonoBehaviour
{
    [Header("Arrow Appearance")]
    [Tooltip("Max length of the arrow shaft in pixels")]
    [SerializeField] private float arrowLength = 100f;

    [Tooltip("Width of the arrow line in pixels")]
    [SerializeField] private float arrowWidth = 20f;

    [Tooltip("Color when drag is at minimum pull")]
    [SerializeField] private Color colorMin = new Color(1f, 1f, 0f, 0.8f);

    [Tooltip("Color when drag is at maximum pull")]
    [SerializeField] private Color colorMax = new Color(1f, 0.2f, 0f, 1f);

    //============================================================================

    private RectTransform rectTransform;
    private Image image;

    //============================================================================

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        image = GetComponent<Image>();

        SetVisible(false);
    }

    //============================================================================

    public void UpdateArrow(Vector3 origin, Vector3 launchDirection, float t)
    {
        if(launchDirection == Vector3.zero) return;

        //Set scale of arrow
        float scaledLength = arrowLength * t;
        rectTransform.sizeDelta = new Vector2(arrowWidth, scaledLength);

        //Set rotation of arrow
        Vector3 dir = Quaternion.LookRotation(launchDirection).eulerAngles;
        dir = new Vector3(0, 0, -dir.y);
        rectTransform.localRotation = Quaternion.Euler(dir);

        //Interpolate color based on pull strength
        Color c = Color.Lerp(colorMin, colorMax, t);
        image.color = c;
    }

    //Shows or hides the trajectory arrow
    public void SetVisible(bool visible)
    {
        image.enabled = visible;
    }
}
