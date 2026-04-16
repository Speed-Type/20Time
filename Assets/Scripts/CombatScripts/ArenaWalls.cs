using UnityEngine;

public class ArenaWalls : MonoBehaviour
{
    [Header("Boundary (World Space, XZ Plane)")]
    public float left = -10f;
    public float right = 10f;
    public float bottom = -10f;
    public float top = 10f;

    //Nice gizmo

#if UNITY_EDITOR
    void OnDrawGizmos()
    {
        Gizmos.color = Color.cyan;
        Vector3 center = new Vector3((left + right) * 0.5f, transform.position.y, (bottom + top) * 0.5f);
        Vector3 size   = new Vector3(right - left, 0.05f, top - bottom);
        Gizmos.DrawWireCube(center, size);
    }
#endif
}
