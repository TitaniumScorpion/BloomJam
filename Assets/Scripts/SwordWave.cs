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

        if (other.TryGetComponent(out IDamageable damageable))
        {
            damageable.TakeDamage(damage);
            return; // Pierce through enemies
        }

        // Stop on solid environment. Non-damageable colliders that belong to an enemy
        // (body parts, hitbox shells) are passed through rather than stopping the wave —
        // SpawnerDrone needs naming explicitly because only its weak points are damageable.
        if (!other.isTrigger && other.GetComponentInParent<IDamageable>() == null
            && other.GetComponentInParent<SpawnerDrone>() == null)
            gameObject.SetActive(false);
    }
}
