using UnityEngine;

public class DroneWeakPoint : MonoBehaviour
{
    [Tooltip("Drag the SpawnerDrone root object here")]
    public SpawnerDrone parentDrone;
    public int damage = 1;

    public void TakeDamage(int incomingDamage)
    {
        if (parentDrone != null)
            parentDrone.TakeDamage(damage);
        else
            Debug.LogWarning("DroneWeakPoint hit but parentDrone is not assigned!", this);
    }
}
