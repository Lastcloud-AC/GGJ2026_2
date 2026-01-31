using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform target;
    public Vector3 offset = new Vector3(0f, 10f, -10f);
    public float smoothTime = 0.2f;
    public bool followRotation = false;

    private Vector3 velocity = Vector3.zero;

    private void LateUpdate()
    {
        if (target == null)
        {
            return;
        }

        Vector3 desired = target.position + offset;
        transform.position = Vector3.SmoothDamp(transform.position, desired, ref velocity, Mathf.Max(0.01f, smoothTime));

        if (followRotation)
        {
            transform.rotation = target.rotation;
        }
    }
}
