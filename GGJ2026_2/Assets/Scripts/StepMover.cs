using System.Collections;
using UnityEngine;

public class StepMover : MonoBehaviour
{
    public Transform target;
    public float stepDistance = 1f;
    public float stepSpeed = 8f;
    public float stepPause = 0.1f;

    [Header("Wall Probe")]
    public bool enableWallCheck = true;
    public string wallTag = "w";
    public float probeRadius = 0.1f;
    public Transform probeUp;
    public Transform probeDown;
    public Transform probeLeft;
    public Transform probeRight;
    public float bumpDistance = 0.2f;
    public float bumpSpeed = 12f;

    [Header("Trap")]
    public bool enableTrapCheck = true;
    public string trapTag = "trap";
    public float trapCheckRadius = 0.2f;
    public LevelManager levelManager;

    [Header("Mask")]
    public MaskController maskController;
    public float maskProbeMargin = 0f;

    public bool IsMoving { get; private set; }

    public IEnumerator MoveStep(Vector3 direction)
    {
        if (target == null)
        {
            yield break;
        }

        IsMoving = true;

        Transform probe = GetProbe(direction);
        bool wallBlocked = enableWallCheck && IsBlocked(probe);
        bool maskBlocked = maskController != null && probe != null
            && !maskController.IsPointInsideMaskXZ(probe.position, maskProbeMargin);
        if (wallBlocked || maskBlocked)
        {
            yield return StartCoroutine(Bump(direction));
            IsMoving = false;
            yield break;
        }

        Vector3 start = target.position;
        Vector3 end = start + direction.normalized * stepDistance;
        float t = 0f;
        float duration = Mathf.Max(0.01f, stepDistance / Mathf.Max(0.01f, stepSpeed));

        while (t < 1f)
        {
            t += Time.deltaTime / duration;
            target.position = Vector3.Lerp(start, end, Mathf.Clamp01(t));
            yield return null;
        }

        target.position = end;
        if (stepPause > 0f)
        {
            yield return new WaitForSeconds(stepPause);
        }

        if (enableTrapCheck && IsOnTrap())
        {
            if (levelManager != null)
            {
                levelManager.RestartLevel();
            }
        }

        IsMoving = false;
    }

    private bool IsBlocked(Transform probe)
    {
        if (probe == null)
        {
            return false;
        }

        Collider[] hits = Physics.OverlapSphere(probe.position, probeRadius, ~0, QueryTriggerInteraction.Ignore);
        for (int i = 0; i < hits.Length; i++)
        {
            if (hits[i] != null && hits[i].CompareTag(wallTag))
            {
                return true;
            }
        }

        return false;
    }

    private Transform GetProbe(Vector3 direction)
    {
        Vector3 dir = direction.normalized;
        if (Mathf.Abs(dir.z) >= Mathf.Abs(dir.x))
        {
            return dir.z >= 0f ? probeUp : probeDown;
        }

        return dir.x >= 0f ? probeRight : probeLeft;
    }

    private IEnumerator Bump(Vector3 direction)
    {
        Vector3 start = target.position;
        Vector3 end = start + direction.normalized * bumpDistance;
        float t = 0f;
        float duration = Mathf.Max(0.01f, bumpDistance / Mathf.Max(0.01f, bumpSpeed));

        while (t < 1f)
        {
            t += Time.deltaTime / duration;
            target.position = Vector3.Lerp(start, end, Mathf.Clamp01(t));
            yield return null;
        }

        t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / duration;
            target.position = Vector3.Lerp(end, start, Mathf.Clamp01(t));
            yield return null;
        }

        target.position = start;
    }

    private bool IsOnTrap()
    {
        if (target == null)
        {
            return false;
        }

        Collider[] hits = Physics.OverlapSphere(target.position, trapCheckRadius, ~0, QueryTriggerInteraction.Collide);
        for (int i = 0; i < hits.Length; i++)
        {
            if (hits[i] != null && hits[i].CompareTag(trapTag))
            {
                return true;
            }
        }

        return false;
    }
}
