using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class ImpactBomb : MonoBehaviour
{
    [Header("Bomb Settings")]
    public float throwForce = 25f;
    public float upwardForce = 3f;
    public float detonationTime = 2f;
    
    [Header("Explosion Settings")]
    public float explosionRadius = 6f;
    public int explosionDamage = 10;
    public float playerBlastForce = 35f;

    private Rigidbody rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.useGravity = true; // Bombs need gravity to bounce and roll
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
    }

    private void OnEnable()
    {
        // Reset velocity to ensure consistent throws when pulling from the Object Pool
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        // Apply the throw force (forward + slight upward arc)
        rb.AddForce(transform.forward * throwForce + transform.up * upwardForce, ForceMode.VelocityChange);

        // Detonate after the fuse timer finishes
        Invoke(nameof(Detonate), detonationTime);
    }

    private void OnDisable()
    {
        CancelInvoke();
    }

    private void Detonate()
    {
        // Create a sphere to detect everything caught in the blast
        Collider[] colliders = Physics.OverlapSphere(transform.position, explosionRadius);

        foreach (Collider hit in colliders)
        {
            // 1. Damage Standard Swarmers
            if (hit.TryGetComponent(out StandardSwarmer swarmer))
            {
                swarmer.TakeDamage(explosionDamage);
            }
            // 2. Damage Elite Boss Weak Points
            else if (hit.TryGetComponent(out EnemyWeakPoint weakPoint))
            {
                weakPoint.TakeDamage(explosionDamage);
            }
            // 3. Bomb Jump: Launch the Player!
            else if (hit.CompareTag("Player") && hit.TryGetComponent(out Rigidbody playerRb))
            {
                // Calculate direction from explosion center to the player
                Vector3 blastDir = (hit.transform.position - transform.position).normalized;
                
                // Force the blast direction to always push slightly upward so the jump is reliable
                blastDir.y = Mathf.Max(blastDir.y, 0.5f);
                blastDir = blastDir.normalized;

                // Nullify current falling velocity so the blast always achieves full height
                playerRb.linearVelocity = new Vector3(playerRb.linearVelocity.x, 0f, playerRb.linearVelocity.z);
                
                playerRb.AddForce(blastDir * playerBlastForce, ForceMode.Impulse);
            }
        }

        // Deactivate and return to the Object Pool
        gameObject.SetActive(false);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, explosionRadius);
    }
}