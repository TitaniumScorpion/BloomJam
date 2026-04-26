using UnityEngine;

public class AutoDeactivate : MonoBehaviour
{
    [Tooltip("How long the particle effect should last before returning to the Object Pool")]
    public float lifetime = 0.1f; // Quick default for muzzle flashes
    
    private Transform originalParent;

    private void Awake()
    {
        // Remember the ObjectPooler so we can return to it cleanly later
        originalParent = transform.parent;
    }

    private void OnEnable()
    {
        // Start the timer to return this object to the pool
        Invoke(nameof(Deactivate), lifetime);
    }

    private void OnDisable()
    {
        CancelInvoke();
    }

    private void Deactivate()
    {
        // Unparent from the player's weapon so it doesn't get permanently stuck to it
        if (originalParent != null)
        {
            transform.SetParent(originalParent);
        }
        
        gameObject.SetActive(false);
    }
}