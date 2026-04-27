using UnityEngine;

public class ArenaWalls : MonoBehaviour
{
    [Header("Boundary (World Space, XZ Plane)")]
    [Tooltip("Radius of the circular arena in world units.")]
    public float radius = 5f;

    //Center of the arena on the XZ plane
    public Vector2 center => new Vector2(transform.position.x, transform.position.z);

#if UNITY_EDITOR
    void OnDrawGizmos()
    {
        Gizmos.color = Color.cyan;
        Vector3 center3 = new Vector3(transform.position.x, transform.position.y, transform.position.z);
        int segments = 64;
        for (int i = 0; i < segments; i++)
        {
            float a1 = i       / (float)segments * Mathf.PI * 2f;
            float a2 = (i + 1) / (float)segments * Mathf.PI * 2f;
            Gizmos.DrawLine(
                center3 + new Vector3(Mathf.Cos(a1), 0f, Mathf.Sin(a1)) * radius,
                center3 + new Vector3(Mathf.Cos(a2), 0f, Mathf.Sin(a2)) * radius);
        }
    }
#endif
}
