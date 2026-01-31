using UnityEngine;

public class AreaTracker : MonoBehaviour
{
    public CapsuleCollider playerCollider;
    public LayerMask areaMask = ~0;
    public bool debugCurrentArea = false;
    public float debugInterval = 0.2f;

    private string currentAreaName = string.Empty;
    private float nextDebugTime = 0f;

    public void RefreshCurrent()
    {
        currentAreaName = GetAreaName();
    }

    private void Update()
    {
        if (!debugCurrentArea || Time.time < nextDebugTime)
        {
            return;
        }

        string areaName = GetAreaName();
        //Debug.Log("Current area: " + (string.IsNullOrEmpty(areaName) ? "<none>" : areaName));
        nextDebugTime = Time.time + Mathf.Max(0.05f, debugInterval);
    }

    public bool EnteredNewArea(out string areaName)
    {
        areaName = GetAreaName();
        if (!string.IsNullOrEmpty(areaName) && areaName != currentAreaName)
        {
            currentAreaName = areaName;
            Debug.Log("Entered new area: " + currentAreaName);
            return true;
        }

        return false;
    }

    public string GetAreaNameNow()
    {
        return GetAreaName();
    }

    private string GetAreaName()
    {
        if (playerCollider == null)
        {
            return string.Empty;
        }

        Vector3 point0;
        Vector3 point1;
        float radius;
        GetCapsuleWorldPoints(playerCollider, out point0, out point1, out radius);

        Collider[] hits = Physics.OverlapCapsule(point0, point1, radius, areaMask, QueryTriggerInteraction.Collide);
        for (int i = 0; i < hits.Length; i++)
        {
            if (hits[i] != null)
            {
                return hits[i].name;
            }
        }

        return string.Empty;
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
}
