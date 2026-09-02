using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class EnemyProjectile : MonoBehaviour
{
    public int damage = 1;
    public float lifetime = 5f;
    private Rigidbody rb;
    private BulletTimeFreeze bulletTimeFreeze;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.useGravity = true;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
    }

    private void OnEnable()
    {
        bulletTimeFreeze.Reset();
        Invoke(nameof(Deactivate), lifetime);
    }

    private void OnDisable()
    {
        CancelInvoke();
    }

    private void FixedUpdate()
    {
        // Gravity is stored and restored by the helper, so the arc resumes mid-flight
        // exactly where it left off rather than dropping straight down.
        bulletTimeFreeze.Tick(rb);
    }

    private void Deactivate()
    {
        gameObject.SetActive(false);
    }

    private void OnTriggerEnter(Collider other)
    {
        // 1. If it hits the player, deal damage and explode
        if (other.CompareTag("Player") && other.TryGetComponent(out PlayerHealth playerHealth))
        {
            playerHealth.TakeDamage(damage);
            gameObject.SetActive(false);
            return;
        }
        
        // 2. Ignore collisions with the boss itself, standard swarmers, and abstract trigger zones
        if (other.isTrigger || other.GetComponentInParent<AdvancedEnemy>() != null || other.GetComponent<StandardSwarmer>() != null)
        {
            return;
        }
        
        // 3. If it hits anything else (the floor, walls), deactivate it
        gameObject.SetActive(false);
    }
}