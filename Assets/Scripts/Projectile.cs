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

    //YILMAZ KOD MAESTER WAS HERE
    [SerializeField] private GameObject bulletGFX;
    [SerializeField] private float minSpeed = 450f;
    [SerializeField] private float maxSpeed = 600f;
    private float rotationSpeed;
    //YILMAZ KOD MAESTER WAS HERE


    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.useGravity = false;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        originalScale = transform.localScale;
    }


    //YILMAZ KOD MAESTER WAS HERE
    private void Start()
    {
        rotationSpeed = Random.Range(minSpeed, maxSpeed);

        // %50 ihtimalle negatif yap
        if (Random.value < 0.5f)
        {
            rotationSpeed *= -1f;
        }
    }
    private void Update()
    {
        bulletGFX.transform.Rotate(0, 0, rotationSpeed * Time.deltaTime);
    }
    //YILMAZ KOD MAESTER WAS HERE


    private void OnEnable()
    {
        transform.localScale = originalScale;
        rb.linearVelocity = transform.forward * speed;
        Invoke(nameof(Deactivate), lifetime);
    }

    private void OnDisable()
    {
        CancelInvoke();
    }

    private void Deactivate()
    {
        gameObject.SetActive(false);
    }

    private void OnCollisionEnter(Collision collision)
    {
        gameObject.SetActive(false);
    }

    // There is deliberately no spawn grace period here. One used to ignore every trigger for the
    // first 0.05s, which at 80 m/s blanked out the first 4m of flight - the muzzle already sits
    // ~2.4m ahead of the camera, so shots passed straight through anything within ~6m of the
    // player. It existed to stop bullets killing each other at the muzzle; the "PlayerBullet"
    // layer handles that now, and the Player tag check below covers the player's own capsule.
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player")) return;
        // Any enemy or weak point implements IDamageable, so one lookup covers them all
        bool hitEnemy = other.TryGetComponent(out IDamageable damageable);
        if (hitEnemy)
        {
            // Sparks MUST be spawned before the damage lands. Swarmers have 1 HP, so TakeDamage
            // deactivates the enemy and takes its collider out of the physics scene - querying
            // that collider afterwards cannot resolve, and the burst ends up at world origin.
            // -transform.forward reverses the direction of travel, so the burst sprays back
            // toward the player instead of through the enemy.
            HitSparks.SpawnOnCollider(other, transform.position, -transform.forward);
            damageable.TakeDamage(damage);
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