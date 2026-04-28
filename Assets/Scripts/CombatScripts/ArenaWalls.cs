using UnityEngine;

public class ArenaWalls : MonoBehaviour
{
    [Header("Boundary (World Space, XZ Plane)")]
    [Tooltip("Half-width of the ellipse along the X axis.")]
    public float radiusX = 14f;
    [Tooltip("Half-height of the ellipse along the Z axis.")]
    public float radiusZ = 10f;
 
    //Center of the arena on the XZ plane
    public Vector2 center => new Vector2(transform.position.x, transform.position.z);
 
    //Returns how far outside the ellipse boundary a token of given radius is.
    //Positive = outside, zero or negative = inside.
    //Also outputs the inward-pointing surface normal at the token's position.
    public float GetPenetration(Vector2 tokenPos, float tokenRadius, out Vector2 inwardNormal)
    {
        Vector2 local = tokenPos - center;
 
        // Shrink the ellipse by the token radius so the token's edge, not its
        // center, is what we keep inside. We approximate the offset ellipse by
        // scaling both radii down by tokenRadius — exact for circles, a good
        // approximation for tokens that are small relative to the arena.
        float rx = Mathf.Max(radiusX - tokenRadius, 0.001f);
        float rz = Mathf.Max(radiusZ - tokenRadius, 0.001f);
 
        // Ellipse equation: (x/rx)² + (z/rz)² = 1
        // Values > 1 mean the point is outside.
        float ellipseVal = (local.x * local.x) / (rx * rx)
                         + (local.y * local.y) / (rz * rz);
 
        // The gradient of the ellipse function gives the outward normal.
        // Gradient = (2x/rx², 2z/rz²) — normalised for direction.
        Vector2 gradient = new Vector2(
            local.x / (rx * rx),
            local.y / (rz * rz));
 
        float gradMag = gradient.magnitude;
        inwardNormal  = gradMag > 1e-6f ? -gradient / gradMag : Vector2.up;
 
        if (ellipseVal <= 1f)
        {
            // Inside — return negative penetration (no overlap)
            return -(1f - Mathf.Sqrt(ellipseVal)) * Mathf.Min(rx, rz);
        }
 
        // Outside — approximate penetration depth.
        // We find the nearest point on the ellipse along the gradient direction
        // and return the distance from the token center to that point.
        // For a quick approximation: scale local back onto the ellipse surface.
        float scale        = 1f / Mathf.Sqrt(ellipseVal);
        Vector2 nearestPt  = center + local * scale;
        return (tokenPos - nearestPt).magnitude;
    }
 
#if UNITY_EDITOR
    void OnDrawGizmos()
    {
        Gizmos.color = Color.cyan;
        Vector3 center3  = transform.position;
        int     segments = 64;
        for (int i = 0; i < segments; i++)
        {
            float a1 = i       / (float)segments * Mathf.PI * 2f;
            float a2 = (i + 1) / (float)segments * Mathf.PI * 2f;
            Gizmos.DrawLine(
                center3 + new Vector3(Mathf.Cos(a1) * radiusX, 0f, Mathf.Sin(a1) * radiusZ),
                center3 + new Vector3(Mathf.Cos(a2) * radiusX, 0f, Mathf.Sin(a2) * radiusZ));
        }
    }
#endif
}
