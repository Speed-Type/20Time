using UnityEngine;

public class TrajectoryArrow : MonoBehaviour
{
    [Header("Arrow Appearance")]
    [Tooltip("Total length of the arrow shaft in world units.")]
    [SerializeField] private float arrowLength = 1.5f;

    [Tooltip("Length of each arrowhead wing in world units.")]
    [SerializeField] private float arrowHeadSize = 0.25f;

    [Tooltip("Angle of arrowhead wings from the shaft, in degrees.")]
    [SerializeField] private float arrowHeadAngle = 30f;

    [Tooltip("Width of the arrow line in world units.")]
    [SerializeField] private float lineWidth = 0.05f;

    [Tooltip("Color when drag is at minimum pull.")]
    [SerializeField] private Color colorMin = new Color(1f, 1f, 0f, 0.8f);

    [Tooltip("Color when drag is at maximum pull.")]
    [SerializeField] private Color colorMax = new Color(1f, 0.2f, 0f, 1f);

    //============================================================================

    private LineRenderer lineRenderer;
    private const int LinePointCount = 6;

    //============================================================================

    private void Awake()
    {
        lineRenderer = GetComponent<LineRenderer>();
        lineRenderer.useWorldSpace = true;
        lineRenderer.positionCount = LinePointCount;
        lineRenderer.startWidth = lineWidth;
        lineRenderer.endWidth = lineWidth;
        lineRenderer.numCapVertices = 4;

        SetVisible(false);
    }

    //============================================================================

    public void UpdateArrow(Vector3 origin, Vector3 launchDirection, float t)
    {
        if(launchDirection == Vector3.zero) return;

        t = Mathf.Clamp01(t);
        Vector3 dir = launchDirection.normalized;

        //Scale arrow length with pull strength so it grows as you pull back
        float scaledLength = arrowLength * t;
        Vector3 tip = origin + dir * scaledLength;

        //Arrowhead wings
        //Rotate dir around Y axis (top-down, so Y is up)
        Vector3 wingDirL = Quaternion.Euler(0f, arrowHeadAngle, 0f) * (-dir);
        Vector3 wingDirR = Quaternion.Euler(0f, -arrowHeadAngle, 0f) * (-dir);

        float headSize = arrowHeadSize * Mathf.Lerp(0.5f, 1f, t);
        Vector3 wingL = tip + wingDirL * headSize;
        Vector3 wingR = tip + wingDirR * headSize;

        //Keep arrow on the same y plane as the origin
        tip.y = origin.y;
        wingL.y = origin.y;
        wingR.y = origin.y;

        lineRenderer.SetPosition(0, origin);
        lineRenderer.SetPosition(1, tip);
        lineRenderer.SetPosition(2, wingL);
        lineRenderer.SetPosition(3, tip);
        lineRenderer.SetPosition(4, wingR);
        lineRenderer.SetPosition(5, tip);

        //Interpolate color based on pull strength
        Color c = Color.Lerp(colorMin, colorMax, t);
        lineRenderer.startColor = c;
        lineRenderer.endColor = c;
    }

    //Shows or hides the trajectory arrow
    public void SetVisible(bool visible)
    {
        lineRenderer.enabled = visible;
    }
}
