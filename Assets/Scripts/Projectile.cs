using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class Projectile : MonoBehaviour
{
    [Header("Projectile Settings")]
    public float speed = 80f;
    public float lifetime = 2f;
    public int damage = 1; // Standard swarmer takes 1 shot, so 1 damage is perfect

    private Rigidbody rb;
    private Vector3 originalScale;
    private bool inGracePeriod;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.useGravity = false;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        originalScale = transform.localScale;
    }

    private void OnEnable()
    {
        transform.localScale = originalScale;
        rb.linearVelocity = transform.forward * speed;
        inGracePeriod = true;
        Invoke(nameof(EndGracePeriod), 0.05f);
        Invoke(nameof(Deactivate), lifetime);
    }

    private void OnDisable()
    {
        CancelInvoke();
        inGracePeriod = false;
    }

    private void EndGracePeriod() => inGracePeriod = false;

    private void Deactivate()
    {
        gameObject.SetActive(false);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (inGracePeriod) return;
        if (other.CompareTag("Player")) return;
        bool hitEnemy = false;

        // Check if the object we hit has the StandardSwarmer script
        if (other.TryGetComponent(out StandardSwarmer enemy))
        {
            enemy.TakeDamage(damage);
            hitEnemy = true;
        }
        // Check if we hit an Elite boss's weak point
        else if (other.TryGetComponent(out EnemyWeakPoint weakPoint))
        {
            weakPoint.TakeDamage(damage);
            hitEnemy = true;
        }
        else if (other.TryGetComponent(out DroneWeakPoint dronePoint))
        {
            dronePoint.TakeDamage(damage);
            hitEnemy = true;
        }

        if (hitEnemy && AudioManager.Instance != null && AudioManager.Instance.hitSound != null)
        {
            // Give hit sounds a high priority (80) so they don't get lost in the mix
            AudioManager.Instance.PlaySoundAtLocation(AudioManager.Instance.hitSound, transform.position, AudioManager.Instance.hitVolume, Random.Range(0.9f, 1.1f), 80);
        }
        
        // Deactivate the projectile upon hitting anything (enemy, wall, floor, etc.)
        gameObject.SetActive(false);
    }
}