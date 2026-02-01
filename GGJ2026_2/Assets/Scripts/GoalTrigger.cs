using UnityEngine;

public class GoalTrigger : MonoBehaviour
{
    public SceneM sceneManager;
    public Collider goalCollider;

    private bool triggered = false;
    private Collider playerCollider;

    private void Awake()
    {
        playerCollider = GetComponent<Collider>();
    }

    private void Update()
    {
        if (triggered || sceneManager == null || goalCollider == null || playerCollider == null)
        {
            return;
        }

        if (playerCollider.bounds.Intersects(goalCollider.bounds))
        {
            triggered = true;
            sceneManager.SceneLoading();
        }
    }
}