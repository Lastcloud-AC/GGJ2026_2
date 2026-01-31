using UnityEngine;

public class MaskController : MonoBehaviour
{
    public BoxCollider maskCollider;
    public Vector2Int size = new Vector2Int(3, 3);
    public float cellSize = 1f;
    public float boxHeight = 1f;

    [Header("Player Collider")]
    public CapsuleCollider playerCollider;

    public Color gizmoColor = new Color(0f, 0.8f, 1f, 0.35f);

    private void Awake()
    {
        if (maskCollider == null)
        {
            maskCollider = GetComponent<BoxCollider>();
        }
    }

    private void Start()
    {
        UpdateBoxSize();
    }

    private void OnValidate()
    {
        UpdateBoxSize();
    }

    public void UpdateBoxSize()
    {
        if (maskCollider == null)
        {
            return;
        }

        float width = Mathf.Max(1, size.x) * cellSize;
        float depth = Mathf.Max(1, size.y) * cellSize;
        maskCollider.size = new Vector3(width, Mathf.Max(0.1f, boxHeight), depth);
        maskCollider.center = Vector3.zero;
    }

    public bool IsPointInsideMaskXZ(Vector3 point, float margin = 0f)
    {
        if (maskCollider == null)
        {
            return false;
        }

        Bounds bounds = maskCollider.bounds;
        Vector3 min = bounds.min;
        Vector3 max = bounds.max;

        return point.x >= min.x + margin && point.x <= max.x - margin
            && point.z >= min.z + margin && point.z <= max.z - margin;
    }

    public bool TryMove(Vector3 direction)
    {
        Vector3 dir = new Vector3(direction.x, 0f, direction.z).normalized;
        if (dir.sqrMagnitude < 0.001f || maskCollider == null)
        {
            return false;
        }

        Vector3 targetCenter = transform.position + dir * cellSize;
        if (!ColliderWithinBounds(targetCenter))
        {
            return false;
        }

        transform.position = targetCenter;
        AudioManager.Instance.PlaySfx("MaskMovement");
        return true;
    }

    private bool ColliderWithinBounds(Vector3 targetCenter)
    {
        if (playerCollider == null)
        {
            return false;
        }

        Bounds bounds = new Bounds(targetCenter, maskCollider.bounds.size);
        GetCapsuleWorldPoints(playerCollider, out Vector3 p0, out Vector3 p1, out float radius);

        return PointSphereInsideBoundsXZ(bounds, p0, radius)
            && PointSphereInsideBoundsXZ(bounds, p1, radius);
    }

    private static bool PointSphereInsideBoundsXZ(Bounds bounds, Vector3 point, float radius)
    {
        Vector3 min = bounds.min;
        Vector3 max = bounds.max;

        return point.x >= min.x + radius && point.x <= max.x - radius
            && point.z >= min.z + radius && point.z <= max.z - radius;
    }

    private static void GetCapsuleWorldPoints(CapsuleCollider capsule, out Vector3 p0, out Vector3 p1, out float radius)
    {
        Transform t = capsule.transform;
        Vector3 center = t.TransformPoint(capsule.center);

        Vector3 scale = t.lossyScale;
        int dir = capsule.direction;

        Vector3 axis;
        float heightScale;
        float radiusScale;
        if (dir == 0)
        {
            axis = t.right;
            heightScale = scale.x;
            radiusScale = Mathf.Max(scale.y, scale.z);
        }
        else if (dir == 1)
        {
            axis = t.up;
            heightScale = scale.y;
            radiusScale = Mathf.Max(scale.x, scale.z);
        }
        else
        {
            axis = t.forward;
            heightScale = scale.z;
            radiusScale = Mathf.Max(scale.x, scale.y);
        }

        float height = Mathf.Max(capsule.height * heightScale, capsule.radius * 2f);
        radius = capsule.radius * radiusScale;

        float half = Mathf.Max(0f, (height * 0.5f) - radius);
        Vector3 offset = axis * half;

        p0 = center + offset;
        p1 = center - offset;
    }

    private void OnDrawGizmos()
    {
        BoxCollider collider = maskCollider != null ? maskCollider : GetComponent<BoxCollider>();
        if (collider == null)
        {
            return;
        }

        Vector3 center = collider.bounds.center;
        Vector3 boundsSize = collider.bounds.size;
        Vector3 sizeWorld = new Vector3(boundsSize.x, 0.1f, boundsSize.z);

        Gizmos.color = gizmoColor;
        Gizmos.DrawWireCube(center, sizeWorld);
        Gizmos.color = new Color(gizmoColor.r, gizmoColor.g, gizmoColor.b, 0.1f);
        Gizmos.DrawCube(center, sizeWorld);
    }
}
