using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class SwordWave : MonoBehaviour
{
    public float speed = 20f;
    public float lifetime = 1.5f;
    public int damage = 2;
    [Tooltip("Uncheck on the BulletTimeSwordWave prefab so it keeps moving during bullet time")]
    public bool freezeDuringBulletTime = true;

    private Rigidbody rb;
    private bool wasFrozen;
    private Vector3 frozenVelocity;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.useGravity = false;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
    }

    private void OnEnable()
    {
        wasFrozen = false;
        rb.linearVelocity = transform.forward * speed;
        Invoke(nameof(Deactivate), lifetime);
    }

    private void OnDisable() => CancelInvoke();

    private void Deactivate() => gameObject.SetActive(false);

    private void FixedUpdate()
    {
        if (freezeDuringBulletTime && KatanaWeapon.IsBulletTimeActive)
        {
            if (!wasFrozen)
            {
                frozenVelocity = rb.linearVelocity;
                rb.linearVelocity = Vector3.zero;
                wasFrozen = true;
            }
            return;
        }

        if (wasFrozen)
        {
            rb.linearVelocity = frozenVelocity;
            wasFrozen = false;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player")) return;

        if (other.TryGetComponent(out StandardSwarmer swarmer))
        {
            swarmer.TakeDamage(damage);
            return; // Pierce through enemies
        }
        if (other.TryGetComponent(out EnemyWeakPoint weakPoint))
        {
            weakPoint.TakeDamage(damage);
            return;
        }
        if (other.TryGetComponent(out DroneWeakPoint dronePoint))
        {
            dronePoint.TakeDamage(damage);
            return;
        }
        if (other.TryGetComponent(out DasherEnemy dasher))
        {
            dasher.TakeDamage(damage);
            return;
        }
        if (other.TryGetComponent(out TrailEnemy trailEnemy))
        {
            trailEnemy.TakeDamage(damage);
            return;
        }

        // Stop on solid environment
        if (!other.isTrigger && other.GetComponentInParent<AdvancedEnemy>() == null
            && other.GetComponentInParent<SpawnerDrone>() == null
            && other.GetComponentInParent<DasherEnemy>() == null
            && other.GetComponentInParent<TrailEnemy>() == null)
            gameObject.SetActive(false);
    }
}
