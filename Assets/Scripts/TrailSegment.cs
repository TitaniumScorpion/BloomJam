using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class TrailSegment : MonoBehaviour
{
    [Tooltip("Damage dealt to the player on contact")]
    public int damage = 1;
    [Tooltip("Seconds before the same segment can damage the player again")]
    public float damageCooldown = 1f;

    private float damageTimer;

    private void Awake()
    {
        Rigidbody rb = GetComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;

        Collider col = GetComponent<Collider>();
        if (col != null) col.isTrigger = true;
    }

    private void OnEnable()
    {
        damageTimer = 0f;
    }

    private void Update()
    {
        if (damageTimer > 0f) damageTimer -= Time.deltaTime;
    }

    private void OnTriggerEnter(Collider other) => HandleDamage(other);

    private void OnTriggerStay(Collider other) => HandleDamage(other);

    private void HandleDamage(Collider other)
    {
        if (damageTimer > 0f) return;
        if (other.CompareTag("Player")
            && other.TryGetComponent(out PlayerHealth health))
        {
            health.TakeDamage(damage);
            damageTimer = damageCooldown;
        }
    }
}
