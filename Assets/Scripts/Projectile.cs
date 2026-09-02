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

    private void OnCollisionEnter(Collision collision)
    {
        if (inGracePeriod) return;
        gameObject.SetActive(false);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (inGracePeriod) return;
        if (other.CompareTag("Player")) return;
        // Any enemy or weak point implements IDamageable, so one lookup covers them all
        bool hitEnemy = other.TryGetComponent(out IDamageable damageable);
        if (hitEnemy) damageable.TakeDamage(damage);

        if (hitEnemy && AudioManager.Instance != null && AudioManager.Instance.hitSound != null)
        {
            // Give hit sounds a high priority (80) so they don't get lost in the mix
            AudioManager.Instance.PlaySoundAtLocation(AudioManager.Instance.hitSound, transform.position, AudioManager.Instance.hitVolume, Random.Range(0.9f, 1.1f), 80);
        }
        
        // Deactivate the projectile upon hitting anything (enemy, wall, floor, etc.)
        gameObject.SetActive(false);
    }
}